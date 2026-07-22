# 031 — Camera bounds + terrain follow-up
2026-07-22

## 1. Summary

Human follow-up to Report 030, two requests:

1. **Extend the water backdrop, and clamp camera pan so the background color/skybox is never
   visible.** `GridDefinition` gained a new `TerrainMargin` field (30 world units, up from Report
   030's hardcoded 20) — the single source of truth for how far the water extends beyond the
   grid, read by *both* the baked `Terrain/Water` Tilemap and the camera's new bounds, so the two
   can never drift out of sync. `CameraController` gained `SetBounds(min, max)` and a pure,
   testable `ClampToBounds`, applied after every `Pan`/`Zoom` — the camera's viewport (computed
   from live `orthographicSize`/`aspect`, not a fixed guess) can no longer extend past the water's
   own painted rectangle at any zoom level. `GameplayBootstrap` computes the bounds from
   `GridModel` + `TerrainMargin` and wires them in `Start()`.

2. **Use the actual `Tilemap_color1` grass/cliff tileset to build a proper island**, with the
   solid background color as the surrounding ocean (already built, Report 030). I looked into
   this and found the pack ships its own official usage documentation right in the project
   (`Assets/Tiny Swords/Terrain/Tileset/Unity Tile Guide/Unity Tile Guide_01–07.png`) — reading
   it **confirmed** Report 030's "Water Background color" choice was exactly the artist's intended
   "BG Color" layer, and clarified how the grass/cliff tileset is actually meant to be assembled
   (a layered system: BG Color → Water Foam → Flat/Elevated Ground → Shadow, not a single
   autotile pass). Painting an actual island correctly still requires *seeing* the result to
   judge scale, cliff-row placement, and edge treatment — exactly the kind of visual-judgment
   task I can't verify from this environment. Per your own offer, **§4 below is a precise,
   numbered Tile Palette walkthrough for you to do by hand**, pointing directly at the pack's own
   reference images so you're matching real art, not guessing from my description of it.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Grid/GridDefinition.cs`](../../Assets/_Project/Scripts/Runtime/Grid/GridDefinition.cs) — new `terrainMargin` field (default 30, clamped ≥0), `TerrainMargin` property.
- [`Assets/_Project/Scripts/Runtime/Grid/GridView.cs`](../../Assets/_Project/Scripts/Runtime/Grid/GridView.cs) — new `Definition` property (exposes the underlying `GridDefinition`, mirroring `BuildingBase`/`SoldierBase`'s own strongly-typed `Definition` pattern).
- [`Assets/_Project/Scripts/Runtime/CameraControl/CameraController.cs`](../../Assets/_Project/Scripts/Runtime/CameraControl/CameraController.cs) — new `SetBounds(Vector2, Vector2)`; new pure static `ClampToBounds`/`ClampAxis`; `Pan`/`Zoom` now call `ApplyBoundsClamp` after moving.
- [`Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs`](../../Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs) — new `cameraController` field; `Start()` computes bounds from `GridModel.CellToWorld` ± `TerrainMargin` and calls `SetBounds`.
- [`Assets/_Project/ScriptableObjects/GridDef_Default.asset`](../../Assets/_Project/ScriptableObjects/GridDef_Default.asset) — `terrainMargin: 30` added (direct low-risk YAML edit, matching Report 029's precedent for this same file).
- [`Assets/_Project/Scenes/Gameplay.unity`](../../Assets/_Project/Scenes/Gameplay.unity) — `GameplayBootstrap`'s new `cameraController` field wired to the existing `CameraController` component (direct low-risk YAML edit); `Terrain/Water` Tilemap repainted to the new 30-unit margin via throwaway script (idempotent re-paint, old area included).
- [`Assets/_Project/Scripts/Tests/EditMode/CameraControl/CameraControllerTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/CameraControl/CameraControllerTests.cs) (+7) — `ClampToBounds` unit tests (inside bounds, past max/min, viewport-wider-than-bounds centering), `SetBounds`/`Pan`/`Zoom` integration tests against a real `Camera`.
- [`Assets/_Project/Scripts/Tests/EditMode/Grid/GridDefinitionTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Grid/GridDefinitionTests.cs) (+1) — `TerrainMargin` clamp test.
- [`Docs/Agent/ARCHITECTURE.md`](../Agent/ARCHITECTURE.md) — Grid/CameraControl/Gameplay/Environment module map rows updated; new decisions log entry #66.
- [`Docs/Agent/CURRENT_STATUS.md`](../Agent/CURRENT_STATUS.md) — "Last report" updated to 031, Done line extended.

**Throwaway editor script** (`Scripts/Editor/Setup/Temp/ExtendWaterTerrainSetup.cs`, per CLAUDE.md's
editor script policy): repainted the `Terrain/Water` Tilemap using the new `TerrainMargin`.
Verified by reading the regenerated scene back — tile count went from 8,960 to 18,240 (matching
the new 60-cell/30-world-unit margin exactly: (140×60)×2 + (60×12)×2), first tile now at
(-60,-60). Deleted immediately after verification.

## 3. Test results

Batchmode EditMode run: **209/209 passing, 0 compile errors** (201 from Report 030 + 7
`CameraControllerTests` + 1 `GridDefinitionTests`).

**Hand-test required** (camera feel/visual correctness can't be verified from this environment):
- Pan/zoom around the board in Play Mode — confirm the background color/skybox never becomes
  visible at any zoom level or pan direction, and that the clamp doesn't feel jarring (e.g. a
  sudden full-stop is expected at the edge; if it feels too tight/loose, `TerrainMargin` on
  `GridDef_Default.asset` is the one number to retune).
- Zoom all the way out (`maxOrthographicSize` 16) at the corners — this is the case most likely
  to reveal any remaining gap if the margin were too small; 30 world units should comfortably
  cover it, but confirm by eye.

## 4. Manual Tile Palette walkthrough — building the island with `Tilemap_color1`

This part is intentionally **not automated** — assembling this tileset correctly requires judging
the result visually as you go, which I can't do from here. Everything below is precise steps, not
guesswork: the pack ships its own labeled reference images at
`Assets/Tiny Swords/Terrain/Tileset/Unity Tile Guide/Unity Tile Guide_01.png` through `_07.png` —
**open these alongside the Tile Palette while you paint**; they show the actual art for every
numbered piece, so you're matching by eye against the artist's own legend, not translating my
description of it.

**What the guide establishes** (from `_01`/`_02`/`_05`/`_06`):
- **BG Color** = the flat water color (already in use as `Tile_Water.asset`, Report 030 — confirmed correct).
- **Flat Ground** = ground at water level; **Elevated Ground** = a raised block with its own grass top (pieces numbered 1–16, same layout as Flat Ground) plus **Elevated Ground Cliff** (pieces 17–24, the rock-face pieces that go *underneath* the grass at whichever edge faces something lower).
- Piece **5** in either 16-piece grid is the plain, freely-repeating **center/interior** tile.
- Pieces **1/2/3** (top row), **4/6** (sides), **7/8/9** (bottom row) are the **9-slice border**: corners at 1/3/7/9, straight edges at 2 (top)/8 (bottom)/4 (left)/6 (right). Pieces 10–16 are extra pieces for 1-tile-wide strips/fully-isolated single tiles — only needed if your island's shape isn't a plain rectangle.
- **Shadow** and **Elevated Ground** sprites are 128×128 but placed on the 64×64 grid, deliberately overlapping — `_04` shows the exact offset (shadow shifted one full 64×64 cell **down** from the walkable area it sits beneath) that fakes the raised-block depth.

**Steps:**

1. **Set up the source sprites** (if not already sliced — check first): select
   `Assets/Tiny Swords/Terrain/Tileset/Tilemap_color1.png`, confirm **Sprite Mode: Multiple** in
   the Inspector (it already is — pre-sliced by the pack into 45 sub-sprites). Open the **Sprite
   Editor** once to see the slice grid overlaid on the actual image if you want to cross-reference
   positions directly against the numbered guide.

2. **Create a Tile Palette**: `Window → 2D → Tile Palette` → `Create New Palette` → name it
   e.g. `Palette_Island`, save under `Assets/_Project/Art/Tiles/`. Drag
   `Tilemap_color1.png` from the Project window into the open palette — Unity generates one
   `Tile` asset per sub-sprite automatically (they'll land wherever you point the save dialog;
   suggest a new `Assets/_Project/Art/Tiles/Island/` subfolder to keep them out of the loose
   `Art/Tiles/` root).

3. **Add the Tilemap layers** under the existing `Terrain` GameObject (sibling of `Water`, same
   `Grid` parent so cell size stays aligned at 0.5):
   - `Island_Grass` (Tilemap + TilemapRenderer) — sorting order **above** `Water`'s -2000 but
     comfortably below buildings/units (e.g. -100).
   - `Island_Cliff` (Tilemap + TilemapRenderer) — sorting order **just below** `Island_Grass`
     (e.g. -150), since the cliff renders *underneath* the grass edge per the Shadow-layering trick.

4. **Paint the grass top** on `Island_Grass`, covering the grid's own bounds (cells `(0,0)` to
   `(20,12)` to match `GridDef_Default.asset` exactly, or 1 cell smaller on each side if you want
   a visible grass margin before the drop-off — your call, easiest to judge by eye once the first
   pass is down):
   - Fill the interior with piece **5** (the repeating center tile) using the Tile Palette's
     rectangle/paint-bucket brush.
   - Trace the 4 edges with the corner/edge pieces (**1/2/3** top, **4**/**6** sides, **7/8/9**
     bottom) — this alone satisfies "borders being the border tiles."

5. **Paint the cliff row** on `Island_Cliff`, directly beneath the **bottom** edge of the grass
   (this is "cliffs facing downwards towards the player's perspective," since the bottom edge is
   the one facing the camera in this top-down layout) — shifted down by exactly one 64×64
   cell/one grid cell (0.5 world units) per the guide's `_04` instructions, so it visually reads
   as a rock face *under* the grass edge, not a separate strip floating below it. The tileset
   provides **two cliff-row variants** (pieces 17–20 and 21–24) for "cliff meets water" vs. "cliff
   meets another walkable tier" — since this island sits directly on water on every side, use
   whichever of the two visually matches "meets water" in the `_06` reference image (I can't tell
   you which fileID that is without seeing them rendered — it'll be obvious once both are in the
   palette next to the guide image).

6. **Optional polish** (skip for a first pass, easy to add later): **Water Foam** where the grass
   touches water (animated, same 128×128-on-64×64-grid placement as Shadow, per `_03`) — the guide
   suggests starting each placed Foam sprite's animation at a different frame so they don't pulse
   in lockstep.

7. **Save** — no code changes needed for the island itself; `BorderDecorationLayout`'s rock ring
   and `CameraController`'s bounds are already sized independently of this artwork, so painting
   the island doesn't require touching either.

Once it's in and you've seen how it reads, let me know if you want the rock ring
(`BorderDecorations`) repositioned or removed — with a proper painted cliff edge now in place, the
standalone rocks (previously placed to make an otherwise-bare water edge read as a boundary) may
end up redundant or need to sit further out in the water instead.

## 5. Editor hookup checklist

- Camera bounds wiring was already applied and verified (§2's throwaway script + direct scene
  edit) — nothing further required for that part.
- §4 above **is** the checklist for the island itself — no additional automated wiring, by design.

## 6. Deviations

None from CONVENTIONS.md. Section 4 is a deliberate exception to "implement it yourself" for
exactly the reason CLAUDE.md's editor-hookup-checklist policy exists: a one-off action that's
genuinely error-prone/needs visual judgment goes to the human with precise steps, not a script.
