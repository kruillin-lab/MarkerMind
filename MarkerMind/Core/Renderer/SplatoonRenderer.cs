using System;
using System.Collections.Generic;
using System.Numerics;

namespace MarkerMind;

public class SplatoonRenderer : IDisposable
{
    private bool isSplatoonAvailable = false;
    private List<ActiveElement> activeElements = new();
    
    public SplatoonRenderer()
    {
        CheckSplatoonAvailability();
    }
    
    private void CheckSplatoonAvailability()
    {
        try
        {
            // Check if Splatoon IPC is available
            // This uses reflection
            isSplatoonAvailable = Plugin.PluginInterface.GetType().Assembly
                .GetType("Splatoon.Splatoon") != null;
            
            if (isSplatoonAvailable)
            {
                Plugin.Chat.Print("[MarkerMind] Splatoon detected! Markers enabled.");
            }
        }
        catch
        {
            isSplatoonAvailable = false;
        }
    }
    
    public void RenderMarker(string mechanicId, Vector3 position, int disclosureLevel, PlayerRole role)
    {
        if (!isSplatoonAvailable)
        {
            RenderChatFallback(mechanicId, position, disclosureLevel);
            return;
        }
        
        // Clear previous elements for this mechanic
        RemoveElementsForMechanic(mechanicId);
        
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
    
    private void RenderDangerZone(string mechanicId, Vector3 position)
    {
        // Create danger zone circle (red)
        // This would use Splatoon IPC to inject elements
        // Placeholder implementation
        activeElements.Add(new ActiveElement
        {
            MechanicId = mechanicId,
            Type = ElementType.DangerZone,
            Position = position
        });
    }
    
    private void RenderSafeSpot(string mechanicId, Vector3 position)
    {
        // Create safe spot marker (green)
        activeElements.Add(new ActiveElement
        {
            MechanicId = mechanicId,
            Type = ElementType.SafeSpot,
            Position = position
        });
    }
    
    private void RenderMovementPath(string mechanicId, Vector3 position)
    {
        // Create movement arrow/path
        if (Plugin.Instance?.gameState?.LocalPlayer != null)
        {
            var playerPos = Plugin.Instance.gameState.Position;
            activeElements.Add(new ActiveElement
            {
                MechanicId = mechanicId,
                Type = ElementType.Path,
                StartPosition = playerPos,
                EndPosition = position
            });
        }
    }
    
    private void RenderChatFallback(string mechanicId, Vector3 position, int disclosureLevel)
    {
        // Fallback: Print to chat
        var levelText = disclosureLevel switch
        {
            1 => "Danger detected",
            2 => "Safe spot suggested",
            3 => "Follow the path",
            4 => "Position for your role",
            _ => "Mechanic detected"
        };
        
        Plugin.Chat.Print($"[MarkerMind] {levelText}: {mechanicId}");
    }
    
    private void RemoveElementsForMechanic(string mechanicId)
    {
        activeElements.RemoveAll(e => e.MechanicId == mechanicId);
    }
    
    public void ClearAll()
    {
        activeElements.Clear();
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
