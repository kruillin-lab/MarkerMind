using ImGuiNET;
using System;

namespace MarkerMind;

public class ConfigWindow : IDisposable
{
    private bool visible = false;
    public void Toggle() => visible = !visible;
    
    public void Draw()
    {
        if (!visible) return;
        
        if (ImGui.Begin("MarkerMind Settings", ref visible))
        {
            ImGui.Text("Learning Settings");
            ImGui.Separator();
            
            var enableLearning = Plugin.Instance.Config.EnableLearning;
            if (ImGui.Checkbox("Enable Learning", ref enableLearning))
                Plugin.Instance.Config.EnableLearning = enableLearning;
            
            ImGui.Text("Progressive Disclosure Thresholds:");
            var l2 = Plugin.Instance.Config.Level2Threshold;
            if (ImGui.SliderFloat("Level 2 (Safe spots)", ref l2, 0.0f, 1.0f, "%.2f"))
                Plugin.Instance.Config.Level2Threshold = l2;
            
            var l3 = Plugin.Instance.Config.Level3Threshold;
            if (ImGui.SliderFloat("Level 3 (Paths)", ref l3, 0.0f, 1.0f, "%.2f"))
                Plugin.Instance.Config.Level3Threshold = l3;
            
            var l4 = Plugin.Instance.Config.Level4Threshold;
            if (ImGui.SliderFloat("Level 4 (Role-specific)", ref l4, 0.0f, 1.0f, "%.2f"))
                Plugin.Instance.Config.Level4Threshold = l4;
            
            ImGui.Separator();
            if (ImGui.Button("Save"))
                Plugin.Instance.Config.Save();
        }
        ImGui.End();
    }
    
    public void Dispose() { }
}
