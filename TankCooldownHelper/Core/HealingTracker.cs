using ECommons.DalamudServices;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class HealingTracker
{
    private readonly CombatEventBuffer _buffer;

    public HealingTracker(CombatEventBuffer buffer)
    {
        _buffer = buffer;
    }

    public float GetIncomingHps(uint targetId)
    {
        var totalHealing = _buffer.GetTotalHealing(targetId);
        var windowSeconds = GetEffectiveWindowSeconds();
        return totalHealing / windowSeconds;
    }

    public float GetPartyIncomingHps()
    {
        if (!Svc.ClientState.IsLoggedIn) return 0;
        
        var totalHps = 0f;
        var targets = _buffer.GetTrackedTargetIds();
        
        foreach (var targetId in targets)
        {
            totalHps += GetIncomingHps(targetId);
        }
        
        return totalHps;
    }

    private float GetEffectiveWindowSeconds()
    {
        return 5.0f;
    }

    public void SimulateHealing(uint targetId, float amount)
    {
        // For testing - add mock healing event
        _buffer.AddEvent(new CombatEvent(targetId, 0, 0, amount, CombatEventType.Healing));
    }
}
