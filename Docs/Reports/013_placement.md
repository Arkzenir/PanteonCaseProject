**2026-07-22 — Placement**

## 1. Summary
Implemented the `Placement` module (`CaseGame.Placement`) — requirement 3's "user must be
visually informed when the location is invalid." `BuildingGhostView` (the View, per your
request to think through that part) toggles a building instance between a desaturated ghost
silhouette and its real sprite; `PlacementController` (the Controller) drives it — tracks the
mouse over the grid, queries `GridModel.IsAreaFree` each frame with no coupling to specific
building types, and commits (marks the grid occupied, reveals the real sprite) or cancels
(returns the instance to its pool) on input.

**On the grayscale-ghost technique:** your instinct was exactly right, and it needs one thing
to actually work — real desaturation before the tint, not a plain color multiply on the
original sprite. `SpriteRenderer.color` only multiplies existing pixel colors, so tinting the
full-color `Visuals` sprite green/red directly would blend with whatever hues are already in
the art (a blue banner region would multiply to muddy dark-green, not clean green). I wrote a
small custom shader (`Art/Shaders/SpriteGrayscaleGhost.shader`, ~25 lines: samples the
texture, computes luminance, multiplies by `_Color`) for `VisualsGrayscale`'s material —
hand-authored HLSL rather than Shader Graph, specifically to avoid any version-availability
uncertainty around URP's 2D Shader Graph target on this pinned Editor/URP version (golden rule
7); I verified it by batchmode import, which compiles shaders and would surface any error the
same way a C# compile error would.

**On object lifecycle:** the ghost and the final placed building are the *same* pooled
instance — `BuildingGhostView.ShowGhost()`/`Commit()` just flip which children (`Visuals` vs
`VisualsGrayscale`, plus `Hitbox`) are active on one object, rather than managing a disposable
preview object separate from a freshly-instantiated real one. This reuses the existing
`BuildingFactory.Create` path unchanged; cancelling a placement just returns that instance to
its pool via a new `BuildingFactory.Release` method (previously instances only auto-released
via death — placement cancellation isn't a death).

Since nothing calls `BeginPlacement` yet (that's UI.Production's job, not built), I added
`[ContextMenu]` debug helpers on `BuildingGhostView` (Editor-only, stripped from builds) so you
can right-click the component in Play Mode and directly see "Show Valid Ghost" / "Show Invalid
Ghost" / "Commit" working — the fastest way to actually look at the effect before Production
Menu exists to trigger it for real.

## 2. Changes
- [`Assets/_Project/Art/Shaders/SpriteGrayscaleGhost.shader`](Assets/_Project/Art/Shaders/SpriteGrayscaleGhost.shader) — desaturate-then-tint sprite shader.
- [`Assets/_Project/Scripts/Runtime/Placement/BuildingGhostView.cs`](Assets/_Project/Scripts/Runtime/Placement/BuildingGhostView.cs) — the View: `ShowGhost()`/`SetValid(bool)`/`Commit()`, plus Editor-only `[ContextMenu]` debug helpers.
- [`Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs`](Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs) — the Controller: mouse→cell (`Update()`), testable `BeginPlacement`/`UpdateGhostAt(cell)`/`TryCommitAt(cell)`/`CancelPlacement`.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingFactory.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingFactory.cs) — added `Release(prefab, instance)` for the cancel-without-dying case.
- [`Assets/_Project/Scripts/Tests/EditMode/Placement/BuildingGhostViewTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Placement/BuildingGhostViewTests.cs) — 4 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Placement/PlacementControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Placement/PlacementControllerTests.cs) — 6 tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — Placement module map row, implementation log entry, decisions log entries #28–30.
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — `VisualsGrayscale` added to the per-prefab-child-structure override; new shader-naming row (first shader in the project).
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**
— 0 `error CS` lines, and the shader compiled with no shader errors (confirmed by grepping the
batchmode log for "shader error"/"error in" in addition to the usual C# check; the log shows
`SpriteGrayscaleGhost.shader` being imported and run through Unity's shader compiler cleanly).

EditMode tests: **80/80 passed** — the 70 pre-existing plus 10 new:
- `BuildingGhostViewTests` (4): `ShowGhost` hides `Visuals`/`Hitbox` and shows
  `VisualsGrayscale`; `SetValid(true)`/`SetValid(false)` tint the grayscale renderer
  greenish/reddish respectively; `Commit` reverses `ShowGhost`.
- `PlacementControllerTests` (6): `BeginPlacement` starts placing; `CancelPlacement` stops
  it; `UpdateGhostAt` on a free cell tints the ghost valid, on an occupied cell tints it
  invalid; `TryCommitAt` on a free cell marks the grid occupied and returns true (and stops
  placing), on an occupied cell returns false and placement continues.

Added a `PlacementController.CurrentGhost` read-only property (the instance currently being
placed, or null) partway through writing these tests — without it, the tests had no way to
inspect the spawned ghost's own state (its `VisualsGrayscale` color, its position), since the
controller's `_ghostInstance` field is private and the ghost is a separate `Instantiate`d
object from whatever prefab was passed in. Small, legitimate API surface (mirrors `IsPlacing`),
not test-only scaffolding — plausible future consumers exist (e.g. UI wanting to show "what's
currently being placed").

`PlacementController.Update()` itself (actual `Mouse.current` reading) isn't automated-tested,
consistent with established precedent for Update-loop/input-bound code — but unlike earlier
features, almost none of the actual logic lives there; `Update()` is a 5-line orchestrator that
delegates to the fully-tested `UpdateGhostAt`/`TryCommitAt`/`CancelPlacement`.

## 4. Editor hookup checklist
1. Create a Material: right-click in `Assets/_Project/Art/Materials/` (new folder) →
   **Create → Material**, name it `M_SpriteGrayscaleGhost`. Set its **Shader** dropdown to
   **CaseGame → SpriteGrayscaleGhost**.
2. On `Building_Barracks.prefab` and `Building_PowerPlant.prefab`: duplicate the `Visuals`
   child (Ctrl+D), rename the copy to `VisualsGrayscale`, and on its `SpriteRenderer` set
   **Material** to `M_SpriteGrayscaleGhost` (the **Sprite** field stays the same as `Visuals`
   — same artwork, the shader does the desaturation, not a different source image). Set
   `VisualsGrayscale` **inactive** by default (unchecked in the Inspector).
3. On each prefab's root, **Add Component → Building Ghost View**, then wire:
   - **Visuals** → the `Visuals` child
   - **Visuals Grayscale** → the `VisualsGrayscale` child
   - **Grayscale Renderer** → `VisualsGrayscale`'s `SpriteRenderer` component
   - **Hitbox** → the `Hitbox` child (optional field, but fill it in since it's there)
4. To see it working right now (before Production Menu exists to drive real placement):
   enter Play mode with either building prefab instance in a scene, select it in the
   Hierarchy, right-click the **Building Ghost View** component header in the Inspector, and
   choose **Debug: Show Valid Ghost** / **Debug: Show Invalid Ghost** / **Debug: Commit**. You
   should see the sprite go flat-green or flat-red (not a muddy tinted version of the original
   art) and then snap back to the normal sprite on Commit.
5. `PlacementController` isn't added to a scene yet — it needs a `GridModel` and
   `BuildingFactory` via `Initialize(grid, factory)`, and nothing currently calls that (no
   scene bootstrap exists). That wiring is Gameplay scene assembly's job (a later roadmap
   item), not this feature's.

## 5. Deviations
None beyond the `CurrentGhost` property addition, explained in Test results (small, motivated
by testability, not scope creep).
