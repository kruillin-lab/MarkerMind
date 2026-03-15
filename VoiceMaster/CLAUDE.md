# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

**VoiceMaster** is a Dalamud plugin for FFXIV that provides Text-to-Speech (TTS) capabilities for in-game dialogue. It's a rebrand of Echokraut with self-hosted TTS service support. The plugin makes FFXIV's story and NPC dialogue accessible via spoken audio.

## Build Commands

```bash
# Build (Release)
dotnet build VoiceMaster.csproj -c Release

# Build with auto-deploy to devPlugins
# The PostBuild target automatically copies to %APPDATA%\XIVLauncher\devPlugins\VoiceMaster\
```

## Architecture

### Core Components

**Plugin.cs** - Main plugin entry point
- Registers plugin services via `[PluginService]` attributes
- Initializes helper systems (LipSync, Sound, Addon helpers)
- Manages window system (Config, Alltalk, FirstTime, DialogExtraOptions)
- NPC ignore list management (session and instance level)
- Commands: `/vm`, `/vmtalk`, `/vmbtalk`, `/vmbubble`, `/vmchat`

**Configuration.cs** - Plugin settings (in `DataClasses/`)
- Backend selection (AllTalk, etc.)
- Voice configuration and mapping
- Volume, speed, pitch settings
- Dialogue source toggles (Talk, BattleTalk, Bubbles, Chat)
- NPC ignore lists

**ConfigWindow.cs** - Main configuration UI (in `Windows/`)
- Backend connection settings
- Voice assignment interface
- General settings (volume, speed, etc.)
- NPC management tab

### Helper Systems

Located in `Helper/` folder:

**Audio/Playback:**
- **SoundHelper.cs** - Audio playback management using ManagedBass
- **LipSyncHelper.cs** - Character lip synchronization
- **bass.dll** - Native audio library

**Addon Integration:**
- **AddonTalkHelper.cs** - Standard dialogue window (AddonTalk)
- **AddonBattleTalkHelper.cs** - Battle dialogue (AddonBattleTalk)
- **AddonSelectStringHelper.cs** - Player choice dialogue
- **AddonCutSceneSelectStringHelper.cs** - Cutscene choices
- **AddonBubbleHelper.cs** - Chat bubble text

**Data Processing:**
- **ChatTalkHelper.cs** - Chat message TTS
- **DataHelper/** - Data processing utilities
- **API/** - Backend API communication
- **Addons/** - Dalamud addon event handling

**Functional Utilities:**
- **Functional/** - Functional programming utilities
- **Data/** - Static data and constants

### Backend System

Located in `Backends/` folder:
- **AllTalk/** - AllTalk TTS backend integration
- Backend interface for extensible TTS services

### Data Classes

Located in `DataClasses/` folder:
- Player/NPC voice mapping data
- Configuration structures
- Serializable settings

### Enums

Located in `Enums/` folder:
- Backend types
- Voice categories
- Dialogue source types
- Configuration flags

### Dialogue Gating Architecture

VoiceMaster uses a sophisticated state-based gating system:

**NPC Dialogue (Talk Window):**
- Fires on `PostDraw` (every frame while visible)
- State tracked as `(Speaker, NormalizedText)` tuple
- Only speaks when state changes (prevents spam)
- Resets when Talk window closes
- Text normalization: punctuation standardization (emdashes, ellipsis, etc.)

**Player Dialogue (SelectString):**
- Fires on `PreFinalize` (when selection confirmed)
- Always speaks when selected (fully repeatable)
- No suppression between selections

**Battle Talk:**
- Similar to NPC dialogue but for battle text
- Separate visibility tracking

**Bubbles:**
- Chat bubble text detection
- Distance-based filtering

### Key Patterns

**Dalamud Service Injection**:
```csharp
[PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
[PluginService] internal static IClientState ClientState { get; private set; } = null!;
[PluginService] internal static IDataManager DataManager { get; private set; } = null!;
[PluginService] internal static IFramework Framework { get; private set; } = null!;
[PluginService] internal static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
// ... etc
```

**Addon Event Subscription**:
```csharp
AddonLifecycle.RegisterListener(AddonEvent.PostDraw, "Talk", OnTalkPostDraw);
AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "SelectString", OnSelectStringPreFinalize);
```

**NPC Ignore System**:
```csharp
// Session-level ignore (persists until plugin reload)
public static readonly HashSet<string> IgnoredNpcSession = new(StringComparer.OrdinalIgnoreCase);

// Instance-level ignore (persists until zone change/instance reset)
public static readonly HashSet<string> IgnoredNpcInstance = new(StringComparer.OrdinalIgnoreCase);
```

**Text Processing Pipeline**:
```
Raw Text → Normalization → State Check → TTS Request → Audio Playback → LipSync
```

## Key Files

| File | Purpose |
|------|---------|
| `Plugin.cs` | Main entry point, service initialization |
| `Configuration.cs` | Settings data class |
| `ConfigWindow.cs` | Main settings UI |
| `SoundHelper.cs` | Audio playback |
| `LipSyncHelper.cs` | Character lip sync |
| `AddonTalkHelper.cs` | Standard dialogue |
| `AddonBattleTalkHelper.cs` | Battle dialogue |
| `AddonBubbleHelper.cs` | Chat bubbles |
| `ChatTalkHelper.cs` | Chat TTS |

## Commands

- `/vm` - Open configuration window
- `/vmtalk` - Talk window settings
- `/vmbtalk` - Battle talk settings
- `/vmbubble` - Bubble settings
- `/vmchat` - Chat settings

## Dependencies

- **Dalamud API 14** - Plugin framework
- **ManagedBass 4.0.1** - Audio playback library
- **Humanizer.Core** - Text normalization (multiple locales)
- **R3 1.1.13** - Reactive extensions
- **Reloaded.Memory 7.1.0** - Memory utilities
- **bass.dll** - Native audio library (included)

## Project Metadata

- **AssemblyName**: VoiceMaster
- **InternalName**: VoiceMaster
- **Version**: 0.15.0.1
- **DalamudApiLevel**: 14
- **License**: AGPL-3.0-or-later
- **TargetFramework**: net10.0-windows10.0.26100.0

## Important Notes

- Self-hosted TTS backend required (AllTalk, etc.)
- Uses ManagedBass for cross-platform audio
- Lip sync integrates with FFXIV character models
- NPC ignores are case-insensitive
- Text normalization ensures consistent speech
- Voice mapping can be configured per-NPC
- Supports multiple languages via Humanizer
- Audio files cached for performance

## Credits

Based on **Echokraut** by Ren Nagasaki:
- Original: https://github.com/RenNagasaki/Echokraut
- License: AGPL-3.0-or-later
