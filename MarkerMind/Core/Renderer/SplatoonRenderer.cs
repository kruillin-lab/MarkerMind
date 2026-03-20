using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json;

namespace MarkerMind;

/// <summary>
/// Renders visual markers via Splatoon Scripting system.
/// Creates Scripting-compatible layout JSON.
/// </summary>
public class SplatoonRenderer : IDisposable
{
    private bool isSplatoonAvailable = false;
    private Dictionary<string, SplatoonElement> activeElements = new();
    private int elementCounter = 0;
    private string layoutName = "MarkerMind_Dynamic";
    
    public SplatoonRenderer()
    {
        CheckSplatoonAvailability();
    }
    
    private void CheckSplatoonAvailability()
    {
        // Splatoon detection is done at render time
        // We assume it might be available and try file-based approach
        isSplatoonAvailable = true;
    }
    
    public void RenderMarker(string mechanicId, Vector3 position, int disclosureLevel, PlayerRole role)
    {
        // Clear previous elements for this mechanic
        RemoveElementsForMechanic(mechanicId);
        
        // Generate Splatoon Scripting layout
        var layout = GenerateLayout(mechanicId, position, disclosureLevel);
        
        // Try to inject into Splatoon
        if (InjectIntoSplatoon(layout))
        {
            Plugin.Chat.Print($"[MarkerMind] Marker rendered at ({position.X:F1}, {position.Z:F1})");
        }
        else
        {
            RenderChatFallback(mechanicId, position, disclosureLevel);
        }
    }
    
    private string GenerateLayout(string mechanicId, Vector3 position, int disclosureLevel)
    {
        var elements = new List<SplatoonElement>();
        
        Plugin.Chat.Print($"[MarkerMind DEBUG] GenerateLayout called with level {disclosureLevel}");
        
        // Add elements based on disclosure level
        switch (disclosureLevel)
        {
            case 1:
                Plugin.Chat.Print("[MarkerMind DEBUG] Adding danger circle for level 1");
                elements.Add(CreateCircle(position, 5.0f, 0xFF0000FF, "Danger"));
                break;
            case 2:
                elements.Add(CreateCircle(position, 5.0f, 0xFF0000FF, "Danger"));
                elements.Add(CreateCircle(position + new Vector3(0, 0, 5), 2.0f, 0xFF00FF00, "Safe"));
                break;
            case 3:
            case 4:
                elements.Add(CreateCircle(position, 5.0f, 0xFF0000FF, "Danger"));
                elements.Add(CreateCircle(position + new Vector3(0, 0, 5), 2.0f, 0xFF00FF00, "Safe"));
                if (Plugin.ClientState.LocalPlayer != null)
                {
                    var playerPos = Plugin.ClientState.LocalPlayer.Position;
                    elements.Add(CreateLine(playerPos, position + new Vector3(0, 0, 5), 0xFFFF0000, "Path"));
                }
                break;
        }
        
        Plugin.Chat.Print($"[MarkerMind DEBUG] Created {elements.Count} elements");
        
        // Store for tracking
        foreach (var elem in elements)
        {
            activeElements[$"{mechanicId}_{elem.Name}"] = elem;
        }
        
        // Generate Scripting-compatible JSON
        var layout = new SplatoonLayout
        {
            Name = layoutName,
            Elements = elements
        };
        
        var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
        Plugin.Chat.Print($"[MarkerMind DEBUG] Generated JSON: {json.Substring(0, Math.Min(100, json.Length))}...");
        
        return json;
    }
    
    private SplatoonElement CreateCircle(Vector3 position, float radius, uint color, string name)
    {
        return new SplatoonElement
        {
            Type = 0, // Circle
            Name = name,
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Radius = radius,
            Color = color,
            Thicc = 2,
            Fill = true
        };
    }
    
    private SplatoonElement CreateLine(Vector3 start, Vector3 end, uint color, string name)
    {
        return new SplatoonElement
        {
            Type = 3, // Line
            Name = name,
            X = start.X,
            Y = start.Y,
            Z = start.Z,
            ToX = end.X,
            ToY = end.Y,
            ToZ = end.Z,
            Color = color,
            Thicc = 3
        };
    }
    
    private bool InjectIntoSplatoon(string layoutJson)
    {
        try
        {
            Plugin.Chat.Print($"[MarkerMind DEBUG] InjectIntoSplatoon called with JSON length: {layoutJson.Length}");
            
            // Write to Splatoon's Script folder
            var tempPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher", "pluginConfigs", "Splatoon", "Script", "markermind_dynamic.json"
            );
            
            Plugin.Chat.Print($"[MarkerMind DEBUG] Writing to: {tempPath}");
            
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(tempPath)!);
            System.IO.File.WriteAllText(tempPath, layoutJson);
            
            var exists = System.IO.File.Exists(tempPath);
            Plugin.Chat.Print($"[MarkerMind DEBUG] File exists after write: {exists}");
            
            // Try to trigger Splatoon to reload scripts
            try
            {
                var splatoonAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.FullName?.Contains("Splatoon") == true);
                
                if (splatoonAssembly != null)
                {
                    // Try to get Splatoon instance and call reload
                    var splatoonType = splatoonAssembly.GetType("Splatoon.Splatoon");
                    if (splatoonType != null)
                    {
                        var instanceProp = splatoonType.GetProperty("Instance");
                        var instance = instanceProp?.GetValue(null);
                        
                        if (instance != null)
                        {
                            // Try to find reload method
                            var reloadMethod = splatoonType.GetMethod("ReloadScript");
                            if (reloadMethod != null)
                            {
                                reloadMethod.Invoke(instance, new object[] { "markermind_dynamic" });
                                Plugin.Chat.Print("[MarkerMind DEBUG] Triggered Splatoon reload");
                            }
                            else
                            {
                                // Try ScriptManager
                                var scriptManagerType = splatoonAssembly.GetType("Splatoon.Scripting.ScriptManager");
                                if (scriptManagerType != null)
                                {
                                    var reloadAllMethod = scriptManagerType.GetMethod("ReloadAll");
                                    if (reloadAllMethod != null)
                                    {
                                        reloadAllMethod.Invoke(null, null);
                                        Plugin.Chat.Print("[MarkerMind DEBUG] Triggered ScriptManager reload");
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Chat.Print($"[MarkerMind DEBUG] Could not trigger reload: {ex.Message}");
            }
            
            // Success if file was written
            return exists;
        }
        catch (Exception ex)
        {
            Plugin.Chat.Print($"[MarkerMind DEBUG] Inject failed: {ex.Message}");
            return false;
        }
    }
    
    private void RemoveElementsForMechanic(string mechanicId)
    {
        var toRemove = new List<string>();
        foreach (var key in activeElements.Keys)
        {
            if (key.StartsWith(mechanicId))
                toRemove.Add(key);
        }
        foreach (var key in toRemove)
        {
            activeElements.Remove(key);
        }
        
        // Update the layout file
        UpdateLayoutFile();
    }
    
    private void UpdateLayoutFile()
    {
        try
        {
            var layout = new SplatoonLayout
            {
                Name = layoutName,
                Elements = new List<SplatoonElement>(activeElements.Values)
            };
            
            var json = JsonSerializer.Serialize(layout, new JsonSerializerOptions { WriteIndented = true });
            
            var tempPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher", "pluginConfigs", "Splatoon", "Script", "markermind_dynamic.json"
            );
            
            System.IO.File.WriteAllText(tempPath, json);
        }
        catch { }
    }
    
    private void RenderChatFallback(string mechanicId, Vector3 position, int disclosureLevel)
    {
        var levelText = disclosureLevel switch
        {
            1 => $"Danger at ({position.X:F1}, {position.Z:F1})",
            2 => $"Safe spot at ({position.X:F1}, {position.Z:F1})",
            3 => $"Move to ({position.X:F1}, {position.Z:F1})",
            4 => $"Position for {Plugin.Instance?.gameState?.Role}",
            _ => $"Mechanic at ({position.X:F1}, {position.Z:F1})"
        };
        
        Plugin.Chat.Print($"[MarkerMind] {levelText}");
    }
    
    public void ClearAll()
    {
        activeElements.Clear();
        UpdateLayoutFile();
    }
    
    public void Dispose()
    {
        ClearAll();
    }
}

// Scripting-compatible layout structure
public class SplatoonLayout
{
    public string Name { get; set; } = "";
    public List<SplatoonElement> Elements { get; set; } = new();
}

public class SplatoonElement
{
    public int Type { get; set; } // 0=Circle, 3=Line
    public string Name { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    public float? ToX { get; set; }
    public float? ToY { get; set; }
    public float? ToZ { get; set; }
    public float Radius { get; set; }
    public uint Color { get; set; }
    public int Thicc { get; set; }
    public bool Fill { get; set; }
}

public class ActiveElement
{
    public string MechanicId { get; set; } = string.Empty;
    public ElementType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 StartPosition { get; set; }
    public Vector3 EndPosition { get; set; }
}

public enum ElementType
{
    DangerZone,
    SafeSpot,
    Path
}
