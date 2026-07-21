using System;
using CaseGame.Combat;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Humble base for a placed building instance: owns a <see cref="Health"/> constructed
    /// from its <see cref="BuildingDefinition"/>, forwards damage to it, and renders the
    /// definition's sprite. Concrete subclasses (<see cref="Barracks"/>, <see cref="PowerPlant"/>)
    /// add only what differs between building types (OOP inheritance/polymorphism, per the
    /// brief's DESIGN section). Doesn't know about pooling — <see cref="Initialize"/> takes an
    /// optional death callback so whoever creates the instance (<see cref="BuildingFactory"/>)
    /// decides what "destroyed" means, without this class needing to know.
    /// </summary>
    public abstract class BuildingBase : MonoBehaviour, IDamageable
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Health _health;
        private Action _onDied;

        public BuildingDefinition Definition { get; private set; }
        public int MaxHealth => _health.MaxHealth;
        public int CurrentHealth => _health.CurrentHealth;
        public bool IsDead => _health.IsDead;

        public void Initialize(BuildingDefinition definition, Action onDied = null)
        {
            Definition = definition;
            _onDied = onDied;
            _health = new Health(definition.MaxHealth);
            _health.Died += HandleDied;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = definition.Sprite;
            }
        }

        public void ApplyDamage(int amount)
        {
            _health.ApplyDamage(amount);
        }

        private void HandleDied()
        {
            _onDied?.Invoke();
        }
    }
}
