using System.Collections.Generic;
using CaseGame.Entities;
using CaseGame.Units;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Building-specific data on top of <see cref="GameEntityDefinition"/>: the list of units
    /// it can produce, each paired with the prefab to spawn for it (empty for buildings like
    /// Power Plant that produce nothing). Designers add new building types by adding a new
    /// asset, not by editing code.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingDef_New", menuName = "CaseGame/Buildings/Building Definition")]
    public class BuildingDefinition : GameEntityDefinition
    {
        [SerializeField] private List<UnitCatalogEntry> producibleUnits = new List<UnitCatalogEntry>();

        public IReadOnlyList<UnitCatalogEntry> ProducibleUnits => producibleUnits;
        public bool CanProduceUnits => producibleUnits.Count > 0;
    }
}
