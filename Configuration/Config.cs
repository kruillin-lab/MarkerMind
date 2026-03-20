using Dalamud.Configuration;
using System;

namespace MarkerMind;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // Learning settings
    public bool EnableLearning { get; set; } = true;
    public int MinSampleSize { get; set; } = 5;
    public float PositionTolerance { get; set; } = 2.0f;
    public float ClusterDistance { get; set; } = 2.0f;
    public float EmaAlpha { get; set; } = 0.3f;
    public int DataDecayDays { get; set; } = 90;
    public int TelemetryRetentionDays { get; set; } = 30;
    
    // Disclosure thresholds
    public float Level2Threshold { get; set; } = 0.5f;
    public float Level3Threshold { get; set; } = 0.7f;
    public float Level4Threshold { get; set; } = 0.85f;
    
    // Rendering settings
    public float MarkerOpacity { get; set; } = 0.8f;
    public int MaxMarkersPerMechanic { get; set; } = 3;
    public bool RequireSplatoon { get; set; } = false;
    
    public void Initialize() { }
    public void Save() => Plugin.PluginInterface.SavePluginConfig(this);
}
