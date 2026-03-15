namespace TankCooldownHelper.Data;

public class PartyMemberState
{
    public uint ObjectId { get; set; }
    public string Name { get; set; } = "";
    public uint CurrentHp { get; set; }
    public uint MaxHp { get; set; }
    public float HpPercent => MaxHp > 0 ? (float)CurrentHp / MaxHp * 100 : 0;
    
    // Calculated metrics
    public float IncomingDps { get; set; }
    public float IncomingHps { get; set; }
    public float DangerRatio { get; set; }
    public DangerLevel DangerLevel { get; set; }
    public float? SecondsUntilDeath { get; set; }
    
    // Job info for icon display
    public uint JobId { get; set; }
    public bool IsTank { get; set; }
    public bool IsHealer { get; set; }
}
