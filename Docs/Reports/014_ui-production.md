**2026-07-22 — UI.Production**

## 1. Summary
Implemented the `UI.Production` module (`CaseGame.UI.Production`) — the Production Menu's
"infinite scroll view (object-pooled)" (UX brief section, requirement 15). `ScrollRecycler` is
the actual recycling math: a plain, static C# class that — given how many buildings exist, how
many pooled row slots there are, and how far the view has scrolled — decides which catalog
index each slot should currently show and where. The pool of `ProductionMenuItemView` rows
never grows with the catalog size, which is what makes the list genuinely "infinite"-capable
rather than one row per building. `ProductionMenuController` applies that math to a real
`ScrollRect`; `ProductionMenuItemView` is the pooled row itself — `Bind()` re-labels it for
whichever building it currently represents, and its "produce" click raises a new
`BuildingCatalogEntryEventChannel`.

That channel is the project's first *concrete* `GameEventChannel<T>` payload channel — the
generic base has sat unused since Report 006, specifically waiting for a real payload type to
show up. `PlacementController` now subscribes to it directly and calls its existing
`BeginPlacement`, so UI.Production and Placement stay fully decoupled: neither references the
other, they just share the one channel asset.

The Production Menu lists a new `BuildingCatalog` asset (`CaseGame.Buildings` — a list of
`BuildingCatalogEntry`, each a `BuildingDefinition` + prefab pair) **generically** — no
per-building-type branch exists anywhere in this code, satisfying requirement 2's modularity
mandate. A designer adds a new producible building by adding one entry to that asset.

Building the actual scroll view UI (Canvas, EventSystem, `ScrollRect`/`Viewport`/`Content`/
scrollbars, the item prefab) by hand would have been well over the "10+ fiddly steps" threshold
in CLAUDE.md's editor script policy, so I used a throwaway setup script — announced what it
would do, ran it, verified the result by reading the generated `.unity`/`.prefab`/`.asset`
files back (cross-checked every GUID it wrote against the real asset `.meta` files), and
deleted it in the same turn.

**Scope boundary:** `PlacementController` is *not* added to `Gameplay.unity` yet — it has
nothing to be initialized with (no `GridModel`/`BuildingFactory` bootstrap exists in the scene),
and building that bootstrap now would have front-run Gameplay scene assembly's actual job. So
the Production Menu's "produce" click can't be hand-tested end-to-end today; everything in this
feature is covered by automated tests instead, and the wiring itself was verified by reading
the generated scene/asset files back.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalogEntry.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalogEntry.cs) — `BuildingDefinition` + prefab pair.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalog.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalog.cs) — SO listing producible buildings.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalogEntryEventChannel.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingCatalogEntryEventChannel.cs) — concrete `GameEventChannel<BuildingCatalogEntry>`.
- [`Assets/_Project/Scripts/Runtime/UI/Production/ScrollRecycler.cs`](Assets/_Project/Scripts/Runtime/UI/Production/ScrollRecycler.cs) — the pooled-recycling math, plain C#.
- [`Assets/_Project/Scripts/Runtime/UI/Production/ProductionMenuItemView.cs`](Assets/_Project/Scripts/Runtime/UI/Production/ProductionMenuItemView.cs) — pooled row View.
- [`Assets/_Project/Scripts/Runtime/UI/Production/ProductionMenuController.cs`](Assets/_Project/Scripts/Runtime/UI/Production/ProductionMenuController.cs) — Controller wiring `ScrollRecycler` to a real `ScrollRect`/`PrefabPool`.
- [`Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs`](Assets/_Project/Scripts/Runtime/Placement/PlacementController.cs) — subscribes to `BuildingCatalogEntryEventChannel`, calls `BeginPlacement` on receipt.
- [`Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef`](Assets/_Project/Scripts/Editor/CaseGame.Editor.asmdef) — added `UnityEngine.UI` reference (needed by the now-deleted setup script; asmdef references aren't transitive).
- [`Assets/_Project/Scripts/Tests/EditMode/CaseGame.Tests.EditMode.asmdef`](Assets/_Project/Scripts/Tests/EditMode/CaseGame.Tests.EditMode.asmdef) — added `UnityEngine.UI`/`Unity.TextMeshPro` references for the new UI tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingCatalogTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingCatalogTests.cs) — 1 test.
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Production/ScrollRecyclerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Production/ScrollRecyclerTests.cs) — 8 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Production/ProductionMenuItemViewTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Production/ProductionMenuItemViewTests.cs) — 3 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/UI/Production/ProductionMenuControllerTests.cs`](Assets/_Project/Scripts/Tests/EditMode/UI/Production/ProductionMenuControllerTests.cs) — 4 tests.
- **Scene/prefab/assets** (built + verified by a throwaway `Scripts/Editor/Setup/Temp/ProductionMenuSetup.cs`, deleted after use): `Assets/_Project/Scenes/Gameplay.unity` — added `--- UI ---` organizer with `Canvas`/`EventSystem`/the `ProductionMenu` scroll view; `Assets/_Project/Prefabs/UI/ProductionMenuItem.prefab` (new); `Assets/_Project/ScriptableObjects/BuildingCatalog_Default.asset` (new, populated with Barracks + Power Plant); `Assets/_Project/ScriptableObjects/BuildingCatalogEntryEvent_Default.asset` (new).
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map rows (Buildings, Placement, UI.Production), implementation log entry, scene/prefab composition update, decisions log entries #31–35.
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — new row for UI prefab naming (no category prefix) and root-level config SO asset naming.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B batchmode + Mode A `dotnet build`, editor
closed for both runs): **passed** — 0 `error CS` lines in either.

EditMode tests (Mode B batchmode, `-runTests -testPlatform EditMode`): **96/96 passed** — the 80
pre-existing plus 16 new:
- `BuildingCatalogTests` (1): `Entries` reflects the serialized list.
- `ScrollRecyclerTests` (8): zero-offset assigns slots to the first items in order; a scrolled
  offset shifts which indices are assigned; fewer items than slots only assigns what exists;
  scrolling past the end clamps to the last reachable window instead of running off the list;
  zero item/slot count yields nothing; content-height math.
- `ProductionMenuItemViewTests` (3): `Bind` sets the icon sprite and name text from the
  definition; `RequestProduce` raises the channel with the currently bound entry; raising with
  no channel assigned doesn't throw.
- `ProductionMenuControllerTests` (4): item count below pool size builds one slot per item; item
  count above pool size caps slots at pool size (the actual "doesn't grow with catalog size"
  behavior); `RefreshLayout` binds the first N slots and sizes the content correctly; a scrolled
  offset rebinds slots to the correct window.

Consistent with established precedent (`ENVIRONMENT.md`'s note that `Awake`/`OnEnable` don't
reliably fire on `AddComponent`-created objects in this batchmode EditMode runner):
`ProductionMenuController.Initialize()` is exposed as a public, idempotent method — called from
`Awake()` at runtime, called directly by tests — mirroring `PlacementController.Initialize`'s
existing pattern (decision #30). The `ScrollRect.onValueChanged`/button-click wiring
(`OnEnable`-registered) is **not** automated-tested, same precedent as MainMenu/Settings —
hand-test once Gameplay scene assembly makes end-to-end testing possible.

## 4. Editor hookup checklist
None required — the throwaway setup script did the scene/prefab/asset wiring, and I verified
the result directly (grepped `Gameplay.unity`, `ProductionMenuItem.prefab`, and
`BuildingCatalog_Default.asset` back and cross-checked every GUID against the real asset `.meta`
files, rather than trusting the script's clean exit code alone). Two things worth knowing:

1. **The Production Menu can't be hand-tested yet.** Clicking "produce" on a row raises the
   event channel correctly (verified by test), but nothing in the scene is listening yet —
   `PlacementController` isn't in `Gameplay.unity`. That lands with Gameplay scene assembly.
2. **`ScrollRect`'s scroll-direction sign convention is assumed, not visually verified.**
   `ProductionMenuController` reads `Mathf.Abs(content.anchoredPosition.y)` as the scroll
   offset — correct for a standard top-anchored `Content` (Unity's default for a vertical
   `ScrollRect`, which is what `DefaultControls.CreateScrollView` produced here), and the
   `Abs()` is a deliberate hedge against the sign convention either way. I can't interactively
   scroll it myself to confirm rows recycle smoothly and don't visually "pop." Once Gameplay
   scene assembly makes the Production Menu reachable in Play Mode, please scroll it with more
   than 8 catalog entries (temporarily duplicate a `BuildingCatalog_Default.asset` entry or two
   to get past the pool size) and confirm rows rebind cleanly with no flicker/gap — if the
   recycling looks off, it's almost certainly this sign assumption, not the underlying
   `ScrollRecycler` math (which is fully unit-tested).

## 5. Deviations
- Added `BuildingCatalogEntry(BuildingDefinition, BuildingBase)` as a public constructor
  (beyond just serialized fields) — needed for tests to construct entries directly without a
  `SerializedObject` detour, and a legitimate real API besides (Editor tooling/future code may
  want to build entries programmatically too). Small, motivated by testability, not scope creep.
- `ProductionMenuController.ActiveSlots` (read-only) and `ApplyScrollOffset`/`Initialize` are
  public beyond the minimal MonoBehaviour surface, mirroring `PlacementController.CurrentGhost`/
  `Initialize`'s existing precedent (Report 013) for the same reason: testability without
  reflection or lifecycle dependence.
- Added `UnityEngine.UI` to `CaseGame.Editor.asmdef` (needed by the throwaway setup script,
  now deleted) — left in place since it's a legitimate, likely-reused reference for future
  Editor tooling that touches UI prefabs, not a leftover.
