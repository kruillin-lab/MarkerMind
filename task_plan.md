# Task Plan

## Overview
**Project:** [Name]
**Created:** 2026-03-11

---

## Phase Breakdown

### Phase 0: Initialization
**Goal:** Set up project infrastructure

**Tasks:**
- [ ] Create memory files
- [ ] Define data schema
- [ ] Answer discovery questions

**Deliverable:** Project is ready for [[stages/01-blueprint/CONTEXT\|Blueprint phase]]

---

### Phase 1: Blueprint (B) - Vision & Logic
**Goal:** Define WHAT we're building and WHY

**Discovery Questions:**
1. **North Star:** What is the singular desired outcome?
2. **Integrations:** Which external services do we need? Are keys ready?
3. **Source of Truth:** Where does the primary data live?
4. **Delivery Payload:** How and where should the final result be delivered?
5. **Behavioral Rules:** How should the system "act"? (Tone, constraints, "Do Not" rules)

**Research:**
- [ ] Search GitHub repos for similar implementations
- [ ] Check API documentation
- [ ] Identify rate limits and constraints

**Deliverable:** Approved blueprint with data schema in [[stages/01-blueprint/output/blueprint\|stages/01-blueprint/output/blueprint.md]]

---

### Phase 2: Link (L) - Connectivity
**Goal:** Verify all connections work

**Tasks:**
- [ ] Create .env file
- [ ] Test each API connection
- [ ] Build handshake verification scripts
- [ ] Document any auth quirks

**Deliverable:** All APIs responding correctly, documented in [[stages/02-link/output/links\|stages/02-link/output/links.md]]

---

### Phase 3: Architect (A) - The Build
**Goal:** Build the actual automation

**Layer 1 - SOPs:**
- [ ] Write technical SOPs in [[architecture/\|architecture/]]
- [ ] Document inputs, outputs, edge cases
- [ ] Define error handling procedures

**Layer 3 - Tools:**
- [ ] Build atomic Python scripts in [[tools/\|tools/]]
- [ ] Each tool does ONE thing
- [ ] All tools are testable independently

**Integration:**
- [ ] Connect tools per SOPs
- [ ] Test full workflow end-to-end

**Deliverable:** Working automation system, logged in [[stages/03-architect/output/architect-log\|stages/03-architect/output/architect-log.md]]

---

### Phase 4: Stylize (S) - Refinement
**Goal:** Polish the output

**Tasks:**
- [ ] Format outputs (Slack blocks, Notion layouts, HTML)
- [ ] Apply UI/UX if there's a frontend
- [ ] Get user feedback
- [ ] Iterate based on feedback

**Deliverable:** Professional, polished results in [[stages/04-stylize/output/stylize-log\|stages/04-stylize/output/stylize-log.md]]

---

### Phase 5: Trigger (T) - Deployment
**Goal:** Go live

**Tasks:**
- [ ] Move logic to production cloud
- [ ] Set up cron/webhooks/listeners
- [ ] Finalize maintenance log
- [ ] Document runbook for operators

**Deliverable:** Automated system running in production, report in [[stages/05-trigger/output/trigger-report\|stages/05-trigger/output/trigger-report.md]]

---

## Current Phase
**Status:** [Not Started]

**Next Action:** [TBD]

---

## Blockers

| Issue | Impact | Resolution |
|-------|--------|------------|
| None | - | - |
