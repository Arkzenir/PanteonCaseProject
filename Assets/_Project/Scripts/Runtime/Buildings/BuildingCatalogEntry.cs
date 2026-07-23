using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// A producible building type paired with the prefab to spawn for it — the same shape
    /// <see cref="BuildingFactory.Create"/> needs (definition + prefab), reused here as a
    /// single value so the Production Menu can list buildings generically and hand off exactly
    /// one payload when the player asks to produce one.
    /// </summary>
    [System.Serializable]
    public struct BuildingCatalogEntry
    {
        [SerializeField] private BuildingDefinition definition;
        [SerializeField] private BuildingBase prefab;

        public BuildingCatalogEntry(BuildingDefinition definition, BuildingBase prefab)
        {
            this.definition = definition;
            this.prefab = prefab;
        }

        public BuildingDefinition Definition => definition;
        public BuildingBase Prefab => prefab;
    }
}
