# 034 — Post-hand-test polish/bugfix pass
2026-07-23

## 1. Summary

Four items flagged after you playtested Report 033's animations:

1. **Units flip to face movement/attack direction.** You suggested either flipping the whole
   GameObject's scale or grouping the visual children under a scaled parent, and explicitly
   invited a counter-argument. I went with a third option: `GameEntityBase.SetFlippedHorizontally`
   toggles `SpriteRenderer.flipX` on both the sprite and its selection outline. This is the
   idiomatic Unity 2D tool for exactly this and has zero blast radius on `Collider2D` shapes,
   child-transform math, or any future code reading `transform.localScale`/`lossyScale` — a
   negative scale would likely have been harmless too given this project's simple symmetric
   colliders, but "has no blast radius by construction" beats "probably fine." No new hierarchy
   needed, since `GameEntityBase` already directly owns both renderer references. Driven by a new
   pure `SoldierBase.FacesLeft(fromX, toX)`, called from movement direction (`FollowPath`) and
   attack-target direction (`PerformAttack`).
2. **Archer's arrow fires at the end of the animation**, not the instant the attack tick starts.
   Implemented via a new `SoldierBase.ReleaseAttack()` — called through a new
   `SoldierAnimationEvents` relay component (Animation Events call `SendMessage` on the `Visuals`
   child where the `Animator` lives, not the root where `SoldierBase` lives) wired to an Animation
   Event on `Archer_Shoot_Blue.anim` at its last frame. Melee's instant damage is unchanged; a
   soldier without a wired `Animator` also falls back to the old immediate-launch behavior.
3. **Fixed a real bug**, root-caused, not just patched at the symptom: cancelling a building
   placement with right-click was *also* moving the currently-selected unit to the clicked cell.
   `PlacementController` and `SelectionController` each independently read the same right-click in
   their own `Update()`, with no awareness of each other. The identical gap exists for left-click
   too (committing a placement could also deselect the current selection) — fixed alongside the
   reported case, since it's the same root cause: `SelectionController` now skips its own
   `Update()` entirely while a `PlacementController` reference reports `IsPlacing`.
4. **Fixed camera zoom jumping to min/max on one scroll notch.** Root cause: the new Input
   System's `Mouse.scroll` reports ±120 per physical notch on Windows (matching the legacy
   `WHEEL_DELTA` convention), not the small ±1-ish value `zoomSpeed` was tuned against — your
   repro numbers (zoomSpeed 0.2, one notch swinging across an 18-unit min/max range) confirmed
   this exactly. A new pure `CameraController.NotchesFromRawScrollDelta` divides by 120 before
   `Zoom` is called; `Zoom` itself (and its existing tests) is unchanged.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs`](../../Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs) — new `protected SetFlippedHorizontally(bool)`; reset to unflipped in `Initialize` (pooled-reuse hazard, same reasoning as the existing outline reset).
- [`Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs`](../../Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs) — new pure `FacesLeft(fromX, toX)`; facing updates in `FollowPath` (per step) and `PerformAttack` (toward target); new `ReleaseAttack()` + pending-target/damage/factory fields; ranged `PerformAttack` defers to it when an `Animator` is present.
- [`Assets/_Project/Scripts/Runtime/Units/SoldierAnimationEvents.cs`](../../Assets/_Project/Scripts/Runtime/Units/SoldierAnimationEvents.cs) (new) — relay `MonoBehaviour`, `OnAttackRelease()` forwards to the parent `SoldierBase.ReleaseAttack()`.
- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](../../Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — new `placementController` field; `Update()` returns early while it reports `IsPlacing`.
- [`Assets/_Project/Scripts/Runtime/CameraControl/CameraController.cs`](../../Assets/_Project/Scripts/Runtime/CameraControl/CameraController.cs) — new pure `NotchesFromRawScrollDelta`; `Update()` converts the raw scroll value before calling `Zoom`.
- [`Assets/_Project/Prefabs/Units/Soldier_2.prefab`](../../Assets/_Project/Prefabs/Units/Soldier_2.prefab) — `Visuals` child gained a `SoldierAnimationEvents` component, wired to the root's `Soldier`.
- [`Assets/Tiny Swords/Units/Blue Units/Archer/Archer Blue Animations/Archer_Shoot_Blue.anim`](<../../Assets/Tiny%20Swords/Units/Blue%20Units/Archer/Archer%20Blue%20Animations/Archer_Shoot_Blue.anim>) — Animation Event added at `time: 0.7` (last frame) calling `OnAttackRelease`.
- [`Assets/_Project/Scenes/Gameplay.unity`](../../Assets/_Project/Scenes/Gameplay.unity) — `SelectionController`'s new `placementController` field wired to the existing `PlacementController` (direct low-risk YAML edit).
- [`Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs) (+6) — `FacesLeft` (left/right/tie), `ReleaseAttack` with no pending target, `MoveTo` toward a cell to the left/right flips/doesn't flip a wired `SpriteRenderer`.
- [`Assets/_Project/Scripts/Tests/EditMode/CameraControl/CameraControllerTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/CameraControl/CameraControllerTests.cs) (+4) — `NotchesFromRawScrollDelta` (one notch, reverse, half-notch), and a direct repro of your reported numbers confirming one notch no longer jumps to an extreme.

**Throwaway editor script** (`Scripts/Editor/Setup/Temp/ArcherReleaseEventSetup.cs`, per CLAUDE.md's
editor script policy): added `SoldierAnimationEvents` to Soldier_2's `Visuals` child and the
Animation Event to the Shoot clip. Verified by reading both regenerated files back: the prefab's
`soldier` field resolves to the `Soldier_2` root's `Soldier` component; the clip's `m_Events`
block has exactly one entry, `time: 0.7`, `functionName: OnAttackRelease`. Deleted immediately
after verification.

## 3. Test results

Batchmode EditMode run: **228/228 passing, 0 compile errors** (218 from Report 033 + 10 new).

**Hand-test required** (visual/feel correctness can't be verified from this environment):
- Move a unit left and right — confirm it flips to face the direction of travel, and that the
  selection outline flips in lockstep (no visible mismatch between sprite and outline).
- Attack a target to the unit's left vs. right — confirm it faces the target, independent of
  which way it's standing.
- Command an Archer to attack — confirm the arrow now visually leaves the bow at (or very near)
  the release frame rather than instantly on the attack tick. **The event's timing (`time: 0.7`,
  the clip's last frame) is a best-effort guess** — I can't preview the Shoot animation's actual
  draw/release pose from this environment. If the arrow still looks like it fires too early/late,
  the fix is a one-line edit in the Animation window (drag the event marker), not a code change.
- Select a unit, click a Production Menu building, then right-click to cancel — confirm the unit
  no longer also moves. Also check the left-click/commit case: select a unit, start a placement,
  left-click to commit it — confirm the unit stays selected (the analogous bug this same fix
  closes, not separately reported but same root cause).
- Zoom with your notched scroll wheel — confirm one notch now moves smoothly toward min/max
  instead of jumping straight there. If notches still feel too big/small, `zoomSpeed` is now a
  true "orthographic-size change per notch" value and can be retuned directly.

## 4. Editor hookup checklist

None — all wiring (relay component, Animation Event, `SelectionController` reference) was already
applied and verified via the throwaway script and a direct scene edit above.

## 5. Deviations

- **Countered your own suggested approach** for unit facing (§1, item 1) per your explicit
  invitation — used `SpriteRenderer.flipX` instead of a `transform.localScale` flip on the root or
  a new scaled parent grouping. Recorded as decision #69(a).
- Everything else follows CONVENTIONS.md; no other deviations.
