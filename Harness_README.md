# Unity Agent Harness

A Claude Code harness for developing Unity case projects step-by-step: you direct features,
the agent implements to evaluation-grade standards, verifies compilation/tests, and hands
you a concise editor hookup checklist after every step. Built for briefs that will be
evaluated on architecture, project structure, and naming — and for briefs that mandate
specific (even non-best-practice) approaches, which always override defaults.

## What's in the box

```
CLAUDE.md                         Master agent instructions: golden rules, workflow,
                                  editor-script policy, report format. Auto-loaded by
                                  Claude Code every session.
.claude/commands/
  ingest-brief.md                 /ingest-brief — Phase 0: analyze brief, rewrite docs, stop.
  feature.md                      /feature — one feature: plan, build, verify, report, stop.
  final-report.md                 /final-report — distill all reports into a PDF dev log.
Docs/Agent/
  BRIEF.md                        Template → distilled brief (source of truth for scope).
  ARCHITECTURE.md                 Template → module map, data flow, decisions log.
  CONVENTIONS.md                  Baseline naming/folders/code standards + per-brief overrides.
  ENVIRONMENT.md                  Machine-specific: pinned Unity version, compile/test commands.
Docs/Brief/                       Drop the company's original brief file(s) here.
Docs/Reports/                     Auto-populated: one report per feature, append-only.
.gitignore                        Unity + Rider + Claude Code ignores (kit files stay tracked).
README.md                         This file.
```

## Prerequisites

- **Unity** — the editor version required by your brief, installed via Unity Hub.
- **Claude Code** — install per https://docs.claude.com/en/docs/claude-code/overview and
  log in (`claude` in any terminal; works with a Claude subscription or Console account).
- **.NET SDK** (optional but recommended) — enables the fast `dotnet build` compile check
  without closing the Unity editor.
- **JetBrains Rider** (optional) — install the "Claude Code" plugin from the JetBrains
  Marketplace and restart Rider. Then run `claude` in Rider's integrated terminal (or `/ide`
  from an external terminal) to get in-IDE diffs and automatic sharing of Rider's
  Unity-aware diagnostics with the agent. If IDE detection fails (a known intermittent
  issue on some version combos), everything still works from the terminal.

## Setup (fresh from this repo)

1. **Create the Unity project** in Unity Hub with the brief's required version. Close Unity.
2. **Pull this harness into the project root** (same level as `Assets/` and `Packages/`):
   ```
   git clone <this-repo> tmp-harness
   cp -r tmp-harness/CLAUDE.md tmp-harness/.claude tmp-harness/Docs tmp-harness/.gitignore .
   rm -rf tmp-harness
   ```
   (Or use this repo as a template and create the Unity project inside it — either way,
   `CLAUDE.md` must sit at the repo root Claude Code is launched from.)
3. **Init version control** if not already: `git init && git add -A && git commit -m "Bootstrap project + agent harness"`.
   In Unity: Edit → Project Settings → Editor → set *Version Control: Visible Meta Files*
   and *Asset Serialization: Force Text* (usually the default).
4. **Fill in `Docs/Agent/ENVIRONMENT.md`** — Unity editor path, project path, and (after
   first open) the key package versions from `Packages/manifest.json`. The Unity version
   pin gets confirmed/overwritten from the brief during ingestion.
5. **Drop the brief** (PDF/docx/md) into `Docs/Brief/`.
6. **Start the agent** from the project root: `claude` (in Rider's terminal for IDE
   integration). Recommended: run `/permissions` once and allow the compile/test commands
   from ENVIRONMENT.md so verification doesn't prompt every time.

## Usage

### Phase 0 — ingest the brief (once per project)
```
/ingest-brief Docs/Brief/case-brief.pdf
```
The agent reads the entire brief, rewrites `BRIEF.md` and `ARCHITECTURE.md`, records any
brief-mandated deviations in `CONVENTIONS.md` overrides and the decisions log, pins the
Unity version, and stops with a summary + open questions. **Review this carefully** —
answer the open questions and correct anything before writing code. No code is written in
this phase.

### Feature loop (repeat)
```
/feature player movement with the acceleration curve from section 3 of the brief
```
(Plain chat requests work too; the slash command just enforces the full loop.)
Each iteration: brief plan (with stated assumptions) → implementation per conventions →
compile check → EditMode tests where the feature has engine-independent logic → report:

1. Summary and design decisions
2. Files changed
3. Test results (or what to hand-test and how)
4. **Editor hookup checklist** — numbered manual steps with explanations
5. Deviations from conventions (or "none")

The report is saved to `Docs/Reports/NNN_feature.md` and the agent **stops**. Do the
hookup steps in Unity, hand-test, review the diff (Rider's diff viewer if connected),
commit, then request the next feature. If Unity's console shows errors after script
reload, paste them back — the agent fixes before you move on.

### End of project
```
/final-report
```
Distills all feature reports + the architecture doc into `Docs/Reports/DEVELOPMENT_LOG.md`
for your approval, then exports `DEVELOPMENT_LOG.pdf`.

## Behavior contract (enforced by CLAUDE.md — the short version)

- **Brief overrides best practice.** Mandated systems are implemented as written and
  logged as decisions; everything else uses modern best practice *for the pinned Unity
  version only*.
- **One feature per turn, then stop.** No speculative work, no unrequested refactors,
  smallest correct diff.
- **Editor scripts are reusable tools only** (validators, save-state inspectors, debug
  panels). One-off scene wiring is done by you via checklist; a throwaway setup script is
  offered only for large error-prone wiring and deleted the same turn.
- **Packages, ProjectSettings changes, deletions, and asset renames require your approval.**
- **Nothing lands outside the convention structure** (`Assets/_Project/...`, naming table
  in `CONVENTIONS.md`).

Edit `Docs/Agent/CONVENTIONS.md` to taste before your first project — the agent treats it
as law.

## Tips & troubleshooting

- **Compile check while Unity is open:** Unity locks the project to one editor instance,
  so batchmode fails. The agent falls back to `dotnet build` (fast, C#-only) and asks you
  to confirm the console is clean. Close Unity when you want authoritative batchmode
  compiles or automated test runs.
- **Stale solution:** after the agent adds asmdefs/scripts, Rider regenerates the `.sln`
  on focus; if `dotnet build` complains about missing projects, focus Rider once or let
  the agent use batchmode.
- **Fresh session / context reset:** the agent re-reads `BRIEF.md`, `ARCHITECTURE.md`,
  `CONVENTIONS.md` at session start, and `Docs/Reports/` provides full history — safe to
  `/clear` between features on long projects.
- **Rider `/ide` doesn't detect the IDE:** ensure the plugin is installed/enabled and
  Rider fully restarted; if it still fails, continue in the terminal — only the diff
  viewer and diagnostics sharing are lost.
- **Old Unity versions (2021/2023 LTS):** the version-discipline rule prevents most
  newer-API usage, but expect the occasional one-round-trip compile fix on obscure APIs —
  the pinned-editor compile is the backstop. For very old targets, add a "known
  unavailable" list to CONVENTIONS.md overrides during ingestion.
- **Commit cadence:** one commit per feature keeps the evaluator-facing history clean and
  makes rollback trivial if a feature review fails.
