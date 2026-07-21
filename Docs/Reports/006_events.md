**2026-07-21 — Events Foundation**

## 1. Summary
Implemented the `Events` module foundation (`CaseGame.Events`) that ARCHITECTURE.md's module
map already lists as a dependency of nearly every future system (Core, Buildings, Units,
Combat, Placement, Selection, both UI panels), and that the brief's DESIGN section explicitly
mandates as a required pattern ("Events — decoupled communication between systems"). Since no
concrete gameplay types exist yet (no `BuildingDefinition`, `Health`, etc.) to serve as event
payloads, this feature ships the reusable infrastructure only — the same "foundation, no
consumers yet" shape as Report 001 (Grid) and Report 002 (Core Singleton):

- `GameEvent` — a parameterless, `[CreateAssetMenu]` ScriptableObject signal channel
  (Subscribe/Unsubscribe/Raise). A raiser and a listener both just reference the same asset;
  neither needs a reference to the other.
- `GameEventChannel<T>` — a generic base for typed payload channels. Unity can't
  `[CreateAssetMenu]` an open generic type, so a concrete channel (e.g. a `BuildingDefinition`
  channel for Selection → Info Panel) will subclass this with a specific `T` once that payload
  type actually exists — inventing payload types speculatively now would violate CLAUDE.md
  rule 2.
- `GameEventListener` — a humble MonoBehaviour that subscribes to a `GameEvent` while enabled
  and forwards it to a designer-configured `UnityEvent`, so a response (sound, animation, UI
  refresh) can be wired entirely in the Inspector with no script referencing the raiser.

This follows CONVENTIONS.md's own baseline, which already names "ScriptableObject event
channels when designer-facing wiring is valuable" as the preferred decoupling mechanism ahead
of plain static C# events (which would reintroduce the static/global coupling the brief's
Events requirement exists to avoid).

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Events/GameEvent.cs`](Assets/_Project/Scripts/Runtime/Events/GameEvent.cs) — parameterless SO event channel.
- [`Assets/_Project/Scripts/Runtime/Events/GameEventChannel.cs`](Assets/_Project/Scripts/Runtime/Events/GameEventChannel.cs) — generic `GameEventChannel<T>` base for future typed channels.
- [`Assets/_Project/Scripts/Runtime/Events/GameEventListener.cs`](Assets/_Project/Scripts/Runtime/Events/GameEventListener.cs) — `GameEvent` → `UnityEvent` bridge component.
- [`Assets/_Project/Scripts/Tests/EditMode/Events/GameEventTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Events/GameEventTests.cs) — 6 EditMode tests for `GameEvent`.
- [`Assets/_Project/Scripts/Tests/EditMode/Events/GameEventChannelTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Events/GameEventChannelTests.cs) — 4 EditMode tests for `GameEventChannel<T>` (via a private test-only `TestIntEventChannel` subclass).
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — implementation log entry, decisions log entry #17.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check — Mode A while the editor was still catching up
on reimporting the new files, then Mode B once closed): **passed**, 0 `error CS` lines.

EditMode tests: **25/25 passed** — the 15 pre-existing (8 Grid, 7 `ResolutionOptionsBuilder`)
plus 10 new:
- `GameEventTests` (6): raise invokes a subscribed listener; raise with no listeners doesn't
  throw; raise invokes multiple listeners; unsubscribe stops future raises; subscribing the
  same listener twice invokes it only once; a listener unsubscribing itself mid-raise doesn't
  throw or skip other listeners (exercises the back-to-front iteration in both classes).
- `GameEventChannelTests` (4): raise passes the payload through to the listener; raise with no
  listeners doesn't throw; unsubscribe stops future raises; duplicate-subscribe protection.

`GameEventListener`'s `OnEnable`/`OnDisable` subscribe/unsubscribe wiring is not covered by
automated tests — consistent with the `ENVIRONMENT.md` note from Report 002: MonoBehaviour
lifecycle callbacks don't reliably fire on `AddComponent`-created objects in this machine's
batchmode EditMode test runner. It'll get exercised naturally by hand-testing once a future
feature actually wires a `GameEvent` + `GameEventListener` into a scene.

## 4. Editor hookup checklist
None — this feature is pure C#/ScriptableObject infrastructure with no consumers yet, so
there's no scene, asset, or Inspector wiring to do. The first feature that raises or listens
to an actual event (e.g. Selection → Info Panel) will create the concrete `GameEvent`/
`GameEventChannel<T>` asset instance(s) and wire them at that point.

## 5. Deviations
None.
