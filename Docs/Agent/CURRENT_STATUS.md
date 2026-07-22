# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 031 (`Camera bounds + terrain follow-up`), 2026-07-22 — human follow-up to Report
030. (1) `GridDefinition` gained `TerrainMargin` (30 world units, up from Report 030's hardcoded
20) as the single source of truth for how far the water backdrop extends — both the baked
`Terrain/Water` Tilemap and `CameraController`'s new bounds read it, so they can't drift apart.
(2) `CameraController` gained `SetBounds`/a pure `ClampToBounds`, applied after every `Pan`/`Zoom`
— the camera can no longer show the background color/skybox past the water's own painted edge;
`GameplayBootstrap` computes the bounds from `GridModel` + `TerrainMargin` and wires them in
`Start()`. (3) Investigated `Tilemap_color1.png`'s actual intended usage via the pack's own
shipped "Unity Tile Guide" documentation (`Assets/Tiny Swords/Terrain/Tileset/Unity Tile Guide/`)
— confirms the flat "Water Background color" tile used in Report 030 is exactly the artist's
intended "BG Color" layer. Manual Tile Palette instructions for painting the actual grass/cliff
island (human will do this by hand, per their own offer) are in Report 031's checklist.
209/209 EditMode tests passing.

**Report 030 (`Environment/terrain visuals`), 2026-07-22** — backlog item 17, scoped to
**purely decorative** per explicit human confirmation (no changes to grid occupancy/pathfinding).
A flat solid-color water `Tile` is painted as a `Tilemap` frame around the grid's bounds
(margin 20 world units), leaving the grid's own interior cells unpainted so the existing camera
background still shows through as "land" — the pack's actual grass/cliff tileset turned out to
be an autotile "floating island" blob set that can't be safely flat-tiled or hand-assembled
without visual verification this environment can't provide, so it was deliberately not used this
pass. A ring of static `Rock1–4.png` decorations (not the pack's animated Bush sprites, for the
same reason) rings the grid's edge, placed via a new pure `BorderDecorationLayout`
(`CaseGame.Environment`). Both are static, one-time-authored scene content baked by a throwaway
script — no new runtime MonoBehaviour. `Tileset`/`Decorations/Rocks` added to
`SpriteAtlas_Gameplay`'s packables. 201/201 EditMode tests passing.

**Report 029 (`Grid line rendering`), 2026-07-22** — backlog item 16. `GridView`'s
Scene-view-only `OnDrawGizmos` replaced with an actual runtime-rendered grid: the whole board's
lines are built as one combined mesh (`GridLineMeshBuilder`, one quad per line) rendered by a
single `MeshRenderer`/`MeshFilter` — one draw call total regardless of board size, protecting
GI-12's <20 SetPass-call budget (one `LineRenderer` per line would have cost dozens). New
`GridLines.shader`/`M_GridLines.mat` (plain vertex-color pass-through). `GridDefinition` gained
`LineColor`/`LineThickness` (data-driven visuals); `GridView` gained `[ExecuteAlways]` + a cheap
Edit-Mode `Update` signature check so tweaking `GridDefinition` previews live without Play Mode,
plus a public `SetLinesVisible(bool)` toggle (no scene UI button wired this pass — the backlog
text called that part optional). 194/194 EditMode tests passing.

**Report 028 (`Combat/UI bugfix pass`), 2026-07-22** — 3 human-flagged fixes following
Report 027. (1) Attacking a building now targets the nearest cell of its actual footprint, not
always its `transform.position` cell — new `GameEntityBase.GetNearestOccupiedCell` (default: own
cell), overridden in `BuildingBase` to clamp into its footprint rectangle; `SoldierBase.AttackRoutine`
uses it for both the range check and approach-pathing. (2) `Projectile` now rotates to face its
travel direction each frame (`FacingRotation`, pure/testable), with an inspector-tunable
`spriteForwardOffsetDegrees` since the Arrow.png art's own default facing wasn't verified in code.
(3) The 3 building/unit icon `Image`s that were stretching fixed-size art (`InformationPanel`'s
`BuildingIcon`, `ProducibleUnitIcon`'s `Image`, `ProductionMenuItem`'s `Icon`) now have
`Preserve Aspect` on — direct prefab edits, no layout change. 186/186 EditMode tests passing.

**Report 027 (`Ranged combat & combat overhaul`), 2026-07-22** — backlog item 12, the
largest single feature so far. `SoldierBase.TryAttack` (instant hit) replaced by `Attack`: walks
into range then sustains a tick loop until the target dies or leaves range; melee/ranged share
the path, only damage delivery differs (instant vs. a new pooled `Projectile`). Soldier 2 is now
the Archer. 179/179 EditMode tests passing (also fixed a real pre-existing bug found along the
way: `InfoPanelControllerTests`' `_panelRoot` lacked a `RectTransform`, breaking Report 025's
`ForceRebuildLayoutImmediate` cast — never caught since that report's turn skipped batch testing).

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

**Report 027 (this one) — item 12, "Ranged combat & combat overhaul."** The full spec from the
expanded backlog item (attack speed, sustained auto-attack, move-into-range, ranged projectile
tracking, no unit collision) landed in one pass:
- `UnitDefinition` gained `Ranged`/`AttackRange`/`AttackSpeed`. `SoldierBase.TryAttack` (a single
  instant hit) is replaced by `Attack(target, grid, projectileFactory)` — walks into range via
  the new `AStarPathfinder.FindApproachCell` if needed, then attacks once every
  `1 / AttackSpeed` seconds until the target dies or leaves range. `MoveTo` and `Attack` share
  one action-coroutine slot, so a move order cancels an attack and vice versa, and a new
  `Attack` call on a different target switches onto it immediately (human-confirmed) — no
  special-case "already attacking" logic needed.
- Melee and ranged share the exact same path; only damage delivery differs
  (`PerformAttack`) — instant for melee, a launched `Projectile` for ranged. New `Projectile`/
  `ProjectileFactory` (`CaseGame.Units`) — a pooled, purely-visual homing indicator (no
  `Collider`, tracks the target's live position every frame, applies damage on arrival). Chosen
  over a `ParticleSystem` — see decisions log #62 for the reasoning.
- Soldier 2 is now the Archer (`ranged: true`, `attackRange: 4`; sprite/damage unchanged).
- `SelectionController.HandleRightClick` now takes `GameEntityBase` (was `IDamageable`) so it
  can pass the target straight through to `Attack`.
- Range/approach checks use the target's own position (not footprint-aware for buildings) to
  avoid a circular `Units → Buildings` dependency — same reasoning as decision #43.
- **Caught and fixed a real pre-existing bug, unrelated to this feature**, while running the
  full test suite: `InfoPanelControllerTests`' `_panelRoot` was a plain `GameObject` lacking a
  `RectTransform`, so Report 025's `LayoutRebuilder.ForceRebuildLayoutImmediate` cast threw —
  never caught since that report's turn skipped batch testing. Test-only fix, no production
  code change needed (the real `PanelContent` object always had a `RectTransform`).

179/179 EditMode tests passing, 0 compile errors.

See ARCHITECTURE.md decisions log #52–62 for the full reasoning on each.

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

*Done (Reports 012–031):* ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
~~Gameplay scene assembly~~, ~~Draw-call/batching architecture~~, ~~Camera controls~~,
~~Placement/Grid architecture fixes~~, ~~Building events rearchitecture~~, ~~Selection polish~~,
~~Movement timing fix~~, ~~Info Panel producible-units layout fix~~, ~~UI visual polish~~,
~~Ranged combat & combat overhaul~~, ~~Combat/UI bugfix pass~~, ~~Grid line rendering~~,
~~Environment/terrain visuals~~, ~~Camera bounds + terrain follow-up~~.

*Backlog* — catalogued 2026-07-22 from the human's own post-hand-test notes after confirming
Report 017 "purely mechanically works." Grouped by which module(s) each touches, not by the
human's original presentation order (they explicitly said regrouping was fine). Suggested
order below is dependency-aware, not a hard requirement — pick freely.

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
