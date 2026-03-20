using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace MarkerMind;

public sealed class Plugin : IDalamudPlugin
{
    public static Plugin Instance { get; private set; } = null!;
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static IClientState ClientState { get; private set; } = null!;
    public static IFramework Framework { get; private set; } = null!;
    public static IChatGui Chat { get; private set; } = null!;
    public static IObjectTable Objects { get; private set; } = null!;
    public static ICondition Condition { get; private set; } = null!;
    
    public string Name => "MarkerMind";
    
    public Configuration Config { get; private set; } = null!;
    public GameStateTracker gameState { get; private set; } = null!;
    public TelemetryCollector telemetry { get; private set; } = null!;
    
    private ConfigWindow configWindow = null!;
    private BossmodBridge bossmodBridge = null!;
    private SplatoonRenderer splatoonRenderer = null!;
    private LearningEngine learningEngine = null!;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IClientState clientState,
        IFramework framework,
        IChatGui chat,
        IObjectTable objects,
        ICondition condition)
    {
        Instance = this;
        PluginInterface = pluginInterface;
        ClientState = clientState;
        Framework = framework;
        Chat = chat;
        Objects = objects;
        Condition = condition;
        
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
        pluginInterface.UiBuilder.Draw += configWindow.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        pluginInterface.UiBuilder.OpenMainUi += configWindow.Toggle;
        
        // Game loop
        framework.Update += OnUpdate;
        
        // Welcome message
        var status = bossmodBridge.IsBossmodAvailable ? "enabled" : "disabled";
        chat.Print($"[MarkerMind] Loaded! Bossmod integration: {status}");
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
        if (!ClientState.IsLoggedIn) return;
        if (ClientState.LocalPlayer is not { } player) return;
        
        gameState.Update(player);
        bossmodBridge.Update();
        telemetry.Update();
        learningEngine.Update();
    }

    public void Dispose()
    {
        Framework.Update -= OnUpdate;
        PluginInterface.UiBuilder.Draw -= configWindow.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Toggle;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Toggle;
        
        learningEngine?.Dispose();
        splatoonRenderer?.Dispose();
        telemetry?.Dispose();
        bossmodBridge?.Dispose();
        gameState?.Dispose();
    }
}
