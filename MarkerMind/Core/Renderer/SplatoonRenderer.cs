using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;

namespace MarkerMind;

/// <summary>
/// Renders visual markers via Splatoon plugin IPC.
/// Falls back to chat messages if Splatoon is not available.
/// </summary>
public class SplatoonRenderer : IDisposable
{
    private bool isSplatoonAvailable = false;
    private object? splatoonInstance = null;
    private MethodInfo? addElementMethod = null;
    private MethodInfo? removeElementMethod = null;
    private List<string> activeElementIds = new();
    private int elementCounter = 0;
    
    public SplatoonRenderer()
    {
        CheckSplatoonAvailability();
    }
    
    private void CheckSplatoonAvailability()
    {
        try
        {
            // Try to find Splatoon plugin via reflection
            var splatoonType = Plugin.PluginInterface.GetType().Assembly
                .GetType("Splatoon.Splatoon");
            
            if (splatoonType != null)
            {
                // Try to get Splatoon instance
                var instanceField = splatoonType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
                if (instanceField != null)
                {
                    splatoonInstance = instanceField.GetValue(null);
                    
                    // Look for methods to add/remove elements
                    addElementMethod = splatoonType.GetMethod("AddDynamicElement");
                    removeElementMethod = splatoonType.GetMethod("RemoveDynamicElement");
                    
                    if (addElementMethod != null)
                    {
                        isSplatoonAvailable = true;
                        Plugin.Chat.Print("[MarkerMind] Splatoon detected! Visual markers enabled.");
                        return;
                    }
                }
            }
            
            isSplatoonAvailable = false;
            Plugin.Chat.Print("[MarkerMind] Splatoon not detected. Using chat fallback.");
        }
        catch (Exception ex)
        {
            isSplatoonAvailable = false;
            Plugin.Chat.Print($"[MarkerMind] Splatoon check failed: {ex.Message}");
        }
    }
    
    public void RenderMarker(string mechanicId, Vector3 position, int disclosureLevel, PlayerRole role)
    {
        // Clear previous elements for this mechanic
        RemoveElementsForMechanic(mechanicId);
        
        if (!isSplatoonAvailable || addElementMethod == null)
        {
            RenderChatFallback(mechanicId, position, disclosureLevel);
            return;
        }
        
        try
        {
            // Render based on disclosure level
            switch (disclosureLevel)
            {
                case 1:
                    RenderDangerZone(mechanicId, position);
                    break;
                case 2:
                    RenderDangerZone(mechanicId, position);
                    RenderSafeSpot(mechanicId, position);
                    break;
                case 3:
                case 4:
                    RenderDangerZone(mechanicId, position);
                    RenderSafeSpot(mechanicId, position);
                    RenderMovementPath(mechanicId, position);
                    break;
            }
        }
        catch (Exception ex)
        {
            Plugin.Chat.Print($"[MarkerMind] Failed to render marker: {ex.Message}");
        }
    }
    
    private void RenderDangerZone(string mechanicId, Vector3 position)
    {
        // Danger zone: Red circle (ARGB format)
        var elementId = $"{mechanicId}_danger_{elementCounter++}";
        activeElementIds.Add(elementId);
        
        // Create element data for Splatoon
        var elementData = CreateCircleElement(position, 5.0f, 0xFF0000FF); // Red
        
        try
        {
            addElementMethod?.Invoke(splatoonInstance, new object[] { elementId, elementData });
        }
        catch
        {
            // Fallback: just track it
        }
    }
    
    private void RenderSafeSpot(string mechanicId, Vector3 position)
    {
        // Safe spot: Green circle, offset slightly
        var safePosition = position + new Vector3(0, 0, 5); // 5 yalms north
        var elementId = $"{mechanicId}_safe_{elementCounter++}";
        activeElementIds.Add(elementId);
        
        var elementData = CreateCircleElement(safePosition, 2.0f, 0xFF00FF00); // Green
        
        try
        {
            addElementMethod?.Invoke(splatoonInstance, new object[] { elementId, elementData });
        }
        catch { }
    }
    
    private void RenderMovementPath(string mechanicId, Vector3 position)
    {
        // Movement path: Blue line from player to safe spot
        if (Plugin.ClientState.LocalPlayer == null) return;
        
        var playerPos = Plugin.ClientState.LocalPlayer.Position;
        var safePosition = position + new Vector3(0, 0, 5);
        
        var elementId = $"{mechanicId}_path_{elementCounter++}";
        activeElementIds.Add(elementId);
        
        var elementData = CreateLineElement(playerPos, safePosition, 0xFFFF0000); // Blue
        
        try
        {
            addElementMethod?.Invoke(splatoonInstance, new object[] { elementId, elementData });
        }
        catch { }
    }
    
    private object CreateCircleElement(Vector3 position, float radius, uint color)
    {
        // Create a Splatoon-compatible element object
        // This is a simplified version - actual Splatoon element structure may vary
        return new
        {
            type = 0, // Circle
            pos = position,
            radius = radius,
            color = color,
            thicc = 2,
            fill = true
        };
    }
    
    private object CreateLineElement(Vector3 start, Vector3 end, uint color)
    {
        return new
        {
            type = 1, // Line
            start = start,
            end = end,
            color = color,
            thicc = 3
        };
    }
    
    private void RemoveElementsForMechanic(string mechanicId)
    {
        var toRemove = activeElementIds.FindAll(id => id.StartsWith(mechanicId));
        foreach (var elementId in toRemove)
        {
            try
            {
                removeElementMethod?.Invoke(splatoonInstance, new object[] { elementId });
            }
            catch { }
            activeElementIds.Remove(elementId);
        }
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
        if (removeElementMethod != null)
        {
            foreach (var elementId in activeElementIds)
            {
                try
                {
                    removeElementMethod.Invoke(splatoonInstance, new object[] { elementId });
                }
                catch { }
            }
        }
        activeElementIds.Clear();
    }
    
    public void Dispose()
    {
        ClearAll();
    }
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
