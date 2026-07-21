**2026-07-21 — GameManager Root-Persistence Fix**

## 1. Summary
Fixed a runtime bug the human found while hand-testing Report 002's checklist:
`DontDestroyOnLoad only works for root GameObjects or components on root GameObjects.` This
happened because `GameManager.Awake` called `DontDestroyOnLoad(gameObject)` on itself, but the
editor hookup checklist (correctly, per CONVENTIONS.md scene composition) placed `GameManager`
as a *child* of the `--- SYSTEMS ---` organizer, not at scene root. `DontDestroyOnLoad` only
accepts root GameObjects. Fixed by persisting `transform.root.gameObject` instead — this also
better matches the architecture's intent (`ARCHITECTURE.md` §4) that everything under
`--- SYSTEMS ---` (event channels, pools, etc., once they exist) survives scene loads
together, not just `GameManager` in isolation.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Core/GameManager.cs`](Assets/_Project/Scripts/Runtime/Core/GameManager.cs) — `DontDestroyOnLoad(gameObject)` → `DontDestroyOnLoad(transform.root.gameObject)`; doc comment updated to explain why.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — implementation log entry and decisions log entry #12 for this fix.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode A — `dotnet build`, editor was open):
**passed**, 0 errors, 0 warnings. No automated tests apply (same reasoning as Report 002 —
this is lifecycle-bound code, not engine-independent logic).

**Hand-test needed (re-run):** with `Boot.unity` open, re-enter Play mode and confirm the
`GameManager`-error no longer appears in the Console, and that the `--- SYSTEMS ---` object
(carrying `GameManager` as its child) now shows under the "DontDestroyOnLoad" pseudo-scene in
the Hierarchy after a scene load.

## 4. Editor hookup checklist
None — no new scene/asset wiring, this only changes existing script behavior already wired up
in Report 002's checklist. Just re-run the hand-test above to confirm.

## 5. Deviations
None.
