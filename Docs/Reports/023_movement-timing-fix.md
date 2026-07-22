**2026-07-22 — Movement timing fix**

## 1. Summary
Item 11 off the post-hand-test backlog: `SoldierBase.MoveSpeed` should mean "N grid cells per
second," with a diagonal step counting as exactly 1 cell — same as an orthogonal step, not
√2 world-distance units.

`FollowPath`'s old implementation drove each step with
`Vector3.MoveTowards(transform.position, targetPosition, Definition.MoveSpeed * Time.deltaTime)`
— a constant *world-distance* speed. Since a diagonal step's target is `cellSize·√2` away from
its start (vs. an orthogonal step's `cellSize`), the same nominal `MoveSpeed` made diagonal steps
take ~41% longer than orthogonal ones, contradicting GI-7/8's "shortest path" being measured in
steps, not raw distance.

Fixed by replacing the distance-based `MoveTowards` loop with a fixed-duration, time-based
interpolation: every step now takes exactly `StepDuration(moveSpeed) = 1f / moveSpeed` seconds,
regardless of the step's real-world distance, via a new `InterpolateStep` Lerp. Both are `public
static` methods on `SoldierBase` — pure functions with no MonoBehaviour/coroutine dependency —
extracted specifically so the actual timing contract (duration depends only on `moveSpeed`, never
on distance) is directly unit-testable, since the coroutine itself can't run under this
project's batchmode EditMode test runner (the same Awake/Update-doesn't-fire environment gotcha
already documented in `ENVIRONMENT.md` extends to coroutines needing a live Update loop to tick).

One real behavior change worth flagging: `GridDef_Default.asset`'s `cellSize` is 0.5, not 1.
Under the old world-distance approach, a `moveSpeed` of `3` meant 3 world-units/sec, which for
orthogonal movement worked out to `3 / 0.5 = 6` cells/sec. Under the new cells/sec semantics,
`moveSpeed: 3` now means literally 3 cells/sec — soldiers will feel roughly 2× slower than
before at the same numeric value. This is the correct, intended consequence of the fix (the
number now means what it says), not a bug — but it's a felt-speed change the human may want to
retune by eye. Purely a data value (`UnitDef_Soldier1/2/3.asset`'s `moveSpeed`), no code change
needed either way.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs`](Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs) — added `public static float StepDuration(float moveSpeed)` and `public static Vector3 InterpolateStep(Vector3 start, Vector3 end, float elapsed, float duration)`; `FollowPath` rewritten to use them (fixed per-step duration instead of `MoveTowards`).
- [`Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs`](Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs) — doc comment on `MoveSpeed` clarifies it's cells/second, diagonal steps included, not world-units/second.
- [`Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs) — 6 new tests, see §3.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Units), implementation log entry, decisions log #57.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report; backlog item 11 marked done.

No asset/scene/prefab changes — pure logic.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines.

EditMode tests (Mode B batchmode): **167/167 passed** — the 161 prior plus 6 new, all on
`SoldierBaseTests`:
- `StepDuration_ReturnsReciprocalOfMoveSpeed` — `StepDuration(4) == 0.25`.
- `StepDuration_MatchesWorkedExample_EightStepsAtSpeedFourTakeTwoSeconds` — the human's own
  worked example (5 orthogonal + 3 diagonal steps at speed 4) encoded directly as a regression
  test: `8 * StepDuration(4) == 2f`.
- `InterpolateStep_AtZeroElapsed_ReturnsStart`, `_AtHalfDuration_ReturnsMidpoint`,
  `_ElapsedAtOrPastDuration_ClampsToEnd` — basic interpolation correctness.
- `InterpolateStep_DiagonalStep_SameElapsedFractionOfEqualDuration_ReachesSameProgressFractionAsOrthogonal`
  — the core regression test: an orthogonal step and a diagonal step of the same fixed duration
  are both exactly half-complete at the same elapsed fraction, proving the interpolation is
  time-based, not distance-based (this is precisely the bug being fixed).

The coroutine itself (`FollowPath`'s frame-by-frame `yield return null` loop) is Play-Mode-only
and not covered by an automated test, consistent with `ENVIRONMENT.md`'s documented limitation —
**hand-test: command a soldier to move along a path with both orthogonal and diagonal legs
(e.g. an L-shaped route vs. a straight diagonal route of the same step-count) and confirm they
take visually equal time.**

## 4. Editor hookup checklist
None. Pure logic change; no new scene objects, prefab changes, or asset data. (Optional,
not required: hand-tune `UnitDef_Soldier1/2/3.asset`'s `moveSpeed` values in the Inspector if
the new, correctly-scaled felt speed doesn't match what's wanted — see §1.)

## 5. Deviations
- None. Human-directed fix, implemented as specified (including the worked example verbatim as
  a test).
