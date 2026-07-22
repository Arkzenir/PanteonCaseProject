# 032 — Procedural island tilemap generation
2026-07-22

## 1. Summary

Follow-up to Report 031: after hand-painting the grass/cliff island using the Tile Palette
walkthrough (leaving out Water Foam), the human asked for that exact result to regenerate
procedurally for any grid size — e.g. resizing the grid to 16×16 should reproduce the same
corner/edge/center grass layout and bottom-row cliff treatment automatically.

- **`IslandTileSet`** (new ScriptableObject) — 12 named `TileBase` references: 9 for the grass
  layer's "9-slice" (4 corners, 4 edges, 1 repeating center), 3 for the cliff row (left corner,
  repeating middle, right corner).
- **`IslandTilemapLayout`** (new pure static class) — `BuildGrass(columns, rows, tileSet)` places
  corner tiles at the 4 corners, edge tiles along each side, and the center tile everywhere else;
  `BuildCliff(columns, tileSet)` places one row at `y = -1` (directly beneath the grass's bottom
  row — "facing the player," per the human's own spec), corner tiles at each end and the middle
  tile between them.
- **`IslandTerrainView`** (new `[ExecuteAlways]` MonoBehaviour) — rebuilds both Tilemaps whenever
  `GridDefinition.Columns`/`Rows` (or the `IslandTileSet` itself) change, using the exact same
  Awake/OnValidate/Edit-Mode-`Update`-signature-check pattern `GridView` already established in
  Report 029 — resizing the grid in the Inspector regenerates the island live, without Play Mode.

**The 12 tile references were not manually re-specified** — a throwaway script read them directly
off the human's already-painted `Island_Grass`/`Island_Cliff` Tilemaps (corner cells, edge
midpoints, the center cell, and the cliff row's ends/middle), then — before creating any asset or
wiring anything live — **verified the extracted set reproduces every single originally-painted
cell exactly**, plus confirmed neither Tilemap had any painted cells outside the expected
rectangle/row. Zero mismatches. Only after that clean verification did the script create the
`IslandTileSet` asset and add `IslandTerrainView`, whose own `Awake` then regenerated both
Tilemaps from the now-verified data — reproducing the human's hand-painted result exactly, just
through the new procedural path instead of the original manual one.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Environment/IslandTileSet.cs`](../../Assets/_Project/Scripts/Runtime/Environment/IslandTileSet.cs) (new) — SO with the 12 named `TileBase` fields/getters.
- [`Assets/_Project/Scripts/Runtime/Environment/IslandTilemapLayout.cs`](../../Assets/_Project/Scripts/Runtime/Environment/IslandTilemapLayout.cs) (new) — pure `BuildGrass`/`BuildCliff`.
- [`Assets/_Project/Scripts/Runtime/Environment/IslandTerrainView.cs`](../../Assets/_Project/Scripts/Runtime/Environment/IslandTerrainView.cs) (new) — `[ExecuteAlways]` view, mirrors `GridView`'s rebuild-trigger pattern.
- [`Assets/_Project/Scripts/Tests/EditMode/Environment/IslandTilemapLayoutTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Environment/IslandTilemapLayoutTests.cs) (new, +9) — grass corner/edge/center placement, a different grid size (16×16) reproducing the same corner logic at the new extents, cliff corner/middle placement and row-coordinate checks.
- [`Assets/_Project/ScriptableObjects/IslandTileSet_Default.asset`](../../Assets/_Project/ScriptableObjects/IslandTileSet_Default.asset) (new) — the extracted tile references (all 12 populated, verified distinct).
- [`Assets/_Project/Scenes/Gameplay.unity`](../../Assets/_Project/Scenes/Gameplay.unity) — `IslandTerrainView` added to the `Terrain` GameObject, wired to `GridDef_Default.asset`, `IslandTileSet_Default.asset`, and the existing `Island_Grass`/`Island_Cliff` Tilemaps.

**Throwaway editor script** (`Scripts/Editor/Setup/Temp/IslandTileSetExtractionSetup.cs`, per
CLAUDE.md's editor script policy): read the 12 reference tiles directly off the live Tilemaps via
`Tilemap.GetTile`, ran a full verification pass (every `IslandTilemapLayout`-computed cell for the
current 20×12 grid compared against the actual painted tile, plus a `cellBounds` check on both
Tilemaps) — logged **zero** mismatches — then created the asset and wired `IslandTerrainView`.
Verified after the fact by reading the regenerated scene/asset files back: `IslandTileSet_Default.asset`
has all 12 fields populated with distinct GUIDs (no accidental duplicates/nulls); the scene's
`Terrain` GameObject gained the `IslandTerrainView` component with all 4 references (`gridDefinition`,
`tileSet`, `grassTilemap`, `cliffTilemap`) correctly pointing at the expected assets/components.
Deleted immediately after verification.

## 3. Test results

Batchmode EditMode run: **218/218 passing, 0 compile errors** (209 from Report 031 + 9
`IslandTilemapLayoutTests`).

**Hand-test recommended** (to confirm the regenerated result still looks identical to your
original painting, and that resizing actually works as expected):
- Open `Gameplay.unity` — the island should look exactly as you left it (this was already
  verified programmatically, but worth a glance).
- Temporarily change `GridDef_Default.asset`'s `Columns`/`Rows` (e.g. to 16×16) in the Inspector
  and confirm the island regenerates live in the Scene view without entering Play Mode, keeping
  the same corner/edge/cliff treatment at the new size. Change it back to 20×12 (or whatever you
  want as the shipping size) afterward.

## 4. Editor hookup checklist

None — extraction, asset creation, and scene wiring were already applied and verified via the
throwaway script above.

If you want to **swap the art** (e.g. a different `Tilemap_color` variant) later: create a new
`IslandTileSet` asset (`Assets → Create → CaseGame → Environment → Island Tile Set`), assign the
12 tile references by hand from the new palette, and point `IslandTerrainView`'s `Tile Set` field
at it.

## 5. Deviations

None from CONVENTIONS.md. This report is unusual only in that its editor-hookup work fully
succeeded via a throwaway script rather than needing a manual checklist — that was possible
specifically because the source of truth (the human's hand-painted Tilemaps) already existed and
could be read + verified programmatically, unlike Report 031's original painting task, which
needed a human's visual judgment to create that source of truth in the first place.
