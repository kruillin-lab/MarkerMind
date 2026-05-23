using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using Dalamud.Bindings.ImGui;

namespace MarkerMind;

public class ConfigWindow : IDisposable
{
    private bool visible = false;

    // Simulator state
    private string mockMechanicId = "sim_mechanic_1";
    private string mockMechanicName = "Mock Mechanic";
    private int mockBossId = 999;
    private float mockDuration = 5.0f;
    private MechanicEvent? activeSimulatedEvent;

    public void Open() => visible = true;
    public void Toggle() => visible = !visible;
    
    public void Draw()
    {
        if (!visible)
            return;

        ImGui.SetNextWindowSize(new Vector2(480, 480), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("MarkerMind Settings & Tools", ref visible))
        {
            ImGui.End();
            return;
        }

        var config = Plugin.Instance.Config;
        var changed = false;

        if (ImGui.BeginTabBar("MarkerMindTabBar"))
        {
            // ==================== SETTINGS TAB ====================
            if (ImGui.BeginTabItem("Settings"))
            {
                ImGui.Spacing();
                ImGui.TextUnformatted("Learning Engine");
                ImGui.Separator();
                changed |= Checkbox("Enable Learning Engine", config.EnableLearning, v => config.EnableLearning = v);
                changed |= InputIntClamped("Minimum Samples for Confidence", config.MinSampleSize, 1, 100, v => config.MinSampleSize = v);
                changed |= SliderFloatClamped("Cluster Distance Threshold", config.ClusterDistance, 0.5f, 10.0f, v => config.ClusterDistance = v);
                changed |= SliderFloatClamped("Position Tolerance", config.PositionTolerance, 0.5f, 10.0f, v => config.PositionTolerance = v);
                changed |= SliderFloatClamped("EMA Smoothing (Alpha)", config.EmaAlpha, 0.01f, 1.0f, v => config.EmaAlpha = v);

                ImGui.Spacing();
                ImGui.TextUnformatted("Progressive Disclosure");
                ImGui.Separator();
                changed |= SliderFloatClamped("Level 2 Confidence Threshold", config.Level2Threshold, 0.0f, 1.0f, v => config.Level2Threshold = v);
                changed |= SliderFloatClamped("Level 3 Confidence Threshold", config.Level3Threshold, 0.0f, 1.0f, v => config.Level3Threshold = v);
                changed |= SliderFloatClamped("Level 4 Confidence Threshold", config.Level4Threshold, 0.0f, 1.0f, v => config.Level4Threshold = v);

                ImGui.Spacing();
                int currentOverride = config.OverrideDisclosureLevel;
                string[] levels = { "Automatic (Dynamic)", "Level 1 (Raw/Telemetry only)", "Level 2 (Initial prediction)", "Level 3 (Aggressive/Tighter)", "Level 4 (Forced/Validated spots)" };
                if (ImGui.Combo("Override Disclosure Level", ref currentOverride, levels, levels.Length))
                {
                    config.OverrideDisclosureLevel = currentOverride;
                    changed = true;
                }

                ImGui.Spacing();
                ImGui.TextUnformatted("Rendering");
                ImGui.Separator();
                changed |= SliderFloatClamped("Marker Opacity", config.MarkerOpacity, 0.05f, 1.0f, v => config.MarkerOpacity = v);
                changed |= InputIntClamped("Max Markers Per Mechanic", config.MaxMarkersPerMechanic, 1, 20, v => config.MaxMarkersPerMechanic = v);
                changed |= Checkbox("Require Splatoon Integration", config.RequireSplatoon, v => config.RequireSplatoon = v);
                changed |= Checkbox("Show In-Game Ground Overlay", config.ShowGameOverlay, v => config.ShowGameOverlay = v);

                ImGui.Spacing();
                if (ImGui.Button("Save Configuration"))
                {
                    config.Save();
                    changed = false;
                    Plugin.Chat.Print("[MarkerMind] Settings saved successfully.");
                }

                ImGui.EndTabItem();
            }

            // ==================== SIMULATOR TAB ====================
            if (ImGui.BeginTabItem("Simulator"))
            {
                ImGui.Spacing();
                ImGui.TextUnformatted("Mechanic Simulator");
                ImGui.Separator();

                ImGui.InputText("Mechanic ID", ref mockMechanicId, 64);
                ImGui.InputText("Mechanic Name", ref mockMechanicName, 64);
                
                var bossIdRef = mockBossId;
                if (ImGui.InputInt("Boss/NPC ID", ref bossIdRef))
                {
                    mockBossId = Math.Max(0, bossIdRef);
                }
                
                ImGui.SliderFloat("Cast Duration (s)", ref mockDuration, 1.0f, 30.0f, "%.1f");

                ImGui.Spacing();
                ImGui.TextUnformatted("Simulation Actions");
                ImGui.Separator();

                var hasActive = activeSimulatedEvent != null;
                
                if (activeSimulatedEvent is { } activeSim)
                {
                    ImGui.TextColored(new Vector4(0.3f, 1.0f, 0.3f, 1.0f), $"Active: {activeSim.MechanicName} ({activeSim.MechanicId})");
                    ImGui.Spacing();

                    if (ImGui.Button("Trigger Survived (Success)"))
                    {
                        Plugin.Instance.bossmodBridge.TriggerMockMechanicResolve(activeSim, "survived");
                        activeSimulatedEvent = null;
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Trigger Died (Failure)"))
                    {
                        Plugin.Instance.bossmodBridge.TriggerMockMechanicResolve(activeSim, "died");
                        activeSimulatedEvent = null;
                    }
                }
                else
                {
                    ImGui.TextUnformatted("No active simulated mechanic.");
                    ImGui.Spacing();

                    if (ImGui.Button("Trigger Start Cast"))
                    {
                        activeSimulatedEvent = new MechanicEvent
                        {
                            MechanicId = mockMechanicId,
                            MechanicName = mockMechanicName,
                            BossId = (uint)mockBossId,
                            BossPosition = Plugin.Instance.gameState?.Position ?? Vector3.Zero,
                            Duration = mockDuration,
                            RemainingTime = mockDuration,
                            Type = MechanicType.Other
                        };
                        Plugin.Instance.bossmodBridge.TriggerMockMechanicStart(activeSimulatedEvent);
                    }
                }

                ImGui.Spacing();
                ImGui.TextUnformatted("Manual Telemetry Injector");
                ImGui.Separator();

                var currentPos = Plugin.Instance.gameState?.Position ?? Vector3.Zero;
                var currentRole = Plugin.Instance.gameState?.Role ?? PlayerRole.Unknown;
                
                ImGui.TextUnformatted($"Position: X: {currentPos.X:F2}, Y: {currentPos.Y:F2}, Z: {currentPos.Z:F2}");
                ImGui.TextUnformatted($"Current Role: {currentRole}");

                if (ImGui.Button("Inject Current Position as Safe Spot"))
                {
                    var targetId = Plugin.Instance.learningEngine.ActiveMechanicId ?? mockMechanicId;
                    var targetName = Plugin.Instance.learningEngine.ActiveMechanicId != null ? "Active Encounter Mechanic" : mockMechanicName;
                    
                    Plugin.Instance.learningEngine.InjectManualSample(targetId, targetName, currentPos, currentRole);
                    Plugin.Chat.Print($"[MarkerMind] Injected telemetry safe spot into '{targetName}' ({targetId}) at position {currentPos} for role {currentRole}.");
                }

                ImGui.EndTabItem();
            }

            // ==================== DATABASE TAB ====================
            if (ImGui.BeginTabItem("Encounter Database"))
            {
                ImGui.Spacing();
                
                var currentTerritory = Plugin.ClientState.TerritoryType;
                var db = Plugin.Instance.learningEngine.DataStore;
                var encounterIds = db.GetSavedEncounterIds();

                ImGui.TextUnformatted($"Saved Encounters ({encounterIds.Length})");
                ImGui.TextUnformatted($"Current Territory ID: {currentTerritory}");
                ImGui.Separator();

                if (ImGui.BeginChild("EncounterList", new Vector2(0, 200), true))
                {
                    if (encounterIds.Length == 0)
                    {
                        ImGui.TextUnformatted("No saved encounter profiles found.");
                    }
                    else
                    {
                        foreach (var id in encounterIds)
                        {
                            var isCurrent = id == currentTerritory.ToString();
                            if (isCurrent)
                            {
                                ImGui.TextColored(new Vector4(0.3f, 1.0f, 1.0f, 1.0f), $"* Encounter {id} (Current Zone)");
                            }
                            else
                            {
                                ImGui.TextUnformatted($"  Encounter {id}");
                            }

                            ImGui.SameLine(ImGui.GetWindowWidth() - 180);
                            
                            if (ImGui.Button($"Export##{id}"))
                            {
                                ExportEncounter(id);
                            }
                            
                            ImGui.SameLine();
                            if (ImGui.Button($"Delete##{id}"))
                            {
                                db.DeleteEncounter(id);
                                Plugin.Chat.Print($"[MarkerMind] Deleted encounter profile for Zone {id}.");
                            }
                        }
                    }
                    ImGui.EndChild();
                }

                ImGui.Spacing();
                ImGui.TextUnformatted("Import Shared Profile");
                ImGui.Separator();
                
                if (ImGui.Button("Import from Clipboard", new Vector2(-1, 30)))
                {
                    ImportEncounterFromClipboard();
                }
                ImGui.TextWrapped("Click to import a JSON encounter profile currently stored in your clipboard. It will be verified and added to your database.");

                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }

        ImGui.Spacing();
        ImGui.Separator();
        
        if (ImGui.Button("Close Window"))
        {
            visible = false;
        }

        if (changed)
        {
            config.Save();
        }

        ImGui.End();
    }

    private void ExportEncounter(string encounterId)
    {
        try
        {
            var db = Plugin.Instance.learningEngine.DataStore;
            var data = db.LoadEncounter(encounterId);
            if (data == null)
            {
                Plugin.Chat.Print($"[MarkerMind] Could not load encounter {encounterId}.");
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new Vector3JsonConverter());

            var json = JsonSerializer.Serialize(data, options);
            ImGui.SetClipboardText(json);
            Plugin.Chat.Print($"[MarkerMind] Encounter profile {encounterId} successfully copied to clipboard!");
        }
        catch (Exception ex)
        {
            Plugin.Chat.Print($"[MarkerMind] Failed to export encounter {encounterId}: {ex.Message}");
        }
    }

    private void ImportEncounterFromClipboard()
    {
        try
        {
            var json = ImGui.GetClipboardText();
            if (string.IsNullOrWhiteSpace(json))
            {
                Plugin.Chat.Print("[MarkerMind] Import failed: Clipboard is empty or does not contain text.");
                return;
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            options.Converters.Add(new Vector3JsonConverter());

            var data = JsonSerializer.Deserialize<EncounterData>(json, options);
            if (data == null || string.IsNullOrWhiteSpace(data.EncounterId))
            {
                Plugin.Chat.Print("[MarkerMind] Import failed: Clipboard JSON is not a valid encounter profile schema.");
                return;
            }

            var db = Plugin.Instance.learningEngine.DataStore;
            db.SaveEncounter(data.EncounterId, data);
            Plugin.Chat.Print($"[MarkerMind] Successfully imported encounter {data.EncounterId} with {data.Mechanics.Count} mechanics!");
        }
        catch (Exception ex)
        {
            Plugin.Chat.Print($"[MarkerMind] Import failed with exception: {ex.Message}");
        }
    }

    private static bool Checkbox(string label, bool value, Action<bool> setValue)
    {
        var next = value;
        if (!ImGui.Checkbox(label, ref next))
            return false;

        setValue(next);
        return true;
    }

    private static bool SliderFloatClamped(string label, float value, float min, float max, Action<float> setValue)
    {
        var next = value;
        if (!ImGui.SliderFloat(label, ref next, min, max, "%.2f"))
            return false;

        setValue(Math.Clamp(next, min, max));
        return true;
    }

    private static bool InputIntClamped(string label, int value, int min, int max, Action<int> setValue)
    {
        var next = value;
        if (!ImGui.InputInt(label, ref next))
            return false;

        setValue(Math.Clamp(next, min, max));
        return true;
    }
    
    public void Dispose() { }
}
