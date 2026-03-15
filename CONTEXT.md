# Antigravity Workspace Context - BLAST Protocol

Welcome to the Antigravity Layer 1 Context. This workspace implements the [[Interpreted Context Methodology (ICM)]] combined with the core **BLAST Protocol** via a filesystem-based agent architecture.

## Architecture

This structure forces a multi-step orchestration via single-agent workflows. 
- **Layer 0**: [[AGENTS.md]] and [[CLAUDE.md]] - "Where am I?" (Always Loaded)
- **Layer 1**: This file - "Where do I go?" (Read on entry)
- **Layer 2**: Stage `CONTEXT.md` files - "What do I do?" (Read per-task)
- **Layer 3**: `references/` & `_config/` - "What rules apply?" (Loaded selectively)
- **Layer 4**: `output/` - "What am I working with?" (Loaded selectively)

## Core Automation Framework (BLAST)

Walk through these numbered folder stages sequentially for any project or task:
- [[stages/01-blueprint/CONTEXT|Stage 01: Blueprint]] - Planning, requirement mapping, and formulating step-by-step action plans.
- [[stages/02-link/CONTEXT|Stage 02: Link]] - Gathering required files, importing context, establishing dependencies, and mapping integrations.
- [[stages/03-architect/CONTEXT|Stage 03: Architect]] - Core execution, writing the logic, building systems and architecture based on the Blueprint.
- [[stages/04-stylize/CONTEXT|Stage 04: Stylize]] - Refactoring, aesthetic improvements, applying `brand-guidelines`, naming conventions, and cleanup.
- [[stages/05-trigger/CONTEXT|Stage 05: Trigger]] - Testing, review, final audits, quality gates, and triggering any necessary downstream operations.

Whenever a task enters a new stage:
1. Navigate to the stage folder.
2. Read the stage's `CONTEXT.md`.
3. Read the defined inputs from `references/` or the previous stage's `output/`.
4. Run the process, pausing for human review if a checkpoint is specified.
5. Save the state/deliverable strictly in the stage's `output/` directory as a markdown file formatted for Obsidian (`[[Links]]`).
