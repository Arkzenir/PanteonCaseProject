**2026-07-22 — Selection polish**

## 1. Summary
First item off the post-hand-test backlog (item 10, "Selection polish"): replace
`GameEntityBase.SetSelected`'s placeholder color-tint feedback with a real outline.

The tint approach (`spriteRenderer.color = SelectedTint`) was always a stopgap — its own Report
015 doc comment said so explicitly ("no new prefab child is needed"). Now that real (Tiny
Swords) art has replaced placeholders, multiplying a full-color sprite by a yellow tint reads
muddy and ambiguous, the same problem the Placement ghost's desaturate-then-tint shader
(decision #28) was built to avoid for exactly this reason. Applied the same fix here: a new
hand-authored shader, `SpriteSelectionOutline.shader`, draws a solid-color ring only where the
sprite's own alpha is transparent but a neighboring texel (offset by a data-driven
`_OutlineThickness`) is opaque — so it never overlaps the sprite's own pixels and reads clearly
regardless of the underlying art's colors. A new `M_SpriteSelectionOutline.mat` material carries
the tunable `_OutlineColor`/`_OutlineThickness` values.

Each of the 5 `GameEntityBase`-derived prefabs (`Building_Barracks`, `Building_PowerPlant`,
`Soldier_1/2/3`) needed a new `Outline` child SpriteRenderer — same sprite as `Visuals`, sorted
one behind it, disabled by default — wired to a new `GameEntityBase.outlineRenderer` field.
`SetSelected(bool)` now just toggles `outlineRenderer.enabled` instead of touching color;
`Initialize` sets the outline child's sprite to match the definition (so it silhouette-matches
whatever art the pooled instance currently represents) and resets `enabled = false` for pooled
reuse — the same correctness concern as decision #38, just relocated from color to a renderer
flag.

Wiring 5 prefabs by hand (new GameObject + Transform + SpriteRenderer + sprite/material/sorting
assignment + field wiring, per prefab) crossed the "10+ fiddly steps" threshold in CLAUDE.md's
Editor script policy, so it was done via a throwaway `Scripts/Editor/Setup/Temp/` script — same
precedent as Report 013's `VisualsGrayscale` rollout. Verified by reading every regenerated
prefab and the new material back as YAML (not just trusting the batchmode exit code), then
deleted the script per policy.

## 2. Changes
- [`Assets/_Project/Art/Shaders/SpriteSelectionOutline.shader`](Assets/_Project/Art/Shaders/SpriteSelectionOutline.shader) — new. Hand-authored HLSL, same style/includes as `SpriteGrayscaleGhost.shader`; draws an outline ring via neighbor-alpha sampling.
- [`Assets/_Project/Art/Materials/M_SpriteSelectionOutline.mat`](Assets/_Project/Art/Materials/M_SpriteSelectionOutline.mat) — new. `_OutlineColor` (default yellow), `_OutlineThickness` (default 1.5 texels — kept small relative to the Sprite Atlas's 4px packing padding, decision #50, to avoid the neighbor-sampling technique leaking into an adjacent packed sprite), GPU Instancing enabled (matches decision #50's precedent for the ghost material).
- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs`](Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs) — new `outlineRenderer` field; `SetSelected` toggles its `enabled` flag instead of tinting `spriteRenderer.color`; `Initialize` syncs the outline's sprite and resets it disabled; removed the now-unused `SelectedTint` constant and the color-reset it required.
- `Assets/_Project/Prefabs/Buildings/Building_Barracks.prefab`, `Building_PowerPlant.prefab`, `Assets/_Project/Prefabs/Units/Soldier_1/2/3.prefab` — each gained an `Outline` child (SpriteRenderer, `M_SpriteSelectionOutline` material, same sprite as `Visuals`, sorting order one below it, disabled), wired to `outlineRenderer` on the prefab's `GameEntityBase`-derived component.
- [`Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityBaseTests.cs) — `SetSelected`/reset tests rewritten against `outlineRenderer.enabled` instead of `spriteRenderer.color`; added a test that `Initialize` syncs the outline's sprite to the definition.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Entities), §4 prefab composition, implementation log entry, decisions log #56.
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — prefab-structure override row extended to mention the new `Outline` child.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report; backlog item 10 marked done.
- (Throwaway, deleted this turn) `Scripts/Editor/Setup/Temp/AddSelectionOutlineToEntityPrefabs.cs` — created the material and wired all 5 prefabs; `Scripts/Editor/Setup/Temp/` is empty again.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines; shader
imported cleanly (no shader compile errors in the batch log).

EditMode tests (Mode B batchmode): **161/161 passed** — the 160 prior plus 1 net new
(`GameEntityBaseTests`: 3 tests rewritten in place for the outline API, 1 new —
`Initialize_SetsOutlineRendererSpriteToMatchDefinition`).

Visual result (outline thickness/color, atlas-edge cleanliness) is a rendering outcome the agent
cannot verify without Play Mode — **hand-test: select a building and a soldier in Play Mode,
confirm a clean yellow outline appears around each (not overlapping the art, not bleeding into
neighboring packed-atlas sprites) and disappears on deselect.** If the ring looks too thick/thin
or shows atlas-edge artifacts, adjust `M_SpriteSelectionOutline.mat`'s `_OutlineThickness`/
`_OutlineColor` directly in the Inspector — no code change needed, it's fully data-driven.

## 4. Editor hookup checklist
None. All 5 prefabs are already wired (verified by reading the regenerated files back, see
§1/§2) and no scene changes were needed — `SelectionController`/`InfoPanelController` already
call `GameEntityBase.SetSelected`, unchanged by this feature.

## 5. Deviations
- Used a throwaway `Scripts/Editor/Setup/Temp/` script rather than a manual per-prefab checklist,
  per CLAUDE.md's explicit exception for one-off wiring that's large/error-prone by hand (5
  prefabs × several sub-steps each crosses the ~10-step bar). Deleted immediately after the run
  was verified; `Temp/` is empty again.
- Outline thickness/color are tuned defaults (1.5 texels, yellow), not something the agent could
  visually calibrate without Play Mode — flagged above as a hand-test item, adjustable purely via
  the material's Inspector values if it doesn't look right.
