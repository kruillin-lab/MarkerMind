using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ECommons.SplatoonAPI;
using SplatoonElement = ECommons.SplatoonAPI.Element;
using SplatoonElementType = ECommons.SplatoonAPI.ElementType;

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
            isSplatoonAvailable = Splatoon.IsConnected();
            
            if (isSplatoonAvailable)
            {
                Plugin.Chat.Print("[MarkerMind] Splatoon detected! Markers enabled.");
            }
            else if (Plugin.Instance.Config.RequireSplatoon)
            {
                Plugin.Chat.Print("[MarkerMind] Splatoon not connected. Ground markers disabled.");
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
        CheckSplatoonAvailability();

        if (!isSplatoonAvailable)
        {
            RenderChatFallback(mechanicId, position, disclosureLevel);
            return;
        }
        
        RemoveElementsForMechanic(mechanicId);

        switch (disclosureLevel)
        {
            case 1:
                RenderDangerZone(mechanicId, position);
                break;
            case 2:
                RenderSafeSpot(mechanicId, position);
                break;
            case 3:
            case 4:
                RenderSafeSpot(mechanicId, position);
                RenderMovementPath(mechanicId, position);
                break;
        }
    }
    
    private void RenderDangerZone(string mechanicId, Vector3 position)
    {
        AddDynamicElement(new ActiveElement
        {
            MechanicId = mechanicId,
            Type = ElementType.DangerZone,
            Position = position
        });
    }
    
    private void RenderSafeSpot(string mechanicId, Vector3 position)
    {
        AddDynamicElement(new ActiveElement
        {
            MechanicId = mechanicId,
            Type = ElementType.SafeSpot,
            Position = position
        });
    }
    
    private void RenderMovementPath(string mechanicId, Vector3 position)
    {
        if (Plugin.Instance?.gameState?.LocalPlayer != null)
        {
            var playerPos = Plugin.Instance.gameState.Position;
            AddDynamicElement(new ActiveElement
            {
                MechanicId = mechanicId,
                Type = ElementType.Path,
                StartPosition = playerPos,
                EndPosition = position
            });
        }
    }

    private void AddDynamicElement(ActiveElement element)
    {
        activeElements.Add(element);

        if (!TryCreateSplatoonElement(element, out var splatoonElement))
        {
            Plugin.Chat.Print($"[MarkerMind] Failed to create {element.Type} marker for {element.MechanicId}.");
            return;
        }

        var name = ElementNamespace(element.MechanicId);
        var added = Splatoon.AddDynamicElement(name, splatoonElement, -1L);
        if (!added)
        {
            Plugin.Chat.Print($"[MarkerMind] Splatoon rejected {element.Type} marker for {element.MechanicId}.");
        }
    }

    private bool TryCreateSplatoonElement(ActiveElement element, out SplatoonElement splatoonElement)
    {
        splatoonElement = element.Type == ElementType.Path
            ? new SplatoonElement(SplatoonElementType.LineBetweenTwoFixedCoordinates)
            : new SplatoonElement(SplatoonElementType.CircleAtFixedCoordinates);

        splatoonElement.Enabled = true;

        switch (element.Type)
        {
            case ElementType.DangerZone:
                splatoonElement.SetRefCoord(element.Position);
                splatoonElement.radius = 3.0f;
                splatoonElement.thicc = 4.0f;
                splatoonElement.color = 0x664040FF;
                return true;
            case ElementType.SafeSpot:
                splatoonElement.SetRefCoord(element.Position);
                splatoonElement.radius = 1.5f;
                splatoonElement.thicc = 5.0f;
                splatoonElement.color = 0x8030FF30;
                splatoonElement.overlayText = "Safe";
                splatoonElement.overlayTextColor = 0xFFFFFFFF;
                splatoonElement.overlayBGColor = 0x80000000;
                return true;
            case ElementType.Path:
                splatoonElement.SetRefCoord(element.StartPosition);
                splatoonElement.SetOffCoord(element.EndPosition);
                splatoonElement.thicc = 5.0f;
                splatoonElement.color = 0x80FFFF30;
                return true;
            default:
                return false;
        }
    }
    
    private void RenderChatFallback(string mechanicId, Vector3 position, int disclosureLevel)
    {
        var levelText = disclosureLevel switch
        {
            1 => "Danger zone",
            2 => "Safe spot",
            3 => "Move here",
            4 => $"Role spot ({Plugin.Instance?.gameState?.Role})",
            _ => "Mechanic"
        };

        Plugin.Chat.Print($"[MarkerMind] {levelText} at ({position.X:F1}, {position.Y:F1}, {position.Z:F1}) — check ground overlay (green = safe).");
    }
    
    private void RemoveElementsForMechanic(string mechanicId)
    {
        activeElements.RemoveAll(e => e.MechanicId == mechanicId);
        if (Splatoon.IsConnected())
        {
            Splatoon.RemoveDynamicElements(ElementNamespace(mechanicId));
        }
    }

    private static string ElementNamespace(string mechanicId) => $"MarkerMind:{mechanicId}";
    
    public void ClearAll()
    {
        foreach (var mechanicId in activeElements.Select(e => e.MechanicId).Distinct().ToList())
        {
            if (Splatoon.IsConnected())
            {
                Splatoon.RemoveDynamicElements(ElementNamespace(mechanicId));
            }
        }
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
