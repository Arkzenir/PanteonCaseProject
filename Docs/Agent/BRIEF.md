# BRIEF.md — Distilled Project Brief

**Original brief location:** `Docs/Brief/Panteon_GameDeveloperDemoProject2026.pdf`

## What is being built
A 2D Windows strategy-game demo in Unity 2021 LTS demonstrating core RTS mechanics:
building placement/production, unit (soldier) production, and commanding units to move
across the map along a pathfound route, with basic combat and destruction. Delivered as a
playable Windows build plus a reviewed source project (Bitbucket/GitHub).

## Hard requirements (must ship)
Numbers reference the brief's "GENERAL INFORMATION" numbered list unless noted otherwise.

1. Build for **Unity 2021 LTS**, **2D**, playable **Windows build** included in submission. (Hello/GI intro)
2. Production Menu shows at least **Barracks**, **Power Plant**, and **Soldier Units** as
   producible/related entries. **Only these types ship**; the human confirmed the "besides..."
   wording is about system extensibility, not extra content. The human additionally required
   that **every layer stay modular/extensible for future building & unit types — not just the
   data layer**: the Production Menu UI must iterate over building definitions generically
   (no per-type hardcoded UI branches), and a building's list of producible units must be
   data-driven (`BuildingDefinition` references a list of `UnitDefinition`s) rather than
   switch-cased per building type. See decision log entry in ARCHITECTURE.md. (GI-1, GI-2)
3. Buildings are placed on the game board at a **user-selected location**; the user must be
   **visually informed when the location is invalid** (e.g., a red highlight/ghost). Buildings
   have a **name**, an **image**, and **dimensions** (footprint), given in grid cells. Human
   guidance from the UI mockup and follow-up correction:
   - Grid **cell size is not fixed** — it's an editable value (a `GridDefinition` SO field),
     to be set to whatever X×X pixel size fits the actual art once visuals are chosen. The
     mockup's 32×32px was illustrative, not a locked spec.
   - Footprints are **per-type editable values on each definition** (`BuildingDefinition`/
     `UnitDefinition`), not hardcoded — a designer must be able to change them without code.
     Starting values from the mockup: Barracks → 4×4 cells, Power Plant → 2×3 cells,
     Soldier → 1×1 cell. (GI-3)
4. Production has **no production time and no quantity cap** — clicking "produce" instantiates
   immediately, as many times as the user wants. Applies to barracks, power plants, and
   military units. **No resource/currency cost system is implied or required.** (GI-4)
5. Selecting a building on the game board shows its **image on the Information Panel**. If the
   building can produce units, the **images of its producible units** are also listed there. (GI-5)
6. **Only Barracks produce units** (Soldiers). Power Plant has no products, so it needs no
   production sub-menu. (GI-6, GI-9)
7. Soldiers produced from a Barracks **spawn at a designated spawn point** belonging to that
   barracks. (GI-7)
8. Unit movement: **left-click selects unit(s)**, **right-click commands them to a map point**.
   They must travel via the **shortest path** and **must route around buildings** ("wander
   around the buildings") rather than through them, during the journey. (GI-7)
9. **3 soldier types**, all with **10 HP**, differing only in attack damage:
   - Soldier 1 → 10 damage/attack
   - Soldier 2 → 5 damage/attack
   - Soldier 3 → 2 damage/attack (GI-8)
10. **Building HP:** Barracks = 100 HP, Power Plant = 50 HP. (GI-9)
11. Selected soldier(s) **attack via right-click on a target unit or building**. (GI-10)
12. Units and buildings are **destroyed when HP reaches 0**. (GI-11)
13. **Draw calls (SetPass calls) must stay under 20**, achieved via **batching and GPU
    instancing**. (GI-12)
14. UI/game must **work correctly across different aspect ratios and resolutions**. (GI-13)
15. Interface has exactly three areas (see reference layout sketch in the brief):
    - **Production Menu** — an **infinite scroll view** (object-pooled) listing producible buildings.
    - **Game Board** — grid-style area where placed buildings and units are displayed.
    - **Information Panel** — shows the currently selected unit's/building's info.
16. Code must be **legible and standards-compliant**: naming, scalability, comments where
    warranted. (GI-14)
17. **Scene structure, GameObject naming, and folder structure** are reviewed alongside code
    — evaluation-grade hygiene is a hard requirement, not a nicety. (GI-15)
18. Design must **account for edge cases**; the project will be examined in detail. (GI-16)
19. A **basic Main Menu** with a **Play button** and a **Settings screen**. Human-mandated,
    tied to GI-13 (aspect ratio/resolution support): the settings screen is how resolution
    switching is demonstrated to the evaluator. Keep it minimal — Play + Settings
    (resolution/display-mode) only; no other menu features are implied.

## Mandated systems, patterns, or structures
Straight from the brief's "DESIGN" section — **all of the following must be demonstrably
used somewhere in the project**, not just technically possible:

- **OOP:** Polymorphism, Inheritance — building/unit type hierarchies.
- **S.O.L.I.D.** principles throughout.
- **Design Patterns:** **Factory** (building/unit creation) and **Singleton** (explicitly named
  — see [[architecture-decisions]] for how this is reconciled with the baseline
  anti-singleton stance in CONVENTIONS.md).
- **MVC** — UI and game logic must be separated (brief's own wording: "UI and Logic should
  be separated from each other using techniques like MVC").
- **Draw Call** awareness/optimization (ties to requirement 13 above).
- **Object Pooling** — explicitly required for the infinite scroll view (UX section:
  "Infinite Scrollview — Object Pooling"), and the natural fit for frequently
  spawned/destroyed units and placed buildings.
- **Coroutine** usage (e.g., movement/attack sequencing, scroll view or feedback timing).
- **Events** — decoupled communication between systems (selection → info panel, HP → death, etc.).
- **Platform:** 2D.
- **Algorithm:** **A\*** pathfinding for unit movement (custom grid-based implementation,
  not Unity NavMesh — NavMesh is never mentioned and would pull in an unrequested package).

## Evaluation criteria
Explicitly stated or strongly implied by the brief:
- Correct, complete implementation of every "GENERAL INFORMATION" requirement above.
- Demonstrated, correct use of every item in the DESIGN section (not just present in name).
- Draw call count < 20 (measurable, explicit).
- Aspect-ratio/resolution robustness.
- Code legibility: naming, scalability, comments.
- Project layout: scene structure, GameObject naming, folder structure.
- Handling of edge cases ("the project will be examined in detail").

## Constraints
- **Engine/version:** Unity **2021 LTS** (pinned to `2021.3.45f2`, already installed — see
  `Docs/Agent/ENVIRONMENT.md`; this satisfies "2021 LTS" and needs no version change).
- **Platform:** Windows standalone, 2D.
- **Packages/assets:** none mandated by the brief; anything beyond built-in Unity requires
  approval per CLAUDE.md golden rule 6.
- **Delivery:** complete project shared via Bitbucket or GitHub, link emailed to
  `hr@panteon.games`, plus a playable Windows build included in the submission. (Sending the
  email and creating any remote repo/build distribution step requires the human — Claude
  will not do this.)
- No time budget or art constraints specified.

## Explicitly out of scope / do not build
- No resource/currency economy (production is free and instant per requirement 4).
- No production queue/timers — production is instantaneous, so no progress bars or queue UI.
- No mention of enemy AI/opposing faction — **human confirmed there is no opposing AI
  faction to fight.** Combat (right-click attack) is a demonstrable mechanic against any
  legal target (including the player's own units/buildings), not a win/lose battle system.
- No save/load system mentioned — do not build one.
- No meta-progression, pause menu, audio settings, or other menu features beyond
  **Play + a resolution/display-mode Settings screen** (human-mandated, requirement 19) —
  do not gold-plate the main menu.
- No NavMesh — pathfinding is custom A* per the brief's explicit algorithm choice.

## Open questions
All Phase 0 open questions are resolved by the human:
1. **Building/unit extensibility scope** — only Barracks + Power Plant + the 3 soldier types
   ship, but every layer (data, factories, Production Menu UI, per-building producible-unit
   lists) must be modular/extensible for future types. See requirement 2 above.
2. **No opposing AI faction.** Confirmed — see "Explicitly out of scope" above.
3. **Game Board is a grid**, per the human-supplied UI mockup (`Docs/Brief/` — layout: Production
   Menu | Game Board | Information Panel, matching Interface requirement 15). The mockup's
   grid *drawing* (row/column count) is illustrative only, not literal — actual cell count is
   whatever fits the game's visuals. **Cell size and building/unit footprints are editable
   values, not fixed constants** — see requirement 3 above (human correction: cell size is
   "any X by X pixel value, decided depending on the visual used"; footprints "should be
   easily modifiable by a designer").
4. **Main Menu required** — Play button + Settings screen (resolution/display-mode), to let
   the evaluator exercise GI-13 (aspect ratio/resolution robustness) directly. See requirement
   19 above; scene flow is now `Boot.unity` → `MainMenu.unity` → `Gameplay.unity`.
