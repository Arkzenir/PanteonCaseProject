using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Data for a producible unit type: name, image, footprint, HP, attack damage. This is a
    /// minimal prerequisite for <c>BuildingDefinition</c>'s producible-units list (brief
    /// requirement 2) — the full Units feature (SoldierBase, Soldier1/2/3 behavior,
    /// UnitFactory) lands as its own feature.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitDef_New", menuName = "CaseGame/Units/Unit Definition")]
    public class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string unitName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private int attackDamage = 1;

        public string UnitName => unitName;
        public Sprite Sprite => sprite;
        public Vector2Int Footprint => footprint;
        public int MaxHealth => maxHealth;
        public int AttackDamage => attackDamage;

        private void OnValidate()
        {
            footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            maxHealth = Mathf.Max(1, maxHealth);
            attackDamage = Mathf.Max(0, attackDamage);
        }
    }
}
