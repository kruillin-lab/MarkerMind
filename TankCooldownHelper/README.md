# TankCooldownHelper

A Dalamud plugin for FFXIV that monitors party-wide damage and healing, providing real-time warnings when mitigation is needed.

## Features

- **Real-time DPS/HPS Tracking**: Monitors incoming damage vs healing
- **Danger Ratio Display**: Visual bar showing damage-to-healing ratio
- **Color-coded Warnings**: 
  - Green: Safe (healing covers damage)
  - Yellow: Warning (damage exceeds healing by 1.0-1.5x)
  - Orange: Critical (ratio 1.5-2.0x)
  - Red: Emergency (ratio > 2.0x)
- **Predictive Timeline**: HP projection showing estimated time until death
- **Configurable Time Window**: Adjust calculation window from 1-15 seconds
- **Party Tracking**: Track damage/healing for all party members

## Installation

1. Build the project: `dotnet build -c Release`
2. The plugin auto-deploys to `%APPDATA%\XIVLauncher\devPlugins\TankCooldownHelper\`
3. Enable in Dalamud plugin installer

## Usage

- Type `/tch` to toggle the main window
- Type `/tch config` to open settings
- Window shows real-time DPS/HPS ratio with color-coded warnings
- Adjust thresholds and display options in the settings window

## Configuration

- **Time Window**: How far back to calculate (1-15 seconds)
- **Thresholds**: Adjust ratio levels for each warning color
- **Display**: Toggle individual UI modules (Danger Meter, Timeline, etc.)

## Architecture

Uses ECommons pattern (Svc static accessor) for Dalamud services.
All game state access happens in Framework.Update for thread safety.

## Dependencies

- Dalamud API 14
- ECommons 3.1.0.13
- FFXIVClientStructs (via Dalamud)

## Future Enhancements

- ParseLord3 integration for automated reactions
- Cooldown suggestions based on available mitigations
- Voice alerts for critical danger
- Historical analysis and reports
