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
        }

        public void ApplyDamage(int amount)
        {
            _health.ApplyDamage(amount);
        }

        /// <summary>Visual-only selection feedback (Selection decides *what's* selected; this just renders it) — toggles a dedicated outline child renderer (same sprite, <c>SpriteSelectionOutline</c> shader) rather than tinting the real sprite, so full-color art doesn't get muddied by a color multiply (same reasoning as the Placement ghost's desaturate-then-tint approach).</summary>
        public void SetSelected(bool selected)
        {
            if (outlineRenderer != null)
            {
                outlineRenderer.enabled = selected;
            }
        }

        /// <summary>Grid cell nearest to <paramref name="fromCell"/> that this entity actually occupies. Default (units): just its own cell. <see cref="Buildings.BuildingBase"/> overrides this to clamp into its multi-cell footprint instead of always reporting one arbitrary corner — lets attack range/approach-pathing (<see cref="Units.SoldierBase"/>) treat buildings and units identically without a Units→Buildings reference (circular-dependency constraint, see ARCHITECTURE.md decisions log).</summary>
        public virtual Vector2Int GetNearestOccupiedCell(GridModel grid, Vector2Int fromCell)
        {
            return grid.WorldToCell(transform.position);
        }

        private void HandleDied()
        {
            _onDied?.Invoke();
            OnEntityDied();
        }

        /// <summary>Extension point for a subclass's own death-time cleanup beyond the pooling callback (e.g. <see cref="CaseGame.Buildings.BuildingBase"/> releasing its grid footprint) — a plain virtual hook rather than an event, since it always targets exactly this instance's current state with no subscription-lifetime/pooled-reuse hazards to manage.</summary>
        protected virtual void OnEntityDied()
        {
        }
    }
}
