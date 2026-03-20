# MarkerMind Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Dalamud plugin that bridges Bossmod Reborn's mechanic detection to Splatoon's visual markers, with self-learning capabilities that improve predictions over time.

**Architecture:** Plugin uses ECommons for IPC and service management. Adapter layer connects to Bossmod events, Translation Engine maps mechanics to marker strategies, Predictor scores positions based on learned patterns, and Renderer outputs to Splatoon via IPC. Learning data stored locally in JSON.

**Tech Stack:** C# 11, Dalamud API 10, ECommons (IPC, services), .NET 8, Splatoon IPC (optional), Bossmod IPC (optional)

---

## Project Structure

```
MarkerMind/
├── MarkerMind/
│   ├── MarkerMind.csproj
│   ├── MarkerMind.json
│   ├── Plugin.cs                    # Main plugin entry point
│   ├── Configuration/
│   │   ├── Config.cs                # Plugin configuration
│   │   └── ConfigWindow.cs          # Settings UI
│   ├── Core/
│   │   ├── Adapter/
│   │   │   ├── BossmodBridge.cs     # Bossmod IPC integration
│   │   │   └── GameState.cs         # Player state tracking
│   │   ├── Translation/
│   │   │   ├── MechanicMapper.cs    # Maps mechanics to strategies
│   │   │   ├── RoleClassifier.cs    # Detects player role
│   │   │   └── ProgressionTracker.cs # Manages disclosure levels
│   │   ├── Predictor/
│   │   │   ├── PositionModel.cs     # Position prediction logic
│   │   │   ├── ConfidenceScorer.cs  # Confidence calculation
│   │   │   └── SafetyCalculator.cs  # Safe zone computation
│   │   └── Renderer/
│   │       ├── SplatoonIPC.cs       # Splatoon integration
│   │       ├── ElementFactory.cs    # Creates marker elements
│   │       └── FallbackRenderer.cs  # Chat fallback
│   ├── Learning/
│   │   ├── TelemetryCollector.cs    # Position/movement capture
│   │   ├── PatternExtractor.cs      # DBSCAN clustering
│   │   ├── ModelUpdater.cs          # Updates prediction models
│   │   └── DataStore.cs             # File I/O for learning data
│   └── Utils/
│       ├── Constants.cs             # Game constants
│       └── Extensions.cs            # Helper methods
├── MarkerMind.Tests/
│   └── MarkerMind.Tests.csproj
└── docs/
    └── specs/
        └── 2026-03-20-markermind-design.md
```

---

## Chunk 1: Project Skeleton

### Task 1.1: Create Project Files

**Files:**
- Create: `MarkerMind/MarkerMind.csproj`
- Create: `MarkerMind/MarkerMind.json`

- [ ] **Step 1: Create .csproj file**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>11</LangVersion>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <PropertyGroup>
    <DalamudLibPath>$(APPDATA)\XIVLauncher\addon\Hooks\dev\</DalamudLibPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="DalamudPackager" Version="2.1.13" />
    <Reference Include="Dalamud">
      <HintPath>$(DalamudLibPath)Dalamud.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="FFXIVClientStructs">
      <HintPath>$(DalamudLibPath)FFXIVClientStructs.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="ImGui.NET">
      <HintPath>$(DalamudLibPath)ImGui.NET.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Lumina">
      <HintPath>$(DalamudLibPath)Lumina.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="Lumina.Excel">
      <HintPath>$(DalamudLibPath)Lumina.Excel.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\..\ECommons\ECommons\ECommons.csproj" />
  </ItemGroup>

  <Target Name="CopyToDevPlugins" AfterTargets="Build">
    <PropertyGroup>
      <DevPluginsPath>$(APPDATA)\XIVLauncher\devPlugins\MarkerMind\</DevPluginsPath>
    </PropertyGroup>
    <MakeDir Directories="$(DevPluginsPath)" Condition="!Exists('$(DevPluginsPath)')" />
    <Copy SourceFiles="$(OutputPath)MarkerMind.dll" DestinationFiles="$(DevPluginsPath)MarkerMind.dll" />
    <Copy SourceFiles="$(OutputPath)ECommons.dll" DestinationFiles="$(DevPluginsPath)ECommons.dll" />
    <Copy SourceFiles="$(MSBuildProjectDirectory)\MarkerMind.json" DestinationFiles="$(DevPluginsPath)MarkerMind.json" />
  </Target>
</Project>
```

- [ ] **Step 2: Create manifest JSON**

```json
{
  "Author": "MarkerMind",
  "Name": "MarkerMind",
  "Punchline": "Self-learning encounter markers via Splatoon",
  "Description": "Bridges Bossmod mechanics to Splatoon markers, learning from your positioning over time.",
  "InternalName": "MarkerMind",
  "AssemblyVersion": "0.0.0.1",
  "Testing": true,
  "RepoUrl": "https://github.com/yourname/markermind",
  "ApplicableVersion": "any",
  "DalamudApiLevel": 10,
  "LoadPriority": 0,
  "IconUrl": "",
  "ImageUrls": []
}
```

- [ ] **Step 3: Commit**

```bash
git add MarkerMind/MarkerMind.csproj MarkerMind/MarkerMind.json
git commit -m "chore: add project files"
```

---

## Chunk 2: Core Plugin Skeleton

### Task 2.1: Plugin Entry Point

**Files:**
- Create: `MarkerMind/Plugin.cs`

- [ ] **Step 1: Write Plugin.cs**

```csharp
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
    private ConfigWindow configWindow = null!;
    
    // Core components
    private BossmodBridge bossmodBridge = null!;
    private GameStateTracker gameState = null!;
    private TelemetryCollector telemetry = null!;
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
        
        // UI
        configWindow = new ConfigWindow();
        Svc.PluginInterface.UiBuilder.Draw += configWindow.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += configWindow.Toggle;
        
        // Game loop
        Svc.Framework.Update += OnUpdate;
        
        Svc.Chat.Print("[MarkerMind] Loaded! Learning disabled until Bossmod detected.");
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
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Plugin.cs
git commit -m "feat: add plugin entry point"
```

---

### Task 2.2: Configuration System

**Files:**
- Create: `MarkerMind/Configuration/Config.cs`
- Create: `MarkerMind/Configuration/ConfigWindow.cs`

- [ ] **Step 1: Write Config.cs**

```csharp
using Dalamud.Configuration;
using System;

namespace MarkerMind;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // Learning settings
    public bool EnableLearning { get; set; } = true;
    public int MinSampleSize { get; set; } = 5;
    public float PositionTolerance { get; set; } = 2.0f;
    public float ClusterDistance { get; set; } = 2.0f;
    public float EmaAlpha { get; set; } = 0.3f;
    public int DataDecayDays { get; set; } = 90;
    public int TelemetryRetentionDays { get; set; } = 30;
    
    // Disclosure thresholds
    public float Level2Threshold { get; set; } = 0.5f;
    public float Level3Threshold { get; set; } = 0.7f;
    public float Level4Threshold { get; set; } = 0.85f;
    
    // Rendering settings
    public float MarkerOpacity { get; set; } = 0.8f;
    public int MaxMarkersPerMechanic { get; set; } = 3;
    public bool RequireSplatoon { get; set; } = false;
    
    public void Initialize() { }
    public void Save() => Svc.PluginInterface.SavePluginConfig(this);
}
```

- [ ] **Step 2: Write minimal ConfigWindow.cs**

```csharp
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
```

- [ ] **Step 3: Commit**

```bash
git add MarkerMind/Configuration/
git commit -m "feat: add configuration system"
```

---

## Chunk 3: Game State & Adapter Layer

### Task 3.1: Game State Tracking

**Files:**
- Create: `MarkerMind/Core/Adapter/GameState.cs`

- [ ] **Step 1: Write GameState.cs**

```csharp
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using ECommons.GameHelpers;
using System.Numerics;

namespace MarkerMind;

public enum PlayerRole
{
    Unknown,
    Tank,
    Healer,
    MeleeDPS,
    RangedDPS,
    CasterDPS
}

public class GameStateTracker : IDisposable
{
    public IPlayerCharacter? LocalPlayer { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector3 Velocity { get; private set; }
    public float Facing { get; private set; }
    public PlayerRole Role { get; private set; } = PlayerRole.Unknown;
    public uint TerritoryId { get; private set; }
    
    private Vector3 lastPosition;
    private DateTime lastUpdate = DateTime.MinValue;

    public void Update(IPlayerCharacter player)
    {
        LocalPlayer = player;
        TerritoryId = Svc.ClientState.TerritoryType;
        
        var currentPos = player.Position;
        var now = DateTime.UtcNow;
        var deltaTime = (float)(now - lastUpdate).TotalSeconds;
        
        if (deltaTime > 0)
        {
            Velocity = (currentPos - lastPosition) / deltaTime;
        }
        
        Position = currentPos;
        lastPosition = currentPos;
        lastUpdate = now;
        
        Facing = player.Rotation;
        
        if (Role == PlayerRole.Unknown)
            DetectRole(player);
    }
    
    private void DetectRole(IPlayerCharacter player)
    {
        var jobId = player.ClassJob.RowId;
        Role = jobId switch
        {
            1 or 3 or 19 or 21 or 32 or 37 => PlayerRole.Tank, // GLA, MRD, PLD, WAR, DRK, GNB
            6 or 26 or 33 => PlayerRole.Healer, // CNJ, WHM, SCH, AST, SGE
            2 or 4 or 29 or 34 or 39 => PlayerRole.MeleeDPS, // PGL, LNC, ROG, SAM, RPR, VPR
            5 or 23 or 31 or 38 => PlayerRole.RangedDPS, // ARC, BRD, MCH, DNC
            7 or 26 or 35 or 40 => PlayerRole.CasterDPS, // THM, ACN, BLM, SMN, RDM, PCT
            _ => PlayerRole.Unknown
        };
    }
    
    public void Dispose()
    {
        LocalPlayer = null;
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Core/Adapter/GameState.cs
git commit -m "feat: add game state tracking"
```

---

### Task 3.2: Bossmod Bridge

**Files:**
- Create: `MarkerMind/Core/Adapter/BossmodBridge.cs`

- [ ] **Step 1: Write BossmodBridge.cs**

```csharp
using ECommons.Events;
using ECommons.GameHelpers;
using System;
using System.Collections.Generic;

namespace MarkerMind;

public class BossmodBridge : IDisposable
{
    public bool IsBossmodAvailable { get; private set; } = false;
    public event Action<MechanicEvent>? OnMechanicStart;
    public event Action<MechanicEvent, string>? OnMechanicResolve;
    
    private Dictionary<uint, MechanicEvent> activeMechanics = new();
    
    public BossmodBridge()
    {
        // Try to subscribe to Bossmod IPC
        TrySubscribeBossmod();
        
        // Fallback: Listen to combat events
        Svc.ClientState.TerritoryChanged += OnTerritoryChanged;
    }
    
    private void TrySubscribeBossmod()
    {
        try
        {
            // Bossmod IPC subscription via ECommons
            // This is a placeholder - actual implementation depends on Bossmod's IPC
            var bossmod = Svc.PluginInterface.GetType().Assembly
                .GetType("BossMod.BossMod");
            
            if (bossmod != null)
            {
                IsBossmodAvailable = true;
                Svc.Chat.Print("[MarkerMind] Bossmod detected! Learning enabled.");
            }
        }
        catch
        {
            IsBossmodAvailable = false;
        }
    }
    
    public void Update()
    {
        if (!IsBossmodAvailable) return;
        
        // Poll Bossmod state for active mechanics
        // Placeholder: In real implementation, this would use IPC events
    }
    
    private void OnTerritoryChanged(ushort territoryId)
    {
        activeMechanics.Clear();
        TrySubscribeBossmod();
    }
    
    public void Dispose()
    {
        Svc.ClientState.TerritoryChanged -= OnTerritoryChanged;
        activeMechanics.Clear();
    }
}

public class MechanicEvent
{
    public string MechanicId { get; set; } = string.Empty;
    public string MechanicName { get; set; } = string.Empty;
    public uint BossId { get; set; }
    public Vector3? BossPosition { get; set; }
    public float Duration { get; set; }
    public float RemainingTime { get; set; }
    public MechanicType Type { get; set; }
    public List<Vector3> SafeZones { get; set; } = new();
    public List<Vector3> DangerZones { get; set; } = new();
}

public enum MechanicType
{
    Stack,
    Spread,
    Tankbuster,
    AOE,
    Dodge,
    Movement,
    Other
}
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Core/Adapter/BossmodBridge.cs
git commit -m "feat: add Bossmod bridge skeleton"
```

---

## Chunk 4: Learning System Core

### Task 4.1: Telemetry Collector

**Files:**
- Create: `MarkerMind/Learning/TelemetryCollector.cs`

- [ ] **Step 1: Write TelemetryCollector.cs**

```csharp
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
        
        timeSinceLastSample += (float)Svc.Framework.UpdateDelta.TotalSeconds;
        
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
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Learning/TelemetryCollector.cs
git commit -m "feat: add telemetry collection"
```

---

### Task 4.2: Pattern Extractor (DBSCAN)

**Files:**
- Create: `MarkerMind/Learning/PatternExtractor.cs`

- [ ] **Step 1: Write PatternExtractor.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MarkerMind;

public class PatternExtractor
{
    public List<PositionCluster> ExtractClusters(List<TelemetrySample> successfulSamples, PlayerRole role)
    {
        // Filter by role
        var roleSamples = successfulSamples.Where(s => s.Role == role).ToList();
        if (roleSamples.Count < 3) return new List<PositionCluster>();
        
        // DBSCAN parameters from config
        float eps = Plugin.Instance.Config.ClusterDistance; // 2.0 yalms default
        int minPoints = 3;
        
        var positions = roleSamples.Select(s => s.Position).ToList();
        var clusters = DBSCAN(positions, eps, minPoints);
        
        return clusters.Select(c => new PositionCluster
        {
            Centroid = CalculateCentroid(c),
            Radius = CalculateRadius(c),
            SampleCount = c.Count,
            Role = role
        }).ToList();
    }
    
    private List<List<Vector3>> DBSCAN(List<Vector3> points, float eps, int minPoints)
    {
        var clusters = new List<List<Vector3>>();
        var visited = new HashSet<int>();
        var noise = new HashSet<int>();
        
        for (int i = 0; i < points.Count; i++)
        {
            if (visited.Contains(i)) continue;
            
            visited.Add(i);
            var neighbors = GetNeighbors(points, i, eps);
            
            if (neighbors.Count < minPoints)
            {
                noise.Add(i);
            }
            else
            {
                var cluster = new List<Vector3>();
                ExpandCluster(points, i, neighbors, cluster, visited, eps, minPoints);
                if (cluster.Count > 0)
                    clusters.Add(cluster);
            }
        }
        
        return clusters;
    }
    
    private List<int> GetNeighbors(List<Vector3> points, int pointIdx, float eps)
    {
        var neighbors = new List<int>();
        for (int i = 0; i < points.Count; i++)
        {
            if (i != pointIdx && Distance2D(points[pointIdx], points[i]) <= eps)
                neighbors.Add(i);
        }
        return neighbors;
    }
    
    private void ExpandCluster(List<Vector3> points, int pointIdx, List<int> neighbors, 
        List<Vector3> cluster, HashSet<int> visited, float eps, int minPoints)
    {
        cluster.Add(points[pointIdx]);
        
        int i = 0;
        while (i < neighbors.Count)
        {
            int neighborIdx = neighbors[i];
            
            if (!visited.Contains(neighborIdx))
            {
                visited.Add(neighborIdx);
                var neighborNeighbors = GetNeighbors(points, neighborIdx, eps);
                
                if (neighborNeighbors.Count >= minPoints)
                {
                    neighbors.AddRange(neighborNeighbors.Where(n => !neighbors.Contains(n)));
                }
            }
            
            if (!cluster.Contains(points[neighborIdx]))
            {
                cluster.Add(points[neighborIdx]);
            }
            
            i++;
        }
    }
    
    private float Distance2D(Vector3 a, Vector3 b)
    {
        float dx = a.X - b.X;
        float dz = a.Z - b.Z;
        return (float)Math.Sqrt(dx * dx + dz * dz);
    }
    
    private Vector3 CalculateCentroid(List<Vector3> cluster)
    {
        return new Vector3(
            cluster.Average(p => p.X),
            cluster.Average(p => p.Y),
            cluster.Average(p => p.Z)
        );
    }
    
    private float CalculateRadius(List<Vector3> cluster)
    {
        var centroid = CalculateCentroid(cluster);
        return cluster.Max(p => Distance2D(p, centroid));
    }
}

public class PositionCluster
{
    public Vector3 Centroid { get; set; }
    public float Radius { get; set; }
    public int SampleCount { get; set; }
    public PlayerRole Role { get; set; }
}
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Learning/PatternExtractor.cs
git commit -m "feat: add DBSCAN pattern extraction"
```

---

### Task 4.3: Data Store

**Files:**
- Create: `MarkerMind/Learning/DataStore.cs`

- [ ] **Step 1: Write DataStore.cs**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarkerMind;

public class DataStore
{
    private readonly string basePath;
    private readonly JsonSerializerOptions jsonOptions;
    
    public DataStore()
    {
        basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "XIVLauncher", "pluginConfigs", "MarkerMind"
        );
        
        Directory.CreateDirectory(basePath);
        Directory.CreateDirectory(Path.Combine(basePath, "encounters"));
        Directory.CreateDirectory(Path.Combine(basePath, "telemetry"));
        
        jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        jsonOptions.Converters.Add(new Vector3JsonConverter());
    }
    
    public EncounterData? LoadEncounter(string encounterId)
    {
        string path = Path.Combine(basePath, "encounters", $"{encounterId}.json");
        if (!File.Exists(path)) return null;
        
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<EncounterData>(json, jsonOptions);
    }
    
    public void SaveEncounter(string encounterId, EncounterData data)
    {
        string path = Path.Combine(basePath, "encounters", $"{encounterId}.json");
        data.LastUpdated = DateTime.UtcNow;
        var json = JsonSerializer.Serialize(data, jsonOptions);
        File.WriteAllText(path, json);
    }
    
    public void SaveTelemetry(string encounterId, List<TelemetryEntry> entries)
    {
        string dateDir = Path.Combine(basePath, "telemetry", DateTime.Now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dateDir);
        
        string path = Path.Combine(dateDir, $"{encounterId}-{Guid.NewGuid()}.json");
        var json = JsonSerializer.Serialize(entries, jsonOptions);
        File.WriteAllText(path, json);
    }
}

public class EncounterData
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";
    
    [JsonPropertyName("lastUpdated")]
    public DateTime LastUpdated { get; set; }
    
    [JsonPropertyName("encounterId")]
    public string EncounterId { get; set; } = string.Empty;
    
    [JsonPropertyName("territoryId")]
    public uint TerritoryId { get; set; }
    
    [JsonPropertyName("mechanics")]
    public Dictionary<string, MechanicData> Mechanics { get; set; } = new();
}

public class MechanicData
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("observations")]
    public int Observations { get; set; }
    
    [JsonPropertyName("successRate")]
    public float SuccessRate { get; set; }
    
    [JsonPropertyName("clusters")]
    public List<ClusterData> Clusters { get; set; } = new();
    
    [JsonPropertyName("avoidZones")]
    public List<ZoneData> AvoidZones { get; set; } = new();
    
    [JsonPropertyName("confidence")]
    public float Confidence { get; set; }
    
    [JsonPropertyName("lastSeen")]
    public DateTime LastSeen { get; set; }
}

public class ClusterData
{
    [JsonPropertyName("centroid")]
    public Vector3 Centroid { get; set; }
    
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
    
    [JsonPropertyName("sampleCount")]
    public int SampleCount { get; set; }
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = "unknown";
}

public class ZoneData
{
    [JsonPropertyName("x")]
    public float X { get; set; }
    
    [JsonPropertyName("y")]
    public float Y { get; set; }
    
    [JsonPropertyName("z")]
    public float Z { get; set; }
    
    [JsonPropertyName("radius")]
    public float Radius { get; set; }
}

public class TelemetryEntry
{
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }
    
    [JsonPropertyName("mechanicId")]
    public string MechanicId { get; set; } = string.Empty;
    
    [JsonPropertyName("position")]
    public Vector3 Position { get; set; }
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = "unknown";
    
    [JsonPropertyName("outcome")]
    public string Outcome { get; set; } = string.Empty;
}

public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return new Vector3(
            root.GetProperty("x").GetSingle(),
            root.GetProperty("y").GetSingle(),
            root.GetProperty("z").GetSingle()
        );
    }
    
    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Learning/DataStore.cs
git commit -m "feat: add data persistence layer"
```

---

## Chunk 5: Learning Engine

### Task 5.1: Learning Engine

**Files:**
- Create: `MarkerMind/Learning/LearningEngine.cs`

- [ ] **Step 1: Write LearningEngine.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MarkerMind;

public class LearningEngine : IDisposable
{
    private readonly DataStore dataStore;
    private readonly PatternExtractor patternExtractor;
    
    // Current encounter state
    private string? currentEncounterId;
    private Dictionary<string, MechanicData> mechanicCache = new();
    
    // Active mechanic tracking
    private string? activeMechanicId;
    private Vector3? markerPosition;
    
    public LearningEngine()
    {
        dataStore = new DataStore();
        patternExtractor = new PatternExtractor();
        
        // Subscribe to telemetry events
        if (Plugin.Instance?.telemetry != null)
        {
            Plugin.Instance.telemetry.OnMechanicComplete += OnMechanicComplete;
        }
    }
    
    public void Update()
    {
        // Check for encounter changes
        var territoryId = Svc.ClientState.TerritoryType;
        var newEncounterId = $"{territoryId}";
        
        if (newEncounterId != currentEncounterId)
        {
            LoadEncounter(newEncounterId);
        }
    }
    
    private void LoadEncounter(string encounterId)
    {
        currentEncounterId = encounterId;
        var data = dataStore.LoadEncounter(encounterId);
        
        if (data != null)
        {
            mechanicCache = data.Mechanics;
        }
        else
        {
            mechanicCache = new Dictionary<string, MechanicData>();
        }
    }
    
    public void StartMechanic(string mechanicId, string mechanicName)
    {
        activeMechanicId = mechanicId;
        
        // Get or create mechanic data
        if (!mechanicCache.TryGetValue(mechanicId, out var mechanicData))
        {
            mechanicData = new MechanicData
            {
                Name = mechanicName,
                Observations = 0,
                SuccessRate = 0,
                Confidence = 0
            };
            mechanicCache[mechanicId] = mechanicData;
        }
        
        // Calculate predicted position
        markerPosition = PredictPosition(mechanicData);
        
        // Start telemetry capture
        Plugin.Instance?.telemetry?.StartCapture(mechanicId);
    }
    
    public void EndMechanic(string outcome)
    {
        if (activeMechanicId == null) return;
        
        Plugin.Instance?.telemetry?.StopCapture(outcome);
        
        activeMechanicId = null;
        markerPosition = null;
    }
    
    private Vector3? PredictPosition(MechanicData mechanicData)
    {
        var role = Plugin.Instance?.gameState?.Role ?? PlayerRole.Unknown;
        var roleStr = role.ToString().ToLowerInvariant();
        
        // Find clusters for current role
        var roleClusters = mechanicData.Clusters
            .Where(c => c.Role.Equals(roleStr, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (roleClusters.Count == 0)
        {
            // Fallback: use any cluster
            roleClusters = mechanicData.Clusters.ToList();
        }
        
        if (roleClusters.Count == 0) return null;
        
        // Return centroid of largest cluster
        var bestCluster = roleClusters.OrderByDescending(c => c.SampleCount).First();
        return bestCluster.Centroid;
    }
    
    private void OnMechanicComplete(string mechanicId, List<TelemetrySample> samples, string outcome)
    {
        if (!mechanicCache.TryGetValue(mechanicId, out var mechanicData))
            return;
        
        // Update observations
        mechanicData.Observations++;
        
        // Update success rate
        float alpha = Plugin.Instance?.Config.EmaAlpha ?? 0.3f;
        float outcomeValue = outcome == "survived" ? 1.0f : 0.0f;
        mechanicData.SuccessRate = (mechanicData.SuccessRate * (1 - alpha)) + (outcomeValue * alpha);
        
        // Extract new clusters if survived
        if (outcome == "survived" && samples.Count > 0)
        {
            var role = samples.First().Role;
            var newClusters = patternExtractor.ExtractClusters(samples, role);
            
            // Merge with existing clusters
            foreach (var newCluster in newClusters)
            {
                var existing = mechanicData.Clusters
                    .FirstOrDefault(c => c.Role.Equals(role.ToString(), StringComparison.OrdinalIgnoreCase));
                
                if (existing != null)
                {
                    // Update existing cluster with EMA
                    existing.Centroid = new Vector3(
                        existing.Centroid.X * (1 - alpha) + newCluster.Centroid.X * alpha,
                        existing.Centroid.Y * (1 - alpha) + newCluster.Centroid.Y * alpha,
                        existing.Centroid.Z * (1 - alpha) + newCluster.Centroid.Z * alpha
                    );
                    existing.SampleCount += newCluster.SampleCount;
                    existing.Radius = Math.Max(existing.Radius, newCluster.Radius);
                }
                else
                {
                    mechanicData.Clusters.Add(new ClusterData
                    {
                        Centroid = newCluster.Centroid,
                        Radius = newCluster.Radius,
                        SampleCount = newCluster.SampleCount,
                        Role = role.ToString().ToLowerInvariant()
                    });
                }
            }
        }
        
        // Update confidence
        UpdateConfidence(mechanicData);
        
        // Save to disk
        mechanicData.LastSeen = DateTime.UtcNow;
        if (currentEncounterId != null)
        {
            dataStore.SaveEncounter(currentEncounterId, new EncounterData
            {
                EncounterId = currentEncounterId,
                TerritoryId = Svc.ClientState.TerritoryType,
                Mechanics = mechanicCache
            });
        }
    }
    
    private void UpdateConfidence(MechanicData mechanicData)
    {
        int minSamples = Plugin.Instance?.Config.MinSampleSize ?? 5;
        
        if (mechanicData.Observations < minSamples)
        {
            mechanicData.Confidence = 0;
            return;
        }
        
        // Confidence based on success rate and sample count
        float sampleConfidence = Math.Min(1.0f, (float)mechanicData.Observations / (minSamples * 2));
        float successConfidence = mechanicData.SuccessRate;
        
        // Cluster quality (tighter clusters = higher confidence)
        float clusterConfidence = 0.5f;
        if (mechanicData.Clusters.Count > 0)
        {
            var avgRadius = mechanicData.Clusters.Average(c => c.Radius);
            clusterConfidence = Math.Min(1.0f, 1.0f / (avgRadius + 0.1f));
        }
        
        mechanicData.Confidence = (sampleConfidence + successConfidence + clusterConfidence) / 3.0f;
    }
    
    public int GetDisclosureLevel(string mechanicId)
    {
        if (!mechanicCache.TryGetValue(mechanicId, out var mechanicData))
            return 1;
        
        float confidence = mechanicData.Confidence;
        
        if (confidence >= (Plugin.Instance?.Config.Level4Threshold ?? 0.85f))
            return 4;
        if (confidence >= (Plugin.Instance?.Config.Level3Threshold ?? 0.7f))
            return 3;
        if (confidence >= (Plugin.Instance?.Config.Level2Threshold ?? 0.5f))
            return 2;
        
        return 1;
    }
    
    public void Dispose()
    {
        if (Plugin.Instance?.telemetry != null)
        {
            Plugin.Instance.telemetry.OnMechanicComplete -= OnMechanicComplete;
        }
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Learning/LearningEngine.cs
git commit -m "feat: add learning engine with confidence scoring"
```

---

## Chunk 6: Rendering Layer

### Task 6.1: Splatoon IPC Integration

**Files:**
- Create: `MarkerMind/Core/Renderer/SplatoonIPC.cs`

- [ ] **Step 1: Write SplatoonIPC.cs**

```csharp
using System;
using System.Collections.Generic;
using System.Numerics;

namespace MarkerMind;

public class SplatoonRenderer : IDisposable
{
    private bool isSplatoonAvailable = false;
    private List<ActiveElement> activeElements = new();
    
    public SplatoonRenderer()
    {
        CheckSplatoonAvailability();
    }
    
    private void CheckSplatoonAvailability()
    {
        try
        {
            // Check if Splatoon IPC is available
            // This uses ECommons IPC
            isSplatoonAvailable = Svc.PluginInterface.GetType().Assembly
                .GetType("Splatoon.Splatoon") != null;
            
            if (isSplatoonAvailable)
            {
                Svc.Chat.Print("[MarkerMind] Splatoon detected! Markers enabled.");
            }
        }
        catch
        {
            isSplatoonAvailable = false;
        }
    }
    
    public void RenderMarker(string mechanicId, Vector3 position, int disclosureLevel, PlayerRole role)
    {
        if (!isSplatoonAvailable)
        {
            RenderChatFallback(mechanicId, position, disclosureLevel);
            return;
        }
        
        // Clear previous elements for this mechanic
        RemoveElementsForMechanic(mechanicId);
        
        // Render based on disclosure level
        switch (disclosureLevel)
        {
            case 1:
                RenderDangerZone(mechanicId, position);
                break;
            case 2:
                RenderDangerZone(mechanicId, position);
                RenderSafeSpot(mechanicId, position);
                break;
            case 3:
            case 4:
                RenderDangerZone(mechanicId, position);
                RenderSafeSpot(mechanicId, position);
                RenderMovementPath(mechanicId, position);
                break;
        }
    }
    
    private void RenderDangerZone(string mechanicId, Vector3 position)
    {
        // Create danger zone circle (red)
        // This would use Splatoon IPC to inject elements
        // Placeholder implementation
        activeElements.Add(new ActiveElement
        {
            MechanicId = mechanicId,
            Type = ElementType.DangerZone,
            Position = position
        });
    }
    
    private void RenderSafeSpot(string mechanicId, Vector3 position)
    {
        // Create safe spot marker (green)
        activeElements.Add(new ActiveElement
        {
            MechanicId = mechanicId,
            Type = ElementType.SafeSpot,
            Position = position
        });
    }
    
    private void RenderMovementPath(string mechanicId, Vector3 position)
    {
        // Create movement arrow/path
        if (Plugin.Instance?.gameState?.LocalPlayer != null)
        {
            var playerPos = Plugin.Instance.gameState.Position;
            activeElements.Add(new ActiveElement
            {
                MechanicId = mechanicId,
                Type = ElementType.Path,
                StartPosition = playerPos,
                EndPosition = position
            });
        }
    }
    
    private void RenderChatFallback(string mechanicId, Vector3 position, int disclosureLevel)
    {
        // Fallback: Print to chat
        var levelText = disclosureLevel switch
        {
            1 => "Danger detected",
            2 => "Safe spot suggested",
            3 => "Follow the path",
            4 => "Position for your role",
            _ => "Mechanic detected"
        };
        
        Svc.Chat.Print($"[MarkerMind] {levelText}: {mechanicId}");
    }
    
    private void RemoveElementsForMechanic(string mechanicId)
    {
        activeElements.RemoveAll(e => e.MechanicId == mechanicId);
    }
    
    public void ClearAll()
    {
        activeElements.Clear();
    }
    
    public void Dispose()
    {
        ClearAll();
    }
}

public class ActiveElement
{
    public string MechanicId { get; set; } = string.Empty;
    public ElementType Type { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 StartPosition { get; set; }
    public Vector3 EndPosition { get; set; }
}

public enum ElementType
{
    DangerZone,
    SafeSpot,
    Path
}
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Core/Renderer/SplatoonIPC.cs
git commit -m "feat: add Splatoon rendering integration"
```

---

## Chunk 7: Wire Everything Together

### Task 7.1: Update Plugin.cs with Full Integration

**Files:**
- Modify: `MarkerMind/Plugin.cs`

- [ ] **Step 1: Update Plugin.cs**

```csharp
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
```

- [ ] **Step 2: Commit**

```bash
git add MarkerMind/Plugin.cs
git commit -m "feat: wire up all components"
```

---

## Chunk 8: Build & Test

### Task 8.1: Build Project

**Files:**
- All

- [ ] **Step 1: Build the solution**

```bash
cd MarkerMind
dotnet build
```

Expected: Build succeeds with no errors

- [ ] **Step 2: Verify files copied to devPlugins**

Check that these files exist:
- `%APPDATA%\XIVLauncher\devPlugins\MarkerMind\MarkerMind.dll`
- `%APPDATA%\XIVLauncher\devPlugins\MarkerMind\MarkerMind.json`
- `%APPDATA%\XIVLauncher\devPlugins\MarkerMind\ECommons.dll`

- [ ] **Step 3: Commit**

```bash
git commit -m "chore: initial build"
```

---

## Post-Implementation Checklist

- [ ] Plugin loads without errors
- [ ] Config window opens via `/xlsettings` → MarkerMind
- [ ] Settings save and persist
- [ ] Learning data directory created on first run
- [ ] Bossmod detection works (if installed)
- [ ] Splatoon detection works (if installed)
- [ ] Graceful fallback when dependencies missing

---

**Plan complete and saved to `MarkerMind/docs/plans/2026-03-20-markermind-implementation.md`. Ready to execute?**
