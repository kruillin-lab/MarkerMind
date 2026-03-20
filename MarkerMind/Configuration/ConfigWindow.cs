using System;

namespace MarkerMind;

public class ConfigWindow : IDisposable
{
    private bool visible = false;
    public void Toggle() => visible = !visible;
    
    public void Draw()
    {
        // Stub - ImGui implementation would go here
        // This is a placeholder to allow compilation
    }
    
    public void Dispose() { }
}
