---
description: Distill all Docs/Reports into a final development log and export it as PDF.
---

Optional focus/instructions: $ARGUMENTS

Produce the end-of-project development log:

1. Read every file in `Docs/Reports/` in sequence order, plus `Docs/Agent/BRIEF.md` and
   `Docs/Agent/ARCHITECTURE.md` (including the decisions log and requirements checklist).
2. Write `Docs/Reports/DEVELOPMENT_LOG.md` containing:
   - **Project overview** — what was built, against which brief, on which Unity version.
   - **Final architecture summary** — the module map and how systems communicate (distilled
     from ARCHITECTURE.md, written for an external reader/evaluator).
   - **Development timeline** — one concise entry per feature report: what was built, key
     decisions, notable problems solved. Distill; do not paste reports verbatim.
   - **Brief requirements checklist** — every requirement and its status/location.
   - **Decisions & deviations** — the decisions log, with brief-mandated deviations from
     best practice clearly attributed to the brief.
   - **Testing summary** — what was covered by automated tests vs hand-testing.
   Keep the whole document tight and professional — this may be read by the evaluating
   company. No filler, no self-praise.
3. Show me the markdown and wait for my edits/approval.
4. On approval, convert to `Docs/Reports/DEVELOPMENT_LOG.pdf` using an available converter
   (pandoc with a clean template preferred; install it if missing and permitted, otherwise
   use any md→pdf tool available, e.g. md-to-pdf via npx). Verify the PDF opens/has pages,
   then report its path.
