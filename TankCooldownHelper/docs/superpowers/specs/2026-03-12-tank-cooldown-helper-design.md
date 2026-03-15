# TankCooldownHelper - Design Specification

**Date:** 2026-03-12  
**Status:** Draft  
**Plugin Type:** Dalamud Plugin (FFXIV)  
**Target API:** Dalamud API 14 / .NET 10

---

## 1. Overview

**TankCooldownHelper** is a Dalamud plugin that monitors party-wide damage intake and healing output, providing real-time visualization of "danger moments" when mitigation is needed. It helps tanks (and healers) identify when incoming damage exceeds healing capacity, suggesting optimal cooldown usage timing.

### 1.1 Core Value Proposition
- **Predictive Awareness:** Know when you're about to die *before* it happens
- **Cooldown Optimization:** Use mitigation proactively rather than reactively
- **Party Context:** See the whole party's danger state, not just your own
- **Visual Clarity:** FFXIV-themed UI that's informative without being overwhelming

---

## 2. Functional Requirements

### 2.1 Data Collection

| Data Point | Source | Update Frequency |
|------------|--------|------------------|
| Incoming Damage | `CombatEvent` hook / `FFXIVClientStructs` | Real-time (per packet) |
| Incoming Healing | `CombatEvent` hook | Real-time (per packet) |
| Party Member HP | `IClientState.PartyList` | Per Framework.Update |
| Player Cooldowns | `IClientState.LocalPlayer.StatusList` | Per Framework.Update |
| Action Availability | `ActionManager` unsafe hooks | On-demand |

### 2.2 Calculation Engine

**DPS/HPS Calculation:**
- **Time Window:** 5 seconds sliding window (default)
- **Configurable Range:** 1-15 seconds via settings
- **Formula:** `RollingSum(damage_events) / window_duration`

**Danger Ratio Calculation:**
```
DangerRatio = IncomingDPS / IncomingHPS
```
- Ratio < 1.0: Healing covers damage (Safe - Green)
- Ratio 1.0-1.5: Damage exceeds healing moderately (Warning - Yellow)
- Ratio 1.5-2.0: Damage significantly exceeds healing (Critical - Orange)
- Ratio > 2.0: Imminent death without mitigation (Emergency - Red)

*Note: Threshold percentages are configurable in settings*

**Predictive Death Timer:**
```
SecondsUntilDeath = CurrentHP / (IncomingDPS - IncomingHPS)
```
Only calculated when DPS > HPS.

### 2.3 Display Modes

#### Mode 1: Real-Time Danger Meter
Shows current DPS vs HPS ratio with color-coded warning.

**Visual:**
- Bar graph showing DPS (red) vs HPS (green)
- Ratio percentage display
- Color-coded background based on danger level

#### Mode 2: Predictive Timeline
Shows projected HP over next 5-10 seconds.

**Visual:**
- Line graph with HP on Y-axis, time on X-axis
- Current HP marker
- Projected HP curve (dotted line when extrapolating)
- Death point marker (if applicable)

#### Mode 3: Cooldown-Aware Suggestions
Analyzes available mitigation and suggests optimal usage.

**Visual:**
- List of available cooldowns with icons
- "Use Now" indicator when a cooldown would prevent death
- Estimated HP saved if cooldown is used
- Priority ranking (which cooldown to use first)

### 2.4 Party Context

**Tracked Entities:**
- All 8 party members (or alliance members in 24-man)
- Focus target (if outside party)
- Priority highlighting for lowest HP % member

**Party Breakdown Module (optional):**
- Shows each member's individual danger state
- Sorted by danger ratio (most endangered first)
- Quick-target button to select endangered member

---

## 3. UI/UX Design

### 3.1 Main Window Layout

```
+--------------------------------------------------+
|  TankCooldownHelper                    [⚙][✕]   |
+--------------------------------------------------+
|  [Danger Meter]        [Net Damage: +2,847/s]    |
|  ████████████░░░░░░░░  [Healing: -1,234/s]       |
|  DPS: 4,081  |  HPS: 1,234  |  Ratio: 3.31x      |
+--------------------------------------------------+
|  [Predictive Timeline]                           |
|     HP                                          |
|  50k ┤          ╭─── Death in 4.2s              |
|  40k ┤      ╭────╯                              |
|  30k ┤  ╭────                                    |
|  20k ┤──╯                                        |
|  10k ┤                                           |
|    0 ┼────┬────┬────┬────┬────→ Time           |
|      Now  2s   4s   6s   8s                     |
+--------------------------------------------------+
|  [Cooldown Suggestions]                          |
|  ⚡ Rampart      - Use now! (+8.2s to live)     |
|  🛡️ Sentinel    - Available                     |
|  🌿 Invincible  - On cooldown (12s)             |
+--------------------------------------------------+
|  [Party Overview - Collapsible]                  |
|  🔴 You (Tank)      45% HP  |  Ratio: 3.31x     |
|  🟡 Healer          78% HP  |  Ratio: 0.84x     |
|  🟢 DPS1            92% HP  |  Ratio: 0.12x     |
+--------------------------------------------------+
```

### 3.2 Customizable Modules

Each module can be:
- **Toggled on/off** via settings
- **Reordered** via drag-and-drop
- **Resized** (some modules support compact/full modes)

**Available Modules:**
1. Danger Meter (bar + ratio)
2. Net Damage Counter (numeric display)
3. Predictive Timeline (graph)
4. Cooldown Suggestions (action icons)
5. Party Breakdown (party list with danger states)
6. Recent Spikes Log (last 5 danger events with timestamps)
7. Mini Mode (compact single-line display)

### 3.3 Visual Theme

**FFXIV-Style Design:**
- Color palette matching game UI (dark blues, gold accents)
- Font: Matches Dalamud/ImGui defaults
- Window border: Subtle rounded corners
- Opacity: Configurable background transparency
- Scale: Respects Dalamud global UI scale

**Color Coding:**
- Safe (Ratio < 1.0): `#4CAF50` (Green)
- Warning (Ratio 1.0-1.5): `#FFC107` (Yellow/Amber)
- Critical (Ratio 1.5-2.0): `#FF9800` (Orange)
- Emergency (Ratio > 2.0): `#F44336` (Red)

---

## 4. Architecture

### 4.1 Project Structure

```
TankCooldownHelper/
├── TankCooldownHelper.csproj
├── TankCooldownHelper.json          # Plugin manifest
├── TankCooldownHelper.cs            # Plugin entry point
├── Configuration.cs                 # Settings data class
├── UI/
│   ├── MainWindow.cs               # Primary ImGui window
│   ├── ModuleComponents.cs         # Reusable UI modules
│   └── SettingsWindow.cs           # Configuration UI
├── Core/
│   ├── DamageTracker.cs            # Incoming damage calculation
│   ├── HealingTracker.cs           # Incoming healing calculation
│   ├── Predictor.cs                # HP projection engine
│   ├── CooldownManager.cs          # Available mitigation tracking
│   └── DangerCalculator.cs         # Ratio/threshold calculations
├── Data/
│   ├── PartyState.cs               # Party member tracking
│   ├── CombatEventBuffer.cs        # Rolling window buffer
│   └── CooldownData.cs             # Mitigation action definitions
└── Extensions/
    └── ImGuiExtensions.cs          # Custom ImGui utilities
```

### 4.2 Data Flow

```
Combat Events (Network/Packets)
    ↓
CombatEventBuffer (sliding window)
    ↓
DamageTracker / HealingTracker (aggregate by target)
    ↓
DangerCalculator (compute ratios/thresholds)
    ↓
Predictor (generate HP projections)
    ↓
CooldownManager (check available mitigations)
    ↓
MainWindow (render UI)
```

### 4.3 Key Classes

**CombatEventBuffer:**
```csharp
public class CombatEventBuffer
{
    private readonly Queue<CombatEvent> _events = new();
    private readonly float _windowSeconds;
    
    public void AddEvent(CombatEvent evt);
    public float GetTotalDamage(uint targetId);
    public float GetTotalHealing(uint targetId);
    public void PruneOldEvents();
}
```

**DangerCalculator:**
```csharp
public class DangerCalculator
{
    public DangerLevel CalculateDangerLevel(float dps, float hps);
    public float CalculateDangerRatio(float dps, float hps);
    public float? CalculateDeathTimer(float currentHp, float netDps);
}

public enum DangerLevel
{
    Safe,      // Ratio < 1.0
    Warning,   // Ratio 1.0-1.5
    Critical,  // Ratio 1.5-2.0
    Emergency  // Ratio > 2.0
}
```

**CooldownManager:**
```csharp
public class CooldownManager
{
    public IEnumerable<MitigationOption> GetAvailableMitigations();
    public float EstimateHPSaved(IMitigationAction action, float incomingDps);
    public IEnumerable<MitigationSuggestion> GetSuggestions(float dangerRatio, float deathTimer);
}
```

---

## 5. Configuration

### 5.1 Settings Schema

```csharp
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    
    // Time Window
    public float TimeWindowSeconds { get; set; } = 5.0f;
    public float MinTimeWindow { get; set; } = 1.0f;
    public float MaxTimeWindow { get; set; } = 15.0f;
    
    // Thresholds
    public float WarningThreshold { get; set; } = 1.0f;    // DPS/HPS ratio
    public float CriticalThreshold { get; set; } = 1.5f;
    public float EmergencyThreshold { get; set; } = 2.0f;
    
    // Display
    public bool ShowDangerMeter { get; set; } = true;
    public bool ShowPredictiveTimeline { get; set; } = true;
    public bool ShowCooldownSuggestions { get; set; } = true;
    public bool ShowPartyBreakdown { get; set; } = false;
    public bool ShowRecentSpikes { get; set; } = false;
    
    // Party
    public bool TrackFullParty { get; set; } = true;
    public bool HighlightMostEndangered { get; set; } = true;
    
    // Mode
    public DisplayMode DefaultMode { get; set; } = DisplayMode.All;
    public bool AutoSwitchMode { get; set; } = false;
    
    // Window
    public bool LockWindowPosition { get; set; } = false;
    public float WindowOpacity { get; set; } = 0.95f;
    
    // ParseLord3 Integration (future)
    public bool EnablePL3Integration { get; set; } = false;
    public bool ExportDangerStateToPL3 { get; set; } = false;
}

public enum DisplayMode
{
    RealTime,    // Danger meter only
    Predictive,  // Timeline only
    Cooldown,    // Suggestions only
    All          // Everything
}
```

### 5.2 Persistence
- Settings saved to: `%APPDATA%/XIVLauncher/pluginConfigs/TankCooldownHelper.json`
- Uses standard Dalamud `IPluginConfiguration` interface

---

## 6. ParseLord3 Integration Plan

### 6.1 Phase 1: Standalone
Plugin operates independently with no external dependencies.

### 6.2 Phase 2: IPC Integration
Export danger state via Dalamud IPC for ParseLord3 consumption:

```csharp
// TankCooldownHelper exposes:
public static class DangerStateIPC
{
    public static DangerState GetCurrentDangerState();
    public static event Action<DangerState> OnDangerStateChanged;
}

// ParseLord3 can consume:
public class DangerState
{
    public float DangerRatio { get; set; }
    public DangerLevel Level { get; set; }
    public float? SecondsUntilDeath { get; set; }
    public uint MostEndangeredPartyMember { get; set; }
}
```

### 6.3 Phase 3: Shared Library
Potentially extract core calculation logic into shared library (ParseLord.Basic extension).

---

## 7. Technical Considerations

### 7.1 Performance
- Combat event processing: O(1) per event with rolling buffer
- UI update: Once per Framework.Update (60fps max)
- Memory: Bounded by time window (max ~1000 events in 15s window)
- Target: <0.1ms per frame processing time

### 7.2 Thread Safety
- All game state access from Framework.Update thread only
- Combat events queued and processed on main thread
- No unsafe memory access outside `Framework.Update`

### 7.3 Combat Event Sources

**Option A: Network Packet Hook (Highest Accuracy)**
- Hook into `CombatEvent` packets
- Pros: Exact damage/healing values, immediate
- Cons: Requires unsafe code, packet structure changes with patches

**Option B: FFXIVClientStructs (Recommended)**
- Poll `ActionManager` and `Character` state
- Pros: Stable API through Dalamud
- Cons: Slightly delayed, may miss very fast events

**Decision:** Start with Option B for stability, evaluate Option A for accuracy if needed.

### 7.4 Limitations
- Cannot track damage that hasn't happened yet (boss mechanics must resolve)
- Healing prediction limited to known heals (HoTs, casted heals)
- Does not account for shields unless tracked via status effects
- Cannot know boss intentions (only reactive to incoming damage)

### 7.5 Error Handling

**Null State Handling:**
- If `IClientState.LocalPlayer` is null: Display "Waiting for game data..." overlay
- If `PartyList` is empty: Show self-only mode indicator
- If combat buffer is empty for >5s: Gray out display with "No recent combat"

**Calculation Safety:**
- Division by zero protection: Return `null` for death timer when DPS ≈ HPS
- Clamp danger ratio display to reasonable range (0.0 - 99.9x)
- Validate all game object IDs before lookup

**Configuration Resilience:**
- Clamp invalid threshold values to min/max bounds on load
- Log warnings for out-of-range settings, use defaults
- Validate time window: if < 0.5s or > 30s, reset to 5.0s

**Graceful Degradation:**
- If FFXIVClientStructs read fails, retry next frame (max 3 attempts)
- If combat event source unavailable, switch to polling mode
- Never throw exceptions in Framework.Update (catch and log)

### 7.6 Testing Strategy

**Unit Tests (Testable Logic):**
- `DangerCalculator` - Pure math functions, easily unit testable
- `Predictor` - HP projection calculations with mock data
- `CombatEventBuffer` - Rolling window aggregation

**Integration Tests (Dalamud Context):**
- Settings serialization/deserialization
- Window rendering (ImGui integration)
- Combat event pipeline end-to-end

**Manual Test Scenarios:**
1. **Solo Tank Dummy:** Self-damage only, verify DPS calculation
2. **Party with Healer:** Mixed incoming DPS/HPS, verify ratio accuracy
3. **8-Man Content:** Full party tracking, performance check
4. **24-Man Alliance Raid:** Large-scale party context, stress test
5. **Synced Content:** Level 50/60/70 content, verify scaling
6. **Edge Cases:** 
   - Death and resurrection (HP reset)
   - Zone transitions (combat buffer clear)
   - Long periods without combat (idle state)

---

## 8. Future Enhancements

### 8.1 Phase 2 Features
- [ ] Encounter-specific cooldown suggestions (e.g., "Use Rampart for tankbuster in 5s")
- [ ] Voice alerts for critical danger
- [ ] Screenshot/recording of death moments for review
- [ ] Historical analysis (post-combat danger report)

### 8.2 Phase 3 Features
- [ ] Machine learning danger prediction based on encounter patterns
- [ ] Integration with cactbot/triggernometry for mechanic warnings
- [ ] Customizable alert sounds
- [ ] Multi-monitor support (dedicated danger display)

---

## 9. Dependencies

### 9.1 Required
- **Dalamud API 14** - Plugin framework
- **ECommons 3.1.0.13+** - Dalamud utilities (`Svc` pattern)
- **FFXIVClientStructs** - Game state access

### 9.2 Optional
- **ParseLord3** - Future IPC integration (soft dependency)

### 9.3 System Requirements
- .NET 10.0
- Windows 10/11
- FFXIV + Dalamud

---

## 10. Success Criteria

### 10.1 MVP Definition & Gate Criteria

**Core Features:**
- [ ] Plugin loads without errors
- [ ] Displays real-time DPS/HPS ratio accurately
- [ ] Shows color-coded warning based on configurable thresholds
- [ ] Tracks full party context
- [ ] Window is repositionable and configurable
- [ ] Settings persist between sessions

**Gate Criteria (Definition of Done):**
- [ ] All functional requirements from Section 2 implemented
- [ ] Error handling covers all cases in Section 7.5
- [ ] Zero crashes in 4-hour continuous playtest session
- [ ] Memory usage verified <50MB (Section 10.2)
- [ ] Unit tests pass for DangerCalculator and Predictor
- [ ] Code review completed against Dalamud-Patterns.md
- [ ] CLAUDE.md created with build commands and architecture notes
- [ ] Plugin manifest (TankCooldownHelper.json) validated

### 10.2 Quality Metrics
- UI updates within 1 frame of combat events
- Danger ratio accuracy within 5% of actual game values
- Zero crashes during typical 8-man content
- Memory usage under 50MB

---

## 11. Open Questions & Resolution Plan

| Question | Impact | Resolution Approach |
|----------|--------|---------------------|
| Threshold tuning (Q1) | UI accuracy | Implement with defaults (1.0x, 1.5x, 2.0x), add optional telemetry to collect real-world data for tuning |
| Cooldown data (Q2) | Feature completeness | Use Lumina Excel sheets for action data, hardcode mitigation potencies for MVP, verify via community testing |
| Shield tracking (Q3) | Prediction accuracy | Track via StatusEffect IDs (Divine Veil, Sacred Soil, etc.) - research task to identify all shield effects |
| Alliance scope (Q4) | Performance | Start with party-only (8 members) for MVP, add alliance tracking (24 members) as post-MVP config option |

**Research Tasks:**
- [ ] Identify all tank mitigation action IDs and potencies via Lumina
- [ ] Identify all shield StatusEffect IDs (healer shields, tank self-shields)
- [ ] Test shield absorption detection via combat events

---

## 12. References

- [[../Dalamud-Patterns.md]] - Existing plugin patterns from ParseLord3, ActionStacksEX, VoiceMaster
- [[../../ParseLord3/CLAUDE.md]] - ParseLord3 architecture
- [[../../ActionStacksEX/CLAUDE.md]] - Action interception patterns
- [[../../CLAUDE.md]] - Project-specific documentation (to be created)
- Dalamud Documentation: https://dalamud.dev/

## 13. Project Documentation Checklist

- [ ] Create CLAUDE.md at project root following ParseLord3 template
- [ ] Document build commands and project structure
- [ ] Document architecture decisions (why ECommons vs DI, why Option B for events, etc.)
- [ ] Add troubleshooting section for common issues

---

*Document Version: 1.0*  
*Next Step: Implementation planning via writing-plans skill*
