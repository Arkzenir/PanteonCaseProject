using UnityEngine;

namespace CaseGame.Entities
{
    /// <summary>
    /// Shared data for anything placed on the board with a name/image/footprint/HP —
    /// buildings and units both need exactly this shape (brief requirements 3, 9, 10), so it
    /// lives here once instead of being duplicated per concrete definition type. Concrete
    /// subclasses add only what actually differs (<c>BuildingDefinition</c>: producible units;
    /// <c>UnitDefinition</c>: attack damage).
    /// </summary>
    public abstract class GameEntityDefinition : ScriptableObject
    {
        [SerializeField] private string entityName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private int maxHealth = 1;

        public string EntityName => entityName;
        public Sprite Sprite => sprite;
        public Vector2Int Footprint => footprint;
        public int MaxHealth => maxHealth;

        protected virtual void OnValidate()
        {
            footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            maxHealth = Mathf.Max(1, maxHealth);
        }
    }
}
