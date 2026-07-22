**2026-07-22 — Selection**

## 1. Summary
Implemented the `Selection` module (`CaseGame.Selection`) — requirements 8/10/11: left-click
select, right-click move-or-attack. `SelectionController` hit-tests under the cursor via
`Physics2D.OverlapPoint` against each entity's `Hitbox` collider (the child GameObjects
Reports 009/012 already left as placeholders "for future selection" — this is that future).

**Left-click:** a plain click on a building selects it (single). A plain click on a soldier
replaces the current soldier selection with just that soldier. **Shift-click on a soldier**
adds it to (or, if already selected, removes it from) the current selection — the brief's
requirement 8 says "unit(s)," which I read as a real signal that multi-select must be
supported, but it doesn't specify how. I chose shift-click over a drag/marquee box-select
specifically because it needs no new visual (a selection-rectangle overlay) and no
screen-to-world-rect geometry — the whole feature stays logic I can unit-test directly, at the
cost of the more "expected" RTS drag-select feel. Documented as an explicit interpretation
(decision #36) in case you'd rather have the box.

Building selection and soldier selection are mutually exclusive — selecting one clears the
other, since only soldiers take movement/attack commands and only buildings are shown on the
(future) Information Panel.

**Right-click:** if the cursor is over an `IDamageable` (a unit or building), every currently
selected soldier calls `TryAttack` on it — instant, no range check, no walking into position
first. `TryAttack`'s signature (Report 012) already only took a target with no position info,
and its doc comment specifically anticipated Selection wiring it up exactly like this; the
brief's requirement 11 never mentions range or approach, unlike requirement 8's explicit
"shortest path" for movement. Otherwise (empty grid cell under the cursor), every selected
soldier gets `MoveTo(cell, grid)`.

**Visual feedback:** selection tints the entity's existing sprite (`GameEntityBase.SetSelected`,
new) rather than adding a new prefab child — this doubles as a shared capability for both
buildings and soldiers with zero new hookup. Building this surfaced a real latent bug given the
project's pooling architecture: `GameEntityBase.Initialize` didn't reset `spriteRenderer.color`,
so a pooled instance released while selected would start its *next* life pre-tinted yellow.
Fixed by resetting to white on every `Initialize` call.

**Edge case handled:** a selected soldier that dies mid-game is pruned from the selection the
next time a right-click command is dispatched (`IsDead`/null check), so a dead reference doesn't
silently eat a move/attack command. A narrower race — the same pooled instance getting reused
as a *different* soldier before the next command — is a known, accepted, documented limitation
(decision #39), not something I quietly ignored.

**Scope boundary:** `SelectionController` is *not* added to `Gameplay.unity` — same reasoning as
`PlacementController` in Report 014: it needs `Initialize(grid)` and no scene bootstrap exists
yet. That's Gameplay scene assembly's job.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs`](Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs) — added `SetSelected(bool)`; `Initialize` now resets `spriteRenderer.color` to white.
- [`Assets/_Project/Scripts/Runtime/Buildings/SelectedBuildingEventChannel.cs`](Assets/_Project/Scripts/Runtime/Buildings/SelectedBuildingEventChannel.cs) — concrete `GameEventChannel<BuildingBase>`.
- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — the Controller: `HandleLeftClick`/`HandleRightClick`/`ClearSelection` (testable, explicit inputs) plus a thin `Update()` mouse/hit-test orchestrator.
- [`Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityBaseTests.cs) — 3 new tests for `SetSelected`/the color-reset fix.
- [`Assets/_Project/Scripts/Tests/EditMode/Selection/SelectionControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Selection/SelectionControllerTests.cs) — 14 tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map rows (Entities, Buildings, Selection), implementation log entry, decisions log entries #36–39.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (Mode A `dotnet build` + Mode B Unity batchmode, editor closed for both): **passed**
— 0 `error CS` lines in either.

EditMode tests (Mode B batchmode): **113/113 passed** — the 96 pre-existing plus 17 new:
- `GameEntityBaseTests` (+3): `SetSelected(true)` tints away from white; `SetSelected(false)`
  after `true` restores white; `Initialize` resets a leftover tint from a previous pooled use.
- `SelectionControllerTests` (14): building click selects + raises the channel; soldier click
  (non-additive) replaces the selection; shift-click adds; shift-click on an already-selected
  soldier removes it; selecting a soldier clears a previously selected building and vice versa;
  empty-ground click clears selection (non-additive) / leaves it alone (additive); right-click
  with a target applies each selected soldier's damage (summed, verifying multi-select dispatch);
  right-click with no selection is a no-op; a dead selected soldier is pruned and doesn't attack;
  right-click with a soldier already at the destination cell doesn't throw; `ClearSelection`
  deselects everything and raises `null`.

Consistent with established precedent: `Update()`'s actual `Mouse.current`/`Physics2D.OverlapPoint`
reading isn't automated-tested (same reasoning as `PlacementController`/`ProductionMenuController`
— it's a thin orchestrator over the fully-tested `HandleLeftClick`/`HandleRightClick`). The
**move** branch of `HandleRightClick` is only tested for the "soldier already at the destination"
case (`SoldierBase.MoveTo` short-circuits before calling `StartCoroutine` there) — same boundary
`SoldierBaseTests` already established in Report 012, since `StartCoroutine` isn't safe to
exercise from this batchmode EditMode runner. The **attack** branch (the actually novel logic
here) is fully tested end-to-end including multi-soldier dispatch and dead-soldier pruning.

## 4. Editor hookup checklist
1. On `Building_Barracks.prefab`, `Building_PowerPlant.prefab`, `Soldier_1.prefab`,
   `Soldier_2.prefab`, `Soldier_3.prefab`: select the **Hitbox** child → **Add Component →
   Box Collider 2D**. Check **Is Trigger**. Use **Edit Collider** (or just eyeball it) to size
   the box to roughly match the sprite — this is what click-selection/attack-targeting actually
   hit-tests against, so it needs to cover the visible art reasonably well.
2. That's it for this feature — `SelectionController` isn't in any scene yet (see Scope
   boundary above), so there's nothing to hand-test end-to-end until Gameplay scene assembly.
   Once it is, the thing worth checking is: click a building → it tints; click a soldier → it
   tints and a previous building selection clears; shift-click a second soldier → both stay
   tinted; click empty ground → both clear; right-click a tinted-selected soldier's target →
   damage applies / the soldier walks there.

## 5. Deviations
- **Multi-select mechanism (shift-click, not drag-box)** — the brief says "unit(s)" but doesn't
  specify how multiple units get selected. Explained in Summary and decisions log #36; easy to
  swap for a drag-box later if you'd rather have that instead.
- **Attack is instant/range-less** — `TryAttack` applies damage immediately regardless of
  distance between the attacking soldier and the target. Explained in Summary and decisions
  log #37; matches `TryAttack`'s existing (Report 012) signature and the brief's actual wording.
- `GameEntityBase.Initialize` resetting `spriteRenderer.color` to white — a necessary correctness
  fix surfaced by adding `SetSelected` to a pooled type, not a stylistic addition (decision #38).
