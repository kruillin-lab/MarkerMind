# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**ActionStacksEX** is a Dalamud plugin for FFXIV that provides enhanced battle system quality-of-life features. It's an enhanced version of ReActionEX with more features, decoupled from ParseLord. The plugin targets Dalamud API 14 / .NET 10.

## Build Commands

```bash
# Build (Release)
dotnet build ActionStacksEX.csproj -c Release

# Build with auto-deploy to devPlugins
# The PostBuild target automatically copies to %APPDATA%\XIVLauncher\devPlugins\ActionStacksEX\
```

## Architecture

### Core Components

**ActionStacksEX.cs** - Main plugin entry point
- Initializes ECommons modules (DalamudReflector, ObjectFunctions)
- Loads action/status Excel sheets from Lumina
- Handles commands: `/actionstacksex`, `/ax`, `/asmacroqueue`, `/asmqueue`
- Manages macro queue patching

**ActionStackManager.cs** - Core action interception logic
- Hooks into `ActionManager.UseAction` via Hypostasis
- Pre/Post action event delegates for stack processing
- Modifier key detection for stack triggering
- Ground target queuing support

**Configuration.cs** - Plugin settings
- Action stack definitions with serialization
- Export/Import with compressed JSON
- Feature toggles for all modules

**PluginUI.cs** - ImGui configuration interface
- Stack creation and management UI
- Real-time action editing
- Import/export functionality

### Module System

Located in `Modules/` folder:
- **ActionStacks.cs** - Core stack execution logic
- **AutoCastCancel.cs** - Automatic cast cancellation
- **AutoDismount.cs** - Auto-dismount on action
- **AutoFocusTarget.cs** - Target focusing
- **AutoRefocusTarget.cs** - Target refocusing
- **AutoTarget.cs** - Automatic target acquisition
- **CameraRelativeActions.cs** - Camera-relative dash/directional abilities
- **Decombos.cs** - Breaks combo actions into individual buttons
- **EnhancedAutoFaceTarget.cs** - Improved auto-facing
- **FrameAlignment.cs** - Frame timing adjustments
- **QueueAdjustments.cs** - Action queue timing tweaks
- **QueueMore.cs** - Extended queuing capabilities
- **SpellAutoAttacks.cs** - Auto-attack during spell casts
- **TurboHotbars.cs** - Rapid hotbar input

### Key Patterns

**ECommons Integration** - Uses ECommons library for common Dalamud patterns:
```csharp
ECommonsMain.Init(pluginInterface, this,
    ECommons.Module.DalamudReflector,
    ECommons.Module.ObjectFunctions);
```

**Hypostasis Game Structures** - Uses unsafe FFXIVClientStructs wrappers:
```csharp
using ActionManager = Hypostasis.Game.Structures.ActionManager;
```

**Action Stack Definition**:
```csharp
public class ActionStack
{
    public string Name;
    public List<Action> Actions;        // Trigger actions
    public List<ActionStackItem> Items;   // Stack execution items
    public uint ModifierKeys;             // Key modifiers (Shift/Ctrl/Alt)
    public bool BlockOriginal;              // Block original action
    public bool CheckRange;                 // Range validation
    public bool CheckCooldown;              // Cooldown validation
}
```

**Modifier Keys Bitmask**:
- Bit 0: Shift
- Bit 1: Ctrl
- Bit 2: Alt
- Bit 3: Exact match flag

### Action Interception Flow

```
UseAction called
  → PreUseAction event
  → GetAdjustedActionId
  → PreActionStack event
  → Check configured stacks
    → Match action ID against stack.Actions
    → Check modifier keys
    → Execute stack.Items in sequence
  → PostActionStack event
  → Original action (if not blocked)
  → PostUseAction event
```

## Configuration System

**ActionStacks** are user-configurable action sequences:
- Trigger action(s) define what activates the stack
- Stack items execute in sequence on the selected target
- HP ratio conditions for healing actions
- Status application/removal checks

**Export/Import Format**:
- Compressed JSON with custom serialization binder
- Prefix: `ASEX_`
- Shareable between players

## Key Files

| File | Purpose |
|------|---------|
| `ActionStacksEX.cs` | Plugin entry point |
| `ActionStackManager.cs` | Core action hook logic |
| `Configuration.cs` | Settings and serialization |
| `PluginUI.cs` | Configuration UI |
| `Game.cs` | Game state and patches |
| `PronounManager.cs` | Target pronoun resolution (<t>, <me>, etc.) |
| `Extensions.cs` | Utility extensions |
| `JobRole.cs` | Job role definitions |

## Commands

- `/actionstacksex` or `/ax` - Toggle config window
- `/asmacroqueue` or `/asmqueue [on|off]` - Toggle macro queueing

## Dependencies

- **Dalamud API 14** - Plugin framework
- **ECommons 3.1.0.13** - Dalamud utility library
- **Hypostasis** - FFXIVClientStructs wrappers
- **Lumina** - Game data access
- **Newtonsoft.Json** - JSON serialization

## Important Notes

- Uses unsafe blocks for FFXIVClientStructs access
- Patches `/ac` command queueing when macros run
- Supports ground target action queuing
- Can decombo abilities (Meditation, Bunshin, etc.)
- Action IDs reference Lumina Excel sheets
- Target resolution via PronounManager (<t>, <me>, <f>, etc.)

## Project Metadata

- **AssemblyName**: ActionStacksEX
- **InternalName**: ActionStacksEX
- **Version**: 1.0.0.0
- **DalamudApiLevel**: 14
- **TargetFramework**: net10.0-windows10.0.26100.0
