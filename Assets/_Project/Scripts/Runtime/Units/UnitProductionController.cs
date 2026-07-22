using UnityEngine;

namespace CaseGame.Units
{
    /// <summary>
    /// Controller: responds to a clicked producible-unit icon on the Information Panel by
    /// spawning that unit at the requested position (GI-4/GI-6/GI-7 — free, instant, spawns at
    /// the producing building's spawn point). Mirrors <c>PlacementController</c>'s
    /// produce-request subscription (decisions log #33) but for units instead of buildings —
    /// units don't go through a ghost/placement flow, they just appear.
    /// </summary>
    public class UnitProductionController : MonoBehaviour
    {
        [SerializeField] private UnitProductionRequestEventChannel produceRequestedChannel;

        private UnitFactory _factory;

        /// <summary>Explicit initialization (not Awake-wired), mirroring the rest of the project's scene-bootstrap-calls-Initialize pattern — <see cref="UnitFactory"/> needs a real container Transform that only exists once the scene's bootstrap runs.</summary>
        public void Initialize(UnitFactory factory)
        {
            _factory = factory;
        }

        private void OnEnable()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Subscribe(HandleProduceRequested);
            }
        }

        private void OnDisable()
        {
            if (produceRequestedChannel != null)
            {
                produceRequestedChannel.Unsubscribe(HandleProduceRequested);
            }
        }

        /// <summary>Public and independent of the channel callback so it's directly testable, same pattern as every other controller in the project.</summary>
        public void HandleProduceRequested(UnitProductionRequest request)
        {
            var instance = _factory.Create(request.Entry.Definition, request.Entry.Prefab);
            instance.transform.position = request.SpawnPosition;
        }
    }
}
