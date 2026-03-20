using System;
using System.Collections.Generic;
using System.Numerics;

namespace MarkerMind;

public class BossmodBridge : IDisposable
{
    public bool IsBossmodAvailable { get; private set; } = false;
    public event Action<MechanicEvent>? OnMechanicStart;
    public event Action<MechanicEvent, string>? OnMechanicResolve;
    
    private Dictionary<uint, MechanicEvent> activeMechanics = new();
    
    public BossmodBridge()
    {
        // Try to subscribe to Bossmod IPC
        TrySubscribeBossmod();
        
        // Fallback: Listen to combat events
        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
    }
    
    private void TrySubscribeBossmod()
    {
        try
        {
            // Bossmod IPC subscription via reflection
            var bossmod = Plugin.PluginInterface.GetType().Assembly
                .GetType("BossMod.BossMod");
            
            if (bossmod != null)
            {
                IsBossmodAvailable = true;
                Plugin.Chat.Print("[MarkerMind] Bossmod detected! Learning enabled.");
            }
        }
        catch
        {
            IsBossmodAvailable = false;
        }
    }
    
    public void Update()
    {
        if (!IsBossmodAvailable) return;
        
        // Poll Bossmod state for active mechanics
        // Placeholder: In real implementation, this would use IPC events
    }
    
    private void OnTerritoryChanged(ushort territoryId)
    {
        activeMechanics.Clear();
        TrySubscribeBossmod();
    }
    
    public void Dispose()
    {
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        activeMechanics.Clear();
    }
}

public class MechanicEvent
{
    public string MechanicId { get; set; } = string.Empty;
    public string MechanicName { get; set; } = string.Empty;
    public uint BossId { get; set; }
    public Vector3? BossPosition { get; set; }
    public float Duration { get; set; }
    public float RemainingTime { get; set; }
    public MechanicType Type { get; set; }
    public List<Vector3> SafeZones { get; set; } = new();
    public List<Vector3> DangerZones { get; set; } = new();
}

public enum MechanicType
{
    Stack,
    Spread,
    Tankbuster,
    AOE,
    Dodge,
    Movement,
    Other
}
