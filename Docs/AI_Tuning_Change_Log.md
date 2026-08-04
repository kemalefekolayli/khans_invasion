# AI Tuning Change Log

Branch: `codex/ai-simulation-tuning`.

## AI tempo and force commitment

| Setting | Previous | Candidate |
| --- | ---: | ---: |
| AI starting army size | 100 | 140 |
| Min troops before attack | 500 | 260 |
| Min troops before raid | 100 | 80 |
| Attack readiness ratio | 1.20 | 1.05 |
| Strong readiness ratio | 1.50 | 1.30 |
| Weak readiness ratio | 0.80 | 0.75 |
| Max armies moved per nation per turn | 3 | 2 |
| Max attack orders per nation per turn | 2 | 1 |
| Max army size | 1000 | 650 |

## Aggression and target selection

| Setting | Previous | Candidate |
| --- | ---: | ---: |
| Aggression mean | default | 3.80 |
| Aggression standard deviation | default | 1.25 |
| Aggression range | default | 1–6 |
| Strong aggression bonus | default | 1 |
| Weak aggression penalty | default | 1 |
| Scout aggression level | default | 2 |
| Attack aggression level | default | 3 |
| Raid aggression level | 3 | 2 |
| Siege aggression level | 6 | 5 |
| Player-neighbor strength weight | 0.35 | 0.50 |
| Player-target score multiplier | 0.35 | 0.60 |
| Enemy weakness target weight | 50 | 55 |
| Enemy richness target weight | 1.00 | 1.25 |
| Capital province attack bonus | 100 | 125 |
| Border province attack bonus | 25 | 30 |
| War-intent diagnostics | off | on |

## Raids and conquest

| Setting | Previous | Candidate |
| --- | ---: | ---: |
| Raid readiness ratio | 0.75 | 0.70 |
| AI raid effectiveness | 0.40 | 0.55 |
| Allow AI raids | default | enabled |
| Raid fortress province | default | disabled |
| Raids before siege consideration | 2 | 3 |
| Raid pressure memory turns | 6 | 8 |
| Raid-to-siege aggression minimum | default | 4 |
| Conquest province-ratio threshold | 0.30 | 0.25 |
| Conquest minimum province count | 5 | 3 |
| Conquest readiness ratio | 0.90 | 1.00 |

## Economy, recruitment, and development

| Setting | Previous | Candidate |
| --- | ---: | ---: |
| AI starting treasury | 300 | 260 |
| Recruitment gold reserve | 100 | 90 |
| AI recruit population fraction | 0.18 | 0.12 |
| AI recruit cap per province | 350 | 220 |
| Allow AI field recruitment | default | enabled |
| Buildings per nation per turn | 5 hard-coded | 2 inspector field |
| Development gold reserve | none | 80 inspector field |
| Base barracks score | 15 | 18 |
| Base housing score | 10 | 12 |
| Base fortress score | 5 | 8 |
| Base farm score | 12 | 16 |
| Base trade score | 14 | 13 |
| Population saturation threshold | 0.90 | 0.82 |
| No-army barracks multiplier | 5, unused | 3.5, active |
| Distance-to-capital penalty | 2.00 | 1.20 |
| Province importance weight | 0.50 | 0.75 |

## Test surface added

- `SimulationRunner` now accepts seed, turn count, interval, timeout, report name, and mode via command line.
- `SimulationController` stores those requested settings and records the seed in every report.
- `Tools/RunAISimulationSweep.ps1` runs twelve deterministic 80-turn AI-only games serially and writes per-seed reports plus a CSV summary under `Logs/AIExperiments` after Unity releases the project lock.
