# 035 — Selection/placement race fix, corrected
2026-07-23

## 1. Summary

Report 034's fix for the placement/selection interaction bug didn't actually work — you confirmed
both symptoms were still happening: cancelling a placement still moved the selected unit, and
committing one still produced a confusing immediate reselection.

**Root cause of the failed fix:** `PlacementController` and `SelectionController` each
independently read the same right/left-click in their own `Update()`, with no guaranteed
execution order between components on different GameObjects. Report 034's `IsPlacing` guard in
`SelectionController.Update()` only helped in whichever frames `PlacementController`'s `Update()`
happened to run *after* it — when Placement ran first instead (cancelling/committing before
Selection's guard check that same frame), `IsPlacing` was already `false` by the time
`SelectionController` checked it, so the guard silently did nothing and the original bug fired.
This matches exactly what you diagnosed yourself.

**Fix (your first suggested option, which you confirmed was acceptable):** rather than trying to
make the click-ordering deterministic, remove the *stale selection* the race could act on in the
first place. `SelectionController` no longer holds any reference to `PlacementController` at all
— it now independently subscribes to the same `BuildingCatalogEntryEventChannel` that
`PlacementController` already reacts to (the Production Menu's "produce" click) and clears the
current selection the instant a placement is requested. With no old selection left, neither
click-ordering outcome can produce a wrong result: cancelling has nothing to move, and committing
correctly ends with the new building selected (your explicitly-accepted outcome) regardless of
which controller's `Update()` ran first.

Your second option (making the `IsPlacing` check itself non-racy, e.g. via Script Execution
Order) would have worked too, but requires a `ProjectSettings/` change — which needs your explicit
approval per CLAUDE.md before I touch it — for a fix that ends up doing the same job as the
already-established event-channel decoupling pattern this project uses everywhere else. Went with
the simpler, already-approved option.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](../../Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — removed the `PlacementController` reference and the `IsPlacing` guard in `Update()` (both from Report 034, now superseded); added a `produceRequestedChannel` field (`BuildingCatalogEntryEventChannel`), subscribed/unsubscribed in `OnEnable`/`OnDisable`, and a new `HandleProduceRequested(BuildingCatalogEntry)` that calls `ClearSelection()`.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalogEntryEventChannel.cs`](../../Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalogEntryEventChannel.cs) — doc comment updated: Selection is now a second, independent subscriber alongside Placement.
- [`Assets/_Project/Scenes/Gameplay.unity`](../../Assets/_Project/Scenes/Gameplay.unity) — `SelectionController`'s `placementController` reference removed; new `produceRequestedChannel` wired to the same channel asset `PlacementController` already uses (direct low-risk YAML edit).
- [`Assets/_Project/Scripts/Tests/EditMode/Selection/SelectionControllerTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Selection/SelectionControllerTests.cs) (+3) — `HandleProduceRequested` clears soldier selection, clears building selection (and raises null on the channel), and no-ops safely with nothing selected.

No throwaway script needed — this was a pure code + one-line scene-reference change, both within
"freely"/"direct low-risk YAML edit" territory per CLAUDE.md.

## 3. Test results

Batchmode EditMode run: **231/231 passing, 0 compile errors** (228 from Report 034 + 3 new).

**Hand-test recommended** (to confirm this actually fixes it this time, unlike Report 034):
- Select a unit, click a Production Menu building, right-click to cancel — confirm the unit does
  **not** move, regardless of how many times you repeat this (Report 034's fix worked
  inconsistently frame-to-frame; this one shouldn't, since there's no ordering dependency left).
- Select a unit, click a Production Menu building, left-click to commit — confirm the new building
  ends up selected (expected/accepted) and nothing strange happens to the old selection in
  between.

## 4. Editor hookup checklist

None — the scene reference swap was already applied and verified above.

## 5. Deviations

None from CONVENTIONS.md. This report supersedes part of Report 034 (decision #69(c)) rather than
modifying it — per the append-only reports rule, 034 is left as-is; the correction is recorded
here and in ARCHITECTURE.md's decisions log as a new entry (#70) that explicitly references and
supersedes the earlier one.
