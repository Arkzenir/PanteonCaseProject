2026-07-23 — UI pack-art reskin

## 1. Summary

Reworked every plain flat-color UI `Image` (no sprite, just a tinted rectangle) across the Main
Menu family and the Gameplay HUD panels into properly-art-directed elements using the Tiny Swords
"UI Elements" pack, per explicit request. Asset choices were made by actually viewing the source
PNGs (via the Read tool) rather than guessing, and by trusting the pack's own shipped 9-slice
`spriteBorder` import data rather than estimating borders blind — the same caution this project
applied when it deliberately avoided guessing the grass/cliff autotile sheet (decisions log #65),
just now backed by the ability to actually look at the art first. Mapping used:

- **Main Menu family backgrounds** (`MainPanel`, `SettingsPanel`, `HowToPlayPanel`) →
  `SpecialPaper.png` — a dark navy parchment panel with gold corner filigree, reads as a proper
  title-screen backdrop.
- **Main Menu family buttons** (Play, How To Play, Settings, Apply, both Back buttons) →
  `BigBlueButton_Regular.png` — a riveted-frame teal button.
- **Gameplay HUD panel backgrounds** (`ProductionMenu`, `InformationPanel`'s `PanelContent`) →
  `RegularPaper.png` — a lighter parchment, reads better as an in-play HUD sitting over the board
  than the darker Main Menu panel would.
- **Section header banners** (`ProductionMenu`'s `Banner`, `InformationPanel`'s
  `BannerBackground`) → `Banner.png` — a scroll-shaped banner behind the "Production"/"Information"
  text.
- **Production Menu list rows** (`ProductionMenuItem`, currently an 8%-alpha near-invisible box) →
  `Banner_Slots.png` — a worn-parchment slot texture.
- **Remove Building button** → `BigRedButton_Regular.png` — destructive-action red, replacing the
  old manual red color tint on a generic sprite.

A new `SpriteAtlas_UI.spriteatlas` packs the source `Papers`/`Buttons`/`Banners` folders
(folder-level packing, mirrors decision #50's rationale for `SpriteAtlas_Gameplay`), so any future
art added to those folders is picked up automatically. All 13 reskinned elements were wired via a
throwaway editor script and individually verified by reading each prefab's regenerated
`m_Sprite`/`m_Type`/`m_Color` back before the script was deleted.

**Deliberately not touched:** the 3 runtime-bound content `Image`s (`ProductionMenuItem`'s `Icon`,
`ProducibleUnitIcon`'s `Image`, `InformationPanel`'s `BuildingIcon`) — these get their sprite set
by code from the selected building/unit's own art and must stay untouched. Also not scripted: the
Settings screen's `ResolutionDropdown`/`FullscreenToggle` internal TMP sub-elements (arrow,
checkbox, list template) — these are fiddly, deeply-nested, auto-generated sub-objects better
adjusted by hand with live Inspector feedback than blind-scripted; see the checklist below.

## 2. Changes

- `Assets/_Project/Prefabs/UI/MainPanel.prefab` — `MainPanel` (bg), `PlayButton`,
  `HowToPlayButton`, `SettingsButton` reskinned.
- `Assets/_Project/Prefabs/UI/SettingsPanel.prefab` — `SettingsPanel` (bg), `ApplyButton`,
  `BackButton` reskinned.
- `Assets/_Project/Prefabs/UI/HowToPlayPanel.prefab` — `HowToPlayPanel` (bg), `BackButton`
  reskinned.
- `Assets/_Project/Prefabs/UI/ProductionMenu.prefab` — root panel (bg) and `Banner` reskinned.
- `Assets/_Project/Prefabs/UI/ProductionMenuItem.prefab` — root row background reskinned.
- `Assets/_Project/Prefabs/UI/InformationPanel.prefab` — `PanelContent` (bg), `BannerBackground`,
  `RemoveButton` reskinned.
- `Assets/_Project/Art/Textures/SpriteAtlas_UI.spriteatlas` (new) — packs
  `Assets/Tiny Swords/UI Elements/{Papers,Buttons,Banners}`.
- `Docs/Agent/ARCHITECTURE.md` — UI.Production/UI.Info/UI.MainMenu/UI.Settings module rows
  updated, new asset noted in §4, decisions log entry #75.
- `Docs/Agent/CURRENT_STATUS.md` — "Last report" pointer and "Done" list updated.
- (Throwaway, deleted after verification) `Scripts/Editor/Setup/Temp/UiPolishSetup.cs`.

## 3. Test results

Not a logic feature — no new EditMode tests (pure asset/prefab wiring, nothing engine-independent
to test). Full batchmode pass after the reskin and after deleting the throwaway script:

**232/232 EditMode tests passing, 0 compile errors** — no regressions.

**Hand-test (please do this before treating the reskin as final — it's real art now, but I
can't render a screenshot from this environment to confirm the on-screen result myself):**
open `MainMenu.unity` and `Gameplay.unity` in Play Mode and look at each reskinned panel/button
for anything that reads wrong (stretching, muddy contrast, text illegible against the new
background, etc.). Everything below is wired correctly at the data level (verified by reading
the prefab YAML back) — what's unverified is purely how it *looks*, since this environment has
no way to render a preview.

## 4. Editor hookup checklist

Longer than usual, per your explicit request — this pass intentionally left the fiddliest,
lowest-confidence pieces for you to finish by hand rather than script them blind.

### A. Quick visual confirmation of the scripted reskin

1. Open `MainMenu.unity`. Select `MainPanel` in the Hierarchy — confirm its background now shows
   the dark, gold-filigreed `SpecialPaper` art (not a flat dark rectangle), and that
   `PlayButton`/`SettingsButton` show the teal riveted-frame button art with their existing "Play"/
   "Settings" text still legible on top.
2. Click the "How to Play" button in Play Mode (or select `HowToPlayPanel` directly) — confirm the
   same `SpecialPaper` background and a `BigBlueButton_Regular`-styled Back button.
3. Click "Settings" (or select `SettingsPanel` directly) — confirm the same background, and that
   `ApplyButton`/`BackButton` show the button art. **Don't worry about the Resolution
   Dropdown/Fullscreen Toggle yet** — that's section B below.
4. Open `Gameplay.unity`. Select `ProductionMenu` — confirm the parchment (`RegularPaper`)
   background and the scroll-shaped `Banner` behind the "Production" header text. Enter Play Mode
   and confirm individual list rows now show the `Banner_Slots` parchment-scrap texture instead of
   a barely-visible box.
5. Select a building on the board to open the Information Panel — confirm the same `RegularPaper`
   background, `Banner`-styled "Information" header, and that the Remove Building button now shows
   red button art (not a plain red rectangle).

### B. Hand-wire the Settings screen's Dropdown/Toggle internals

These are TMP's auto-generated sub-hierarchies (`TMP_DefaultControls.CreateDropdown`/
`CreateToggle`, per `ENVIRONMENT.md`) — several small nested objects, best set with the Inspector
open and the result visible live, rather than scripted blind. All target sprites are already
imported and available in the Project window (Tiny Swords/UI Elements/...) — this is pure
drag-and-drop, no new import settings needed.

6. In `SettingsPanel.prefab`, find `ResolutionDropdown`. Select its `Background` child (or the
   `ResolutionDropdown` object itself if the Image sits there directly) → drag
   `Banner_Slots.png` into the Image's `Sprite` field, set `Image Type` to `Sliced`. This gives the
   dropdown's closed-state field a textured look instead of a flat dark-grey box.
7. Expand `ResolutionDropdown > Template` → select `Template` itself → drag `RegularPaper.png`
   into its Image's `Sprite` field, `Image Type` = `Sliced`. This is the panel that drops down when
   you click the dropdown open — currently a flat white box.
8. Inside `Template`, find `Item Background` (one level down, under `Item`) → optionally drag
   `Banner_Slots.png` in as well (`Sliced`) so each row in the open dropdown list matches the
   Production Menu's item-row look. This one's low-visibility (only shows while the list is open)
   — skip if you'd rather leave it plain.
9. `ResolutionDropdown`'s `Arrow` child currently has no sprite at all (a small white square as a
   placeholder). **No suitable arrow/chevron glyph exists in the Tiny Swords UI pack** — I checked
   the Icons folder and it's all resource/action icons (hammer, swords, gear, music note), nothing
   arrow-shaped. Simplest fix: select `Arrow` and just darken its `Color` (e.g. a muted grey) so
   it's less of a glaring white square; leave the sprite empty. If you have or want a proper
   chevron asset, that's a 2-minute manual swap once you have one.
10. `FullscreenToggle`'s `Checkmark` child (green tinted box, `Checkmark`/`Item Checkmark` in the
    prefab) has the same problem — no checkmark glyph in the pack. Suggest: select the toggle's
    background/frame element (whichever object is the toggle's outer box, sibling to `Checkmark`)
    and drag `TinySquareBlueButton.png` in as its Image, `Image Type` = `Simple` (this art has no
    9-slice border data, so `Simple` is correct — don't set it to `Sliced`). Leave the green
    checkmark fill as a plain colored box; there's genuinely no better asset for it in this pack.

### C. Optional look-and-feel tuning (your call, not required)

11. The button art (`BigBlueButton_Regular`/`BigRedButton_Regular`) is a roughly square (192×192)
    image with a 64px border on each side, while the buttons using it are wide and short
    (240×60/200×60/RemoveButton's 0×50). `Image.Type.Sliced` handles this gracefully — Unity
    proportionally shrinks the border segments when a rect is smaller than their sum rather than
    producing any visible corruption — but the ornate corner frame will look noticeably thinner
    than the art's "native" appearance. If it looks too compressed once you see it live, bumping
    button heights (e.g. 60 → 90) would let more of the frame art show. I deliberately didn't do
    this myself: the Main Menu's 3 buttons are positioned via fixed `anchoredPosition` values, not
    a `VerticalLayoutGroup`, so changing height without recalculating spacing risks visible overlap
    that I have no way to check without a screenshot — safer to leave sizing to you once you can
    actually see the result.
12. If `SpecialPaper` (Main Menu family) and `RegularPaper` (Gameplay HUD) end up feeling
    mismatched side-by-side, or either background makes the existing white TMP text harder to
    read than before, swapping between `RegularPaper.png`/`SpecialPaper.png` on any given panel is
    a single drag-and-drop — both are already imported with correct 9-slice borders.
13. `Banner_Slots.png` on `ProductionMenuItem` rows is currently opaque (alpha 1) — if the scroll
    list looks too "busy"/heavy with real texture on every row once populated, dropping that
    Image's alpha back down (e.g. 0.85) is a quick tweak.

## 5. Deviations

- Settings screen's Dropdown/Toggle internals intentionally left for hand-wiring (section B above)
  rather than scripted — explained above and in decisions log #75.
- No arrow/chevron or checkmark glyph exists in the Tiny Swords UI pack for the Dropdown arrow or
  Toggle checkmark; both are flagged in the checklist rather than silently left broken.
- Existing button/panel `RectTransform` sizes were left unchanged rather than resized to better
  match the art's native aspect ratio — flagged as an easy, optional follow-up (checklist item 11)
  instead of a blind layout change I couldn't visually verify myself.
