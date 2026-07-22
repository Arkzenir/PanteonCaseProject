using CaseGame.Events;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Concrete typed event channel (see <see cref="GameEventChannel{T}"/>) carrying which
    /// <see cref="BuildingCatalogEntry"/> the player asked to produce. UI.Production raises it
    /// on a click; Placement subscribes and begins placement, and (as of Report 035) Selection
    /// independently subscribes and clears the current selection — none of the three modules
    /// reference each other directly.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingCatalogEntryEvent_New", menuName = "CaseGame/Events/Building Catalog Entry Event")]
    public class BuildingCatalogEntryEventChannel : GameEventChannel<BuildingCatalogEntry>
    {
    }
}
