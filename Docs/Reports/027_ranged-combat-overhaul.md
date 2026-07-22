**2026-07-22 — Ranged combat & combat overhaul**

## 1. Summary
Backlog item 12, the largest single feature so far — the human's fully-fleshed-out spec (see
`CURRENT_STATUS.md`'s item 12 history) folded in one pass: attack speed, a sustained auto-attack
loop replacing the old instant hit, move-into-range-then-attack, a tracked ranged projectile
visual, and a Particle-System-vs-MonoBehaviour determination the human explicitly delegated to
the agent.

**Data.** `UnitDefinition` gains `Ranged` (bool), `AttackRange` (grid cells, Chebyshev distance,
min 1), and `AttackSpeed` (attacks/second, min 0.01 — mirroring `MoveSpeed`'s existing "N per
second" convention, since the human didn't specify the exact unit).

**Sustained auto-attack, not a single instant hit.** `SoldierBase.TryAttack` (decision #37 — an
instant, range-less hit) is replaced by `Attack(GameEntityBase target, GridModel grid,
ProjectileFactory projectileFactory)`. It walks into `AttackRange` first if not already there
(via a new `AStarPathfinder.FindApproachCell`), then attacks once every `1 / AttackSpeed` seconds
until the target dies or leaves range. This applies uniformly to melee and ranged units — the
human's wording was "attack input" generically, not ranged-specific.

**Cancellation and target-switching.** `MoveTo` and `Attack` now share a single
`_actionCoroutine` slot on `SoldierBase` (previously `MoveTo` had its own `_moveCoroutine`): a
move order cancels an in-progress attack, and a new `Attack` call cancels a move or a previous
attack. Calling `Attack` again with a *different* target switches onto it immediately — no
"attack lock" to clear first — since both `MoveTo` and `Attack` already stop-and-replace
whatever's running in that one slot; this is exactly the human-confirmed target-switching
behavior, with no special-case code needed for it.

**Move-then-attack, corrected.** A unit given an attack command out of range paths to the
*nearest unoccupied cell within `AttackRange` of the target's current cell* (new
`AStarPathfinder.FindApproachCell`, ranked by straight-line distance to the attacker) — not onto
the target's own cell. A melee unit with `AttackRange = 1` ends up on an adjacent cell (the
human's explicitly confirmed expected behavior); a larger configured range stops that many cells
away instead. Same code path for melee and ranged.

**Ranged projectile — a new pooled `Projectile`/`ProjectileFactory` (`CaseGame.Units`).**
`PerformAttack` branches only on `Ranged`: melee applies damage instantly (unchanged
brief-minimum behavior); ranged launches a `Projectile` instead. `Projectile` has no
`Collider`/`Rigidbody` — it's purely visual, never collides with anything it passes over — and
re-tracks its target's *live* `transform.position` every `Update`, not a fixed snapshot
trajectory, applying damage only on actual arrival. Pooled in its own dedicated
`PrefabPool<Projectile>` (mirrors `BuildingFactory`/`UnitFactory`, decision #18), not shared with
any other pool.

**Particle System vs. MonoBehaviour — determined** (the human explicitly asked the agent to
decide this): a plain pooled MonoBehaviour, not a Shuriken `ParticleSystem`. The requirement is
precise per-target position tracking plus an exact "arrived → apply damage" trigger;
reproducing that with `ParticleSystem` would mean manually driving individual particles via
`SetParticles`/`GetParticles` every frame anyway — no ergonomic win over a MonoBehaviour, while
losing straightforward `Update()`-based homing and clean arrival timing. `ParticleSystem` remains
the right tool for a later, separate concern (impact bursts/muzzle flash, backlog item 19), not
the projectile's own flight.

**Wiring.** `SelectionController.HandleRightClick`'s target parameter changed from `IDamageable`
to `GameEntityBase` — `IDamageable` alone has no position, but both range-checking and
projectile-tracking need the target's live `transform`; every real attack target is already a
`GameEntityBase` at the call site (`HitTestEntity`), so this is a strict narrowing, not a new
coupling. `SelectionController.Initialize` gained a `ProjectileFactory` parameter, threaded from
a new `GameplayBootstrap`-constructed instance (mirrors `BuildingFactory`/`UnitFactory`'s
existing construction pattern).

**Soldier 2 is now the Archer** (`ranged: true`, `attackRange: 4`) — sprite and attack damage
unchanged; the Tiny Swords pack's existing `Arrow.png` is reused for the projectile prefab's
visual rather than a placeholder.

**Scope note (architectural).** Range/approach checks use the target's own
`transform.position` (a single reference cell), not footprint-aware distance to a building's
occupied cells. `Units` cannot reference `BuildingBase` — `Buildings` already depends on `Units`
(for `ProducibleUnits`), so the reverse would be circular, the same reasoning decision #43
already established for building spawn positions. Large-footprint buildings may need a
generously-sized `attackRange` configured to guarantee reachability; refining this to true
nearest-footprint-edge distance is a clean, isolated follow-up if it's ever visually wrong, not
a blocker now.

**Unrelated pre-existing bug found and fixed while verifying.** The full EditMode suite failed 7
tests in `InfoPanelControllerTests` on the first run — all with `InvalidCastException` at
`InfoPanelController.cs:103`, the `(RectTransform)panelRoot.transform` cast Report 025 added.
The test's `_panelRoot` was created as a plain `GameObject` (no `RectTransform`) — a real,
pre-existing gap, invisible until now because Report 025's turn explicitly skipped batch testing
per the human's own instruction that turn. Fixed by giving the test's `_panelRoot` a
`RectTransform`, matching every other RectTransform-typed field already in that same test file.
Production code is unaffected — the real `InformationPanel.prefab`'s `PanelContent` object
always had a `RectTransform`.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs`](Assets/_Project/Scripts/Runtime/Units/UnitDefinition.cs) — `Ranged`/`AttackRange`/`AttackSpeed` fields + `OnValidate` clamping.
- [`Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs`](Assets/_Project/Scripts/Runtime/Units/SoldierBase.cs) — `TryAttack` → `Attack`; unified `_actionCoroutine`; `IsActing`, `ChebyshevDistance`, `IsInRange`, `AttackInterval` (pure, testable); `AttackRoutine`/`PerformAttack`/`MoveRoutine`.
- [`Assets/_Project/Scripts/Runtime/Pathfinding/AStarPathfinder.cs`](Assets/_Project/Scripts/Runtime/Pathfinding/AStarPathfinder.cs) — new `FindApproachCell` (pure, testable).
- [`Assets/_Project/Scripts/Runtime/Units/Projectile.cs`](Assets/_Project/Scripts/Runtime/Units/Projectile.cs) — new. Pooled visual-only tracking projectile; `Step`/`HasArrived` extracted as pure/testable.
- [`Assets/_Project/Scripts/Runtime/Units/ProjectileFactory.cs`](Assets/_Project/Scripts/Runtime/Units/ProjectileFactory.cs) — new. Wraps a single `PrefabPool<Projectile>`.
- [`Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs`](Assets/_Project/Scripts/Runtime/Selection/SelectionController.cs) — `HandleRightClick`'s target param `IDamageable` → `GameEntityBase`; `Initialize` gained `ProjectileFactory`; calls `Attack` instead of `TryAttack`.
- [`Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs`](Assets/_Project/Scripts/Runtime/Gameplay/GameplayBootstrap.cs) — constructs `ProjectileFactory`, new `projectilesContainer`/`projectilePrefab` fields, passes it to `SelectionController.Initialize`.
- `Assets/_Project/ScriptableObjects/GameEntityDefs/Units/UnitDef_Soldier2.asset` — `ranged: 1`, `attackRange: 4`, `attackSpeed: 1` added (data-only edit).
- `Assets/_Project/Prefabs/Units/Projectile.prefab` — new. `SpriteRenderer` (Tiny Swords' `Arrow.png`) + `Projectile`, no collider. Built via a throwaway script, verified by reading it back, then deleted.
- Tests: `UnitDefinitionTests`, `SoldierBaseTests`, `SelectionControllerTests`, `AStarPathfinderTests` updated/extended — see §3. `InfoPanelControllerTests` — unrelated pre-existing bug fix (see §1).
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map (Units, Pathfinding, Selection, Gameplay), implementation log entry, decisions log #62.
- [`Docs/Agent/CURRENT_STATUS.md`](Docs/Agent/CURRENT_STATUS.md) — updated for this report; backlog item 12 marked done.

## 3. Test results
Compile check (Mode B Unity batchmode, editor closed): **passed** — 0 `error CS` lines.

EditMode tests (Mode B batchmode): **179/179 passed** (first run: 172/179, all 7 failures in
`InfoPanelControllerTests` from the unrelated pre-existing bug described in §1; 179/179 after
that one-line test fix).

New/changed coverage:
- `UnitDefinitionTests` (+4): `Ranged` defaults false and is settable; `AttackRange` clamps to
  ≥1; `AttackSpeed` clamps to a positive minimum.
- `AStarPathfinderTests` (+4): `FindApproachCell` returns the nearest free cell within range
  ranked by distance to the attacker; never returns the target's own cell; returns null when
  every candidate is occupied; returns the attacker's own cell if already in range and free (no
  special-casing needed — this falls out of the general nearest-candidate search).
- `SoldierBaseTests`: removed the `IDamageable`-based `TryAttack` tests (API no longer takes
  one); added `Attack_NullTarget_...`/`Attack_DeadTarget_...` (both hit the early-return guard,
  confirmed via `IsActing` staying false) and pure tests for `ChebyshevDistance`/`IsInRange`/
  `AttackInterval`.
- `SelectionControllerTests`: replaced the `FakeDamageable` test double (no longer usable —
  `HandleRightClick` takes `GameEntityBase`, an abstract MonoBehaviour, not fakeable via a plain
  interface implementation) with real `TestBuilding` targets; assertions changed from "damage
  applied" to `IsActing` becoming true, since attacking is now coroutine-driven and its full
  tick-by-tick progression is a Play-Mode-only concern (see below) — `IsActing` is safely
  observable regardless, since it's set synchronously by `StartCoroutine`'s own return value.

**Play-Mode-only / hand-test items** (consistent with this project's existing
`FollowPath`/coroutine precedent — `ENVIRONMENT.md` already documents that this machine's
batchmode EditMode test runner can't reliably drive a live Update loop): the actual walk-into-
range behavior, the sustained per-tick damage/projectile-launch loop, stopping when the target
leaves range or dies, cancellation on a new move order, and target-switching on a new attack
order are all only verifiable in Play Mode. **Hand-test**: select a melee soldier, right-click an
out-of-range target — confirm it walks adjacent then starts hitting repeatedly; do the same with
the Archer and confirm arrows launch, track the target if it moves, and land; right-click empty
ground mid-attack and confirm it stops and moves instead; right-click a second valid target
mid-attack and confirm it switches immediately.

## 4. Editor hookup checklist
1. Open `Gameplay.unity`. Select the GameObject holding `GameplayBootstrap`.
2. In its Inspector, a new **Projectiles Container** field and **Projectile Prefab** field will
   be empty (both newly added).
3. Under `--- GAMEPLAY ---` (alongside the existing `Buildings`/`Units` empty containers), add a
   new empty child GameObject named `Projectiles` — this is where pooled projectile instances
   will parent at runtime, mirroring `Buildings`/`Units`.
4. Drag that new `Projectiles` Transform into `GameplayBootstrap`'s **Projectiles Container**
   field.
5. Drag `Assets/_Project/Prefabs/Units/Projectile.prefab` into `GameplayBootstrap`'s **Projectile
   Prefab** field.
6. Save the scene.

## 5. Deviations
- Range/approach checks are not footprint-aware for building targets (§1's scope note) — an
  architectural boundary, not an oversight; see decisions log #62.
- Found and fixed a pre-existing test bug unrelated to this feature (`InfoPanelControllerTests`'
  `_panelRoot` missing a `RectTransform`) — surfaced only because this is the first full
  batchmode test run since Report 025 introduced the code path it broke. Test-only fix.
- The full attack loop's runtime behavior (walk-into-range, sustained ticking, cancellation,
  target-switching) is Play-Mode-only verifiable, consistent with this project's existing
  coroutine-testing limits — flagged explicitly above as hand-test items rather than silently
  assumed correct.
