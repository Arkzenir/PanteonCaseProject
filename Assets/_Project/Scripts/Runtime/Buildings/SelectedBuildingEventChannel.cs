using CaseGame.Events;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Concrete typed event channel (see <see cref="GameEventChannel{T}"/>) carrying which
    /// building is currently selected, or null when nothing/only soldiers are selected.
    /// Selection raises it; the future Information Panel (UI.Info) will subscribe — requirement
    /// 5's "selecting a building shows its image" — with no direct reference between the two.
    /// </summary>
    [CreateAssetMenu(fileName = "SelectedBuildingEvent_New", menuName = "CaseGame/Events/Selected Building Event")]
    public class SelectedBuildingEventChannel : GameEventChannel<BuildingBase>
    {
    }
}
