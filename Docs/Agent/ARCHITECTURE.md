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
| Entities | `CaseGame.Entities` | `GameEntityDefinition` (abstract SO: name/sprite/footprint/maxHealth) and `GameEntityBase` (abstract MonoBehaviour: owns a `Health`, implements `IDamageable`, sprite assignment, death callback, and — as of Report 015 — `SetSelected(bool)`, a shared selection-tint visual both Buildings and Units get for free) — shared by Buildings and Units so neither duplicates this shape (see decisions log #22) | Combat |
| Buildings | `CaseGame.Buildings` | `BuildingBase` (: `GameEntityBase` — as of Report 017 also a virtual `SpawnPosition`, default `transform.position`, so any building can be asked "where do your products spawn" without a type-check, see decisions log #42), `Barracks` (overrides `SpawnPosition`), `PowerPlant`, `BuildingDefinition` (: `GameEntityDefinition`; `ProducibleUnits` is `List<UnitCatalogEntry>` as of Report 017 — was `List<UnitDefinition>`, see decisions log #41), `BuildingFactory`; as of Report 014 also `BuildingCatalogEntry`/`BuildingCatalog` (the Production Menu's data source — a `Definition`+prefab pair per producible building) `BuildingCatalogEntryEventChannel` (typed event channel carrying a "produce this" request — kept in this namespace since the payload is Buildings-domain data both Placement and UI.Production already depend on Buildings, see decisions log #32), and (Report 015) `SelectedBuildingEventChannel` (same reasoning — Selection raises it, the Info Panel consumes it) | Entities, Pooling, Units, Events |
| Units | `CaseGame.Units` | `SoldierBase` (: `GameEntityBase`, adds `MoveTo`/`TryAttack`), `Soldier` (the one concrete type — 3 soldier variants are `UnitDefinition` data, not 3 classes, see decisions log #26), `UnitDefinition` (: `GameEntityDefinition`), `UnitFactory`; as of Report 017 also `UnitCatalogEntry` (`UnitDefinition` + prefab pair, mirrors `BuildingCatalogEntry`), `UnitProductionRequest`/`UnitProductionRequestEventChannel` (a clicked producible-unit icon's "produce this, here" request), and `UnitProductionController` (Controller: subscribes, spawns via `UnitFactory`, positions at the requested spawn point — see decisions log #43/#44) | Entities, Grid, Pathfinding, Combat, Events |
| Combat | `CaseGame.Combat` | `IDamageable`, `Health` (HP, damage, death via plain per-instance C# events — see decisions log #19) | — |
| Pathfinding | `CaseGame.Pathfinding` | `AStarPathfinder`: static, tested 8-directional grid A* with corner-cutting prevention ("wander around buildings," GI-7/8). No request-queue/coroutine layer yet — added if a real caller (Units movement) shows it's needed | Grid |
| Placement | `CaseGame.Placement` | `BuildingGhostView` (View: toggles a desaturated ghost silhouette, tinted green/red, vs. the real sprite — see decisions log #28) and `PlacementController` (Controller: mouse→cell, validity query, commit-to-grid, and — as of Report 014 — subscribes to `BuildingCatalogEntryEventChannel` to start placement when the Production Menu raises a "produce" request; as of Report 017, `Update()` also skips world input while the pointer is over UI, see decisions log #47) | Grid, Buildings, Events |
| Selection | `CaseGame.Selection` | `SelectionController`: left-click select (plain click replaces, shift-click adds/removes a soldier — GI-7/8's "unit(s)"), right-click move-or-attack (GI-7/8/10/11 — attack takes priority over movement when the cursor is over an `IDamageable`). Selecting a building and selecting soldiers are mutually exclusive; visual feedback is `GameEntityBase.SetSelected`, no new prefab wiring. Raises `SelectedBuildingEventChannel` on building-selection change; as of Report 017, `Update()` also skips world input while the pointer is over UI (decisions log #47) | Units, Buildings, Grid, Combat, Events |
| Gameplay | `CaseGame.Gameplay` | `GameplayBootstrap` (Report 017) — `Gameplay.unity`'s composition root: the one place that builds `BuildingFactory`/`UnitFactory` with a real container `Transform` and calls `PlacementController`/`SelectionController`/`UnitProductionController`'s `Initialize` from `Start()` (runs after every object's `Awake`, unlike relying on Awake-order between them and whoever owns the `GridModel`). Holds no gameplay logic itself — see decisions log #45 for why this is its own module rather than living in Core | Grid, Buildings, Units, Placement, Selection |
| Pooling | `CaseGame.Pooling` | Generic `ObjectPool<T>` used by the scroll view and by frequently spawned/destroyed units/buildings | — |
| Events | `CaseGame.Events` | Lightweight C# event channels (plain events and/or SO event channels) connecting the above without direct references | — |
| UI.Production | `CaseGame.UI.Production` | `ScrollRecycler` (plain C#: given item count/pool size/scroll offset, decides which data index each pooled slot shows and where — the actual "infinite, object-pooled scroll" math, tested independent of Unity UI), `ProductionMenuItemView` (View: one pooled, rebindable row), `ProductionMenuController` (Controller: owns the `PrefabPool<ProductionMenuItemView>`, applies `ScrollRecycler`'s decisions to `ScrollRect`). Iterates `BuildingCatalog` **generically** (no per-type UI branches); a row's "produce" click raises `BuildingCatalogEntryEventChannel` — Placement listens, so neither module references the other | Pooling, Buildings, Events |
| UI.Info | `CaseGame.UI.Info` | `InfoPanelController` (subscribes to `SelectedBuildingEventChannel`; shows the selected building's image/name, hides entirely for null/soldier selection) and `ProducibleUnitIconView` (icon per producible unit, spawned generically off `BuildingDefinition.ProducibleUnits` — requirement 6's "Power Plant needs no production sub-menu" falls out for free as an empty list, no per-type branch). As of Report 017 these icons are **clickable** — this row *is* GI-6's "production sub-menu," so clicking one raises `UnitProductionRequestEventChannel` (`CaseGame.Units`); see decisions log #44 for why Report 016 originally built this non-interactive and what changed | Events, Buildings, Units |
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
- `Gameplay.unity` — the actual demo. As of Report 017, fully assembled:
  - `--- ENVIRONMENT ---`: `Main Camera` (orthographic, reframed on the grid — see decisions
    log #48), `Global Light 2D`, `Grid` (`GridView`, wired to `GridDef_Default.asset`).
  - `--- SYSTEMS ---`: `PlacementController` (wired: `gameCamera` = Main Camera,
    `produceRequestedChannel` = `BuildingCatalogEntryEvent_Default.asset`), `SelectionController`
    (wired: `gameCamera` = Main Camera, `selectedBuildingChannel` = `SelectedBuildingEvent_Default.asset`),
    `UnitProductionController` (wired: `produceRequestedChannel` = `UnitProductionRequestEvent_Default.asset`),
    `GameplayBootstrap` (wired to `Grid`'s `GridView` and the three controllers above, plus the
    `--- GAMEPLAY ---` containers below — `Start()` builds the Factories and calls each
    controller's `Initialize`).
  - `--- GAMEPLAY ---`: `Buildings` and `Units` (empty container Transforms — `BuildingFactory`/
    `UnitFactory`'s pooled instances parent here at runtime).
  - `--- UI ---`: `Canvas` (`ScaleWithScreenSize`, 1920×1080 reference), `EventSystem`
    (`InputSystemUIInputModule`), `ProductionMenu` (`ProductionMenuController`, left-anchored,
    wired to `BuildingCatalog_Default.asset`), `InformationPanel` (`InfoPanelController`,
    right-anchored, starts inactive, wired to `SelectedBuildingEvent_Default.asset`).
- Key prefabs, all under `Assets/_Project/Prefabs/`:
  `Buildings/Building_Barracks.prefab` + `Building_PowerPlant.prefab` (human-created after
  Report 009, `Hitbox` collider added per Report 015's hookup) — a parent GameObject with
  `Visuals` (SpriteRenderer), `VisualsGrayscale` (Report 013's ghost), `Hitbox` (`BoxCollider2D`,
  trigger), and (Barracks only) `SpawnPoint` as children. `Units/Soldier_1.prefab`,
  `Soldier_2.prefab`, `Soldier_3.prefab` (human-created after Report 012, same `<Category>_<Name>`
  naming, `Hitbox` collider added per Report 015) — same `Visuals`/`Hitbox` child structure,
  `Soldier` component on the root. `UI/ProductionMenuItem.prefab` (Report 014) — `Icon`
  (Image) + `Name` (TMP text) children, `Button` + `ProductionMenuItemView` on the root; no
  `<Category>_` prefix since there's no family of named variants to disambiguate.
  `UI/ProducibleUnitIcon.prefab` (Report 016, **rebuilt in Report 017** with an added `Button` —
  see decisions log #44) — same `Image`/`Name` shape, now clickable. All agent-created prefabs
  built/rebuilt via throwaway editor scripts, verified by reading the generated files back
  rather than trusting a clean exit code. All per CONVENTIONS.md's per-prefab grouping
  convention. Ghost/preview prefab for placement: none needed, reuses the building prefabs
  themselves (decisions log #29).
- SO definition/config assets live under `Assets/_Project/ScriptableObjects/`: `GridDef_Default.asset`,
  `BuildingCatalog_Default.asset`, `BuildingCatalogEntryEvent_Default.asset` (Report 014),
  `SelectedBuildingEvent_Default.asset` (Report 016), and `UnitProductionRequestEvent_Default.asset`
  (Report 017) at the root, `GameEntityDefs/Buildings/BuildingDef_Barracks.asset` +
  `BuildingDef_PowerPlant.asset` (Report 017: `producibleUnits` populated with Soldier 1/2/3;
  `entityName` fixed — see decisions log #46), and `GameEntityDefs/Units/UnitDef_Soldier1.asset` +
  `UnitDef_Soldier2.asset` + `UnitDef_Soldier3.asset` (Report 017: `entityName`/Soldier 3's stats
  fixed, same decision) — human's own organization, renamed from an earlier
  `ScriptableObjects/Units/{Buildings,Troops}/` pass to `GameEntityDefs/` once the
  `GameEntityDefinition` shared base landed (Report 010), which is a more accurate name for
  what the folder actually holds. `GameEntityDefs/` is reserved for `GameEntityDefinition`
  subclasses specifically — `BuildingCatalog`/event channels are config/data assets, not
  entity definitions, so they stay at the SO root alongside `GridDef_Default.asset`.
- This section is updated feature-by-feature as prefabs/assets are actually created — treat
  the above as current inventory as of Report 017.

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
- Report 012 (`Docs/Reports/012_units.md`): landed the `Units` module — `SoldierBase`
  (`: GameEntityBase`, adds `MoveTo` — pathfinds via `AStarPathfinder` then walks the route in
  a Coroutine, the brief-mandated Coroutine pattern's natural home per §3's data-flow — and
  `TryAttack`), `Soldier` (the one concrete class), `UnitFactory` (Factory pattern, pools via
  `PrefabPool<T>`, mirrors `BuildingFactory`), and extended the existing `UnitDefinition` with
  `MoveSpeed`. The brief's 3 soldier types ship as 3 `UnitDefinition` assets sharing the one
  `Soldier` class, not 3 separate classes — see decisions log #26. Still no scene has an actual
  soldier in it (that's Selection/Production's job to spawn and command one) — the human needs
  to create the 3 `UnitDef_Soldier{1,2,3}` assets and prefabs per the editor hookup checklist.
- Report 013 (`Docs/Reports/013_placement.md`): landed the `Placement` module — implements
  requirement 3's ghost/invalid-area feedback. `BuildingGhostView` toggles a building instance
  between a desaturated "ghost" silhouette (new `Art/Shaders/SpriteGrayscaleGhost.shader`,
  tinted green/red via `SpriteRenderer.color`) and its real sprite, on the *same* pooled
  instance — no separate temp-preview object. `PlacementController` drives it: mouse → grid
  cell, validity query each frame (`GridModel.IsAreaFree`, no per-building-type coupling),
  commit marks the grid occupied and reveals the real sprite, cancel returns the instance to
  its pool (new `BuildingFactory.Release`). Human-directed design: the grayscale-then-tint
  technique (not tinting the full-color sprite directly) — see decisions log #28.

- Report 014 (`Docs/Reports/014_ui-production.md`): landed the `UI.Production` module —
  the Production Menu's infinite, pooled scroll view (UX brief: "Infinite Scrollview — Object
  Pooling"). `ScrollRecycler` (plain C#, tested) computes which `BuildingCatalog` index each of
  a small, fixed-size pool of `ProductionMenuItemView` rows should currently show as the view
  scrolls — the pool never grows with the catalog size, which is what makes the list genuinely
  "infinite"-capable rather than one row per entry. A row's "produce" click raises the new
  `BuildingCatalogEntryEventChannel` (the project's first concrete `GameEventChannel<T>`
  payload channel, foreshadowed as a foundation-only placeholder back in Report 006);
  `PlacementController` now subscribes to it and calls its existing `BeginPlacement`, so
  UI.Production and Placement stay fully decoupled — connected only via the shared channel
  asset. Also added `BuildingCatalogEntry`/`BuildingCatalog` (`CaseGame.Buildings`) as the
  Production Menu's data source — a designer adds a producible building by adding one entry to
  this asset, no code change (requirement 2's modularity mandate). A throwaway editor script
  (`Scripts/Editor/Setup/Temp/ProductionMenuSetup.cs`, deleted same turn per CLAUDE.md's editor
  script policy) built `Gameplay.unity`'s first UI: a `--- UI ---` organizer with a Canvas
  (`ScaleWithScreenSize`, 1920×1080 reference — same settings as `MainMenu.unity`'s, for
  GI-13 consistency) and `EventSystem`/`InputSystemUIInputModule`, the pooled scroll view
  itself, and the `ProductionMenuItem` prefab (`Prefabs/UI/`) — populated the real
  `BuildingCatalog_Default.asset` with the existing Barracks/Power Plant definitions and
  prefabs. Scope boundary: `PlacementController` is *not yet* added to `Gameplay.unity` (it has
  nothing to be initialized with — no `GridModel`/`BuildingFactory` bootstrap exists in the
  scene yet), so the Production Menu's "produce" click can't be hand-tested end-to-end until
  Gameplay scene assembly (a later roadmap item) wires that up; this feature's own logic is
  fully unit-tested independent of that.

- Report 015 (`Docs/Reports/015_selection.md`): landed the `Selection` module —
  `SelectionController` (`CaseGame.Selection`): left-click selects (plain click replaces the
  current selection, shift-click adds/removes a soldier — a deliberately minimal multi-select
  mechanism, see decisions log #36), right-click on selected soldiers either attacks
  (`SoldierBase.TryAttack`, instant, no range/approach) or moves (`SoldierBase.MoveTo`) depending
  on whether the cursor is over an `IDamageable`. Building selection and soldier selection are
  mutually exclusive "modes." Hit-testing uses `Physics2D.OverlapPoint` against `Hitbox`
  colliders (new — the building/unit prefabs' `Hitbox` children have been empty placeholders
  since Reports 009/012, "for future selection"; this is that future). Visual selection feedback
  reuses `GameEntityBase.SetSelected` (new, tints the existing `Visuals` sprite) rather than a
  new prefab child — also fixed a latent pooling bug this surfaced: `Initialize` now resets
  `spriteRenderer.color` to white, since without it a pooled instance released while selected
  would start its next life pre-tinted. Added `SelectedBuildingEventChannel`
  (`CaseGame.Buildings`) — Selection raises it on building-selection change; nothing subscribes
  yet (UI.Info, not yet built, is the intended consumer — requirement 5). Scope boundary:
  `SelectionController` is *not* added to `Gameplay.unity` yet, same reasoning as
  `PlacementController` in Report 014 — no scene bootstrap exists to `Initialize` it with a
  `GridModel`. Editor hookup: the 5 existing building/soldier prefabs need a `BoxCollider2D`
  (trigger) added to their `Hitbox` child — full checklist in the report.

- Report 016 (`Docs/Reports/016_ui-info.md`): landed the `UI.Info` module — the Information
  Panel (requirement 5). `InfoPanelController` subscribes to `SelectedBuildingEventChannel`
  (raised by `SelectionController`, Report 015 — first real consumer) and shows the selected
  building's image/name, hiding entirely when nothing (or a soldier) is selected.
  `ProducibleUnitIconView` is a small, non-interactive, ungrouped icon (unlike
  `ProductionMenuItemView` — producing still only happens via the Production Menu) spawned
  generically off `BuildingDefinition.ProducibleUnits`; Power Plant's empty list means its row
  is simply empty — no per-building-type branch anywhere (requirement 6). Deliberately *not*
  pooled (decisions log #40) — the list is always small and only rebuilds on the rare
  "selection changed" event, unlike the Production Menu's actually-infinite list. A throwaway
  editor script (deleted same turn) built `Gameplay.unity`'s Information Panel — a right-anchored
  panel under the existing `--- UI ---` Canvas, mirroring the Production Menu's left-anchored
  strip — the `ProducibleUnitIcon` prefab, and populated `SelectedBuildingEvent_Default.asset`.
  Scope boundary, same as Reports 014/015: `SelectionController` still isn't in the scene, so
  the panel can't be hand-tested end-to-end yet — it only shows/hides correctly today because
  its own `SetSelectedBuilding` was called directly by tests, not because anything in the scene
  is driving it.

- Report 017 (`Docs/Reports/017_gameplay-scene-assembly.md`): **Gameplay scene assembly** —
  the integration feature. Per the human's explicit request, this pass started with a full
  audit of every existing module before touching the scene, and found real gaps rather than
  just wiring what already existed cleanly together:
  - **Units were never actually producible.** The Production Menu only lists buildings; the
    Info Panel's producible-unit icons (Report 016) were built non-interactive; and
    `BuildingDefinition.ProducibleUnits` had no way to know which *prefab* to spawn for a given
    `UnitDefinition`. Re-reading requirements 5/6 together confirmed the Info Panel's
    producible-unit row *is* GI-6's "production sub-menu" — so it needed to be the click target,
    not just a display. Fixed: `BuildingDefinition.ProducibleUnits` is now `List<UnitCatalogEntry>`
    (mirrors `BuildingCatalogEntry`, decisions log #41); `BuildingBase` gained a virtual
    `SpawnPosition` (#42) so resolving "where do this building's products appear" needs no
    per-type cast; `ProducibleUnitIconView` is now clickable and raises the new
    `UnitProductionRequestEventChannel` (`CaseGame.Units`) carrying a `UnitProductionRequest`
    (catalog entry + spawn position, bundled to avoid a circular Units→Selection dependency,
    #43/#44); a new `UnitProductionController` subscribes and spawns via `UnitFactory`.
  - **Two data bugs, found reading the actual asset YAML, not assumed correct.**
    `BuildingDef_Barracks`/`BuildingDef_PowerPlant` still serialized a stale `buildingName`
    field predating the `entityName` rename in Report 010 — `entityName` had been sitting empty
    this whole time, which would have shown blank building names on the Production Menu/Info
    Panel. `UnitDef_Soldier3` had `maxHealth: 1`/`attackDamage: 1` instead of the brief's
    explicit 10 HP / 2 damage (requirement 9). Both fixed via the same asset-touching script,
    documented rather than silently changed (decisions log #46).
  - **UI vs. world input conflict.** `PlacementController`/`SelectionController` read
    `Mouse.current` directly every frame with no awareness of whether the pointer was over a UI
    element — meaning clicking a Production Menu row would *also* fire a world-space
    commit/cancel or select/deselect at whatever was on-screen underneath the panel. Fixed by
    gating both `Update()`s on `EventSystem.current.IsPointerOverGameObject()` (decisions log
    #47) — standard Unity practice for exactly this conflict, not previously needed since no
    controller reading raw pointer input coexisted with clickable UI in the same scene until now.
  - **The actual wiring.** New `CaseGame.Gameplay` module, `GameplayBootstrap` (composition
    root — see decisions log #45 for why this isn't in Core): builds `BuildingFactory`/
    `UnitFactory` with real `--- GAMEPLAY ---` container Transforms and calls
    `PlacementController`/`SelectionController`/`UnitProductionController`'s `Initialize` from
    `Start()`. All three controllers, plus `GameplayBootstrap` itself, now live under a new
    `--- SYSTEMS ---` organizer, fully wired to the real event channel assets from Reports
    014–016. Camera repositioned/resized to frame the grid within the screen area the
    Production Menu/Info Panel don't occlude — computed from `GridDef_Default.asset` and the
    panels' known fixed UI width, **not visually verified** (decisions log #48; flagged for
    hand-test).
  - **Scope check, not scope creep.** Draw-call/batching verification and the Windows build
    export remain separate, later roadmap items — this report is integration and the bugs it
    surfaced, nothing beyond.
  - Every module in the Phase-0 plan is now wired into a live scene; nothing was hand-tested by
    a human yet (the agent cannot enter Play Mode) — see the report's audit checklist for what
    to verify.

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
| 26 | The brief's 3 soldier types ship as 3 `UnitDefinition` assets/prefabs sharing one concrete `Soldier : SoldierBase` class, not 3 separate `Soldier1`/`Soldier2`/`Soldier3` classes as Phase-0's module map assumed (Report 012) | Requirements 9's only difference between the 3 types is attack damage (10/5/2) — a data value. Nothing behavioral differs, so 3 empty subclasses would be an OOP checkbox with no substance behind it, and would contradict the project's own established philosophy of differentiating via data rather than code (the same reasoning already applied to `BuildingDefinition`'s producible-units list, decision #8). `SoldierBase` stays abstract (mirroring `BuildingBase`) so a *genuinely* different future soldier type still has a clean extension point | 3 empty subclasses purely to match the Phase-0 naming — would look like more inheritance than is actually true, and the brief never requires the classes to be literally named Soldier1/2/3, just that 3 soldier types with those stats exist |
| 27 | `SoldierBase.MoveTo` requests a path and walks it via `StartCoroutine` directly on the soldier itself, rather than Selection owning movement execution (Report 012) | Matches ARCHITECTURE.md §3's own data-flow description: "the soldier's controller consumes it, requests a path from Pathfinding, and moves along it via coroutine." Selection's job (once built) is just to interpret the right-click and call `MoveTo`/`TryAttack` — the humble MonoBehaviour that owns the transform is the natural place for the coroutine that moves it | Movement execution living in Selection instead — would make Selection reach into a soldier's transform from outside, and scatters "how a soldier moves" across two modules for no benefit |
| 28 | Placement ghost is desaturated first (custom `Art/Shaders/SpriteGrayscaleGhost.shader`, luminance per-pixel), *then* tinted via `SpriteRenderer.color`, rather than tinting the building's full-color sprite directly (Report 013) | Human-directed. A plain color multiply over the original full-color sprite would blend with whatever hues are already in the art (e.g. a blue banner region would go muddy dark-green instead of reading as clean green) — desaturating first is what makes the green/red read as an unambiguous valid/invalid signal (GI-3's "user must be visually informed when the location is invalid"). Hand-authored HLSL, not Shader Graph, to avoid any version-availability uncertainty around URP's 2D Shader Graph target on this pinned version (golden rule 7); verified by batchmode import producing no shader compile errors | Tinting the full-color sprite directly (simpler, but muddy/ambiguous); a Shader Graph 2D Unlit target (unverified availability on this exact URP/Editor version, unnecessary risk for a ~25-line shader); pre-baking desaturated texture variants as separate art assets (extra asset-authoring burden per building type, redundant with what a shader does automatically for any sprite) |
| 29 | The ghost preview and the final placed building are the *same* pooled instance — `BuildingGhostView` toggles `Visuals`/`VisualsGrayscale`/`Hitbox` active state on one object — rather than a separate temporary ghost object swapped for a freshly-created real one on commit (Report 013) | Human-directed. Avoids managing two objects with two lifecycles for what is really one placement lifecycle; reuses the existing pooled `BuildingFactory.Create` path unchanged — cancelling just returns the same instance to its pool (new `BuildingFactory.Release`) instead of needing separate ghost-cleanup logic | A disposable non-pooled ghost prefab instantiated fresh each placement attempt and destroyed on cancel/commit — works, but duplicates object-management logic and doesn't reuse pooling for the ghost phase |
| 30 | `PlacementController`'s decision logic (`BeginPlacement`/`UpdateGhostAt(cell)`/`TryCommitAt(cell)`/`CancelPlacement`) is exposed as plain methods taking an explicit cell, with `Update()` reduced to a 5-line mouse-reading orchestrator that calls them (Report 013) | Keeps the actual placement logic directly testable (no Mouse/Update-loop dependency in the tests) while `Update()` stays humble per CONVENTIONS.md — the same "extract the testable decision, keep the MonoBehaviour thin" pattern used throughout the project | Putting mouse-reading and validity/commit logic together inside `Update()` — untestable without simulating Input System device state and frame ticks |
| 31 | The Production Menu's recycling decision (which catalog index each pooled row slot shows, and at what Y position, given a scroll offset) is extracted into `ScrollRecycler` — a static, plain-C# class with no `ScrollRect`/UI dependency (Report 014) | Same "extract the testable decision" pattern as #30, applied to the trickiest new logic in the project so far. `ScrollRect`'s exact `anchoredPosition` sign convention isn't something the agent can verify without an interactive Play Mode session, but the *recycling math itself* (given a scroll offset, which items are visible) is pure and fully testable independent of that uncertainty | Writing the recycling logic directly inside `ProductionMenuController`'s `ScrollRect.onValueChanged` callback — untestable without a live `ScrollRect`/Canvas, and conflates "what Unity's scroll callback reports" with "what should be visible", the same two concerns #30 already separated for Placement |
| 32 | `BuildingCatalogEntry` (struct: `BuildingDefinition` + prefab pair), `BuildingCatalog` (SO listing them), and `BuildingCatalogEntryEventChannel` (typed event channel carrying one) all live in `CaseGame.Buildings`, not `CaseGame.UI.Production` (Report 014) | This is Buildings-domain data — "a building type and the prefab to spawn for it" — with nothing UI-specific about it. Both consumers (`UI.Production`, which lists them, and `Placement`, which subscribes to the channel) already depend on `Buildings`, so keeping the type there avoids introducing a *new* cross-module dependency in either direction | Defining these types in `UI.Production` (would make `Placement` depend on `UI.Production` just to receive its event payload type — backwards, since Placement is the more foundational module) |
| 33 | `PlacementController` subscribes directly to `BuildingCatalogEntryEventChannel` in its own `OnEnable`/`OnDisable` and forwards to its existing `BeginPlacement` (Report 014), rather than a separate bridge/listener component | "Start placing what was requested" is squarely `PlacementController`'s own responsibility — no different in kind from being called directly, just triggered by an event instead. This is also the project's *first* concrete `GameEventChannel<T>` payload channel, landing exactly when decision #17 said one would: alongside the feature that first needs it | A generic bridge component (`GameEventListener`-style) forwarding to a `UnityEvent` — that pattern exists for wiring a *designer-configured* response in the Inspector to a parameterless `GameEvent`; here the response (`BeginPlacement(entry.Definition, entry.Prefab)`) is fixed, non-designer-facing logic, so a direct subscription is simpler and just as decoupled |
| 34 | `ProductionMenuController.poolSize` is a fixed `[SerializeField] int` (default 8) rather than computed at runtime from the `ScrollRect` viewport's `RectTransform.rect.height` (Report 014) | Reading `RectTransform.rect` reliably requires a layout pass to have already run, which isn't guaranteed at `Awake`/`Start` for a freshly-loaded UI hierarchy — a real Unity UI timing hazard the agent can't fully verify without an interactive session. A designer-set pool size sidesteps that fragility entirely; the brief's actual data set (2 buildings today, a handful more at most) makes "exactly enough slots for the current viewport" an unnecessary optimization anyway — the point being demonstrated is the pooling/recycling *technique*, not squeezing out the last unused row view | Computing `poolSize` from `scrollRect.viewport.rect.height / itemHeight` at `Awake` — more "automatically correct," but fragile to a real Unity layout-timing gotcha with no way for the agent to verify it here; flagged for the human to double-check scroll behavior visually once in the Editor |
| 35 | Production Menu UI (`--- UI ---` organizer, `Canvas`/`EventSystem`, the `ProductionMenu` scroll view, `ProductionMenuItem` prefab) was added to `Gameplay.unity` as part of *this* feature, but `PlacementController` was deliberately *not* added to the scene (Report 014) | The Production Menu's own UI is this feature's actual scope — same as how Main Menu's feature built its own scene UI. `PlacementController` still has nothing to be initialized with (`Initialize(grid, factory)` needs a live `GridModel`/`BuildingFactory`, and no scene bootstrap wiring those exists yet) — adding it now would mean either leaving it broken or building bootstrap plumbing that's `Gameplay scene assembly`'s job, not this feature's | Also wiring `PlacementController` + a minimal bootstrap now, to make the whole produce→ghost→place loop hand-testable today — would have front-run Gameplay scene assembly's actual scope (Info Panel, camera framing, full 3-area layout) for the sake of one extra hand-test, when this feature's logic is already fully covered by automated tests |
| 36 | Multi-selecting soldiers uses shift-click (add/remove one at a time), not a drag/marquee box-select (Report 015) | The brief's only signal on this is the wording "unit(s)" in requirement 8 — it doesn't specify a mechanism. A drag-select needs a new visual (a selection-rectangle overlay) and world-rect-vs-position geometry; shift-click needs neither, is a completely standard alternative multi-select convention, and keeps the feature's diff to logic that's directly unit-testable (no `ScrollRect`-style Editor-verification-only visual). Recorded as an explicit interpretation, not a silent guess, so it's easy to revisit if a drag-box is actually wanted | Drag/marquee box-select (more "expected RTS feel," but a materially bigger feature — a rectangle-overlay View, screen-to-world rect conversion, and a visual the agent can't verify without an interactive session, unlike the rest of this feature) |
| 37 | Right-click attack (`SoldierBase.TryAttack`) is instant and range-less — no check that the target is within any distance of the attacking soldier, no "walk into range first" (Report 015) | `TryAttack`'s existing signature (Report 012) already only takes an `IDamageable`, no position/range info, and its doc comment explicitly anticipated Selection wiring it up exactly this way. The brief's requirement 11 says only "attack via right-click on a target," with no range/approach language, unlike requirement 8's explicit "shortest path" for movement — so range enforcement would be adding a mechanic the brief never asked for | Requiring the soldier to `MoveTo` attack range before applying damage — a materially bigger feature (attack-range data, movement-then-attack sequencing) with no textual basis in the brief |
| 38 | `GameEntityBase.Initialize` now resets `spriteRenderer.color` to white every call (Report 015) | Necessary correctness fix, not a stylistic addition: `SetSelected` (this report) makes sprite color stateful, and instances are pooled/reused (`BuildingFactory`/`UnitFactory`) — without the reset, an instance released while selected would start its *next* life pre-tinted yellow, a real bug the moment Selection exists, not a hypothetical one | Leaving color-reset to whoever adds selection visuals to a *specific* type later — would silently reintroduce the bug for every pooled type, since the root cause (pooling + stateful color) lives in the shared base |
| 39 | `SelectionController.HandleRightClick` prunes dead/null soldiers from the selection (`_selectedSoldiers.RemoveAll(s => s == null \|\| s.IsDead)`) at the point of use, rather than proactively unsubscribing from a death event as soon as a soldier is deselected-by-dying (Report 015) | Handles the realistic case (a selected soldier dies, sits inactive in its pool, hasn't been reused yet) with one line and no event-subscription bookkeeping. The narrower race — a dead selected soldier's pooled instance gets reused as a *different* soldier before the player's next right-click — is a known, accepted, documented limitation, not silently ignored: pruning by `IsDead` can't catch a reused instance still passing sanity as "not dead" | Subscribing to each selected soldier's death individually (would need a per-instance-death event GameEntityBase doesn't currently expose, plus careful subscribe/unsubscribe pairing on every selection change) to close a race that's unlikely to matter for a demo project of this scope |
| 40 | `InfoPanelController`'s producible-unit icons are plain `Instantiate`/`Destroy` per selection change, not pooled via `PrefabPool<T>` (Report 016) | The brief ties Object Pooling specifically to the Production Menu's *infinite* scroll view (UX section) and to frequently spawned/destroyed units/buildings (decision #4) — this list is always small (≤3 today, bounded by however many `UnitDefinition`s a `BuildingDefinition` lists) and only rebuilds on the rare "selection changed" event, not every frame or even every second. Pooling it would need an arbitrary pool-size guess with no data-driven contract behind it, for a churn rate pooling doesn't meaningfully help | Reusing `PrefabPool<ProducibleUnitIconView>` for consistency with `ProductionMenuController` — would apply the same tool to a problem it doesn't have (over-engineering, golden rule 2) |
| 41 | `BuildingDefinition.ProducibleUnits` changed from `List<UnitDefinition>` to `List<UnitCatalogEntry>` (a `UnitDefinition` + `SoldierBase` prefab pair, mirroring `BuildingCatalogEntry`) (Report 017) | Nothing anywhere in the project could answer "which prefab do I instantiate for this producible unit" — `UnitFactory.Create` needs both a definition and a prefab, but `BuildingDefinition` only ever stored the definition. Confirmed safe to change (not just theoretically cleaner): both `BuildingDef_Barracks`/`BuildingDef_PowerPlant`'s `producibleUnits` were still `[]` (empty) when this was found, so no authored data was lost | A separate global `UnitCatalog` registry (mirroring `BuildingCatalog`) that `InfoPanelController` looks up prefabs from — redundant, since `BuildingDefinition.ProducibleUnits` is already the complete, per-building list this problem needs; a global catalog would just be a second place the same definitions have to be kept in sync |
| 42 | `BuildingBase` gained a `public virtual Vector3 SpawnPosition => transform.position`; `Barracks` now *overrides* it (was previously its own unrelated property) (Report 017) | `InfoPanelController`/`UnitProductionController` need "where do this building's products spawn" for *any* building capable of producing units — hardcoding a cast to `Barracks` there would violate the project's own established no-per-type-branch discipline (decisions #8/#32/#35's data-driven-generic thread) the moment a second unit-producing building type ever exists. Polymorphism is literally the brief-mandated tool for exactly this ("OOP: Polymorphism, Inheritance") | Type-checking/casting to `Barracks` wherever a spawn position is needed — works today (only Barracks produces units) but bakes in a per-type branch the rest of the project deliberately avoids |
| 43 | `UnitProductionRequest` (plain struct: a `UnitCatalogEntry` + a `Vector3` spawn position) bundles the spawn position directly, rather than `UnitProductionController` looking up "whichever building is currently selected" itself (Report 017) | `UnitProductionController` (`CaseGame.Units`) would otherwise need a reference to `SelectionController` (`CaseGame.Selection`) to answer "where does this spawn" — but `Selection` already depends on `Units`, so that would be a circular module dependency. `InfoPanelController` already knows which building's row is being displayed when it binds each icon, so it's the natural place to supply the spawn position once, at bind time | `UnitProductionController` holding a `SelectionController` reference and reading `SelectedBuilding.SpawnPosition` at request time — circular dependency, and also subtly wrong if the selection changed between the icon being shown and being clicked |
| 44 | `ProducibleUnitIconView` is now clickable (added a `Button`, raises `UnitProductionRequestEventChannel`) — reversing Report 016's explicit "purely informational, no button" design | Re-auditing requirements 5 and 6 together while assembling the full scene: the Production Menu only ever lists *buildings* (confirmed by `BuildingCatalog`'s actual content and `ARCHITECTURE.md`'s own module description), so the Info Panel's producible-unit row is the *only* place a unit is ever shown to the player — meaning it **must** be GI-6's "production sub-menu," not a passive display, or units could never be produced at all. This was a real, previously-unnoticed gap: nothing in the shipped code could ever spawn a soldier | Leaving it non-interactive and inventing some other production path (e.g. a second scroll view for units) — would contradict GI-6's own wording ("sub-menu") and duplicate UI.Production's already-built pooled-scroll machinery for a list that's always small, non-infinite, and doesn't need it |
| 45 | New `CaseGame.Gameplay` module holding just `GameplayBootstrap`, rather than adding it to `CaseGame.Core` (Report 017) | `GameManager`/`SceneReference`/`IGameManager` are generic, reusable across *any* scene (Boot, MainMenu, Gameplay); `GameplayBootstrap` is specifically Gameplay.unity's own composition root, wiring Grid/Buildings/Units/Placement/Selection together — a fundamentally different, scene-specific concern that would otherwise dilute what "Core" means and give it a pile of gameplay-module dependencies it has no other reason to have | Putting it in `CaseGame.Core` anyway (was the original placeholder plan from Report 014) — technically works but conflates "generic app lifecycle" with "this one scene's assembly," and forces Core to depend on Grid/Buildings/Units/Placement/Selection for no benefit to Core itself |
| 46 | Found and fixed two pre-existing data bugs while auditing the definition assets this feature touches, rather than leaving them (Report 017): (1) `BuildingDef_Barracks`/`BuildingDef_PowerPlant` had an empty `entityName` (a stale `buildingName` field from before Report 010's rename was still populated instead); (2) `UnitDef_Soldier3` had `maxHealth: 1`/`attackDamage: 1` instead of the brief's explicit 10 HP / 2 damage (requirement 9) | These are unambiguous correctness bugs, not judgment calls: (1) would show a blank building name on two UI panels this very feature just wired up, and (2) directly contradicts a numbered brief requirement. Both are asset-data-only fixes (no code/logic change), low-risk and easily reversible if the human disagrees with the correction — but silently leaving a known, explicit-requirement violation in place because it predates this feature would have been the wrong call | Leaving them for the human to notice separately — the brief's own evaluation criteria explicitly call out "edge cases will be examined" and correctness against the numbered requirements; finding this during an explicitly-requested integration audit and not acting on it would defeat the purpose of doing the audit |
| 47 | `PlacementController`/`SelectionController`'s `Update()` now both check `EventSystem.current.IsPointerOverGameObject()` and skip world-input handling when true (Report 017) | Both read `Mouse.current` directly every frame with no UI awareness — before this feature, they never coexisted with clickable UI in the same live scene, so the conflict was latent, not yet manifest. Now that the Production Menu and Info Panel are both interactive *and* live in the same scene as these controllers, clicking a UI row would otherwise also fire a world-space commit/cancel/select/deselect at whatever happened to render underneath that screen position — standard Unity idiom, applied where it was newly needed | Leaving it unguarded — would produce a real, reproducible interaction bug (clicking "produce" also deselecting/cancelling) the very first time someone actually plays the assembled scene |
| 48 | Camera repositioned to the grid's center and resized (orthographic size 10.5) via a formula derived from `GridDef_Default.asset`'s dimensions and the Production Menu/Info Panel's known fixed UI width (360 units each, 1920 reference width), rather than left at Unity's untouched default (0,0,-10, size 5) (Report 017) | The default camera was never pointed at the grid at all (grid spans world X:[0,20] Y:[0,12]; default camera framing shows mostly empty space below-left of it) — this needed *some* fix for the scene to be usable. The formula targets fitting the grid's width inside the screen strip the two side panels don't occlude, with modest padding | Leaving the camera untouched and asking the human to fix it by hand — the math is straightforward enough to compute directly; flagged as *unverified* (agent cannot enter Play Mode) rather than silently presented as correct, so the human knows to actually look at it |
