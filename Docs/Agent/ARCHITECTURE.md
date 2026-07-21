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
| Buildings | `CaseGame.Buildings` | `BuildingBase` (abstract), `Barracks`, `PowerPlant`, `BuildingDefinition` (SO), `BuildingFactory` | Grid, Combat, Events |
| Units | `CaseGame.Units` | `SoldierBase` (abstract), `Soldier1/2/3`, `UnitDefinition` (SO), `UnitFactory` | Grid, Combat, Pathfinding, Events |
| Combat | `CaseGame.Combat` | `IDamageable`, `Health` (HP, damage, death), attack resolution | Events |
| Pathfinding | `CaseGame.Pathfinding` | Grid-based A* implementation, path requests, obstacle-aware routing ("wander around buildings") | Grid |
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
- Key prefabs (added as each feature lands): `Barracks.prefab`, `PowerPlant.prefab`,
  `Soldier_1/2/3.prefab`, a Production Menu list-item prefab (pooled), ghost/preview prefab
  for placement.
- This section is updated feature-by-feature as prefabs are actually created — treat the
  above as the plan, not yet-built inventory.

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
- [ ] 19. Main Menu with Play button + Settings screen (resolution/display-mode)

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
