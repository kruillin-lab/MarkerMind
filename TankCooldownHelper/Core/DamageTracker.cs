using ECommons.DalamudServices;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class DamageTracker
{
    private readonly CombatEventBuffer _buffer;

    public DamageTracker(CombatEventBuffer buffer)
    {
        _buffer = buffer;
    }

    public float GetIncomingDps(uint targetId)
    {
        var totalDamage = _buffer.GetTotalDamage(targetId);
        // Use actual time span of events, or configured window
        var windowSeconds = GetEffectiveWindowSeconds();
        return totalDamage / windowSeconds;
    }

    public float GetPartyIncomingDps()
    {
        if (!Svc.ClientState.IsLoggedIn) return 0;
        
        var totalDps = 0f;
        var targets = _buffer.GetTrackedTargetIds();
        
        foreach (var targetId in targets)
        {
            totalDps += GetIncomingDps(targetId);
        }
        
        return totalDps;
    }

    private float GetEffectiveWindowSeconds()
    {
        // For MVP, use a fixed calculation based on buffer size
        // In future, track actual time span of oldest event
        return 5.0f;
    }

    public void SimulateDamage(uint targetId, float amount)
    {
        // For testing - add mock damage event
        _buffer.AddEvent(new CombatEvent(targetId, 0, 0, amount, CombatEventType.Damage));
    }
}
