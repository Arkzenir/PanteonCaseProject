using CaseGame.Entities;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Unit-specific data on top of <see cref="GameEntityDefinition"/>: attack damage and move
    /// speed. The brief's 3 soldier types (10/5/2 attack damage, GI-9) are 3 instances of this
    /// asset, not 3 code classes — see <see cref="SoldierBase"/>'s doc comment.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDef_New", menuName = "CaseGame/Units/Unit Definition")]
    public class UnitDefinition : GameEntityDefinition
    {
        [SerializeField] private int attackDamage = 1;
        [SerializeField] private float moveSpeed = 3f;

        public int AttackDamage => attackDamage;

        /// <summary>Cells per second — a diagonal step costs the same as an orthogonal one (both count as exactly 1 cell), not scaled by world distance. See <see cref="SoldierBase.StepDuration"/>.</summary>
        public float MoveSpeed => moveSpeed;

        protected override void OnValidate()
        {
            base.OnValidate();
            attackDamage = Mathf.Max(0, attackDamage);
            moveSpeed = Mathf.Max(0.01f, moveSpeed);
        }
    }
}
