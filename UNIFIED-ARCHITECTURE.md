# Unified Cognitive Architecture
## ICM + B.L.A.S.T. + Swarm Integration

## The Three-Layer Vision

```
┌─────────────────────────────────────────────────────────────┐
│  LAYER 3: SWARM (Execution)                                 │
│  ┌──────────┬──────────┬──────────┬──────────┐             │
│  │ EXPLORER │  FIXER   │ REVIEWER │  ORACLE  │             │
│  └──────────┴──────────┴──────────┴──────────┘             │
│         ↑ Role assignment based on B.L.A.S.T. phase         │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  LAYER 2: B.L.A.S.T. (Methodology)                          │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐    │
│  │ BLUEPRINT│→→│   LINK   │→→│ ARCHITECT│→→│  STYLIZE │→→ │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘    │
│       │              │              │              │        │
│       └──────────────┴──────────────┴──────────────┘        │
│              Phase-specific role requirements               │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│  LAYER 1: ICM (Organization)                                │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐    │
│  │ Layer 0│ │ Layer 1│ │ Layer 2│ │ Layer 3│ │ Layer 4│    │
│  │Identity│ │Navigate│ │ Stages │ │Reference││ Output │    │
│  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘    │
│       Filesystem structure with Obsidian wikilinks          │
└─────────────────────────────────────────────────────────────┘
```

## Role Assignment by B.L.A.S.T. Phase

### 🔵 BLUEPRINT Phase (Research & Planning)
**Primary Roles**: `explorer` + `oracle`

```yaml
Blueprint Swarm:
  agent_1:
    role: explorer
    task: "Map existing codebase structure"
    output: stages/01-blueprint/output/codebase-map.md
  
  agent_2:
    role: explorer
    task: "Research integration requirements"
    output: stages/01-blueprint/output/integrations.md
  
  agent_3:
    role: oracle
    task: "Design system architecture"
    output: stages/01-blueprint/output/architecture.md
  
  checkpoint: "Human review before proceeding"
```

**ICM Integration**:
- All agents read `stages/01-blueprint/CONTEXT.md`
- Access `references/` for technical docs
- Output to `stages/01-blueprint/output/`

---

### 🔗 LINK Phase (Resource Gathering & Verification)
**Primary Roles**: `explorer` + `fixer` (for connections)

```yaml
Link Swarm:
  agent_1:
    role: explorer
    task: "Verify API credentials and access"
    output: stages/02-link/output/api-verification.md
  
  agent_2:
    role: explorer
    task: "Map data flow between systems"
    output: stages/02-link/output/data-flow.md
  
  agent_3:
    role: fixer
    task: "Establish test connections"
    output: stages/02-link/output/connection-tests.md
```

---

### 🏗️ ARCHITECT Phase (Build & Implementation)
**Primary Roles**: `fixer` + `oracle` (for complex decisions)

```yaml
Architect Swarm:
  parallel_modules:
    module_auth:
      role: fixer
      task: "Build authentication system"
      input: stages/01-blueprint/output/architecture.md
      output: stages/03-architect/output/auth-module/
    
    module_api:
      role: fixer
      task: "Build REST API endpoints"
      input: stages/01-blueprint/output/architecture.md
      output: stages/03-architect/output/api-module/
    
    module_db:
      role: fixer
      task: "Build database layer"
      input: stages/01-blueprint/output/architecture.md
      output: stages/03-architect/output/db-module/
    
    architecture_review:
      role: oracle
      task: "Review cross-module integration"
      input: All module outputs
      output: stages/03-architect/output/integration-review.md
```

---

### 🎨 STYLIZE Phase (Refinement & Polish)
**Primary Roles**: `reviewer` + `fixer` (for fixes)

```yaml
Stylize Swarm:
  agent_1:
    role: reviewer
    task: "Code quality audit"
    output: stages/04-stylize/output/quality-report.md
  
  agent_2:
    role: reviewer
    task: "Security review"
    output: stages/04-stylize/output/security-review.md
  
  agent_3:
    role: fixer
    task: "Apply fixes from reviews"
    input: Review outputs
    output: stages/04-stylize/output/refined-code/
```

---

### 🚀 TRIGGER Phase (Testing & Deployment)
**Primary Roles**: `fixer` + `explorer` (for testing)

```yaml
Trigger Swarm:
  agent_1:
    role: fixer
    task: "Implement CI/CD pipeline"
    output: stages/05-trigger/output/cicd-config/
  
  agent_2:
    role: explorer
    task: "Comprehensive testing"
    output: stages/05-trigger/output/test-results.md
  
  agent_3:
    role: fixer
    task: "Handle CI failures automatically"
    auto: true
    max_retries: 3
```

---

## Command Interface

### Unified Commands

```bash
# Initialize the full stack
icm-blast-swarm init

# Start a B.L.A.S.T. phase with swarm
icm-blast-swarm phase blueprint
  → Spawns: 2x explorer + 1x oracle
  → Outputs to: stages/01-blueprint/output/
  → Checkpoint: Human review

icm-blast-swarm phase architect --parallel 5
  → Spawns: 5x fixer + 1x oracle
  → Each fixer gets different module
  → Outputs to: stages/03-architect/output/

# Status across all layers
icm-blast-swarm status
  → Shows: ICM layer + B.L.A.S.T. phase + Swarm agents

# Complete workflow automation
icm-blast-swarm run --project "My SaaS"
  → Executes all 5 phases with appropriate swarms
  → Respects checkpoints between phases
  → Auto-advances when possible
```

---

## Configuration

### `.opencode/icm-blast-swarm.yaml`

```yaml
# ICM Configuration
icm:
  vault_path: .
  layers:
    - _config/
    - references/
    - architecture/
    - stages/
  wikilink_format: obsidian

# B.L.A.S.T. Configuration
blast:
  phases:
    blueprint:
      swarm:
        explorer: 2
        oracle: 1
      checkpoint: required
      
    link:
      swarm:
        explorer: 2
        fixer: 1
      checkpoint: optional
      
    architect:
      swarm:
        fixer: 3
        oracle: 1
      parallel_modules: true
      checkpoint: required
      
    stylize:
      swarm:
        reviewer: 2
        fixer: 1
      checkpoint: required
      
    trigger:
      swarm:
        fixer: 2
        explorer: 1
      auto_ci: true
      checkpoint: optional

# Swarm Configuration
swarm:
  max_parallel: 5
  worktree_base: .worktrees
  agent_types:
    explorer:
      prompt_template: "Research and document: {{task}}"
    fixer:
      prompt_template: "Implement efficiently: {{task}}"
    reviewer:
      prompt_template: "Review for quality: {{task}}"
    oracle:
      prompt_template: "Architect strategically: {{task}}"
```

---

## Workflow Examples

### Example 1: Website Build

```bash
# User: "Build a marketing website"

$ icm-blast-swarm phase blueprint
→ Spawns:
  - explorer: "Research design requirements"
  - explorer: "Map content structure"
  - oracle: "Design technical architecture"
→ Output: stages/01-blueprint/output/website-plan.md
→ Checkpoint: ✅ User approves plan

$ icm-blast-swarm phase architect --parallel 4
→ Spawns:
  - fixer: "Build hero section" → worktree/agent-001/
  - fixer: "Build features section" → worktree/agent-002/
  - fixer: "Build testimonials section" → worktree/agent-003/
  - fixer: "Build contact section" → worktree/agent-004/
→ All parallel, isolated worktrees
→ Output: stages/03-architect/output/

$ icm-blast-swarm phase stylize
→ Spawns:
  - reviewer: "UI/UX audit"
  - reviewer: "Accessibility check"
  - fixer: "Apply polish and fixes"
→ Output: stages/04-stylize/output/refined-website/

$ icm-blast-swarm phase trigger
→ Spawns:
  - fixer: "Deploy to Vercel"
  - explorer: "Run lighthouse tests"
→ Website is live! 🚀
```

### Example 2: Refactor Legacy Codebase

```bash
# User: "Migrate to TypeScript"

$ icm-blast-swarm phase blueprint
→ Spawns:
  - explorer: "Map all JS files"
  - oracle: "Design TS architecture"
  - explorer: "Identify dependencies"
→ Output: stages/01-blueprint/output/ts-migration-plan.md

$ icm-blast-swarm phase architect --parallel 10
→ Spawns 10 fixers in parallel:
  - fixer-1: Migrate models/
  - fixer-2: Migrate services/
  - fixer-3: Migrate controllers/
  - ... etc
→ Each in isolated worktree
→ Auto-merge on success

$ icm-blast-swarm phase trigger
→ Spawns:
  - fixer: "Run full test suite"
  - fixer: "Auto-fix any CI failures"
→ Migration complete! ✅
```

---

## Benefits of This Architecture

### 1. **Deterministic + Parallel**
- B.L.A.S.T. phases are sequential and predictable
- Within each phase, swarm executes in parallel
- Best of both worlds: order + speed

### 2. **Context-Aware**
- ICM provides filesystem structure
- Each agent knows where to read/write
- Wikilinks connect everything

### 3. **Role-Appropriate**
- Each B.L.A.S.T. phase spawns right mix of agents
- No mismatched capabilities
- Specialists do what they do best

### 4. **Observable**
- Dashboard shows: Phase → Agents → Outputs
- Clear lineage: Blueprint → Link → Architect → Stylize → Trigger
- Checkpoint gates ensure quality

### 5. **Scalable**
- Small project: 1 agent per phase
- Large project: 10+ agents per phase
- Same workflow, different scale

---

## Summary

**Your Vision Realized:**

> "ICM as the way everything is organized, B.L.A.S.T. as the methodology of working through a problem, and the swarm as a way to get it done with roles assigned by B.L.A.S.T."

✅ **ICM**: Provides filesystem structure (where agents work)  
✅ **B.L.A.S.T.**: Defines the workflow (what agents do)  
✅ **Swarm**: Executes with role assignment (which agents do it)  

**Result**: A deterministic, observable, parallelizable automation system that scales from simple tasks to complex projects.
