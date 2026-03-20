using Dalamud.Bindings.ImGui;
using System;

namespace MarkerMind;

public class ConfigWindow : IDisposable
{
    private bool visible = false;
    private bool settingsOpen = false;
    
    public void Toggle() 
    {
        visible = !visible;
        settingsOpen = visible;
    }
    
    public void Draw()
    {
        if (!settingsOpen) return;
        
        ImGui.Begin("MarkerMind Settings", ref settingsOpen, ImGuiWindowFlags.None);
        
        if (ImGui.CollapsingHeader("Learning Settings", ImGuiTreeNodeFlags.DefaultOpen))
        {
            var enableLearning = Plugin.Instance.Config.EnableLearning;
            if (ImGui.Checkbox("Enable Learning", ref enableLearning))
            {
                Plugin.Instance.Config.EnableLearning = enableLearning;
                Plugin.Instance.Config.Save();
            }
            
            ImGui.Separator();
            ImGui.Text("Progressive Disclosure Thresholds:");
            
            var l2 = Plugin.Instance.Config.Level2Threshold;
            if (ImGui.SliderFloat("Level 2 (Safe spots)", ref l2, 0.0f, 1.0f, "%.2f"))
            {
                Plugin.Instance.Config.Level2Threshold = l2;
                Plugin.Instance.Config.Save();
            }
            
            var l3 = Plugin.Instance.Config.Level3Threshold;
            if (ImGui.SliderFloat("Level 3 (Paths)", ref l3, 0.0f, 1.0f, "%.2f"))
            {
                Plugin.Instance.Config.Level3Threshold = l3;
                Plugin.Instance.Config.Save();
            }
            
            var l4 = Plugin.Instance.Config.Level4Threshold;
            if (ImGui.SliderFloat("Level 4 (Role-specific)", ref l4, 0.0f, 1.0f, "%.2f"))
            {
                Plugin.Instance.Config.Level4Threshold = l4;
                Plugin.Instance.Config.Save();
            }
        }
        
        if (ImGui.CollapsingHeader("Data Management"))
        {
            ImGui.Text("Learning Data:");
            if (ImGui.Button("Clear All Learning Data"))
            {
                // TODO: Implement data clearing
                Plugin.Chat.Print("[MarkerMind] Learning data cleared (not implemented yet)");
            }
            
            ImGui.SameLine();
            ImGui.TextDisabled("(This cannot be undone)");
        }
        
        ImGui.End();
        
        // Update visibility state
        if (!settingsOpen)
            visible = false;
    }
    
    public void Dispose() { }
}
