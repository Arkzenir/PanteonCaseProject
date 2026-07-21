# ARCHITECTURE.md — System Architecture

> Kept current as features land. This file is the map an evaluator (or the agent in a fresh
> session) uses to understand the whole project.

## 1. High-level overview
A 2D top-down strategy demo built on a discrete grid Game Board. The player places
buildings (Barracks, Power Plant) from a pooled, infinitely-scrolling Production Menu,
produces soldiers from Barracks, and commands soldiers with left-click-select /
right-click-move-or-attack. Movement uses a custom grid A* pathfinder that routes around
building obstacles. Architecture is MVC-flavored and data-driven: building/unit *stats* live
in ScriptableObject definitions, *creation* goes through Factories, *cross-system
communication* goes through C# events, and MonoBehaviours stay humble (Views/Controllers)
while plain C# classes hold the testable logic (Models) — chosen because the brief explicitly
mandates Factory, Singleton, MVC, Events, Object Pooling, and A*, and this is the
straightforward best-practice way to satisfy all of them together without contradicting each
other.

## 2. Module map

| Module | Namespace | Responsibility | Depends on |
|---|---|---|---|
| Core | `CaseGame.Core` | Bootstrapping, the one sanctioned Singleton (`GameManager`), scene/system lifecycle | Events |
| Grid | `CaseGame.Grid` | Grid data model: cell size/bounds (fully designer-editable via a `GridDefinition` SO — no fixed pixel size, set once art is chosen), occupancy, world↔cell conversion, placement validity queries | Core |
| Entities | `CaseGame.Entities` | `GameEntityDefinition` (abstract SO: name/sprite/footprint/maxHealth) and `GameEntityBase` (abstract MonoBehaviour: owns a `Health`, implements `IDamageable`, sprite assignment, death callback) — shared by Buildings and Units so neither duplicates this shape (see decisions log #22) | Combat |
| Buildings | `CaseGame.Buildings` | `BuildingBase` (: `GameEntityBase`), `Barracks`, `PowerPlant`, `BuildingDefinition` (: `GameEntityDefinition`), `BuildingFactory` | Entities, Pooling, Units |
| Units | `CaseGame.Units` | `SoldierBase` (abstract, future: `GameEntityBase`), `Soldier1/2/3`, `UnitDefinition` (: `GameEntityDefinition`), `UnitFactory` | Entities, Grid, Pathfinding, Events |
| Combat | `CaseGame.Combat` | `IDamageable`, `Health` (HP, damage, death via plain per-instance C# events — see decisions log #19) | — |
| Pathfinding | `CaseGame.Pathfinding` | `AStarPathfinder`: static, tested 8-directional grid A* with corner-cutting prevention ("wander around buildings," GI-7/8). No request-queue/coroutine layer yet — added if a real caller (Units movement) shows it's needed | Grid |
| Placement | `CaseGame.Placement` | Ghost/preview while placing a building, valid/invalid (red) area feedback, commit-to-grid | Grid, Buildings, Events |
| Selection | `CaseGame.Selection` | Left-click select, right-click move/attack command interpretation, raises intent events | Units, Buildings, Events |
| Pooling | `CaseGame.Pooling` | Generic `ObjectPool<T>` used by the scroll view and by frequently spawned/destroyed units/buildings | — |
| Events | `CaseGame.Events` | Lightweight C# event channels (plain events and/or SO event channels) connecting the above without direct references | — |
| UI.Production | `CaseGame.UI.Production` | Infinite, pooled scroll view listing producible buildings, iterated **generically off a `BuildingDefinition` list** (no per-type UI branches); "produce" triggers Factory + Placement | Pooling, Buildings, Events |
| UI.Info | `CaseGame.UI.Info` | Information Panel: shows selected building/unit image + producible-unit images, driven by each `BuildingDefinition`'s own `UnitDefinition` list | Events, Buildings, Units |
| UI.MainMenu | `CaseGame.UI.MainMenu` | Main Menu screen: Play button → loads `Gameplay.unity` | Core |
| UI.Settings | `CaseGame.UI.Settings` | Settings screen: resolution / display-mode selection, applied via `Screen.SetResolution` | Core |

MVC mapping: **Model** = `BuildingDefinition`/`UnitDefinition` (SO data) + `Health`/combat
state (plain C#); **View** = MonoBehaviours rendering sprites/UI only; **Controller** =
MonoBehaviours in Selection/Placement/UI.* that translate input and model changes into view
updates, publishing/subscribing via Events rather than reaching into each other directly.

## 3. Data flow & communication
- **Definitions (Model data):** `BuildingDefinition` and `UnitDefinition` ScriptableObjects
  hold name, sprite, footprint dimensions, max HP, and (for buildings) damage-per-attack or
  produced-unit list. Designers add new building/unit types by adding a new SO asset +
  minimal subclass, not by editing unrelated systems (Open Question #1 in BRIEF.md).
- **Creation:** `BuildingFactory`/`UnitFactory` (Factory pattern) take a `Definition` and
  return a configured, pooled instance. No `new` scattered across controllers.
- **Selection → Info Panel:** `Selection` raises a `SelectionChanged` event carrying the
  selected object's definition/state; `UI.Info` subscribes and renders — no direct reference
  from selection logic to UI.
- **Move/Attack commands:** right-click while soldiers are selected raises a `MoveCommand` or
  `AttackCommand` event (context-dependent on what's under the cursor); the soldier's
  controller consumes it, requests a path from `Pathfinding`, and moves along it via
  coroutine, or resolves an attack via `Combat`.
- **HP/Death:** `Health.Damaged`/`Health.Died` events drive destruction (return to pool)
  without the attacker needing to know about pooling or scene cleanup.
- **Placement validity:** `Placement` queries `Grid` for occupancy/bounds each frame while a
  ghost is active, and toggles the red/valid visual purely from that query — no coupling to
  specific building types.
- No save/load path (out of scope per BRIEF.md).

## 4. Scene & prefab composition
- `Boot.unity` — persistent systems only: `GameManager` (Singleton), event channels, pools.
  Loads `MainMenu.unity` on start.
- `MainMenu.unity` — Play button → loads `Gameplay.unity`; Settings screen for
  resolution/display-mode (demonstrates GI-13, human-mandated requirement 19).
- `Gameplay.unity` — the actual demo: Game Board (grid), Production Menu (UI), Information
  Panel (UI). Hierarchy uses top-level organizers per CONVENTIONS.md: `--- SYSTEMS ---`,
  `--- ENVIRONMENT ---` (grid/board), `--- GAMEPLAY ---` (spawned buildings/units container),
  `--- UI ---` (Production Menu, Information Panel, Canvas).
- Key prefabs (added as each feature lands): `Assets/_Project/Prefabs/Buildings/Building_Barracks.prefab`
  and `Building_PowerPlant.prefab` (human-created after Report 009) — each a parent GameObject
  with `Visuals` (SpriteRenderer), `Hitbox` (collider, for future selection), and (Barracks
  only) `SpawnPoint` as children, per CONVENTIONS.md's per-prefab grouping convention. Still to
  come: `Soldier_1/2/3.prefab`, a Production Menu list-item prefab (pooled), ghost/preview
  prefab for placement.
- SO definition assets live under `Assets/_Project/ScriptableObjects/`: `GridDef_Default.asset`
  at the root, `Units/Buildings/BuildingDef_Barracks.asset` + `BuildingDef_PowerPlant.asset`
  under a shared `Units/` parent (human's own organization — Buildings and Troops defs both
  nest under `Units/`, a discussion point for a possible shared-base-class refactor, TBD).
- This section is updated feature-by-feature as prefabs are actually created — treat the
  above as the plan, not yet-built inventory (except the two building prefabs above, which
  now exist and are still mid-wiring pending an architecture discussion in progress).

## 5. Brief-mandated requirements checklist
Tracks BRIEF.md's "Hard requirements" 1–19 by number. No single numbered requirement is
fully done yet — see the implementation log below the table for foundation work already
landed that these requirements will build on.

**Implementation log:**
- Report 001 (`Docs/Reports/001_grid-foundation.md`): `GridDefinition` (SO), `GridModel`
  (plain C#, tested), `GridView` (Gizmo visualization) landed under
  `Assets/_Project/Scripts/Runtime/Grid/`. Foundation only — not yet wired into a scene, not
  yet consumed by Placement/Buildings/Pathfinding. No requirement below is checked off by
  this alone.
- Report 002 (`Docs/Reports/002_core-singleton.md`): `GameManager` (the brief-mandated
  Singleton, isolated behind `IGameManager`) landed under `Assets/_Project/Scripts/Runtime/Core/`.
  Handles cross-scene persistence and scene-transition entry point. Not yet wired into
  `Boot.unity` (scene doesn't exist yet). Automated tests dropped for this feature — see
  `ENVIRONMENT.md` note on EditMode `Awake` not firing reliably in batchmode; hand-tested
  instead.
- Report 003 (`Docs/Reports/003_gamemanager-root-persistence-fix.md`): fixed a bug the human
  found during Report 002's hand-test — `DontDestroyOnLoad` only accepts root GameObjects, but
  `GameManager` lives under the `--- SYSTEMS ---` organizer per CONVENTIONS.md, so it was
  throwing. Now targets `transform.root.gameObject`.
- Report 004 (`Docs/Reports/004_scene-reference-asset-picker.md`): added `SceneReference`
  (`CaseGame.Core`) — a reusable, Inspector-friendly scene-asset-picker wrapper — plus its
  `SceneReferenceDrawer` (first script under `Assets/_Project/Scripts/Editor/`, new
  `CaseGame.Editor` asmdef). `GameManager.firstScene` now uses it instead of a raw string
  field. Project convention going forward: prefer typed asset-picker wrappers over raw string
  fields for asset references (see `CONVENTIONS.md` overrides).
- Report 005 (`Docs/Reports/005_main-menu-and-settings.md`): implements requirement 19 — Main
  Menu (`MainMenuController`) with Play/Settings navigation and a Settings screen
  (`SettingsController` + tested `ResolutionOptionsBuilder`) for resolution/fullscreen, built on
  TextMeshPro (`TMP_Dropdown` for resolution selection) and the Input System package's
  `InputSystemUIInputModule` — both installed by the human specifically for this feature, with
  Input System also intended for gameplay input later. Also relocated `Boot.unity`/
  `Gameplay.unity` from `Assets/Scenes/` to `Assets/_Project/Scenes/` (human-approved hygiene
  fix) and added `MainMenu.unity` there; scene flow `Boot → MainMenu → Gameplay` is fully wired
  end-to-end (Build Settings, `GameManager`, `MainMenuController`), pending only the human's
  hand-test confirmation. (Redone in place per human instruction — the original legacy-UI/no
  packages version of this feature was superseded, not shipped as a separate feature.)
- Report 006 (`Docs/Reports/006_events.md`): landed the `Events` module foundation —
  `GameEvent` (parameterless, `[CreateAssetMenu]` SO signal channel), generic
  `GameEventChannel<T>` base for future typed payload channels, and `GameEventListener`
  (bridges a `GameEvent` to a designer-configured `UnityEvent`) — under
  `Assets/_Project/Scripts/Runtime/Events/`. Foundation only, like Grid/Core before it: no
  concrete payload channels exist yet since no gameplay types (BuildingDefinition, Health,
  etc.) exist to carry — those land alongside whichever future feature first needs one.
- Report 007 (`Docs/Reports/007_pooling.md`): landed the `Pooling` module foundation —
  `PrefabPool<T>` (generic, `T : Component`), a thin wrapper around Unity's built-in
  `UnityEngine.Pool.ObjectPool<T>` (confirmed available on `2021.3.45f2` by compile) handling
  instantiate/activate/deactivate/destroy so callers just `Get()`/`Release()`. Foundation only:
  no concrete pooled type (scroll-view list items, soldiers, buildings) exists yet — this
  lands alongside whichever future feature first needs one (Production Menu is next in line).
- Report 008 (`Docs/Reports/008_combat.md`): landed the `Combat` module foundation —
  `IDamageable` (contract) and `Health` (plain C# HP state, damage/clamp/death logic, tested)
  under `Assets/_Project/Scripts/Runtime/Combat/`. Foundation only: no soldier/building type
  exists yet to own a `Health` instance with the brief's actual numbers (requirements 9/10),
  and nothing yet calls `ApplyDamage` from an attack command (requirement 11) — those land
  with Units/Buildings/Selection. Refines the Phase-0 assumption that Combat depends on
  Events: `Damaged`/`Died` are plain per-instance C# events, not a shared `GameEventChannel<T>`
  (see decisions log #19).
- Report 009 (`Docs/Reports/009_buildings.md`): landed the `Buildings` module —
  `BuildingDefinition` (SO), `BuildingBase` (abstract, humble, implements `IDamageable` via a
  `Health` it owns), `Barracks` (adds a spawn point, GI-7), `PowerPlant` (no additions — only
  building type with no producible units, GI-6), and `BuildingFactory` (Factory pattern,
  pools instances via the already-built `PrefabPool<T>`) — all under
  `Assets/_Project/Scripts/Runtime/Buildings/`. Also added a minimal `UnitDefinition` (SO,
  data only) under `Assets/_Project/Scripts/Runtime/Units/` — a prerequisite for
  `BuildingDefinition`'s producible-units list (requirement 2); the full Units feature
  (`SoldierBase`, `Soldier1/2/3` behavior, `UnitFactory`) is still a separate future feature.
  Foundation-ish but one step closer to shippable: no scene has an actual placed building yet
  (that's Placement's job), and no requirement below is checked off — the human still needs
  to create the two concrete `BuildingDef_Barracks`/`BuildingDef_PowerPlant` assets and
  prefabs per the report's editor hookup checklist.
- Report 010 (`Docs/Reports/010_shared-entity-base.md`): extracted `GameEntityDefinition` and
  `GameEntityBase` (new `Entities` module) out of `BuildingDefinition`/`BuildingBase`, per the
  human's suggestion. `BuildingDefinition`/`UnitDefinition` now both extend
  `GameEntityDefinition`; `BuildingBase` now extends `GameEntityBase` (and just re-exposes
  `Definition` as the strongly-typed `BuildingDefinition`). Removes the field/logic
  duplication that existed between Buildings' and Units' definitions and bases, and gives the
  brief's mandated "Inheritance" requirement a real hierarchy to stand on ahead of the future
  `SoldierBase : GameEntityBase`. Human also hand-adjusted the project's actual folder/prefab
  structure after Report 009 (`ScriptableObjects/Units/{Buildings,Troops}/` instead of
  `Settings/`; prefabs use child GameObjects for `Visuals`/`Hitbox`/`SpawnPoint`) — recorded in
  `CONVENTIONS.md`.
- Report 011 (`Docs/Reports/011_pathfinding.md`): landed the `Pathfinding` module —
  `AStarPathfinder` (static, plain C#, tested): 8-directional grid A* over `GridModel`, with
  corner-cutting prevented (a diagonal step is rejected if either flanking orthogonal cell is
  blocked) so a path can't clip through a building's corner — this is the actual mechanism
  behind requirement 8's "wander around the buildings." No coroutine-based path-request queue
  yet — nothing consumes pathfinding until Units/Selection exist, so a queued/async layer
  would be speculative before a real caller reveals whether one's even needed.

- [ ] 1. Unity 2021 LTS, 2D, Windows build
- [ ] 2. Production Menu: Barracks, Power Plant, Soldier Units (+ extensible for more)
- [ ] 3. Building placement with invalid-area feedback; name/image/dimensions
- [ ] 4. Free, instant, unlimited production
- [ ] 5. Info Panel shows selected building image + producible unit images
- [ ] 6. Only Barracks produce units
- [ ] 7. Barracks spawn point for produced soldiers
- [ ] 8. Left-click select / right-click move; shortest path; routes around buildings
- [ ] 9. 3 soldier types, 10 HP each, damage 10/5/2
- [ ] 10. Building HP: Barracks 100, Power Plant 50
- [ ] 11. Right-click attack on unit/building
- [ ] 12. Destruction at 0 HP
- [ ] 13. Draw calls < 20 via batching/instancing
- [ ] 14. Multi-aspect-ratio/resolution support
- [ ] 15. Production Menu (pooled infinite scroll) / Game Board / Information Panel layout
- [ ] 16. Legible, standards-compliant code
- [ ] 17. Evaluation-grade scene/naming/folder hygiene
- [ ] 18. Edge cases considered
- [x] 19. Main Menu with Play button + Settings screen (resolution/display-mode) — Report 005, `Assets/_Project/Scenes/MainMenu.unity`; human hand-tested and confirmed working (2026-07-21)

## 6. Decisions log

| # | Decision | Reason | Alternatives rejected |
|---|---|---|---|
| 1 | Use a Singleton for `GameManager` (isolated behind a minimal interface where consumed) | Brief's DESIGN section explicitly names Singleton as a required pattern, overriding CONVENTIONS.md's baseline anti-singleton stance (golden rule 1: brief overrides best practice) | Pure DI/service locator — not what was asked for; brief names Singleton specifically |
| 10 | `GameManager` implemented (Report 002) with static instance isolated behind `IGameManager`; duplicate-instance self-destruction uses `Application.isPlaying` to pick `Destroy` (Play Mode) vs `DestroyImmediate` (Edit Mode) | Fulfils decision #1's "isolate behind a minimal interface" clause; the Play/Edit Mode branch is needed because `Destroy()` is invalid outside Play Mode | A single unconditional `Destroy()` call — throws in Edit Mode |
| 11 | Dropped automated EditMode tests for `GameManager`'s Awake-driven singleton/lifecycle behavior; hand-test instead | Empirically confirmed (via temporary `Debug.Log` instrumentation) that `Awake` does not fire on `AddComponent`-created objects within this machine's `-runTests -testPlatform EditMode` batchmode run, even after 10 `yield return null` frames in a `[UnityTest]` — an environment limitation, not a logic bug. Documented in `ENVIRONMENT.md` for future features | Continuing to chase the EditMode test (would test the environment, not the code); PlayMode tests (not attempted — out of scope for this narrow feature) |
| 2 | MVC-style split: SO/plain-C# Models, MonoBehaviour Views, MonoBehaviour Controllers wiring via Events | Brief explicitly requires "UI and Logic separated... using techniques like MVC" | Fully custom architecture ignoring the brief's named technique |
| 3 | Custom grid-based A* pathfinding, no Unity NavMesh/NavMesh2D | Brief explicitly names "Pathfinding (A*)" as the required algorithm; NavMesh is never mentioned and would add an unrequested package (golden rule 6) | Unity NavMesh 2D package |
| 4 | Object pooling used for both the infinite Production Menu scroll view AND for units/buildings that are frequently created/destroyed | Brief explicitly ties Object Pooling to the infinite scroll view (UX section) and separately lists Object Pooling as a required general pattern; frequent Instantiate/Destroy of units/buildings also directly threatens the <20 draw call budget (requirement 13) | Plain Instantiate/Destroy everywhere |
| 5 | Game Board modeled as a discrete grid; cell size lives on a `GridDefinition` SO and building/unit footprints live on their own `Definition` SOs — **none of these are hardcoded**, all designer-editable | Human confirmed grid layout from the UI mockup, then corrected that cell size is "any X by X pixel value" decided once visuals are chosen (not the mockup's 32×32px), and footprints "should be easily modifiable by a designer." Mockup's drawn row/column count remains illustrative only — actual board dimensions are a data/visual-fit choice | Continuous/free-form placement with custom collider-based validity checks; hardcoding 32×32px or the mockup's footprint numbers as constants |
| 6 | No resource/currency economy implemented | Brief requirement 4 explicitly states production has no time cost and is unlimited; no cost currency is mentioned anywhere | Adding a generic resource system speculatively |
| 7 | No enemy AI / opposing faction implemented, human-confirmed | Human explicitly confirmed no opposing AI faction; combat requirement (10) only specifies right-click-to-attack a unit/building, any legal target | Building a full enemy-faction AI (would have been speculative scope per golden rule 2) |
| 8 | Building/unit system kept modular across every layer: data (SO defs), creation (Factories), and UI (Production Menu iterates definitions generically; producible-unit lists are data on `BuildingDefinition`, not switch-cased) — even though only Barracks + Power Plant + 3 soldiers ship | Human explicitly required extensibility beyond just the data layer: "all systems for buildings, including UI as well as the list of units it can spawn etc. should be modular and extensible" | Hardcoding a fixed enum/switch of building types in the Production Menu UI or elsewhere |
| 9 | Added a Main Menu (`MainMenu.unity`) with Play + Settings (resolution/display-mode), inserted into scene flow as `Boot` → `MainMenu` → `Gameplay` | Human-mandated so the evaluator can exercise GI-13 (aspect ratio/resolution support) directly rather than only via editor Game-view resizing; brief's own resolution requirement is the stated reason | No main menu (original Phase-0 assumption, since superseded) |
| 12 | `GameManager.Awake` calls `DontDestroyOnLoad(transform.root.gameObject)` instead of `DontDestroyOnLoad(gameObject)` (Report 003) | `DontDestroyOnLoad` only accepts root GameObjects; `GameManager` is a child of `--- SYSTEMS ---` per CONVENTIONS.md scene composition, so the un-rooted call threw at runtime (human caught this hand-testing Report 002). Persisting the whole root also matches ARCHITECTURE.md §4's intent that all of `--- SYSTEMS ---` (event channels, pools, etc., once they exist) survive scene loads together | Moving `GameManager` to scene root instead — would violate CONVENTIONS.md's "no loose objects at root" rule |
| 13 | Added `SceneReference` (`CaseGame.Core`) as a reusable asset-picker wrapper for scene references, backed by an editor-only `SceneAsset` field synced to a runtime-safe `sceneName` string via `ISerializationCallbackReceiver`; added `SceneReferenceDrawer` so it still shows as a single object-picker field, not two raw fields. First script under `Scripts/Editor/`, first `CaseGame.Editor` asmdef (Report 004) | Human-mandated project convention: prefer typed asset-picker `SerializeField`s over raw strings for asset references, to avoid typo-prone/rename-unsafe hardcoded names — `GameManager.firstScene` was the first offender | Leaving `firstSceneName` as a raw string; using Unity's Build-Settings scene index instead (more fragile to reordering) |
| 14 | Relocated `Boot.unity`/`Gameplay.unity` from `Assets/Scenes/` to `Assets/_Project/Scenes/` (Report 005) | Both had been hand-created in Unity's default `Assets/Scenes/` folder during earlier editor hookup checklists instead of the CONVENTIONS.md-mandated `_Project/` root; caught by the agent while starting the Main Menu feature, fixed with explicit human approval since it required moving/editing tracked assets and `ProjectSettings/EditorBuildSettings.asset` | Leaving them misplaced (would fail the brief's explicit folder-structure evaluation criterion, GI-15) |
| 15 | Main Menu built with TextMeshPro (`TMP_Dropdown` for resolution selection, via `TMPro.TMP_DefaultControls` factory methods) and the Input System package's `InputSystemUIInputModule` on the scene's `EventSystem` (Report 005, redone in place) | Human explicitly installed `com.unity.inputsystem` and imported TMP Essential Resources specifically for this feature, with Input System also intended for gameplay input later (per CONVENTIONS.md's pre-existing baseline default, previously un-actionable because the package wasn't installed) and TMP_Dropdown specifically requested over the earlier prev/next-button cycler workaround | Keeping the legacy `UnityEngine.UI`/cycler version from the original pass at this feature — superseded, not shipped |
| 16 | `TMP_DefaultControls.CreateButton/CreateText/CreateDropdown(new TMP_DefaultControls.Resources())` (all-null sprite fields) safely builds fully-functional TMP widgets — including `TMP_Dropdown`'s full template/viewport/scrollbar sub-hierarchy — via editor script now that TMP Essentials are imported; `InputSystemUIInputModule` self-configures via its `Reset()`/`OnEnable()`'s `AssignDefaultActions()`, wiring a complete default Point/Click/Navigate/Submit/Cancel action set from the Input System package's own bundled default actions asset, with no manual `.inputactions` authoring needed (Report 005) | Empirically verified by reading both source (`Library/PackageCache/...`) and the generated scene YAML back after running the setup script — confirms these APIs are safe to drive from a headless batchmode editor script on this pinned version | Hand-building the Dropdown's template hierarchy or a custom `InputActionAsset` — unnecessary, both packages already provide safe, complete defaults |
| 17 | Events module uses ScriptableObject event channels (`GameEvent`, generic `GameEventChannel<T>` base), not plain static C# `event`/`Action` fields, plus a `GameEventListener` MonoBehaviour bridging to a designer-configured `UnityEvent` (Report 006) | CONVENTIONS.md's baseline already names "ScriptableObject event channels when designer-facing wiring is valuable" as the preferred decoupling mechanism; SO channels let a raiser and listener both reference the same asset with zero direct-reference or static coupling, and are asset-picker-friendly (consistent with decision #13's asset-picker-over-string convention). Only the parameterless `GameEvent` ships now — typed payload channels (e.g. a future `BuildingDefinition` channel for Selection → Info Panel) get added as concrete `GameEventChannel<T>` subclasses alongside whichever feature first needs one, since inventing payload types speculatively would violate golden rule 2 | Static C# `event Action` fields on a static bus class (works, but re-introduces static/global coupling the brief's Events requirement is meant to avoid); building typed listener/channel variants now for hypothetical future payloads |
| 18 | `PrefabPool<T>` wraps Unity's built-in `UnityEngine.Pool.ObjectPool<T>` rather than a hand-rolled stack/queue pool (Report 007) | The brief mandates the Object Pooling *pattern* being demonstrably used, not a from-scratch implementation; `ObjectPool<T>` has been part of `UnityEngine.CoreModule` since Unity 2021.1 (confirmed available on the pinned `2021.3.45f2` by compile — no package needed) and already handles capacity limits and double-release detection (`collectionCheck: true`) correctly, which a hand-rolled version would have to reimplement and re-test | Writing a custom pool from scratch (more code to test/maintain for no benefit — golden rule 1: where the brief is silent on *how*, use modern Unity best practice) |
| 19 | `Health.Damaged`/`Health.Died` are plain per-instance C# events, not a shared `GameEventChannel<T>` from the Events module (Report 008) — refines the Phase-0 module map, which listed Combat as depending on Events | Every unit/building will own its *own* `Health` instance; a shared/global SO channel would broadcast every instance's HP changes to every listener, which is the wrong shape for "this specific soldier took damage." Plain instance events are the correct decoupling tool here (raiser and the one interested listener — that instance's own controller/view — share the `Health` object reference directly), while a global channel remains right for genuinely cross-system, one-of-a-kind signals (e.g. a future `SelectionChanged`) | Routing every `Health` instance's events through a shared `GameEventChannel<Health>` — would require every listener to filter for "is this event about the instance I care about", solving a problem that doesn't exist here |
| 20 | Added a minimal `UnitDefinition` (SO, data only: name/sprite/footprint/HP/attack damage) under `CaseGame.Units` as part of the Buildings feature, ahead of the full Units feature (Report 009) | Brief requirement 2 explicitly mandates `BuildingDefinition` reference a list of `UnitDefinition`s (data-driven producible units, not switch-cased) — `BuildingDefinition` can't compile without the type existing. This is a data-only stub, not scope creep into Units' actual behavior (`SoldierBase`/`Soldier1/2/3`/`UnitFactory` still land as their own feature) | Making `BuildingDefinition`'s producible list untyped (`object`/string names) to defer the dependency — would give up compile-time safety and contradict the brief's explicit `UnitDefinition` reference mandate |
| 21 | `BuildingBase.Initialize(definition, onDied)` takes an optional death callback instead of knowing about `PrefabPool<T>`/pooling itself; `BuildingFactory.Create` wires `() => pool.Release(instance)` as that callback | Keeps `BuildingBase` single-responsibility (HP/sprite only) and testable via a plain callback with no pool in the test at all; the Factory is the one thing that knows both "this instance came from a pool" and "this instance died," so it's the natural place to connect the two | `BuildingBase` holding a direct reference to its own pool (couples the humble building class to a pooling implementation detail it doesn't need to know about) |
| 22 | Extracted `GameEntityDefinition`/`GameEntityBase` (new `CaseGame.Entities` module) as a shared base for `BuildingDefinition`/`BuildingBase` and (future) `UnitDefinition`/`SoldierBase` (Report 010) | Human-suggested. `BuildingDefinition`/`UnitDefinition` already duplicated identical fields (name, sprite, footprint, maxHealth) and `BuildingBase`'s `Health`/`IDamageable`/sprite/death-callback logic is exactly what a future `SoldierBase` needs too — real, current duplication, not speculative. Gives the brief's mandated "Inheritance" a real hierarchy rather than the shallow `Barracks`/`PowerPlant : BuildingBase` split alone. Named `GameEntity*`, not `Unit*` — the brief's own text uses "unit(s)" specifically to mean soldiers throughout ("Soldier Units," "attack...on a unit or building," etc.), always as a sibling of "building," never as an umbrella term, so naming the shared base "Unit" would misread against the brief's own vocabulary and would have collided with the already-existing `UnitDefinition` | Naming the shared base `Unit*`/`UnitBase` (collides with existing `UnitDefinition` and the brief's own soldier-specific use of "unit"); leaving the duplication in place since only 2 concrete types ship |
| 23 | `AStarPathfinder` is a static, stateless class (`FindPath(grid, start, goal)`) rather than an instantiable object holding the grid as a field (Report 011) | It has no state beyond the grid reference passed in for a single call — no per-instance data to justify object lifecycle management; callers don't need to construct/hold a pathfinder object at all | Instance-based `new AStarPathfinder(grid).FindPath(start, goal)` — adds ceremony with no benefit here |
| 24 | 8-directional movement (diagonal cost √2, octile-distance heuristic) with corner-cutting prevention, instead of 4-directional-only | 4-directional-only would still technically "route around" buildings but produce visibly blocky, unnatural-looking paths for a 2D top-down game; corner-cutting prevention (reject a diagonal step if either flanking orthogonal cell is blocked) is what actually makes "wander around the buildings" (GI-7/8) true instead of soldiers visually clipping through a building's corner | 4-directional-only movement (simpler, but worse-looking paths and doesn't need the corner-cut edge case at all — would undersell "accounting for edge cases," GI-16) |
| 25 | No coroutine-based path-request queue/manager in this pass, despite ARCHITECTURE.md's original module description mentioning "path requests" (Report 011) | Nothing calls `AStarPathfinder` yet — Units/Selection (the actual callers) don't exist. Building a request-queue layer now would be speculative (golden rule 2); the brief's Coroutine requirement doesn't specifically require it live in Pathfinding — it's equally at home in the future unit-movement code that steps a soldier along the returned path | Building an async/queued path-request layer now on the assumption it'll be needed — defer until a real caller shows whether pathfinding actually needs to be spread across frames |
