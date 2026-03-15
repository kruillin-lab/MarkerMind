# Interpreted Context Methodology (ICM)

## Overview

The Interpreted Context Methodology (ICM) is a filesystem-based agent architecture that combines with the **B.L.A.S.T. Protocol** to create deterministic, multi-step automation workflows.

## Core Philosophy

**Context is Code.** The filesystem structure itself guides agent behavior. By organizing context into layered markdown files with Obsidian-compatible wikilinks, we create a self-documenting, navigable system that both humans and AI agents can understand and execute.

## Layer Architecture

### Layer 0: Identity & Orientation
**Files**: `AGENTS.md`, `CLAUDE.md`, `SOUL.md`, `USER.md`

These files answer "Who am I?" and "Where am I?" They contain:
- Agent identity and capabilities
- User preferences and context
- Workspace overview and project inventory
- **Always loaded** at session start

### Layer 1: Navigation
**File**: `CONTEXT.md` (workspace root)

This file answers "Where do I go?" It contains:
- High-level workflow overview
- Links to stage contexts
- Current project status
- **Read on entry** to orient the agent

### Layer 2: Stage Contexts
**Files**: `stages/XX-stage/CONTEXT.md`

These files answer "What do I do?" Each stage has:
- **Inputs**: Table linking to required resources
- **Process**: Step-by-step instructions
- **Outputs**: Table defining deliverables
- **Checkpoint**: Human approval gates

The 5 B.L.A.S.T. stages:
1. [[stages/01-blueprint/CONTEXT\|01: Blueprint]] - Planning & requirements
2. [[stages/02-link/CONTEXT\|02: Link]] - Resource gathering
3. [[stages/03-architect/CONTEXT\|03: Architect]] - Execution & building
4. [[stages/04-stylize/CONTEXT\|04: Stylize]] - Refinement & polish
5. [[stages/05-trigger/CONTEXT\|05: Trigger]] - Testing & deployment

### Layer 3: References & Config
**Folders**: `references/`, `_config/`

These contain:
- Technical specifications
- Brand guidelines
- Voice rules
- API documentation
- **Loaded selectively** based on task needs

### Layer 4: Output & Work Products
**Folders**: `output/` (within each stage), `.tmp/`

These contain:
- Stage deliverables (markdown)
- Temporary work files
- Final payloads

## File Naming Conventions

### Markdown Files
- Use **kebab-case**: `my-file-name.md`
- Prefer lowercase
- Be descriptive: `api-handshake-verification.md` not `test.md`

### Code Files
- Python: `snake_case.py`
- Use descriptive names: `verify_slack_connection.py`

### Link Format
Obsidian wikilinks with escaped pipes:
```markdown
[[filename]]
[[path/to/file\|Display Text]]
[[../relative/path\|Link Text]]
```

## Workflow Execution

### Session Start
1. Load Layer 0 (identity files)
2. Read Layer 1 (`CONTEXT.md`)
3. Determine current task/project
4. Navigate to appropriate stage

### Stage Execution
1. Read Stage `CONTEXT.md` (Layer 2)
2. Load required Inputs (follow wikilinks)
3. Execute Process steps
4. Write Outputs to `output/` folder
5. Checkpoint for human review if required
6. Proceed to next stage

### Navigation Patterns

**Moving Between Stages:**
```markdown
<!-- From stages/01-blueprint/CONTEXT.md -->
Next: [[../02-link/CONTEXT\|Stage 02: Link]]
```

**Referencing Outputs:**
```markdown
<!-- From stages/03-architect/CONTEXT.md -->
Input: [[../01-blueprint/output/blueprint\|Blueprint Document]]
```

**Root-Relative Links:**
```markdown
<!-- From any stage -->
Config: [[../../_config/brand-guidelines\|Brand Guidelines]]
```

## Self-Healing & Maintenance

### When Errors Occur
1. **Analyze**: Read error, check logs in `.tmp/`
2. **Patch**: Fix the issue
3. **Document**: Update SOP in `architecture/`
4. **Prevent**: Add rule to `_config/` if needed

### When Scope Changes
1. **Update**: Modify affected `CONTEXT.md` files
2. **Propagate**: Check downstream stages for impact
3. **Log**: Record change in `MEMORY.md`
4. **Review**: Get human approval for major changes

## Integration with B.L.A.S.T.

ICM provides the **filesystem structure** that B.L.A.S.T. executes within:

| ICM Layer | B.L.A.S.T. Phase | Purpose |
|-----------|------------------|---------|
| Layer 0 | N/A | Identity & orientation |
| Layer 1 | N/A | Navigation hub |
| Layer 2 | Blueprint (B) | Planning & research |
| Layer 2 | Link (L) | Resource mapping |
| Layer 2 | Architect (A) | Execution & building |
| Layer 2 | Stylize (S) | Refinement |
| Layer 2 | Trigger (T) | Testing & deployment |
| Layer 3 | All phases | Reference materials |
| Layer 4 | All phases | Deliverables |

## Best Practices

1. **Never Hardcode**: Use `.env` for secrets
2. **Always Link**: Use wikilinks, not plain text references
3. **Checkpoint Often**: Human approval prevents rework
4. **Document Decisions**: Log "why" in `findings.md`
5. **Clean Temp Files**: `.tmp/` is ephemeral
6. **Commit Outputs**: `output/` files are deliverables

## Example Workflow

```markdown
<!-- User request: "Build a Slack bot" -->

1. Read CONTEXT.md (Layer 1)
2. Navigate to stages/01-blueprint/CONTEXT.md (Layer 2)
3. Execute Blueprint phase:
   - Define requirements
   - Research Slack API
   - Write output/blueprint.md
4. Checkpoint: Get user approval
5. Navigate to stages/02-link/CONTEXT.md
6. Execute Link phase:
   - Verify Slack API keys
   - Test connection
   - Write output/links.md
7. Continue through Architect, Stylize, Trigger...
```

## Update Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-03-11 | Documented ICM | Standardizing workspace architecture |
