using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TankCooldownHelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // Time Window Settings
    public float TimeWindowSeconds { get; set; } = 5.0f;
    public float MinTimeWindow { get; set; } = 1.0f;
    public float MaxTimeWindow { get; set; } = 15.0f;
    
    // Thresholds (DPS/HPS ratio)
    public float WarningThreshold { get; set; } = 1.0f;
    public float CriticalThreshold { get; set; } = 1.5f;
    public float EmergencyThreshold { get; set; } = 2.0f;
    
    // HP-Based Thresholds (% of max HP per second)
    public float HpWarningThreshold { get; set; } = 5.0f;   // 5% HP/sec
    public float HpCriticalThreshold { get; set; } = 10.0f; // 10% HP/sec  
    public float HpEmergencyThreshold { get; set; } = 20.0f; // 20% HP/sec
    
    // Display Settings
    public bool ShowDangerMeter { get; set; } = true;
    public bool ShowPredictiveTimeline { get; set; } = true;
    public bool ShowCooldownSuggestions { get; set; } = true;
    public bool ShowPartyBreakdown { get; set; } = false;
    public bool ShowNetDamageCounter { get; set; } = true;
    
    // Party Tracking
    public bool TrackFullParty { get; set; } = true;
    public bool HighlightMostEndangered { get; set; } = true;
    
    // Window Settings
    public bool LockWindowPosition { get; set; } = false;
    public float WindowOpacity { get; set; } = 0.95f;
    public bool ShowInCombatOnly { get; set; } = false;
    
    // ParseLord3 Integration (future)
    public bool EnablePL3Integration { get; set; } = false;
    
    public void Save(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.SavePluginConfig(this);
    }
}
