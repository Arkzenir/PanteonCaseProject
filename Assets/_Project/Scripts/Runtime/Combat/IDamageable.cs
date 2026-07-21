namespace CaseGame.Combat
{
    /// <summary>Contract for anything that has HP and can be damaged/destroyed (soldiers, buildings).</summary>
    public interface IDamageable
    {
        int MaxHealth { get; }
        int CurrentHealth { get; }
        bool IsDead { get; }

        void ApplyDamage(int amount);
    }
}
