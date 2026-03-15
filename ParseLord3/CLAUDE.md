# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ParseLord3** is a Dalamud plugin for FFXIV that provides automated combat rotations. It combines RSR-style high-performance action execution with granular configuration UI. The plugin targets FFXIV Patch 7.4 / Dalamud API 14 / .NET 10.

## Build Commands

```bash
# Build the entire solution
dotnet build RotationSolver.sln -c Release

# Build just the main plugin
dotnet build RotationSolver/RotationSolver.csproj -c Release

# Build the Basic library (rotation primitives)
dotnet build RotationSolver.Basic/RotationSolver.Basic.csproj -c Release

# Use the build script (wrapper for Release build)
./build.sh
```

The PostBuild target in `RotationSolver.csproj` automatically copies compiled DLLs to `%APPDATA%\XIVLauncher\devPlugins\ParseLord3\`:
- `ParseLord3.dll`
- `ParseLord3.json` (manifest)
- `RotationSolver.Basic.dll`
- `ECommons.dll`

## Test Commands

```bash
# Run all tests
dotnet test RotationSolver.Tests/RotationSolver.Tests.csproj

# Run tests with verbosity
dotnet test RotationSolver.Tests/RotationSolver.Tests.csproj -v n

# Run a specific test class
dotnet test RotationSolver.Tests --filter "ConfigurationHelperTests"

# Run a specific test method
dotnet test RotationSolver.Tests --filter "ToVirtual_ReturnsCorrectVirtualKey"
```

Tests use xUnit with Moq for mocking. The test project references `RotationSolver.Basic` and Dalamud DLLs from the local Dalamud hooks directory.

## Project Structure

The solution contains 6 projects:

| Project | Purpose |
|---------|---------|
| `RotationSolver` | Main plugin entry point, UI windows, IPC, updaters |
| `RotationSolver.Basic` | Core interfaces, data models, targeting + rotation helpers (packaged as `ParseLord.Basic`) |
| `RotationSolver.SourceGenerators` | Roslyn analyzers and code generators |
| `RotationSolver.GameData` | Excel sheet data extraction tools |
| `RotationSolver.DocumentationGenerator` | Wiki documentation generator (dotnet tool) |
| `RotationSolver.Tests` | xUnit test suite |

## Architecture

### Core Flow
1. **RotationSolverPlugin.cs** - Entry point. Initializes ECommons, loads config, creates windows, starts updaters
2. **MajorUpdater** - Hooked into `Framework.Update`, calls `RotationUpdater` and `ActionUpdater`
3. **RotationUpdater** - Determines next action via rotation logic or custom triggers
4. **ActionUpdater** - Executes actions through `ActionManager` with weaving checks
5. **ActionManager** - Wraps `FFXIVClientStructs.ActionManager` for unsafe action execution

### Rotation Decision Flow
```
Framework.Update → MajorUpdater.Update
  → RotationUpdater.Update
    → Check CustomTriggers (reaction-style triggers with target chains)
    → Check ActionQueueManager (prioritized action queue)
    → If no queued action: call ICustomRotation.GetNextAction()
      → Emergency (self-heals)
      → Opener sequence
      → oGCD weaving (if CanWeave())
      → AoE or Single Target GCDs
```

### Key Patterns

**Dalamud DI** (`RotationSolverPlugin.cs`): Services accessed via `Svc` static class from ECommons (`Svc.ClientState`, `Svc.Framework`, etc.). This is the modern ECommons pattern rather than `[PluginService]` attributes.

**Configuration** (`Configs.cs`, `OtherConfiguration.cs`): Split between:
- `Configs` - Main settings (saved to `RotationSolver.json`)
- `OtherConfiguration` - Separate JSON files for hostiles, priorities, rotation records

**Action Execution** (`ActionUpdater.cs`, `ActionQueueManager.cs`):
- Actions are queued via `ActionQueueManager.PushAction()` with priority
- `ActionUpdater` processes the queue on each frame
- Weaving logic in `ActionBasicInfo` - checks GCD remaining, animation lock, and queue window

**Rotation Interface** (`ICustomRotation` in `RotationSolver.Basic`):
```csharp
public interface ICustomRotation
{
    bool TryGetNextAction(out IAction? action);
}
```

Rotations inherit from base classes in `Rotations/Basic/` (e.g., `DragoonRotation`, `PaladinRotation`).

**Target Resolution** (`TargetUpdater.cs`, `TargetFilter.cs`):
- `TargetType` enum defines targeting modes (Hostile, Move, Dispel, etc.)
- `TargetFilter` applies priority logic (lowest HP, highest max HP, etc.)

## Key Files

| File | Purpose |
|------|---------|
| `RotationSolver/RotationSolverPlugin.cs` | Plugin entry point, initialization |
| `RotationSolver/Updaters/MajorUpdater.cs` | Main game loop hook |
| `RotationSolver/Updaters/RotationUpdater.cs` | Rotation decision logic |
| `RotationSolver/Updaters/ActionUpdater.cs` | Action execution |
| `RotationSolver.Basic/Rotations/CustomRotation_Invoke.cs` | Rotation base class methods |
| `RotationSolver.Basic/Helpers/ActionManagerHelper.cs` | FFXIVClientStructs wrapper |
| `RotationSolver.Basic/Actions/BaseAction.cs` | Action definition and settings |
| `RotationSolver.Basic/Configuration/Configs.cs` | Main configuration class |
| `RotationSolver.Basic/DataCenter.cs` | Runtime state cache |
| `RotationSolver/UI/RotationConfigWindow.cs` | Main configuration UI |

## Important Implementation Details

- **Assembly name**: `ParseLord3` (not `RotationSolver`)
- **Manifest**: `manifest.json` (copied to `ParseLord3.json` on build)
- **Commands**: `/parselord`, `/pl` (legacy `/rotation` also registered)
- **Dalamud API Level**: 14
- **.NET Version**: 10.0
- **Dalamud SDK**: `Dalamud.NET.Sdk/14.0.1`

## Dependencies

- **ECommons** (3.1.0.13) - Dalamud utility library, provides `Svc` service accessor
- **Dalamud** - Plugin framework (from Dalamud hooks)
- **FFXIVClientStructs** - Direct game memory access
- **Lumina** - Game data access
- **Newtonsoft.Json** - JSON serialization

## Development Notes

- Uses nullable reference types (`<Nullable>enable</Nullable>`)
- Uses implicit usings defined in `.csproj` files
- Unsafe blocks allowed for FFXIVClientStructs access
- Source generators in `RotationSolver.SourceGenerators` generate boilerplate for configurations
- The `DalamudPackager` is disabled for local dev (`<DalamudPackagerEnabled>false</DalamudPackagerEnabled>`)

## Resources

From README.md:
- **Dalamud**: https://github.com/goatcorp/Dalamud
- **Dalamud Docs**: https://dalamud.dev/
- **Dalamud Discord**: https://discord.com/channels/581875019861328007/1450366875190952198
