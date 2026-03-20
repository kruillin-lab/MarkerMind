using System;
using System.Collections.Generic;
using System.Numerics;

namespace MarkerMind;

/// <summary>
/// Renders visual markers via ECommons SplatoonAPI.
/// Falls back to chat messages if Splatoon is not available.
/// </summary>
public class SplatoonRenderer : IDisposable
{
    private bool isSplatoonAvailable = false;
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
            // Try to use ECommons SplatoonAPI
            // ECommons provides a wrapper around Splatoon
            var splatoonType = Type.GetType("ECommons.SplatoonAPI.Splatoon, ECommons");
            
            if (splatoonType != null)
            {
                isSplatoonAvailable = true;
                Plugin.Chat.Print("[MarkerMind] SplatoonAPI via ECommons detected!");
            }
            else
            {
                isSplatoonAvailable = false;
                Plugin.Chat.Print("[MarkerMind] SplatoonAPI not available. Using chat fallback.");
            }
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
        
        if (!isSplatoonAvailable)
        {
            RenderChatFallback(mechanicId, position, disclosureLevel);
            return;
        }
        
        try
        {
            // Use ECommons SplatoonAPI to draw elements
            // For now, use reflection to call the API
            var splatoonType = Type.GetType("ECommons.SplatoonAPI.Splatoon, ECommons");
            if (splatoonType == null)
            {
                RenderChatFallback(mechanicId, position, disclosureLevel);
                return;
            }
            
            // Render based on disclosure level
            switch (disclosureLevel)
            {
                case 1:
                    RenderDangerZoneViaECommons(splatoonType, mechanicId, position);
                    break;
                case 2:
                    RenderDangerZoneViaECommons(splatoonType, mechanicId, position);
                    RenderSafeSpotViaECommons(splatoonType, mechanicId, position);
                    break;
                case 3:
                case 4:
                    RenderDangerZoneViaECommons(splatoonType, mechanicId, position);
                    RenderSafeSpotViaECommons(splatoonType, mechanicId, position);
                    RenderMovementPathViaECommons(splatoonType, mechanicId, position);
                    break;
            }
        }
        catch (Exception ex)
        {
            Plugin.Chat.Print($"[MarkerMind] Failed to render: {ex.Message}");
            RenderChatFallback(mechanicId, position, disclosureLevel);
        }
    }
    
    private void RenderDangerZoneViaECommons(Type splatoonType, string mechanicId, Vector3 position)
    {
        try
        {
            // Call Splatoon.AddDynamicElement or similar through ECommons
            var elementId = $"markermind_{mechanicId}_danger_{elementCounter++}";
            activeElementIds.Add(elementId);
            
            // Try to invoke DrawCircle or similar method
            var drawMethod = splatoonType.GetMethod("DrawCircle");
            if (drawMethod != null)
            {
                // Parameters: position, radius, color
                drawMethod.Invoke(null, new object[] { position, 5.0f, 0xFF0000FF });
                Plugin.Chat.Print($"[MarkerMind] Drew danger circle at {position.X:F1}, {position.Z:F1}");
            }
            else
            {
                // Try AddDynamicElement
                var addMethod = splatoonType.GetMethod("AddDynamicElement");
                if (addMethod != null)
                {
                    var element = CreateCircleData(position, 5.0f, 0xFF0000FF);
                    addMethod.Invoke(null, new object[] { elementId, element });
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Chat.Print($"[MarkerMind] Error drawing danger: {ex.Message}");
        }
    }
    
    private void RenderSafeSpotViaECommons(Type splatoonType, string mechanicId, Vector3 position)
    {
        try
        {
            var safePos = position + new Vector3(0, 0, 5);
            var elementId = $"markermind_{mechanicId}_safe_{elementCounter++}";
            activeElementIds.Add(elementId);
            
            var drawMethod = splatoonType.GetMethod("DrawCircle");
            if (drawMethod != null)
            {
                drawMethod.Invoke(null, new object[] { safePos, 2.0f, 0xFF00FF00 });
                Plugin.Chat.Print($"[MarkerMind] Drew safe circle at {safePos.X:F1}, {safePos.Z:F1}");
            }
        }
        catch { }
    }
    
    private void RenderMovementPathViaECommons(Type splatoonType, string mechanicId, Vector3 position)
    {
        try
        {
            if (Plugin.ClientState.LocalPlayer == null) return;
            
            var playerPos = Plugin.ClientState.LocalPlayer.Position;
            var safePos = position + new Vector3(0, 0, 5);
            var elementId = $"markermind_{mechanicId}_path_{elementCounter++}";
            activeElementIds.Add(elementId);
            
            var drawMethod = splatoonType.GetMethod("DrawLine");
            if (drawMethod != null)
            {
                drawMethod.Invoke(null, new object[] { playerPos, safePos, 0xFFFF0000 });
            }
        }
        catch { }
    }
    
    private object CreateCircleData(Vector3 position, float radius, uint color)
    {
        // Create element data for Splatoon
        return new
        {
            Type = 0, // Circle
            Pos = position,
            Radius = radius,
            Color = color,
            Thicc = 2,
            Fill = true
        };
    }
    
    private void RemoveElementsForMechanic(string mechanicId)
    {
        // ECommons Splatoon elements auto-expire, but we track them
        activeElementIds.RemoveAll(id => id.Contains(mechanicId));
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
        // ECommons elements auto-expire
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
