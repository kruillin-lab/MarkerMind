using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using System.Numerics;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace TankCooldownHelper.UI;

public class SettingsWindow : Window, IDisposable
{
    private readonly Configuration _config;

    public SettingsWindow(Configuration config) 
        : base("Tank Cooldown Helper - Settings###SettingsWindow")
    {
        _config = config;
        IsOpen = false;
        RespectCloseHotkey = true;
        // Window size is handled by ImGui - user can resize freely
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("SettingsTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Thresholds"))
            {
                DrawThresholdsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Display"))
            {
                DrawDisplayTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        
        if (ImGui.Button("Save"))
        {
            _config.Save(Svc.PluginInterface);
            ImGui.CloseCurrentPopup();
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("Cancel"))
        {
            IsOpen = false;
        }
    }

    private void DrawGeneralTab()
    {
        ImGui.Text("Time Window Settings");
        ImGui.Separator();
        
        var windowSeconds = _config.TimeWindowSeconds;
        if (ImGui.SliderFloat("Time Window (seconds)", ref windowSeconds, _config.MinTimeWindow, _config.MaxTimeWindow, "%.1f"))
        {
            _config.TimeWindowSeconds = windowSeconds;
        }
        ImGui.TextDisabled("How far back to look for damage/healing calculations");
        
        ImGui.Spacing();
        
        ImGui.Text("Party Tracking");
        ImGui.Separator();
        
        var trackParty = _config.TrackFullParty;
        if (ImGui.Checkbox("Track Full Party", ref trackParty))
        {
            _config.TrackFullParty = trackParty;
        }
        
        var highlightEndangered = _config.HighlightMostEndangered;
        if (ImGui.Checkbox("Highlight Most Endangered Member", ref highlightEndangered))
        {
            _config.HighlightMostEndangered = highlightEndangered;
        }
        
        ImGui.Spacing();
        
        ImGui.Text("Combat Display");
        ImGui.Separator();
        
        var showInCombatOnly = _config.ShowInCombatOnly;
        if (ImGui.Checkbox("Show Only In Combat", ref showInCombatOnly))
        {
            _config.ShowInCombatOnly = showInCombatOnly;
        }
    }

    private void DrawThresholdsTab()
    {
        ImGui.Text("Danger Ratio Thresholds (DPS / HPS)");
        ImGui.Separator();
        
        var warning = _config.WarningThreshold;
        if (ImGui.SliderFloat("Warning Threshold", ref warning, 0.5f, 3.0f, "%.2fx"))
        {
            _config.WarningThreshold = Math.Min(warning, _config.CriticalThreshold);
        }
        ImGui.TextDisabled("Yellow warning when damage exceeds healing by this ratio");
        
        ImGui.Spacing();
        
        var critical = _config.CriticalThreshold;
        if (ImGui.SliderFloat("Critical Threshold", ref critical, 1.0f, 4.0f, "%.2fx"))
        {
            _config.CriticalThreshold = Math.Max(critical, _config.WarningThreshold);
        }
        ImGui.TextDisabled("Orange alert when ratio reaches this level");
        
        ImGui.Spacing();
        
        var emergency = _config.EmergencyThreshold;
        if (ImGui.SliderFloat("Emergency Threshold", ref emergency, 1.5f, 5.0f, "%.2fx"))
        {
            _config.EmergencyThreshold = Math.Max(emergency, _config.CriticalThreshold);
        }
        ImGui.TextDisabled("Red emergency when ratio reaches this level");
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.Text("HP-Based Danger Thresholds (% of Max HP per second)");
        ImGui.Separator();
        
        var hpWarning = _config.HpWarningThreshold;
        if (ImGui.SliderFloat("HP Warning Threshold", ref hpWarning, 1.0f, 10.0f, "%.1f%%"))
        {
            _config.HpWarningThreshold = Math.Min(hpWarning, _config.HpCriticalThreshold);
        }
        ImGui.TextDisabled("Warning when taking this % of max HP per second");
        
        ImGui.Spacing();
        
        var hpCritical = _config.HpCriticalThreshold;
        if (ImGui.SliderFloat("HP Critical Threshold", ref hpCritical, 5.0f, 20.0f, "%.1f%%"))
        {
            _config.HpCriticalThreshold = Math.Max(hpCritical, _config.HpWarningThreshold);
        }
        ImGui.TextDisabled("Critical when taking this % of max HP per second");
        
        ImGui.Spacing();
        
        var hpEmergency = _config.HpEmergencyThreshold;
        if (ImGui.SliderFloat("HP Emergency Threshold", ref hpEmergency, 10.0f, 50.0f, "%.1f%%"))
        {
            _config.HpEmergencyThreshold = Math.Max(hpEmergency, _config.HpCriticalThreshold);
        }
        ImGui.TextDisabled("Emergency when taking this % of max HP per second");
    }

    private void DrawDisplayTab()
    {
        ImGui.Text("Visible Modules");
        ImGui.Separator();
        
        var showDangerMeter = _config.ShowDangerMeter;
        if (ImGui.Checkbox("Danger Meter (Bar + Stats)", ref showDangerMeter))
        {
            _config.ShowDangerMeter = showDangerMeter;
        }
        
        var showTimeline = _config.ShowPredictiveTimeline;
        if (ImGui.Checkbox("Predictive Timeline (HP Projection)", ref showTimeline))
        {
            _config.ShowPredictiveTimeline = showTimeline;
        }
        
        var showNetDamage = _config.ShowNetDamageCounter;
        if (ImGui.Checkbox("Net Damage Counter", ref showNetDamage))
        {
            _config.ShowNetDamageCounter = showNetDamage;
        }
        
        var showPartyBreakdown = _config.ShowPartyBreakdown;
        if (ImGui.Checkbox("Party Breakdown (Full Party List)", ref showPartyBreakdown))
        {
            _config.ShowPartyBreakdown = showPartyBreakdown;
        }
        
        ImGui.Spacing();
        
        ImGui.Text("Window Settings");
        ImGui.Separator();
        
        var lockPosition = _config.LockWindowPosition;
        if (ImGui.Checkbox("Lock Window Position", ref lockPosition))
        {
            _config.LockWindowPosition = lockPosition;
        }
        
        var opacity = _config.WindowOpacity;
        if (ImGui.SliderFloat("Window Opacity", ref opacity, 0.5f, 1.0f, "%.2f"))
        {
            _config.WindowOpacity = opacity;
        }
    }

    public new void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
