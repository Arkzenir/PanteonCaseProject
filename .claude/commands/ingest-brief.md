---
description: Analyze the case brief and rewrite the project docs (Phase 0). No code.
---

The case brief is located at: $ARGUMENTS
(If no path was given, look in `Docs/Brief/` and confirm which file is the brief.)

Perform Phase 0 exactly:

1. Read the ENTIRE brief carefully. Read `CLAUDE.md`, `Docs/Agent/CONVENTIONS.md`,
   `Docs/Agent/ARCHITECTURE.md`, `Docs/Agent/BRIEF.md`.
2. Rewrite `Docs/Agent/BRIEF.md` from its template: extract every hard requirement,
   every mandated system/pattern/structure, evaluation criteria, constraints, and out-of-scope
   items. Every requirement must be traceable to the brief — do not invent scope.
3. Rewrite `Docs/Agent/ARCHITECTURE.md` from its template: propose the module map, data
   flow, and scene composition that satisfies the brief. Where the brief mandates an
   approach, use it and log it in the decisions table; where it is silent, use best practice.
4. Update the "Project-specific overrides" section of `Docs/Agent/CONVENTIONS.md` with any
   naming/structure/pattern rules the brief dictates. Do not touch the baseline sections.
4b. Extract the required Unity version (and any required package/pipeline versions) from
   the brief and write them into `Docs/Agent/ENVIRONMENT.md` as the pinned version. If the
   brief specifies a version and ENVIRONMENT.md already lists a different installed editor,
   flag the mismatch as a blocking open question. If the brief is silent on version, ask me
   which installed version to pin. All subsequent code targets the pinned version's API
   surface (CLAUDE.md golden rule 7).
5. Output to me: a short summary of the project, the proposed architecture in ~10 lines,
   the list of brief-mandated deviations from best practice, and all open questions.

Do NOT write any C# or create any assets. Stop after the summary and wait for my approval
or corrections.
