# 028 — Combat/UI bugfix pass
2026-07-22

## 1. Summary

Three human-flagged issues surfaced after hand-testing Report 027, fixed in one pass:

1. **Attacking a building targeted its `transform.position` cell, not its actual footprint.** A
   unit attacking (or walking toward) a multi-cell building could be judged "out of range" or
   "approach here" against the wrong cell. Fixed with a new `GameEntityBase.GetNearestOccupiedCell`
   virtual method (default: the entity's own cell), overridden in `BuildingBase` to clamp the
   attacker's cell into the building's actual footprint rectangle. This is exactly the
   "clean, isolated follow-up" decision #62(a) flagged and deliberately deferred during Report
   027 — it stays on `GameEntityBase` (not a new `Buildings`-only helper) so `Units` still never
   needs to reference `Buildings` directly, preserving the circular-dependency boundary from
   decision #43/#62.
2. **The arrow projectile didn't rotate to face its travel direction.** `Projectile` now computes
   a facing rotation (`FacingRotation`, pure/testable) toward its live target every frame, with an
   inspector-tunable `spriteForwardOffsetDegrees` correction — the Arrow.png art's own default
   facing direction was never verified against any prior rotation logic, so a hardcoded angle
   risked being backwards or off by 90°; this is adjustable by eye instead.
3. **Building/unit icons stretched.** `InformationPanel.prefab`'s `BuildingIcon`,
   `ProducibleUnitIcon.prefab`'s `Image`, and `ProductionMenuItem.prefab`'s `Icon` all had
   `Preserve Aspect` off on a fixed-size `RectTransform`, so non-square sprite assets stretched to
   fill the box. Turned on directly (`m_PreserveAspect: 1`) — a built-in Unity UI behavior that
   letterboxes within the existing box, no layout/code changes needed.

## 2. Changes

- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs`](../../Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs) — new `public virtual Vector2Int GetNearestOccupiedCell(GridModel, Vector2Int)`, default returns the entity's own cell.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs`](../../Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs) — overrides `GetNearestOccupiedCell` to clamp the attacker's cell into `FootprintOrigin`/`Definition.Footprint`; falls back to the base single-cell behavior if not currently placed on a grid.
- [`Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs`](../../Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs) — `AttackRoutine` now calls `target.GetNearestOccupiedCell(grid, attackerCell)` instead of `grid.WorldToCell(target.transform.position)`, recomputed each range check (attacker and target may both move) and passed as `FindApproachCell`'s target argument.
- [`Assets/_Project/Scripts/Runtime/Units/Projectile.cs`](../../Assets/_Project/Scripts/Runtime/Units/Projectile.cs) — new `spriteForwardOffsetDegrees` field; new pure `static Quaternion FacingRotation(Vector3 currentPosition, Vector3 targetPosition, float forwardOffsetDegrees)`; `Update()` sets `transform.rotation` from it every frame before stepping position.
- [`Assets/_Project/Prefabs/UI/InformationPanel.prefab`](../../Assets/_Project/Prefabs/UI/InformationPanel.prefab) — `BuildingIcon`'s `Image.m_PreserveAspect` 0 → 1 (direct YAML edit).
- [`Assets/_Project/Prefabs/UI/ProducibleUnitIcon.prefab`](../../Assets/_Project/Prefabs/UI/ProducibleUnitIcon.prefab) — `Image.m_PreserveAspect` 0 → 1.
- [`Assets/_Project/Prefabs/UI/ProductionMenuItem.prefab`](../../Assets/_Project/Prefabs/UI/ProductionMenuItem.prefab) — `Icon`'s `Image.m_PreserveAspect` 0 → 1.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs) (+3) — `GetNearestOccupiedCell_AttackerOutsideFootprint_ClampsToNearestEdgeCell`, `_AttackerAboveFootprint_ClampsToTopEdge`, `_NeverPlaced_FallsBackToTransformPositionCell`.
- [`Assets/_Project/Scripts/Tests/EditMode/Units/ProjectileTests.cs`](../../Assets/_Project/Scripts/Tests/EditMode/Units/ProjectileTests.cs) (new file, +4) — `FacingRotation_TargetDirectlyToTheRight_FacesZeroDegrees`, `_TargetDirectlyAbove_FacesNinetyDegrees`, `_AppliesForwardOffset`, `_SamePositionAsTarget_ReturnsIdentity`.
- [`Docs/Agent/ARCHITECTURE.md`](../Agent/ARCHITECTURE.md) — module map entries updated (Entities, Buildings, Units); new decisions log entry #63.
- [`Docs/Agent/CURRENT_STATUS.md`](../Agent/CURRENT_STATUS.md) — "Last report" updated to 028, Report 027's blurb kept immediately below it, Done line extended.

## 3. Test results

Batchmode EditMode run (`-runTests -testPlatform EditMode`): **186/186 passing, 0 compile
errors** (179 from Report 027 + 3 `BuildingBaseTests` + 4 `ProjectileTests`).

**Hand-test** (Play Mode, since coroutine progression and live rendering can't be verified by
batchmode EditMode tests in this environment — `ENVIRONMENT.md`):
- Place a building with a footprint larger than 1×1 (e.g. a 2×2 test building, if one exists, or
  temporarily bump `Barracks`/`PowerPlant`'s footprint for the check). Attack it from each side —
  confirm the attacking unit walks to and stops at the *nearest* footprint edge, not always the
  same corner, and that `attackRange` is judged from that nearest edge.
- Command an Archer (Soldier 2) to attack a moving target — confirm the arrow visually rotates
  to track its flight direction as it travels, not staying at a fixed orientation. If it looks
  backwards or rotated 90° off from the actual travel direction, tune the new
  `spriteForwardOffsetDegrees` field on the `Projectile` prefab (try 180 or ±90) until it looks
  right — no code change needed.
- Open the Information Panel (select a building with producible units) and the Production Menu —
  confirm building/unit icons no longer look squashed/stretched, and instead show their native
  aspect ratio letterboxed within the existing icon box.

## 4. Editor hookup checklist

None — all three fixes are either pure code changes (picked up automatically by every existing
prefab reference to `GameEntityBase`/`BuildingBase`/`SoldierBase`/`Projectile`) or direct,
already-applied prefab edits (`Preserve Aspect`). Nothing new needs manual scene/prefab wiring
this turn.

If the Arrow.png sprite's rotation looks wrong when hand-tested (see §3), that's a one-field
Inspector tweak, not a hookup step:
1. Select `Assets/_Project/Prefabs/Units/Projectile.prefab`.
2. On its `Projectile` component, adjust `Sprite Forward Offset Degrees` (try 90, -90, or 180)
   until the arrow visually points along its flight direction.

## 5. Deviations

None from CONVENTIONS.md. The footprint-aware range check is not a new deviation — it directly
implements the follow-up decision #62(a) already flagged and deferred, now revisited per the
human's own testing.
