# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 018 (`Draw-call/batching architecture`), 2026-07-22. Compile clean (no C# code
touched — asset/settings only), **127/127 EditMode tests passing** (unchanged from Report 017).

**Human hand-tested Report 017 since it landed: "purely mechanically, the system works."**
Adjustments were made directly in-Editor (not captured as a report — no code/doc changes came
back from that pass). Visual polish is now underway in parallel (real art — the "Tiny Swords"
pack — has replaced placeholder sprites; see ARCHITECTURE.md §4).

**Report 018 scope, and why it's split:** the human asked when to do the draw-call/batching
pass — before or after visual polish — and the agreed answer was to split it: batching
*architecture* now (this report), numeric *verification* (actually counting draw calls against
the &lt;20 budget) later, once visual polish is further along (decisions log #49). This report is
architecture only:
- Confirmed URP's SRP Batcher was already on (project-default, no action needed).
- Confirmed every building/unit prefab's `Visuals` renderer already shares one material — the
  other precondition for batching was already correct.
- The real gap: no `SpriteAtlas` existed, so same-material sprites from *different* building/
  unit types still cost separate draw calls (Unity's sprite batching needs matching texture,
  not just material). Added `SpriteAtlas_Gameplay.spriteatlas` covering the actual art folders.
- Enabled GPU Instancing on the project's own `M_SpriteGrayscaleGhost` material (was off) — the
  brief names GPU Instancing as its own required pattern separate from batching.
- No C# touched. Verified by reading the generated `.spriteatlas`/`.mat` files back.

**The project is feature-complete and assembled; mechanically hand-tested once (Report 017) but
not yet re-verified after visual polish began.** Report 017's audit checklist
(`Docs/Reports/017_gameplay-scene-assembly.md`) is still the reference for what to click
through if anything seems off after further polish.

**What Report 017 found and fixed (read the report before assuming the obvious wiring pass is
all this feature did):**
- **Units were never producible** — the Production Menu only lists buildings, and Report 016
  had built the Info Panel's producible-unit icons non-interactive. Re-reading requirements 5/6
  together made clear the Info Panel's producible-unit row *is* GI-6's "production sub-menu" —
  fixed: those icons are now clickable and actually spawn a soldier (`UnitCatalogEntry`,
  `UnitProductionController`, new `CaseGame.Units` types — see decisions log #41–44).
- **Two data bugs in existing assets**, found by reading the raw YAML, not assumed correct:
  `BuildingDef_Barracks`/`BuildingDef_PowerPlant` had an empty `entityName` (stale pre-Report-010
  field name), and `UnitDef_Soldier3` had 1 HP/1 damage instead of the brief's mandated 10/2.
  Both fixed (decisions log #46).
- **UI-vs-world input conflict** — `PlacementController`/`SelectionController` read raw mouse
  input with no UI awareness; clicking a Production Menu row would *also* fire a world-space
  action underneath it. Fixed with `EventSystem.current.IsPointerOverGameObject()` guards
  (decisions log #47).
- Camera repositioned/resized to frame the grid, computed from `GridDef_Default.asset` and the
  UI panels' known width — **not visually verified**, flagged in the audit checklist (#48).

**Modules with real, tested code — every one of them now wired into `Gameplay.unity` too:**
Core (`GameManager`), Grid, Entities (`GameEntityDefinition`/`GameEntityBase` + `SetSelected`),
Combat, Buildings (`BuildingCatalog`/`BuildingCatalogEntry`/`BuildingCatalogEntryEventChannel`/
`SelectedBuildingEventChannel`, `BuildingBase.SpawnPosition` virtual), Units (`SoldierBase`/
`Soldier`/`UnitFactory`/`UnitCatalogEntry`/`UnitProductionController` — 3 soldier *types* are
`UnitDefinition` data, not 3 classes, see decisions log #26), Placement (`BuildingGhostView`/
`PlacementController`), UI.Production (`ScrollRecycler`/`ProductionMenuItemView`/
`ProductionMenuController`), Selection (`SelectionController`), UI.Info (`InfoPanelController`/
`ProducibleUnitIconView`, now clickable), **Gameplay** (new module, `GameplayBootstrap` — the
scene's composition root), Events, Pooling, Pathfinding.

**Not yet built:** draw-call/batching *numeric verification* (architecture is done, see above),
Windows build export, `/final-report`. Visual polish itself is the human's own ongoing work,
not a queued agent feature.

**Recommended next-feature order:**
1–7. ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
   ~~Gameplay scene assembly~~, ~~Draw-call/batching architecture~~ — all done (Reports 012–018).
8. **Draw-call/batching verification** — once visual polish is far enough along that the numbers
   will actually reflect final content: enter Play Mode, open the Stats window (or Frame Debugger
   for exact SetPass call attribution), confirm &lt;20, and adjust (more atlasing, fewer unique
   materials, etc.) if not.
9. **Windows build + `/final-report`**.

**Known environment gotchas** (full detail in `ENVIRONMENT.md`): `Awake`/`OnEnable` don't
reliably fire on `AddComponent`-created objects in this machine's batchmode EditMode test
runner, **nor on scene objects loaded via `EditorSceneManager.OpenScene` outside Play Mode**
(confirmed this feature, Report 017 — a throwaway script reading `GridView.GridModel` right
after opening the scene got null; fixed by constructing a fresh `GridModel` from the
`GridDefinition` asset directly instead, plain-C# and Awake-independent). Asmdef `references`
are not transitive. Always verify a throwaway editor script's result by reading the generated
files back, not just trusting a clean exit code.

**Standing conventions to remember** (full detail in `CONVENTIONS.md` overrides table): prefer
typed asset-picker `SerializeField`s over raw strings (see `SceneReference`); SO definition
assets live under `Assets/_Project/ScriptableObjects/GameEntityDefs/<Category>/`, not
`Settings/`; building/unit prefabs are a parent GameObject with child GameObjects per concern
(`Visuals`, `Hitbox`, `SpawnPoint`, ...) — buildings under `Prefabs/Buildings/` named
`Building_<Name>`, soldiers under `Prefabs/Units/` named `Soldier_<N>` (same `<Category>_<Name>`
convention). UI prefabs live under `Prefabs/UI/` with a plain name, no category prefix (e.g.
`ProductionMenuItem.prefab`); root-level config/data SO assets (not entity defs) use
`<Type>_Default.asset` at the `ScriptableObjects/` root (e.g. `BuildingCatalog_Default.asset`).
Scene-specific composition roots (scene bootstraps) get their own small module rather than
living in Core (`CaseGame.Gameplay`/`GameplayBootstrap`, Report 017) — Core stays generic/
reusable across scenes.
