using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MarkerMind;

public class LearningEngine : IDisposable
{
    private readonly DataStore dataStore;
    private readonly PatternExtractor patternExtractor;
    
    // Current encounter state
    private string? currentEncounterId;
    private Dictionary<string, MechanicData> mechanicCache = new();
    
    // Active mechanic tracking
    private string? activeMechanicId;
    private Vector3? markerPosition;
    
    public LearningEngine()
    {
        dataStore = new DataStore();
        patternExtractor = new PatternExtractor();
        
        // Subscribe to telemetry events
        if (Plugin.Instance?.telemetry != null)
        {
            Plugin.Instance.telemetry.OnMechanicComplete += OnMechanicComplete;
        }
    }
    
    public void Update()
    {
        // Check for encounter changes
        var territoryId = Plugin.ClientState.TerritoryType;
        var newEncounterId = $"{territoryId}";
        
        if (newEncounterId != currentEncounterId)
        {
            LoadEncounter(newEncounterId);
        }
    }
    
    private void LoadEncounter(string encounterId)
    {
        currentEncounterId = encounterId;
        var data = dataStore.LoadEncounter(encounterId);
        
        if (data != null)
        {
            mechanicCache = data.Mechanics;
        }
        else
        {
            mechanicCache = new Dictionary<string, MechanicData>();
        }
    }
    
    public void StartMechanic(string mechanicId, string mechanicName)
    {
        activeMechanicId = mechanicId;
        
        // Get or create mechanic data
        if (!mechanicCache.TryGetValue(mechanicId, out var mechanicData))
        {
            mechanicData = new MechanicData
            {
                Name = mechanicName,
                Observations = 0,
                SuccessRate = 0,
                Confidence = 0
            };
            mechanicCache[mechanicId] = mechanicData;
        }
        
        // Calculate predicted position
        markerPosition = PredictPosition(mechanicData);
        
        // Start telemetry capture
        Plugin.Instance?.telemetry?.StartCapture(mechanicId);
    }
    
    public void EndMechanic(string outcome)
    {
        if (activeMechanicId == null) return;
        
        Plugin.Instance?.telemetry?.StopCapture(outcome);
        
        activeMechanicId = null;
        markerPosition = null;
    }
    
    private Vector3? PredictPosition(MechanicData mechanicData)
    {
        var role = Plugin.Instance?.gameState?.Role ?? PlayerRole.Unknown;
        var roleStr = role.ToString().ToLowerInvariant();
        
        // Find clusters for current role
        var roleClusters = mechanicData.Clusters
            .Where(c => c.Role.Equals(roleStr, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (roleClusters.Count == 0)
        {
            // Fallback: use any cluster
            roleClusters = mechanicData.Clusters.ToList();
        }
        
        if (roleClusters.Count == 0) return null;
        
        // Return centroid of largest cluster
        var bestCluster = roleClusters.OrderByDescending(c => c.SampleCount).First();
        return bestCluster.Centroid;
    }
    
    private void OnMechanicComplete(string mechanicId, List<TelemetrySample> samples, string outcome)
    {
        if (!mechanicCache.TryGetValue(mechanicId, out var mechanicData))
            return;
        
        // Update observations
        mechanicData.Observations++;
        
        // Update success rate
        float alpha = Plugin.Instance?.Config.EmaAlpha ?? 0.3f;
        float outcomeValue = outcome == "survived" ? 1.0f : 0.0f;
        mechanicData.SuccessRate = (mechanicData.SuccessRate * (1 - alpha)) + (outcomeValue * alpha);
        
        // Extract new clusters if survived
        if (outcome == "survived" && samples.Count > 0)
        {
            var role = samples.First().Role;
            var newClusters = patternExtractor.ExtractClusters(samples, role);
            
            // Merge with existing clusters
            foreach (var newCluster in newClusters)
            {
                var existing = mechanicData.Clusters
                    .FirstOrDefault(c => c.Role.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    // Update existing cluster with EMA
                    existing.Centroid = new Vector3(
                        existing.Centroid.X * (1 - alpha) + newCluster.Centroid.X * alpha,
                        existing.Centroid.Y * (1 - alpha) + newCluster.Centroid.Y * alpha,
                        existing.Centroid.Z * (1 - alpha) + newCluster.Centroid.Z * alpha
                    );
                    existing.SampleCount += newCluster.SampleCount;
                    existing.Radius = Math.Max(existing.Radius, newCluster.Radius);
                }
                else
                {
                    mechanicData.Clusters.Add(new ClusterData
                    {
                        Centroid = newCluster.Centroid,
                        Radius = newCluster.Radius,
                        SampleCount = newCluster.SampleCount,
                        Role = role.ToString().ToLowerInvariant()
                    });
                }
            }
        }
        
        // Update confidence
        UpdateConfidence(mechanicData);
        
        // Save to disk
        mechanicData.LastSeen = DateTime.UtcNow;
        if (currentEncounterId != null)
        {
            dataStore.SaveEncounter(currentEncounterId, new EncounterData
            {
                EncounterId = currentEncounterId,
                TerritoryId = Plugin.ClientState.TerritoryType,
                Mechanics = mechanicCache
            });
        }
    }
    
    private void UpdateConfidence(MechanicData mechanicData)
    {
        int minSamples = Plugin.Instance?.Config.MinSampleSize ?? 5;
        
        if (mechanicData.Observations < minSamples)
        {
            mechanicData.Confidence = 0;
            return;
        }
        
        // Confidence based on success rate and sample count
        float sampleConfidence = Math.Min(1.0f, (float)mechanicData.Observations / (minSamples * 2));
        float successConfidence = mechanicData.SuccessRate;
        
        // Cluster quality (tighter clusters = higher confidence)
        float clusterConfidence = 0.5f;
        if (mechanicData.Clusters.Count > 0)
        {
            var avgRadius = mechanicData.Clusters.Average(c => c.Radius);
            clusterConfidence = Math.Min(1.0f, 1.0f / (avgRadius + 0.1f));
        }
        
        mechanicData.Confidence = (sampleConfidence + successConfidence + clusterConfidence) / 3.0f;
    }
    
    public int GetDisclosureLevel(string mechanicId)
    {
        if (!mechanicCache.TryGetValue(mechanicId, out var mechanicData))
            return 1;
        
        float confidence = mechanicData.Confidence;
        
        if (confidence >= (Plugin.Instance?.Config.Level4Threshold ?? 0.85f))
            return 4;
        if (confidence >= (Plugin.Instance?.Config.Level3Threshold ?? 0.7f))
            return 3;
        if (confidence >= (Plugin.Instance?.Config.Level2Threshold ?? 0.5f))
            return 2;
        
        return 1;
    }
    
    public void Dispose()
    {
        if (Plugin.Instance?.telemetry != null)
        {
            Plugin.Instance.telemetry.OnMechanicComplete -= OnMechanicComplete;
        }
    }
}
