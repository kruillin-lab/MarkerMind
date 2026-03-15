# TankCooldownHelper Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a Dalamud plugin that monitors party-wide damage/healing and warns tanks when mitigation is needed

**Architecture:** Core plugin with ECommons integration, combat event buffer for rolling window calculations, modular ImGui UI with draggable components, and future IPC hooks for ParseLord3 integration

**Tech Stack:** C# 13, .NET 10, Dalamud API 14, ECommons 3.1.0.13, FFXIVClientStructs, ImGui

---

## File Structure

```
TankCooldownHelper/
├── TankCooldownHelper.csproj              # Project file
├── TankCooldownHelper.json                # Plugin manifest
├── TankCooldownHelper.cs                  # Plugin entry point
├── Configuration.cs                       # Settings persistence
├── CLAUDE.md                              # Project documentation
├── Core/
│   ├── CombatEventBuffer.cs              # Rolling window data storage
│   ├── DangerCalculator.cs               # Ratio/threshold calculations
│   ├── DamageTracker.cs                  # Incoming DPS calculation
│   ├── HealingTracker.cs                 # Incoming HPS calculation
│   └── Predictor.cs                      # HP projection engine
├── Data/
│   ├── CombatEvent.cs                    # Event data structure
│   ├── PartyMemberState.cs               # Per-member tracking
│   └── DangerLevel.cs                    # Enum definitions
└── UI/
    ├── MainWindow.cs                     # Primary ImGui window
    ├── DangerMeterModule.cs              # DPS/HPS bar component
    ├── PredictiveTimelineModule.cs       # HP projection graph
    └── SettingsWindow.cs                 # Configuration UI
```

---

## Chunk 1: Project Setup and Foundation

### Task 1.1: Create Project Structure

**Files:**
- Create: `TankCooldownHelper/TankCooldownHelper.csproj`
- Create: `TankCooldownHelper/TankCooldownHelper.json`
- Create: `TankCooldownHelper/TankCooldownHelper.cs`
- Create: `TankCooldownHelper/CLAUDE.md`

- [ ] **Step 1: Create project file**

```xml
<Project Sdk="Dalamud.NET.Sdk/14.0.1">
  <PropertyGroup>
    <AssemblyName>TankCooldownHelper</AssemblyName>
    <Version>1.0.0.0</Version>
    <Description>Real-time damage vs healing tracker with cooldown suggestions</Description>
    <Authors>YourName</Authors>
    <TargetFramework>net10.0-windows10.0.26100.0</TargetFramework>
    <PlatformTarget>x64</PlatformTarget>
    <Nullable>enable</Nullable>
    <LangVersion>13</LangVersion>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
    <OutputPath>$(AppData)\XIVLauncher\devPlugins\TankCooldownHelper\</OutputPath>
    <DalamudPackagerEnabled>false</DalamudPackagerEnabled>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="ECommons" Version="3.1.0.13" />
  </ItemGroup>

  <Target Name="CopyToDevPlugins" AfterTargets="Build">
    <PropertyGroup>
      <DevPluginsPath>$(AppData)\XIVLauncher\devPlugins\TankCooldownHelper\</DevPluginsPath>
    </PropertyGroup>
    <MakeDir Directories="$(DevPluginsPath)" Condition="!Exists('$(DevPluginsPath)')" />
    <Copy SourceFiles="$(OutputPath)TankCooldownHelper.dll" DestinationFiles="$(DevPluginsPath)TankCooldownHelper.dll" />
    <Copy SourceFiles="$(OutputPath)TankCooldownHelper.json" DestinationFiles="$(DevPluginsPath)TankCooldownHelper.json" />
    <Copy SourceFiles="$(OutputPath)ECommons.dll" DestinationFiles="$(DevPluginsPath)ECommons.dll" />
  </Target>
</Project>
```

- [ ] **Step 2: Create plugin manifest**

```json
{
  "Author": "YourName",
  "Name": "Tank Cooldown Helper",
  "InternalName": "TankCooldownHelper",
  "AssemblyVersion": "1.0.0.0",
  "Description": "Real-time damage vs healing tracker with cooldown suggestions for tanks and healers",
  "ApplicableVersion": "any",
  "DalamudApiLevel": 14,
  "LoadPriority": 0,
  "Tags": ["tank", "cooldown", "mitigation", "healing", "utility"],
  "CategoryTags": ["utility"],
  "IsHide": false,
  "IsTestingExclusive": false,
  "IconUrl": "",
  "Punchline": "Know when to use your cooldowns before it's too late"
}
```

- [ ] **Step 3: Create plugin entry point**

```csharp
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;

namespace TankCooldownHelper;

public class TankCooldownHelperPlugin : IDalamudPlugin
{
    public string Name => "Tank Cooldown Helper";
    
    private readonly MainWindow _mainWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly Configuration _configuration;
    private readonly CombatEventBuffer _combatBuffer;
    private readonly DangerCalculator _dangerCalculator;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;
    private readonly Predictor _predictor;

    public TankCooldownHelperPlugin(IDalamudPluginInterface pluginInterface)
    {
        // Initialize ECommons
        ECommonsMain.Init(pluginInterface, this,
            ECommons.Module.DalamudReflector,
            ECommons.Module.ObjectFunctions);

        // Load configuration
        _configuration = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        
        // Initialize core systems
        _combatBuffer = new CombatEventBuffer(_configuration.TimeWindowSeconds);
        _dangerCalculator = new DangerCalculator(_configuration);
        _damageTracker = new DamageTracker(_combatBuffer);
        _healingTracker = new HealingTracker(_combatBuffer);
        _predictor = new Predictor(_dangerCalculator);
        
        // Initialize UI
        _mainWindow = new MainWindow(_configuration, _dangerCalculator, _damageTracker, _healingTracker, _predictor);
        _settingsWindow = new SettingsWindow(_configuration);
        
        // Register command
        Svc.Commands.AddHandler("/tch", new Dalamud.Game.Command.CommandInfo(OnCommand)
        {
            HelpMessage = "Toggle Tank Cooldown Helper window",
            ShowInHelp = true
        });
        
        // Hook into Framework.Update
        Svc.Framework.Update += OnFrameworkUpdate;
        
        Svc.Log.Info("TankCooldownHelper initialized");
    }

    private void OnCommand(string command, string args)
    {
        if (args.Equals("config", StringComparison.OrdinalIgnoreCase))
        {
            _settingsWindow.Toggle();
        }
        else
        {
            _mainWindow.Toggle();
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Svc.ClientState.IsLoggedIn) return;
        if (Svc.ClientState.LocalPlayer is not { } player) return;
        
        _combatBuffer.PruneOldEvents();
        _mainWindow.Update();
    }

    public void Dispose()
    {
        Svc.Framework.Update -= OnFrameworkUpdate;
        Svc.Commands.RemoveHandler("/tch");
        
        _mainWindow.Dispose();
        _settingsWindow.Dispose();
        
        ECommonsMain.Dispose();
    }
}
```

- [ ] **Step 4: Create CLAUDE.md**

```markdown
# CLAUDE.md - TankCooldownHelper

This file provides guidance for working with the TankCooldownHelper codebase.

## Project Overview

TankCooldownHelper is a Dalamud plugin for FFXIV that monitors party-wide damage and healing,
providing real-time warnings when mitigation is needed.

## Build Commands

```bash
# Build the plugin (auto-deploys to devPlugins)
dotnet build TankCooldownHelper.csproj -c Release

# Build only (no deploy)
dotnet build TankCooldownHelper.csproj
```

## Project Structure

| Folder | Purpose |
|--------|---------|
| `Core/` | Calculation engines and data processing |
| `Data/` | Data structures and enums |
| `UI/` | ImGui windows and components |

## Key Classes

- `TankCooldownHelperPlugin` - Plugin entry point
- `CombatEventBuffer` - Rolling window for combat events
- `DangerCalculator` - Computes danger ratios and levels
- `DamageTracker` / `HealingTracker` - Aggregate DPS/HPS
- `Predictor` - HP projection calculations
- `MainWindow` - Primary UI window

## Architecture Patterns

Uses ECommons pattern (Svc static accessor) rather than [PluginService] injection.
All game state access happens in Framework.Update for thread safety.

## Dependencies

- Dalamud API 14
- ECommons 3.1.0.13
- FFXIVClientStructs (via Dalamud)
```

- [ ] **Step 5: Test build**

Run: `dotnet build TankCooldownHelper/TankCooldownHelper.csproj`
Expected: Build succeeds (may have warnings about missing classes)

- [ ] **Step 6: Commit**

```bash
git add TankCooldownHelper/
git commit -m "feat: initialize TankCooldownHelper project structure"
```

---

## Chunk 2: Data Structures and Configuration

### Task 2.1: Create Core Data Types

**Files:**
- Create: `TankCooldownHelper/Data/CombatEvent.cs`
- Create: `TankCooldownHelper/Data/DangerLevel.cs`
- Create: `TankCooldownHelper/Data/PartyMemberState.cs`

- [ ] **Step 1: Create CombatEvent record**

```csharp
namespace TankCooldownHelper.Data;

public readonly record struct CombatEvent
{
    public readonly uint TargetId;
    public readonly uint SourceId;
    public readonly uint ActionId;
    public readonly float Amount;
    public readonly CombatEventType Type;
    public readonly DateTime Timestamp;

    public CombatEvent(uint targetId, uint sourceId, uint actionId, float amount, CombatEventType type)
    {
        TargetId = targetId;
        SourceId = sourceId;
        ActionId = actionId;
        Amount = amount;
        Type = type;
        Timestamp = DateTime.UtcNow;
    }
}

public enum CombatEventType
{
    Damage,
    Healing,
    ShieldApplied,
    ShieldConsumed
}
```

- [ ] **Step 2: Create DangerLevel enum**

```csharp
namespace TankCooldownHelper.Data;

public enum DangerLevel
{
    Safe,      // Ratio < 1.0 - Healing covers damage
    Warning,   // Ratio 1.0-1.5 - Damage exceeds healing moderately
    Critical,  // Ratio 1.5-2.0 - Damage significantly exceeds healing
    Emergency  // Ratio > 2.0 - Imminent death without mitigation
}
```

- [ ] **Step 3: Create PartyMemberState**

```csharp
namespace TankCooldownHelper.Data;

public class PartyMemberState
{
    public uint ObjectId { get; set; }
    public string Name { get; set; } = "";
    public uint CurrentHp { get; set; }
    public uint MaxHp { get; set; }
    public float HpPercent => MaxHp > 0 ? (float)CurrentHp / MaxHp * 100 : 0;
    
    // Calculated metrics
    public float IncomingDps { get; set; }
    public float IncomingHps { get; set; }
    public float DangerRatio { get; set; }
    public DangerLevel DangerLevel { get; set; }
    public float? SecondsUntilDeath { get; set; }
    
    // Job info for icon display
    public uint JobId { get; set; }
    public bool IsTank { get; set; }
    public bool IsHealer { get; set; }
}
```

- [ ] **Step 4: Commit**

```bash
git add TankCooldownHelper/Data/
git commit -m "feat: add core data structures"
```

### Task 2.2: Create Configuration

**Files:**
- Create: `TankCooldownHelper/Configuration.cs`

- [ ] **Step 1: Implement Configuration class**

```csharp
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace TankCooldownHelper;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // Time Window Settings
    public float TimeWindowSeconds { get; set; } = 5.0f;
    public float MinTimeWindow { get; set; } = 1.0f;
    public float MaxTimeWindow { get; set; } = 15.0f;
    
    // Thresholds (DPS/HPS ratio)
    public float WarningThreshold { get; set; } = 1.0f;
    public float CriticalThreshold { get; set; } = 1.5f;
    public float EmergencyThreshold { get; set; } = 2.0f;
    
    // Display Settings
    public bool ShowDangerMeter { get; set; } = true;
    public bool ShowPredictiveTimeline { get; set; } = true;
    public bool ShowCooldownSuggestions { get; set; } = true;
    public bool ShowPartyBreakdown { get; set; } = false;
    public bool ShowNetDamageCounter { get; set; } = true;
    
    // Party Tracking
    public bool TrackFullParty { get; set; } = true;
    public bool HighlightMostEndangered { get; set; } = true;
    
    // Window Settings
    public bool LockWindowPosition { get; set; } = false;
    public float WindowOpacity { get; set; } = 0.95f;
    public bool ShowInCombatOnly { get; set; } = false;
    
    // ParseLord3 Integration (future)
    public bool EnablePL3Integration { get; set; } = false;
    
    public void Save(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.SavePluginConfig(this);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/Configuration.cs
git commit -m "feat: add configuration system"
```

---

## Chunk 3: Core Calculation Engine

### Task 3.1: Create CombatEventBuffer

**Files:**
- Create: `TankCooldownHelper/Core/CombatEventBuffer.cs`
- Test: `TankCooldownHelper.Tests/CombatEventBufferTests.cs` (if test project exists)

- [ ] **Step 1: Implement CombatEventBuffer**

```csharp
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class CombatEventBuffer
{
    private readonly Queue<CombatEvent> _events = new();
    private readonly float _windowSeconds;
    private readonly object _lock = new();

    public CombatEventBuffer(float windowSeconds)
    {
        _windowSeconds = windowSeconds;
    }

    public void UpdateWindow(float newWindowSeconds)
    {
        lock (_lock)
        {
            _windowSeconds = newWindowSeconds;
            PruneOldEvents();
        }
    }

    public void AddEvent(CombatEvent evt)
    {
        lock (_lock)
        {
            _events.Enqueue(evt);
        }
    }

    public void PruneOldEvents()
    {
        lock (_lock)
        {
            var cutoff = DateTime.UtcNow.AddSeconds(-_windowSeconds);
            while (_events.Count > 0 && _events.Peek().Timestamp < cutoff)
            {
                _events.Dequeue();
            }
        }
    }

    public float GetTotalDamage(uint targetId)
    {
        lock (_lock)
        {
            return _events
                .Where(e => e.TargetId == targetId && e.Type == CombatEventType.Damage)
                .Sum(e => e.Amount);
        }
    }

    public float GetTotalHealing(uint targetId)
    {
        lock (_lock)
        {
            return _events
                .Where(e => e.TargetId == targetId && e.Type == CombatEventType.Healing)
                .Sum(e => e.Amount);
        }
    }

    public IEnumerable<uint> GetTrackedTargetIds()
    {
        lock (_lock)
        {
            return _events.Select(e => e.TargetId).Distinct().ToList();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _events.Clear();
        }
    }

    public int Count => _events.Count;
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/Core/CombatEventBuffer.cs
git commit -m "feat: add combat event buffer with rolling window"
```

### Task 3.2: Create DangerCalculator

**Files:**
- Create: `TankCooldownHelper/Core/DangerCalculator.cs`

- [ ] **Step 1: Implement DangerCalculator**

```csharp
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class DangerCalculator
{
    private readonly Configuration _config;

    public DangerCalculator(Configuration config)
    {
        _config = config;
    }

    public DangerLevel CalculateDangerLevel(float dps, float hps)
    {
        var ratio = CalculateDangerRatio(dps, hps);
        
        if (ratio >= _config.EmergencyThreshold)
            return DangerLevel.Emergency;
        if (ratio >= _config.CriticalThreshold)
            return DangerLevel.Critical;
        if (ratio >= _config.WarningThreshold)
            return DangerLevel.Warning;
        
        return DangerLevel.Safe;
    }

    public float CalculateDangerRatio(float dps, float hps)
    {
        // Avoid division by zero - if no healing, ratio is effectively infinite
        if (hps <= 0)
            return dps > 0 ? 99.9f : 0f;
        
        return dps / hps;
    }

    public float? CalculateDeathTimer(float currentHp, float dps, float hps)
    {
        var netDps = dps - hps;
        
        // If healing exceeds damage, no death timer
        if (netDps <= 0)
            return null;
        
        // Avoid division by very small numbers
        if (netDps < 1)
            return null;
        
        var seconds = currentHp / netDps;
        
        // Cap at reasonable max to avoid overflow display
        return seconds > 999 ? 999 : seconds;
    }

    public uint GetColorForDangerLevel(DangerLevel level)
    {
        return level switch
        {
            DangerLevel.Safe => 0xFF4CAF50,      // Green
            DangerLevel.Warning => 0xFFFFC107,   // Yellow/Amber
            DangerLevel.Critical => 0xFFFF9800,  // Orange
            DangerLevel.Emergency => 0xFFF44336, // Red
            _ => 0xFF888888
        };
    }

    public string GetDangerLevelText(DangerLevel level)
    {
        return level switch
        {
            DangerLevel.Safe => "Safe",
            DangerLevel.Warning => "Warning",
            DangerLevel.Critical => "Critical",
            DangerLevel.Emergency => "EMERGENCY",
            _ => "Unknown"
        };
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/Core/DangerCalculator.cs
git commit -m "feat: add danger calculator with ratio and death timer"
```

### Task 3.3: Create DamageTracker and HealingTracker

**Files:**
- Create: `TankCooldownHelper/Core/DamageTracker.cs`
- Create: `TankCooldownHelper/Core/HealingTracker.cs`

- [ ] **Step 1: Implement DamageTracker**

```csharp
using ECommons.DalamudServices;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class DamageTracker
{
    private readonly CombatEventBuffer _buffer;

    public DamageTracker(CombatEventBuffer buffer)
    {
        _buffer = buffer;
    }

    public float GetIncomingDps(uint targetId)
    {
        var totalDamage = _buffer.GetTotalDamage(targetId);
        // Use actual time span of events, or configured window
        var windowSeconds = GetEffectiveWindowSeconds();
        return totalDamage / windowSeconds;
    }

    public float GetPartyIncomingDps()
    {
        if (!Svc.ClientState.IsLoggedIn) return 0;
        
        var totalDps = 0f;
        var targets = _buffer.GetTrackedTargetIds();
        
        foreach (var targetId in targets)
        {
            totalDps += GetIncomingDps(targetId);
        }
        
        return totalDps;
    }

    private float GetEffectiveWindowSeconds()
    {
        // For MVP, use a fixed calculation based on buffer size
        // In future, track actual time span of oldest event
        return 5.0f;
    }

    public void SimulateDamage(uint targetId, float amount)
    {
        // For testing - add mock damage event
        _buffer.AddEvent(new CombatEvent(targetId, 0, 0, amount, CombatEventType.Damage));
    }
}
```

- [ ] **Step 2: Implement HealingTracker**

```csharp
using ECommons.DalamudServices;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class HealingTracker
{
    private readonly CombatEventBuffer _buffer;

    public HealingTracker(CombatEventBuffer buffer)
    {
        _buffer = buffer;
    }

    public float GetIncomingHps(uint targetId)
    {
        var totalHealing = _buffer.GetTotalHealing(targetId);
        var windowSeconds = GetEffectiveWindowSeconds();
        return totalHealing / windowSeconds;
    }

    public float GetPartyIncomingHps()
    {
        if (!Svc.ClientState.IsLoggedIn) return 0;
        
        var totalHps = 0f;
        var targets = _buffer.GetTrackedTargetIds();
        
        foreach (var targetId in targets)
        {
            totalHps += GetIncomingHps(targetId);
        }
        
        return totalHps;
    }

    private float GetEffectiveWindowSeconds()
    {
        return 5.0f;
    }

    public void SimulateHealing(uint targetId, float amount)
    {
        // For testing - add mock healing event
        _buffer.AddEvent(new CombatEvent(targetId, 0, 0, amount, CombatEventType.Healing));
    }
}
```

- [ ] **Step 3: Commit**

```bash
git add TankCooldownHelper/Core/DamageTracker.cs TankCooldownHelper/Core/HealingTracker.cs
git commit -m "feat: add damage and healing trackers"
```

### Task 3.4: Create Predictor

**Files:**
- Create: `TankCooldownHelper/Core/Predictor.cs`

- [ ] **Step 1: Implement Predictor**

```csharp
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class Predictor
{
    private readonly DangerCalculator _dangerCalculator;

    public Predictor(DangerCalculator dangerCalculator)
    {
        _dangerCalculator = dangerCalculator;
    }

    public float? PredictTimeOfDeath(uint currentHp, float dps, float hps)
    {
        return _dangerCalculator.CalculateDeathTimer(currentHp, dps, hps);
    }

    public float[] ProjectHpTimeline(uint currentHp, float maxHp, float dps, float hps, int secondsToProject)
    {
        var timeline = new float[secondsToProject + 1];
        var netDps = dps - hps;
        
        timeline[0] = currentHp;
        
        for (int i = 1; i <= secondsToProject; i++)
        {
            var projectedHp = currentHp - (netDps * i);
            timeline[i] = Math.Max(0, Math.Min(projectedHp, maxHp));
        }
        
        return timeline;
    }

    public DangerLevel PredictDangerState(float dps, float hps)
    {
        return _dangerCalculator.CalculateDangerLevel(dps, hps);
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/Core/Predictor.cs
git commit -m "feat: add HP predictor with timeline projection"
```

---

## Chunk 4: Combat Event Collection

### Task 4.1: Hook Combat Events

**Files:**
- Modify: `TankCooldownHelper/TankCooldownHelper.cs`
- Create: `TankCooldownHelper/Core/CombatEventCollector.cs`

- [ ] **Step 1: Create CombatEventCollector**

```csharp
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.Core;

public class CombatEventCollector : IDisposable
{
    private readonly CombatEventBuffer _buffer;

    public CombatEventCollector(CombatEventBuffer buffer)
    {
        _buffer = buffer;
        // Hook into combat events via FFXIVClientStructs
        // For MVP, we'll poll from Framework.Update instead of direct hooks
    }

    public void PollCombatEvents()
    {
        // In a full implementation, this would hook into:
        // - ActionEffect packets for damage/healing
        // - StatusEffect updates for shields
        // For MVP, we'll rely on polling HP changes
    }

    public void AddDamageEvent(uint targetId, uint sourceId, uint actionId, float amount)
    {
        _buffer.AddEvent(new CombatEvent(targetId, sourceId, actionId, amount, CombatEventType.Damage));
    }

    public void AddHealingEvent(uint targetId, uint sourceId, uint actionId, float amount)
    {
        _buffer.AddEvent(new CombatEvent(targetId, sourceId, actionId, amount, CombatEventType.Healing));
    }

    public void Dispose()
    {
        // Cleanup hooks if any
    }
}
```

- [ ] **Step 2: Update plugin to use collector**

Modify `TankCooldownHelper.cs` to add combat event collection:

```csharp
// Add to class fields:
private readonly CombatEventCollector _combatEventCollector;

// In constructor, after creating buffer:
_combatEventCollector = new CombatEventCollector(_combatBuffer);

// In OnFrameworkUpdate, add:
_combatEventCollector.PollCombatEvents();

// In Dispose:
_combatEventCollector.Dispose();
```

- [ ] **Step 3: Commit**

```bash
git add TankCooldownHelper/Core/CombatEventCollector.cs
git commit -m "feat: add combat event collector skeleton"
```

---

## Chunk 5: UI Components

### Task 5.1: Create MainWindow Base

**Files:**
- Create: `TankCooldownHelper/UI/MainWindow.cs`

- [ ] **Step 1: Implement MainWindow**

```csharp
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ImGuiNET;
using TankCooldownHelper.Core;

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
        _predictiveTimelineModule = new PredictiveTimelineModule(config, predictor);
        
        IsOpen = true;
        Size = new System.Numerics.Vector2(400, 300);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        if (_config.ShowInCombatOnly && !IsInCombat())
        {
            ImGui.TextDisabled("Show in combat only is enabled...");
            return;
        }

        // Set window opacity
        ImGui.SetNextWindowBgAlpha(_config.WindowOpacity);

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
    }

    private void DrawNetDamageCounter()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return;

        var dps = _damageTracker.GetIncomingDps(player.GameObjectId);
        var hps = _healingTracker.GetIncomingHps(player.GameObjectId);
        var netDps = dps - hps;
        
        var color = netDps > 0 ? 0xFFF44336 : 0xFF4CAF50;
        
        ImGui.TextColored(ImGui.ColorConvertU32ToFloat4(color), $"Net Damage: {netDps:F0}/s");
        ImGui.SameLine();
        ImGui.TextDisabled($"(DPS: {dps:F0} | HPS: {hps:F0})");
    }

    private bool IsInCombat()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return false;
        
        // Check combat status via StatusList or Character flags
        return player.StatusList.Any(s => s.StatusId == 418); // 418 = In Combat
    }

    public void Update()
    {
        // Called every Framework.Update - can be used for animations or smooth updates
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Dispose()
    {
        _dangerMeterModule.Dispose();
        _predictiveTimelineModule.Dispose();
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/UI/MainWindow.cs
git commit -m "feat: add main window with module system"
```

### Task 5.2: Create DangerMeterModule

**Files:**
- Create: `TankCooldownHelper/UI/DangerMeterModule.cs`

- [ ] **Step 1: Implement DangerMeterModule**

```csharp
using ECommons.DalamudServices;
using ImGuiNET;
using System.Numerics;
using TankCooldownHelper.Core;
using TankCooldownHelper.Data;

namespace TankCooldownHelper.UI;

public class DangerMeterModule : IDisposable
{
    private readonly Configuration _config;
    private readonly DangerCalculator _calculator;
    private readonly DamageTracker _damageTracker;
    private readonly HealingTracker _healingTracker;

    public DangerMeterModule(
        Configuration config,
        DangerCalculator calculator,
        DamageTracker damageTracker,
        HealingTracker healingTracker)
    {
        _config = config;
        _calculator = calculator;
        _damageTracker = damageTracker;
        _healingTracker = healingTracker;
    }

    public void Draw()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null)
        {
            ImGui.TextDisabled("Waiting for player data...");
            return;
        }

        var targetId = player.GameObjectId;
        var dps = _damageTracker.GetIncomingDps((uint)targetId);
        var hps = _healingTracker.GetIncomingHps((uint)targetId);
        var ratio = _calculator.CalculateDangerRatio(dps, hps);
        var level = _calculator.CalculateDangerLevel(dps, hps);
        
        DrawDangerBar(dps, hps, ratio, level);
        DrawStats(dps, hps, ratio, level);
    }

    private void DrawDangerBar(float dps, float hps, float ratio, DangerLevel level)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();
        var windowWidth = ImGui.GetContentRegionAvail().X;
        var barHeight = 20f;
        
        // Background
        var bgMin = cursorPos;
        var bgMax = new Vector2(cursorPos.X + windowWidth, cursorPos.Y + barHeight);
        drawList.AddRectFilled(bgMin, bgMax, ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)), 4f);
        
        // Calculate bar widths
        var total = dps + hps;
        if (total > 0)
        {
            var dpsWidth = (dps / total) * windowWidth;
            var hpsWidth = (hps / total) * windowWidth;
            
            // DPS bar (red)
            if (dps > 0)
            {
                var dpsMax = new Vector2(cursorPos.X + dpsWidth, cursorPos.Y + barHeight);
                drawList.AddRectFilled(cursorPos, dpsMax, 0xFFF44336, 4f);
            }
            
            // HPS bar (green)
            if (hps > 0)
            {
                var hpsMin = new Vector2(cursorPos.X + dpsWidth, cursorPos.Y);
                var hpsMax = new Vector2(hpsMin.X + hpsWidth, cursorPos.Y + barHeight);
                drawList.AddRectFilled(hpsMin, hpsMax, 0xFF4CAF50, 4f);
            }
        }
        
        // Border
        drawList.AddRect(bgMin, bgMax, ImGui.ColorConvertFloat4ToU32(new Vector4(0.4f, 0.4f, 0.4f, 1f)), 4f);
        
        ImGui.Dummy(new Vector2(0, barHeight + 4));
    }

    private void DrawStats(float dps, float hps, float ratio, DangerLevel level)
    {
        var colorU32 = _calculator.GetColorForDangerLevel(level);
        var colorVec = ImGui.ColorConvertU32ToFloat4(colorU32);
        
        ImGui.Text("DPS: "); ImGui.SameLine();
        ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), $"{dps:F0}");
        
        ImGui.SameLine();
        ImGui.TextDisabled(" | ");
        ImGui.SameLine();
        
        ImGui.Text("HPS: "); ImGui.SameLine();
        ImGui.TextColored(new Vector4(0.3f, 1, 0.3f, 1), $"{hps:F0}");
        
        ImGui.SameLine();
        ImGui.TextDisabled(" | ");
        ImGui.SameLine();
        
        ImGui.Text("Ratio: "); ImGui.SameLine();
        ImGui.TextColored(colorVec, $"{ratio:F2}x");
        
        if (level != DangerLevel.Safe)
        {
            ImGui.SameLine();
            ImGui.TextDisabled(" | ");
            ImGui.SameLine();
            
            var levelText = _calculator.GetDangerLevelText(level);
            ImGui.TextColored(colorVec, levelText);
        }
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/UI/DangerMeterModule.cs
git commit -m "feat: add danger meter UI module with bar and stats"
```

### Task 5.3: Create PredictiveTimelineModule

**Files:**
- Create: `TankCooldownHelper/UI/PredictiveTimelineModule.cs`

- [ ] **Step 1: Implement PredictiveTimelineModule**

```csharp
using ECommons.DalamudServices;
using ImGuiNET;
using System.Numerics;
using TankCooldownHelper.Core;

namespace TankCooldownHelper.UI;

public class PredictiveTimelineModule : IDisposable
{
    private readonly Configuration _config;
    private readonly Predictor _predictor;

    public PredictiveTimelineModule(Configuration config, Predictor predictor)
    {
        _config = config;
        _predictor = predictor;
    }

    public void Draw()
    {
        var player = Svc.ClientState.LocalPlayer;
        if (player == null) return;

        var currentHp = player.CurrentHp;
        var maxHp = player.MaxHp;
        
        // Get damage/healing from combat tracking
        // For MVP, use placeholder values
        var dps = 1000f;  // TODO: Get from DamageTracker
        var hps = 800f;   // TODO: Get from HealingTracker
        
        var deathTimer = _predictor.PredictTimeOfDeath(currentHp, dps, hps);
        
        DrawTimeline(currentHp, maxHp, dps, hps, deathTimer);
        DrawDeathWarning(deathTimer);
    }

    private void DrawTimeline(uint currentHp, uint maxHp, float dps, float hps, float? deathTimer)
    {
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();
        var width = ImGui.GetContentRegionAvail().X;
        var height = 60f;
        
        // Draw background grid
        for (int i = 0; i <= 4; i++)
        {
            var y = cursorPos.Y + (height / 4) * i;
            drawList.AddLine(
                new Vector2(cursorPos.X, y),
                new Vector2(cursorPos.X + width, y),
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 0.3f)),
                1f
            );
        }
        
        // Draw HP projection line
        var projectionSeconds = 10;
        var timeline = _predictor.ProjectHpTimeline(currentHp, maxHp, dps, hps, projectionSeconds);
        
        var points = new Vector2[projectionSeconds + 1];
        for (int i = 0; i <= projectionSeconds; i++)
        {
            var x = cursorPos.X + (width / projectionSeconds) * i;
            var hpPercent = timeline[i] / maxHp;
            var y = cursorPos.Y + height - (hpPercent * height);
            points[i] = new Vector2(x, y);
        }
        
        // Draw line
        for (int i = 0; i < projectionSeconds; i++)
        {
            var color = points[i + 1].Y > points[i].Y ? 0xFF4CAF50 : 0xFFF44336;
            drawList.AddLine(points[i], points[i + 1], color, 2f);
        }
        
        // Draw current HP marker
        var currentY = cursorPos.Y + height - ((float)currentHp / maxHp * height);
        drawList.AddCircleFilled(new Vector2(cursorPos.X, currentY), 4f, 0xFFFFFFFF);
        
        // Draw death marker if applicable
        if (deathTimer.HasValue && deathTimer.Value <= projectionSeconds)
        {
            var deathX = cursorPos.X + (width / projectionSeconds) * deathTimer.Value;
            drawList.AddCircleFilled(new Vector2(deathX, cursorPos.Y + height), 6f, 0xFFF44336);
            
            var text = $"Death in {deathTimer.Value:F1}s";
            var textSize = ImGui.CalcTextSize(text);
            drawList.AddText(
                new Vector2(deathX - textSize.X / 2, cursorPos.Y + height - 20),
                0xFFF44336,
                text
            );
        }
        
        // Labels
        ImGui.SetCursorScreenPos(new Vector2(cursorPos.X, cursorPos.Y + height + 4));
        ImGui.TextDisabled("Now");
        
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 60);
        ImGui.TextDisabled($"+{projectionSeconds}s");
        
        ImGui.Dummy(new Vector2(0, 4));
    }

    private void DrawDeathWarning(float? deathTimer)
    {
        if (!deathTimer.HasValue) return;
        
        var timer = deathTimer.Value;
        var color = timer < 5 ? new Vector4(1, 0.2f, 0.2f, 1) :
                    timer < 10 ? new Vector4(1, 0.6f, 0.2f, 1) :
                    new Vector4(0.8f, 0.8f, 0.8f, 1);
        
        ImGui.TextColored(color, $"⚠ Death projected in {timer:F1} seconds");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/UI/PredictiveTimelineModule.cs
git commit -m "feat: add predictive timeline module with HP projection"
```

### Task 5.4: Create SettingsWindow

**Files:**
- Create: `TankCooldownHelper/UI/SettingsWindow.cs`

- [ ] **Step 1: Implement SettingsWindow**

```csharp
using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ImGuiNET;

namespace TankCooldownHelper.UI;

public class SettingsWindow : Window, IDisposable
{
    private readonly Configuration _config;

    public SettingsWindow(Configuration config) 
        : base("Tank Cooldown Helper - Settings###SettingsWindow")
    {
        _config = config;
        IsOpen = false;
        Size = new System.Numerics.Vector2(500, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        if (ImGui.BeginTabBar("SettingsTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Thresholds"))
            {
                DrawThresholdsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Display"))
            {
                DrawDisplayTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        
        if (ImGui.Button("Save"))
        {
            _config.Save(Svc.PluginInterface);
            ImGui.CloseCurrentPopup();
        }
        
        ImGui.SameLine();
        
        if (ImGui.Button("Cancel"))
        {
            IsOpen = false;
        }
    }

    private void DrawGeneralTab()
    {
        ImGui.Text("Time Window Settings");
        ImGui.Separator();
        
        var windowSeconds = _config.TimeWindowSeconds;
        if (ImGui.SliderFloat("Time Window (seconds)", ref windowSeconds, _config.MinTimeWindow, _config.MaxTimeWindow, "%.1f"))
        {
            _config.TimeWindowSeconds = windowSeconds;
        }
        ImGui.TextDisabled("How far back to look for damage/healing calculations");
        
        ImGui.Spacing();
        
        ImGui.Text("Party Tracking");
        ImGui.Separator();
        
        var trackParty = _config.TrackFullParty;
        if (ImGui.Checkbox("Track Full Party", ref trackParty))
        {
            _config.TrackFullParty = trackParty;
        }
        
        var highlightEndangered = _config.HighlightMostEndangered;
        if (ImGui.Checkbox("Highlight Most Endangered Member", ref highlightEndangered))
        {
            _config.HighlightMostEndangered = highlightEndangered;
        }
        
        ImGui.Spacing();
        
        ImGui.Text("Combat Display");
        ImGui.Separator();
        
        var showInCombatOnly = _config.ShowInCombatOnly;
        if (ImGui.Checkbox("Show Only In Combat", ref showInCombatOnly))
        {
            _config.ShowInCombatOnly = showInCombatOnly;
        }
    }

    private void DrawThresholdsTab()
    {
        ImGui.Text("Danger Ratio Thresholds (DPS / HPS)");
        ImGui.Separator();
        
        var warning = _config.WarningThreshold;
        if (ImGui.SliderFloat("Warning Threshold", ref warning, 0.5f, 3.0f, "%.2fx"))
        {
            _config.WarningThreshold = Math.Min(warning, _config.CriticalThreshold);
        }
        ImGui.TextDisabled("Yellow warning when damage exceeds healing by this ratio");
        
        ImGui.Spacing();
        
        var critical = _config.CriticalThreshold;
        if (ImGui.SliderFloat("Critical Threshold", ref critical, 1.0f, 4.0f, "%.2fx"))
        {
            _config.CriticalThreshold = Math.Max(critical, _config.WarningThreshold);
        }
        ImGui.TextDisabled("Orange alert when ratio reaches this level");
        
        ImGui.Spacing();
        
        var emergency = _config.EmergencyThreshold;
        if (ImGui.SliderFloat("Emergency Threshold", ref emergency, 1.5f, 5.0f, "%.2fx"))
        {
            _config.EmergencyThreshold = Math.Max(emergency, _config.CriticalThreshold);
        }
        ImGui.TextDisabled("Red emergency when ratio reaches this level");
    }

    private void DrawDisplayTab()
    {
        ImGui.Text("Visible Modules");
        ImGui.Separator();
        
        var showDangerMeter = _config.ShowDangerMeter;
        if (ImGui.Checkbox("Danger Meter (Bar + Stats)", ref showDangerMeter))
        {
            _config.ShowDangerMeter = showDangerMeter;
        }
        
        var showTimeline = _config.ShowPredictiveTimeline;
        if (ImGui.Checkbox("Predictive Timeline (HP Projection)", ref showTimeline))
        {
            _config.ShowPredictiveTimeline = showTimeline;
        }
        
        var showNetDamage = _config.ShowNetDamageCounter;
        if (ImGui.Checkbox("Net Damage Counter", ref showNetDamage))
        {
            _config.ShowNetDamageCounter = showNetDamage;
        }
        
        var showPartyBreakdown = _config.ShowPartyBreakdown;
        if (ImGui.Checkbox("Party Breakdown (Full Party List)", ref showPartyBreakdown))
        {
            _config.ShowPartyBreakdown = showPartyBreakdown;
        }
        
        ImGui.Spacing();
        
        ImGui.Text("Window Settings");
        ImGui.Separator();
        
        var lockPosition = _config.LockWindowPosition;
        if (ImGui.Checkbox("Lock Window Position", ref lockPosition))
        {
            _config.LockWindowPosition = lockPosition;
        }
        
        var opacity = _config.WindowOpacity;
        if (ImGui.SliderFloat("Window Opacity", ref opacity, 0.5f, 1.0f, "%.2f"))
        {
            _config.WindowOpacity = opacity;
        }
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add TankCooldownHelper/UI/SettingsWindow.cs
git commit -m "feat: add settings window with tabs for configuration"
```

---

## Chunk 6: Integration and Testing

### Task 6.1: Fix Plugin Compilation

**Files:**
- Modify: `TankCooldownHelper/TankCooldownHelper.cs`

- [ ] **Step 1: Add missing imports and fix compilation errors**

Update `TankCooldownHelper.cs` to add missing imports and ensure all dependencies are initialized:

```csharp
// Add at top:
using TankCooldownHelper.UI;
using TankCooldownHelper.Core;
using System.Linq;
```

- [ ] **Step 2: Build and test**

Run: `dotnet build TankCooldownHelper/TankCooldownHelper.csproj -c Release`
Expected: Build succeeds with no errors

- [ ] **Step 3: Commit**

```bash
git add TankCooldownHelper/
git commit -m "fix: resolve compilation errors and add missing imports"
```

### Task 6.2: Add Combat Event Collection

**Files:**
- Modify: `TankCooldownHelper/Core/CombatEventCollector.cs`
- Modify: `TankCooldownHelper/TankCooldownHelper.cs`

- [ ] **Step 1: Implement HP-based damage detection**

For MVP, we'll detect damage by monitoring HP changes:

```csharp
// Add to CombatEventCollector:
private readonly Dictionary<uint, uint> _lastHpValues = new();

public void DetectDamageFromHpChanges()
{
    var player = Svc.ClientState.LocalPlayer;
    if (player == null) return;
    
    // Check player HP
    CheckHpChange((uint)player.GameObjectId, player.CurrentHp, player.MaxHp);
    
    // Check party members
    foreach (var member in Svc.Party)
    {
        if (member?.GameObject is not { } gameObject) continue;
        if (gameObject is not Dalamud.Game.ClientState.Objects.Types.ICharacter character) continue;
        
        CheckHpChange((uint)gameObject.GameObjectId, character.CurrentHp, character.MaxHp);
    }
}

private void CheckHpChange(uint objectId, uint currentHp, uint maxHp)
{
    if (_lastHpValues.TryGetValue(objectId, out var lastHp))
    {
        var hpDiff = (int)currentHp - (int)lastHp;
        
        if (hpDiff < 0)
        {
            // HP went down - damage taken
            AddDamageEvent(objectId, 0, 0, Math.Abs(hpDiff));
        }
        else if (hpDiff > 0)
        {
            // HP went up - healing received
            AddHealingEvent(objectId, 0, 0, hpDiff);
        }
    }
    
    _lastHpValues[objectId] = currentHp;
}
```

- [ ] **Step 2: Update plugin to call HP detection**

Modify `OnFrameworkUpdate` in `TankCooldownHelper.cs`:

```csharp
private void OnFrameworkUpdate(IFramework framework)
{
    if (!Svc.ClientState.IsLoggedIn) return;
    if (Svc.ClientState.LocalPlayer is not { } player) return;
    
    _combatBuffer.PruneOldEvents();
    _combatEventCollector.DetectDamageFromHpChanges();
    _mainWindow.Update();
}
```

- [ ] **Step 3: Commit**

```bash
git add TankCooldownHelper/
git commit -m "feat: add HP-change based combat event detection"
```

### Task 6.3: Final Testing and Verification

**Files:**
- All files

- [ ] **Step 1: Verify full build**

Run: `dotnet build TankCooldownHelper/TankCooldownHelper.csproj -c Release`
Expected: Build succeeds

- [ ] **Step 2: Verify files are deployed**

Check: `%APPDATA%\XIVLauncher\devPlugins\TankCooldownHelper\`
Expected: Contains `.dll` and `.json` files

- [ ] **Step 3: Document installation**

Add to README.md (create if doesn't exist):

```markdown
# TankCooldownHelper

## Installation
1. Build the project: `dotnet build -c Release`
2. Plugin auto-deploys to `devPlugins` folder
3. Enable in Dalamud plugin installer

## Usage
- Type `/tch` to toggle the main window
- Type `/tch config` to open settings
- Window shows real-time DPS/HPS ratio
- Yellow/Orange/Red warnings when damage exceeds healing

## Configuration
- Time Window: How far back to calculate (1-15 seconds)
- Thresholds: Adjust ratio levels for each warning color
- Display: Toggle individual UI modules
```

- [ ] **Step 4: Final commit**

```bash
git add .
git commit -m "feat: complete MVP implementation of TankCooldownHelper"
```

---

## Post-MVP Enhancements (Future Work)

These are not part of the current plan but documented for future reference:

### Phase 2: Enhanced Combat Tracking
- [ ] Hook into action effects for accurate damage/healing values
- [ ] Track shield applications and absorptions
- [ ] Support for mitigation tracking (Rampart, Sentinel, etc.)
- [ ] Encounter-specific cooldown suggestions

### Phase 3: ParseLord3 Integration
- [ ] Export danger state via Dalamud IPC
- [ ] Shared library for core calculations
- [ ] PL3 automated reactions based on danger level

### Phase 4: Advanced Features
- [ ] Voice alerts for critical danger
- [ ] Historical analysis and reports
- [ ] Machine learning for encounter pattern recognition
- [ ] Customizable alert sounds

---

## Success Criteria

MVP is complete when:
- ✅ Plugin loads without errors in Dalamud
- ✅ `/tch` command toggles the window
- ✅ Window displays DPS/HPS bar
- ✅ Color changes based on ratio thresholds
- ✅ Settings window opens with `/tch config`
- ✅ Configuration persists between sessions
- ✅ Tracks HP changes to detect damage/healing
- ✅ No crashes during typical gameplay

---

*Plan Version: 1.0*  
*Created: 2026-03-12*  
*Status: Ready for implementation*
