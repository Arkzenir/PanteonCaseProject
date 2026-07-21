# ARCHITECTURE.md — System Architecture (rewritten per brief)

> TEMPLATE. The agent rewrites this file completely during brief ingestion and keeps it
> current as features land. This file is the map an evaluator (or the agent in a fresh
> session) uses to understand the whole project.

## 1. High-level overview
_2–4 sentences: what the game/demo is, the core loop, and the architectural style chosen
(and why it fits the brief)._

## 2. Module map
_One entry per system. Keep each to 3–5 lines._

| Module | Namespace | Responsibility | Depends on |
|---|---|---|---|
| _e.g. Combat_ | `CaseGame.Combat` | _damage, health, hit resolution_ | _Core, Events_ |

## 3. Data flow & communication
_How systems talk: which events exist, who raises them, who listens. Which data lives in
ScriptableObjects vs runtime state. Save/load path if any._

## 4. Scene & prefab composition
_Scene list with purpose and load flow. Key prefabs and what wires them together._

## 5. Brief-mandated requirements checklist
_Every architectural/design requirement extracted from the brief, checked off as
implemented. This is the agent's contract — nothing ships unimplemented, nothing extra
gets invented._

- [ ] _requirement — status / where implemented_

## 6. Decisions log
_Append-only. One row per significant decision, especially brief-driven deviations from
best practice._

| # | Decision | Reason | Alternatives rejected |
|---|---|---|---|
| 1 | | | |
