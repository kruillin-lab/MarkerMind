using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Plugin.Services;

namespace MarkerMind;

/// <summary>
/// Draws world-space markers on screen via ImGui when Splatoon is unavailable or as a fallback.
/// </summary>
public class OverlayRenderer : IDisposable
{
    private readonly IGameGui _gameGui;
    private readonly Dictionary<string, List<OverlayElement>> _activeMechanics = new();

    public OverlayRenderer(IGameGui gameGui)
    {
        _gameGui = gameGui;
    }

    public void RenderMarker(string mechanicId, Vector3 position, int disclosureLevel, PlayerRole role)
    {
        if (Plugin.Instance?.Config.ShowGameOverlay == false)
            return;

        RemoveElementsForMechanic(mechanicId);

        var elements = new List<OverlayElement>();
        var opacity = Plugin.Instance?.Config.MarkerOpacity ?? 0.8f;

        switch (disclosureLevel)
        {
            case 1:
                elements.Add(CreateCircle(position, 3.0f, ApplyOpacity(0x664040FF, opacity), "Danger"));
                break;
            case 2:
                elements.Add(CreateCircle(position, 1.5f, ApplyOpacity(0x8030FF30, opacity), "Safe"));
                break;
            case 3:
            case 4:
                elements.Add(CreateCircle(position, 1.5f, ApplyOpacity(0x8030FF30, opacity), "Safe"));
                if (Plugin.Objects.LocalPlayer != null)
                {
                    var playerPos = Plugin.Objects.LocalPlayer.Position;
                    elements.Add(new OverlayElement
                    {
                        Type = OverlayElementType.Line,
                        StartPosition = playerPos,
                        EndPosition = position,
                        Color = ApplyOpacity(0x80FFFF30, opacity),
                        Name = "Path"
                    });
                }
                break;
        }

        if (elements.Count > 0)
            _activeMechanics[mechanicId] = elements;
    }

    private static OverlayElement CreateCircle(Vector3 position, float radius, uint color, string name) =>
        new()
        {
            Type = OverlayElementType.Circle,
            Position = position,
            Radius = radius,
            Color = color,
            Name = name
        };

    private static uint ApplyOpacity(uint argb, float opacity)
    {
        var alpha = (byte)Math.Clamp((int)(((argb >> 24) & 0xFF) * opacity), 0, 255);
        return (uint)(alpha << 24) | (argb & 0x00FFFFFF);
    }

    public void ClearAll() => _activeMechanics.Clear();

    public void RemoveElementsForMechanic(string mechanicId) => _activeMechanics.Remove(mechanicId);

    public void Draw()
    {
        if (_activeMechanics.Count == 0 || Plugin.Objects.LocalPlayer == null)
            return;

        var drawList = ImGui.GetBackgroundDrawList();

        foreach (var mechanic in _activeMechanics.Values)
        {
            foreach (var elem in mechanic)
            {
                switch (elem.Type)
                {
                    case OverlayElementType.Circle:
                        DrawCircle(drawList, elem);
                        break;
                    case OverlayElementType.Line:
                        DrawLine(drawList, elem);
                        break;
                }
            }
        }
    }

    private void DrawCircle(ImDrawListPtr drawList, OverlayElement elem)
    {
        if (!_gameGui.WorldToScreen(elem.Position, out var centerScreen))
            return;

        var edgeWorld = elem.Position + new Vector3(elem.Radius, 0, 0);
        if (!_gameGui.WorldToScreen(edgeWorld, out var edgeScreen))
            return;

        var screenRadius = Math.Max(8f, Vector2.Distance(centerScreen, edgeScreen));
        drawList.AddCircleFilled(centerScreen, screenRadius, elem.Color);
        drawList.AddCircle(centerScreen, screenRadius, 0xFFFFFFFF, 0, 2f);
    }

    private void DrawLine(ImDrawListPtr drawList, OverlayElement elem)
    {
        if (!_gameGui.WorldToScreen(elem.StartPosition, out var startScreen))
            return;
        if (!_gameGui.WorldToScreen(elem.EndPosition, out var endScreen))
            return;

        drawList.AddLine(startScreen, endScreen, elem.Color, 4f);
    }

    public void Dispose() => ClearAll();
}

public class OverlayElement
{
    public OverlayElementType Type { get; set; }
    public string Name { get; set; } = "";
    public Vector3 Position { get; set; }
    public Vector3 StartPosition { get; set; }
    public Vector3 EndPosition { get; set; }
    public float Radius { get; set; }
    public uint Color { get; set; }
}

public enum OverlayElementType
{
    Circle,
    Line
}
