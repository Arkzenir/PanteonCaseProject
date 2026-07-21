# CONVENTIONS.md — Project Structure, Naming, and Code Standards

> The agent rewrites the **Project-specific overrides** section at the bottom during brief
> ingestion. Everything above it is the baseline standard and only changes when the brief
> demands it.

## Folder structure
All project-authored content lives under a single root to separate it from imported assets:

```
Assets/
  _Project/
    Art/                  (Models/, Materials/, Textures/, Animations/, Shaders/)
    Audio/                (Music/, SFX/, Mixers/)
    Prefabs/              (grouped by feature: Player/, Enemies/, UI/, Environment/ ...)
    Scenes/               (shipping scenes at root; Sandbox/ for test scenes)
    Scripts/
      Runtime/            (grouped by feature/domain, mirrors namespaces)
      Editor/             (custom inspectors, tooling; Setup/ for one-click wiring scripts)
      Tests/
        EditMode/
        PlayMode/
    Settings/             (ScriptableObject config assets, Input actions, render/quality)
    UI/                   (UXML/USS or sprite atlases, fonts)
  Plugins/                (third-party, only with approval)
```

- Empty folders are never committed; create folders only when first used.
- Nothing project-authored goes outside `_Project` except `Plugins/`.

## Assembly definitions
- `ProjectName.Runtime` on `Scripts/Runtime/`
- `ProjectName.Editor` on `Scripts/Editor/` (editor-only, references Runtime)
- `ProjectName.Tests.EditMode` / `ProjectName.Tests.PlayMode` on the test folders
- Keep `Auto Referenced` sensible; no cyclic references. Split further per-feature only if
  the project grows enough to justify it.

## Naming
| Thing | Convention | Example |
|---|---|---|
| C# class / file | PascalCase, one public type per file | `EnemySpawner.cs` |
| Interface | `I` prefix | `IDamageable` |
| Namespace | `ProjectName.Feature`, mirrors folder | `CaseGame.Combat` |
| Private field | `_camelCase` | `_currentHealth` |
| Serialized private field | `[SerializeField] private` + camelCase | `[SerializeField] private float moveSpeed;` |
| Public property / method | PascalCase | `CurrentHealth`, `ApplyDamage()` |
| Constant / static readonly | PascalCase | `MaxEnemies` |
| Event | PascalCase, past/present intent | `HealthChanged`, `Died` |
| Scene | PascalCase noun | `Boot.unity`, `MainMenu.unity`, `Gameplay.unity` |
| Prefab | PascalCase; variants suffixed | `Enemy_Grunt.prefab`, `Enemy_Grunt_Elite.prefab` |
| ScriptableObject asset | Type-prefixed | `Config_PlayerMovement.asset`, `Wave_Level1.asset` |
| Input actions | `ProjectName.inputactions` | |
| Materials / textures | `M_`, `T_` prefixes | `M_Enemy_Grunt`, `T_Enemy_Grunt_Albedo` |

## Code standards
- **Humble MonoBehaviours.** MonoBehaviours handle Unity lifecycle, serialization, and
  scene references only; decision logic lives in plain C# classes injected or constructed
  by them, unit-testable without the engine.
- **Configuration in ScriptableObjects**, never hardcoded tuning values. Designers must be
  able to tune without code changes.
- **Decoupling:** communicate across systems via C# events, or ScriptableObject event
  channels when designer-facing wiring is valuable. No `GameObject.Find`, no string-based
  `SendMessage`, no cross-system singletons unless the brief mandates them. If a service
  locator or singleton is brief-mandated, isolate it behind an interface.
- **References:** assign via inspector or explicit initialization. `GetComponent` cached in
  `Awake`. `RequireComponent` where a dependency is structural.
- **Update discipline:** no per-frame allocations, no empty Unity messages, no polling that
  an event can replace. Pool anything spawned repeatedly.
- **Input:** Unity Input System (new), via generated C# wrapper or `PlayerInput`, per brief.
- **Async:** coroutines by default; `async/await`/UniTask only with approval or brief mandate.
- **Null checks on Unity objects** use implicit bool / `ReferenceEquals` correctly; never `?.`
  on UnityEngine.Object.
- Braces always, `var` only when the type is obvious, expression-bodied members for trivial
  accessors, XML doc comments on public APIs of non-obvious systems.

## Scene composition
- `Boot` scene (entry point, persistent systems) → loads content scenes additively or via
  transitions, unless the brief specifies a different flow.
- Scene hierarchy uses top-level organizer objects: `--- SYSTEMS ---`, `--- ENVIRONMENT ---`,
  `--- GAMEPLAY ---`, `--- UI ---`. No loose objects at root.
- Everything instantiated at runtime parents under a designated container.

## Version control hygiene
- `.gitignore` for Unity (Library, Temp, Logs, UserSettings, obj, .idea excluded).
- Visible meta files + force text serialization (assumed already set; flag if not).
- Commit per feature with imperative messages: `Add wave spawning system`.

---

## Project-specific overrides (rewritten per brief)
_None yet. Populated during brief ingestion. Each entry: what the brief mandates, what
baseline rule it replaces, and a pointer to the brief section._
