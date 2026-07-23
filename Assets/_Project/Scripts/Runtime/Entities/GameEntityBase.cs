using System;
using CaseGame.Combat;
using CaseGame.Grid;
using UnityEngine;

namespace CaseGame.Entities
{
    /// <summary>
    /// Shared humble base for anything placed on the board with HP: buildings and units both
    /// need to own a <see cref="Health"/> built from their definition, implement
    /// <see cref="IDamageable"/>, render a sprite, and notify someone on death (see
    /// <see cref="GameEntityDefinition"/> for why this is shared rather than duplicated).
    /// Doesn't know about pooling — <see cref="Initialize"/> takes an optional death callback
    /// so whoever creates the instance (a Factory) decides what "destroyed" means.
    /// </summary>
    public abstract class GameEntityBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private SpriteRenderer outlineRenderer;
        [SerializeField] private ParticleSystem damageEffect;
        [SerializeField] private ParticleSystem deathEffectPrefab;

        private Health _health;
        private Action _onDied;

        public GameEntityDefinition Definition { get; private set; }
        public int MaxHealth => _health.MaxHealth;
        public int CurrentHealth => _health.CurrentHealth;
        public bool IsDead => _health.IsDead;

        public void Initialize(GameEntityDefinition definition, Action onDied = null)
        {
            Definition = definition;
            _onDied = onDied;
            _health = new Health(definition.MaxHealth);
            _health.Died += HandleDied;
            _health.Damaged += HandleDamaged;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.Sprite;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.sprite = definition.Sprite;
                // Reset any leftover selection outline from a previous life of this pooled instance.
                outlineRenderer.enabled = false;
            }

            // Reset any leftover horizontal flip from a previous life of this pooled instance —
            // same pooled-reuse hazard as the outline reset above.
            SetFlippedHorizontally(false);
        }

        public void ApplyDamage(int amount)
        {
            _health.ApplyDamage(amount);
        }

        /// <summary>Keeps the selection outline tracking whichever sprite frame is currently showing — needed because <see cref="Units.SoldierBase"/> soldiers animate <see cref="spriteRenderer"/>'s sprite every frame via their <c>Animator</c>, which would otherwise leave the outline frozen on its initial pose while the real sprite moves. <c>LateUpdate</c>, not <c>Update</c>, so this always runs after the Animator has applied the current frame's sprite. Gated on the outline actually being visible — copying the reference costs nothing extra (same shared atlas texture, same draw call either way), but there's no reason to do it for the common case of an unselected entity.</summary>
        private void LateUpdate()
        {
            if (outlineRenderer != null && outlineRenderer.enabled && spriteRenderer != null)
            {
                outlineRenderer.sprite = spriteRenderer.sprite;
            }
        }

        /// <summary>Visual-only selection feedback (Selection decides *what's* selected; this just renders it) — toggles a dedicated outline child renderer (same sprite, <c>SpriteSelectionOutline</c> shader) rather than tinting the real sprite, so full-color art doesn't get muddied by a color multiply (same reasoning as the Placement ghost's desaturate-then-tint approach).</summary>
        public void SetSelected(bool selected)
        {
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = selected;
            }
        }

        /// <summary>Mirrors both the real sprite and its selection outline horizontally — applied to both renderers together so the outline never drifts out of sync with the sprite it's supposed to trace. A plain protected method, not public: only a subclass decides its own facing (<see cref="Units.SoldierBase"/>); nothing external needs to flip an entity the way <see cref="SetSelected"/> is driven externally by Selection.</summary>
        protected void SetFlippedHorizontally(bool flipped)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = flipped;
            }

            if (outlineRenderer != null)
            {
                outlineRenderer.flipX = flipped;
            }
        }

        /// <summary>Grid cell nearest to <paramref name="fromCell"/> that this entity actually occupies. Default (units): just its own cell. <see cref="Buildings.BuildingBase"/> overrides this to clamp into its multi-cell footprint instead of always reporting one arbitrary corner — lets attack range/approach-pathing (<see cref="Units.SoldierBase"/>) treat buildings and units identically without Units needing a direct reference to Buildings.</summary>
        public virtual Vector2Int GetNearestOccupiedCell(GridModel grid, Vector2Int fromCell)
        {
            return grid.WorldToCell(transform.position);
        }

        /// <summary>Replays the (permanently-owned, never-destroyed) damage-taken burst on every non-fatal hit — a plain child <see cref="ParticleSystem"/>, not pooled/spawned, since it survives this instance's entire pooled lifetime unlike <see cref="deathEffectPrefab"/> below.</summary>
        private void HandleDamaged(int amount)
        {
            if (damageEffect != null)
            {
                damageEffect.Play();
            }
        }

        /// <summary>
        /// Spawns a fresh, independent copy of <see cref="deathEffectPrefab"/> at this instance's
        /// position <em>before</em> the pooling callback below runs. This can't reuse a permanent
        /// child the way <see cref="HandleDamaged"/> does: <c>PrefabPool&lt;T&gt;.Release</c>
        /// (triggered by <see cref="_onDied"/>) deactivates this entire GameObject immediately, which
        /// would cut off a child particle before it rendered a single frame. The spawned copy is
        /// parented under this instance's own <c>transform.parent</c> (the same Buildings/Units
        /// container it already lives in, avoiding a loose root object) and cleans itself up via the
        /// prefab's own Particle System "Stop Action = Destroy" — no pooling needed for a
        /// once-per-lifetime event, unlike the sustained-fire case <see cref="Units.ProjectileFactory"/>
        /// pools.
        /// </summary>
        private void HandleDied()
        {
            if (deathEffectPrefab != null)
            {
                var effect = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity, transform.parent);
                effect.Play();
            }

            _onDied?.Invoke();
            OnEntityDied();
        }

        /// <summary>Extension point for a subclass's own death-time cleanup beyond the pooling callback (e.g. <see cref="CaseGame.Buildings.BuildingBase"/> releasing its grid footprint) — a plain virtual hook rather than an event, since it always targets exactly this instance's current state with no subscription-lifetime/pooled-reuse hazards to manage.</summary>
        protected virtual void OnEntityDied()
        {
        }
    }
}
