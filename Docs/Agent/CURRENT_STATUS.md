# CURRENT_STATUS.md — Quick Orientation

> This is a pointer, not a source of truth. `ARCHITECTURE.md` (implementation log §5,
> decisions log §6) is authoritative — this file just summarizes where things stand so a
> fresh/compacted session doesn't have to re-derive it. Update this alongside `ARCHITECTURE.md`
> at the end of every feature turn; if the two ever disagree, trust `ARCHITECTURE.md` and fix
> this file. Still read `BRIEF.md` → `ARCHITECTURE.md` → `CONVENTIONS.md` per CLAUDE.md's
> required reading order — this doesn't replace that, it's a fast orientation before it.

**Last report:** 012 (`Units`), 2026-07-21. Compile clean, **70/70 EditMode tests passing**.

**Pending on the human:** nothing outstanding — Report 012's hand-wiring is done (3
`UnitDef_Soldier{1,2,3}` assets + `Soldier1/2/3.prefab`, sprites still unassigned pending an
art pick, which is fine to leave for later). Requirement 19 (Main Menu) remains the one
fully-closed brief requirement; everything else in the checklist is still `[ ]`.

**Modules with real, tested code:** Core (`GameManager`), Grid, Entities (shared
`GameEntityDefinition`/`GameEntityBase`), Combat, Buildings, Units (`SoldierBase`/`Soldier`/
`UnitFactory` — note: 3 soldier *types* are `UnitDefinition` data, not 3 classes, see
decisions log #26), Events, Pooling, Pathfinding.

**Not yet built:** Placement, Selection, UI.Production, UI.Info, actual Gameplay scene
assembly, draw-call/batching verification, Windows build export.

**Recommended next-feature order** (dependency-driven — each step only needs what's already
shipped above it; full reasoning given in chat and the published "Development Dispatch"
artifact on 2026-07-21, not otherwise saved in the repo):
1. ~~Units~~ — done (Report 012).
2. **Placement** — ghost preview, red/valid feedback, commit-to-grid. Only needs Grid + Buildings.
3. **UI.Production** — infinite pooled scroll view; wires to Placement's entry point.
4. **Selection** — left-click select / right-click move-or-attack. Needs Units to exist first.
5. **UI.Info** — Information Panel, listens for the selection event.
6. **Gameplay scene assembly** — wire the brief's 3-area layout together end-to-end.
7. **Draw-call/batching pass** — only measurable once real content exists to profile.
8. **Windows build + `/final-report`**.

**Known environment gotchas** (full detail in `ENVIRONMENT.md`): `Awake`/`OnEnable` don't
reliably fire on `AddComponent`-created objects in this machine's batchmode EditMode test
runner — don't chase that, hand-test lifecycle-bound MonoBehaviours instead. Asmdef
`references` are not transitive. Always verify a throwaway editor script's result by reading
the generated files back, not just trusting a clean exit code.

**Standing conventions to remember** (full detail in `CONVENTIONS.md` overrides table): prefer
typed asset-picker `SerializeField`s over raw strings (see `SceneReference`); SO definition
assets live under `Assets/_Project/ScriptableObjects/GameEntityDefs/<Category>/`, not
`Settings/`; building/unit prefabs are a parent GameObject with child GameObjects per concern
(`Visuals`, `Hitbox`, `SpawnPoint`, ...) — buildings under `Prefabs/Buildings/` named
`Building_<Name>`, soldiers under `Prefabs/Units/` named `Soldier_<N>` (same `<Category>_<Name>`
convention).
