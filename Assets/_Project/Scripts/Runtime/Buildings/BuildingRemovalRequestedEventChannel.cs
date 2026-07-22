using CaseGame.Events;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Concrete typed event channel (see <see cref="GameEventChannel{T}"/>) carrying "this
    /// building should be removed" — raised by the Information Panel's Remove button.
    /// Deliberately independent of Health/<c>ApplyDamage</c>: manual removal and combat death
    /// are separate triggers that both end up freeing the building's grid footprint and pooled
    /// instance, but through different code paths (see ARCHITECTURE.md decisions log). Placement
    /// subscribes to actually perform the removal; Selection subscribes to clear a stale
    /// selection if the removed building was the one selected.
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingRemovalRequestEvent_New", menuName = "CaseGame/Events/Building Removal Request Event")]
    public class BuildingRemovalRequestedEventChannel : GameEventChannel<BuildingBase>
    {
    }
}
