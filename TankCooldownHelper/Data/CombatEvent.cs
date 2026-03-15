namespace TankCooldownHelper.Data;

public readonly record struct CombatEvent
{
    public readonly uint TargetId;
    public readonly uint SourceId;
    public readonly uint ActionId;
    public readonly float Amount;
    public readonly CombatEventType Type;
    public readonly DateTime Timestamp;

    public CombatEvent(uint targetId, uint sourceId, uint actionId, float amount, CombatEventType type)
    {
        TargetId = targetId;
        SourceId = sourceId;
        ActionId = actionId;
        Amount = amount;
        Type = type;
        Timestamp = DateTime.UtcNow;
    }
}

public enum CombatEventType
{
    Damage,
    Healing,
    ShieldApplied,
    ShieldConsumed
}
