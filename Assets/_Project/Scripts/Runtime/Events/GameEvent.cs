using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaseGame.Events
{
    /// <summary>
    /// A parameterless, designer-wireable signal channel: raisers and listeners both just
    /// reference the same asset instance, with no direct reference to each other. Use a
    /// concrete <see cref="GameEventChannel{T}"/> subclass instead when a payload is needed.
    /// </summary>
    [CreateAssetMenu(fileName = "GameEvent_New", menuName = "CaseGame/Events/Game Event")]
    public class GameEvent : ScriptableObject
    {
        private readonly List<Action> _listeners = new List<Action>();

        public void Raise()
        {
            for (var i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke();
            }
        }

        public void Subscribe(Action listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void Unsubscribe(Action listener)
        {
            _listeners.Remove(listener);
        }
    }
}
