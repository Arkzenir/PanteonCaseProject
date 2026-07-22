**2026-07-22 — Info Panel stack reflow (polish and bugfix pass)**

## 1. Summary
Same-day polish-and-bugfix pass following Report 024, in three parts: the original ask (reflow
the panel's sections so they fit properly as the producible-units grid grows/shrinks), a width
bug the human caught and fixed by hand, and a layout-timing bug fixed in code.

**Part 1 — stack reflow.** Report 024 gave `ProducibleUnitsContainer` a `ContentSizeFitter` so
its own height correctly tracks the producible-unit grid's row count, but nothing connected that
height change to the rest of the panel — `RemoveButton` was still anchored to a fixed position
relative to the panel's center, correct only by coincidence when the grid happened to be exactly
1 row tall. Fixed with the standard Unity idiom for "a variable-height element should push the
next one down": added a `VerticalLayoutGroup` to `PanelContent` (the parent holding all 4
sections), so `BuildingIcon` → `BuildingName` → `ProducibleUnitsContainer` → `RemoveButton` stack
top-to-bottom, each positioned based on the actual height of everything above it — including
`ProducibleUnitsContainer`'s own `ContentSizeFitter`-computed height (Unity resolves nested
layout groups bottom-up before a parent group reads a child's size, so this composition needs no
custom code). Done via a throwaway `Scripts/Editor/Setup/Temp/` script, verified by reading the
regenerated prefab back, then deleted.

**Part 2 — width bug (human caught, human fixed).** The initial `VerticalLayoutGroup` config
left `Child Force Expand Width` on. Reading Unity's own `HorizontalOrVerticalLayoutGroup` source
(`Library/PackageCache/com.unity.ugui@1.0.0/.../HorizontalOrVerticalLayoutGroup.cs`) confirmed
why: `GetChildSizes` unconditionally does `if (childForceExpand) flexible = Mathf.Max(flexible,
1);` for *every* child, regardless of what that child's own `LayoutElement` says — it's an
all-or-nothing group setting with no per-child opt-out. So `BuildingIcon`/`RemoveButton` got
stretched to the full 328-wide panel despite their `LayoutElement.preferredWidth` (128/160). The
human fixed it directly in the Editor: turned `Force Expand Width` off, and instead gave
`BuildingName`/`ProducibleUnitsContainer` an explicit `LayoutElement.flexibleWidth = 1` each, so
only those two specifically request the extra space — confirmed by reading the resulting prefab
back (`m_ChildForceExpandWidth: 0`, both containers' new `LayoutElement`s with `m_FlexibleWidth:
1`, `BuildingIcon`/`RemoveButton` unchanged).

**Part 3 — layout-timing bug (fixed in code).** With the width fixed, the human found: selecting
a building showed the *previous* selection's stale panel layout for one frame, self-correcting
only on the next selection change. Worse, switching directly from a building with more
producible units to one with fewer (Barracks' 8 → Power Plant's 0, no deselect in between) never
corrected at all. Root cause, in two layers:
1. Unity's `GridLayoutGroup`/`ContentSizeFitter`/`VerticalLayoutGroup` rebuild lazily by default
   (deferred to a later automatic pass), so freshly-instantiated icons don't resize anything
   until after the current frame renders. Fixed by calling
   `LayoutRebuilder.ForceRebuildLayoutImmediate(panelRoot)` right after spawning icons in
   `InfoPanelController.SetSelectedBuilding` — this alone fixed the *growing* direction, since
   `Instantiate` is synchronous.
2. The *shrinking* direction (more → fewer) still failed even with the forced rebuild, because
   `Destroy()` in Play Mode defers actual removal from the hierarchy to end-of-frame — so the
   forced rebuild, running in the very same method call, still measured the about-to-be-destroyed
   old icons as real children. Fixed by changing `DestroyView` to always use `DestroyImmediate`
   (previously `Destroy` in Play Mode, `DestroyImmediate` only in Edit Mode/tests) — safe here
   since `ClearProducibleUnitIcons` only ever iterates its own local list, never Unity's live
   Transform children collection.

The human's own 4-combination test matrix (Power Plant↔Barracks, with/without deselecting in
between) is what surfaced the exact asymmetry (growing worked, shrinking-without-deselect
didn't) that pinned down cause #2.

## 2. Changes
- `Assets/_Project/Prefabs/UI/InformationPanel.prefab`:
  - `PanelContent` gained a `VerticalLayoutGroup` (padding top 16, spacing 16, Upper Center alignment, Child Control Width on / Child Control Height off, Force Expand Width **off**).
  - `BuildingIcon`/`RemoveButton` each have a `LayoutElement` pinning their existing preferred size (128×128 / 160×60).
  - `BuildingName`/`ProducibleUnitsContainer` each have a `LayoutElement` with `flexibleWidth = 1` (human's hand-fix) so only these two stretch to the panel's full width.
- [`Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs`](Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs) — `SetSelectedBuilding` forces an immediate layout rebuild after spawning producible-unit icons; `DestroyView` always uses `DestroyImmediate` (dropped the Play-Mode/Edit-Mode branch).
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (UI.Info), implementation log entry, decisions log #59 (corrected to match the human's actual hand-fix) and #60 (new, the layout-timing bugfix).
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.
- (Throwaway, deleted this turn) `Scripts/Editor/Setup/Temp/AddPanelContentStackLayout.cs` — performed the initial `VerticalLayoutGroup`/`LayoutElement` additions. A second throwaway script drafted to fix the width bug (`FixPanelContentStackWidths.cs`) was discarded unused at the human's request, since they fixed it by hand instead; `Scripts/Editor/Setup/Temp/` is empty.

## 3. Test results
Per explicit instruction, no batchmode compile/EditMode-test run for this pass — the human
hand-verified all three parts directly in the Editor (including the 4-combination selection
test matrix that caught the layout-timing bug). The one runtime code change
(`InfoPanelController`) is a Play-Mode-only layout-timing concern with no engine-independent
logic to unit test either way, consistent with this project's established pattern for
rendering/layout outcomes the agent can't verify without a live Canvas.

Verification performed: reading the regenerated prefab YAML back after both the initial script
run and the human's hand-fix, to confirm the actual component/field values matched what was
intended at each step.

## 4. Editor hookup checklist
None — all prefab wiring is done (partly via throwaway script, partly by the human's own hand
edit) and confirmed working in the Editor.

## 5. Deviations
- Skipped the usual batchmode verification pass, per explicit instruction, for the full
  extent of this turn (not just the original stack-reflow ask) — including the
  `InfoPanelController.cs` code change. Verification was the human's own direct Editor testing.
- This report folds three back-to-back turns (the original ask, the human's width-bug fix, and
  the layout-timing bugfix) into one, per explicit instruction, rather than the usual one-report-
  per-turn granularity.
