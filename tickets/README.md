# Khan's Invasion — Ticket System

This folder is the coordination layer between the **Planner** (an opencode agent) and **Engineer Agents** (opencode subagents orchestrated by the planner).

## Structure

```
tickets/
├── ticket_board.html   ← Visual board: edit tickets, reorder, track status, manual-check flags
├── README.md           ← This file
├── AGENT_KICKSTARTER.md ← (legacy Cline runner — still valid if you run Cline)
├── inbox/              ← Planner drops ticket files here for agents to pick up
│   └── TX.Y.md
└── outbox/             ← Agents write their results here
    └── README.md       ← Result format instructions
```

## Orchestration (opencode model)

- **Product Owner (you)** — hands the planner tickets with requirements.
- **Planner (opencode main agent)** — prioritizes, breaks down tickets, spawns engineers, runs the reviewer, merges to `main`, tags versions.
- **Engineer** — `.opencode/agents/engineer.md` subagent. Implements one ticket on `feature/tX.Y-slug`, follows the anti-slop checklist, writes result to `tickets/outbox/TX.Y.md`.
- **Reviewer** — `.opencode/agents/reviewer.md` subagent. Read-only PRUNE GATE review of every diff before merge.
- **Tester** — `.opencode/agents/tester.md` subagent. Runs Unity batchmode compile checks + the `SimulationController` harness, reports pass/fail evidence.

Restart opencode after editing `.opencode/` files so the agents load.

## Workflow

1. **Planner** writes a ticket to `tickets/inbox/TX.Y.md`
2. **You** open a Cline agent in Act mode and tell it:
   > "Read `tickets/inbox/TX.Y.md` and implement it. When done, write your result to `tickets/outbox/TX.Y.md` following the format in `tickets/outbox/README.md`."
3. **Agent** implements the ticket on the feature branch, commits, and writes the result to the outbox
4. **Planner** reads the outbox, reviews the diff against `main`, checks for redundant code, and either approves the merge or sends feedback

## Ticket Format

Every ticket in `inbox/` follows this structure:

- **Context** — why this ticket exists, what it touches
- **Must Have** — concrete, testable requirements
- **Nice to Have** — optional enhancements
- **Done When** — the conditions the test agent verifies
- **Manual Check** — YES/NO flag: whether a human must visually verify
- **Files Touched** — expected files
- **Branch** — the feature branch name

## Branching Model

- `main` — integration branch; every merged ticket is a tagged version
- `prod` — stable playable builds; promoted from `main` after test-agent signoff
- `feature/tX.Y-slug` — per-ticket branches created by engineer agents

## Pruning Gate

Before any merge, the Planner reviews the diff specifically hunting for:
- Duplicated methods (e.g., legacy AI code paths)
- Unused public methods / dead events
- Stale settings / assets
- Code that isn't necessary — it gets deleted in the same ticket

## Versioning

- `main` — integration branch; every merged ticket is a tagged version (`vX.Y`)
- `prod` — stable playable builds; promoted from `main` only after tester signoff
- `feature/tX.Y-slug` — per-ticket branches created by engineer agents
- After each merge: `git tag vX.Y`, update `tickets/outbox/SUMMARY.md`