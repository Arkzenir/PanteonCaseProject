2026-07-23 — Damage/death particle effects

## 1. Summary

Implemented backlog item 19 ("Death/destruction particle effects"), extended per this feature's
explicit request to also cover non-fatal damage: "when an entity is damaged or dies, the particle
system fires the appropriate particles." Uses the actual Unity Particle System (Shuriken), not an
Animator-driven sprite flipbook — both new effect prefabs texture an 8-frame Tiny Swords
`Particle FX` spritesheet (`Dust_01.png` for damage, `Explosion_01.png` for death) through the
Particle System's own Texture Sheet Animation module, so the burst is genuinely animated pack art,
not a static tinted dot.

`GameEntityBase` now subscribes to `Health.Damaged` (alongside its existing `Health.Died`
subscription) and holds two `ParticleSystem` references, deliberately shaped differently:

- **`damageEffect`** — a permanent child on each entity prefab, never destroyed or detached, just
  `.Play()`'d on every non-fatal hit. Zero `Instantiate`/`Destroy` cost per hit, safe across this
  pooled instance's entire lifetime (many "lives" as the same GameObject gets reused).
- **`deathEffectPrefab`** — a prefab *asset* reference, `Instantiate()`d fresh at the entity's
  position the moment it dies, *before* the existing pooling callback runs. This can't be a
  permanent child like the damage effect: `PrefabPool<T>.Release` (which the existing death
  pipeline calls) deactivates the *entire* entity GameObject immediately, which would cut a child
  particle off before it rendered a single frame. The spawned copy is parented under the entity's
  own existing container (`transform.parent` — the same `Buildings`/`Units` container it already
  lived in, so nothing ends up loose at scene root) and self-cleans via its own Particle System's
  "Stop Action = Destroy" — no custom cleanup code, and deliberately *not* pooled (see §5).

## 2. Changes

- `Assets/_Project/Scripts/Runtime/Entities/GameEntityBase.cs` — added `damageEffect`/
  `deathEffectPrefab` `[SerializeField]` fields, a `Health.Damaged` subscription + `HandleDamaged`,
  and `HandleDied` now spawns the death effect before invoking the existing pooling callback.
- `Assets/_Project/Prefabs/Effects/DamageEffect.prefab` (new) — small, brief burst (0.3s), Dust
  spritesheet, warm-tinted, Stop Action `None` (stays alive, reused).
- `Assets/_Project/Prefabs/Effects/DeathEffect.prefab` (new) — bigger, longer burst (0.6s),
  Explosion spritesheet, Stop Action `Destroy` (self-cleans after playing).
- `Assets/_Project/Art/Materials/M_ParticleDamage.mat` / `M_ParticleDeathExplosion.mat` (new) —
  URP `Particles/Unlit`, textured with `Dust_01.png`/`Explosion_01.png`.
- `Assets/_Project/Prefabs/Buildings/Building_Barracks.prefab`, `Building_PowerPlant.prefab`,
  `Assets/_Project/Prefabs/Units/Soldier_1/2/3.prefab` — each gained a nested `DamageEffect`
  child instance and both `GameEntityBase` fields wired.
- `Docs/Agent/ARCHITECTURE.md` — Entities module row updated, new assets noted in §4, decisions
  log entry #76.
- `Docs/Agent/CURRENT_STATUS.md` — "Last report" pointer, "Done" list, backlog (now empty)
  updated.
- (Throwaway, deleted after verification) `Scripts/Editor/Setup/Temp/ParticleEffectsSetup.cs`.

## 3. Test results

No new EditMode tests — this is pure Particle System/prefab wiring plus two trivial null-guarded
event handlers (`HandleDamaged`/the death-effect spawn in `HandleDied`), the same class of
side-effect-only logic Report 033's Animator wiring already established isn't independently
unit-testable in this environment (`ENVIRONMENT.md`'s Awake/Update-doesn't-fire-reliably gotcha).

**Full batchmode pass after wiring and after deleting the throwaway script: 232/232 EditMode
tests passing, 0 compile errors — no regressions.**

**Hand-test:** in Play Mode, attack any unit or building without killing it — confirm a small
warm-toned dust burst plays at the target and the target itself is unaffected otherwise. Then land
a killing blow — confirm a larger explosion burst plays at the death position and disappears on
its own shortly after (no leftover GameObject). Try this on both a unit and a building, and try
killing something that's already mid-attack-animation or moving, to sanity-check the effect still
fires correctly under those conditions.

## 4. Editor hookup checklist

Nothing required — all 5 entity prefabs are already fully wired (verified by reading each
regenerated prefab's `damageEffect`/`deathEffectPrefab` fields and the nested `DamageEffect`
child's prefab-instance linkage back before deleting the throwaway script). Optional tuning, all
single-point-of-change since both effects are nested-prefab templates:

1. To change how either effect *looks* (size, color, burst count, duration, spread), edit
   `Prefabs/Effects/DamageEffect.prefab` or `DeathEffect.prefab` directly — the change propagates
   to all 5 entities automatically via Unity's nested-prefab instancing, no per-entity re-wiring
   needed.
2. Both particle renderers are set to sorting order `5` (same sorting layer as the entity sprites,
   order `0`) so they draw on top — if a future visual layer needs to sit above the particles
   (e.g. a new UI-adjacent world-space element), bump its own sorting order past `5`.
3. Not required now, but worth a glance whenever the already-deferred draw-call verification pass
   (backlog item 20) happens: these effects introduce 2 new distinct materials/textures, so a
   moment where both a damage burst and a death burst are visible simultaneously adds up to 2
   extra batches beyond what's already budgeted — almost certainly harmless against the <20
   SetPass budget, but worth confirming numerically once that pass actually runs.

## 5. Deviations

- The death effect is deliberately **not pooled**, unlike `Projectile`/`ProjectileFactory`
  (decisions log #62's precedent for "short-lived visual, no Health/definition"). Death is a
  once-per-instance-lifetime event, not a sustained-fire case like ranged combat's repeated
  projectile launches — plain `Instantiate` + the Particle System's own built-in
  `Stop Action = Destroy` is the standard, low-risk idiomatic approach for a one-shot VFX at this
  frequency, and the brief's Object Pooling pattern is already demonstrated elsewhere (Projectile,
  the entities themselves, the Production Menu scroll view). See decisions log #76 for the full
  reasoning, including why a pooled `ParticleEffectFactory` was considered and set aside rather
  than dismissed outright.
- Considered (and rejected) detaching-and-reusing the *same* child instance for the death effect,
  matching the damage effect's shape exactly. Traced through and found a real bug: since entities
  are pooled and reused across many "lives," a detached-and-destroyed child would leave that
  `[SerializeField]` reference dangling the *next* time this same pooled GameObject is reused —
  silently breaking the death effect on every life after the first. The fresh-`Instantiate`
  approach never touches the entity's own permanent hierarchy, so this can't happen.
