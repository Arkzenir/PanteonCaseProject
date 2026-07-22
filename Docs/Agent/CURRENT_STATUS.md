# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 022 (`Selection polish`), 2026-07-22. Compile clean,
**161/161 EditMode tests passing** (160 prior + 1 new).

**Earlier history, condensed** (full detail in ARCHITECTURE.md's implementation log if needed):
Report 017 (Gameplay scene assembly) was hand-tested once by the human and confirmed "purely
mechanically, the system works"; visual polish (the "Tiny Swords" art pack) started in parallel
and is ongoing. Report 018 built the draw-call/batching *architecture* only (Sprite Atlas, GPU
Instancing on the project's own material) — numeric verification is deliberately deferred until
polish is further along (decisions log #49). Report 019 added `CameraControl` (pan/zoom).
None of 018–020 have been hand-tested in Play Mode yet.

**Report 020 — first item off the post-hand-test backlog, "Placement/Grid architecture
fixes."** Three related fixes:
- **Center-anchored footprint placement** — the hovered cell is now the footprint's center, not
  its bottom-left corner (was visibly misaligning centered building art from its footprint).
- **"Remove Building"** on the Info Panel — originally implemented as `ApplyDamage(MaxHealth)`,
  reusing the death pipeline; **corrected by Report 021, see below.** Surfaced and fixed a real
  pre-existing bug along the way: combat-destroyed buildings never released their grid cells
  either (nothing connected "died" to "unoccupy") — fixed via a new
  `GameEntityBase.OnEntityDied()` hook, which still stands.
- **Unit spawn-cell occupancy** — units spawn snapped to the nearest cell to their Barracks'
  spawn point, blocked if that cell has a building or another unit on it (a live `UnitFactory`
  scan, not a persisted occupancy grid — scoped to spawn-time only, not unit pathfinding).

**Report 021 (this one) — human-directed correction to Report 020's "Remove Building" design.**
The required event shape: Buildings — Creation → Event, Removal → Event, Death → Handled by
health; Units — Creation → Event, Death → Handled by health. Removal was wrong (folded into
Death); fixed with a new `BuildingRemovalRequestedEventChannel` — `InfoPanelController` raises
it, `PlacementController` subscribes and does the actual removal (`ReleaseFootprint` +
`BuildingFactory.Release`, zero Health involvement), `SelectionController` subscribes to clear a
stale selection reactively (removal no longer sets `IsDead` for the old lazy check to catch).

**Report 022 (this one) — item 10 off the backlog, "Selection polish."** Replaced
`GameEntityBase.SetSelected`'s color-tint feedback with a real outline: new hand-authored
`SpriteSelectionOutline.shader`/`M_SpriteSelectionOutline.mat` (neighbor-alpha-sampling ring,
same style as the Placement ghost's shader) drawn by a new `outlineRenderer` child on all 5
entity prefabs, toggled via `SpriteRenderer.enabled`. Wired via a throwaway editor script,
verified by reading the regenerated prefab/material files back, then deleted per policy.

See ARCHITECTURE.md decisions log #52–56 for the full reasoning on each.

**Modules with real, tested code — every one of them now wired into `Gameplay.unity` too:**
Core (`GameManager`), Grid (`GridModel` + `FootprintCenterToWorld`), Entities
(`GameEntityDefinition`/`GameEntityBase` + `SetSelected`/`OnEntityDied`), Combat, Buildings
(`BuildingCatalog`/`BuildingCatalogEntry`/`BuildingCatalogEntryEventChannel`/
`SelectedBuildingEventChannel`, `BuildingBase.SpawnPosition`/`SetPlacement`), Units
(`SoldierBase`/`Soldier`/`UnitFactory` + `ActiveUnits`/`UnitCatalogEntry`/
`UnitProductionController` — 3 soldier *types* are `UnitDefinition` data, not 3 classes,
decisions log #26), Placement (`BuildingGhostView`/`PlacementController`), UI.Production
(`ScrollRecycler`/`ProductionMenuItemView`/`ProductionMenuController`), Selection
(`SelectionController`), UI.Info (`InfoPanelController`/`ProducibleUnitIconView` — Remove
Building button), **Gameplay** (`GameplayBootstrap`), **CameraControl** (`CameraController`),
Events, Pooling, Pathfinding.

**Not yet built:** the rest of the polish/mechanical-adjustment backlog below (items 10
onward), draw-call/batching *numeric verification* (architecture is done), Windows build
export, `/final-report`.

**Recommended next-feature order:**

*Done (Reports 012–022):* ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
~~Gameplay scene assembly~~, ~~Draw-call/batching architecture~~, ~~Camera controls~~,
~~Placement/Grid architecture fixes~~, ~~Building events rearchitecture~~, ~~Selection polish~~.

*Backlog* — catalogued 2026-07-22 from the human's own post-hand-test notes after confirming
Report 017 "purely mechanically works." Grouped by which module(s) each touches, not by the
human's original presentation order (they explicitly said regrouping was fine). Suggested
order below is dependency-aware, not a hard requirement — pick freely.

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
16. **Grid line rendering (runtime, data-driven, toggleable)** — human-added 2026-07-22.
    `GridView` currently draws lines only via `OnDrawGizmos` — Scene-view/editor-only, never
    visible in Play Mode or a build. Replace with an actual runtime-rendered grid (`LineRenderer`
    or equivalent), with four specific requirements:
    - **Toggleable on/off**, via an API call/event *and* optionally a UI button — needs a public
      method/property on `GridView` (or a new small `GridVisualController`) that something can
      call/bind to; where the UI button lives (Settings? an in-scene toggle?) is open, human's
      call at implementation time.
    - **Data-driven visuals** (color, line thickness, etc.) — extend `GridDefinition` with new
      fields, same pattern as the existing `cellSize`/`columns`/`rows`/`originWorldPosition`
      (decision #5: nothing about the grid is hardcoded).
    - **Renders behind everything else** — sorting layer/order on the line-rendering component
      set so buildings/units/ghosts are never occluded by grid lines.
    - **Live-updates in Edit Mode** as `GridDefinition` values change in the Inspector, without
      entering Play Mode — real implementation challenge: `GridView.Awake()` (where `GridModel`
      is currently built) doesn't fire in Edit Mode at all (the same Awake-doesn't-fire-outside-
      Play-Mode gotcha `ENVIRONMENT.md`/decisions log already document elsewhere). Likely needs
      `[ExecuteAlways]` plus rebuilding the rendered lines from `OnValidate`/a change-driven hook,
      not relying on `Awake`.
17. **Environment/terrain visuals** — likely the largest single item in this backlog:
    - Tilemap terrain backdrop so the ground isn't empty/skybox behind the grid. Optionally tie
      specific tile types (forest, mountain) to *inherent* grid occupancy — i.e. some cells are
      permanently unplaceable/unwalkable because of the terrain tile there, not just because a
      building was placed. If pursued, this interacts directly with Report 020's unit
      spawn-cell-occupancy work (above) — worth designing together, since both touch "what
      counts as occupied."
    - Auto-generated/placed border tiles around the grid's edges (e.g. forest/mountain), so the
      board's boundary reads visually, not just via the grid lines (item 16, above).
    - Once terrain art exists, add it to `SpriteAtlas_Gameplay.spriteatlas` (Report 018) —
      folder-level packing already covers *building/unit* art automatically; terrain art will
      likely live in a different source folder and need its own packable entry added.
18. **Unit animations** — idle/move/attack, ideally sequenced after item 11 (movement timing)
    and item 12 (ranged combat) land, so animation trigger points match final movement/attack
    logic rather than needing rework. The Tiny Swords pack already ships full Animator
    Controllers + clips for these unit types (noticed during Report 018's audit) — this may be
    mostly wiring an `Animator`/`AnimatorController` onto the soldier prefabs and triggering
    states from `SoldierBase`, not building animation from scratch.
19. **Death/destruction particle effects** — a burst/VFX on unit death and building destruction.
    Small and low-dependency: both already funnel through the same shared `Health.Died` event
    (`GameEntityBase`, decisions log #19), so this is likely one hookup point (a particle-prefab
    spawn on death, played *before* — or independent of — the instance returning to its pool),
    not two separate systems for units vs. buildings. Human-added 2026-07-22.

*After the backlog above:*
20. **Draw-call/batching verification** — once visual polish (including the terrain/tilemap
    work) is far enough along that the numbers reflect final content: enter Play Mode, open the
    Stats window (or Frame Debugger for exact SetPass call attribution), confirm &lt;20, and adjust
    if not.
21. **Windows build + `/final-report`**.

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
