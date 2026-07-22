using System;
using CaseGame.Combat;
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
        private static readonly Color SelectedTint = new Color(1f, 1f, 0.5f);

        [SerializeField] private SpriteRenderer spriteRenderer;

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
                // Reset any leftover selection tint from a previous life of this pooled instance.
                spriteRenderer.color = Color.white;
            }
        }

        public void ApplyDamage(int amount)
        {
            _health.ApplyDamage(amount);
        }

        /// <summary>Visual-only selection feedback (Selection decides *what's* selected; this just renders it) — tints the same sprite every entity already has, so no new prefab child is needed.</summary>
        public void SetSelected(bool selected)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = selected ? SelectedTint : Color.white;
            }
        }

        private void HandleDied()
        {
            _onDied?.Invoke();
        }
    }
}
