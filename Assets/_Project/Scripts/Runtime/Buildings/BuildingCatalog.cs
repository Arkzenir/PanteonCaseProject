using System.Collections.Generic;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// The ordered list of buildings the Production Menu offers. Designers add a new producible
    /// building by adding an entry to this asset — no code change, no new UI branch needed.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingCatalog_New", menuName = "CaseGame/Buildings/Building Catalog")]
    public class BuildingCatalog : ScriptableObject
    {
        [SerializeField] private List<BuildingCatalogEntry> entries = new List<BuildingCatalogEntry>();

        public IReadOnlyList<BuildingCatalogEntry> Entries => entries;
    }
}
