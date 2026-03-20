using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MarkerMind;

public class PatternExtractor
{
    public List<PositionCluster> ExtractClusters(List<TelemetrySample> successfulSamples, PlayerRole role)
    {
        // Filter by role
        var roleSamples = successfulSamples.Where(s => s.Role == role).ToList();
        if (roleSamples.Count < 3) return new List<PositionCluster>();
        
        // DBSCAN parameters from config
        float eps = Plugin.Instance.Config.ClusterDistance; // 2.0 yalms default
        int minPoints = 3;
        
        var positions = roleSamples.Select(s => s.Position).ToList();
        var clusters = DBSCAN(positions, eps, minPoints);
        
        return clusters.Select(c => new PositionCluster
        {
            Centroid = CalculateCentroid(c),
            Radius = CalculateRadius(c),
            SampleCount = c.Count,
            Role = role
        }).ToList();
    }
    
    private List<List<Vector3>> DBSCAN(List<Vector3> points, float eps, int minPoints)
    {
        var clusters = new List<List<Vector3>>();
        var visited = new HashSet<int>();
        var noise = new HashSet<int>();
        
        for (int i = 0; i < points.Count; i++)
        {
            if (visited.Contains(i)) continue;
            
            visited.Add(i);
            var neighbors = GetNeighbors(points, i, eps);
            
            if (neighbors.Count < minPoints)
            {
                noise.Add(i);
            }
            else
            {
                var cluster = new List<Vector3>();
                ExpandCluster(points, i, neighbors, cluster, visited, eps, minPoints);
                if (cluster.Count > 0)
                    clusters.Add(cluster);
            }
        }
        
        return clusters;
    }
    
    private List<int> GetNeighbors(List<Vector3> points, int pointIdx, float eps)
    {
        var neighbors = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            if (i != pointIdx && Distance2D(points[pointIdx], points[i]) <= eps)
                neighbors.Add(i);
        }
        return neighbors;
    }
    
    private void ExpandCluster(List<Vector3> points, int pointIdx, List<int> neighbors, 
        List<Vector3> cluster, HashSet<int> visited, float eps, int minPoints)
    {
        cluster.Add(points[pointIdx]);
        
        int i = 0;
        while (i < neighbors.Count)
        {
            int neighborIdx = neighbors[i];
            
            if (!visited.Contains(neighborIdx))
            {
                visited.Add(neighborIdx);
                var neighborNeighbors = GetNeighbors(points, neighborIdx, eps);
                
                if (neighborNeighbors.Count >= minPoints)
                {
                    neighbors.AddRange(neighborNeighbors.Where(n => !neighbors.Contains(n)));
                }
            }
            
            if (!cluster.Contains(points[neighborIdx]))
            {
                cluster.Add(points[neighborIdx]);
            }
            
            i++;
        }
    }
    
    private float Distance2D(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }
    
    private Vector3 CalculateCentroid(List<Vector3> cluster)
    {
        return new Vector3(
            cluster.Average(p => p.X),
            cluster.Average(p => p.Y),
            cluster.Average(p => p.Z)
        );
    }
    
    private float CalculateRadius(List<Vector3> cluster)
    {
        var centroid = CalculateCentroid(cluster);
        return cluster.Max(p => Distance2D(p, centroid));
    }
}

public class PositionCluster
{
    public Vector3 Centroid { get; set; }
    public float Radius { get; set; }
    public int SampleCount { get; set; }
    public PlayerRole Role { get; set; }
}
