**2026-07-21 — Buildings**

## 1. Summary
Implemented the `Buildings` module (`CaseGame.Buildings`) per ARCHITECTURE.md's module map:
`BuildingDefinition` (SO — name, sprite, footprint in grid cells, max HP, producible units
list), `BuildingBase` (abstract, humble MonoBehaviour implementing `IDamageable` via a
`Health` it constructs from the definition), `Barracks` (adds a spawn point — GI-7) and
`PowerPlant` (no additions — the only building type with no producible units, GI-6),
demonstrating real OOP inheritance/polymorphism rather than a token abstract class. Also
`BuildingFactory` — the brief-mandated Factory pattern — which pools instances via the
already-built `PrefabPool<T>` (Report 007) rather than plain `Instantiate`/`Destroy`.

`BuildingBase` doesn't know about pooling at all: `Initialize(definition, onDied)` takes an
optional death callback, and `BuildingFactory.Create` is the one place that wires
`() => pool.Release(instance)` — the humble building class stays single-responsibility
(HP/sprite only) and stays testable with a plain callback, no pool involved in its own tests.

One necessary prerequisite: brief requirement 2 explicitly mandates `BuildingDefinition`
reference a list of `UnitDefinition`s (data-driven, not switch-cased per building type) — the
type has to exist for `BuildingDefinition` to compile. I added a minimal `UnitDefinition` (SO,
data only: name/sprite/footprint/HP/attack damage) under `CaseGame.Units`. This is *not* the
full Units feature — `SoldierBase`, `Soldier1/2/3` behavior, and `UnitFactory` are still a
separate future feature; this is just the data shape Buildings needs to reference.

This feature is foundation-*ish* rather than pure foundation like Reports 001/002/006/007/008:
the types are real and match the brief's exact mechanics (Barracks 100 HP / Power Plant 50 HP,
destruction at 0 HP), but nothing places a building in a scene yet — that's Placement's job
per the module map ("Placement... commit-to-grid"), so no numbered requirement is checked off
this turn. The human needs to create the two concrete definition assets + prefabs (checklist
below) before anything is visibly testable in the Editor.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs`](Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs) — minimal producible-unit data (prerequisite for `BuildingDefinition`).
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingDefinition.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingDefinition.cs) — building data, `CanProduceUnits`, footprint/HP clamping via `OnValidate`.
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingBase.cs) — abstract base: `Health` ownership, `IDamageable`, sprite assignment, death callback.
- [`Assets/_Project/Scripts/Runtime/Buildings/Barracks.cs`](Assets/_Project/Scripts/Runtime/Buildings/Barracks.cs) — adds `SpawnPosition` (GI-7).
- [`Assets/_Project/Scripts/Runtime/Buildings/PowerPlant.cs`](Assets/_Project/Scripts/Runtime/Buildings/PowerPlant.cs) — no additions (GI-6).
- [`Assets/_Project/Scripts/Runtime/Buildings/BuildingFactory.cs`](Assets/_Project/Scripts/Runtime/Buildings/BuildingFactory.cs) — Factory pattern, per-prefab `PrefabPool<BuildingBase>`.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingDefinitionTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingDefinitionTests.cs) — 3 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingBaseTests.cs) — 4 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingFactoryTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Buildings/BuildingFactoryTests.cs) — 3 tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Buildings' dependency list corrected to Combat/Pooling/Units), implementation log entry, decisions log entries #20–21.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**,
0 `error CS` lines.

EditMode tests: **50/50 passed** — the 40 pre-existing plus 10 new:
- `BuildingDefinitionTests` (3): `CanProduceUnits` false on an empty list, true once one
  `UnitDefinition` is added; `OnValidate` clamps a non-positive footprint/max HP up to 1.
- `BuildingBaseTests` (4): `Initialize` sets `Definition`/HP from the definition;
  `ApplyDamage` forwards to `Health` and reduces `CurrentHealth`; damage reducing HP to 0 sets
  `IsDead` and invokes the `onDied` callback; killing an instance initialized *without* a
  callback doesn't throw (the callback is optional).
- `BuildingFactoryTests` (3): `Create` returns an instance initialized with the given
  definition; after an instance dies (releasing it back to its pool via the death callback),
  the next `Create` with the same prefab reuses that exact instance (confirms the
  `PrefabPool<T>` wiring, not just a fresh `Instantiate`); two distinct prefabs produce
  independent instances from separate pools.

`BuildingDefinitionTests`' `OnValidate` test invokes the private method via reflection since
`OnValidate` only fires automatically inside the Editor, not from a plain `[Test]` — matches
how `GridDefinition`'s equivalent behavior was exercised in Report 001.

## 4. Editor hookup checklist
Two building types need a concrete `BuildingDefinition` asset and a prefab before anything is
placeable (by the future Placement feature). Neither is created automatically — this is
straightforward reference-assignment well under the throwaway-script threshold.

1. In the Project window, go to `Assets/_Project/Settings/` and create two definition assets:
   **Create → CaseGame → Buildings → Building Definition**, named `BuildingDef_Barracks` and
   `BuildingDef_PowerPlant` (matches the `BuildingDef_<Name>` naming convention).
2. Set `BuildingDef_Barracks`: Building Name = `Barracks`, Footprint = `(4, 4)`, Max Health =
   `100` — these are the brief's exact numbers (GI-3, GI-9).
3. Set `BuildingDef_PowerPlant`: Building Name = `Power Plant`, Footprint = `(2, 3)`, Max
   Health = `50`. Leave **Producible Units** empty on this one (GI-6 — Power Plant produces
   nothing).
4. For sprites: the imported Tiny Swords pack has a literal
   `Assets/Tiny Swords/Buildings/<Color> Buildings/Barracks.png` you can assign directly to
   `BuildingDef_Barracks`. There's no literal "Power Plant" sprite in a fantasy asset pack —
   `Tower.png` or `Monastery.png` from the same folder are reasonable stand-ins, or leave it
   blank until final art is chosen; this is a placeholder decision, not a code dependency.
5. Create two prefabs under `Assets/_Project/Prefabs/Buildings/`: `Barracks.prefab` and
   `PowerPlant.prefab`. Each needs a `SpriteRenderer` component (assign it to the `Sprite
   Renderer` field on the `Barracks`/`PowerPlant` component you add) and the matching
   `Barracks`/`PowerPlant` script component. For `Barracks.prefab`, also add a child empty
   GameObject (e.g. named `SpawnPoint`) positioned where soldiers should emerge, and assign it
   to the `Spawn Point` field.
6. Nothing needs wiring into a scene yet — `BuildingFactory` isn't called from anywhere until
   Placement (or a temporary hand-test harness) exists to drive it.

## 5. Deviations
None beyond the `UnitDefinition` prerequisite and the pooling-callback design, both explained
above and recorded in the decisions log (#20, #21).
