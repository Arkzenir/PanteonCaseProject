# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 015 (`Selection`), 2026-07-22. Compile clean,
**113/113 EditMode tests passing** (96 prior + 17 new).

**Pending on the human:** editor hookup for Report 015 — add a `BoxCollider2D` (Is Trigger
checked) to the `Hitbox` child on all 5 existing prefabs (`Building_Barracks`,
`Building_PowerPlant`, `Soldier_1/2/3`) — click-hit-testing has nothing to hit without this.
Full steps in Report 015. Requirement 19 (Main Menu) remains the one fully-closed brief
requirement; everything else in the checklist is still `[ ]` (Selection's logic is built and
tested, but nothing is wired into `Gameplay.unity` yet — see below).

**Modules with real, tested code:** Core (`GameManager`), Grid, Entities (shared
`GameEntityDefinition`/`GameEntityBase` — now also `SetSelected` for selection tinting),
Combat, Buildings (`BuildingCatalog`/`BuildingCatalogEntry`/`BuildingCatalogEntryEventChannel`/
`SelectedBuildingEventChannel`), Units (`SoldierBase`/`Soldier`/`UnitFactory` — note: 3 soldier
*types* are `UnitDefinition` data, not 3 classes, see decisions log #26), Placement
(`BuildingGhostView`/`PlacementController`), UI.Production (`ScrollRecycler`/
`ProductionMenuItemView`/`ProductionMenuController`), Selection (`SelectionController`),
Events, Pooling, Pathfinding.

**Not yet built:** UI.Info, actual Gameplay scene assembly, draw-call/batching verification,
Windows build export.

**Recommended next-feature order** (dependency-driven — each step only needs what's already
shipped above it; full reasoning given in chat and the published "Development Dispatch"
artifact on 2026-07-21, not otherwise saved in the repo):
1. ~~Units~~ — done (Report 012).
2. ~~Placement~~ — done (Report 013).
3. ~~UI.Production~~ — done (Report 014).
4. ~~Selection~~ — done (Report 015). Note: like `PlacementController`,
   `SelectionController.Initialize(grid)` is never called from a scene yet — same bootstrap gap.
5. **UI.Info** — Information Panel, subscribes to `SelectedBuildingEventChannel`.
6. **Gameplay scene assembly** — wire the brief's 3-area layout together end-to-end; this is
   also where `PlacementController` *and* `SelectionController` finally get added to
   `Gameplay.unity` and initialized with a real `GridModel`/`BuildingFactory`, making the whole
   produce → place → select → move/attack loop hand-testable for the first time.
7. **Draw-call/batching pass** — only measurable once real content exists to profile.
8. **Windows build + `/final-report`**.

**Known environment gotchas** (full detail in `ENVIRONMENT.md`): `Awake`/`OnEnable` don't
reliably fire on `AddComponent`-created objects in this machine's batchmode EditMode test
runner — don't chase that, hand-test lifecycle-bound MonoBehaviours instead. Asmdef
`references` are not transitive. Always verify a throwaway editor script's result by reading
the generated files back, not just trusting a clean exit code.

**Standing conventions to remember** (full detail in `CONVENTIONS.md` overrides table): prefer
typed asset-picker `SerializeField`s over raw strings (see `SceneReference`); SO definition
assets live under `Assets/_Project/ScriptableObjects/GameEntityDefs/<Category>/`, not
`Settings/`; building/unit prefabs are a parent GameObject with child GameObjects per concern
(`Visuals`, `Hitbox`, `SpawnPoint`, ...) — buildings under `Prefabs/Buildings/` named
`Building_<Name>`, soldiers under `Prefabs/Units/` named `Soldier_<N>` (same `<Category>_<Name>`
convention). UI prefabs live under `Prefabs/UI/` with a plain name, no category prefix (e.g.
`ProductionMenuItem.prefab`); root-level config/data SO assets (not entity defs) use
`<Type>_Default.asset` at the `ScriptableObjects/` root (e.g. `BuildingCatalog_Default.asset`).
