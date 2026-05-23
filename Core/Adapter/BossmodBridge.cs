using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.Types;

namespace MarkerMind;

public class BossmodBridge : IDisposable
{
    public bool IsBossmodAvailable { get; private set; } = false;
    public event Action<MechanicEvent>? OnMechanicStart;
    public event Action<MechanicEvent, string>? OnMechanicResolve;
    
    private Dictionary<ulong, MechanicEvent> activeMechanics = new();
    private Dictionary<ulong, uint> activeCasts = new();
    
    public BossmodBridge()
    {
        TrySubscribeBossmod();
        
        Plugin.ClientState.TerritoryChanged += OnTerritoryChanged;
    }
    
    private void TrySubscribeBossmod()
    {
        try
        {
            Plugin.PluginInterface
                .GetIpcSubscriber<uint, bool>("BossMod.HasModuleByDataId")
                .InvokeFunc(0);
            IsBossmodAvailable = true;
        }
        catch
        {
            IsBossmodAvailable = false;
        }
    }
    
    public void Update()
    {
        var stillCasting = new HashSet<ulong>();

        foreach (var obj in Plugin.Objects)
        {
            if (obj is not IBattleChara actor)
                continue;
            if (actor.ObjectKind != ObjectKind.BattleNpc || actor.IsDead)
                continue;
            if (!actor.IsCasting || actor.CastActionId == 0 || actor.TotalCastTime <= 0.5f)
                continue;

            var actorId = actor.GameObjectId;
            stillCasting.Add(actorId);

            if (activeCasts.TryGetValue(actorId, out var activeCastId) && activeCastId == actor.CastActionId)
                continue;

            var mechanic = CreateMechanicEvent(actor);
            activeCasts[actorId] = actor.CastActionId;
            activeMechanics[actorId] = mechanic;
            OnMechanicStart?.Invoke(mechanic);
        }

        foreach (var actorId in activeCasts.Keys.ToArray())
        {
            if (stillCasting.Contains(actorId))
                continue;

            if (activeMechanics.TryGetValue(actorId, out var mechanic))
            {
                OnMechanicResolve?.Invoke(mechanic, ResolveOutcome());
            }

            activeCasts.Remove(actorId);
            activeMechanics.Remove(actorId);
        }
    }

    private MechanicEvent CreateMechanicEvent(IBattleChara actor)
    {
        var mechanicName = $"{actor.Name} cast {actor.CastActionId}";
        return new MechanicEvent
        {
            MechanicId = $"{Plugin.ClientState.TerritoryType}-{actor.BaseId}-{actor.CastActionId}",
            MechanicName = mechanicName,
            BossId = actor.BaseId,
            BossPosition = actor.Position,
            Duration = actor.TotalCastTime,
            RemainingTime = Math.Max(0f, actor.TotalCastTime - actor.CurrentCastTime),
            Type = MechanicType.Other
        };
    }

    private string ResolveOutcome()
    {
        return Plugin.Objects.LocalPlayer is { IsDead: false } ? "survived" : "died";
    }
    
    private void OnTerritoryChanged(uint territoryId)
    {
        activeMechanics.Clear();
        activeCasts.Clear();
        TrySubscribeBossmod();
    }

    public void TriggerMockMechanicStart(MechanicEvent mechanic)
    {
        OnMechanicStart?.Invoke(mechanic);
    }

    public void TriggerMockMechanicResolve(MechanicEvent mechanic, string outcome)
    {
        OnMechanicResolve?.Invoke(mechanic, outcome);
    }
    
    public void Dispose()
    {
        Plugin.ClientState.TerritoryChanged -= OnTerritoryChanged;
        activeMechanics.Clear();
        activeCasts.Clear();
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
