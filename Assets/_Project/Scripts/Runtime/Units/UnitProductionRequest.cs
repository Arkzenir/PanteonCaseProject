using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// "Produce this unit, spawn it here" — raised by a clicked producible-unit icon on the
    /// Information Panel. Carries the spawn position directly rather than requiring the
    /// listener to look up "whichever building is currently selected": the Info Panel already
    /// knows which building's row was clicked, so bundling it here keeps <c>UnitProductionController</c>
    /// (in <c>CaseGame.Units</c>) from needing a dependency on Selection/Buildings just to answer
    /// "where does this spawn" (that would be a back-reference — Selection already depends on
    /// Units, not the other way around).
    /// </summary>
    public readonly struct UnitProductionRequest
    {
        public UnitProductionRequest(UnitCatalogEntry entry, Vector3 spawnPosition)
        {
            Entry = entry;
            SpawnPosition = spawnPosition;
        }

        public UnitCatalogEntry Entry { get; }
        public Vector3 SpawnPosition { get; }
    }
}
