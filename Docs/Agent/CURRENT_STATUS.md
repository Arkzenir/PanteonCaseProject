# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 017 (`Gameplay scene assembly`), 2026-07-22. Compile clean,
**127/127 EditMode tests passing** (120 prior + 7 new). Every module now lives, wired, in
`Gameplay.unity` — this was the full integration pass, plus a requested audit that found and
fixed real gaps (see below), not just wiring.

**The project is feature-complete and assembled, but *unverified by a human in Play Mode*.**
The agent cannot enter Play Mode — everything below "works" per EditMode tests + reading the
generated scene/asset files back, not per actually playing it. Report 017's audit checklist
(in `Docs/Reports/017_gameplay-scene-assembly.md`) is the priority: a specific, ordered list of
things to actually click through in the Editor. Until that happens, treat every requirement
below as "implemented, not confirmed."

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

**Not yet built:** draw-call/batching verification, Windows build export, `/final-report`. That's
the entire remaining roadmap.

**Recommended next-feature order:**
1–6. ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
   ~~Gameplay scene assembly~~ — all done (Reports 012–017).
7. **Hand-test pass** (human, not a feature turn) — work through Report 017's audit checklist
   in the Editor. Very likely to surface small issues (camera framing, spawn point placement,
   scroll feel) that are cheap fixes once seen, per the report's own flagged uncertainties.
8. **Draw-call/batching pass** — only measurable once real content exists to profile; now it
   does.
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
