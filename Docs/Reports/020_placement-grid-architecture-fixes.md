**2026-07-22 — Placement/Grid architecture fixes**

## 1. Summary
The first item off the post-hand-test polish backlog's "Placement/Grid architecture fixes"
group — three related, human-requested fixes (not brief-mandated, but two of them fix a real
bug against requirements 3/12's implicit correctness expectations).

**Center-anchored footprint placement.** Building art is centered on its own GameObject origin,
but the grid was anchoring a building's footprint at its bottom-left corner — so a centered
sprite ended up visually offset from the cells it actually occupied. Fixed: the cell under the
cursor is now treated as the footprint's *center*. `GridModel` gained `FootprintCenterToWorld`
(the true geometric center of a footprint rectangle — correct for both odd and even sizes,
unlike picking a single "center cell," which doesn't exist for an even footprint).
`PlacementController` converts the hover cell to a bottom-left "origin" internally via integer
division (`origin = hoverCell - footprint/2`) before handing it to the unchanged
`IsAreaFree`/`SetAreaOccupied` — so the occupancy *shape* is identical, only what the cursor
cell means changed.

**"Remove Building"** on the Information Panel. Rather than building a parallel removal path, it
just calls `ApplyDamage(MaxHealth)` on the selected building — reusing the existing death
pipeline exactly. Investigating this surfaced a real, pre-existing gap: buildings destroyed in
*combat* never released their occupied grid cells either — nothing previously connected "this
building died" to "unoccupy its footprint," for either path. Fixed once, for both: `GameEntityBase`
gained a `protected virtual OnEntityDied()` hook (called from the death path alongside the
existing pooling callback), which `BuildingBase` overrides to release its footprint. `BuildingBase`
now tracks its own placement state (`FootprintOrigin`, set by `PlacementController.TryCommitAt`
via a new `SetPlacement` call) so it can self-manage this without `PlacementController` needing
to externally track "which cells does every placed building occupy" for its entire lifetime.

Since a selected building can now disappear via a second path (not just distant combat),
`SelectionController.SetSelectedBuilding` also gained a defensive staleness check — mirrors the
existing dead-soldier-pruning pattern (decisions log #39) — so a since-reused pooled reference
can't be mistaken for "already selected."

**Unit spawn-cell occupancy.** Units now spawn at the grid cell nearest their Barracks'
`SpawnPoint` (not its raw world position), and production is blocked outright if that cell
already has a building *or* another unit on it. `UnitFactory` gained `ActiveUnits` — a live set
of everything it's created and hasn't released — used for the unit-vs-unit check. This is
deliberately a point-in-time scan at spawn time, not a persisted per-cell occupancy grid for
units the way buildings have: units move continuously, and the brief only requires routing
*around buildings* (GI-7/8), not other units — a persisted, continuously-updated unit-occupancy
layer would be real, unscoped complexity in the direction of unit-vs-unit collision/pathfinding
that wasn't asked for.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Grid/GridModel.cs`](Assets/_Project/Scripts/Runtime/Grid/GridModel.cs) — added `FootprintCenterToWorld`.
- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs`](Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs) — added `protected virtual OnEntityDied()`, called from the death path.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs) — `FootprintOrigin`/`SetPlacement`; overrides `OnEntityDied()` to release its grid footprint. New dependency on `CaseGame.Grid`.
- [`Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs`](Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs) — `UpdateGhostAt`/`TryCommitAt`/`IsCellValid` now treat the cell as the footprint's center (`ComputeFootprintOrigin`); `TryCommitAt` calls `SetPlacement`.
- [`Assets/_Project/Scripts/Runtime/Units/UnitFactory.cs`](Assets/_Project/Scripts/Runtime/Units/UnitFactory.cs) — added `ActiveUnits`, tracked on create/death.
- [`Assets/_Project/Scripts/Runtime/Units/UnitProductionController.cs`](Assets/_Project/Scripts/Runtime/Units/UnitProductionController.cs) — `Initialize` now also takes a `GridModel`; snaps spawn position to a cell, blocks if occupied.
- [`Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs`](Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs) — new `removeBuildingButton` field, `RequestRemoveBuilding()`.
- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — defensive `IsDead` staleness check in `SetSelectedBuilding`.
- [`Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs`](Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs) — updated `unitProductionController.Initialize` call for the new `GridModel` parameter.
- Tests updated/added across `GridModelTests`, `PlacementControllerTests`, `BuildingBaseTests`, `UnitFactoryTests`, `UnitProductionControllerTests`, `InfoPanelControllerTests`, `SelectionControllerTests` — see §3.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Grid, Entities, Buildings, Units, Placement, Selection, UI.Info), implementation log entry, decisions log #52–54.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report; also condensed the now-two-features-old Report 017/018 detail to keep the file from growing unboundedly.

No asset/scene/prefab changes this feature — pure logic, no editor hookup needed.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines (one
compile error caught and fixed during development: a test helper's return type needed to be
`SoldierBase`, not `Soldier`, to match `UnitFactory.ActiveUnits`' element type).

EditMode tests (Mode B batchmode): **153/153 passed** — the 134 pre-existing plus 19 new:
- `GridModelTests` (+3): `FootprintCenterToWorld` returns the true center for even and odd
  footprints, and respects a non-zero origin/cell size.
- `PlacementControllerTests` (+3 new, 1 updated): ghost/commit position at the true footprint
  center for both `UpdateGhostAt`/`TryCommitAt`; `TryCommitAt` records the correct origin on the
  instance; the pre-existing "marks grid occupied" test updated for the new hover-cell semantics
  (the other three pre-existing tests needed no change — traced by hand that their expected
  occupied/invalid regions still hold under the new centering math).
- `BuildingBaseTests` (+4): `FootprintOrigin` starts null; `SetPlacement` records it; killing a
  placed building releases its footprint (and clears `FootprintOrigin`); killing a
  never-placed building doesn't throw.
- `UnitFactoryTests` (+2): `Create` adds to `ActiveUnits`; a killed instance is removed from it.
- `UnitProductionControllerTests` (2 updated, +3 new): spawn position is now asserted against
  the cell's center (not a raw position); occupied-by-building and occupied-by-another-unit both
  block production; a free cell still spawns.
- `InfoPanelControllerTests` (+3): no-op (doesn't throw) with nothing selected;
  `RequestRemoveBuilding` actually kills the building; the panel hides immediately.
- `SelectionControllerTests` (+1): reselecting the same (pooled-reuse-shaped) instance after it
  died elsewhere still raises the selection-changed event — regression test for the staleness
  fix; confirmed by tracing it would fail (early-return, no raise) without the fix.

## 4. Editor hookup checklist
None. This feature is pure C# logic — no new scene objects, prefab changes, or asset data.
Everything is covered by the automated tests above; nothing needs manual verification beyond
the human's own general hand-testing of placement/removal/production feel in Play Mode.

## 5. Deviations
- `BuildingBase` now depends on `CaseGame.Grid` (a new module dependency, `BuildingFactory`
  itself remains grid-agnostic) — explained in Summary/decisions log #52: needed so a building
  can release its own footprint on death without `PlacementController` maintaining an external
  "which cells does every placed building occupy" registry for the building's entire lifetime.
- Fixed a pre-existing bug (combat-destroyed buildings never freed their grid cells) that
  predates this feature, while implementing "Remove Building" — both paths needed the exact
  same fix, so fixing only the new path would have left the old one silently still broken.
