using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaseGame.Events
{
    /// <summary>
    /// Generic base for a designer-wireable, typed event channel ScriptableObject: raisers and
    /// listeners both just reference the same asset, with no direct reference to each other.
    /// Unity can't <c>[CreateAssetMenu]</c> an open generic type, so a concrete payload channel
    /// (e.g. a <c>BuildingDefinition</c> channel for Selection → Info Panel) subclasses this
    /// with a specific <typeparamref name="T"/> once that payload type actually exists.
    /// </summary>
    public abstract class GameEventChannel<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new List<Action<T>>();

        public void Raise(T value)
        {
            // Iterate back-to-front so a listener unsubscribing itself mid-callback doesn't
            // skip or corrupt the walk.
            for (var i = _listeners.Count - 1; i >= 0; i--)
            {
                _listeners[i]?.Invoke(value);
            }
        }

        public void Subscribe(Action<T> listener)
        {
            if (!_listeners.Contains(listener))
            {
                _listeners.Add(listener);
            }
        }

        public void Unsubscribe(Action<T> listener)
        {
            _listeners.Remove(listener);
        }
    }
}
