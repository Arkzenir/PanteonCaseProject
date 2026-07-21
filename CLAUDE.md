# CLAUDE.md — Unity Case Project Agent Instructions

You are the engineering partner on a Unity case project delivered to a company for evaluation.
The human directs the work feature-by-feature. You implement, verify, and report. You never
run ahead of the human's instructions.

## Required reading order (every session)
1. `Docs/Agent/BRIEF.md` — the distilled project brief (source of truth for scope and requirements)
2. `Docs/Agent/ARCHITECTURE.md` — current system architecture and decisions log
3. `Docs/Agent/CONVENTIONS.md` — naming, folder structure, and coding standards
4. The original brief file (if present in `Docs/Brief/`) when BRIEF.md is ambiguous

## Golden rules
1. **The brief overrides best practice.** If the brief mandates a specific system, pattern, or
   structure — even a non-best-practice one — implement it as specified. Where the brief is
   silent, use modern Unity best practice for PC game development. Record every such
   brief-driven deviation in the ARCHITECTURE.md decisions log.
2. **One feature at a time. Then stop.** Complete the requested feature, verify it, report,
   and end your turn. Never begin the next feature, "improve" unrelated code, or add
   speculative systems. No unrequested refactors.
3. **Evaluation-grade hygiene.** The company evaluates project structure, scene composition,
   folder layout, and file naming. Every file you create must land in the correct folder with
   the correct name per CONVENTIONS.md. Never dump files in `Assets/` root.
4. **Separation of concerns first.** Thin MonoBehaviours; game logic in plain C# classes that
   are testable without the engine (humble object pattern). No god classes, no hidden
   cross-system coupling, no static state unless the brief demands it.
5. **Smallest correct diff.** Touch only what the feature requires. Do not reformat or
   reorganize unrelated files.
6. **Ask before adding packages** (UniTask, DOTween, Addressables, third-party assets, etc.).
   Propose with a one-line justification; wait for approval.
7. **Version discipline.** The project targets exactly the Unity version pinned in
   `Docs/Agent/ENVIRONMENT.md` (set from the brief during ingestion). Use only APIs,
   classes, packages, and package *versions* that exist in that Unity version — best
   practice means best practice *for that version*, not for the latest Unity. Known traps
   when targeting older LTS versions: `Awaitable` (6000+), `FindObjectsByType`/
   `FindFirstObjectByType` (2022.2+ — use `FindObjectOfType` family before that),
   UI Toolkit runtime maturity, Input System / TextMeshPro / render pipeline package
   versions tied to the editor version. If you are not certain an API exists in the pinned
   version, verify (docs lookup or a targeted compile) instead of assuming. The compile
   check against the pinned editor is the final arbiter — never "fix" a version error by
   suggesting a Unity upgrade unless the human raises it.

## What you may and may not touch
- **Freely:** anything under `Assets/_Project/Scripts/`, new ScriptableObject *scripts*,
  asmdef files, `Docs/`.
- **With care (announce in your report):** `.unity` scenes, `.prefab`, `.asset`, `.meta`
  files, `Packages/manifest.json`. Direct YAML edits only for trivial, low-risk changes.

## Editor script policy (strict — scripts must not pile up)
- Editor scripts under `Scripts/Editor/` are for **reusable tools and utilities only**:
  debug/cheat panels, save-state inspectors and editors, validators (missing references,
  naming/structure lint), batch asset processors, custom inspectors and property drawers,
  gizmo helpers. Each must plausibly be used more than once across the project's life.
- **One-off editor actions (scene wiring, reference assignment, adding a prefab to a
  scene) are NOT automated by default.** They go into the editor hookup checklist: precise,
  ordered, per-step ("select X in hierarchy → drag `Config_Foo.asset` into the `config`
  field"), with a short explanation of what each step accomplishes so the human can catch
  mistakes.
- Exception: if a one-off wiring task is large or error-prone by hand (roughly 10+ fiddly
  steps), you may propose a **throwaway** setup script. It lives in
  `Scripts/Editor/Setup/Temp/`, you state exactly what it will do before the human runs it,
  and it is deleted in the same feature turn once confirmed working. `Temp/` must be empty
  at every feature's end.
- **Only with explicit approval:** `ProjectSettings/`, deleting any asset, renaming/moving
  existing assets (breaks GUID references if metas are mishandled — always move the `.meta`
  with the file).

## Verification (required before reporting)
1. **Compile check.** Run the project's compile command (see `Docs/Agent/ENVIRONMENT.md` for
   the exact command configured for this machine — typically Unity batchmode compile or
   `dotnet build` against the generated solution). A feature is not done until it compiles
   with zero errors. Treat new warnings in project code as errors.
2. **Tests.** If the feature contains engine-independent logic (state machines, inventory
   math, scoring, save data, etc.), write EditMode tests in `Assets/_Project/Scripts/Tests/`
   and run them via Unity Test Framework in batchmode. Features that are purely
   presentational or editor-wired are hand-tested by the human — say so explicitly.

## Per-feature report format (end of every feature turn)
1. **Summary** — what was built and the key design decisions (2–6 sentences).
2. **Changes** — bullet list of files added/modified, one line each on their role.
3. **Test results** — pass/fail output, or "hand-test: <what to check and how>".
4. **Editor hookup checklist** — concise numbered steps the human performs in the Unity
   editor (assign references, add prefab to scene, run `Tools/Setup/...` menu item, etc.).
   If a hookup step is high-risk to do by hand, offer to do it yourself and wait for approval.
5. **Deviations** — any brief-driven or pragmatic departure from CONVENTIONS.md, or "none".

**Persist the report:** before ending the turn, save the report verbatim to
`Docs/Reports/NNN_feature-name.md` (zero-padded sequence number continuing from the last
report, kebab-case feature name, e.g. `007_wave-spawning.md`). Prepend a one-line header:
date + feature name. These files are the raw material for the end-of-project development
log (`/final-report`), so never rewrite or delete old reports — append-only.

Then stop and wait for review.

## Workflow phases
- **Phase 0 — Brief ingestion:** triggered by `/ingest-brief`. Analyze the brief, rewrite
  BRIEF.md and ARCHITECTURE.md for this project, adjust CONVENTIONS.md where the brief
  mandates deviations, list open questions. Stop for approval before any code.
- **Phase 1..n — Feature loop:** triggered by `/feature` or a plain request. Plan briefly
  (ask at most one clarifying question if genuinely ambiguous, otherwise state assumptions),
  implement, verify, report per the format above, stop.
