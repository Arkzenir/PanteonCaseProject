using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// A producible unit type paired with the prefab to spawn for it — the shape
    /// <see cref="UnitFactory.Create"/> needs (definition + prefab), mirroring
    /// <c>BuildingCatalogEntry</c>. Lives on <c>BuildingDefinition.ProducibleUnits</c> so a
    /// building's producible-unit list can resolve straight to a spawnable prefab, not just a
    /// stats asset with no known prefab to instantiate.
    /// </summary>
    [System.Serializable]
    public struct UnitCatalogEntry
    {
        [SerializeField] private UnitDefinition definition;
        [SerializeField] private SoldierBase prefab;

        public UnitCatalogEntry(UnitDefinition definition, SoldierBase prefab)
        {
            this.definition = definition;
            this.prefab = prefab;
        }

        public UnitDefinition Definition => definition;
        public SoldierBase Prefab => prefab;
    }
}
