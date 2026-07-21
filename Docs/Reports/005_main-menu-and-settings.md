**2026-07-21 — Main Menu and Settings (Requirement 19)**

> This report was redone in place per explicit human instruction, not shipped as a separate
> feature. The original version of this feature used legacy `UnityEngine.UI` widgets and a
> prev/next-button resolution cycler (to avoid two then-unverified risks: TMP Essential
> Resources weren't imported yet, and the Input System package wasn't installed). The human
> then installed the Input System package and imported TMP Essentials, and asked for this
> feature to be redone using both — TMP_Text/TMP_Dropdown and the Input System — rather than
> filed as a new feature. This is that redo; the report, ARCHITECTURE.md, and ENVIRONMENT.md
> entries tied to the original pass have been rewritten to describe what's actually shipped.

## 1. Summary
Implemented the brief's requirement 19: a basic Main Menu with a Play button and a Settings
screen for resolution/display-mode, completing the `Boot → MainMenu → Gameplay` scene flow
first laid out in Phase 0. `MainMenuController` handles Play/Settings/Back navigation and
calls `GameManager.Instance.LoadScene(...)` to enter Gameplay. `SettingsController` populates
a `TMP_Dropdown` with the display's distinct resolutions (built by the tested
`ResolutionOptionsBuilder`) and applies the chosen resolution + fullscreen state via
`Screen.SetResolution` — this is how GI-13 (aspect ratio/resolution support) gets demonstrated
to the evaluator.

The UI is built with **TextMeshPro** (via `TMPro.TMP_DefaultControls`'s factory methods —
`CreateButton`/`CreateText`/`CreateDropdown` — which correctly build `TMP_Dropdown`'s full
template/viewport/scrollbar sub-hierarchy) rather than legacy `UnityEngine.UI.Text`, and the
scene's `EventSystem` uses the **Input System package**'s `InputSystemUIInputModule` rather
than the legacy `StandaloneInputModule`. Both packages were installed/imported by the human
specifically for this: TMP Essential Resources so `TMP_DefaultControls`/`TMP_Text` are safe to
use, and the Input System package so UI input (and, per the human, gameplay input later) both
go through it — this also finally lets the project follow CONVENTIONS.md's pre-existing
baseline default ("Input: Unity Input System (new)"), which had been un-actionable until now
because the package wasn't installed.

As with the first pass, I used CLAUDE.md's throwaway-setup-script exception given the scale of
one-off scene/UI wiring (a full Canvas hierarchy, two controller components' worth of
`SerializeField` references, Build Settings, `GameManager.firstScene`) — well past the "~10+
steps" threshold for manual wiring. The script ran via batchmode `-executeMethod`, was
verified by reading the generated `.unity`/`EditorBuildSettings.asset` files back, and was
deleted immediately after (`Scripts/Editor/Setup/Temp/` is empty again).

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/UI/MainMenu/MainMenuController.cs`](Assets/_Project/Scripts/Runtime/UI/MainMenu/MainMenuController.cs) — navigation controller: Play → `GameManager.LoadScene`, Settings/Back → panel switching. Unchanged from the first pass (doesn't touch Text/Dropdown/input directly).
- [`Assets/_Project/Scripts/Runtime/UI/Settings/SettingsController.cs`](Assets/_Project/Scripts/Runtime/UI/Settings/SettingsController.cs) — rewritten: `TMP_Dropdown resolutionDropdown` (populated via `ClearOptions`/`AddOptions`) instead of a prev/next-button cycler + plain `Text` label.
- [`Assets/_Project/Scripts/Runtime/UI/Settings/ResolutionOptionsBuilder.cs`](Assets/_Project/Scripts/Runtime/UI/Settings/ResolutionOptionsBuilder.cs) / [`ResolutionOption.cs`](Assets/_Project/Scripts/Runtime/UI/Settings/ResolutionOption.cs) — unchanged; this logic was already engine/UI-technology-independent.
- [`Assets/_Project/Scripts/Runtime/CaseGame.Runtime.asmdef`](Assets/_Project/Scripts/Runtime/CaseGame.Runtime.asmdef) — added `Unity.TextMeshPro` and `Unity.InputSystem` references (alongside the existing `UnityEngine.UI`).
- [`Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef`](Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef) — added `Unity.TextMeshPro` and `Unity.InputSystem` references directly (asmdef references are not transitive — see `ENVIRONMENT.md`).
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Settings/ResolutionOptionsBuilderTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Settings/ResolutionOptionsBuilderTests.cs) — unchanged, still 7/7 passing.
- [`Assets/_Project/Scenes/MainMenu.unity`](Assets/_Project/Scenes/MainMenu.unity) — rebuilt: Canvas (MainPanel + SettingsPanel) using TMP widgets throughout, `EventSystem` + `InputSystemUIInputModule` (auto-wired default UI actions), `MainMenuController` + `SettingsController` fully wired.
- [`Assets/_Project/Scenes/Boot.unity`](Assets/_Project/Scenes/Boot.unity) — `GameManager.firstScene` re-wired to the rebuilt `MainMenu.unity` (same scene, unchanged path/GUID).
- `ProjectSettings/EditorBuildSettings.asset` — re-confirmed `Boot → MainMenu → Gameplay` at `Assets/_Project/Scenes/`.
- [`Docs/Agent/ENVIRONMENT.md`](Docs/Agent/ENVIRONMENT.md) — package versions note updated; the now-obsolete `LegacyRuntime.ttf` note replaced with `TMP_DefaultControls`/`InputSystemUIInputModule` findings and a new note on asmdef references not being transitive.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — implementation log entry and decisions log entries #15–16 rewritten to describe the TMP/Input System implementation instead of the superseded legacy-UI one.
- `Scripts/Editor/Setup/Temp/MainMenuSceneSetup.cs` — throwaway, rewritten to use `TMP_DefaultControls` + `InputSystemUIInputModule`, run once, deleted (see Summary).
- (No change from the first pass, still in effect: `Boot.unity`/`Gameplay.unity` relocated from `Assets/Scenes/` to `Assets/_Project/Scenes/`, `Assets/Scenes/` removed.)

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**,
0 `error CS` lines, after fixing one issue caught mid-verification (see Deviations — asmdef
references aren't transitive).

EditMode tests: **15/15 passed** (8 Grid, 7 `ResolutionOptionsBuilderTests` — this logic didn't
change between the legacy-UI and TMP versions since it never touched UI widgets directly).

`MainMenuSceneSetup`'s output was verified by reading the generated files back rather than
trusting the batchmode exit code alone: confirmed `MainMenu.unity`'s `EventSystem` has an
`InputSystemUIInputModule` with a fully populated default `InputActionAsset` reference (Point/
Move/Submit/Cancel/LeftClick/MiddleClick/RightClick/ScrollWheel/TrackedDevice* all wired to a
real asset GUID — auto-assigned, not something I built), `SettingsController.resolutionDropdown`
resolves to a real `TMP_Dropdown` component, and all other references (`mainPanel`,
`settingsPanel`, `playButton`, `settingsButton`, `backButton`, `gameplayScene`,
`fullscreenToggle`, `applyButton`) point at real objects — no `{fileID: 0}` entries.
`Boot.unity`'s `GameManager.firstScene` and Build Settings were re-confirmed unchanged/correct.

**Hand-test needed** (UI/scene-wiring, per CLAUDE.md's presentational-feature carve-out):
1. Open `Boot.unity`, enter Play mode — should immediately transition to `MainMenu.unity`.
2. Click **Play** (mouse click routed through the Input System now, not legacy Input Manager)
   — should load `Gameplay.unity`.
3. Back in `MainMenu.unity`, click **Settings** — panel should switch to show a resolution
   `TMP_Dropdown`, a Fullscreen toggle, Apply, and Back.
4. Open the dropdown — it should list your display's distinct resolutions (deduped across
   refresh rates) with the current one pre-selected, and scroll correctly if there are many.
5. Click **Apply** — note: in-Editor Play mode, `Screen.SetResolution` does not resize the
   Game view panel the way it would a real windowed build, so this is best confirmed via an
   actual Development Build later rather than in-Editor. The click itself should not error.
6. Click **Back** — should return to the main Play/Settings panel.
7. General input sanity check: since this is the first feature using the Input System package,
   confirm mouse hover/click states on buttons look normal (highlight on hover, pressed state
   on click) — this exercises `InputSystemUIInputModule`'s pointer handling end-to-end.

## 4. Editor hookup checklist
None required — the throwaway script did all the scene/reference wiring, verified by reading
the scene files back (see Test results). Just run the hand-test above.

## 5. Deviations
- **Asmdef references turned out not to be transitive**: `CaseGame.Editor.asmdef` only
  referenced `CaseGame.Runtime`, and I'd assumed (based on the first pass compiling fine with
  `UnityEngine.UI` types used the same way) that `CaseGame.Runtime`'s own references would
  flow through. They don't — the first compile attempt failed on `TMPro`/
  `UnityEngine.InputSystem` not being found. Fixed by adding `Unity.TextMeshPro` and
  `Unity.InputSystem` to `CaseGame.Editor.asmdef` directly. Logged in `ENVIRONMENT.md` since
  it'll matter for every future editor script that touches a Runtime-only-referenced package.
- No automated tests for `MainMenuController`/`SettingsController` themselves (UI-wiring
  MonoBehaviours) — same reasoning as the first pass and Report 002: their one genuinely
  testable piece of logic (`ResolutionOptionsBuilder`) is fully covered; the rest is
  lifecycle/Editor-GUI-bound and hand-tested instead.
- Everything else matches the plan: TMP throughout, Input System's UI module on `EventSystem`,
  no legacy `UnityEngine.UI.Text` or `StandaloneInputModule` remaining anywhere in the scene.
