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

**Not yet built:** the polish/mechanical-adjustment backlog below (catalogued 2026-07-22 from the
human's post-hand-test notes, not yet started), draw-call/batching *numeric verification*
(architecture is done, see above), Windows build export, `/final-report`.

**Recommended next-feature order:**

*Done (Reports 012–018):* ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
~~Gameplay scene assembly~~, ~~Draw-call/batching architecture~~.

*Backlog* — catalogued 2026-07-22 from the human's own post-hand-test notes after confirming
Report 017 "purely mechanically works." Grouped by which module(s) each touches, not by the
human's original presentation order (they explicitly said regrouping was fine). Suggested
order below is dependency-aware, not a hard requirement — pick freely.

8. **Camera controls** — middle-mouse-drag pan, scroll-wheel zoom. Standalone, no dependencies;
   good first pick. Touches: nothing existing, purely additive (likely its own small
   `CameraController` in `CaseGame.Gameplay` or `CaseGame.Core`, or added to `GameplayBootstrap`'s
   scene — human's call at implementation time).
9. **Placement/Grid architecture fixes** — do before the two items below, since both build on
   its corrected footprint math:
   - Building footprint origin: currently bottom-left-anchored (`GridModel.CellToWorld`/
     `SetAreaOccupied`'s existing corner-based math, decision #5); needs to become
     **center-anchored** — the building's placement "origin" cell should be the *center* of its
     footprint, matching how the (now real, Tiny-Swords-sourced) visual art is centered on the
     GameObject's own origin. Touches `GridModel`, `BuildingDefinition.Footprint` semantics,
     `PlacementController`'s cell math.
   - **"Remove Building" button** on the Information Panel — a demolish action (the inverse of
     Placement/Production): needs to release the building's occupied footprint cells (today
     only death/`ApplyDamage`-to-0 does anything like this, and that doesn't unoccupy the grid
     either — check `BuildingBase`/`Health`'s death path when implementing this, it may have the
     same gap).
   - **Unit spawn cell occupancy**: units should spawn on the grid cell closest to/overlapping
     the Barracks' `SpawnPoint` (not the prefab's raw world position), and spawning should be
     **blocked** if that cell is already occupied — by a building *or* another unit. This is a
     real model extension: `GridModel`'s occupancy grid currently only tracks buildings
     (`SetAreaOccupied`, called only by `PlacementController`) — units moving around never
     register/clear occupancy today. Needs design thought at implementation time: do units
     occupy a cell only while stationary, or always (blocking pathing through them too)? The
     brief only requires "routes around buildings" (GI-7/8), not units — don't over-scope this
     into full unit-collision/pathfinding-around-units unless asked.
10. **Selection polish**:
    - Selection outline visual for units — a real outline (shader/sprite-mask/child renderer),
      likely alongside or replacing `GameEntityBase.SetSelected`'s current color-tint approach
      (decision #38's fix would still apply — reset on `Initialize` for pooled reuse).
    - Re-verify "left-click empty ground clears selection" — this should already work
      (`SelectionController.HandleLeftClick`'s non-additive/null-hit branch, Report 015); the
      human listed it as needed, so either it regressed, there's an edge case (e.g. clicking
      terrain once tilemap exists) it doesn't cover, or it just needs re-confirming now that the
      full scene is wired. Check before assuming it needs new code.
11. **Movement timing fix** — `SoldierBase.MoveSpeed` should mean "N grid cells per second,"
    with a **diagonal step counting as 1 cell**, not √2 world-distance units (today
    `FollowPath`'s `Vector3.MoveTowards(..., MoveSpeed * Time.deltaTime)` moves at a constant
    *world-distance* speed, so diagonal steps currently take longer than orthogonal ones at the
    same nominal speed). Revisit `SoldierBase.FollowPath`'s per-step timing math — likely needs
    a fixed per-step duration (`1f / MoveSpeed` seconds) rather than distance-based
    `MoveTowards`, so a path with 5 orthogonal + 3 diagonal steps at speed 4 takes exactly
    8 × 0.25s = 2s regardless of diagonal mix (the human's own worked example).
12. **Ranged combat**:
    - `UnitDefinition` gains a `ranged` bool and an `attackRange` field (footprint-cell units,
      presumably) — melee units can have `attackRange` &gt; 1 too (just no projectile); only
      `ranged` units fire one.
    - Change one soldier (human suggests Soldier 2) to an **Archer** — ranged attack, fires a
      simple non-colliding projectile visual (a moving GameObject/particle tracking toward the
      target, dealing damage on arrival — no physics/collision needed, purely visual timing).
    - **Attack-range enforcement**: right-click attack while out of range should move the
      soldier to the nearest in-range cell first, *then* attack — this directly supersedes
      decision #37 ("Right-click attack is instant and range-less... no textual basis in the
      brief for range enforcement"). That decision was correct *for the brief as written*; this
      is the human explicitly choosing to go beyond the brief's minimum for better game feel —
      update/append to decision #37 when this lands rather than silently overwriting it.
13. **Info Panel producible-units layout fix** — the producible-unit icon row currently uses a
    `HorizontalLayoutGroup` (Report 016/017) and overflows sideways past 3 entries; switch to a
    vertical/downward-tiling layout that fits the panel. Small, standalone UI fix
    (`InfoPanelController`'s `producibleUnitsContainer` + the prefab-side layout component).
14. **UI visual polish**:
    - Banner header at the top of the Production Menu and Information Panel (per the human's
      reference mockup) — **not** on the Game Board.
    - Main Menu "How to Play"/"Controls" screen — a new screen alongside the existing Play/
      Settings flow (`CaseGame.UI.MainMenu`), explaining basic controls.
15. **Production Menu scroll fix** — dragging the scroll fast enough currently loses the
    top-of-list items (not the bottom ones); worked around by the human setting
    `ScrollRect.movementType` to `Clamped` directly in the scene (see ARCHITECTURE.md §4) instead
    of the Unity-default `Elastic`. Decide at implementation time whether `Clamped` is the
    permanent fix or whether `ScrollRecycler`/`ProductionMenuController`'s own recycling math
    (decisions #31/#34) has a real bug worth fixing instead.
16. **Environment/terrain visuals** — likely the largest single item in this backlog:
    - Tilemap terrain backdrop so the ground isn't empty/skybox behind the grid. Optionally tie
      specific tile types (forest, mountain) to *inherent* grid occupancy — i.e. some cells are
      permanently unplaceable/unwalkable because of the terrain tile there, not just because a
      building was placed. If pursued, this interacts directly with item 9's `GridModel`
      occupancy-extension work above — worth sequencing after it, or designing both together.
    - Auto-generated/placed border tiles around the grid's edges (e.g. forest/mountain), so the
      board's boundary reads visually, not just via the (currently gizmo-only, not even
      runtime-visible) grid lines.
    - Once terrain art exists, add it to `SpriteAtlas_Gameplay.spriteatlas` (Report 018) —
      folder-level packing already covers *building/unit* art automatically; terrain art will
      likely live in a different source folder and need its own packable entry added.
17. **Unit animations** — idle/move/attack, ideally sequenced after item 11 (movement timing)
    and item 12 (ranged combat) land, so animation trigger points match final movement/attack
    logic rather than needing rework. The Tiny Swords pack already ships full Animator
    Controllers + clips for these unit types (noticed during Report 018's audit) — this may be
    mostly wiring an `Animator`/`AnimatorController` onto the soldier prefabs and triggering
    states from `SoldierBase`, not building animation from scratch.
18. **Death/destruction particle effects** — a burst/VFX on unit death and building destruction.
    Small and low-dependency: both already funnel through the same shared `Health.Died` event
    (`GameEntityBase`, decisions log #19), so this is likely one hookup point (a particle-prefab
    spawn on death, played *before* — or independent of — the instance returning to its pool),
    not two separate systems for units vs. buildings. Human-added 2026-07-22.

*After the backlog above:*
19. **Draw-call/batching verification** — once visual polish (including the terrain/tilemap
    work) is far enough along that the numbers reflect final content: enter Play Mode, open the
    Stats window (or Frame Debugger for exact SetPass call attribution), confirm &lt;20, and adjust
    if not.
20. **Windows build + `/final-report`**.

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
