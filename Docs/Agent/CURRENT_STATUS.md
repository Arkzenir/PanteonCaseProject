# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 026 (`UI visual polish`), 2026-07-22 — backlog item 14: banner headers on the
Production Menu and Information Panel, plus a Main Menu "How to Play" screen. Pure editor/prefab/
scene wiring (one small code change mirroring the existing Settings-panel pattern exactly). Per
explicit instruction, no batchmode compile/test run this turn — human is testing by eye.

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

**Report 023 (this one) — item 11 off the backlog, "Movement timing fix."**
`SoldierBase.FollowPath` moved from `Vector3.MoveTowards(..., MoveSpeed * Time.deltaTime)` (a
constant *world-distance* speed — diagonal steps took ~41% longer than orthogonal ones at the
same nominal speed) to a fixed per-step duration (`StepDuration(moveSpeed) = 1f / moveSpeed`)
with time-based interpolation (`InterpolateStep`) — both pure static methods, directly unit
tested (matches the human's own worked example: 8 steps at speed 4 = exactly 2s, diagonal mix
irrelevant). **Note:** since the grid's actual `cellSize` is 0.5, existing
`UnitDefinition.moveSpeed` values (3/5/3) now mean a real ~2× slower felt speed than before —
data-only, hand-tune in the Inspector if it feels off.

**Report 024 (this one) — item 13 off the backlog, "Info Panel producible-units layout fix."**
`ProducibleUnitsContainer`'s non-wrapping `HorizontalLayoutGroup` (overflowed sideways past 3
entries) replaced with a built-in `GridLayoutGroup` (Fixed Column Count, default 3) + a
`ContentSizeFitter` (auto-height) on `InformationPanel.prefab` — pure editor/prefab wiring, zero
code change, since neither `InfoPanelController` nor `ProducibleUnitIconView` cares how their
container arranges children. Grid shape/size (cell size, spacing, column count) is fully
adjustable directly in `GridLayoutGroup`'s own Inspector, as requested. Done via a throwaway
setup script (component swap), verified by reading the regenerated prefab back, then deleted.

**Report 025 (this one) — same-day polish-and-bugfix pass following Report 024.**
`PanelContent` (in `InformationPanel.prefab`) gained a `VerticalLayoutGroup` so `BuildingIcon`/
`BuildingName`/`ProducibleUnitsContainer`/`RemoveButton` stack top-to-bottom instead of each
being independently anchored — `RemoveButton` now correctly gets pushed down/up as the
producible-units grid's own content-driven height changes, instead of risking an overlap once
the grid grows past 1 row. Two bugs the human caught while testing, both fixed same-day:
- **Width bug** (human fixed by hand): the initial version left `Force Expand Width` on, which
  stretched *every* child — including `BuildingIcon`/`RemoveButton` — to the full panel width
  regardless of their `LayoutElement.preferredWidth`. Fixed by turning it off and giving
  `BuildingName`/`ProducibleUnitsContainer` an explicit `flexibleWidth = 1` instead, so only
  those two stretch.
- **Layout-timing bug** (fixed in code): selecting a building showed the *previous* selection's
  stale layout for one frame — worse, switching from more producible units to fewer (Barracks →
  Power Plant) never corrected without deselecting first. Fixed in
  `InfoPanelController.SetSelectedBuilding`: forces `LayoutRebuilder.ForceRebuildLayoutImmediate`
  after spawning icons, and `DestroyView` now always uses `DestroyImmediate` (Play Mode's
  deferred `Destroy()` was leaving old icons counted by the grid on the very frame being force-
  rebuilt).

**Report 026 (this one) — backlog item 14, "UI visual polish."** Two pieces:
- **Banner headers** on the Production Menu and Information Panel (not the Game Board).
  `ProductionMenu.prefab`'s root anchor trimmed to free a top strip for a new
  `ProductionMenuBanner` sibling in `Gameplay.unity`'s Canvas — didn't touch the `ScrollRect`'s
  own `Viewport`/`Content` anchors, since their current values look unusual and the agent didn't
  want to risk breaking a working scroll view chasing that down. Information Panel's banner
  added inside `PanelContent`'s existing `VerticalLayoutGroup` stack instead (Report 025) —
  shown/hidden with the rest of the panel's content, not always-on; promoting it to always-
  visible later is a small, well-scoped follow-up if wanted.
- **Main Menu "How to Play" screen** — `MainMenuController` gained a third
  panel/button/back-button, mirroring the existing Settings pattern exactly. Built by
  duplicating the Settings button/panel and stripping the Settings-specific children plus the
  cloned `SettingsController` component (which would otherwise `NullReferenceException` on
  `OnEnable` without its dropdown/toggle/button).

Done via a throwaway script, verified by reading the regenerated scene/prefab files back. Per
explicit instruction, no dedicated batchmode test pass — though the script's own run already
required Unity to compile `MainMenuController.cs`'s change first.

See ARCHITECTURE.md decisions log #52–61 for the full reasoning on each.

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

*Done (Reports 012–026):* ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
~~Gameplay scene assembly~~, ~~Draw-call/batching architecture~~, ~~Camera controls~~,
~~Placement/Grid architecture fixes~~, ~~Building events rearchitecture~~, ~~Selection polish~~,
~~Movement timing fix~~, ~~Info Panel producible-units layout fix~~, ~~UI visual polish~~.

*Backlog* — catalogued 2026-07-22 from the human's own post-hand-test notes after confirming
Report 017 "purely mechanically works." Grouped by which module(s) each touches, not by the
human's original presentation order (they explicitly said regrouping was fine). Suggested
order below is dependency-aware, not a hard requirement — pick freely.

12. **Ranged combat & combat overhaul** (expanded 2026-07-22 with the human's detailed
    follow-up — this is now a materially bigger item than the original 3 bullets):
    - `UnitDefinition` gains: `ranged` (bool), `attackRange` (footprint-cell units; melee units
      can have `attackRange` &gt; 1 too — they just never fire a projectile, only `ranged` units
      do), and a new `attackSpeed` field. *Assumption, confirm at implementation time*: attacks
      per second, mirroring `MoveSpeed`'s existing "N per second" convention (decisions log #57)
      — the human said "attack speed" without specifying the exact unit.
    - **Sustained auto-attack loop, not a single instant hit** — applies uniformly to melee and
      ranged (the human's wording was "attack input" generically, not ranged-specific). Once an
      attack begins, the unit keeps attacking the same target automatically, once every
      `1 / attackSpeed` seconds, until the target either leaves `attackRange` or dies. Today's
      `SoldierBase.TryAttack` is a single instant call (decision #37) — this replaces that with
      persistent per-soldier attack state (current target + timer), most likely coroutine-driven
      to mirror `MoveTo`/`FollowPath`'s existing pattern (brief-mandated Coroutine usage).
    - **Cancellation**: the loop stops if the unit is given a move order — right-clicking an
      empty cell while selected, or being commanded to move any other way. **Confirmed with the
      human**: right-clicking a *different, valid* attack target while already attacking
      switches immediately onto the new target (standard RTS convention, no "attack lock" state
      needed) rather than being blocked until the current attack is canceled first.
    - **Move-then-attack, corrected**: a unit given an attack command while out of range paths
      to the *nearest cell within `attackRange` of the target's current cell* — not onto the
      target's own cell — then starts the auto-attack loop from there. A melee unit with
      `attackRange = 1` ends up on an adjacent cell (explicitly confirmed as the intended
      behavior); a melee unit with a larger configured `attackRange` stops that many cells away
      instead. Same code path for melee and ranged — only the projectile visual differs.
    - **Ranged projectile visual** (fires only when `ranged == true`): purely a visual indicator
      — no `Collider`/`Rigidbody`, never collides with any `GameEntity` it passes over — and
      **actively re-tracks the target's current position every frame while in flight**, not a
      fixed straight-line trajectory toward a snapshot position, since the target may be moving
      and this is needed for visual clarity about which unit is shooting whom. Damage applies on
      arrival at the target's *actual* position, not a precomputed impact point/time.
    - **Particle System vs. MonoBehaviour — determined** (human asked the agent to decide this):
      a plain pooled MonoBehaviour-driven projectile, not a Shuriken `ParticleSystem`. The
      requirement is precise per-target position tracking plus an exact "arrived → apply damage"
      trigger; reproducing that with `ParticleSystem` would mean manually driving individual
      particles via `SetParticles`/`GetParticles` every frame anyway — no ergonomic win over a
      MonoBehaviour, while losing straightforward `Update()`-based homing and clean
      damage-on-arrival timing. `ParticleSystem` is still the right tool for a *separate*, later
      concern (impact bursts, muzzle flash — see backlog item 19) — not for the projectile's own
      flight/tracking, which is this item's actual scope.
    - **Pooling**: since the above determination means prefab instances, pool them in their own
      dedicated `PrefabPool&lt;T&gt;` (mirrors `BuildingFactory`/`UnitFactory`, decision #18) — not
      reused from any other existing pool.
    - **No unit-vs-unit collision** (explicitly confirmed acceptable): units may path through one
      another en route to a destination; this item does not add unit-vs-unit
      blocking/collision — consistent with decision #54's existing scope boundary (routing only
      ever needs to go around *buildings*, GI-7/8, not other units).
    - **Attack-range enforcement** (unchanged from before): this directly supersedes decision #37
      ("Right-click attack is instant and range-less... no textual basis in the brief for range
      enforcement"). That decision was correct *for the brief as written*; this is the human
      explicitly choosing to go beyond the brief's minimum for better game feel —
      update/append to decision #37 when this lands rather than silently overwriting it.
    - Change one soldier (human suggests Soldier 2) to an **Archer** — ranged, uses the
      projectile visual above.
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
