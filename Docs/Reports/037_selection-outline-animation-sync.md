2026-07-23 — Selection outline animation sync

## 1. Summary

Human-reported bug: the selection outline stays frozen on the sprite it was given at spawn time,
while an animated soldier's real sprite changes every frame via its `Animator` — visibly out of
sync (different pose) the moment a selected unit moves or attacks. Fixed with the simplest of the
two options the human proposed: `GameEntityBase` now syncs `outlineRenderer.sprite` to
`spriteRenderer.sprite` every frame (when the outline is actually visible), rather than duplicating
an `Animator`/Controller onto the outline object. Confirmed with the human beforehand that this
carries no draw-call cost — the outline already renders via its own dedicated material and was
already its own draw call whenever selected; this change only alters which sprite/frame that
existing draw call shows, not how many draw calls happen.

## 2. Changes

- `Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs` — added a `LateUpdate` that sets
  `outlineRenderer.sprite = spriteRenderer.sprite` whenever `outlineRenderer.enabled` (gated so
  unselected entities don't pay even a cheap reference-copy each frame). `LateUpdate`, not
  `Update`, so it always runs after the Animator has applied the current frame's sprite.
- `Docs/Agent/ARCHITECTURE.md` — decisions log entry #73.
- `Docs/Agent/CURRENT_STATUS.md` — "Last report" pointer and "Done" list updated.

## 3. Test results

No new EditMode tests — this is a pure rendering-sync side effect (a `SpriteRenderer.sprite`
reference copy) with no branching logic to unit-test independent of a live Animator/render loop,
consistent with this project's established EditMode-testing limits for Animator-driven behavior.
Not run via batchmode this turn either (Unity Editor was open).

**Hand-test:** select an animated soldier (e.g. the Archer or Warrior), then move or attack with it
selected. Expected: the outline tracks the sprite's current animation frame/pose at all times, with
no visible lag or mismatched silhouette. Also re-check draw calls (Stats window/Frame Debugger) if
convenient, to confirm the "no draw-call impact" reasoning holds in practice — not required before
approving this report, since the numeric draw-call verification pass is still explicitly deferred
to backlog item 20 (decisions log #49).

## 4. Editor hookup checklist

None — pure code change, no scene/prefab/asset edits.

## 5. Deviations

None.
