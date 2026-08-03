# Khan's Invasion — Agent Guidelines

## Role and workflow

- The lead agent owns design, decomposition, integration, review, and user communication.
- Implementation agents receive one narrowly-scoped ticket. Complete only that ticket; do not broaden the task or refactor unrelated systems.
- Before editing, inspect the relevant C# code and its callers. After editing, run the smallest relevant verification available and report the files changed, behavior implemented, and any unverified Unity-editor step.

## Unity project rules

- Gameplay source of truth is `Assets/code`. Prefer small, modular C# components with one clear responsibility.
- Never edit, create, or review `.meta` files unless the ticket explicitly requires a Unity asset move/creation. Do not include `.meta` files in code-review findings.
- Preserve Unity serialized field names when possible. New designer-tunable behavior must use clearly labelled `[Header]`, `[SerializeField]` inspector fields and safe defaults.
- Use `GameEvents` for cross-system gameplay notifications rather than introducing hidden coupling. Subscribe in `OnEnable` and unsubscribe in `OnDisable`.
- Use `GameLog` and `GameLogCategory` for game diagnostics; do not add casual `Debug.Log` calls to production code.
- Respect the Input System and UI input blocking. Player actions must remain gated by `TurnManager` where applicable.
- When Unity MCP tools are available, use them to inspect and verify scene state. If they are not callable in the current session, state that limitation and use read-only project inspection instead.

## Change discipline

- Do not edit scene YAML, prefabs, ProjectSettings, packages, generated files, or unrelated files unless the ticket explicitly calls for it.
- Do not perform destructive Git commands. Do not commit, push, stage, or switch branches unless the lead agent explicitly asks.
- Keep comments and identifiers in English. Preserve the existing project style around the edited code.
