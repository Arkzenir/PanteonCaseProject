# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 016 (`UI.Info`), 2026-07-22. Compile clean,
**120/120 EditMode tests passing** (113 prior + 7 new).

**Pending on the human:** editor hookup for Report 015 is still open if not already done — add
a `BoxCollider2D` (Is Trigger checked) to the `Hitbox` child on all 5 existing prefabs
(`Building_Barracks`, `Building_PowerPlant`, `Soldier_1/2/3`); click-hit-testing has nothing to
hit without this (full steps in Report 015). Report 016 needed no human hookup — its throwaway
setup script did the scene/prefab/asset wiring itself, verified by reading the generated files
back. Requirement 19 (Main Menu) remains the one fully-closed brief requirement.

**Modules with real, tested code:** Core (`GameManager`), Grid, Entities (shared
`GameEntityDefinition`/`GameEntityBase` — `SetSelected` for selection tinting), Combat,
Buildings (`BuildingCatalog`/`BuildingCatalogEntry`/`BuildingCatalogEntryEventChannel`/
`SelectedBuildingEventChannel`), Units (`SoldierBase`/`Soldier`/`UnitFactory` — note: 3 soldier
*types* are `UnitDefinition` data, not 3 classes, see decisions log #26), Placement
(`BuildingGhostView`/`PlacementController`), UI.Production (`ScrollRecycler`/
`ProductionMenuItemView`/`ProductionMenuController`), Selection (`SelectionController`),
UI.Info (`InfoPanelController`/`ProducibleUnitIconView`), Events, Pooling, Pathfinding.

**Not yet built:** actual Gameplay scene assembly, draw-call/batching verification, Windows
build export. Every module in ARCHITECTURE.md's Phase-0 plan now has real code — what's left is
integration (wiring `PlacementController`/`SelectionController` into `Gameplay.unity` with a
real bootstrap) and the polish/verification passes.

**Recommended next-feature order** (dependency-driven — each step only needs what's already
shipped above it; full reasoning given in chat and the published "Development Dispatch"
artifact on 2026-07-21, not otherwise saved in the repo):
1. ~~Units~~ — done (Report 012).
2. ~~Placement~~ — done (Report 013).
3. ~~UI.Production~~ — done (Report 014).
4. ~~Selection~~ — done (Report 015).
5. ~~UI.Info~~ — done (Report 016).
6. **Gameplay scene assembly** — wire the brief's 3-area layout together end-to-end; the point
   at which `PlacementController` *and* `SelectionController` finally get added to
   `Gameplay.unity` (a real bootstrap calling `Initialize(grid, ...)` on each once `GridModel`/
   `BuildingFactory`/`UnitFactory` exist in the scene), making the whole produce → place →
   select → move/attack → info-panel loop hand-testable end-to-end for the first time.
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
