# Daily Notes Workflow - Specification

## Overview

An automated daily notes system that tracks project activity, generates summaries, and routes notes to appropriate folders in the Obsidian vault.

## Workflow Design

### 1. Activity Detection

**Sources Monitored:**
- Git commits (message, files changed, stats)
- File modifications (new, modified, deleted)
- Time spent in project folders
- Conversations/interactions (optional manual tagging)

**Detection Method:**
```
1. Scan project folders for .git repositories
2. Check for uncommitted changes (git status)
3. Review recent commit history (git log --since="yesterday")
4. Identify modified files by extension/type
5. Categorize activity by project
```

### 2. Note Generation

**Two-Tier System:**

#### A. Per-Project Daily Notes
**Location:** `Obsidian/{ProjectName}/Daily/{YYYY-MM-DD}.md`

**Content Structure:**
```markdown
---
date: 2026-03-08
day_of_week: Sunday
project: ActionStacksEX
activity_type: [coding, debugging, documentation]
files_modified: 5
lines_added: 120
lines_removed: 45
---

# ActionStacksEX - 2026-03-08

## Summary
Brief summary of what was worked on today.

## Activity Log

### 10:30 AM - Module System Refactoring
- Refactored ActionStackManager to use new PluginModule pattern
- Fixed null reference in PronounManager
- Files: ActionStackManager.cs, PronounManager.cs

### 2:15 PM - Documentation Update
- Updated AGENTS.md with new code style guidelines
- Added troubleshooting section for LSP errors

## Files Modified
| File | Change | Lines |
|------|--------|-------|
| ActionStackManager.cs | Modified | +45/-12 |
| PronounManager.cs | Modified | +23/-8 |
| AGENTS.md | Modified | +52/-0 |

## Key Decisions
- Decided to use file-scoped namespaces (C# 10+)
- Nullability enabled globally

## Next Steps
- [ ] Test refactored modules in-game
- [ ] Update Configuration.cs with new options

## Links
- [[ActionStacksEX/SUMMARY|Project Summary]]
- [[ActionStacksEX/Technical-Notes|Technical Notes]]
```

#### B. Master Daily Index
**Location:** `Obsidian/Daily-Notes/{YYYY-MM-DD}.md`

**Content Structure:**
```markdown
---
date: 2026-03-08
day_of_week: Sunday
total_projects: 3
total_commits: 5
total_files_changed: 12
---

# Daily Note - Sunday, March 8, 2026

## Overview
Brief summary of the day's work across all projects.

## Projects Worked On

### [[ActionStacksEX/Daily/2026-03-08|ActionStacksEX]]
- **Focus:** Module system refactoring
- **Files:** 3 modified
- **Commits:** 2
- **Status:** In progress

### [[ParseLord3/Daily/2026-03-08|ParseLord3]]
- **Focus:** Rotation optimization for Dragoon
- **Files:** 2 modified
- **Commits:** 1
- **Status:** Testing

### [[VoiceMaster/Daily/2026-03-08|VoiceMaster]]
- **Focus:** Inworld AI integration debugging
- **Files:** 1 modified
- **Commits:** 0 (uncommitted changes)
- **Status:** Debugging

## Time Distribution
- ActionStacksEX: 3 hours
- ParseLord3: 2 hours
- VoiceMaster: 1 hour

## Daily Reflection
What went well, blockers, insights.

## Tomorrow's Priorities
1. Test ActionStacksEX modules in FFXIV
2. Finish Dragoon rotation optimization
3. Commit VoiceMaster changes

## Quick Links
- [[Weekly-Review/2026-W10|This Week's Review]]
- [[Monthly-Review/2026-03|This Month's Review]]
```

### 3. Folder Structure

```
Obsidian/
├── Daily-Notes/                    # Master daily index
│   ├── 2026-03-08.md
│   ├── 2026-03-09.md
│   └── ...
│
├── ActionStacksEX/
│   ├── SUMMARY.md
│   ├── Daily/                      # Per-project daily notes
│   │   ├── 2026-03-08.md
│   │   └── ...
│   └── ...
│
├── ParseLord3/
│   ├── SUMMARY.md
│   ├── Daily/
│   │   └── ...
│   └── ...
│
├── VoiceMaster/
│   ├── SUMMARY.md
│   ├── Daily/
│   │   └── ...
│   └── ...
│
├── Weekly-Review/                  # Optional: Weekly summaries
│   ├── 2026-W10.md
│   └── ...
│
├── Monthly-Review/                 # Optional: Monthly summaries
│   └── 2026-03.md
│
└── Misc-Projects/                  # For non-project activity
    ├── Daily/
    │   └── ...
    └── ...
```

### 4. Routing Logic

```python
if activity_detected(project):
    if project_folder_exists(project):
        create_note(f"{project}/Daily/{date}.md")
    else:
        create_folder(project)
        create_summary_stub(project)
        create_note(f"{project}/Daily/{date}.md")
    
    update_master_index(project, activity)
```

**Routing Rules:**
1. **Known Projects** → `ProjectName/Daily/YYYY-MM-DD.md`
2. **Unknown Projects** → Create new folder + summary stub
3. **General Activity** → `Misc-Projects/Daily/YYYY-MM-DD.md`
4. **Always Update** → `Daily-Notes/YYYY-MM-DD.md`

### 5. Automation Script

**Trigger Options:**
- **Manual:** Run `/daily-notes` command
- **Scheduled:** Daily at 11 PM
- **Event-based:** On git commit, on file save

**Workflow Steps:**

```
1. START
2. Identify date range (default: today)
3. Scan Projects/ folder for git repos
4. FOR EACH project:
   a. Get git log for date range
   b. Get git status (uncommitted changes)
   c. Identify file modifications
   d. Categorize activity type
   e. Generate per-project note
   f. Store in project/Daily/
5. Generate master daily index
6. Store in Daily-Notes/
7. Update weekly/monthly summaries (if applicable)
8. END
```

### 6. Activity Categorization

**Types:**
- `coding` - Writing/modifying code
- `debugging` - Fixing bugs
- `documentation` - Writing docs, comments
- `refactoring` - Restructuring code
- `testing` - Writing/running tests
- `configuration` - Build/config files
- `research` - Learning, exploring
- `planning` - Architecture, design

**Detection Heuristics:**
```
.gitignore, *.json, *.yml, *.xml → configuration
*.md, README*, AGENTS* → documentation
*.test.cs, *Tests* → testing
*.cs, *.js, *.ts, *.py → coding
Refactored*, Moved*, Renamed* → refactoring
Fixed*, Bug*, Issue* → debugging
```

### 7. Templates

**Per-Project Template:**
```markdown
---
date: {{date}}
day_of_week: {{day_of_week}}
project: {{project_name}}
activity_type: {{activity_types}}
files_modified: {{file_count}}
lines_added: {{additions}}
lines_removed: {{deletions}}
commits: {{commit_count}}
uncommitted: {{has_uncommitted}}
---

# {{project_name}} - {{date}}

## Summary
{{ai_summary}}

## Activity Log
{{activity_entries}}

## Files Modified
{{files_table}}

{{#if commits}}
## Commits
{{commits_list}}
{{/if}}

## Key Decisions
{{decisions}}

## Next Steps
{{next_steps}}

## Links
- [[{{project_name}}/SUMMARY|Project Summary]]
```

**Master Index Template:**
```markdown
---
date: {{date}}
day_of_week: {{day_of_week}}
total_projects: {{project_count}}
total_commits: {{commit_count}}
total_files_changed: {{total_files}}
---

# Daily Note - {{day_name}}, {{date}}

## Overview
{{day_summary}}

## Projects Worked On
{{projects_list}}

## Time Distribution
{{time_breakdown}}

## Daily Reflection
{{reflection}}

## Tomorrow's Priorities
{{priorities}}

## Quick Links
- [[Weekly-Review/{{week_number}}|This Week's Review]]
- [[Monthly-Review/{{month}}|This Month's Review]]
```

### 8. Integration with Existing Structure

**Projects Already Tracked:**
- ActionStacksEX
- ParseLord3
- VoiceMaster

**New Projects Auto-Detected:**
- Any folder with `.git/` in `C:\Users\kruil\Documents\Projects\`
- Any folder with source files (*.cs, *.js, *.ts, *.py, etc.)

**Special Handling:**
- Skip `node_modules/`, `bin/`, `obj/`, `.vs/`
- Skip folders starting with `.`
- Skip empty folders

### 9. Manual Override

**Options:**
- Add manual entry to any daily note
- Tag activity with `#manual` in commit messages
- Use special syntax in notes: `<!-- manual: description -->`

### 10. Backlinks & Graph

**Automatic Links:**
- Project summary ←→ Daily notes
- Related daily notes (same week/month)
- File mentions link to file locations
- Commit hashes link to git history

## Implementation Priority

**Phase 1 - Core (MVP):**
1. Basic git log parsing
2. Per-project daily notes
3. Master daily index
4. Simple template

**Phase 2 - Enhancement:**
1. File change detection
2. Activity categorization
3. Time tracking
4. Weekly/monthly summaries

**Phase 3 - Polish:**
1. Interactive generation (review before save)
2. Custom templates
3. Graph visualization
4. Search/aggregation queries

## Commands

**Proposed slash commands:**
- `/daily-notes` - Generate today's notes
- `/daily-notes yesterday` - Generate yesterday's notes
- `/daily-notes week` - Generate week summary
- `/daily-notes project ActionStacksEX` - Generate notes for specific project

---

*Version: 1.0*
*Created: March 2026*
