using UnityEngine;
using UnityEngine.Events;

namespace CaseGame.Events
{
    /// <summary>
    /// Humble bridge: subscribes to a <see cref="GameEvent"/> while enabled and forwards it to
    /// a designer-configured <see cref="UnityEvent"/>, so a response (animation, sound, UI
    /// refresh, etc.) can be wired entirely in the Inspector with no script referencing the
    /// raiser.
    /// </summary>
    public class GameEventListener : MonoBehaviour
    {
        [SerializeField] private GameEvent gameEvent;
        [SerializeField] private UnityEvent response;

        private void OnEnable()
        {
            gameEvent.Subscribe(OnEventRaised);
        }

        private void OnDisable()
        {
            gameEvent.Unsubscribe(OnEventRaised);
        }

        private void OnEventRaised()
        {
            response.Invoke();
        }
    }
}
