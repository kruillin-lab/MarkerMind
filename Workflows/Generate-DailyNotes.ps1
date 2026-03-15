# Daily Notes Generator - Implementation

$ErrorActionPreference = "Stop"

# Configuration
$ObsidianVault = "C:\Users\kruil\Documents\Projects\Obsidian"
$ProjectsRoot = "C:\Users\kruil\Documents\Projects"
$Date = Get-Date -Format "yyyy-MM-dd"
$DayOfWeek = (Get-Date).DayOfWeek.ToString()
$DayName = (Get-Culture).DateTimeFormat.GetDayName((Get-Date).DayOfWeek)

# Colors for output
$Green = "`e[32m"
$Blue = "`e[34m"
$Yellow = "`e[33m"
$Reset = "`e[0m"

Write-Host "${Blue}=== Daily Notes Generator ===${Reset}"
Write-Host "Date: $Date ($DayName)"
Write-Host "Obsidian Vault: $ObsidianVault"
Write-Host ""

# Function to get git activity for a project
function Get-GitActivity {
    param([string]$ProjectPath)
    
    Set-Location $ProjectPath
    
    # Check if git repo
    if (-not (Test-Path ".git")) {
        return $null
    }
    
    # Get today's commits
    $commits = git log --since="$Date 00:00" --until="$Date 23:59" --pretty=format:"%H|%s|%ad|%an" --date=short 2>$null
    
    # Get uncommitted changes
    $status = git status --short 2>$null
    $hasUncommitted = -not [string]::IsNullOrWhiteSpace($status)
    
    # Get file changes stats
    $stats = git diff --stat HEAD 2>$null
    $diffStat = git diff --shortstat HEAD 2>$null
    
    # Get list of changed files
    $changedFiles = @()
    if ($status) {
        $changedFiles = $status | ForEach-Object {
            $parts = $_ -split "\s+", 2
            [PSCustomObject]@{
                Status = $parts[0]
                File = $parts[1]
            }
        }
    }
    
    # Parse commit entries
    $commitEntries = @()
    if ($commits) {
        $commitEntries = $commits | ForEach-Object {
            $parts = $_ -split "\|"
            [PSCustomObject]@{
                Hash = $parts[0].Substring(0, 7)
                Message = $parts[1]
                Date = $parts[2]
                Author = $parts[3]
            }
        }
    }
    
    return [PSCustomObject]@{
        HasActivity = ($commitEntries.Count -gt 0) -or $hasUncommitted
        Commits = $commitEntries
        HasUncommitted = $hasUncommitted
        ChangedFiles = $changedFiles
        UncommittedFiles = $status
    }
}

# Function to categorize activity
function Get-ActivityCategory {
    param([string]$FileName, [string]$CommitMessage)
    
    $message = $CommitMessage.ToLower()
    $file = $FileName.ToLower()
    
    # Check commit message first
    if ($message -match "fix|bug|debug|repair|correct") { return "debugging" }
    if ($message -match "refactor|restructure|clean|move|rename") { return "refactoring" }
    if ($message -match "doc|readme|comment|agreement|guide") { return "documentation" }
    if ($message -match "test|spec|unittest") { return "testing" }
    if ($message -match "config|setting|json|yaml|xml") { return "configuration" }
    if ($message -match "add|implement|feature|create") { return "coding" }
    
    # Check file extension
    if ($file -match "\.md$|\.txt$") { return "documentation" }
    if ($file -match "\.json$|\.yml$|\.yaml$|\.xml$|\.config$") { return "configuration" }
    if ($file -match "\.test\.|\.spec\.|tests?/") { return "testing" }
    
    return "coding"
}

# Function to generate per-project daily note
function New-ProjectDailyNote {
    param(
        [string]$ProjectName,
        [PSCustomObject]$Activity
    )
    
    $projectFolder = Join-Path $ObsidianVault $ProjectName
    $dailyFolder = Join-Path $projectFolder "Daily"
    $notePath = Join-Path $dailyFolder "$Date.md"
    
    # Create folders if needed
    if (-not (Test-Path $dailyFolder)) {
        New-Item -ItemType Directory -Path $dailyFolder -Force | Out-Null
        Write-Host "${Green}Created folder:${Reset} $dailyFolder"
    }
    
    # Generate activity log entries
    $activityLog = ""
    $categories = @{}
    
    foreach ($commit in $Activity.Commits) {
        $category = Get-ActivityCategory -FileName "" -CommitMessage $commit.Message
        if (-not $categories.ContainsKey($category)) { $categories[$category] = @() }
        $categories[$category] += $commit
        
        $activityLog += @"

### $(Get-Date -Format "h:mm tt") - $($commit.Message)
- Commit: ``$($commit.Hash)``
- Author: $($commit.Author)
"@
    }
    
    if ($Activity.HasUncommitted) {
        $activityLog += @"

### $(Get-Date -Format "h:mm tt") - Uncommitted Changes
- Working directory has uncommitted modifications
"@
    }
    
    # Generate files table
    $filesTable = "| File | Status |`n|------|--------|"
    foreach ($file in $Activity.ChangedFiles) {
        $statusEmoji = switch ($file.Status) {
            "M" { "✏️ Modified" }
            "A" { "➕ Added" }
            "D" { "➖ Deleted" }
            "??" { "❓ Untracked" }
            default { $file.Status }
        }
        $filesTable += "`n| $($file.File) | $statusEmoji |"
    }
    
    # Get activity types
    $activityTypes = ($categories.Keys | ForEach-Object { "$_" }) -join ", "
    if (-not $activityTypes) { $activityTypes = "coding" }
    
    # Create note content
    $content = @"---
date: $Date
day_of_week: $DayOfWeek
project: $ProjectName
activity_type: [$activityTypes]
files_modified: $($Activity.ChangedFiles.Count)
commits: $($Activity.Commits.Count)
uncommitted: $($Activity.HasUncommitted)
---

# $ProjectName - $Date

## Summary
Worked on $ProjectName today. 

*TODO: Add your summary here*

## Activity Log
$activityLog

## Files Changed
$filesTable

$(if ($Activity.Commits.Count -gt 0) {@"
## Commits
" + ($Activity.Commits | ForEach-Object { "- ``$($_.Hash)`` - $($_.Message)" } | Out-String)
} else { "" })

## Key Decisions
- *TODO: Document important decisions*

## Next Steps
- [ ] *TODO: Add next steps*

## Links
- [[$ProjectName/SUMMARY|Project Summary]]
"@
    
    # Write note
    $content | Out-File -FilePath $notePath -Encoding UTF8
    Write-Host "${Green}Created note:${Reset} $notePath"
    
    return $notePath
}

# Function to generate master daily index
function New-MasterDailyNote {
    param([array]$ProjectActivities)
    
    $dailyNotesFolder = Join-Path $ObsidianVault "Daily-Notes"
    $notePath = Join-Path $dailyNotesFolder "$Date.md"
    
    # Create folder if needed
    if (-not (Test-Path $dailyNotesFolder)) {
        New-Item -ItemType Directory -Path $dailyNotesFolder -Force | Out-Null
        Write-Host "${Green}Created folder:${Reset} $dailyNotesFolder"
    }
    
    # Generate projects list
    $projectsList = ""
    $totalCommits = 0
    $totalFiles = 0
    
    foreach ($proj in $ProjectActivities) {
        $totalCommits += $proj.Activity.Commits.Count
        $totalFiles += $proj.Activity.ChangedFiles.Count
        
        $status = if ($proj.Activity.HasUncommitted) { " (uncommitted changes)" } else { "" }
        $projectsList += @"

### [[$($proj.Name)/Daily/$Date|$($proj.Name)]]$status
- **Focus:** *TODO: Add focus*
- **Files:** $($proj.Activity.ChangedFiles.Count) modified
- **Commits:** $($proj.Activity.Commits.Count)
- **Status:** *TODO: In progress / Complete / Testing*
"@
    }
    
    # Create note content
    $content = @"---
date: $Date
day_of_week: $DayOfWeek
total_projects: $($ProjectActivities.Count)
total_commits: $totalCommits
total_files_changed: $totalFiles
---

# Daily Note - $DayName, $Date

## Overview
*TODO: Brief summary of today's work*

## Projects Worked On
$projectsList

## Time Distribution
- *TODO: Track time per project*

## Daily Reflection
*TODO: What went well? Any blockers? Insights?*

## Tomorrow's Priorities
1. [ ] *TODO: Priority 1*
2. [ ] *TODO: Priority 2*
3. [ ] *TODO: Priority 3*

## Quick Links
- [[Weekly-Review/$(Get-Date -Format "yyyy-\W" + (Get-Date).DayOfYear / 7)|This Week's Review]]
- [[Monthly-Review/$(Get-Date -Format "yyyy-MM")|This Month's Review]]
"@
    
    # Write note
    $content | Out-File -FilePath $notePath -Encoding UTF8
    Write-Host "${Green}Created master note:${Reset} $notePath"
    
    return $notePath
}

# Function to create summary stub for new projects
function New-ProjectSummaryStub {
    param([string]$ProjectName, [string]$ProjectPath)
    
    $projectFolder = Join-Path $ObsidianVault $ProjectName
    $summaryPath = Join-Path $projectFolder "SUMMARY.md"
    
    if (-not (Test-Path $projectFolder)) {
        New-Item -ItemType Directory -Path $projectFolder -Force | Out-Null
    }
    
    if (-not (Test-Path $summaryPath)) {
        $content = @"# $ProjectName - Project Summary

## Overview
*TODO: Add project description*

## Technology Stack
- Language: *TODO*
- Framework: *TODO*
- Platform: *TODO*

## Key Files
| File | Purpose |
|------|---------|
| *TODO* | *TODO* |

## Links
- [[$ProjectName/Daily/|Daily Notes]]
- Source: ``$ProjectPath``

---

*Auto-generated stub - $(Get-Date -Format "yyyy-MM-dd")*
"@
        $content | Out-File -FilePath $summaryPath -Encoding UTF8
        Write-Host "${Yellow}Created summary stub:${Reset} $summaryPath"
    }
}

# Main execution
Write-Host "${Blue}Scanning for projects...${Reset}"

# Find all git repositories
$projects = Get-ChildItem -Path $ProjectsRoot -Directory | Where-Object {
    Test-Path (Join-Path $_.FullName ".git")
}

Write-Host "Found $($projects.Count) git repositories"
Write-Host ""

$activeProjects = @()

# Process each project
foreach ($project in $projects) {
    $projectName = $project.Name
    $projectPath = $project.FullName
    
    Write-Host "Checking ${Blue}$projectName${Reset}..."
    
    $activity = Get-GitActivity -ProjectPath $projectPath
    
    if ($activity -and $activity.HasActivity) {
        Write-Host "  ${Green}Activity detected:${Reset} $($activity.Commits.Count) commits, $($activity.ChangedFiles.Count) files"
        
        # Ensure Obsidian folder exists
        $obsidianProjectFolder = Join-Path $ObsidianVault $projectName
        if (-not (Test-Path $obsidianProjectFolder)) {
            New-ProjectSummaryStub -ProjectName $projectName -ProjectPath $projectPath
        }
        
        # Generate daily note
        $notePath = New-ProjectDailyNote -ProjectName $projectName -Activity $activity
        
        $activeProjects += [PSCustomObject]@{
            Name = $projectName
            Path = $projectPath
            Activity = $activity
            NotePath = $notePath
        }
    } else {
        Write-Host "  No activity"
    }
}

Write-Host ""
Write-Host "${Blue}Generating master daily index...${Reset}"

# Generate master daily note
if ($activeProjects.Count -gt 0) {
    $masterNotePath = New-MasterDailyNote -ProjectActivities $activeProjects
    Write-Host ""
    Write-Host "${Green}✓ Generated daily notes for $($activeProjects.Count) projects${Reset}"
    Write-Host "  Master note: $masterNotePath"
    foreach ($proj in $activeProjects) {
        Write-Host "  - $($proj.Name): $($proj.NotePath)"
    }
} else {
    Write-Host "${Yellow}No activity detected in any projects${Reset}"
    
    # Still create a blank daily note
    $dailyNotesFolder = Join-Path $ObsidianVault "Daily-Notes"
    $notePath = Join-Path $dailyNotesFolder "$Date.md"
    
    if (-not (Test-Path $dailyNotesFolder)) {
        New-Item -ItemType Directory -Path $dailyNotesFolder -Force | Out-Null
    }
    
    $content = @"---
date: $Date
day_of_week: $DayOfWeek
total_projects: 0
total_commits: 0
total_files_changed: 0
---

# Daily Note - $DayName, $Date

## Overview
No tracked activity in project repositories today.

## Notes
- *Add any manual notes here*
- *Non-coding activities*
- *Research, planning, etc.*

## Tomorrow's Priorities
1. [ ] *TODO*
"@
    
    $content | Out-File -FilePath $notePath -Encoding UTF8
    Write-Host "${Green}Created blank daily note:${Reset} $notePath"
}

Write-Host ""
Write-Host "${Blue}=== Complete ===${Reset}"
Write-Host "Open Obsidian to view your daily notes!"
