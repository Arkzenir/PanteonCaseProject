**2026-07-21 — Shared Entity Base (Buildings/Units refactor)**

## 1. Summary
Implemented the human's suggestion from reviewing Report 009: extract a common base shared by
buildings and (future) units, since both need exactly the same shape — a name/sprite/footprint/
maxHealth definition, and a MonoBehaviour that owns a `Health`, implements `IDamageable`,
renders a sprite, and reports death to whoever created it. New `CaseGame.Entities` module:
`GameEntityDefinition` (abstract SO base) and `GameEntityBase` (abstract MonoBehaviour base).
`BuildingDefinition`/`UnitDefinition` now extend `GameEntityDefinition`; `BuildingBase` now
extends `GameEntityBase` and does nothing but re-expose `Definition` as the strongly-typed
`BuildingDefinition` for consumers. `Barracks`/`PowerPlant`/`BuildingFactory` are unaffected —
they only ever touched `BuildingBase`'s public surface, which is unchanged in shape.

I did not name the shared base `Unit*` as literally suggested. Checking the brief's actual
wording: "unit(s)" is used consistently and specifically to mean *soldiers* — "Soldier Units,"
"military units," "selecting units with left mouse click," "attack...on a unit or building"
(unit and building always as parallel/sibling terms, never one subsuming the other). Naming
the shared base `UnitBase`/`UnitDefinition` would both misread against the brief's own
vocabulary and collide with the already-existing `UnitDefinition` (soldier data). The technical
merit of the suggestion stands on its own regardless of the terminology point — there was
real, current field/logic duplication between `BuildingDefinition`/`UnitDefinition` and
`BuildingBase`'s HP-handling that a future `SoldierBase` would have repeated a third time — so
I implemented it as `GameEntityDefinition`/`GameEntityBase` instead, per the naming discussed
and agreed with the human before starting.

Separately, the human hand-adjusted the actual project structure after Report 009's checklist,
which I've recorded in `CONVENTIONS.md` as project-specific overrides rather than leaving the
docs describing a structure that no longer matches reality:
- SO definition assets now live under `Assets/_Project/ScriptableObjects/<Category>/` (e.g.
  `ScriptableObjects/Units/Buildings/`, `ScriptableObjects/Units/Troops/`) instead of the
  generic `Settings/` folder, which is now unused/empty.
- Building/unit prefabs are a parent GameObject with child GameObjects per concern (e.g.
  `Building_Barracks` has `Visuals` (SpriteRenderer), `Hitbox` (collider, for future
  selection), and `SpawnPoint`) rather than one flat GameObject with every component on the
  root.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityDefinition.cs`](Assets/_Project/Scripts/Runtime/Entities/GameEntityDefinition.cs) — new shared SO base (name/sprite/footprint/maxHealth, `OnValidate` clamping).
- [`Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs`](Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs) — new shared MonoBehaviour base (`Health`/`IDamageable`/sprite/death-callback — moved here verbatim from the old `BuildingBase`).
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingDefinition.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingDefinition.cs) — now extends `GameEntityDefinition`; only `producibleUnits`/`ProducibleUnits`/`CanProduceUnits` remain here.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs) — now extends `GameEntityBase`; only the strongly-typed `Definition` re-exposure remains here.
- [`Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs`](Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs) — now extends `GameEntityDefinition`; only `attackDamage`/`AttackDamage` remain here.
- [`Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityDefinitionTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityDefinitionTests.cs) — 2 tests (footprint/maxHealth clamping), moved from `BuildingDefinitionTests`.
- [`Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Entities/GameEntityBaseTests.cs) — 4 tests (Initialize/ApplyDamage/death-callback), moved from `BuildingBaseTests`.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingDefinitionTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingDefinitionTests.cs) — trimmed to 2 Building-specific tests (`CanProduceUnits`).
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs) — trimmed to 1 Building-specific test (typed `Definition` re-exposure).
- [`Docs/Agent/CONVENTIONS.md`](Docs/Agent/CONVENTIONS.md) — two new override rows for the human's actual `ScriptableObjects/` folder layout and per-concern prefab hierarchy.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — new `Entities` module row, Buildings/Units dependency rows updated, implementation log entry, decisions log entry #22, prefab/SO-asset inventory in §4 updated to match what's actually on disk.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**,
0 `error CS` lines.

EditMode tests: **52/52 passed** — net +2 versus Report 009's 50 (moved, not duplicated: 3
Building-specific tests were extracted up into 6 shared `Entities` tests, while
`BuildingDefinitionTests`/`BuildingBaseTests` shrank accordingly; `BuildingFactoryTests`
untouched). No behavior changed, confirmed by every previously-passing assertion still
passing under its new location.

## 4. Editor hookup checklist
No new scene/asset wiring required by this refactor itself — it doesn't touch script GUIDs
for `Barracks.cs`/`PowerPlant.cs` (unchanged files), so your in-progress `Building_Barracks`/
`Building_PowerPlant` prefabs aren't affected by anything already done to them. To finish
wiring them (picking up where Report 009's checklist left off):
1. On each prefab's root, **Add Component → Barracks** (or **Power Plant**).
2. Drag the `Visuals` child's `SpriteRenderer` into the component's **Sprite Renderer** field
   (inherited from `GameEntityBase`, shown in the same Inspector).
3. On `Building_Barracks`, drag the `SpawnPoint` child's Transform into the **Spawn Point**
   field.
4. Assign a sprite to each `BuildingDef_Barracks`/`BuildingDef_PowerPlant` asset if you haven't
   yet (unaffected by this refactor — still the same `Sprite` field, now inherited from
   `GameEntityDefinition`).

## 5. Deviations
None beyond the naming departure from the literal suggestion (`GameEntity*` instead of
`Unit*`), discussed and agreed with the human before implementation.
