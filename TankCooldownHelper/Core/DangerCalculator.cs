using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class DangerCalculator
{
    private readonly Configuration _config;

    public DangerCalculator(Configuration config)
    {
        _config = config;
    }

    public DangerLevel CalculateDangerLevel(float dps, float hps)
    {
        var ratio = CalculateDangerRatio(dps, hps);
        
        if (ratio >= _config.EmergencyThreshold)
            return DangerLevel.Emergency;
        if (ratio >= _config.CriticalThreshold)
            return DangerLevel.Critical;
        if (ratio >= _config.WarningThreshold)
            return DangerLevel.Warning;
        
        return DangerLevel.Safe;
    }

    public DangerLevel CalculateDangerLevel(float dps, float hps, uint maxHp)
    {
        // Calculate danger from both ratio and HP percentage
        var ratioDanger = CalculateDangerLevel(dps, hps);
        var hpPercentDanger = CalculateHpPercentDanger(dps, maxHp);
        
        // Return the higher of the two danger levels
        return (DangerLevel)Math.Max((int)ratioDanger, (int)hpPercentDanger);
    }

    private DangerLevel CalculateHpPercentDanger(float dps, uint maxHp)
    {
        if (maxHp <= 0) return DangerLevel.Safe;
        
        // Calculate DPS as percentage of max HP per second
        var hpPercentPerSecond = (dps / maxHp) * 100f;
        
        if (hpPercentPerSecond >= _config.HpEmergencyThreshold)
            return DangerLevel.Emergency;
        if (hpPercentPerSecond >= _config.HpCriticalThreshold)
            return DangerLevel.Critical;
        if (hpPercentPerSecond >= _config.HpWarningThreshold)
            return DangerLevel.Warning;
        
        return DangerLevel.Safe;
    }

    public float CalculateDangerRatio(float dps, float hps)
    {
        // Avoid division by zero - if no healing, ratio is effectively infinite
        if (hps <= 0)
            return dps > 0 ? 99.9f : 0f;
        
        return dps / hps;
    }

    public float? CalculateDeathTimer(float currentHp, float dps, float hps)
    {
        var netDps = dps - hps;
        
        // If healing exceeds damage, no death timer
        if (netDps <= 0)
            return null;
        
        // Avoid division by very small numbers
        if (netDps < 1)
            return null;
        
        var seconds = currentHp / netDps;
        
        // Cap at reasonable max to avoid overflow display
        return seconds > 999 ? 999 : seconds;
    }

    public uint GetColorForDangerLevel(DangerLevel level)
    {
        return level switch
        {
            DangerLevel.Safe => 0xFF4CAF50,      // Green
            DangerLevel.Warning => 0xFFFFC107,   // Yellow/Amber
            DangerLevel.Critical => 0xFFFF9800,  // Orange
            DangerLevel.Emergency => 0xFFF44336, // Red
            _ => 0xFF888888
        };
    }

    public string GetDangerLevelText(DangerLevel level)
    {
        return level switch
        {
            DangerLevel.Safe => "Safe",
            DangerLevel.Warning => "Warning",
            DangerLevel.Critical => "Critical",
            DangerLevel.Emergency => "EMERGENCY",
            _ => "Unknown"
        };
    }
}
