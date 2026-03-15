using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using System.Numerics;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using TankCooldownHelper.Core;

namespace TankCooldownHelper.UI;

public class PredictiveTimelineModule : IDisposable
{
    private readonly Configuration _config;
    private readonly Predictor _predictor;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;

    public PredictiveTimelineModule(Configuration config, Predictor predictor, DamageTracker damageTracker, HealingTracker healingTracker)
    {
        _config = config;
        _predictor = predictor;
        _damageTracker = damageTracker;
        _healingTracker = healingTracker;
    }

    public void Draw()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return;

        var currentHp = player.CurrentHp;
        var maxHp = player.MaxHp;
        
        // Get real damage/healing from trackers
        var dps = _damageTracker.GetIncomingDps((uint)player.GameObjectId);
        var hps = _healingTracker.GetIncomingHps((uint)player.GameObjectId);
        
        var deathTimer = _predictor.PredictTimeOfDeath(currentHp, dps, hps);
        
        DrawTimeline(currentHp, maxHp, dps, hps, deathTimer);
        DrawDeathWarning(deathTimer);
    }

    private void DrawTimeline(uint currentHp, uint maxHp, float dps, float hps, float? deathTimer)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = 60f;
        
        // Draw background grid
        for (int i = 0; i <= 4; i++)
        {
            var y = cursorPos.Y + (height / 4) * i;
            drawList.AddLine(
                new Vector2(cursorPos.X, y),
                new Vector2(cursorPos.X + width, y),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.3f)),
                1f
            );
        }
        
        // Draw HP projection line
        var projectionSeconds = 10;
        var timeline = _predictor.ProjectHpTimeline(currentHp, maxHp, dps, hps, projectionSeconds);
        
        var points = new Vector2[projectionSeconds + 1];
        for (int i = 0; i <= projectionSeconds; i++)
        {
            var x = cursorPos.X + (width / projectionSeconds) * i;
            var hpPercent = timeline[i] / maxHp;
            var y = cursorPos.Y + height - (hpPercent * height);
            points[i] = new Vector2(x, y);
        }
        
        // Draw line
        for (int i = 0; i < projectionSeconds; i++)
        {
            var color = points[i + 1].Y > points[i].Y ? 0xFF4CAF50 : 0xFFF44336;
            drawList.AddLine(points[i], points[i + 1], color, 2f);
        }
        
        // Draw current HP marker
        var currentY = cursorPos.Y + height - ((float)currentHp / maxHp * height);
        drawList.AddCircleFilled(new Vector2(cursorPos.X, currentY), 4f, 0xFFFFFFFF);
        
        // Draw death marker if applicable
        if (deathTimer.HasValue && deathTimer.Value <= projectionSeconds)
        {
            var deathX = cursorPos.X + (width / projectionSeconds) * deathTimer.Value;
            drawList.AddCircleFilled(new Vector2(deathX, cursorPos.Y + height), 6f, 0xFFF44336);
            
            var text = $"Death in {deathTimer.Value:F1}s";
            var textSize = ImGui.CalcTextSize(text);
            drawList.AddText(
                new Vector2(deathX - textSize.X / 2, cursorPos.Y + height - 20),
                0xFFF44336,
                text
            );
        }
        
        // Labels
        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + height + 4));
        ImGui.TextDisabled("Now");
        
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 60);
        ImGui.TextDisabled($"+{projectionSeconds}s");
        
        ImGui.Dummy(new Vector2(0, 4));
    }

    private void DrawDeathWarning(float? deathTimer)
    {
        if (!deathTimer.HasValue) return;
        
        var timer = deathTimer.Value;
        var color = timer < 5 ? new Vector4(1, 0.2f, 0.2f, 1) :
                    timer < 10 ? new Vector4(1, 0.6f, 0.2f, 1) :
                    new Vector4(0.8f, 0.8f, 0.8f, 1);
        
        ImGui.TextColored(color, $"⚠ Death projected in {timer:F1} seconds");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
