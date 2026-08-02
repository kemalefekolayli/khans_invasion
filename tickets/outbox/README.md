# Outbox — Agent Results

When you (an agent) finish implementing a ticket from `tickets/inbox/`, write your result here.

## Naming Convention
Name your result file the same as the ticket, e.g. `T0.1.md`.

## Result File Format

```markdown
# RESULT: T0.1 — Initialize Git Repo & Connect to GitHub

## Status
DONE / PARTIAL / BLOCKED

## Summary
[What you did, in 2-5 sentences]

## Verification
- [ ] Done When condition 1 met (with evidence: command output, file paths)
- [ ] Done When condition 2 met
- [ ] ...

## Manual Check
[YES/NO] — [If YES, what the human needs to verify]

## Files Changed
- [list of files created/modified]

## Branch
[feature branch name]

## Notes / Issues
[Anything the planner should know, e.g. decisions you made, problems encountered]
```

## Workflow
1. Planner writes ticket to `tickets/inbox/T0.1.md`
2. Agent reads the ticket, implements it on the feature branch
3. Agent writes result to `tickets/outbox/T0.1.md`
4. Planner reviews the diff, checks for redundant code, approves merge or sends feedback