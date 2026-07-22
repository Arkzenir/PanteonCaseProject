# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 014 (`UI.Production`), 2026-07-22. Compile clean,
**96/96 EditMode tests passing** (80 prior + 16 new).

**Pending on the human:** nothing blocking from Report 014 — the throwaway setup script did
the scene/prefab/asset wiring itself and it was verified by reading the generated files back.
Report 013's editor hookup (`VisualsGrayscale` child + `BuildingGhostView` on the building
prefabs, `M_SpriteGrayscaleGhost` material) is still open if not already done by the human —
check Report 013 if unsure. Requirement 19 (Main Menu) remains the one fully-closed brief
requirement; everything else in the checklist is still `[ ]` (Report 014 doesn't fully close
requirement 2 or 15 — the Production Menu itself works and is tested, but the brief's 3-area
layout and the Info Panel don't exist yet, see ARCHITECTURE.md's Report 014 log entry).

**Modules with real, tested code:** Core (`GameManager`), Grid, Entities (shared
`GameEntityDefinition`/`GameEntityBase`), Combat, Buildings (now also `BuildingCatalog`/
`BuildingCatalogEntry`/`BuildingCatalogEntryEventChannel`), Units (`SoldierBase`/`Soldier`/
`UnitFactory` — note: 3 soldier *types* are `UnitDefinition` data, not 3 classes, see
decisions log #26), Placement (`BuildingGhostView`/`PlacementController` — now also
subscribes to the produce-request channel), UI.Production (`ScrollRecycler`/
`ProductionMenuItemView`/`ProductionMenuController`), Events, Pooling, Pathfinding.

**Not yet built:** Selection, UI.Info, actual Gameplay scene assembly, draw-call/batching
verification, Windows build export.

**Recommended next-feature order** (dependency-driven — each step only needs what's already
shipped above it; full reasoning given in chat and the published "Development Dispatch"
artifact on 2026-07-21, not otherwise saved in the repo):
1. ~~Units~~ — done (Report 012).
2. ~~Placement~~ — done (Report 013).
3. ~~UI.Production~~ — done (Report 014). Note: `PlacementController.Initialize(grid, factory)`
   is still never called from a scene, so the Production Menu's "produce" click can't be
   hand-tested end-to-end yet — that bootstrap wiring is Gameplay scene assembly's job (step 6).
4. **Selection** — left-click select / right-click move-or-attack. Needs Units to exist first.
5. **UI.Info** — Information Panel, listens for the selection event.
6. **Gameplay scene assembly** — wire the brief's 3-area layout together end-to-end (also the
   point at which `PlacementController` finally gets added to `Gameplay.unity` and initialized).
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
