# 033 — Unit animations
2026-07-23

## 1. Summary

Backlog item 18: idle/move/attack animations for the 3 soldier types.

**One clarifying question, asked before implementing:** Soldier 3's prefab uses "Pawn" art (a
Tiny Swords worker character — Idle/Run/Interact animations, no combat swing) rather than one of
the pack's actual soldier archetypes. You confirmed: **keep Pawn, use its `Interact` clip as the
attack animation** — no prefab/sprite swap.

**What I found investigating:** the pack's `Warrior_Blue`/`Archer_Blue`/`Pawn_Blue` Animator
Controllers ship as **unwired scaffolds** — every clip is present as its own state, but
`m_Transitions: []` and `m_AnimatorParameters: []` are empty on every single state. "Wiring an
Animator onto the prefabs" (the backlog's own phrasing) turned out to mean building the actual
state machine from scratch, not connecting to an existing one.

**Implementation:**
- `SoldierBase` gained an optional `[SerializeField] Animator animator` — mirrors how
  `GameEntityBase` already holds `spriteRenderer`/`outlineRenderer` directly (a rendering handle,
  not routed through an event layer). Drives two parameters:
  - `IsMoving` (bool) — set true for `FollowPath`'s duration (covers both `MoveTo` and
    `Attack`'s walk-into-range phase, since both call `FollowPath`), reset false in
    `CancelAction()` too — `StopCoroutine` abandons `FollowPath` wherever it's suspended with no
    cleanup, so without this an attack order interrupting a mid-stride move would leave the run
    animation stuck on forever.
  - `Attack` (trigger) — fired once per `PerformAttack()` call, the single point every actual
    hit/shot (melee or ranged) passes through.
- A throwaway script built the Idle↔Run/Any State→Attack/Attack→Run|Idle state machine (5
  transitions, 2 parameters) on all 3 controllers via `UnityEditor.Animations.AnimatorController`'s
  scripting API, then added the `Animator` component to each soldier prefab's `Visuals` child
  **specifically** — confirmed by reading one of the pack's own `.anim` files that its
  `SpriteRenderer.m_Sprite` curve binds via an *empty* GameObject path (resolves against whatever
  GameObject the `Animator` itself sits on), and the real `SpriteRenderer` lives on `Visuals`, not
  the prefab root. Placing the `Animator` on the root would have silently animated nothing.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs`](../../Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs) — new `animator` field; `IsMoving` set in `FollowPath`/reset in `CancelAction`; `Attack` triggered in `PerformAttack`. All calls null-guarded.
- [`Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior Blue Animations/Warrior_Blue.controller`](../../Assets/Tiny%20Swords/Units/Blue%20Units/Warrior/Warrior%20Blue%20Animations/Warrior_Blue.controller) — `IsMoving`/`Attack` parameters + 5 transitions added (Idle↔`Warrior_Run_Blue` on the bool, Any State→`Warrior_Attack_Blue` on the trigger, Attack→Run/Idle on exit time).
- [`.../Archer/Archer Blue Animations/Archer_Blue.controller`](../../Assets/Tiny%20Swords/Units/Blue%20Units/Archer/Archer%20Blue%20Animations/Archer_Blue.controller) — same shape: Idle=`Archer_Idle_Blue`, Run=`Archer_Run_Blue`, Attack=`Archer_Shoot_Blue`.
- [`.../Pawn and Resources/Pawn/Blue Pawn/Pawn Blue Animations/Pawn_Blue.controller`](../../Assets/Tiny%20Swords/Pawn%20and%20Resources/Pawn/Blue%20Pawn/Pawn%20Blue%20Animations/Pawn_Blue.controller) — same shape: Idle=`Pawn_Idle Knife_Blue`, Run=`Pawn_Run Knife_Blue`, Attack=`Pawn_Interact Knife_Blue`.
- [`Assets/_Project/Prefabs/Units/Soldier_1.prefab`](../../Assets/_Project/Prefabs/Units/Soldier_1.prefab) / [`Soldier_2.prefab`](../../Assets/_Project/Prefabs/Units/Soldier_2.prefab) / [`Soldier_3.prefab`](../../Assets/_Project/Prefabs/Units/Soldier_3.prefab) — each `Visuals` child gained an `Animator` component (controller = the matching one above); each root's `Soldier` component's new `animator` field wired to it.

**Throwaway editor script** (`Scripts/Editor/Setup/Temp/SoldierAnimationSetup.cs`, per CLAUDE.md's
editor script policy — repeated structural wiring across 3 controllers + 3 prefabs, well past the
manual-checklist threshold): built via `UnityEditor.Animations.AnimatorController`'s API, verified
by reading the regenerated `.controller`/`.prefab` files back — confirmed on all 3 controllers:
`IsMoving` (Bool, type 4) and `Attack` (Trigger, type 9) parameters present, exactly 5
`AnimatorStateTransition` objects, one Any-State transition, default state correctly set to each
controller's own Idle state. Confirmed on all 3 prefabs: an `Animator` component (`!u!95`) on the
`Visuals` GameObject specifically, `m_Controller` pointing at the matching `.controller`'s GUID,
and `SoldierBase.animator` referencing that same component. Deleted immediately after
verification.

## 3. Test results

Batchmode EditMode run: **218/218 passing, 0 compile errors** — unchanged count from Report 032.
No new tests were added: the logic added to `SoldierBase` is entirely null-guarded side effects
on an `Animator` reference (no new branching/decision logic to isolate), and the null-safety path
is already implicitly exercised by every existing `MoveTo`/`Attack` test, none of which assign an
`Animator` — they continue to pass, confirming the guards work.

**Hand-test required** (animation timing/feel/visual correctness can't be verified from this
environment):
- Select a soldier and issue a move order — confirm it plays its run animation while walking and
  returns to idle on arrival.
- Command an attack (melee: Soldier 1/3; ranged: Soldier 2/Archer) — confirm the attack animation
  plays on each hit/shot, and that walking into range first shows the run animation before the
  attack starts.
- Interrupt a move with a new attack order (or vice versa) mid-stride — confirm the animation
  switches cleanly with no stuck-on run/attack state.
- Specifically check Soldier 3 (Pawn/`Interact Knife`) reads acceptably as an "attack" — it's a
  generic tool-swing motion, not a sword strike, per your own confirmed choice.
- If attacks tick faster than the Attack clip's own playback length (fast `AttackSpeed`), the
  `Attack` trigger may not visually restart mid-clip (Any State transitions default to not
  re-triggering into their own current state) — worth a glance at high attack-speed values; not
  fixed this pass since it's an authored-controller nicety, not a functional bug.

## 4. Editor hookup checklist

None — all wiring (controller transitions/parameters, `Animator` components, `SoldierBase`
references) was already applied and verified via the throwaway script above.

## 5. Deviations

- **Soldier 3 keeps Pawn art** — human-confirmed deviation from "3 proper soldier archetypes,"
  recorded as decision #68(d). Its attack animation is a generic tool-interact clip, not a combat
  swing.
- **The selection outline (`GameEntityBase.SetSelected`'s `outlineRenderer`) does not track the
  Animator's current frame** — it still shows whatever sprite was set at `Initialize()` time
  (the definition's static icon sprite), not the currently-playing animation frame. This is a
  pre-existing limitation exposed by adding real animation, not something this pass caused or
  fixed — flagging it as a small, separately-scoped follow-up if the visual mismatch (a
  frozen-frame outline behind an animating sprite) bothers you in Play Mode.
- Everything else follows CONVENTIONS.md; no other deviations.
