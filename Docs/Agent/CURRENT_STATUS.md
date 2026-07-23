# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**BRANCH SPLIT, 2026-07-23 — read this first if you're not sure which branch you're on.**
Three branches now exist, all forked from the same point (this exact commit, docs identical
across all three as of this note):
- **`main`** — frozen. No further work here until the human picks a final direction below.
- **`VisualPolish`** — the fully-polished build exactly as it stands through Report 039 plus the
  draw-call fixes below (`Mask`→`RectMask2D`, HDR/Shadows off). Human-measured: right at, or just
  slightly over, 20 SetPass calls under heavy combat (several units attacking multiple buildings
  at once). This branch stays as-is — no more cuts.
- **`Optimisation`** — will aggressively remove/simplify visual-polish features specifically to
  get comfortably under 20 with real headroom: candidates named so far are replacing the Tilemap
  terrain with a plain ground plane, removing the particle effects (Report 039) entirely, and
  replacing the `Outline` selection shader with a simple marker sprite (a chevron from the UI
  atlas, or similar). Each removal should land as its own feature turn (implement/verify/report),
  same as everything else.

Plan: measure both once `Optimisation`'s cuts are in, compare against the brief's evaluation
criteria, then merge whichever (or a blend) into `main` for the final build. See ARCHITECTURE.md
decisions log #77 for the full reasoning. **If you're picking up a fresh/compacted session, check
`git branch` before assuming which of these three you're working in.**

**`Optimisation` branch note, 2026-07-23:** an earlier Outline→SelectionMarker cut (previously
numbered Report 040) was reverted — human decided it "amounted to nothing" — the branch was reset
with `git reset --hard` back to the branch-split commit and force-pushed. Report numbering
restarted from 040 for the next actual cut (the Tilemap→baked-texture terrain replacement,
below) — the selection-marker cut is not currently landed on this branch.

**Last report (`Optimisation` branch only): 040 (`Tilemap terrain → single baked quad`), 2026-07-23**
— human A/B-tested disabling the `Terrain` GameObject (all 3 Tilemaps) entirely in favor of a
plain placeholder rectangle sprite and measured SetPass/draw calls roughly **halving** in active
combat — confirmed root cause (ARCHITECTURE.md decisions log #78): all 3 Tilemaps already share
the identical material entities use (`Sprite-Lit-Default`), but `TilemapRenderer`'s rendering path
can't join the SRP Batcher regardless, and further splits into 32×32-cell chunks (this grid's
painted area, 80×72 world units, spans several). Fix: `IslandTerrainView`/`IslandTilemapLayout`
keep procedurally generating the island for any grid size exactly as before, but
`TerrainCompositor` (new, `CaseGame.Environment`) now bakes all 3 Tilemap layers into one texture
on a single `SpriteRenderer` quad (`BakedTerrain`, sorting order -2000) at `Gameplay.unity`'s
load, via a dedicated orthographic bake camera + `RenderTexture`, then hides the source Tilemaps
— full tile-art fidelity kept, not downgraded to a flat color. Bake framing reuses a new pure
`TerrainBounds.Compute` (`CaseGame.Grid`, extracted from `GameplayBootstrap`'s previous inline
calc, also now used for the camera pan/zoom clamp) so the two can never drift apart; bake
resolution (`TerrainBakeResolution`) preserves the tileset's native 64px/unit density up to a
4096px ceiling, scaling down uniformly only if a larger grid would exceed it. 238/238 EditMode
tests passing (232 + 6 new, both pure helpers directly tested), 0 compile errors. One open,
deliberately-deferred item: the isolated bake-camera culling layer is left unnamed (raw index 8)
since naming it needs a `ProjectSettings/TagManager.asset` edit, gated behind explicit approval.

**Human follow-up (same day):** first hand-test showed a blank bake (no island/water at all) —
root-caused and fixed same-day: the bake camera's Z position was never moved off the Tilemaps'
own Z=0 plane, so it captured nothing (decisions log #79). Re-tested after the fix: terrain bakes
and displays correctly, Profiler confirms **under 10 SetPass/draw calls at idle** — comfortably
clear of GI-12's <20 budget. One expected, non-blocking observation: a one-time ~25-SetPass spike
on the very first loaded frame (the bake camera's own render of the still-live, not-yet-hidden
Tilemaps landing in that one frame's tally) — inherent to a synchronous one-shot bake, happens
once before the Production Menu is even interactive, doesn't recur, not a GI-12 concern (see
decisions log #79 for the full reasoning on why this is fine as-is). 3 ways to suppress/smooth it
were discussed and explicitly declined by the human — see decisions log #79's alternatives-rejected
column and Report 040 §6: in a real (non-evaluation) production scenario, this exact cost is what
a loading screen/transition exists to absorb (this project has none, out of scope).

**In progress — backlog item 20 (draw-call/batching verification), 2026-07-23:** human-driven
Profiler/Frame Debugger investigation into GI-12's <20 SetPass budget, ahead of a feature turn to
implement the remaining items. No code changed yet this pass except what's noted as "done" below.

*Measurements so far* (`SetPass Calls` from the Profiler Rendering module, the literal GI-12
metric — Frame Debugger step counts undercount this since some pipeline operations cost a SetPass
without a corresponding "batch"): idle started at 24, heavy combat (3 archers + 2 buildings + 2
particles firing) started at 28.

*Done already (human, directly in-editor, not yet in a report):*
- `ProductionMenu`'s Viewport: `Mask` → `RectMask2D` (removes stencil push/pop; also let
  previously-mask-separated draws merge, since `Mask`'s stencil operations were a hard SRP-batch
  boundary — bigger win than the stencil draws alone would suggest). Idle 24 → 16.
- Disabled HDR and Cast Shadows (camera/URP asset). Idle 16 → 15 (shadow *pass* itself was
  costing 1 batch/1 SetPass even with 0 actual casters; HDR mainly freed Render Texture memory,
  14 → 12).
- Catalog trimmed from 12 to 6 entries — confirmed **no effect** on SetPass (Canvas batching
  merges same-material draws regardless of how many objects are in the run; item *count* only
  affects vertex count, not draw/SetPass count, since `ProductionMenu`'s panel is tall enough that
  most/all entries are simultaneously visible either way).

*Root-caused, not yet fixed:* `RectMask2D` still applies the `UNITY_UI_CLIP_RECT` shader keyword
to everything inside the Viewport/Content subtree — a different effective material variant than
anything outside it. That's why `ProductionMenu`'s banner (outside the mask, fixed header) can't
merge with its rows (inside the mask, scrollable) — structurally inherent to "scrollable + clipped
+ fixed header," which `InformationPanel` doesn't need (no ScrollRect/Mask at all), not a bug.

*Candidate remaining optimizations, ranked most → least worth doing* (estimates are per-item
ceilings, mostly from the SetPass-vs-Batches gap staying ~constant at ~7-8 across very different
content, i.e. it's mostly fixed pipeline overhead, not something more content-side batching fixes):

~~1. Consolidate `Water`/`Island_Grass`/`Island_Cliff` into one `Tilemap`/`TilemapRenderer`~~ —
   **ruled out, confirmed no benefit.** Human tested at the camera's actual maximum zoom-out
   (`CameraController`'s configured `maxOrthographicSize` — the real worst case, water dominating
   the entire frame) and the Frame Debugger tree still showed exactly 1 `TilemapRenderer.Render`
   draw. Unity's 2D Tilemap renderer is already combining all 3 Tilemaps into a single draw call
   on its own (they share the same default Tilemap material/sorting settings), at every camera
   position the game allows. Doing the merge would be pure refactor risk for zero gain — dropped.
1. **Unify `DamageEffect`/`DeathEffect` onto one shared, *textured* particle Material** (reuse one
   spritesheet — e.g. `Dust_01` — for both, differing only by a per-instance color tint instead
   of two separate Materials/spritesheets). Small visual cost (both effects share one animated
   shape now, just recolored) but keeps a real animated look. Up to −1 SetPass, only when both
   effects are firing simultaneously (never helps idle).
2. **Unify particles onto one shared, *untextured* (flat-color quad) Material + enable GPU
   Instancing on it.** Bigger visual cost (loses the animated dust/explosion look entirely — flat
   tinted shapes instead). Same capped −1 from the SRP-Batcher merge as option 1, but *also*
   opens the door to GPU Instancing actually engaging (identical mesh/UVs across instances, no
   per-particle-timed animation breaking it) — could merge any number of simultaneous instances
   into one draw, not just two. Larger, less certain upside for a real look downgrade.
3. **`GridLines` → reuse the shared default sprite material** (`Sprite-Lit-Default`, already used
   by every entity) instead of the custom `CaseGame/GridLines` shader. Requires real code work
   (`GridLineMeshBuilder` would need to emit UVs targeting a genuinely-white texel in that shared
   atlas — currently emits none) and loses per-`GridDefinition` `LineColor` tuning. Capped at −1,
   and only when entities are on-screen and drawn adjacent in sort order (GridLines already
   renders as exactly 1 SetPass on its own regardless of board size — this only helps it *merge*
   into an already-open batch, not shrink further on its own). Highest effort-to-guaranteed-gain
   ratio of the batching-technique options.
4. **Drop `ProductionMenuItem`'s per-row background art** (`Banner_Slots`). Guaranteed −1, but
   reverses part of the UI reskin (Report 038) done at explicit human request — rows go back to
   plain icon+text with no decorative slot background.
5. **Move `ProductionMenu`'s banner into the scrollable `Content`** (so its text merges with row
   text, both then sharing `UNITY_UI_CLIP_RECT`). Guaranteed −1, but the "Production Menu" header
   would scroll away with the list instead of staying pinned — a real UX regression, ranked lowest
   since it's a functional loss, not just a decorative one.

Ruled out: switching render pipelines/writing a custom SRP (URP already provides the relevant
technique — SRP Batcher — and GI-12 itself names "batching and GPU instancing," not a pipeline
rewrite, as the expected approach; a custom pipeline would be large, high-risk, disproportionate
effort for a single-digit remaining gap).

**Last report:** 039 (`Damage/death particle effects`), 2026-07-23 — backlog item 19, extended
per human request to also cover non-fatal damage (not just death/destruction). `GameEntityBase`
now subscribes to `Health.Damaged` alongside its existing `Health.Died` subscription. Two
deliberately different implementation shapes: a permanent child `ParticleSystem` (`damageEffect`,
just `.Play()`'d, safe across this pooled instance's whole lifetime) for non-fatal hits, vs. a
prefab-asset reference (`deathEffectPrefab`) freshly `Instantiate()`d at death — required because
`PrefabPool<T>.Release` deactivates the entire entity GameObject the instant death fires, which
would cut off a child particle mid-burst. Both use real 8-frame Tiny Swords `Particle FX`
spritesheets (`Dust_01`/`Explosion_01`) via the actual Unity Particle System's Texture Sheet
Animation module, not a static tinted dot. New `Prefabs/Effects/{DamageEffect,DeathEffect}.prefab`
+ `Art/Materials/M_Particle{Damage,DeathExplosion}.mat`. 232/232 EditMode tests passing, 0 compile
errors. See ARCHITECTURE.md decisions log #76.

**Report 038 (`UI pack-art reskin`), 2026-07-23** — human-requested rework of every plain
flat-color UI `Image` (Main Menu family, Production Menu, Information Panel) into Tiny Swords "UI
Elements" pack art: `SpecialPaper.png` for Main-Menu-family panel backgrounds, `BigBlueButton_Regular.png`
for their buttons, `RegularPaper.png` for the two gameplay HUD panels, `Banner.png` for both panels'
header banners, `Banner_Slots.png` for Production Menu rows, `BigRedButton_Regular.png` for Remove
Building. New `SpriteAtlas_UI.spriteatlas` packs the source folders. Sprite choices were made by
actually viewing the art (not guessing) and trusting the pack's own shipped 9-slice border data;
runtime-bound content icons were left untouched. Settings screen's Dropdown/Toggle internals are
intentionally left for the (long, human-requested) hand-wiring checklist rather than scripted.
232/232 EditMode tests passing, 0 compile errors. See ARCHITECTURE.md decisions log #75.

**Since Report 037 (no report filed, human-directed):** Fixed a real bug — a soldier could be
handed itself as an attack target (right-clicking a cell occupied by one of the currently-selected
soldiers) and would tick damage into itself indefinitely, since the project has no faction/team
system to otherwise rule that out. `SelectionController.HandleRightClick` now skips (no attack, no
move) any selected soldier whose own `hitTarget` is itself; other selected soldiers in the same
click are unaffected. Human is hand-testing directly, no dedicated tests added this pass. See
ARCHITECTURE.md decisions log #74.

**Report 037 (`Selection outline animation sync`), 2026-07-23** — human-reported: the
selection outline stayed frozen on its spawn-time sprite while animated soldiers' real sprite
changed every frame, producing a visible pose mismatch when a selected unit moved/attacked.
`GameEntityBase` gained a `LateUpdate` syncing `outlineRenderer.sprite = spriteRenderer.sprite`
whenever the outline is enabled — no duplicate Animator on the outline needed, and (confirmed with
the human before implementing) no draw-call impact: the outline was already its own draw call via
its dedicated material, this only changes which sprite/frame that existing draw call shows. Not
batchmode-verified this turn (Unity Editor open). See ARCHITECTURE.md decisions log #73.

**Report 036 (`Archer attack-cancel fix`), 2026-07-23** — human-reported: moving the
Archer while its attack animation was playing let the animation finish and fire the projectile
anyway, since cancelling the C# action coroutine never touched the independently-running Animator
state machine. `SoldierBase.CancelAction` now (a) always clears the pending ranged-release target
so a late `ReleaseAttack()` no-ops, and (b) if a release was genuinely pending, forces the Animator
straight back to Idle via a `Start()`-captured Idle state hash (`animator.Play(_idleStateHash, 0,
0f)`) instead of letting the attack animation play out. No controller/asset edits needed. Not
batchmode-verified this turn (Unity Editor was open) — human is hand-testing directly. See
ARCHITECTURE.md decisions log #72.

**Since Report 035 (no report filed, human-directed):** `GridView`/`IslandTerrainView`'s
`OnValidate` no longer rebuild synchronously — deferred one editor tick via
`EditorApplication.delayCall`, eliminating the "SendMessage cannot be called during Awake,
CheckConsistency, or OnValidate" console warnings both were producing (cosmetic only; the
tile/mesh writes always succeeded). Human confirmed no warnings after manual testing. See
ARCHITECTURE.md decisions log #71.

**Report 035 (`Selection/placement race fix, corrected`), 2026-07-23** — the human found
Report 034's placement/selection fix (decision #69(c)) didn't actually work: `PlacementController`/
`SelectionController`'s `Update()`s have no guaranteed execution order, so the `IsPlacing` guard
only helped in whichever frames Placement happened to run second — cancelling still moved the
selected unit, and committing still (re)selected in a confusing order. The human correctly
diagnosed the race themselves and offered two ways forward; went with their simpler one:
`SelectionController` no longer references `PlacementController` at all — it now independently
subscribes to the same `BuildingCatalogEntryEventChannel` `PlacementController` already reacts to,
and clears the current selection the instant a placement is requested. Removing the stale
selection means the click-ordering race can no longer produce a wrong result for either click,
regardless of which controller's `Update()` runs first — no shared reference, no ordering
guarantee, no `ProjectSettings/` change needed. A newly-placed building becoming the selection
afterward is expected (human-confirmed acceptable). 231/231 EditMode tests passing.

**Report 034 (`Post-hand-test polish/bugfix pass`), 2026-07-23** — 4 human-flagged items
after playtesting Report 033's animations. (1) Units now flip horizontally to face movement/
attack direction — `GameEntityBase.SetFlippedHorizontally` uses `SpriteRenderer.flipX` on both
the sprite and its selection outline (countered the human's own suggested `transform.localScale`
approaches, per their invitation, since `flipX` has zero collider/hierarchy blast radius). (2) The
Archer's arrow now launches from a `ReleaseAttack()` call fired by an Animation Event on
`Archer_Shoot_Blue.anim` (via a new `SoldierAnimationEvents` relay on `Visuals`) instead of the
instant the attack tick starts — melee unaffected. (3) Fixed a real bug: cancelling/committing a
building placement with right/left-click was *also* moving/attacking with or deselecting the
current selection underneath the ghost, since `PlacementController`/`SelectionController` each
read the same click independently — `SelectionController` now skips its own `Update()` entirely
while a `PlacementController` reports `IsPlacing`. (4) Fixed camera zoom: Windows' Input System
reports ±120 per physical scroll notch (not ~±1), so `zoomSpeed` was being multiplied ~120× too
much, letting one notch blow through the whole min/max range — a new `NotchesFromRawScrollDelta`
converts before `Zoom` is called. 228/228 EditMode tests passing.

**Report 033 (`Unit animations`), 2026-07-23** — backlog item 18. `SoldierBase` gained an
optional `Animator` field (mirrors `GameEntityBase`'s direct `spriteRenderer`/`outlineRenderer`
handles) driving `IsMoving` (bool, wraps `FollowPath`, reset in `CancelAction` so an interrupted
move never sticks) and `Attack` (trigger, fired once per `PerformAttack`). The Tiny Swords
Warrior/Archer/Pawn Animator Controllers shipped as unwired scaffolds (every clip present, zero
parameters/transitions) — a throwaway script built the actual Idle/Run/Attack state machine on
all 3 via `UnityEditor.Animations.AnimatorController`'s API, then added the `Animator` component
to each soldier prefab's `Visuals` child specifically (confirmed via the clips' own empty-path
sprite binding — root placement would have silently done nothing). Soldier 3 keeps its Pawn art
per human confirmation (its `Attack` trigger maps to `Pawn_Interact Knife_Blue`, a generic
tool-use clip standing in for a combat swing). 218/218 EditMode tests passing (no new tests —
the added logic is pure null-guarded side effects, already implicitly covered by existing
MoveTo/Attack tests that pass no `Animator`).

**Report 032 (`Procedural island tilemap generation`), 2026-07-22** — human follow-up to
Report 031: the human hand-painted the grass/cliff island (leaving out Water Foam) using the Tile
Palette walkthrough from Report 031, then asked for that exact result to regenerate procedurally
for any grid size. New `IslandTileSet` SO (12 named tile references: 9 for the grass 9-slice, 3
for the cliff row) + pure `IslandTilemapLayout` (computes grass/cliff placements for any
columns/rows) + `IslandTerrainView` (`[ExecuteAlways]`, mirrors `GridView`'s live-preview pattern
exactly). The 12 tile references were extracted automatically from the human's own hand-painted
Tilemaps via a throwaway script that verified the extraction reproduced every originally-painted
cell exactly *before* touching anything live — confirmed clean, zero mismatches. 218/218 EditMode
tests passing.

**Report 031 (`Camera bounds + terrain follow-up`), 2026-07-22** — human follow-up to Report
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

*Done (Reports 012–039):* ~~Units~~, ~~Placement~~, ~~UI.Production~~, ~~Selection~~, ~~UI.Info~~,
~~Gameplay scene assembly~~, ~~Draw-call/batching architecture~~, ~~Camera controls~~,
~~Placement/Grid architecture fixes~~, ~~Building events rearchitecture~~, ~~Selection polish~~,
~~Movement timing fix~~, ~~Info Panel producible-units layout fix~~, ~~UI visual polish~~,
~~Ranged combat & combat overhaul~~, ~~Combat/UI bugfix pass~~, ~~Grid line rendering~~,
~~Environment/terrain visuals~~, ~~Camera bounds + terrain follow-up~~,
~~Procedural island tilemap generation~~, ~~Unit animations~~,
~~Post-hand-test polish/bugfix pass~~, ~~Selection/placement race fix, corrected~~,
~~Archer attack-cancel fix~~, ~~Selection outline animation sync~~, ~~UI pack-art reskin~~,
~~Damage/death particle effects~~.

*Backlog* — catalogued 2026-07-22 from the human's own post-hand-test notes after confirming
Report 017 "purely mechanically works." Grouped by which module(s) each touches, not by the
human's original presentation order (they explicitly said regrouping was fine). Suggested
order below is dependency-aware, not a hard requirement — pick freely. **Empty** as of Report 039
— item 19 (the last remaining entry) is done.

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
