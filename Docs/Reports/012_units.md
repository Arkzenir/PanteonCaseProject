**2026-07-21 — Units**

## 1. Summary
Implemented the `Units` module (`CaseGame.Units`), mirroring `Buildings`' shape closely:
`SoldierBase` (abstract, `: GameEntityBase`) re-exposes `Definition` as the strongly-typed
`UnitDefinition` (same pattern as `BuildingBase`) and adds two behaviors: `MoveTo(targetCell,
grid)` — requests a path from the already-built `AStarPathfinder` and walks it via a
**Coroutine** (the brief's mandated Coroutine pattern — its natural home per
ARCHITECTURE.md §3's own data-flow description: "the soldier's controller... requests a path
from Pathfinding, and moves along it via coroutine") — and `TryAttack(IDamageable target)`,
which applies the soldier's attack damage (GI-10/11). `UnitFactory` is the Factory-pattern
creation path, pooling instances via the existing `PrefabPool<T>`, mirroring `BuildingFactory`
exactly. `UnitDefinition` (from Report 009) gained a `MoveSpeed` field — designer-tunable per
CONVENTIONS.md rather than hardcoded.

**One deliberate deviation from the Phase-0 architecture note**, which named `Soldier1`/
`Soldier2`/`Soldier3` as the expected classes: I shipped a single concrete `Soldier :
SoldierBase` (empty — mirrors `PowerPlant`'s "adds nothing" pattern) instead. Requirement 9's
only difference between the 3 soldier types is attack damage (10/5/2) — a data value; nothing
behavioral differs. Three empty subclasses would have been an OOP checkbox with no real
substance behind it, and would have contradicted the project's own established philosophy of
differentiating via data rather than code (the same reasoning already applied to
`BuildingDefinition`'s producible-units list — decision #8). `SoldierBase` stays abstract, so
a *genuinely* different future soldier type still has a clean extension point. The 3 required
types ship as 3 `UnitDefinition` assets + 3 prefabs sharing the one `Soldier` class.

No scene has an actual soldier in it yet — Selection and the Production Menu (the systems
that will actually spawn and command one) don't exist yet.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs`](Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs) — added `MoveSpeed` field + clamp.
- [`Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs`](Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs) — typed `Definition`, `MoveTo` (pathfind + coroutine), `TryAttack`.
- [`Assets/_Project/Scripts/Runtime/Units/Soldier.cs`](Assets/_Project/Scripts/Runtime/Units/Soldier.cs) — the one concrete soldier class (empty).
- [`Assets/_Project/Scripts/Runtime/Units/UnitFactory.cs`](Assets/_Project/Scripts/Runtime/Units/UnitFactory.cs) — Factory pattern, per-prefab `PrefabPool<SoldierBase>`.
- [`Assets/_Project/Scripts/Tests/EditMode/Units/UnitDefinitionTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Units/UnitDefinitionTests.cs) — 2 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Units/SoldierBaseTests.cs) — 5 tests.
- [`Assets/_Project/Scripts/Tests/EditMode/Units/UnitFactoryTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Units/UnitFactoryTests.cs) — 3 tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — Units module map row corrected, implementation log entry, decisions log entries #26–27.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**,
0 `error CS` lines.

EditMode tests: **70/70 passed** — the 60 pre-existing plus 10 new:
- `UnitDefinitionTests` (2): `OnValidate` clamps attack damage to at least 0 and move speed to
  a positive minimum.
- `SoldierBaseTests` (5): `Definition` returns the strongly-typed `UnitDefinition`;
  `TryAttack(null)` doesn't throw; `TryAttack` with a valid target applies exactly the
  definition's attack damage (verified against a small hand-written `FakeDamageable` test
  double, not a real `Health`, to isolate what's being tested); `MoveTo` to the soldier's own
  current cell doesn't throw; `MoveTo` to an unreachable (occupied) goal doesn't throw.
- `UnitFactoryTests` (3): mirrors `BuildingFactoryTests` — `Create` returns an instance
  initialized with the given definition; an instance that dies is reused by the next `Create`
  call on the same prefab (confirms the pool wiring); two distinct prefabs produce independent
  instances.

`MoveTo`'s actual coroutine-driven movement (stepping toward each waypoint over multiple
frames) is **not** automated-tested, consistent with the established precedent
(`ENVIRONMENT.md`'s note on `Awake` not firing reliably in this machine's batchmode EditMode
runner — coroutines have the same category of problem, since Unity's coroutine scheduler is
tied to the Player Loop, which doesn't tick during a synchronous `[Test]`). The two `MoveTo`
tests above were deliberately scoped to only exercise the early-return guard clauses (no path
needed / no path found), which never reach `StartCoroutine`, so they're safe and meaningful
without touching that limitation. Actual movement is hand-test material once a soldier exists
in a scene to watch move.

## 4. Editor hookup checklist
Mirrors Report 009's Buildings checklist. Three `UnitDefinition` assets + prefabs, all sharing
the one `Soldier` script:

1. In `Assets/_Project/ScriptableObjects/Units/Troops/` (matching your existing folder
   layout), create three assets: **Create → CaseGame → Units → Unit Definition**, named
   `UnitDef_Soldier1`, `UnitDef_Soldier2`, `UnitDef_Soldier3`.
2. Set all three to **Max Health = 10** (brief requirement 9 — identical for all 3).
3. Set **Attack Damage**: `10` for Soldier1, `5` for Soldier2, `2` for Soldier3 (brief's exact
   numbers). Leave **Move Speed** at its default (`3`) for all three unless you want them to
   move at different speeds — the brief doesn't require that, so identical is a safe default.
4. Set **Footprint** to `1×1` for all three (brief requirement 3's mockup value) if not
   already defaulted correctly.
5. Create three prefabs (suggest `Assets/_Project/Prefabs/Troops/` to match your `Troops`
   naming, or `Prefabs/Units/` if you'd rather match the script namespace — your call):
   `Soldier_1.prefab`, `Soldier_2.prefab`, `Soldier_3.prefab`. Each needs the same
   `Visuals`/`Hitbox` child structure you used for the buildings, with the **Soldier**
   component (not `Barracks`/`PowerPlant`) on the root, and its **Sprite Renderer** field
   pointing at the `Visuals` child.
6. For sprites: the Tiny Swords pack has full unit sets per color under
   `Assets/Tiny Swords/Units/<Color> Units/{Archer,Lancer,Monk,Warrior}/`, each with its own
   idle sprite and a ready-made Animator Controller (Idle/Run/Attack/Guard) if you want to wire
   animation later. A flavor suggestion, not a requirement: Warrior (heaviest melee) for
   Soldier1 (10 dmg), Archer or Lancer for Soldier2 (5 dmg), Monk for Soldier3 (2 dmg) — purely
   your call.
7. Nothing needs wiring into a scene yet — `UnitFactory` isn't called from anywhere until
   Selection or the Production Menu exists to drive it.

## 5. Deviations
- **`Soldier1`/`Soldier2`/`Soldier3` as one `Soldier` class instead of 3 classes** — discussed
  above and recorded in the decisions log (#26). Not a deviation from anything you specifically
  asked for; a refinement of the Phase-0 architecture note once actually implemented, same
  category as Report 010's `GameEntity*` naming decision.
- No automated test for the coroutine-driven movement itself — explained in Test results,
  consistent with the established `Awake`/lifecycle-testing limitation for this environment.
