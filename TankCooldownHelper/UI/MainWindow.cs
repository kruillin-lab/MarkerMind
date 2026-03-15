using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using ECommons.DalamudServices;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TankCooldownHelper.Core;
using ImGui = Dalamud.Bindings.ImGui.ImGui;

namespace TankCooldownHelper.UI;

public class MainWindow : Window, IDisposable
{
    private readonly Configuration _config;
    private readonly DangerCalculator _dangerCalculator;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;
    private readonly Predictor _predictor;
    
    // Module instances
    private readonly DangerMeterModule _dangerMeterModule;
    private readonly PredictiveTimelineModule _predictiveTimelineModule;

    public MainWindow(
        Configuration config,
        DangerCalculator dangerCalculator,
        DamageTracker damageTracker,
        HealingTracker healingTracker,
        Predictor predictor) 
        : base("Tank Cooldown Helper###MainWindow")
    {
        _config = config;
        _dangerCalculator = dangerCalculator;
        _damageTracker = damageTracker;
        _healingTracker = healingTracker;
        _predictor = predictor;
        
        _dangerMeterModule = new DangerMeterModule(config, dangerCalculator, damageTracker, healingTracker);
        _predictiveTimelineModule = new PredictiveTimelineModule(config, predictor, damageTracker, healingTracker);
        
        IsOpen = false;
        RespectCloseHotkey = true;
        // Window size is handled by ImGui - user can resize freely
    }

    public override void Draw()
    {
        if (_config.ShowInCombatOnly && !IsInCombat())
        {
            ImGui.TextDisabled("Show in combat only is enabled...");
            return;
        }

        if (_config.ShowDangerMeter)
        {
            _dangerMeterModule.Draw();
            ImGui.Separator();
        }

        if (_config.ShowPredictiveTimeline)
        {
            _predictiveTimelineModule.Draw();
            ImGui.Separator();
        }

        if (_config.ShowNetDamageCounter)
        {
            DrawNetDamageCounter();
        }

        if (_config.ShowPartyBreakdown)
        {
            ImGui.Separator();
            DrawPartyBreakdown();
        }
    }

    private void DrawNetDamageCounter()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return;

        var dps = _damageTracker.GetIncomingDps((uint)player.GameObjectId);
        var hps = _healingTracker.GetIncomingHps((uint)player.GameObjectId);
        var netDps = dps - hps;
        
        var color = netDps > 0 ? new Vector4(1, 0.27f, 0.27f, 1) : new Vector4(0.3f, 1, 0.3f, 1);
        
        ImGui.TextColored(color, $"Net Damage: {netDps:F0}/s");
        ImGui.SameLine();
        ImGui.TextDisabled($"(DPS: {dps:F0} | HPS: {hps:F0})");
    }

    private bool IsInCombat()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return false;
        
        // Check combat status via StatusList
        return player.StatusList.Any(s => s.StatusId == 418); // 418 = In Combat
    }

    private void DrawPartyBreakdown()
    {
        ImGui.Text("Party Breakdown");
        ImGui.Separator();

        // Get all party members
        var members = new List<(uint Id, string Name, uint JobId, bool IsPlayer)>();
        
        // Add player first
        var player = Svc.ClientState.LocalPlayer;
        if (player != null)
        {
            members.Add(((uint)player.GameObjectId, player.Name.TextValue, player.ClassJob.RowId, true));
        }

        // Add party members
        foreach (var member in Svc.Party)
        {
            if (member?.GameObject is not { } gameObject) continue;
            if (gameObject.GameObjectId == player?.GameObjectId) continue; // Skip player, already added
            
            members.Add(((uint)gameObject.GameObjectId, member.Name.TextValue, member.ClassJob.RowId, false));
        }

        // Add Trust NPCs (Duty Support system)
        foreach (var gameObject in Svc.Objects)
        {
            if (gameObject?.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.BattleNpc) continue;
            if (gameObject is not Dalamud.Game.ClientState.Objects.Types.ICharacter character) continue;
            if (character.MaxHp == 0) continue; // Skip dead/invalid NPCs
            if (members.Any(m => m.Id == (uint)gameObject.GameObjectId)) continue; // Already added
            
            // Check if NPC is friendly (Trust NPCs are friendly BattleNpcs)
            // Trust NPCs typically have the same Targetable status and are in the same "party" context
            if (!character.IsTargetable) continue;
            
            // Add to members list - we don't have ClassJob for BattleNpcs, so use 0
            members.Add(((uint)gameObject.GameObjectId, character.Name.TextValue, 0u, false));
        }

        // Sort by danger ratio (highest first) if configured
        var memberStats = members.Select(m =>
        {
            var dps = _damageTracker.GetIncomingDps(m.Id);
            var hps = _healingTracker.GetIncomingHps(m.Id);
            var ratio = _dangerCalculator.CalculateDangerRatio(dps, hps);
            // Try to get max HP from game object
            uint maxHp = 1;
            var gameObj = Svc.Objects.FirstOrDefault(o => o.GameObjectId == m.Id);
            if (gameObj is Dalamud.Game.ClientState.Objects.Types.ICharacter charObj)
            {
                maxHp = charObj.MaxHp;
            }
            var level = _dangerCalculator.CalculateDangerLevel(dps, hps, maxHp);
            var hpPercent = maxHp > 0 ? (dps / maxHp) * 100f : 0f;
            return (m, dps, hps, ratio, level, hpPercent);
        });

        if (_config.HighlightMostEndangered)
        {
            memberStats = memberStats.OrderByDescending(x => x.ratio);
        }

        // Draw table
        if (ImGui.BeginTable("PartyBreakdown", 5, (Dalamud.Bindings.ImGui.ImGuiTableFlags)(1 | 2))) // Borders | RowBg
        {
            ImGui.TableSetupColumn("Member");
            ImGui.TableSetupColumn("DPS");
            ImGui.TableSetupColumn("HPS");
            ImGui.TableSetupColumn("Ratio");
            ImGui.TableSetupColumn("Status");
            ImGui.TableHeadersRow();

            foreach (var (member, dps, hps, ratio, level, hpPercent) in memberStats)
            {
                ImGui.TableNextRow();

                // Name column
                ImGui.TableNextColumn();
                var prefix = member.IsPlayer ? "→ " : "";
                ImGui.Text($"{prefix}{member.Name}");

                // DPS column
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), $"{dps:F0}");

                // HPS column
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.3f, 1, 0.3f, 1), $"{hps:F0}");

                // Ratio column
                ImGui.TableNextColumn();
                var color = ImGui.ColorConvertU32ToFloat4(_dangerCalculator.GetColorForDangerLevel(level));
                ImGui.TextColored(color, $"{ratio:F2}x");

                // Status column
                ImGui.TableNextColumn();
                ImGui.TextColored(color, _dangerCalculator.GetDangerLevelText(level));
            }

            ImGui.EndTable();
        }
    }

    public new void Update()
    {
        // Called every Framework.Update - can be used for animations or smooth updates
    }

    public new void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Dispose()
    {
        _dangerMeterModule.Dispose();
        _predictiveTimelineModule.Dispose();
    }
}
