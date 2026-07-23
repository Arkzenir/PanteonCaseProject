using CaseGame.Entities;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Unit-specific data on top of <see cref="GameEntityDefinition"/>: attack damage and move
    /// speed. The different soldier types are separate instances of this asset (varying attack
    /// damage), not separate code classes — see <see cref="SoldierBase"/>'s doc comment.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDef_New", menuName = "CaseGame/Units/Unit Definition")]
    public class UnitDefinition : GameEntityDefinition
    {
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private bool ranged;
        [SerializeField] private int attackRange = 1;
        [SerializeField] private float attackSpeed = 1f;

        public int AttackDamage => attackDamage;

        /// <summary>Cells per second — a diagonal step costs the same as an orthogonal one (both count as exactly 1 cell), not scaled by world distance. See <see cref="SoldierBase.StepDuration"/>.</summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>Whether attacks fire a tracked projectile (<see cref="SoldierBase.PerformAttack"/>) instead of applying damage instantly. Melee units can still have <see cref="AttackRange"/> &gt; 1 — they just never fire one.</summary>
        public bool Ranged => ranged;

        /// <summary>Attack range in grid cells (Chebyshev distance) — how close the unit must be to its target before it starts attacking. Melee units typically use 1 (adjacent cell); ranged units are usually larger.</summary>
        public int AttackRange => attackRange;

        /// <summary>Attacks per second — a unit attacks its target once every <c>1 / AttackSpeed</c> seconds for as long as the target stays in range and alive. See <see cref="SoldierBase.AttackInterval"/>.</summary>
        public float AttackSpeed => attackSpeed;

        protected override void OnValidate()
        {
            base.OnValidate();
            attackDamage = Mathf.Max(0, attackDamage);
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
            attackRange = Mathf.Max(1, attackRange);
            attackSpeed = Mathf.Max(0.01f, attackSpeed);
        }
    }
}
