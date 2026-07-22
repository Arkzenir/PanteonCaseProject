**2026-07-22 — Building events rearchitecture**

## 1. Summary
Human-directed correction to Report 020's "Remove Building" design. The required event shape,
stated explicitly: Buildings — Creation → Event, Removal → Event, Death → Handled by health;
Units — Creation → Event, Death → Handled by health. Three independent triggers for Buildings,
not two.

Auditing the current code against that taxonomy: **Creation** was already event-driven for both
Buildings and Units (the Production Menu/Info Panel raise a request channel; `PlacementController`/
`UnitProductionController` subscribe and call their Factory) — no change needed there.
**Death** already goes through `ApplyDamage` → `Health` → the death callback — no change needed
there either. **Removal** was the one gap: Report 020 implemented it as
`building.ApplyDamage(building.MaxHealth)`, deliberately reusing the death pipeline (decision
#53) — which is exactly the coupling this taxonomy rules out.

Fixed by adding a dedicated `BuildingRemovalRequestedEventChannel` (`CaseGame.Buildings`,
carrying a `BuildingBase`). `InfoPanelController.RequestRemoveBuilding` now raises it instead of
calling `ApplyDamage` — manual removal no longer touches Health at all.
`PlacementController` subscribes and performs the actual removal directly: it already owns
"commit a building to the grid," so it's the natural owner of "uncommit" too. `BuildingBase`
gained a public `ReleaseFootprint()` — the same footprint-freeing logic `OnEntityDied()` already
had, now callable directly by removal instead of only reachable via the death hook.
`BuildingFactory.Release` dropped its `prefab` parameter (it now records each instance's own
pool at `Create` time and looks it up internally) since manual removal, unlike an in-progress
placement ghost, has no "current prefab" lying around to pass in.

Removal no longer sets `IsDead` the way combat death does, which quietly broke
`SelectionController`'s existing lazy staleness guard for exactly the "selected building was
removed" case it used to cover (its own doc comment said so explicitly). Closed the gap with the
same mechanism this feature already introduces: `SelectionController` also subscribes to the new
channel and proactively clears the selection if the removed building was the one selected,
rather than waiting for the next click to (no longer correctly) notice.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingRemovalRequestedEventChannel.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingRemovalRequestedEventChannel.cs) — new. `GameEventChannel<BuildingBase>` carrying a removal request.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingFactory.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingFactory.cs) — `Release(BuildingBase prefab, BuildingBase instance)` → `Release(BuildingBase instance)`; records each instance's pool at `Create` time instead of requiring the caller to pass the prefab back.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs) — extracted `OnEntityDied()`'s footprint-release logic into a new public `ReleaseFootprint()`; `OnEntityDied()` now just calls it (combat-only path).
- [`Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs`](Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs) — new `removalRequestedChannel` field, subscribes to it, new public `RemoveBuilding(BuildingBase)` (frees footprint + releases to pool, no Health involvement); `CancelPlacement` simplified to `_factory.Release(_ghostInstance)`, dropping the now-unneeded `_currentPrefab` field.
- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — new `removalRequestedChannel` field, subscribes to it, new public `HandleBuildingRemoved(BuildingBase)` clearing the selection if it matches; `SetSelectedBuilding`'s existing `IsDead` staleness-guard comment updated to reflect it now only covers combat death.
- [`Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs`](Assets/_Project/Scripts/Runtime/UI/Info/InfoPanelController.cs) — new `removalRequestedChannel` field; `RequestRemoveBuilding` raises it instead of calling `ApplyDamage`.
- Tests updated/added: `BuildingFactoryTests`, `BuildingBaseTests`, `PlacementControllerTests`, `SelectionControllerTests`, `InfoPanelControllerTests` — see §3.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Buildings, Placement, Selection, UI.Info), implementation log entry, decisions log #55 (supersedes #53's ApplyDamage-reuse design; #53 itself left intact as an accurate record of what Report 020 did and why).
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

No prefab changes. One new ScriptableObject asset needed (event channel) — see §4.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines.

EditMode tests (Mode B batchmode): **160/160 passed** — the 153 pre-existing plus 7 new:
- `BuildingFactoryTests` (+1): `Release` called directly (not via death) returns the instance to
  its pool and a subsequent `Create` reuses it, still alive (`IsDead` false).
- `BuildingBaseTests` (+2): `ReleaseFootprint` called directly frees the grid and clears
  `FootprintOrigin` without affecting `IsDead`; calling it on a never-placed building doesn't
  throw.
- `PlacementControllerTests` (+2): `RemoveBuilding` on a committed instance frees its grid area
  without killing it; `RemoveBuilding(null)` doesn't throw.
- `SelectionControllerTests` (+2): `HandleBuildingRemoved` clears the selection (and raises
  null) when the removed building was selected; leaves the selection untouched when it wasn't.
- `InfoPanelControllerTests` (1 replaced): `RequestRemoveBuilding_KillsTheSelectedBuilding` →
  `RequestRemoveBuilding_RaisesRemovalChannelWithTheSelectedBuildingWithoutKillingIt` — asserts
  the channel is raised with the building **and** that `IsDead` stays false, locking in the
  "doesn't touch Health" behavior this feature is about.

The existing combat-death regression test
(`HandleLeftClick_PreviouslySelectedBuildingDiedElsewhere_ReselectingSameInstanceStillRaisesEvent`,
Report 020) needed no change — it uses `ApplyDamage` directly, a path this feature left
untouched, and still passes.

## 4. Editor hookup checklist
One new event channel asset, then three fields to wire (all `Gameplay.unity`):

1. In the Project window, go to `Assets/_Project/ScriptableObjects/`. Right-click → **Create →
   CaseGame → Events → Building Removal Request Event**. Rename the created asset
   `BuildingRemovalRequestEvent_Default` (matches the existing `<Type>Event_Default.asset`
   naming convention for the other three channels already there).
2. In `Gameplay.unity`, select `--- SYSTEMS --- / PlacementController`. Drag
   `BuildingRemovalRequestEvent_Default.asset` into its new **Removal Requested Channel** field.
3. Select `--- SYSTEMS --- / SelectionController`. Drag the **same** asset into its new
   **Removal Requested Channel** field.
4. Select `--- UI --- / InformationPanel` (the `InfoPanelController`). Drag the same asset into
   its new **Removal Requested Channel** field.

All three must reference the exact same asset — that's what connects them, no direct references
between the three controllers/panel exist or are needed.

Hand-test after wiring: place a building, select it, click Remove — it should disappear and the
grid cells free up (placeable again) without the Info Panel or Selection referencing a
now-inactive instance. This is the same behavior Report 020's version had; this feature changes
*how* it's wired internally, not the player-visible result.

## 5. Deviations
- None beyond the correction itself, which the human explicitly directed. `BuildingFactory`'s
  `Release` signature change (dropping the `prefab` parameter) was a necessary consequence of
  supporting removal of an arbitrary already-placed building (which, unlike an in-progress
  placement ghost, has no "current prefab" tracked anywhere) — not a speculative refactor.
