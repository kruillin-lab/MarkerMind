namespace TankCooldownHelper.Data;

public enum DangerLevel
{
    Safe,      // Ratio < 1.0 - Healing covers damage
    Warning,   // Ratio 1.0-1.5 - Damage exceeds healing moderately
    Critical,  // Ratio 1.5-2.0 - Damage significantly exceeds healing
    Emergency  // Ratio > 2.0 - Imminent death without mitigation
}
