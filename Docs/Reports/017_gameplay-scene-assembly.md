**2026-07-22 — Gameplay scene assembly**

## 1. Summary
This is the integration feature — wiring every previously-standalone, already-tested module
into a live `Gameplay.unity`. Per your instruction, I started with a full audit of the existing
systems before touching the scene, rather than just wiring what already existed. That audit
found real gaps, not just loose ends to tie off:

**Units were never actually producible.** The Production Menu (Report 014) only ever lists
*buildings* — confirmed by re-reading `BuildingCatalog`'s actual content and how it's iterated.
The Info Panel's producible-unit icons (Report 016) were deliberately built non-interactive,
reasoning that "producing still only happens through the Production Menu." Putting those two
facts together while assembling the full scene: there was **no path to ever produce a soldier**.
Re-reading requirements 5 and 6 together resolved the ambiguity — GI-6's "Power Plant needs no
production sub-menu" only makes sense if the Info Panel's producible-unit row *is* the
sub-menu requirement 6 is talking about. So Report 016's design choice was wrong, not just
incomplete. Fixed this feature:
- `BuildingDefinition.ProducibleUnits` is now `List<UnitCatalogEntry>` (a `UnitDefinition` +
  `SoldierBase` prefab pair, mirroring `BuildingCatalogEntry`) instead of `List<UnitDefinition>`
  — nothing could previously answer "which prefab do I spawn for this producible unit." Safe to
  change: both Barracks' and Power Plant's `producibleUnits` were still empty (`[]`) when I
  checked, so no authored data existed to lose.
- `BuildingBase` gained a `virtual SpawnPosition` (`Barracks` overrides it with its dedicated
  spawn point; everything else defaults to its own `transform.position`) — so "where do this
  building's products spawn" needs no `is this a Barracks` type-check, consistent with the
  project's existing no-per-type-branch discipline.
- `ProducibleUnitIconView` is now clickable, raising a new `UnitProductionRequestEventChannel`
  (`CaseGame.Units`) carrying a `UnitProductionRequest` (the catalog entry + spawn position,
  bundled at bind time by `InfoPanelController` — this avoids `UnitProductionController` needing
  a reference to `SelectionController`, which would create a circular module dependency since
  `Selection` already depends on `Units`).
- New `UnitProductionController` subscribes and does the actual `UnitFactory.Create` + position.

**Two pre-existing data bugs**, found by reading the actual asset YAML rather than trusting it:
- `BuildingDef_Barracks.asset`/`BuildingDef_PowerPlant.asset` still had a stale `buildingName`
  field — a holdover from before Report 010 renamed it to `entityName` on the shared
  `GameEntityDefinition` base. `entityName` itself had been sitting **empty** ever since. Fixed
  to "Barracks"/"Power Plant" — otherwise the Production Menu and Info Panel would show blank
  names.
- `UnitDef_Soldier3.asset` had `maxHealth: 1`, `attackDamage: 1` — the brief's requirement 9 is
  explicit: all three soldiers have 10 HP, Soldier 3 does 2 damage. Fixed. (Also gave
  Soldier 1/2/3 real display names — `entityName` was populated but held the literal asset
  filename, e.g. "UnitDef_Soldier1", not "Soldier 1".)

**UI-vs-world input conflict.** Neither `PlacementController` nor `SelectionController` checked
whether the pointer was over a UI element before acting on raw mouse input — meaning clicking a
Production Menu row would *also* fire a world-space commit/cancel or select/deselect at
whatever rendered underneath that screen position. This was latent until this feature (no
controller reading raw pointer input previously coexisted with clickable UI in the same live
scene). Fixed both `Update()`s with the standard `EventSystem.current.IsPointerOverGameObject()`
guard.

**The actual wiring:** new `CaseGame.Gameplay` module (`GameplayBootstrap` — the scene's
composition root; not put in `CaseGame.Core` because Core is meant to stay generic/reusable
across scenes, see decisions log #45). It builds `BuildingFactory`/`UnitFactory` with real
`--- GAMEPLAY ---` container Transforms and calls `PlacementController`/`SelectionController`/
`UnitProductionController`'s `Initialize` from `Start()` — guaranteed to run after every
object's `Awake` in the scene, sidestepping the Awake-ordering hazard those controllers were
built to avoid from day one. All three controllers now live under a new `--- SYSTEMS ---`
organizer, fully wired to the real event channel assets from Reports 014–016. Camera
repositioned/resized to frame the grid within the screen strip the Production Menu/Info Panel
don't occlude — **computed via formula, not visually verified** (I cannot enter Play Mode).

I did **not** touch draw-call/batching or the Windows build — those stay separate, later
roadmap items. This report is the integration pass and the bugs it surfaced, not more.

## 2. Changes

**New runtime code:**
- [`Assets/_Project/Scripts/Runtime/Units/UnitCatalogEntry.cs`](Assets/_Project/Scripts/Runtime/Units/UnitCatalogEntry.cs) — `UnitDefinition` + prefab pair.
- [`Assets/_Project/Scripts/Runtime/Units/UnitProductionRequest.cs`](Assets/_Project/Scripts/Runtime/Units/UnitProductionRequest.cs) — "produce this, here" payload.
- [`Assets/_Project/Scripts/Runtime/Units/UnitProductionRequestEventChannel.cs`](Assets/_Project/Scripts/Runtime/Units/UnitProductionRequestEventChannel.cs) — concrete channel.
- [`Assets/_Project/Scripts/Runtime/Units/UnitProductionController.cs`](Assets/_Project/Scripts/Runtime/Units/UnitProductionController.cs) — subscribes, spawns via `UnitFactory`.
- [`Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs`](Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs) — scene composition root (new `CaseGame.Gameplay` module).

**Modified runtime code:**
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingDefinition.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingDefinition.cs) — `ProducibleUnits` type change.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs) — added virtual `SpawnPosition`.
- [`Assets/_Project/Scripts/Runtime/Buildings/Barracks.cs`](Assets/_Project/Scripts/Runtime/Buildings/Barracks.cs) — `SpawnPosition` is now an override.
- [`Assets/_Project/Scripts/Runtime/UI/Info/ProducibleUnitIconView.cs`](Assets/_Project/Scripts/Runtime/UI/Info/ProducibleUnitIconView.cs) — now clickable.
- [`Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs`](Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs) — passes entry + spawn position to each icon.
- [`Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs`](Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs) — `IsPointerOverGameObject` guard.
- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — `IsPointerOverGameObject` guard.

**Tests (new/updated for the data-shape changes above):**
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs) — +1 (`SpawnPosition` default).
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BarracksTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BarracksTests.cs) — new, 2 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingDefinitionTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingDefinitionTests.cs) — updated for `UnitCatalogEntry`.
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Info/ProducibleUnitIconViewTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Info/ProducibleUnitIconViewTests.cs) — updated + 2 new (click/no-channel).
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Info/InfoPanelControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Info/InfoPanelControllerTests.cs) — updated for `UnitCatalogEntry`.
- [`Assets/_Project/Scripts/Tests/EditMode/Units/UnitProductionControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Units/UnitProductionControllerTests.cs) — new, 2 tests.

**Scene/prefab/asset changes** (built + fixed by a throwaway `Scripts/Editor/Setup/Temp/GameplaySceneAssemblySetup.cs`, deleted after use — every change below verified by reading the generated file back, see §4):
- `Assets/_Project/Scenes/Gameplay.unity` — added `--- SYSTEMS ---` (`PlacementController`,
  `SelectionController`, `UnitProductionController`, `GameplayBootstrap`, all wired) and
  `--- GAMEPLAY ---` (`Buildings`/`Units` containers); repositioned/resized the camera.
- `Assets/_Project/Prefabs/UI/ProducibleUnitIcon.prefab` — rebuilt with a `Button` + wired to the new channel.
- `Assets/_Project/ScriptableObjects/UnitProductionRequestEvent_Default.asset` — new.
- `Assets/_Project/ScriptableObjects/GameEntityDefs/Buildings/BuildingDef_Barracks.asset` — `entityName` fixed; `producibleUnits` populated (Soldier 1/2/3).
- `Assets/_Project/ScriptableObjects/GameEntityDefs/Buildings/BuildingDef_PowerPlant.asset` — `entityName` fixed.
- `Assets/_Project/ScriptableObjects/GameEntityDefs/Units/UnitDef_Soldier1.asset` — `entityName` fixed ("Soldier 1").
- `Assets/_Project/ScriptableObjects/GameEntityDefs/Units/UnitDef_Soldier2.asset` — `entityName` fixed ("Soldier 2").
- `Assets/_Project/ScriptableObjects/GameEntityDefs/Units/UnitDef_Soldier3.asset` — `entityName` fixed ("Soldier 3"); `maxHealth` 1→10, `attackDamage` 1→2.

**Docs:**
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Buildings, Units, Placement, Selection, new Gameplay row, UI.Info), full scene composition rewrite, implementation log entry, decisions log #41–48.
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — added `Gameplay/` to the folder list.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — rewritten for this report.

## 3. Test results
Compile check (Mode A `dotnet build` + Mode B Unity batchmode, editor closed for both, run
after every round of code changes including the input-guard fix): **passed** — 0 `error CS`
lines throughout.

EditMode tests (Mode B batchmode): **127/127 passed** — the 120 pre-existing plus 7 new
(`BuildingBaseTests` +1, `BarracksTests` +2, `ProducibleUnitIconViewTests` +2,
`UnitProductionControllerTests` +2), run again after the asset-data fixes and again after the
`IsPointerOverGameObject` fix, both times clean.

Not automated (consistent with established precedent — lifecycle/input-bound code, hand-test
instead): `GameplayBootstrap.Start()`'s actual wiring at runtime, the `IsPointerOverGameObject`
guards, and — necessarily, since I cannot enter Play Mode — everything in §4 below.

## 4. Audit checklist
You asked for an audit checklist alongside the throwaway script rather than a plain hookup
list, since a script did the wiring. Two parts: what I already verified by reading files back
(so you can spot-check my work without re-deriving it), and what only a human in Play Mode can
actually confirm.

### Already verified (by the agent, reading generated files back — not just trusting exit codes)
- Every `SerializeField` reference written by the script cross-checked against the real asset's
  own `.meta` GUID (not just "a value got written somewhere"): `PlacementController.gameCamera`
  → Main Camera's `Camera` component; `produceRequestedChannel` →
  `BuildingCatalogEntryEvent_Default.asset`; `SelectionController.gameCamera`/
  `selectedBuildingChannel` → same Main Camera / `SelectedBuildingEvent_Default.asset`;
  `UnitProductionController.produceRequestedChannel` →
  `UnitProductionRequestEvent_Default.asset`; `GameplayBootstrap`'s six fields → the real
  `GridView` component, the two new container Transforms, and the three controller components.
- `GameplaySceneAssemblySetup` re-run was idempotent: exactly one `--- SYSTEMS ---`/
  `--- GAMEPLAY ---` root each, no duplicate controllers, after running it twice (once to add
  the data-bug fixes, verified no drift from the first run).
- `ProducibleUnitIcon.prefab`'s new `Button`/`produceRequestedChannel` fields wired to the
  correct GUIDs.
- `BuildingDef_Barracks.asset`'s `producibleUnits` — 3 entries, each cross-checked against
  `UnitDef_Soldier{1,2,3}.asset` and `Soldier_{1,2,3}.prefab`'s actual GUIDs.
- `entityName`/stat fixes present in the saved asset YAML (not just "the script ran without
  error") — the stale `buildingName` line is confirmed gone (Unity re-serializes the whole
  object on `ApplyModifiedProperties`, dropping unrecognized orphaned fields).
- Compile + 127/127 tests, after every round of changes, editor closed for each authoritative run.

### Needs a human in Play Mode (the agent cannot enter Play Mode — nothing below has been seen rendered)
1. **Camera framing.** Open `Gameplay.unity`, enter Play Mode. Does the grid appear reasonably
   centered in the space between the Production Menu (left) and Information Panel (right), with
   sensible padding? The position/size were computed from `GridDef_Default.asset`'s 20×12 cells
   and the panels' known 360-unit width at the 1920×1080 reference resolution — if it looks off,
   adjust `Main Camera`'s `Transform.position` and `Camera.orthographic size` directly; nothing
   else depends on the exact values.
2. **Produce a building.** Click a row in the Production Menu → a ghost should appear and follow
   the mouse, green over free cells / red over occupied or out-of-bounds ones → left-click
   commits, right-click cancels.
3. **Select the building you just placed.** Click it → it should tint yellow-ish, and the
   Information Panel should appear on the right showing its image, name, and — for Barracks
   specifically — three producible-unit icons (Power Plant should show none).
4. **Produce a unit.** With a Barracks selected, click one of its three unit icons on the
   Information Panel → a soldier should appear at the Barracks' spawn point. Check the spawn
   point's actual position looks sensible relative to the building's art (this was the human's
   own prefab authoring in Reports 009/012 — I haven't touched it, just started actually using
   it for the first time).
5. **Select and command a soldier.** Click the produced soldier → it tints; right-click empty
   ground → it walks there (routing around any buildings in the way); right-click another
   unit/building → it attacks (instant damage, no walk-up — see decisions log #37). Shift-click
   a second soldier to verify multi-select, then right-click to confirm *both* respond.
6. **Click-through check.** With a soldier selected, click directly on a Production Menu row or
   an Information Panel icon — confirm it does *not* also deselect the soldier or otherwise act
   on the world underneath (this is what the `IsPointerOverGameObject` fix targets — worth
   specifically trying to break).
7. **Scroll the Production Menu** if you temporarily add more than 2 entries to
   `BuildingCatalog_Default.asset` (poolSize is 8) — confirm rows recycle smoothly with no
   flicker/gap, per the scroll-direction caveat flagged back in Report 014 (decision #34) that's
   never been visually confirmed either.
8. **Kill something.** Attack a building/soldier down to 0 HP — confirm it actually disappears
   (returns to its pool) rather than lingering.

## 5. Deviations
- Changed `BuildingDefinition.ProducibleUnits`' underlying type (`List<UnitDefinition>` →
  `List<UnitCatalogEntry>`) — a data-shape change to existing, shipped code, justified in
  Summary/decisions log #41. Confirmed safe (no authored data existed on either building asset).
- Made `ProducibleUnitIconView` clickable, reversing Report 016's explicit "purely
  informational" design — this was a bug fix (units were never producible at all), not a style
  change; full reasoning in Summary/decisions log #44.
- Fixed two pre-existing data bugs on definition assets not touched by this feature's own new
  code (`entityName`, `UnitDef_Soldier3`'s stats) — found during the requested audit, explained
  and flagged rather than silently corrected; decisions log #46.
- Camera position/orthographic size changed from Unity's untouched defaults — necessary for the
  scene to be usable at all, computed rather than guessed, explicitly flagged as unverified.
