# Daily Notes Workflow

An automated system that tracks your coding activity across projects and generates daily notes in your Obsidian vault.

## Quick Start

### 1. Run the Generator

Open PowerShell and run:

```powershell
cd "C:\Users\kruil\Documents\Projects\Obsidian\Workflows"
.\Generate-DailyNotes.ps1
```

### 2. Check Your Notes

Notes are created in:
- **Per-project:** `Obsidian/{ProjectName}/Daily/YYYY-MM-DD.md`
- **Master index:** `Obsidian/Daily-Notes/YYYY-MM-DD.md`

### 3. Review & Customize

Open Obsidian and:
1. Review the auto-generated notes
2. Fill in TODO sections with your context
3. Add manual notes for non-coding work

## What Gets Tracked

### Automatic Detection
- ✅ Git commits (message, hash, author)
- ✅ File modifications (added, modified, deleted)
- ✅ Uncommitted changes
- ✅ Activity categorization (coding, debugging, docs, etc.)

### Manual Additions
Use the TODO sections to add:
- Meeting notes
- Research findings
- Learning activities
- Non-coding project work

## Folder Structure Created

```
Obsidian/
├── Daily-Notes/                    # Master daily index
│   ├── 2026-03-08.md              # Today's overview
│   └── ...
│
├── ActionStacksEX/                 # Existing projects
│   ├── SUMMARY.md
│   └── Daily/
│       └── 2026-03-08.md          # Project-specific details
│
├── ParseLord3/
│   ├── SUMMARY.md
│   └── Daily/
│       └── 2026-03-08.md
│
├── VoiceMaster/
│   ├── SUMMARY.md
│   └── Daily/
│       └── 2026-03-08.md
│
└── Workflows/
    ├── Daily-Notes-Spec.md        # Full specification
    ├── Generate-DailyNotes.ps1    # Generator script
    └── README.md                   # This file
```

## Note Templates

### Per-Project Daily Note
```markdown
---
date: 2026-03-08
day_of_week: Sunday
project: ActionStacksEX
activity_type: [coding, debugging]
files_modified: 5
commits: 2
uncommitted: true
---

# ActionStacksEX - 2026-03-08

## Summary
Brief summary of what was worked on today.

## Activity Log
### 10:30 AM - Module System Refactoring
- Commit: `abc1234`
- Author: kruil

## Files Changed
| File | Status |
|------|--------|
| ActionStackManager.cs | ✏️ Modified |

## Key Decisions
- Decided to use file-scoped namespaces

## Next Steps
- [ ] Test in-game

## Links
- [[ActionStacksEX/SUMMARY|Project Summary]]
```

### Master Daily Index
```markdown
---
date: 2026-03-08
total_projects: 3
total_commits: 5
---

# Daily Note - Sunday, March 8, 2026

## Overview
Brief summary of the day's work.

## Projects Worked On
### [[ActionStacksEX/Daily/2026-03-08|ActionStacksEX]]
- **Focus:** Module system refactoring
- **Files:** 3 modified

## Time Distribution
- ActionStacksEX: 3 hours

## Daily Reflection
What went well, blockers, insights.

## Tomorrow's Priorities
1. [ ] Test ActionStacksEX in FFXIV
```

## Activity Categories

The system automatically categorizes activity:

| Category | Triggers |
|----------|----------|
| **coding** | Writing new code, implementing features |
| **debugging** | Fix, bug, debug, repair in commit message |
| **refactoring** | Refactor, restructure, move, rename |
| **documentation** | .md files, readme, comments, guides |
| **testing** | .test.cs files, spec files |
| **configuration** | .json, .yml, .xml, .config files |

## New Projects

When you work on a new git repository:

1. **Auto-detected:** Script finds any folder with `.git/`
2. **Auto-organized:** Creates `Obsidian/{ProjectName}/Daily/`
3. **Summary stub:** Creates `SUMMARY.md` template
4. **Daily note:** Generates `Daily/YYYY-MM-DD.md`

## Automation Ideas

### Option 1: Scheduled Task (Recommended)
Create a Windows scheduled task to run daily at 11 PM:

```powershell
# Run as daily task
$action = New-ScheduledTaskAction -Execute "PowerShell.exe" -Argument "-File C:\Users\kruil\Documents\Projects\Obsidian\Workflows\Generate-DailyNotes.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 11pm
Register-ScheduledTask -TaskName "DailyNotesGenerator" -Action $action -Trigger $trigger
```

### Option 2: Git Hook
Add to your global git config to trigger on commit:

```bash
# ~/.gitconfig
[core]
    hooksPath = ~/.git-hooks
```

### Option 3: Manual
Run whenever you want to capture activity:

```powershell
# Generate for today
.\Generate-DailyNotes.ps1

# Generate for yesterday (modify script)
$Date = (Get-Date).AddDays(-1).ToString("yyyy-MM-dd")
```

## Customization

### Modify Templates
Edit the `New-ProjectDailyNote` and `New-MasterDailyNote` functions in:
- `Obsidian/Workflows/Generate-DailyNotes.ps1`

### Change Date Format
Edit the `$Date` variable at the top of the script:
```powershell
# ISO format (default)
$Date = Get-Date -Format "yyyy-MM-dd"

# US format
$Date = Get-Date -Format "MM-dd-yyyy"
```

### Add More Metadata
Extend the frontmatter in the templates:
```markdown
---
mood: 😊
energy: high
coffee_count: 3
---
```

## Troubleshooting

### "PowerShell execution policy"
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

### "Git not found"
Ensure git is in your PATH:
```powershell
# Add to your PowerShell profile
$env:PATH += ";C:\Program Files\Git\bin"
```

### No activity detected
- Check you're in a git repository (`git status`)
- Check commits are made (`git log`)
- Check date range (script looks for today's activity)

## Future Enhancements

Possible additions to this workflow:

- [ ] Time tracking integration (Toggl, Clockify)
- [ ] VS Code extension for real-time capture
- [ ] Weekly/monthly automatic summaries
- [ ] Commit message templates
- [ ] Code review tracking
- [ ] Issue/PR linking
- [ ] Screenshot capture
- [ ] Meeting notes integration

## Files Reference

| File | Purpose |
|------|---------|
| `Workflows/Daily-Notes-Spec.md` | Full technical specification |
| `Workflows/Generate-DailyNotes.ps1` | Main automation script |
| `Workflows/README.md` | This documentation |
| `Daily-Notes/*.md` | Generated master daily notes |
| `{Project}/Daily/*.md` | Generated project daily notes |

---

*Version: 1.0*
*Last Updated: March 2026*
