using System.Collections.Generic;
using CaseGame.Units;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Data for a building type: name, image, footprint (grid cells), max HP, and the list of
    /// units it can produce (empty for buildings like Power Plant that produce nothing —
    /// GI-6/GI-9). Designers add new building types by adding a new asset, not by editing code
    /// (BRIEF.md requirement 2's modularity mandate).
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingDef_New", menuName = "CaseGame/Buildings/Building Definition")]
    public class BuildingDefinition : ScriptableObject
    {
        [SerializeField] private string buildingName;
        [SerializeField] private Sprite sprite;
        [SerializeField] private Vector2Int footprint = Vector2Int.one;
        [SerializeField] private int maxHealth = 1;
        [SerializeField] private List<UnitDefinition> producibleUnits = new List<UnitDefinition>();

        public string BuildingName => buildingName;
        public Sprite Sprite => sprite;
        public Vector2Int Footprint => footprint;
        public int MaxHealth => maxHealth;
        public IReadOnlyList<UnitDefinition> ProducibleUnits => producibleUnits;
        public bool CanProduceUnits => producibleUnits.Count > 0;

        private void OnValidate()
        {
            footprint = new Vector2Int(Mathf.Max(1, footprint.x), Mathf.Max(1, footprint.y));
            maxHealth = Mathf.Max(1, maxHealth);
        }
    }
}
