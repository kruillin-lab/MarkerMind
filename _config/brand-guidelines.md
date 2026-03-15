# Brand Guidelines

When generating any user-facing documentation, readme files, UI mockups locally, or presenting structural plans within the vault:

1. **Obsidian Native**: Format files using Obsidian's markdown parser syntax. 
   - Use `[[File Name]]` to reference other markdown files or notes inside the workspace.
   - Attach tags via `#tag/subtag` in YAML frontmatter or inline format.

2. **Structure Over Decoration**: Rely on clearly defined folder structures or header structures (`H1-H6`) over fancy inline HTML tricks so that dataview queries and reference links work natively.

3. **FFXIV / Dalamud Specific**: If referencing game-specific features (Dalamud, Lua, .NET projects), ensure terminology connects precisely to established game data definitions and the API interface names from Dalamud.

4. **Consistency**: Do not fragment knowledge. If a topic is discussed, check if a wiki link `[[Topic]]` already exists. If not, link it appropriately so that the empty node is created as a placeholder.

## B.L.A.S.T. Protocol Standards

### File Naming
- Markdown: kebab-case (e.g., `my-file-name.md`)
- Python: snake_case (e.g., `my_script.py`)
- Directories: lowercase, no spaces

### Obsidian Links
- Use wikilinks: `[[filename]]` or `[[path/to/file\|Display Text]]`
- Always escape the pipe: `\|`
- Use relative paths from current file
- Example: `[[../../_config/brand-guidelines\|Brand Guidelines]]`

### Headers
- H1: `# Main Title` (only one per file)
- H2: `## Section`
- H3: `### Subsection`

### Tables
Use for structured data:
```markdown
| Column 1 | Column 2 |
|----------|----------|
| Data     | Data     |
```

### Code Blocks
- Always specify language: ```python
- Include comments explaining "why", not "what"
