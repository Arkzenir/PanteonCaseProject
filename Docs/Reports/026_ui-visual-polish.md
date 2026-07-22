**2026-07-22 — UI visual polish**

## 1. Summary
Backlog item 14: banner headers on the Production Menu and Information Panel (per the brief's
own reference mockup — the Production Menu/Game Board/Information Panel layout sketch shows a
plain labeled header atop each of the two side panels, not the Game Board), plus a Main Menu
"How to Play" screen alongside the existing Play/Settings flow. Per explicit instruction, this
turn skipped batchmode compile/EditMode-test verification — the human is testing by eye.

**Banner headers.** `ProductionMenu.prefab`'s root `RectTransform` previously spanned the full
0–100% of its parent Canvas's height (a docked, fixed-width left panel) with no room reserved
for a header — its `Viewport`/`ScrollRect` internals already fill essentially that whole span.
Rather than touch the `ScrollRect`'s own `Viewport`/`Content` anchors directly (`Viewport`'s
current anchor values look unusual — both `AnchorMin`/`AnchorMax` at `(0,0)` with a zero
`sizeDelta` — and the agent didn't trace exactly why that still renders a working, scrollable
list, so touching it felt like unnecessary risk to an already-working feature), the *parent*
`ProductionMenu` root's own `AnchorMax.y` was trimmed from `1` to `0.94`, freeing a strip at the
top of the screen without needing to know or touch any of the ScrollRect's internal numbers —
everything inside resizes proportionally for free, since it's all anchored relative to the
now-smaller parent. A new `ProductionMenuBanner` (background `Image` + centered TMP text reading
"Production Menu") was added as a Canvas-level sibling in `Gameplay.unity`, sized to exactly the
freed strip.

The Information Panel's banner went inside `PanelContent`'s existing `VerticalLayoutGroup` stack
(Report 025) as its first element — a plain TMP text reading "Information" with a
`LayoutElement` (`flexibleWidth = 1`, fixed `preferredHeight = 40`). This makes it show/hide
along with the rest of the panel's content (`panelRoot` is the single GameObject
`InfoPanelController.SetSelectedBuilding` toggles for the *entire* panel) rather than being an
always-visible header independent of selection state — a deliberate scope choice: making it
always-on would need splitting `InformationPanel` into a persistent-header part and a
toggled-content part, a bigger structural change than "add a banner" asked for. Easy to promote
later if wanted.

**Main Menu "How to Play" screen.** `MainMenuController` gained a third panel following the
*exact* existing Settings pattern: `howToPlayPanel`/`howToPlayButton`/`howToPlayBackButton`
fields, an `OnHowToPlayClicked` handler, and `ShowMainPanel` now also hides the new panel. Built
by duplicating (`Object.Instantiate`) the existing `SettingsButton` and `SettingsPanel` rather
than hand-authoring new TMP/Button hierarchies from scratch — safer, since a clone starts from a
known-working configuration (colors, fonts, Button transition states) instead of code that can't
be compile-checked ahead of the human's own testing. The duplicate's Settings-specific children
(`ResolutionLabel`, `ResolutionDropdown`, `FullscreenToggle`, `ApplyButton`) were removed, `Title`
and `BackButton` were kept and retitled/reused as-is, and a new `ControlsText` block was added
explaining the core controls (select/move/attack/produce). Also removed: the cloned
`SettingsController` component — its `OnEnable` unconditionally dereferences
`fullscreenToggle`/`resolutionDropdown`/`applyButton`, so leaving it attached to a panel that no
longer has those children would throw a `NullReferenceException` the instant "How to Play" is
clicked.

All three structural changes (2 scenes + 1 prefab) were made via a throwaway
`Scripts/Editor/Setup/Temp/` script, verified by reading every regenerated file back rather than
trusting the batchmode exit code, then deleted.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/UI/MainMenu/MainMenuController.cs`](Assets/_Project/Scripts/Runtime/UI/MainMenu/MainMenuController.cs) — new `howToPlayPanel`/`howToPlayButton`/`howToPlayBackButton` fields; `OnHowToPlayClicked`; `ShowMainPanel` also hides the new panel.
- `Assets/_Project/Prefabs/UI/ProductionMenu.prefab` — root `AnchorMax.y` trimmed 1 → 0.94 (a single-field, low-risk direct edit).
- `Assets/_Project/Prefabs/UI/InformationPanel.prefab` — new `Banner` (TMP text "Information" + `LayoutElement`) as the first child of `PanelContent`'s stack.
- `Assets/_Project/Scenes/Gameplay.unity` — new `ProductionMenuBanner` (background `Image` + TMP text "Production Menu") as a Canvas-level sibling, sized to the strip freed above.
- `Assets/_Project/Scenes/MainMenu.unity` — new `HowToPlayButton` (clone of `SettingsButton`, repositioned, retexted) and `HowToPlayPanel` (clone of `SettingsPanel`, stripped to `Title`/`BackButton` + new `ControlsText`, `SettingsController` removed); `MainMenuController`'s new fields wired.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (UI.Production, UI.Info, UI.MainMenu), implementation log entry, decisions log #61.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report; backlog item 14 marked done.
- (Throwaway, deleted this turn) `Scripts/Editor/Setup/Temp/AddUIVisualPolish.cs` — performed all four scene/prefab changes above; `Scripts/Editor/Setup/Temp/` is empty again.

## 3. Test results
Per explicit instruction, no dedicated batchmode compile/EditMode-test run this turn. The
throwaway script's own execution did require Unity to compile the modified
`MainMenuController.cs` first (batchmode always compiles before running `-executeMethod`), so
that specific change is incidentally known to compile — but this is not the project's usual
dedicated verification pass, and no EditMode tests were run or added (this feature has no new
engine-independent logic; it's UI wiring plus one small, direct mirror of an already-tested-by-
precedent pattern).

Every generated/modified file was read back directly to confirm the actual values (RectTransform
anchors, `LayoutElement` fields, GameObject hierarchy/component lists, `MainMenuController`'s
serialized field references) — see §1 for specifics. The human will verify visually in the
Editor.

## 4. Editor hookup checklist
None required — the throwaway script performed and saved every change; confirmed correct by
reading the files back. **Hand-test when convenient**: open `MainMenu.unity`, click "How to Play"
→ confirm the controls text shows and "Back" returns to the main screen; open `Gameplay.unity` →
confirm the Production Menu banner sits above the scroll list without clipping the topmost row,
and the Information Panel's "Information" banner appears above the building icon when a building
is selected.

## 5. Deviations
- No batchmode verification pass, per explicit instruction for this turn.
- The Information Panel's banner shows/hides with the rest of the panel's content rather than
  being always-visible — a deliberate, smaller-scope interpretation flagged in §1, not a silent
  guess; easy to promote to always-on later if wanted.
- Didn't touch `ProductionMenu`'s own `ScrollRect`/`Viewport` internals directly, instead
  shrinking the parent's own anchor — see decisions log #61 for the specific reasoning (the
  `Viewport`'s current anchor values look unusual and the agent chose not to risk touching them).
