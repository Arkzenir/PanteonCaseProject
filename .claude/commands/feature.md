---
description: Implement a single feature from the brief, verify it, report, and stop.
---

Feature request: $ARGUMENTS

Execute one iteration of the feature loop from CLAUDE.md:

1. Re-read `Docs/Agent/BRIEF.md`, `Docs/Agent/ARCHITECTURE.md`, and
   `Docs/Agent/CONVENTIONS.md`. Locate this feature's requirements in BRIEF.md.
2. State a brief plan (files to add/change, pattern used, how it slots into the
   architecture). If something is genuinely ambiguous, ask me ONE question; otherwise state
   your assumptions and proceed.
3. Implement per CONVENTIONS.md, targeting ONLY the Unity version pinned in
   `Docs/Agent/ENVIRONMENT.md` (CLAUDE.md golden rule 7). Thin MonoBehaviours, testable
   plain-C# logic, correct folders and names. Editor wiring goes into the manual hookup
   checklist with per-step explanations; write editor scripts only if they are genuinely
   reusable tools, per the Editor script policy in CLAUDE.md (throwaway setup scripts only
   for large error-prone wiring, deleted same turn).
4. Verify per `Docs/Agent/ENVIRONMENT.md`: compile check, and EditMode tests if the feature
   contains engine-independent logic.
5. Update `Docs/Agent/ARCHITECTURE.md` (module map, checklist, decisions log) to reflect
   the change.
6. Report using the per-feature format in CLAUDE.md: Summary, Changes, Test results,
   Editor hookup checklist, Deviations.
7. Save the report verbatim to `Docs/Reports/NNN_feature-name.md` (next sequence number,
   append-only, never modify past reports).

Then STOP. Do not begin any other feature or refactor.
