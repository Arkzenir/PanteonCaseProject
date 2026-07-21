**2026-07-21 — Pathfinding**

## 1. Summary
Implemented the `Pathfinding` module (`CaseGame.Pathfinding`): `AStarPathfinder`, a static,
stateless, plain-C# A* implementation over the already-built `GridModel` (custom grid-based
algorithm per the brief's explicit DESIGN-section mandate — decisions log #3, no Unity
NavMesh). `FindPath(grid, start, goal)` returns the shortest path as a `List<Vector2Int>`
(inclusive of both endpoints), or `null` if the goal is unreachable/occupied/out of bounds.

Movement is 8-directional (diagonal cost √2, orthogonal cost 1, octile-distance heuristic) with
corner-cutting prevented: a diagonal step is rejected if either flanking orthogonal cell is
blocked. This is the actual mechanism behind requirement 8's "must wander around the
buildings" — without it, a soldier could clip diagonally through a building's corner, which
would technically avoid walking *through* the building but would still look wrong and wouldn't
hold up under the brief's "the project will be examined in detail" edge-case scrutiny
(requirement 18).

Deliberately **not** included: a coroutine-based path-request queue, even though
ARCHITECTURE.md's original module description mentioned "path requests." Nothing calls
`AStarPathfinder` yet — Units and Selection (the modules that would actually request paths)
don't exist — so building an async/queued request layer now would be speculative scope with
no real caller to validate it against. The brief's Coroutine requirement doesn't specifically
need to live here either; it's equally at home in the future unit-movement code that steps a
soldier along whatever path this returns.

## 2. Changes
- [`Assets/_Project/Scripts/Runtime/Pathfinding/AStarPathfinder.cs`](Assets/_Project/Scripts/Runtime/Pathfinding/AStarPathfinder.cs) — the A* implementation.
- [`Assets/_Project/Scripts/Tests/EditMode/Pathfinding/AStarPathfinderTests.cs`](Assets/_Project/Scripts/Tests/EditMode/Pathfinding/AStarPathfinderTests.cs) — 8 EditMode tests.
- [`Docs/Agent/ARCHITECTURE.md`](Docs/Agent/ARCHITECTURE.md) — module map row corrected to describe what actually shipped, implementation log entry, decisions log entries #23–25.

## 3. Test results
Compile check (`ENVIRONMENT.md` §Compile check, Mode A while the editor was still catching up
on reimporting the new test file, then Mode B once closed): **passed**, 0 `error CS` lines.

EditMode tests: **60/60 passed** — the 52 pre-existing plus 8 new `AStarPathfinderTests`
(all verified via a shared `AssertIsValidPath` helper checking start/goal endpoints, no
revisited cells, no occupied cells, and every step a valid single adjacent move):
- Straight-line path with no obstacles is valid and exactly 5 cells for a 4-cell orthogonal
  distance.
- A diagonal destination produces a 4-cell path, not 7 — confirms diagonal movement is
  actually being used/preferred over an orthogonal-only route.
- `start == goal` returns a 1-cell path.
- An occupied goal cell returns `null`.
- An out-of-bounds goal returns `null`.
- A path correctly routes around an obstacle wall with a single gap (the literal "wander
  around a building" scenario).
- A goal fully enclosed on all 4 orthogonal sides (which also blocks every diagonal approach,
  since each diagonal's required flank is one of the occupied orthogonal cells) returns `null`
  — confirms corner-cutting prevention doesn't accidentally open a false diagonal escape route.
- Blocking both cells flanking a direct diagonal step forces a real, valid detour (path length
  > 2) rather than clipping the corner — this test initially had a bug I caught before
  reporting: placing the blocked flanks flush against the grid corner made the goal genuinely
  unreachable rather than just detour-requiring (no room to route around), which would have
  tested the wrong thing. Fixed by moving the scenario away from the grid edge on an 8×8 board
  so a real detour exists.

## 4. Editor hookup checklist
None — pure C# algorithm, no scene/asset wiring, no consumer yet. The first feature that
actually commands a soldier to move (Units + Selection) will call `AStarPathfinder.FindPath`
and convert the returned cells to world positions via `GridModel.CellCenterToWorld`.

## 5. Deviations
None beyond the scoped-out coroutine/request-queue layer, explained above and recorded in the
decisions log (#25).
