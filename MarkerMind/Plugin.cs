using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;

namespace MarkerMind;

public sealed class Plugin : IDalamudPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public string Name => "MarkerMind";
    
    public Configuration Config { get; private set; } = null!;
    public GameStateTracker gameState { get; private set; } = null!;
    public TelemetryCollector telemetry { get; private set; } = null!;
    
    private ConfigWindow configWindow = null!;
    private BossmodBridge bossmodBridge = null!;
    private SplatoonRenderer splatoonRenderer = null!;
    private LearningEngine learningEngine = null!;

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        Instance = this;
        
        ECommonsMain.Init(pluginInterface, this, Module.ObjectFunctions);
        
        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize();
        
        // Initialize core systems
        gameState = new GameStateTracker();
        bossmodBridge = new BossmodBridge();
        telemetry = new TelemetryCollector();
        splatoonRenderer = new SplatoonRenderer();
        learningEngine = new LearningEngine();
        
        // Wire up events
        WireEvents();
        
        // UI
        configWindow = new ConfigWindow();
        Svc.PluginInterface.UiBuilder.Draw += configWindow.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        
        // Game loop
        Svc.Framework.Update += OnUpdate;
        
        // Welcome message
        var status = bossmodBridge.IsBossmodAvailable ? "enabled" : "disabled";
        Svc.Chat.Print($"[MarkerMind] Loaded! Bossmod integration: {status}");
    }
    
    private void WireEvents()
    {
        // Bossmod events -> Learning
        bossmodBridge.OnMechanicStart += (mechanic) =>
        {
            var mechanicId = ComputeMechanicHash(mechanic);
            learningEngine.StartMechanic(mechanicId, mechanic.MechanicName);
            
            // Render initial marker
            var disclosureLevel = learningEngine.GetDisclosureLevel(mechanicId);
            if (mechanic.SafeZones.Count > 0)
            {
                splatoonRenderer.RenderMarker(mechanicId, mechanic.SafeZones[0], disclosureLevel, gameState.Role);
            }
        };
        
        bossmodBridge.OnMechanicResolve += (mechanic, outcome) =>
        {
            var mechanicId = ComputeMechanicHash(mechanic);
            learningEngine.EndMechanic(outcome);
            splatoonRenderer.ClearAll();
        };
    }
    
    private string ComputeMechanicHash(MechanicEvent mechanic)
    {
        // Simple hash for now
        return $"{mechanic.BossId}-{mechanic.MechanicName}";
    }

    private void OnUpdate(IFramework framework)
    {
        if (!Svc.ClientState.IsLoggedIn) return;
        if (Svc.ClientState.LocalPlayer is not { } player) return;
        
        gameState.Update(player);
        bossmodBridge.Update();
        telemetry.Update();
        learningEngine.Update();
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnUpdate;
        Svc.PluginInterface.UiBuilder.Draw -= configWindow.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        
        learningEngine?.Dispose();
        splatoonRenderer?.Dispose();
        telemetry?.Dispose();
        bossmodBridge?.Dispose();
        gameState?.Dispose();
        
        ECommonsMain.Dispose();
    }
}
