# Obsidian Vault - FFXIV Dalamud Projects + B.L.A.S.T. Automation

This vault contains:
1. **FFXIV Dalamud plugin development** (ActionStacksEX, ParseLord3, VoiceMaster)
2. **B.L.A.S.T. automation framework** for deterministic workflows

---

## FFXIV Projects

### [[ActionStacksEX/CLAUDE.md|ActionStacksEX]]
Enhanced battle system QoL plugin with action stacking, custom pronouns, and 14 feature modules.
- **Status:** Active development
- **Type:** Combat utility
- **Key Features:** Action stacks, macro queueing, decombo abilities

### [[ParseLord3/CLAUDE.md|ParseLord3]]
High-performance rotation solver combining RSR-style execution with granular configuration UI.
- **Status:** Active development
- **Type:** Rotation helper
- **Key Features:** Auto-rotation, custom triggers, action queuing, teaching overlays

### [[VoiceMaster/CLAUDE.md|VoiceMaster]]
Text-to-Speech plugin for FFXIV dialogue (rebrand of Echokraut).
- **Status:** Active development
- **Type:** Accessibility/Immersion
- **Key Features:** AllTalk TTS backend, lip sync, NPC voice mapping

---

## B.L.A.S.T. Automation Framework

Standardized workflow for building deterministic automation. All new automation projects follow the **B.L.A.S.T.** protocol:

| Stage | Purpose | Context |
|-------|---------|---------|
| **B** - Blueprint | Planning & research | [[stages/01-blueprint/CONTEXT\|Stage 01]] |
| **L** - Link | API verification | [[stages/02-link/CONTEXT\|Stage 02]] |
| **A** - Architect | Build & execute | [[stages/03-architect/CONTEXT\|Stage 03]] |
| **S** - Stylize | Refine & polish | [[stages/04-stylize/CONTEXT\|Stage 04]] |
| **T** - Trigger | Test & deploy | [[stages/05-trigger/CONTEXT\|Stage 05]] |

### Core Documentation
- [[CONTEXT\|CONTEXT.md]] - Navigation hub (Layer 1)
- [[Interpreted Context Methodology (ICM)\|Interpreted Context Methodology (ICM).md]] - Architecture guide
- [[claude\|claude.md]] - Project constitution template
- [[task_plan\|task_plan.md]] - Phase tracking
- [[findings\|findings.md]] - Research & discoveries
- [[progress\|progress.md]] - Work log

### Configuration
- [[_config/brand-guidelines\|Brand Guidelines]] - Formatting standards
- [[_config/voice-rules\|Voice Rules]] - Communication style
- [[.env.example\|.env.example]] - Environment template

### Architecture
- [[architecture/discovery\|discovery.md]] - Requirements SOP
- [[architecture/link\|link.md]] - API verification SOP
- [[architecture/_template\|_template.md]] - SOP template

---

## Claude Code Integration

### Local Skills
Located in `~/.claude/skills/`:
- **dalamud-dev.md** - General-purpose Dalamud plugin development patterns

### Interpreted Context Methodology (ICM)
This vault utilizes the [[Interpreted Context Methodology (ICM)\|ICM]]. For all workflow routines and stages, refer to [[CONTEXT\|CONTEXT.md]] at the root of this vault. The folder structure is the agent architecture.

### Daily Notes
Automated daily notes system:
- [[Daily-Notes/2026-03-11\|Today's Notes]]
- [[Workflows/Daily-Notes-Spec\|Workflow Specification]]

---

## Common Patterns Reference

See individual project CLAUDE.md files for:
- Project-specific architecture
- Build commands
- Key file locations
- Implementation patterns

Cross-project patterns: [[Dalamud-Patterns\|Dalamud-Patterns.md]]

Long-term memory: [[MEMORY\|MEMORY.md]]

---

## Quick Links

### FFXIV Projects
- [[ActionStacksEX/SUMMARY|ActionStacksEX Summary]]
- [[ParseLord3/SUMMARY|ParseLord3 Summary]]
- [[VoiceMaster/SUMMARY|VoiceMaster Summary]]
- [[Dalamud-Patterns|Cross-Project Patterns]]

### Automation
- [[stages/01-blueprint/CONTEXT\|Start New Automation]]
- [[tools/_template\|Tool Template]]
- [[architecture/discovery\|Discovery SOP]]

---

*Last Updated: March 11, 2026*
