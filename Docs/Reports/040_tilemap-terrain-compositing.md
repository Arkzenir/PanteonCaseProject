2026-07-23 — Tilemap terrain → single baked quad (Optimisation branch)

## 1. Summary

Human A/B-tested disabling the `Terrain` GameObject (its 3 Tilemaps: `Water`, `Island_Cliff`,
`Island_Grass`) entirely in favor of a placeholder rectangle sprite and measured SetPass/draw
calls roughly **halving** in active combat. Confirmed root cause before implementing (decisions
log #78): all 3 Tilemaps already share the identical material every entity `SpriteRenderer` uses
(URP's built-in `Sprite-Lit-Default`), but `TilemapRenderer`'s rendering path can't join the SRP
Batcher regardless of material match, and additionally splits into internal 32×32-cell chunks —
this grid's painted area (80×72 world units: 20×12 cells + 30-unit `TerrainMargin` on every side)
spans several, each a further dedicated, non-batchable draw/SetPass.

Implemented a runtime terrain compositor that keeps the tileset system generating the island
procedurally for any grid size exactly as before (`IslandTerrainView`/`IslandTilemapLayout`
untouched), but at `Gameplay.unity`'s load, bakes all 3 Tilemap layers into one texture on a
single `SpriteRenderer` quad (`BakedTerrain`, sorting order -2000, same as the old `Water`
Tilemap — beneath everything) via a dedicated orthographic bake camera rendering to a
`RenderTexture` once, then hides the source Tilemaps. Full tile-art fidelity is kept (a bake, not
a downgrade to a flat color) — this trades the ability to alter terrain at runtime, which is
never needed (the grid is fixed once a gameplay scene is loaded, human's own words), for
collapsing the guaranteed-multi-SetPass Tilemap render path down to one fully SRP-batchable draw.

The bake's world-space framing reuses the exact same "grid bounds + margin" formula
`GameplayBootstrap` already used for the camera's pan/zoom clamp — extracted into a new pure
`TerrainBounds.Compute` (`CaseGame.Grid`) so both can never drift out of sync, same reasoning the
original inline calculation's own comment already called out. Bake resolution (`TerrainBakeResolution`,
`CaseGame.Environment`) preserves the tileset's native 64px/world-unit density up to a 4096px
ceiling, scaling down uniformly (preserving aspect, no stretch) only if a larger grid's painted
area would exceed it — bounded VRAM regardless of grid size.

## 2. Changes

- `Assets/_Project/Scripts/Runtime/Grid/TerrainBounds.cs` (new) — pure `Compute(grid, margin)`,
  extracted from `GameplayBootstrap`'s previous inline calculation; now the single source of
  truth for "grid bounds expanded by `TerrainMargin`," used by both the camera bounds clamp and
  the new terrain bake.
- `Assets/_Project/Scripts/Runtime/Environment/TerrainBakeResolution.cs` (new) — pure sizing math:
  given world width/height, native pixels-per-unit, and a max texture dimension, returns the
  actual bake pixel dimensions + effective pixels-per-unit, always matching the world bounds'
  aspect ratio exactly.
- `Assets/_Project/Scripts/Runtime/Environment/TerrainCompositor.cs` (new) — the humble
  MonoBehaviour orchestrator: frames a `[SerializeField] Camera bakeCamera` on the computed
  bounds, renders it once to a `RenderTexture`, reads it back into a `Texture2D`, builds a
  `Sprite`, assigns it to `[SerializeField] SpriteRenderer outputRenderer`, then disables each
  `[SerializeField] Renderer[] sourceRenderers` (the 3 Tilemaps' `TilemapRenderer`s). Public
  `Bake(GridModel, float terrainMargin)`, called once from `GameplayBootstrap.Start()`.
- `Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs` — camera-bounds calculation now
  calls `TerrainBounds.Compute` instead of its own inline math; new
  `[SerializeField] TerrainCompositor terrainCompositor` field, `Bake`d in `Start()` right after
  the camera bounds are set (both need the same `GridModel`/margin, already in scope).
- `Assets/_Project/Scenes/Gameplay.unity` — new `TerrainBakeCamera` (orthographic, disabled,
  culled to a dedicated isolated layer, `SolidColor` clear with 0 alpha) and `BakedTerrain`
  (`SpriteRenderer`, sorting order -2000) as children of `Terrain`; a new `TerrainCompositor`
  component added directly on the `Terrain` GameObject (alongside its existing `Grid`/
  `IslandTerrainView`); `Water`/`Island_Cliff`/`Island_Grass` GameObjects moved onto that same
  isolated layer (raw index 8, left **unnamed** — naming it in `ProjectSettings/TagManager.asset`
  needs explicit approval per CLAUDE.md, see Deviations) so the bake camera's culling mask
  captures only the terrain, not `GridLines` or anything else; `GameplayBootstrap`'s new
  `terrainCompositor` field wired. All done via a throwaway script
  (`TerrainCompositorSetup.cs`), verified by reading every new field/component back from the
  regenerated scene file before deleting it.
- `Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef` — added a reference to
  `Unity.RenderPipelines.Universal.Runtime` (needed by the throwaway script to add
  `UniversalAdditionalCameraData` to the new bake camera; no runtime script needs this
  reference).
- `Assets/_Project/Scripts/Tests/EditMode/Grid/TerrainBoundsTests.cs`,
  `Assets/_Project/Scripts/Tests/EditMode/Environment/TerrainBakeResolutionTests.cs` (new) — cover
  both pure helpers directly.
- `Docs/Agent/ARCHITECTURE.md` — decisions log #78 (root-cause confirmation + chosen design).
- `Docs/Agent/CURRENT_STATUS.md` — `Optimisation` branch section updated (branch reset noted,
  this feature's progress recorded).

## 3. Test results

**238/238 EditMode tests passing, 0 compile errors** (232 before this pass, +6 new: 3
`TerrainBoundsTests`, 3 `TerrainBakeResolutionTests`).

**Hand-test (not automatable — rendering output/Profiler numbers):**
- Enter Play Mode on `Gameplay.unity`; confirm the island/water terrain renders visually
  identically to before (same tile art, same extent) with no visible flash/mismatch at scene
  start (the bake is a single-frame hitch during `Start()`, before the Production Menu is
  interactive).
- Open the Profiler's Rendering module (or Frame Debugger) and confirm SetPass/draw calls have
  dropped versus the pre-bake measurement, both idle and in active combat.
- Confirm camera pan/zoom bounds still clamp correctly at the water's edge (unchanged logic, now
  routed through `TerrainBounds.Compute` — should be pixel-identical to before).
- Confirm the grid's cell lines (`GridLines`) still render on top of the baked terrain, unaffected
  (different GameObject, default layer, never touched by this feature).

## 4. Editor hookup checklist

None — the bake camera, output quad, `TerrainCompositor` component, layer reassignment, and
`GameplayBootstrap` wiring are all already in `Gameplay.unity` (verified by reading the
regenerated scene file back before deleting the throwaway script).

## 5. Deviations

- The isolated bake-camera culling layer (raw index 8) is **unnamed** — giving it a friendly name
  (e.g. "TerrainBake") requires editing `ProjectSettings/TagManager.asset`, which CLAUDE.md gates
  behind explicit human approval ("Only with explicit approval: `ProjectSettings/`"). Functionally
  identical either way (it's a display label only); say the word and it's a one-line change.
- No decorative rock ring exists in the current scene (confirmed via direct scene-file search
  before starting) — nothing for this feature to bake in beyond the 3 Tilemaps, matching what the
  human already expected.

## 6. Follow-up note, 2026-07-23 (post-report, human-directed)

Human hand-tested and found (a) an actual bug — the bake camera's Z position was never moved off
the Tilemaps' own Z=0 plane, so the first shipped version baked nothing; fixed same-day (decisions
log #79) — and (b), after the fix, confirmed the real win: **under 10 SetPass/draw calls at idle**,
comfortably inside GI-12's <20 budget. The fix also surfaced one expected, non-blocking side
effect: a one-time ~25-SetPass spike on the very first loaded frame (the bake camera's own render
of the still-live, not-yet-hidden Tilemaps landing in that one frame's Profiler tally, before
`Bake()` disables them a few lines later).

Discussed 3 ways to suppress/smooth that load-frame spike (deferring the bake a frame, spreading
the bake across multiple frames — one Tilemap layer per frame — or moving the bake to edit-time
entirely, pre-baking a checked-in texture asset with no runtime `Camera.Render()` cost at all).
Human's explicit direction: **do not implement any of them** for this project. Scoping note for
the record — in a real production/live-service development scenario (as opposed to this
fixed-scope evaluation case), this exact one-time load-frame cost is exactly what a loading
screen/transition sequence is *for*: gate the reveal of gameplay (camera fade-in, enabling
interaction) on the terrain bake's completion, so the spike happens entirely behind the loading
UI where no draw-call budget or player-visible frame is at stake, rather than needing to be
suppressed, smoothed, or engineered away at the render level. This project has no loading
screen (out of scope, brief doesn't call for one, `Boot`→`MainMenu`→`Gameplay` are direct scene
loads) — noted here so a future pass (or the final report) doesn't need to rediscover this
reasoning from scratch. No code changed by this note.
