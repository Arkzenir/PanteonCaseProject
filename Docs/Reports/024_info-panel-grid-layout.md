**2026-07-22 — Info Panel producible-units layout fix**

## 1. Summary
Item 13 off the post-hand-test backlog, extended with a more specific ask this turn: change the
Information Panel's producible-unit icon row from a single overflowing line into an actual grid
— N columns, wrapping into full rows with a partially-filled final row when the count doesn't
divide evenly (e.g. 9 units → 3×3 filled, 7 units → 2 filled rows + 1 partial row of 3 columns)
— with the grid's shape and size adjustable through the editor.

`ProducibleUnitsContainer` (in `Assets/_Project/Prefabs/UI/InformationPanel.prefab`) previously
used a `HorizontalLayoutGroup` with no wrapping, so more than ~3 icons ran off the container's
right edge instead of dropping to a new row. Unity's built-in `GridLayoutGroup` already
implements exactly the requested behavior — `Constraint = Fixed Column Count` wraps
automatically once the column count is reached, and `Cell Size`/`Spacing`/`Constraint Count` are
all directly editable in its own Inspector, with zero code. Writing a bespoke C# grid-positioning
class here would have duplicated a problem Unity already solves natively, so this feature is
**pure editor/prefab wiring — no runtime code changed** at all: neither `InfoPanelController`
nor `ProducibleUnitIconView` know or care how their container arranges children, so nothing
about their public API or behavior needed to change.

Also added a `ContentSizeFitter` (vertical Preferred Size) alongside the `GridLayoutGroup`, so
the container's height grows to fit however many rows are actually present instead of a guessed
fixed pixel value that would silently clip a row once the producible-unit count exceeds
whatever height was hand-picked.

Swapping a component (remove `HorizontalLayoutGroup`, add `GridLayoutGroup` + `ContentSizeFitter`)
is a structural prefab edit, not a single scalar tweak, so — matching this project's established
precedent (Reports 013/022) — it was done via a throwaway `Scripts/Editor/Setup/Temp/` script and
verified by reading the regenerated prefab YAML back, then deleted.

While implementing this, `ProducibleUnitIcon.prefab`'s own dimensions changed independently (the
icon/name text were enlarged, root size 72×88 → 90×110) — the human's own concurrent edit made
directly in the Editor, not part of this feature's script. Caught it via the size mismatch it
would have caused (the grid's cell size would've been too small for the now-larger icon) and
corrected `GridLayoutGroup.cellSize` to match the icon's current actual size (90×110) via a
single trivial, low-risk YAML edit — the same category of change as earlier material-property
edits this session.

## 2. Changes
- `Assets/_Project/Prefabs/UI/InformationPanel.prefab` — `ProducibleUnitsContainer`'s `HorizontalLayoutGroup` replaced with a `GridLayoutGroup` (`Cell Size` 90×110 to match the current `ProducibleUnitIcon` prefab size, `Spacing` 8×8, `Constraint` = Fixed Column Count, `Constraint Count` = 3 as an initial default — freely adjustable afterward via the Inspector) and a `ContentSizeFitter` (`Vertical Fit` = Preferred Size).
- `Assets/_Project/Prefabs/UI/ProducibleUnitIcon.prefab` — human's own concurrent edit (icon/name enlarged), not part of this feature; noted here only because it's why the grid's cell size was corrected to 90×110 instead of the icon's earlier 72×88.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (UI.Info), implementation log entry, decisions log #58.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report; backlog item 13 marked done.
- (Throwaway, deleted this turn) `Scripts/Editor/Setup/Temp/ConvertProducibleUnitsContainerToGrid.cs` — performed the component swap; `Scripts/Editor/Setup/Temp/` is empty again.

No runtime C# changed — `InfoPanelController`/`ProducibleUnitIconView` are unaffected.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines (run
before the human's icon-resize edit and the subsequent `cellSize` correction; no code changed
since, so nothing to recompile).

EditMode tests (Mode B batchmode): **167/167 passed** — unchanged from the prior report, since
no runtime code was touched by this feature.

This is a UI-layout-only change with no engine-independent logic to unit test. The human
hand-verified the result directly in the Editor after the `cellSize` correction and confirmed it
looks correct.

## 4. Editor hookup checklist
None required — the throwaway script already performed and saved the prefab change, and the
human has confirmed the result visually. For future reference, the grid's shape/size can be
retuned any time directly on `ProducibleUnitsContainer`'s `Grid Layout Group` component
(`InformationPanel.prefab`): `Cell Size`, `Spacing`, `Constraint`/`Constraint Count` — no code
or script involved.

## 5. Deviations
- Corrected `GridLayoutGroup.cellSize` from the icon prefab's size at the time this feature
  started (72×88) to its size after the human's own concurrent edit (90×110) — a direct,
  trivial one-line YAML edit rather than a second script run, since it was a single scalar
  value matching an already-established low-risk-edit category this session.
- Used a throwaway setup script for the component swap rather than a manual checklist, since a
  remove-one/add-two component change is a structural prefab edit (this project's precedent:
  Reports 013/022), not the kind of single-property tweak appropriate for a direct hand-edit.
