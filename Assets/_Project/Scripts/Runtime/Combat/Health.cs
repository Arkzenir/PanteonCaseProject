using System;

namespace CaseGame.Combat
{
    /// <summary>
    /// Plain C# HP state and damage resolution for a single unit/building instance. Not a
    /// MonoBehaviour — owned/constructed by whichever humble component represents that
    /// instance (a future SoldierController/BuildingController), which forwards
    /// <see cref="Died"/> to pooling/destruction and <see cref="Damaged"/> to any view update.
    /// These are plain per-instance C# events rather than an Events-module
    /// <c>GameEventChannel&lt;T&gt;</c>: every unit/building has its own <see cref="Health"/>,
    /// so a shared/global channel would broadcast every instance's changes to every listener,
    /// which isn't what's needed here.
    /// </summary>
    public class Health : IDamageable
    {
        public event Action<int> Damaged;
        public event Action Died;

        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }

        public Health(int maxHealth)
        {
            if (maxHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHealth), "Max health must be positive.");
            }

            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void ApplyDamage(int amount)
        {
            if (amount <= 0 || IsDead)
            {
                return;
            }

            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            Damaged?.Invoke(amount);

            if (CurrentHealth == 0)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }
    }
}
