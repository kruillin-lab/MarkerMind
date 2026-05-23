using System;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using ECommons;

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
    public static ICommandManager CommandManager { get; private set; } = null!;
    public static IGameGui GameGui { get; private set; } = null!;
    
    public string Name => "MarkerMind";
    
    public Configuration Config { get; private set; } = null!;
    public GameStateTracker gameState { get; private set; } = null!;
    public TelemetryCollector telemetry { get; private set; } = null!;
    
    public ConfigWindow configWindow { get; private set; } = null!;
    public BossmodBridge bossmodBridge { get; private set; } = null!;
    public SplatoonRenderer splatoonRenderer { get; private set; } = null!;
    public OverlayRenderer overlayRenderer { get; private set; } = null!;
    public LearningEngine learningEngine { get; private set; } = null!;

    public Plugin(
        IDalamudPluginInterface pluginInterface,
        IClientState clientState,
        IFramework framework,
        IChatGui chat,
        IObjectTable objects,
        ICondition condition,
        ICommandManager commandManager,
        IGameGui gameGui)
    {
        Instance = this;
        PluginInterface = pluginInterface;
        ClientState = clientState;
        Framework = framework;
        Chat = chat;
        Objects = objects;
        Condition = condition;
        CommandManager = commandManager;
        GameGui = gameGui;

        ECommonsMain.Init(pluginInterface, this, ECommons.Module.SplatoonAPI);
        
        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize();
        
        // Initialize core systems
        gameState = new GameStateTracker();
        bossmodBridge = new BossmodBridge();
        telemetry = new TelemetryCollector();
        splatoonRenderer = new SplatoonRenderer();
        overlayRenderer = new OverlayRenderer(gameGui);
        learningEngine = new LearningEngine();
        
        // Wire up events
        WireEvents();
        
        // UI
        configWindow = new ConfigWindow();
        pluginInterface.UiBuilder.Draw += configWindow.Draw;
        pluginInterface.UiBuilder.Draw += overlayRenderer.Draw;
        pluginInterface.UiBuilder.OpenConfigUi += configWindow.Open;
        pluginInterface.UiBuilder.OpenMainUi += configWindow.Open;
        
        // Register slash commands
        CommandManager.AddHandler("/markermind", new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "Open MarkerMind settings or override disclosure level.",
            ShowInHelp = true
        });
        CommandManager.AddHandler("/mm", new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "Alias for /markermind.",
            ShowInHelp = false
        });

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
            
            var disclosureLevel = learningEngine.GetDisclosureLevel(mechanicId);
            var markerPosition = learningEngine.ActiveMarkerPosition;
            if (markerPosition != null)
            {
                disclosureLevel = Math.Max(disclosureLevel, 2);
            }
            else if (mechanic.SafeZones.Count > 0)
            {
                markerPosition = mechanic.SafeZones[0];
            }
            else if (mechanic.BossPosition is { } bossPos)
            {
                markerPosition = bossPos;
                disclosureLevel = Math.Min(disclosureLevel, 1);
            }

            if (markerPosition is { } position)
            {
                splatoonRenderer.RenderMarker(mechanicId, position, disclosureLevel, gameState.Role);
                overlayRenderer.RenderMarker(mechanicId, position, disclosureLevel, gameState.Role);
            }
            else
            {
                Chat.Print($"[MarkerMind] {mechanic.MechanicName}: no learned safe spot yet.");
            }
        };
        
        bossmodBridge.OnMechanicResolve += (mechanic, outcome) =>
        {
            var mechanicId = ComputeMechanicHash(mechanic);
            learningEngine.EndMechanic(outcome);
            splatoonRenderer.ClearAll();
            overlayRenderer.ClearAll();
        };
    }
    
    private string ComputeMechanicHash(MechanicEvent mechanic)
    {
        return string.IsNullOrWhiteSpace(mechanic.MechanicId)
            ? $"{mechanic.BossId}-{mechanic.MechanicName}"
            : mechanic.MechanicId;
    }

    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            configWindow.Toggle();
            return;
        }

        var parts = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && parts[0].Equals("level", StringComparison.OrdinalIgnoreCase) && parts[1].Equals("set", StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(parts[2], out var lvl) && lvl >= 1 && lvl <= 4)
            {
                Config.OverrideDisclosureLevel = lvl;
                Config.Save();
                Chat.Print($"[MarkerMind] Forced progressive disclosure level overridden to: Level {lvl}.");
            }
            else
            {
                Chat.Print("[MarkerMind] Invalid level. Please use a number between 1 and 4 (e.g. /markermind level set 3).");
            }
        }
        else if (parts.Length >= 2 && parts[0].Equals("level", StringComparison.OrdinalIgnoreCase) && (parts[1].Equals("clear", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("reset", StringComparison.OrdinalIgnoreCase)))
        {
            Config.OverrideDisclosureLevel = 0;
            Config.Save();
            Chat.Print("[MarkerMind] Forced progressive disclosure level cleared. Running in Automatic mode.");
        }
        else
        {
            Chat.Print("[MarkerMind] Usage:\n" +
                       "  /markermind - Open settings UI\n" +
                       "  /markermind level set {1-4} - Force progressive disclosure level\n" +
                       "  /markermind level clear - Clear forced level and run automatically");
        }
    }

    private void OnUpdate(IFramework framework)
    {
        if (!ClientState.IsLoggedIn) return;
        if (Objects.LocalPlayer is not { } player) return;
        
        gameState.Update(player);
        bossmodBridge.Update();
        telemetry.Update();
        learningEngine.Update();
    }

    public void Dispose()
    {
        Framework.Update -= OnUpdate;
        PluginInterface.UiBuilder.Draw -= configWindow.Draw;
        PluginInterface.UiBuilder.Draw -= overlayRenderer.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= configWindow.Open;
        PluginInterface.UiBuilder.OpenMainUi -= configWindow.Open;
        
        CommandManager.RemoveHandler("/markermind");
        CommandManager.RemoveHandler("/mm");

        learningEngine?.Dispose();
        splatoonRenderer?.Dispose();
        overlayRenderer?.Dispose();
        telemetry?.Dispose();
        bossmodBridge?.Dispose();
        gameState?.Dispose();
        ECommonsMain.Dispose();
    }
}
