**2026-07-21 — Combat Foundation**

## 1. Summary
Implemented the `Combat` module foundation (`CaseGame.Combat`): `IDamageable` (the contract —
`MaxHealth`, `CurrentHealth`, `IsDead`, `ApplyDamage(int)`) and `Health` (a plain C# class
implementing it — HP state, damage clamped at 0, `Damaged`/`Died` events). Same
"foundation, no consumers yet" shape as Grid/Core/Events/Pooling: no soldier or building type
exists yet to construct a `Health` with the brief's actual numbers (requirement 9: 10 HP per
soldier, requirement 10: Barracks 100 HP / Power Plant 50 HP), and nothing yet calls
`ApplyDamage` from an attack command (requirement 11) — those land with Units/Buildings/
Selection. I did not add a separate "attack resolver" class: resolving an attack is just
`target.ApplyDamage(amount)`, which would make a wrapper a one-line pass-through with no logic
of its own — premature abstraction.

One refinement to the Phase-0 architecture: ARCHITECTURE.md's module map listed Combat as
depending on Events, presumably meaning `Health.Damaged`/`Died` would go through the Events
module's `GameEventChannel<T>`. I used plain per-instance C# events instead. Every unit/
building will own its *own* `Health` instance; a shared/global SO channel would broadcast
every instance's HP changes to every listener, which is the wrong shape for "this specific
soldier took damage" — a listener would have to filter for "is this event about the instance I
care about," solving a problem that doesn't exist when the raiser and the one interested
listener (that instance's own future controller/view) can just share the `Health` object
reference directly. A shared channel remains the right tool for genuinely cross-system,
one-of-a-kind signals (e.g. a future `SelectionChanged`), just not for per-instance state.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Combat/IDamageable.cs`](Assets/_Project/Scripts/Runtime/Combat/IDamageable.cs) — damageable contract.
- [`Assets/_Project/Scripts/Runtime/Combat/Health.cs`](Assets/_Project/Scripts/Runtime/Combat/Health.cs) — HP state, damage/clamp/death logic, `Damaged`/`Died` events.
- [`Assets/_Project/Scripts/Tests/EditMode/Combat/HealthTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Combat/HealthTests.cs) — 8 EditMode tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map row updated (Combat no longer lists Events as a dependency), implementation log entry, decisions log entry #19.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**,
0 `error CS` lines.

EditMode tests: **40/40 passed** — the 32 pre-existing (8 Grid, 7 `ResolutionOptionsBuilder`,
6 `GameEventTests`, 4 `GameEventChannelTests`, 7 `PrefabPoolTests`) plus 8 new `HealthTests`:
constructor sets max/current HP; non-positive max HP throws; `ApplyDamage` reduces current
HP; `Damaged` fires with the amount applied; reducing HP to exactly 0 sets `IsDead` and fires
`Died`; damage exceeding remaining HP clamps at 0 (doesn't go negative); applying damage to an
already-dead instance is a no-op (HP stays 0, no further `Damaged`/`Died`); zero or negative
damage amounts are no-ops.

## 4. Editor hookup checklist
None — pure C# infrastructure, no consumers yet, no scene/asset wiring required. The first
feature that constructs a real `Health` (Buildings or Units) will wire its `Died` event to
pooling/destruction and `Damaged` to a view update at that point.

## 5. Deviations
None beyond the Events-dependency refinement described above (recorded as decisions log #19,
not a deviation from anything the human specifically asked for — a Phase-0 architectural
assumption turned out not to fit once actually implemented).
