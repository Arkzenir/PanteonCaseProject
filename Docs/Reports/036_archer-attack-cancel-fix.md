2026-07-23 — Archer attack-cancel fix

## 1. Summary

Human-reported bug: if the Archer is moved while its attack animation is still playing, the
animation was allowed to keep playing to completion and its projectile still fired at the end —
even though the unit had already been ordered elsewhere. Root cause: `SoldierBase.CancelAction`
(called by both `MoveTo` and `Attack`) stops the C# action coroutine, but the Animator's own state
machine runs independently of that coroutine — once the `Attack` trigger has fired, the Attack
state plays out on its own regardless of what the coroutine does. `CancelAction` now (a)
unconditionally clears the pending ranged-release target, so a late `ReleaseAttack()` Animation
Event always no-ops, and (b) — only when a release was actually pending, i.e. the Animator is
genuinely mid-attack — forces the Animator straight back to Idle via `animator.Play(...)`, using a
state hash captured once at `Start()` rather than any hardcoded per-controller state name. No
Animator Controller/asset edits were needed.

## 2. Changes

- `Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs` — added a private `_idleStateHash` field
  and a new `Start()` that captures `animator.GetCurrentAnimatorStateInfo(0).fullPathHash` (every
  wired controller's default state is Idle, decisions log #68). `CancelAction()` now always clears
  `_pendingRangedTarget`, and forces `animator.Play(_idleStateHash, 0, 0f)` when a release was
  pending at cancel time.
- `Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs` — added
  `Attack_ThenMoveTo_CancelsAttackAndDoesNotThrow`, a regression test for the always-active
  (non-Animator) half of the code path.
- `Docs/Agent/ARCHITECTURE.md` — decisions log entry #72.
- `Docs/Agent/CURRENT_STATUS.md` — "Last report" pointer and "Done" list updated.

## 3. Test results

**Not run via batchmode this turn** — the Unity Editor was open when this fix was made, and the
human opted to hand-test directly rather than close the Editor for an automated pass. The new
test (`Attack_ThenMoveTo_CancelsAttackAndDoesNotThrow`) exercises only the always-active half of
`CancelAction` (no Animator wired in the test soldier, matching this project's established
EditMode-testing limits for Animator-driven behavior — decisions log #68/#69). The
Animator-specific half (the actual reported bug) is Play-Mode-only behavior and needs the
hand-test below.

**Hand-test:** In Play Mode, select the Archer, right-click a target to start an attack, then —
while its draw/shoot animation is still visibly playing — issue a move order (left-click a
walkable cell) before the animation finishes. Expected: the attack animation snaps back to
Idle/Run immediately, and no arrow is fired. Compare against before this fix, where the animation
kept playing and the arrow still launched after the unit had already started moving.

## 4. Editor hookup checklist

None — pure code change, no scene/prefab/asset edits.

## 5. Deviations

None.
