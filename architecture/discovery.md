# Standard Operating Procedure: Discovery & Requirements

## Purpose
Document the discovery phase for all B.L.A.S.T. automation projects.

## Inputs
| Source | File/Location | Section/Scope | Why |
|--------|--------------|---------------|-----|
| User Request | Ongoing session | Current chat | Core requirements |
| Context Rules | [[../_config/brand-guidelines\|Brand Guidelines]] | General | Aesthetic/voice matching |
| Active Tasks | [[../MEMORY\|MEMORY.md]] | Active items | What are we already doing |

## Outputs
| Artifact | Location | Format |
|----------|----------|--------|
| Blueprint | [[../stages/01-blueprint/output/blueprint\|stages/01-blueprint/output/blueprint.md]] | Obsidian Markdown |
| Findings | [[../findings\|findings.md]] | Obsidian Markdown |

## Procedure

### Step 1: Answer Discovery Questions

**North Star:** What is the singular desired outcome?
- Document the ONE thing this automation must achieve
- Avoid scope creep - if there's more than one outcome, prioritize

**Integrations:** Which external services do we need?
- List every API, service, database, or webhook
- Verify API keys are available or can be obtained
- Note rate limits and quotas

**Source of Truth:** Where does primary data live?
- Identify the authoritative data source
- Note: "Primary" means if there's a conflict, this source wins

**Delivery Payload:** How and where should final results be delivered?
- Specify format (JSON, CSV, Slack message, etc.)
- Specify destination (URL, file path, API endpoint)
- Document any transformation requirements

**Behavioral Rules:** How should the system "act"?
- Tone of voice (if generating content)
- Constraints ("Never do X", "Always do Y")
- Edge case handling

### Step 2: Research

Search for:
- [ ] Similar implementations on GitHub
- [ ] Official API documentation
- [ ] Community tutorials or blog posts
- [ ] Known issues or limitations

Document findings in [[../findings\|findings.md]].

### Step 3: Define Data Schema

Before any code is written, define:

**Input Schema:**
```json
{
  "field_name": {
    "type": "string|number|boolean|array|object",
    "required": true|false,
    "description": "What this field represents",
    "source": "Where this comes from"
  }
}
```

**Output Schema:**
```json
{
  "field_name": {
    "type": "string|number|boolean|array|object",
    "description": "What this field represents",
    "destination": "Where this goes"
  }
}
```

Update [[../claude\|claude.md]] with the schema.

### Step 4: Approval Gate

Before proceeding to [[../stages/02-link/CONTEXT\|Phase 2: Link]]:
- [ ] All 5 discovery questions answered
- [ ] Data schema defined and documented
- [ ] Research findings captured
- [ ] User has confirmed the blueprint

## Edge Cases

| Scenario | Action |
|----------|--------|
| User unclear on requirements | Ask clarifying questions, document assumptions |
| API unavailable | Prototype with mock data, note limitations |
| Scope too large | Break into smaller B.L.A.S.T. projects |

## Related SOPs
- [[link\|link.md]] - API verification procedures
- [[../stages/01-blueprint/CONTEXT\|Stage 01: Blueprint]] - Stage context

## Update Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-03-11 | Created | Initial SOP for B.L.A.S.T. protocol |
