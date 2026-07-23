using CaseGame.Events;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Concrete typed event channel (see <see cref="GameEventChannel{T}"/>) carrying which
    /// <see cref="BuildingCatalogEntry"/> the player asked to produce. The Production UI raises
    /// it on a click; Placement subscribes and begins placement, and Selection independently
    /// subscribes to clear the current selection — none of the three modules reference each
    /// other directly.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingCatalogEntryEvent_New", menuName = "CaseGame/Events/Building Catalog Entry Event")]
    public class BuildingCatalogEntryEventChannel : GameEventChannel<BuildingCatalogEntry>
    {
    }
}
