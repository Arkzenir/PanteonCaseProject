using CaseGame.Entities;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Unit-specific data on top of <see cref="GameEntityDefinition"/>: attack damage. This is
    /// a minimal prerequisite for <c>BuildingDefinition</c>'s producible-units list (brief
    /// requirement 2) — the full Units feature (SoldierBase, Soldier1/2/3 behavior,
    /// UnitFactory) lands as its own feature.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDef_New", menuName = "CaseGame/Units/Unit Definition")]
    public class UnitDefinition : GameEntityDefinition
    {
        [SerializeField] private int attackDamage = 1;

        public int AttackDamage => attackDamage;

        protected override void OnValidate()
        {
            base.OnValidate();
            attackDamage = Mathf.Max(0, attackDamage);
        }
    }
}
