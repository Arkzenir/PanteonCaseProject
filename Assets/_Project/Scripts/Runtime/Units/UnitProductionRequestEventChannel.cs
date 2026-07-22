using CaseGame.Events;
using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Concrete typed event channel (see <see cref="GameEventChannel{T}"/>) carrying a
    /// <see cref="UnitProductionRequest"/>. The Information Panel's producible-unit icons raise
    /// it; <see cref="UnitProductionController"/> subscribes and spawns the unit — neither
    /// references the other directly.
    /// </summary>
    [CreateAssetMenu(fileName = "UnitProductionRequestEvent_New", menuName = "CaseGame/Events/Unit Production Request Event")]
    public class UnitProductionRequestEventChannel : GameEventChannel<UnitProductionRequest>
    {
    }
}
