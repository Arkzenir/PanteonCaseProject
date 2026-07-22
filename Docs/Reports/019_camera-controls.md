**2026-07-22 — Camera controls**

## 1. Summary
Implemented `CameraController` — middle-mouse-drag pans the camera, scroll wheel zooms it. This
is human-requested quality-of-life (first item off the post-hand-test polish backlog in
`CURRENT_STATUS.md`), not a brief requirement; the brief is silent on camera navigation.

**Pan** reads `Mouse.current.delta` while the middle button is held, converts the screen-space
delta to a world-space delta scaled by the camera's current `orthographicSize`/screen height (so
a drag tracks the cursor 1:1 in world space regardless of zoom level), and moves the camera
opposite the drag direction — the standard "grab and drag the world" convention (drag right →
the world appears to move right → the camera moves left).

**Zoom** adjusts `orthographicSize` by the scroll wheel's Y delta × a designer-tunable
`zoomSpeed`, clamped to `[minOrthographicSize, maxOrthographicSize]` (defaults 4/16 — the current
scene sits at 10.5 per Report 017, so this comfortably brackets it either direction). Scroll up
zooms in (shrinks the size); this is the common convention.

Both are skipped while the pointer is over UI (`EventSystem.current.IsPointerOverGameObject()`)
— the same guard Report 017 added to `PlacementController`/`SelectionController` (decisions log
#47) for the identical reason: the Production Menu is itself a `ScrollRect`, so scrolling over
it should scroll its list, not zoom the camera underneath.

`CameraController` has no `GridModel`/Factory dependency — unlike every other controller in the
project, it needs no `Initialize()` call from `GameplayBootstrap`; a direct Inspector-wired
`Camera` reference is sufficient, so the editor hookup below is a plain manual checklist, not a
throwaway script (nowhere near the 10-step threshold).

New module: `CaseGame.CameraControl`. Named that, not `CaseGame.Camera` — a namespace literally
named `Camera` collides with `UnityEngine.Camera` (the exact type this class needs as a field),
a real C# gotcha (`CS0118`) that would force fully-qualifying `UnityEngine.Camera` everywhere in
the file. Renaming the namespace sidesteps it entirely (decisions log #51).

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/CameraControl/CameraController.cs`](Assets/_Project/Scripts/Runtime/CameraControl/CameraController.cs) — the Controller: testable `Pan(Vector2, float)`/`Zoom(float)`, thin `Update()` orchestrator.
- [`Assets/_Project/Scripts/Tests/EditMode/CameraControl/CameraControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/CameraControl/CameraControllerTests.cs) — 7 tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map row, implementation log entry, decisions log #51.
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — added `CameraControl/` to the folder list.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines.

EditMode tests (Mode B batchmode): **134/134 passed** — the 127 pre-existing plus 7 new:
`Pan` moves the camera opposite the drag direction, scaled correctly for a known
orthographic-size/screen-height pair; `Pan` never changes Z; `Pan` with a zero screen height is
a no-op and doesn't throw (guards against a degenerate `Screen.height` read); `Zoom` with
positive/negative scroll decreases/increases `orthographicSize` by the expected amount; `Zoom`
clamps at both the min and max bound.

Not automated (consistent with established precedent — input/lifecycle-bound):
`Update()`'s actual `Mouse.current`/`EventSystem.current` reading — it's a thin orchestrator over
the fully-tested `Pan`/`Zoom`, same pattern as every other controller here.

## 4. Editor hookup checklist
Simple enough for a manual checklist — no throwaway script needed (well under the ~10-step
threshold).

1. Open `Gameplay.unity`. Under the `--- SYSTEMS ---` organizer, create an empty GameObject
   named `CameraController`.
2. **Add Component → Camera Controller** on it.
3. Drag the scene's **Main Camera** into the **Target Camera** field.
4. Leave **Zoom Speed** (2), **Min Orthographic Size** (4), and **Max Orthographic Size** (16) at
   their defaults for now — these are just a starting point; adjust by feel once you can actually
   scroll-zoom in Play Mode; no code change needed, they're plain Inspector fields.
5. Hand-test: enter Play Mode, hold the middle mouse button and drag — the view should pan
   opposite the drag direction; scroll the wheel — the view should zoom in/out, clamped at the
   configured min/max. Also confirm scrolling *over* the Production Menu still scrolls its list
   rather than zooming the camera (the `IsPointerOverGameObject` guard).

## 5. Deviations
None. Zoom bounds (4/16) and default zoom speed (2) are reasonable starting values, not
brief-derived — flagged in the checklist above as tunable by feel, not a hardcoded final answer.
