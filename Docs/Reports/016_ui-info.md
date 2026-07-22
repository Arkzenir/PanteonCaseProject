**2026-07-22 — UI.Info**

## 1. Summary
Implemented the `UI.Info` module (`CaseGame.UI.Info`) — the Information Panel, requirement 5:
"Selecting a building on the game board shows its image on the Information Panel. If the
building can produce units, the images of its producible units are also listed there."

`InfoPanelController` subscribes to `SelectedBuildingEventChannel` — the channel
`SelectionController` (Report 015) raises but which had no consumer until now. On a building
selection, the panel shows the building's image and name and spawns one small
`ProducibleUnitIconView` per entry in `BuildingDefinition.ProducibleUnits`. On no selection (or
a soldier selection — Selection clears the building slot whenever soldiers are selected, per
Report 015), the whole panel hides. Power Plant's empty producible-units list means its row is
simply empty when selected — there's no per-building-type branch anywhere in this code
(requirement 6's "no production sub-menu" falls out naturally from the data being empty, the
same generic-iteration discipline already used by the Production Menu).

`ProducibleUnitIconView` is deliberately simpler than `ProductionMenuItemView` — no button, no
event channel — since the Info Panel is purely informational; producing a unit still only
happens through the Production Menu. I also chose *not* to pool these icons (unlike the
Production Menu's rows): the brief ties Object Pooling specifically to the Production Menu's
*infinite* scroll view and to frequently spawned/destroyed gameplay entities, and this list is
always small (≤3 today) and only rebuilds on the rare "selection changed" event — pooling it
would need an arbitrary size guess for a churn rate pooling doesn't meaningfully help.
Instantiate/Destroy per selection change, with the same `Application.isPlaying`-gated
`Destroy`/`DestroyImmediate` split `GameManager` already established (Report 002/003) for
correct behavior in both Play Mode and EditMode tests.

Built the actual panel into `Gameplay.unity` via a throwaway setup script — same justification
as Report 014's (hand-building this hierarchy plus its supporting prefab/asset is well past 10
steps) — right-anchored under the existing Canvas, mirroring the Production Menu's
left-anchored strip, matching the brief mockup's Production Menu | Game Board | Information
Panel layout.

**Scope boundary:** same as Reports 014/015 — `SelectionController` still isn't added to
`Gameplay.unity` (no bootstrap exists), so the panel can't be hand-tested end-to-end by actually
clicking a building yet. Its logic is fully covered by tests instead, and the scene wiring was
verified by reading the generated files back rather than an interactive session.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/UI/Info/ProducibleUnitIconView.cs`](Assets/_Project/Scripts/Runtime/UI/Info/ProducibleUnitIconView.cs) — small non-interactive icon View.
- [`Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs`](Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs) — the Controller: subscribes to the channel, testable `SetSelectedBuilding(BuildingBase)`.
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Info/ProducibleUnitIconViewTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Info/ProducibleUnitIconViewTests.cs) — 2 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Info/InfoPanelControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Info/InfoPanelControllerTests.cs) — 5 tests.
- **Scene/prefab/assets** (built + verified by a throwaway `Scripts/Editor/Setup/Temp/InfoPanelSetup.cs`, deleted after use): `Assets/_Project/Scenes/Gameplay.unity` — added `InformationPanel` (right-anchored, starts inactive) under the existing `--- UI ---` Canvas; `Assets/_Project/Prefabs/UI/ProducibleUnitIcon.prefab` (new); `Assets/_Project/ScriptableObjects/SelectedBuildingEvent_Default.asset` (new).
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map row (UI.Info), implementation log entry, scene/prefab composition update, decisions log entry #40.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (Mode A `dotnet build` + Mode B Unity batchmode, editor closed for both): **passed**
— 0 `error CS` lines in either.

EditMode tests (Mode B batchmode): **120/120 passed** — the 113 pre-existing plus 7 new:
- `ProducibleUnitIconViewTests` (2): `Bind` sets the icon sprite and name text from the unit
  definition; binding with no name text assigned doesn't throw.
- `InfoPanelControllerTests` (5): a non-null building activates the panel and sets icon/name; a
  null building deactivates it; a building with no producible units spawns no icons (Power
  Plant's case); a building with 2 producible units spawns exactly 2 correctly-bound icons;
  switching to a different building replaces the old icons (both the tracked list and the
  actual container child count go back to 0 first — verifies the `DestroyImmediate` cleanup
  path actually runs synchronously in Edit Mode, not just that the list was cleared).

Consistent with established precedent: the channel subscription itself (`OnEnable`/`OnDisable`)
isn't automated-tested — same reasoning as `PlacementController`'s produce-request subscription
in Report 014 (`Awake`/`OnEnable` don't reliably fire on `AddComponent`-created objects in this
batchmode EditMode runner). `SetSelectedBuilding` is tested directly instead, and is exactly
what the subscription forwards to.

## 4. Editor hookup checklist
None required — the throwaway setup script did the scene/prefab/asset wiring, and I verified
the result directly (grepped `Gameplay.unity`/`ProducibleUnitIcon.prefab`/
`SelectedBuildingEvent_Default.asset` back and cross-checked every GUID against the real asset
`.meta` files). One thing worth knowing: the panel can't be hand-tested by actually selecting a
building yet, since `SelectionController` — the thing that would raise
`SelectedBuildingEvent_Default.asset` for real — isn't in any scene. Once Gameplay scene
assembly adds it, the panel should already be correctly wired to receive it (same asset,
already assigned on both sides — `InfoPanelController.selectedBuildingChannel` today, and
`SelectionController.selectedBuildingChannel` needs the same asset dragged in when it's added).

## 5. Deviations
- Producible-unit icons are not pooled (decision #40) — explained in Summary; the brief's
  Object Pooling mandate is specifically tied to the Production Menu's infinite list and to
  frequently spawned/destroyed gameplay entities, neither of which applies to this small,
  rarely-rebuilt list.
