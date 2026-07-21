**2026-07-21 — Pooling Foundation**

## 1. Summary
Implemented the `Pooling` module foundation (`CaseGame.Pooling`) that ARCHITECTURE.md's module
map already reserves as a dependency for the Production Menu's infinite scroll view and for
frequently spawned/destroyed units and buildings, and that the brief mandates explicitly
("Object Pooling" in the DESIGN section, and specifically tied to the infinite scroll view in
the UX section). Same "foundation, no consumers yet" shape as Grid/Core/Events: no concrete
pooled type (scroll-view list items, soldiers, buildings) exists yet, so this ships the
reusable pooling utility only.

`PrefabPool<T>` (`T : Component`) is a thin wrapper around Unity's built-in
`UnityEngine.Pool.ObjectPool<T>` rather than a hand-rolled stack/queue implementation — the
brief mandates the *pattern* being demonstrably used, not a from-scratch reimplementation, and
`ObjectPool<T>` has shipped in `UnityEngine.CoreModule` since Unity 2021.1 (no package needed).
I confirmed it's actually available on our pinned `2021.3.45f2` by compile rather than
assuming, per CLAUDE.md golden rule 7 — first compile attempt only had an unrelated
`Object`-namespace-ambiguity error in the test file, not anything about `ObjectPool<T>` itself,
confirming the API resolves correctly on this version. `PrefabPool<T>` handles
Instantiate/activate/deactivate/Destroy through the pool's callback hooks, so callers just
`Get()`/`Release()` instances instead of touching `Instantiate`/`Destroy` directly.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Pooling/PrefabPool.cs`](Assets/_Project/Scripts/Runtime/Pooling/PrefabPool.cs) — generic prefab-instance pool wrapping `UnityEngine.Pool.ObjectPool<T>`.
- [`Assets/_Project/Scripts/Tests/EditMode/Pooling/PrefabPoolTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Pooling/PrefabPoolTests.cs) — 7 EditMode tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — implementation log entry, decisions log entry #18.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode B, batchmode, editor closed): **passed**,
0 `error CS` lines, after fixing one issue caught mid-verification (see Deviations).

EditMode tests: **32/32 passed** — the 25 pre-existing (8 Grid, 7 `ResolutionOptionsBuilder`,
6 `GameEventTests`, 4 `GameEventChannelTests`) plus 7 new `PrefabPoolTests`: `Get()` on an
empty pool creates an active instance; the instance is parented under the provided transform;
two `Get()`s without a `Release()` return distinct instances; `Release()` deactivates the
instance; `Get()` after `Release()` reuses the same instance (doesn't create a new one);
`CountActive`/`CountInactive` track correctly across a get/get/release sequence; releasing the
same instance twice throws `InvalidOperationException` (Unity's built-in double-release
detection, confirmed empirically rather than assumed).

## 4. Editor hookup checklist
None — this feature is pure C# infrastructure with no consumers yet, no scene/asset wiring
required. The first feature that actually pools something (most likely the Production Menu's
scroll view, or `UnitFactory`/`BuildingFactory`) will construct a `PrefabPool<T>` for its own
concrete `T` and wire a real prefab reference to it at that point.

## 5. Deviations
- First compile attempt failed with `CS0104: 'Object' is an ambiguous reference between
  'UnityEngine.Object' and 'object'` in the test file (`using System;` for
  `InvalidOperationException` collided with the `using UnityEngine;` `Object` type on a plain
  `Object.DestroyImmediate(...)` call). Fixed by qualifying as `UnityEngine.Object.DestroyImmediate(...)`.
  Not a real design deviation — just a one-line fix caught by the compile check doing its job.
