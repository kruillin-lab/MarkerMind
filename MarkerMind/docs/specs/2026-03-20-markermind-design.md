# MarkerMind Design Specification

**Date:** 2026-03-20  
**Status:** Draft  
**Type:** Dalamud Plugin

---

## 1. Executive Summary

MarkerMind is a Dalamud plugin that bridges Bossmod Reborn's mechanic detection with Splatoon's in-world marker rendering. It features self-learning capabilities that progressively improve marker placement predictions based on player behavior and encounter outcomes.

---

## 2. Core Concept

**Mission:** Provide intelligent, adaptive visual markers that learn from player positioning patterns to optimize mechanic navigation.

**Key Features:**
- Real-time mechanic detection via Bossmod integration
- Progressive marker disclosure (levels 1-4 based on learning)
- Role-aware positioning (tank/healer/DPS specific markers)
- Self-learning prediction system (local-only, no cloud)
- Graceful degradation when dependencies unavailable

---

## 3. User Experience Goals

### 3.1 Progressive Disclosure Levels

| Level | Marker Detail | Trigger Condition |
|-------|--------------|-------------------|
| **1** | Danger zones only | First encounter with mechanic |
| **2** | + Suggested safe spots | Survive mechanic 3+ times |
| **3** | + Movement paths | High-confidence predictions |
| **4** | + Role-specific callouts | Role pattern learned |

### 3.2 Use Cases

1. **Blind Prog:** Learns alongside player, starts minimal
2. **Farm Optimization:** High-confidence markers for speed
3. **Accessibility:** Visual clarity for mechanic processing

---

## 4. Architecture

### 4.1 System Components

```
┌─────────────────────────────────────────────────────────────┐
│                      MARKERMIND PLUGIN                      │
├─────────────────────────────────────────────────────────────┤
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐ │
│  │   ADAPTER    │───►│ TRANSLATION  │───►│   RENDERER   │ │
│  │              │    │   ENGINE     │    │              │ │
│  │ • Bossmod    │    │              │    │ • Splatoon   │ │
│  │   State      │    │ • Mechanic   │    │   IPC        │ │
│  │ • Game       │    │   Mapping    │    │ • Element    │ │
│  │   Context    │    │ • Role       │    │   Injection  │ │
│  │              │    │   Detection  │    │              │ │
│  └──────────────┘    └──────┬───────┘    └──────────────┘ │
│                             │                             │
│                             ▼                             │
│                    ┌──────────────┐                        │
│                    │   PREDICTOR  │                        │
│                    │              │                        │
│                    │ • Position   │                        │
│                    │   Models     │                        │
│                    │ • Confidence │                        │
│                    │   Scoring    │                        │
│                    └──────┬───────┘                        │
│                           │                                 │
│                           ▼                                 │
│                    ┌──────────────┐                        │
│                    │    LEARNER   │                        │
│                    │              │                        │
│                    │ • Telemetry  │                        │
│                    │ • Pattern    │                        │
│                    │   Extraction │                        │
│                    │ • Model      │                        │
│                    │   Updates    │                        │
│                    └──────────────┘                        │
└─────────────────────────────────────────────────────────────┘
```

### 4.2 Component Details

#### 4.2.1 Adapter Layer
- **BossmodBridge:** Subscribes to Bossmod IPC/events for mechanic state
- **GameState:** Tracks player position, role, status, combat state
- **EncounterDetector:** Identifies current encounter and phase

#### 4.2.2 Translation Engine
- **MechanicMapper:** Maps Bossmod mechanic IDs to marker strategies
- **RoleClassifier:** Detects player role from game state
- **ProgressionTracker:** Determines current disclosure level for each mechanic

#### 4.2.3 Predictor
- **PositionModel:** Simple regression/scoring for optimal marker placement
- **ConfidenceScorer:** How sure are we about this prediction?
- **SafetyCalculator:** Where is safe vs unsafe?

#### 4.2.4 Learner
- **TelemetryCollector:** Captures position, movement, outcomes (live/die)
- **PatternExtractor:** Identifies recurring patterns in successful runs
- **ModelUpdater:** Adjusts weights based on new data

#### 4.2.5 Renderer
- **SplatoonIPC:** Communicates with Splatoon via ECommons IPC
- **ElementFactory:** Creates appropriate Splatoon Elements (circles, lines, cones)
- **FallbackRenderer:** Chat messages if Splatoon unavailable

---

## 5. Data Model

### 5.1 Learning Data (Local Storage)

```
%APPDATA%/MarkerMind/
├── encounters/
│   ├── {territoryId}-{encounterId}.json
│   └── ...
├── telemetry/
│   └── {date}/
│       └── {sessionId}.json
├── config.json
└── models/
    └── position-weights.json
```

### 5.2 Encounter Learning Schema

```json
{
  "encounterId": "p12s-p1",
  "mechanics": {
    "mechanicHash123": {
      "name": "Paradeigma",
      "observations": 47,
      "successRate": 0.89,
      "averageSafePositions": [...],
      "rolePositions": {
        "tank": [...],
        "healer": [...],
        "dps": [...]
      },
      "confidence": 0.78
    }
  }
}
```

### 5.3 Telemetry Schema

```json
{
  "timestamp": "2026-03-20T14:30:00Z",
  "encounterId": "p12s-p1",
  "mechanicId": "mechanicHash123",
  "playerPosition": {"x": 100.0, "y": 0.0, "z": 100.0},
  "playerRole": "dps",
  "outcome": "survived",
  "markerShown": true,
  "markerPosition": {"x": 95.0, "y": 0.0, "z": 105.0}
}
```

---

## 6. Integration Points

### 6.1 Bossmod Integration
- **IPC Method:** Subscribe to `BossMod` events via ECommons IPC
- **Fallback:** Parse game memory for encounter state if Bossmod unavailable
- **Data Needed:** Mechanic type, timing, targets, safe zones

### 6.2 Splatoon Integration
- **IPC Method:** `SplatoonIPC.InjectElement()` for dynamic markers
- **Element Types:** Circles (safe/danger), Lines (movement paths), Cones (AOE)
- **Fallback:** Chat-based markers if Splatoon unavailable

---

## 7. Learning Algorithm (Simplified)

```
For each mechanic occurrence:
    1. Predict optimal marker position based on historical safe positions
    2. Show marker to player
    3. Capture player actual position after mechanic resolves
    4. Record outcome (survived/died)
    5. Update running average of "successful positions"
    6. Increase confidence score
    7. When confidence > threshold, enable next disclosure level
```

---

## 8. Configuration

### 8.1 User Settings

| Setting | Type | Default |
|---------|------|---------|
| `EnableLearning` | bool | true |
| `ProgressionMode` | enum | Automatic |
| `MarkerOpacity` | float | 0.8 |
| `MaxMarkersPerMechanic` | int | 3 |
| `RequireSplatoon` | bool | false |
| `TelemetryRetention` | days | 30 |

### 8.2 Role Presets

Users can customize marker strategies per role:
- Tank: Pre-position markers, invuln timing
- Healer: Stack spots, party positioning
- DPS: Uptime positions, melee/ranged specific

---

## 9. Dependencies

| Dependency | Required | Soft | Fallback |
|------------|----------|------|----------|
| Dalamud | ✅ | - | N/A |
| ECommons | ✅ | - | N/A |
| Splatoon | - | ✅ | Chat markers |
| Bossmod | - | ✅ | Game memory parse |

---

## 10. MVP Scope

### Phase 1: Core Bridge
- [ ] Bossmod state adapter
- [ ] Splatoon IPC renderer
- [ ] Basic mechanic → marker mapping

### Phase 2: Learning
- [ ] Telemetry collection
- [ ] Position model updates
- [ ] Confidence scoring

### Phase 3: Intelligence
- [ ] Progressive disclosure
- [ ] Role detection
- [ ] Pattern extraction

### Phase 4: Polish
- [ ] Configuration UI
- [ ] Data management
- [ ] Performance optimization

---

## 11. Success Metrics

1. **Accuracy:** Marker suggestions match successful positions >80% after 10+ pulls
2. **Adoption:** Players use recommended positions without manual adjustment
3. **Performance:** <1ms per-frame overhead
4. **Reliability:** Graceful degradation works 100% of time

---

## 12. Risks & Mitigations

| Risk | Mitigation |
|------|------------|
| Splatoon API changes | Version detection, graceful fallback |
| Bossmod state drift | Multiple data sources, validation |
| Learning data corruption | Backups, validation checksums |
| Performance with many markers | Culling, LOD system |
| False confidence | Minimum sample sizes required |

---

## 13. Future Enhancements

- Import/export learning data
- Community pattern sharing (opt-in)
- Replay analysis from duty recorder
- Voice callout integration
- Multi-monitor support

---

*Document Version: 1.0*  
*Last Updated: 2026-03-20*  
*Status: Ready for Review*
