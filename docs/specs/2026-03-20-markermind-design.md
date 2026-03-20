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

| Level | Marker Detail | Trigger Condition | Reset |
|-------|--------------|-------------------|-------|
| **1** | Danger zones only | First encounter with mechanic | Per-encounter wipe |
| **2** | + Suggested safe spots | Confidence >= 0.5 AND 5+ samples | Never auto-resets |
| **3** | + Movement paths | Confidence >= 0.7 AND 10+ samples | Never auto-resets |
| **4** | + Role-specific callouts | Confidence >= 0.85 AND role clusters >= 3 samples each | Never auto-resets |

**Threshold Rationale:** 
- Level 2 (0.5): Barely better than random, but enough data to suggest
- Level 3 (0.7): Reliable predictions, worth showing paths
- Level 4 (0.85): High confidence, role nuances matter

**Manual Override:** User can force disclosure level via `/markermind level set {1-4}`

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

**TelemetryCollector:**
- **Sampling Rate:** 10Hz (100ms intervals) during active mechanics
- **Capture Triggers:** Mechanic start, mechanic resolution, player death
- **Data Points:** Position (x,y,z), velocity, facing direction, action used

**PatternExtractor:**
- **Algorithm:** Density-based spatial clustering (DBSCAN variant)
- **Distance Threshold:** 2.0 yalms (configurable)
- **Minimum Cluster Size:** 3 successful positions
- **Role Separation:** Clusters computed per-role to handle contradictions

**ModelUpdater:**
- **Update Strategy:** Exponential moving average (α=0.3, weighted toward recent)
- **Success Weight:** Survived = full weight, Died = half weight (what NOT to do)
- **Decay:** Positions older than 90 days reduced to 25% weight

**Confidence Calculation:**
```
confidence = min(1.0, (successCount / minSampleSize) * clusteringQuality)
Where:
- minSampleSize = 5 (configurable)
- clusteringQuality = 0.5-1.0 based on cluster tightness
```

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
  "schemaVersion": "1.0",
  "lastUpdated": "2026-03-20T14:30:00Z",
  "encounterId": "p12s-p1",
  "territoryId": 1154,
  "mechanics": {
    "SHA256:{mechanicDataHash}": {
      "name": "Paradeigma",
      "observations": 47,
      "successRate": 0.89,
      "clusters": [
        {
          "centroid": {"x": 100.0, "y": 0.0, "z": 100.0},
          "radius": 2.5,
          "sampleCount": 23,
          "role": "dps"
        }
      ],
      "avoidZones": [
        {"x": 95.0, "y": 0.0, "z": 105.0, "radius": 3.0}
      ],
      "confidence": 0.78,
      "lastSeen": "2026-03-20T14:00:00Z"
    }
  }
}
```

**Hash Computation:** `mechanicDataHash = SHA256(bossDataId + mechanicName + phaseIndex + castBarId)`

**Coordinate System:** 3D world coordinates relative to zone origin (y = height, ignored for most markers)

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
- **Data Needed:** Mechanic type, timing, targets, safe zones
- **Event Format:** `BossMod.EventStart(uint bossId, string mechanicName, float duration)`

**Without Bossmod:** Plugin falls back to minimal mode:
- No mechanic prediction
- Manual marker placement only
- Chat message: "Bossmod not detected. Running in manual mode."

**Note:** Game memory parsing is out of scope for MVP. Too fragile, version-dependent.

### 6.2 Splatoon Integration
- **IPC Method:** `SplatoonIPC.InjectElement()` for dynamic markers
- **Element Types:** Circles (safe/danger), Lines (movement paths), Cones (AOE)
- **Fallback:** Chat-based markers if Splatoon unavailable

---

## 7. Learning Algorithm

### 7.1 Core Learning Loop

```
ON_MECHANIC_START(mechanicId):
    1. Load model for mechanicId from storage
    2. IF confidence >= 0.5 AND sampleCount >= minSamples:
           predictedPosition = clusterCentroid of successful positions
       ELSE:
           predictedPosition = bossRelativeFallback(mechanicId)
    3. Render marker at predictedPosition
    4. Start telemetry capture at 10Hz

ON_MECHANIC_RESOLVE(mechanicId, outcome):
    1. Stop telemetry capture
    2. FOR EACH capturedPosition IN telemetryBuffer:
           distanceToMarker = distance(capturedPosition, markerPosition)
           IF outcome == "survived" AND distanceToMarker < tolerance:
               addToSuccessCluster(capturedPosition)
           ELSE IF outcome == "died":
               addToAvoidCluster(capturedPosition)
    3. Recalculate cluster centroids
    4. Update confidence score
    5. Persist updated model

ON_CONFIDENCE_UPDATE:
    IF confidence >= LEVEL_THRESHOLD[2] AND disclosureLevel < 2:
        disclosureLevel = 2
    ELSE IF confidence >= LEVEL_THRESHOLD[3] AND disclosureLevel < 3:
        disclosureLevel = 3
    ELSE IF confidence >= LEVEL_THRESHOLD[4] AND disclosureLevel < 4:
        disclosureLevel = 4
```

### 7.2 Configuration Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| `samplingRateHz` | 10 | Telemetry samples per second |
| `minSampleSize` | 5 | Minimum observations before predictions |
| `positionTolerance` | 2.0 yalms | Distance threshold for "following marker" |
| `clusterDistance` | 2.0 yalms | DBSCAN epsilon parameter |
| `emaAlpha` | 0.3 | Exponential moving average weight |
| `dataDecayDays` | 90 | Age before old data loses weight |
| `level2Threshold` | 0.5 | Confidence needed for Level 2 |
| `level3Threshold` | 0.7 | Confidence needed for Level 3 |
| `level4Threshold` | 0.85 | Confidence needed for Level 4 |

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
