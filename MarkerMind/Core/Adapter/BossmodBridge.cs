using System;
using System.Collections.Generic;
using System.Numerics;

namespace MarkerMind;

/// <summary>
/// Bridge to Bossmod Reborn for mechanic detection.
/// Uses Dalamud IPC to subscribe to Bossmod events.
/// </summary>
public class BossmodBridge : IDisposable
{
    public bool IsBossmodAvailable { get; private set; } = false;
    public event Action<MechanicEvent>? OnMechanicStart;
    public event Action<MechanicEvent, string>? OnMechanicResolve;
    
    private Dictionary<uint, MechanicEvent> activeMechanics = new();
    
    // IPC delegates stored for proper unsubscribe
    private Action? _actionStartHandler;
    private Action? _actionEndHandler;
    
    public BossmodBridge()
    {
        TrySubscribeBossmod();
        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
    }
    
    private void TrySubscribeBossmod()
    {
        try
        {
            // Try to get Bossmod IPC providers
            // Bossmod uses ICallGateProvider for broadcasting events
            var startProvider = Plugin.PluginInterface.GetIpcProvider<object, object>("BossMod.MechanicStart");
            var endProvider = Plugin.PluginInterface.GetIpcProvider<object, object>("BossMod.MechanicEnd");
            
            if (startProvider != null || endProvider != null)
            {
                IsBossmodAvailable = true;
                Plugin.Chat.Print("[MarkerMind] Bossmod detected! Learning enabled.");
            }
            else
            {
                IsBossmodAvailable = false;
                Plugin.Chat.Print("[MarkerMind] Bossmod not detected. Running without mechanic detection.");
            }
        }
        catch (Exception ex)
        {
            IsBossmodAvailable = false;
            Plugin.Chat.Print($"[MarkerMind] Bossmod IPC not available: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Called by external systems when a mechanic starts.
    /// This simulates Bossmod IPC events.
    /// </summary>
    public void TriggerMechanicStart(string mechanicName, uint bossId, List<Vector3>? safeZones = null)
    {
        var mechanic = new MechanicEvent
        {
            MechanicId = $"{bossId}-{mechanicName}",
            MechanicName = mechanicName,
            BossId = bossId,
            SafeZones = safeZones ?? new List<Vector3>(),
            Type = GuessMechanicType(mechanicName)
        };
        
        activeMechanics[bossId] = mechanic;
        OnMechanicStart?.Invoke(mechanic);
        
        Plugin.Chat.Print($"[MarkerMind] Mechanic started: {mechanicName}");
    }
    
    /// <summary>
    /// Called by external systems when a mechanic ends.
    /// </summary>
    public void TriggerMechanicEnd(uint bossId)
    {
        if (activeMechanics.TryGetValue(bossId, out var mechanic))
        {
            activeMechanics.Remove(bossId);
            
            var outcome = DetermineOutcome();
            OnMechanicResolve?.Invoke(mechanic, outcome);
            
            Plugin.Chat.Print($"[MarkerMind] Mechanic ended: {mechanic.MechanicName} - {outcome}");
        }
    }
    
    /// <summary>
    /// For testing: Trigger a fake mechanic to test the system
    /// </summary>
    public void TestMechanic()
    {
        if (!IsBossmodAvailable)
        {
            Plugin.Chat.Print("[MarkerMind] Running test mechanic...");
        }
        
        var testSafeZones = new List<Vector3>
        {
            new Vector3(100, 0, 100),
            new Vector3(105, 0, 105)
        };
        
        TriggerMechanicStart("Test Mechanic", 12345, testSafeZones);
        
        // Auto-end after 5 seconds for testing
        System.Threading.Tasks.Task.Run(async () =>
        {
            await System.Threading.Tasks.Task.Delay(5000);
            TriggerMechanicEnd(12345);
        });
    }
    
    private MechanicType GuessMechanicType(string name)
    {
        var lower = name.ToLowerInvariant();
        if (lower.Contains("stack")) return MechanicType.Stack;
        if (lower.Contains("spread")) return MechanicType.Spread;
        if (lower.Contains("tankbuster")) return MechanicType.Tankbuster;
        if (lower.Contains("aoe")) return MechanicType.AOE;
        if (lower.Contains("dodge")) return MechanicType.Dodge;
        return MechanicType.Other;
    }
    
    private string DetermineOutcome()
    {
        var player = Plugin.ClientState.LocalPlayer;
        if (player == null) return "unknown";
        return player.CurrentHp > 0 ? "survived" : "died";
    }
    
    public void Update()
    {
        // IPC is event-driven, no polling needed
    }
    
    private void OnTerritoryChanged(ushort territoryId)
    {
        activeMechanics.Clear();
        
        if (!IsBossmodAvailable)
        {
            TrySubscribeBossmod();
        }
    }
    
    public void Dispose()
    {
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        activeMechanics.Clear();
    }
}

/// <summary>
/// Represents a mechanic event from Bossmod
/// </summary>
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
