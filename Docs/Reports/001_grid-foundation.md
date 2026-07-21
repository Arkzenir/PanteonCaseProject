**2026-07-21 — Grid Foundation**

## 1. Summary
Implemented the foundational Game Board grid system that every later feature (building
placement, pathfinding, unit movement) will sit on top of. `GridDefinition` is a
ScriptableObject holding fully designer-editable cell size and board extents (no hardcoded
pixel size, per the human's explicit correction during Phase 0). `GridModel` is a plain C#,
engine-independent class providing world↔cell coordinate conversion and per-cell occupancy
tracking (including multi-cell footprints, for buildings later). `GridView` is a thin
MonoBehaviour that builds a `GridModel` from an assigned `GridDefinition` and draws Scene-view
gizmos so the math can be visually verified. This is a foundation-only feature: nothing here
is wired into a scene yet, and no numbered brief requirement is fully satisfied by itself —
it exists to be depended on. I chose not to introduce the brief-mandated Singleton
(`GameManager`) in this feature since nothing yet needs global/cross-scene state; it'll land
when a feature actually requires it (per CLAUDE.md rule 2, no speculative systems).

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/CaseGame.Runtime.asmdef`](Assets/_Project/Scripts/Runtime/CaseGame.Runtime.asmdef) — new Runtime assembly definition (root namespace `CaseGame`).
- [`Assets/_Project/Scripts/Tests/EditMode/CaseGame.Tests.EditMode.asmdef`](Assets/_Project/Scripts/Tests/EditMode/CaseGame.Tests.EditMode.asmdef) — new EditMode test assembly, references Runtime + Unity Test Framework.
- [`Assets/_Project/Scripts/Runtime/Grid/GridDefinition.cs`](Assets/_Project/Scripts/Runtime/Grid/GridDefinition.cs) — ScriptableObject: cell size, columns, rows, world origin, all designer-editable; `OnValidate` clamps to non-degenerate values.
- [`Assets/_Project/Scripts/Runtime/Grid/GridModel.cs`](Assets/_Project/Scripts/Runtime/Grid/GridModel.cs) — plain C# grid data model: `WorldToCell`/`CellToWorld`/`CellCenterToWorld`, `IsInBounds`, `IsOccupied`, `IsAreaFree`/`SetAreaOccupied` for multi-cell footprints.
- [`Assets/_Project/Scripts/Runtime/Grid/GridView.cs`](Assets/_Project/Scripts/Runtime/Grid/GridView.cs) — humble MonoBehaviour: holds a `GridDefinition` reference, exposes a `GridModel` at runtime (`Awake`), draws grid-line gizmos in the Scene view for hand verification.
- [`Assets/_Project/Scripts/Tests/EditMode/Grid/GridModelTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Grid/GridModelTests.cs) — 8 EditMode tests covering coordinate round-tripping, origin offset, bounds checking, occupancy, and footprint placement (free/blocked/out-of-bounds).

## 3. Test results
EditMode tests run via Unity Test Framework in batchmode (`ENVIRONMENT.md` §Tests):
```
8/8 passed, 0 failed, 0 inconclusive, 0 skipped (0.04s)
```
All test cases in `CaseGame.Tests.EditMode.Grid.GridModelTests` passed:
`WorldToCell_And_CellToWorld_RoundTrip`, `WorldToCell_RespectsOrigin`,
`IsInBounds_ReturnsFalse_OutsideGrid`, `IsOccupied_ReturnsTrue_ForOutOfBoundsCell`,
`SetAreaOccupied_MarksEveryCellInFootprint`, `IsAreaFree_FalseWhenAnyCellOccupied`,
`IsAreaFree_FalseWhenFootprintExtendsOutOfBounds`, `IsAreaFree_TrueForUntouchedGrid`.

Full-project batchmode compile (`ENVIRONMENT.md` §Compile check, Mode B, editor closed) also
passed clean: both `CaseGame.Runtime.dll` and `CaseGame.Tests.EditMode.dll` built with zero
`error CS` lines.

`GridView`'s gizmo rendering is presentational and requires the Unity Editor GUI — I never
launch the GUI myself, so this is a hand-test item (see checklist below).

## 4. Editor hookup checklist
None of this is automated (well under the ~10-step throwaway-script threshold in CLAUDE.md's
editor script policy, and each step is simple/low-risk) — please do the following in the
Unity Editor:

1. Open the project in Unity `2021.3.45f2`.
2. In the Project window, go to `Assets/_Project/Settings/` and right-click → **Create →
   CaseGame → Grid → Grid Definition**. Name it `GridDef_Default` (follows the
   `GridDef_<Name>` naming convention). This is the tunable data asset `GridView` will read.
3. Select `GridDef_Default` and set placeholder values in the Inspector to hand-test with —
   e.g. Cell Size `1`, Columns `20`, Rows `12`, Origin `(0, 0)`. (Real values come later once
   art is chosen — these just need to be sensible for now.)
4. Create a new scene: **File → New Scene** (Basic 2D template is fine), then **File → Save
   As** → `Assets/_Project/Scenes/Gameplay.unity`. This becomes the real gameplay scene going
   forward per `ARCHITECTURE.md` §4.
5. In the Hierarchy, create an empty GameObject at the scene root named `--- ENVIRONMENT ---`
   (the environment organizer, per `CONVENTIONS.md` scene composition rules).
6. Create an empty GameObject as a child of `--- ENVIRONMENT ---`, name it `Grid`, and **Add
   Component → Grid View**.
7. Drag `GridDef_Default` from the Project window into the `Grid`'s **Grid View → Grid
   Definition** field in the Inspector.
8. With `Grid` selected, look at the **Scene view** (gizmos only draw there, not in Game
   view) — you should see white grid lines forming a 20×12 grid starting at the origin, each
   cell 1 unit. This confirms `GridModel`'s coordinate math is visually correct before
   anything else (placement, pathfinding) gets built on top of it.
9. Save the scene.

## 5. Deviations
- Did not create `Boot.unity` or `MainMenu.unity` this feature — out of scope for a grid-only
  foundation; they belong to whichever future feature actually needs bootstrapping/menu flow.
- Did not implement the brief-mandated Singleton (`GameManager`) yet — nothing in this
  feature needs global/cross-scene state, and CLAUDE.md rule 2 prohibits speculative systems.
  It will land in the feature that first needs it (likely Core bootstrap or event-channel
  setup).
- Otherwise none; implementation follows `CONVENTIONS.md` and `ARCHITECTURE.md` §2 exactly
  (namespace `CaseGame.Grid`, folder `Scripts/Runtime/Grid/`, humble MonoBehaviour + plain C#
  split).
