using System;
using System.Collections.Generic;
using System.Numerics;

namespace MarkerMind;

public class TelemetryCollector : IDisposable
{
    private bool isCapturing = false;
    private string? currentMechanicId;
    private List<TelemetrySample> samples = new();
    private DateTime captureStartTime;
    private readonly float samplingInterval = 0.1f; // 10Hz
    private float timeSinceLastSample = 0f;
    
    public event Action<string, List<TelemetrySample>, string>? OnMechanicComplete;
    
    public void StartCapture(string mechanicId)
    {
        if (!Plugin.Instance.Config.EnableLearning) return;
        
        isCapturing = true;
        currentMechanicId = mechanicId;
        samples.Clear();
        captureStartTime = DateTime.UtcNow;
        timeSinceLastSample = 0f;
    }
    
    public void StopCapture(string outcome)
    {
        if (!isCapturing) return;
        
        isCapturing = false;
        
        if (currentMechanicId != null)
        {
            OnMechanicComplete?.Invoke(currentMechanicId, new List<TelemetrySample>(samples), outcome);
        }
        
        currentMechanicId = null;
        samples.Clear();
    }
    
    public void Update()
    {
        if (!isCapturing) return;
        if (Plugin.Instance.gameState?.LocalPlayer == null) return;
        
        timeSinceLastSample += (float)Plugin.Framework.UpdateDelta.TotalSeconds;
        
        if (timeSinceLastSample >= samplingInterval)
        {
            timeSinceLastSample = 0f;
            
            samples.Add(new TelemetrySample
            {
                Timestamp = DateTime.UtcNow,
                Position = Plugin.Instance.gameState!.Position,
                Velocity = Plugin.Instance.gameState.Velocity,
                Facing = Plugin.Instance.gameState.Facing,
                Role = Plugin.Instance.gameState.Role
            });
        }
    }
    
    public void Dispose()
    {
        if (isCapturing)
            StopCapture("interrupted");
    }
}

public class TelemetrySample
{
    public DateTime Timestamp { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Facing { get; set; }
    public PlayerRole Role { get; set; }
}
