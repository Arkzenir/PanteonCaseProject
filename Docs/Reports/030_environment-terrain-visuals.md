# 030 — Environment/terrain visuals
2026-07-22

## 1. Summary

Backlog item 17, scoped down after one clarifying question: **should border terrain block
movement/placement like an occupied cell, or be purely decorative this pass?** You confirmed
**purely decorative** — no changes to `GridModel`, `AStarPathfinder`, `PlacementController`, or
`UnitProductionController`'s occupancy logic.

Before implementing, I inspected the actual Tiny Swords terrain art (`Tilemap_color1–5.png`) and
found it isn't a flat, repeatable ground tileset — it's a 45-piece autotile set representing a
"floating grass island with cliff sides" shape, where each 64×64 piece only reads correctly next
to its specific neighbors. I can't render the Editor/Play Mode to visually verify a hand-assembled
or scripted composition of that sheet, so committing to it risked shipping a visibly broken result
with no way to catch it. Given that constraint, I deliberately used only the pack's assets that
are safe to place without visual-assembly risk:

- **Water backdrop**: the pack's flat solid-color "Water Background color" sprite, painted as a
  `Tilemap` **frame around the grid's bounds** (20-world-unit margin) — the grid's own interior
  cells are left unpainted, so the existing camera background still shows through as "land."
  Sorting order -2000 (below the grid lines' -1000, decision #64).
- **Border decoration**: a ring of the pack's plain static `Rock1–4.png` sprites (not the
  animated 8-frame Bush sprites, for the same "can't visually verify a guessed frame" reason)
  placed just outside the grid's edge, via a new pure/testable `BorderDecorationLayout`
  (`CaseGame.Environment`, new module).
- **Atlas**: `Tileset` and `Decorations/Rocks` folders added as new packable entries on
  `SpriteAtlas_Gameplay`, per the backlog's explicit instruction (mirrors the existing
  folder-level building/unit packables, decision #18).

Both pieces are **static, one-time-authored scene content** baked directly into `Gameplay.unity`
by a throwaway editor script — there's no new runtime `MonoBehaviour`; unlike `GridView`'s grid
lines (Report 029), this terrain doesn't need to react to `GridDefinition` changes at runtime or
in the Editor, since it's simply level dressing.

**I could not visually verify the actual on-screen composition** (scale, spacing, whether the
water/rock margin looks right at typical camera zoom) — this needs your own eyes in the Editor;
see the hand-test checklist below.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Environment/BorderDecorationLayout.cs`](../../Assets/_Project/Scripts/Runtime/Environment/BorderDecorationLayout.cs) (new) — pure static `BuildRing(GridModel, margin, spacing, variantCount)`: walks the perimeter of the grid's expanded bounds, one placement per `spacing` world units, cycling through `variantCount` sprite variants.
- [`Assets/_Project/Scripts/Tests/EditMode/Environment/BorderDecorationLayoutTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Environment/BorderDecorationLayoutTests.cs) (new, +7) — placement count, corner positions on each edge, variant cycling, empty-result guards for zero/negative spacing or variant count.
- [`Assets/_Project/Art/Tiles/Tile_Water.asset`](../../Assets/_Project/Art/Tiles/Tile_Water.asset) (new) — a `UnityEngine.Tilemaps.Tile` wrapping the pack's "Water Background color" sprite.
- [`Assets/_Project/Art/Textures/SpriteAtlas_Gameplay.spriteatlas`](../../Assets/_Project/Art/Textures/SpriteAtlas_Gameplay.spriteatlas) — added `Tiny Swords/Terrain/Tileset` and `Tiny Swords/Terrain/Decorations/Rocks` as new packable folders.
- [`Assets/_Project/Scenes/Gameplay.unity`](../../Assets/_Project/Scenes/Gameplay.unity) — under `--- ENVIRONMENT ---`: new `Terrain` GameObject (`Grid` component, cell size 0.5 matching `GridDef_Default.asset`) with a child `Water` `Tilemap`/`TilemapRenderer` (8,960 tiles forming a frame around the grid, sorting order -2000); new `BorderDecorations` GameObject with 35 `Rock_N` `SpriteRenderer` children (cycling `Rock1–4.png`), placed via `BorderDecorationLayout.BuildRing`.
- [`Docs/Agent/ARCHITECTURE.md`](../Agent/ARCHITECTURE.md) — new `Environment` module map row; new decisions log entry #65.
- [`Docs/Agent/CONVENTIONS.md`](../Agent/CONVENTIONS.md) — added `Environment/` to the additional-Scripts-folders override row; new row for `Art/Tiles/`.
- [`Docs/Agent/CURRENT_STATUS.md`](../Agent/CURRENT_STATUS.md) — "Last report" updated to 030, Done line extended, backlog item 17 removed.

**Throwaway editor script** (`Scripts/Editor/Setup/Temp/EnvironmentTerrainSetup.cs`, per CLAUDE.md's
editor script policy — comfortably past the ~10-step bar: new folder + asset creation, atlas
edit, new GameObjects/components, bulk tile painting, procedural decoration placement): created
`Tile_Water.asset`, added the two atlas packable folders, built the `Terrain`/`Water` Tilemap
(4 non-overlapping `SetTilesBlock` calls forming the frame) and `BorderDecorations` ring. Verified
by reading the regenerated files back — confirmed: the atlas's `packables` list gained both new
folder GUIDs; `Tile_Water.asset`'s `m_Sprite` points at the correct source sprite; the scene's
`Grid` component has `m_CellSize: {0.5, 0.5, 1}` matching `GridDef_Default.asset`; the `Water`
`TilemapRenderer` has `m_SortingOrder: -2000`; exactly 8,960 tiles are painted, starting at cell
(-40,-40) as expected from the 40-cell margin; exactly 35 `Rock_N` objects exist, matching
`BorderDecorationLayout`'s own perimeter/spacing math for the default 20×12 grid. Deleted
immediately after verification.

## 3. Test results

Batchmode EditMode run: **201/201 passing, 0 compile errors** (194 from Report 029 + 7
`BorderDecorationLayoutTests`).

**Hand-test required** (visual composition can't be verified from this environment):
- Open `Gameplay.unity` in the Editor and look at the Scene/Game view — confirm the water frame
  and rock ring look reasonable at the board's scale, and that the water margin is generous
  enough at max zoom-out (camera's `maxOrthographicSize` is 16).
- Confirm the water renders behind the grid lines/buildings/units, and the rocks don't visually
  overlap anything placed near the board's edge.
- If the water margin looks too small/large, or the rock spacing/scale looks off, these are
  tuning values in `EnvironmentTerrainSetup`'s constants (`MarginCells`, `DecorationMargin`,
  `DecorationSpacing`) — since that script is now deleted (per policy), adjusting them means
  either hand-editing the scene directly or asking for a quick follow-up pass with new constants.

## 4. Editor hookup checklist

None — the scene wiring was already applied and verified via the throwaway script above; nothing
further is required for the terrain to render.

If you want to **retune the composition** by hand:
1. Select the `Terrain/Water` GameObject in `Gameplay.unity` to adjust its `Tilemap`/
   `TilemapRenderer` (e.g. `Sorting Order`) directly.
2. Select individual `BorderDecorations/Rock_N` children to reposition, delete, or restyle
   individual props.

## 5. Deviations

- **Scope-narrowed per your explicit answer**: terrain is decorative only this pass; no
  occupancy/pathfinding changes. Recorded as decision #65's stated scope.
- **Did not use the pack's autotile grass/cliff tileset or animated Bush sprites** — both would
  need visual verification (autotile adjacency correctness; picking a non-arbitrary-looking
  animation frame) that isn't possible from this environment. Used only assets safe to place
  without that risk (flat solid-color water tile, static rock sprites). Flagged, not silently
  substituted — full reasoning in decisions log #65.
- Everything else follows CONVENTIONS.md; no other deviations.
