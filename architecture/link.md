# Standard Operating Procedure: API Connectivity

## Purpose
Verify all external connections work before building automation logic.

## Prerequisites
- .env file created with all required keys
- [[discovery\|discovery.md]] completed
- List of all required APIs from discovery phase

## Inputs
| Source | File/Location | Section/Scope | Why |
|--------|--------------|---------------|-----|
| Blueprint | [[../stages/01-blueprint/output/blueprint\|Blueprint]] | Full file | APIs to connect |
| Credentials | .env | Environment vars | Auth tokens |

## Outputs
| Artifact | Location | Format |
|----------|----------|--------|
| Link Report | [[../stages/02-link/output/links\|stages/02-link/output/links.md]] | Obsidian Markdown |
| Verified Tools | [[../tools/\|tools/]] | Python scripts |

## Tools
Create one verification script per API in [[../tools/\|tools/]]:
- `verify_[service].py`

## Procedure

### Step 1: Create .env Template

```bash
# .env - DO NOT COMMIT THIS FILE
# Copy .env.example to .env and fill in values

# API Keys
SERVICE_API_KEY=your_key_here
SERVICE_API_SECRET=your_secret_here

# Webhooks
SLACK_WEBHOOK_URL=https://hooks.slack.com/services/...

# Database
DB_CONNECTION_STRING=

# Other Config
TIMEOUT_SECONDS=30
MAX_RETRIES=3
```

See [[../.env.example\|.env.example]] for full template.

### Step 2: Build Handshake Scripts

Each `tools/verify_[service].py` should:
1. Load credentials from .env
2. Make minimal API call (e.g., GET /status or GET /me)
3. Print success/failure with response code
4. Exit with code 0 on success, 1 on failure

Template location: [[../tools/_template\|tools/_template.py]]

### Step 3: Run All Verifications

```bash
# Run all verification scripts
python tools/verify_service1.py && \
python tools/verify_service2.py && \
python tools/verify_service3.py
```

If any fail:
1. Check credentials in .env
2. Verify API service status
3. Check network/firewall
4. Document issue in [[../progress\|progress.md]]

### Step 4: Document Quirks

Some APIs have quirks. Document them in [[../findings\|findings.md]]:
- Rate limits (calls per minute/hour)
- Special headers required
- Auth token expiration
- Sandbox vs production endpoints

## Edge Cases

| Scenario | Action |
|----------|--------|
| API rate limited during testing | Add delays between calls |
| Token expires | Implement refresh logic in SOP |
| Service down | Document retry logic, set expectations |

## Gate Criteria

Proceed to [[../stages/03-architect/CONTEXT\|Phase 3: Architect]] ONLY when:
- [ ] All APIs responding with 200 OK
- [ ] Credentials verified
- [ ] Rate limits documented
- [ ] Quirks captured in [[../findings\|findings.md]]

## Related SOPs
- [[discovery\|discovery.md]] - Requirements gathering
- [[../stages/02-link/CONTEXT\|Stage 02: Link]] - Stage context

## Update Log

| Date | Change | Reason |
|------|--------|--------|
| 2026-03-11 | Created | Initial SOP for B.L.A.S.T. protocol |
