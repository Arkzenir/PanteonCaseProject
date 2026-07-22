# 029 — Grid line rendering
2026-07-22

## 1. Summary

Backlog item 16: `GridView` previously drew its cell lines only via `OnDrawGizmos` — visible in
the Scene view during editing, but invisible in Play Mode and in a build. Replaced with an
actual runtime-rendered grid, built as a **single combined mesh** (`GridLineMeshBuilder`: one
thin vertex-colored quad per grid line, one `MeshRenderer` draw call for the entire board
regardless of size) rather than one `LineRenderer` per line — the default 20×12 board already has
34 lines, and a per-line `LineRenderer` approach would risk blowing GI-12's <20 SetPass-call
budget this project has been protecting since decisions #18/#49. A new minimal unlit
vertex-color shader (`GridLines.shader`/`M_GridLines.mat`) renders it, matching the project's
existing custom-shader style (`SpriteSelectionOutline`/`SpriteGrayscaleGhost`).

All four backlog requirements are met:
- **Toggleable** — `GridView.SetLinesVisible(bool)` (+ `LinesVisible` property) toggles the
  `MeshRenderer`. Plain method, no event channel (local render state, nothing else needs to react).
- **Data-driven visuals** — `GridDefinition` gained `LineColor`/`LineThickness`, same pattern as
  its existing `cellSize`/`columns`/`rows`/`originWorldPosition`.
- **Renders behind everything else** — `MeshRenderer.sortingOrder` defaults to `-1000` (a
  `GridView`-level field, since this is a rendering concern of this specific instance, not
  abstract grid data).
- **Live-updates in Edit Mode** — `GridView` is now `[ExecuteAlways]` and rebuilds the mesh on
  `Awake`/`OnValidate`, plus a cheap Edit-Mode-only `Update()` signature check that catches edits
  made directly to the separate `GridDefinition` asset (which fires *its own* `OnValidate`, not
  `GridView`'s) without needing new event-wiring between the SO and every scene object referencing it.

**Assumption (stated, not blocking):** the backlog text itself marked the UI toggle button
"optional... human's call at implementation time." This pass ships the toggle API only; no scene
button is wired, to avoid guessing a location (Settings screen? in-scene HUD?) the human hasn't
specified.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Grid/GridDefinition.cs`](../../Assets/_Project/Scripts/Runtime/Grid/GridDefinition.cs) — new `lineColor`/`lineThickness` fields (+ `OnValidate` clamp on thickness).
- [`Assets/_Project/Scripts/Runtime/Grid/GridLineMeshBuilder.cs`](../../Assets/_Project/Scripts/Runtime/Grid/GridLineMeshBuilder.cs) (new) — pure static: `BuildSegments` (line endpoints from a `GridModel`), `ComputeQuadVertices` (per-segment quad math), `BuildMesh` (combines both + baked vertex color into a single `Mesh`).
- [`Assets/_Project/Scripts/Runtime/Grid/GridView.cs`](../../Assets/_Project/Scripts/Runtime/Grid/GridView.cs) — rewritten: `[ExecuteAlways]`, `[RequireComponent(MeshFilter, MeshRenderer)]`, rebuilds the mesh on `Awake`/`OnValidate`/an Edit-Mode `Update` signature check; new `lineMaterial`/`sortingOrder`/`linesVisibleOnStart` fields; public `SetLinesVisible(bool)`/`LinesVisible`; old `gridLineColor` field and `OnDrawGizmos` removed.
- [`Assets/_Project/Art/Shaders/GridLines.shader`](../../Assets/_Project/Art/Shaders/GridLines.shader) (new) — unlit, vertex-color pass-through, URP HLSL (matches `SpriteSelectionOutline.shader`'s style).
- [`Assets/_Project/Art/Materials/M_GridLines.mat`](../../Assets/_Project/Art/Materials/M_GridLines.mat) (new) — uses the shader above, no extra properties needed (color comes from mesh vertex colors).
- [`Assets/_Project/ScriptableObjects/GridDef_Default.asset`](../../Assets/_Project/ScriptableObjects/GridDef_Default.asset) — `lineColor`/`lineThickness` added, migrated from the value that used to live on the scene's `GridView` component (`{1,1,1,0.35}`), so there's no visual change to the existing look.
- [`Assets/_Project/Scenes/Gameplay.unity`](../../Assets/_Project/Scenes/Gameplay.unity) — the `Grid` GameObject's `GridView` gained `MeshFilter`/`MeshRenderer` components (via throwaway script, see below) and its new `lineMaterial` field points at `M_GridLines.mat`.
- [`Assets/_Project/Scripts/Tests/EditMode/Grid/GridLineMeshBuilderTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Grid/GridLineMeshBuilderTests.cs) (new, +6) — segment count/endpoints, quad-vertex math (horizontal + vertical), mesh vertex/triangle counts, baked vertex color.
- [`Assets/_Project/Scripts/Tests/EditMode/Grid/GridDefinitionTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Grid/GridDefinitionTests.cs) (new, +2) — `lineThickness` clamp, `LineColor` default.
- [`Docs/Agent/ARCHITECTURE.md`](../Agent/ARCHITECTURE.md) — Grid module map entry updated; new decisions log entry #64.
- [`Docs/Agent/CURRENT_STATUS.md`](../Agent/CURRENT_STATUS.md) — "Last report" updated to 029, Done line extended, backlog item 16 removed.

**Throwaway editor script** (`Scripts/Editor/Setup/Temp/GridLineRenderingSetup.cs`, per CLAUDE.md's
editor script policy): created `M_GridLines.mat` and added the `MeshFilter`/`MeshRenderer`
`GridView`'s new `[RequireComponent]` demands onto the already-existing `Grid` GameObject in
`Gameplay.unity` (`RequireComponent` only auto-adds on a *fresh* `AddComponent`, not retroactively
onto a component that predates the attribute). Verified by reading the regenerated
`.mat`/`.unity` files back (confirmed the material's shader GUID matches `GridLines.shader.meta`,
the `Grid` GameObject now lists all 4 components, `lineMaterial` points at the new material, and —
since the scene was open when the script ran — the mesh was actually built and embedded in the
scene file: 136 vertices / 204 triangle indices, matching 34 segments × 4/6 for the default
20×12 grid). Deleted immediately after verification.

## 3. Test results

Batchmode EditMode run: **194/194 passing, 0 compile errors** (186 from Report 028 + 6
`GridLineMeshBuilderTests` + 2 `GridDefinitionTests`).

**Hand-test** (Play Mode / Editor, since visual rendering can't be verified by EditMode tests):
- Open `Gameplay.unity` — the grid lines should already be visible in the Scene view (built when
  the throwaway script's scene save ran) and should also now appear in the **Game view** and in
  Play Mode, which they never did before this feature.
- Confirm the grid renders **behind** buildings/units/ghosts, not on top of them.
- With the Editor in Edit Mode (not Play Mode), select `GridDef_Default.asset` and change
  `Line Color`/`Line Thickness`/`Columns`/`Rows` — the grid mesh in the Scene/Game view should
  update within a moment, without entering Play Mode.
- Call `gridView.SetLinesVisible(false)` (e.g. temporarily from any script, or via the Inspector's
  "Debug" mode to invoke it directly) and confirm the lines disappear/reappear.
- Optional: open the Stats window in Play Mode and confirm the grid contributes exactly one
  extra draw call (SetPass call), not one per line.

## 4. Editor hookup checklist

The scene wiring (`MeshFilter`/`MeshRenderer` + `lineMaterial` assignment) was already applied and
verified via the throwaway script above — nothing further is required for the grid to render.

If a designer later wants to **tune the look**:
1. Select `Assets/_Project/ScriptableObjects/GridDef_Default.asset`.
2. Adjust `Line Color` (alpha included) and `Line Thickness` (world units) directly in the
   Inspector — the grid updates live in the Scene view without entering Play Mode.

If a designer wants to **change where the grid sorts** or its starting visibility:
1. Select the `Grid` GameObject in `Gameplay.unity`.
2. On its `GridView` component, adjust `Sorting Order` (more negative = further behind) or
   `Lines Visible On Start`.

No UI toggle button is wired this pass (see Summary's stated assumption) — if wanted later, call
`gridView.SetLinesVisible(bool)` from any Button's `OnClick` (same direct-call pattern already
used for `InfoPanelController.RequestRemoveBuilding`), no new plumbing required.

## 5. Deviations

None from CONVENTIONS.md. `lineColor`/`lineThickness` living on `GridDefinition` and
`lineMaterial`/`sortingOrder`/visibility living on `GridView` mirrors this project's existing
Model(SO)/View(MonoBehaviour) split (decision #22) rather than being a new pattern.
