using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using System.Numerics;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using TankCooldownHelper.Core;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.UI;

public class DangerMeterModule : IDisposable
{
    private readonly Configuration _config;
    private readonly DangerCalculator _calculator;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;

    public DangerMeterModule(
        Configuration config,
        DangerCalculator calculator,
        DamageTracker damageTracker,
        HealingTracker healingTracker)
    {
        _config = config;
        _calculator = calculator;
        _damageTracker = damageTracker;
        _healingTracker = healingTracker;
    }

    public void Draw()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
        {
            ImGui.TextDisabled("Waiting for player data...");
            return;
        }

        var targetId = player.GameObjectId;
        var maxHp = player.MaxHp;
        var dps = _damageTracker.GetIncomingDps((uint)targetId);
        var hps = _healingTracker.GetIncomingHps((uint)targetId);
        var ratio = _calculator.CalculateDangerRatio(dps, hps);
        var level = _calculator.CalculateDangerLevel(dps, hps, maxHp);
        var hpPercentPerSec = maxHp > 0 ? (dps / maxHp) * 100f : 0f;
        
        DrawDangerBar(dps, hps, ratio, level);
        DrawStats(dps, hps, ratio, level, hpPercentPerSec, maxHp);
    }

    private void DrawDangerBar(float dps, float hps, float ratio, DangerLevel level)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();
        var windowWidth = ImGui.GetContentRegionAvail().X;
        var barHeight = 20f;
        
        // Background
        var bgMin = cursorPos;
        var bgMax = new Vector2(cursorPos.X + windowWidth, cursorPos.Y + barHeight);
        drawList.AddRectFilled(bgMin, bgMax, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)), 4f);
        
        // Calculate bar widths
        var total = dps + hps;
        if (total > 0)
        {
            var dpsWidth = (dps / total) * windowWidth;
            var hpsWidth = (hps / total) * windowWidth;
            
            // DPS bar (red)
            if (dps > 0)
            {
                var dpsMax = new Vector2(cursorPos.X + dpsWidth, cursorPos.Y + barHeight);
                drawList.AddRectFilled(cursorPos, dpsMax, 0xFFF44336, 4f);
            }
            
            // HPS bar (green)
            if (hps > 0)
            {
                var hpsMin = new Vector2(cursorPos.X + dpsWidth, cursorPos.Y);
                var hpsMax = new Vector2(hpsMin.X + hpsWidth, cursorPos.Y + barHeight);
                drawList.AddRectFilled(hpsMin, hpsMax, 0xFF4CAF50, 4f);
            }
        }
        
        // Border
        drawList.AddRect(bgMin, bgMax, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 1f)), 4f);
        
        ImGui.Dummy(new Vector2(0, barHeight + 4));
    }

    private void DrawStats(float dps, float hps, float ratio, DangerLevel level, float hpPercentPerSec, uint maxHp)
    {
        var colorU32 = _calculator.GetColorForDangerLevel(level);
        var colorVec = ImGui.ColorConvertU32ToFloat4(colorU32);
        
        ImGui.Text("DPS: "); ImGui.SameLine();
        ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), $"{dps:F0}");
        
        ImGui.SameLine();
        ImGui.TextDisabled(" | ");
        ImGui.SameLine();
        
        ImGui.Text("HPS: "); ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.3f, 1, 0.3f, 1), $"{hps:F0}");
        
        ImGui.SameLine();
        ImGui.TextDisabled(" | ");
        ImGui.SameLine();
        
        ImGui.Text("Ratio: "); ImGui.SameLine();
        ImGui.TextColored(colorVec, $"{ratio:F2}x");
        
        if (level != DangerLevel.Safe)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(" | ");
            ImGui.SameLine();
            
            var levelText = _calculator.GetDangerLevelText(level);
            ImGui.TextColored(colorVec, levelText);
        }
        
        // Show HP percentage damage
        ImGui.Text("HP Dmg: "); ImGui.SameLine();
        var hpColor = hpPercentPerSec >= _config.HpEmergencyThreshold ? new Vector4(1, 0.2f, 0.2f, 1) :
                      hpPercentPerSec >= _config.HpCriticalThreshold ? new Vector4(1, 0.6f, 0.2f, 1) :
                      hpPercentPerSec >= _config.HpWarningThreshold ? new Vector4(1, 0.9f, 0.2f, 1) :
                      new Vector4(0.8f, 0.8f, 0.8f, 1);
        ImGui.TextColored(hpColor, $"{hpPercentPerSec:F1}%/s");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
