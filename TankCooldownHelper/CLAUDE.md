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
