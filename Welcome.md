# Obsidian Vault - Project Notes

Welcome to your FFXIV plugin development vault!

## 📁 Vault Structure

```
Obsidian/
├── Daily-Notes/              # Master daily activity log
│   └── 2026-03-08.md        # Today's overview
│
├── ActionStacksEX/           # ActionStacksEX Plugin
│   ├── SUMMARY.md           # Project overview
│   └── Daily/               # (Daily notes appear here)
│
├── ParseLord3/               # ParseLord3 Rotation Engine
│   ├── SUMMARY.md           # Project overview
│   └── Daily/               # (Daily notes appear here)
│
├── VoiceMaster/              # VoiceMaster TTS Plugin
│   ├── SUMMARY.md           # Project overview
│   └── Daily/               # (Daily notes appear here)
│
├── Workflows/                # Automation & Tools
│   ├── README.md            # Workflow documentation
│   ├── Daily-Notes-Spec.md  # Technical specification
│   └── Generate-DailyNotes.ps1  # Daily notes generator
│
└── Welcome.md               # This file
```

## 🚀 Quick Links

### Projects
- [[ActionStacksEX/SUMMARY|ActionStacksEX]] - Battle system QoL plugin
- [[ParseLord3/SUMMARY|ParseLord3]] - Rotation engine with overlays
- [[VoiceMaster/SUMMARY|VoiceMaster]] - TTS plugin for FFXIV

### Today
- [[Daily-Notes/2026-03-08|Today's Activity]]

### Workflows
- [[Workflows/README|Daily Notes Workflow]]
- [[Workflows/Daily-Notes-Spec|Technical Specification]]

## 📝 Daily Notes System

This vault includes an **automated daily notes system** that tracks your coding activity.

### How It Works
1. Run `Generate-DailyNotes.ps1` to capture today's activity
2. Review generated notes in project folders
3. Fill in TODO sections with your context
4. Check the master daily index for overview

### Running the Generator
```powershell
cd "C:\Users\kruil\Documents\Projects\Obsidian\Workflows"
.\Generate-DailyNotes.ps1
```

See [[Workflows/README|full documentation]] for setup options.

## 🔧 Projects Tracked

| Project | Type | Description |
|---------|------|-------------|
| ActionStacksEX | Dalamud Plugin | Enhanced battle system with action stacking |
| ParseLord3 | Dalamud Plugin | Rotation solver with teaching mode |
| VoiceMaster | Dalamud Plugin | TTS for dialogue, chat, bubbles |

## 🎯 Next Steps

1. **Test the workflow:** Run the PowerShell script after making git commits
2. **Add more projects:** Run the generator - it'll auto-detect new git repos
3. **Customize templates:** Edit the generator script to match your style
4. **Set up automation:** Create a scheduled task for daily generation

## 📊 Statistics

- **Projects tracked:** 3
- **Daily notes:** 1
- **Total summaries:** 3

---

*Vault initialized: March 8, 2026*
*Last updated: March 8, 2026*
