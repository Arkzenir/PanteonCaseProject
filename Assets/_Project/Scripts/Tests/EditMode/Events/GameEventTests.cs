using CaseGame.Events;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Events
{
    public class GameEventTests
    {
        private GameEvent _gameEvent;

        [SetUp]
        public void SetUp()
        {
            _gameEvent = ScriptableObject.CreateInstance<GameEvent>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_gameEvent);
        }

        [Test]
        public void Raise_InvokesSubscribedListener()
        {
            var invoked = false;
            _gameEvent.Subscribe(() => invoked = true);

            _gameEvent.Raise();

            Assert.IsTrue(invoked);
        }

        [Test]
        public void Raise_WithNoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _gameEvent.Raise());
        }

        [Test]
        public void Raise_InvokesAllSubscribedListeners()
        {
            var firstInvoked = false;
            var secondInvoked = false;
            _gameEvent.Subscribe(() => firstInvoked = true);
            _gameEvent.Subscribe(() => secondInvoked = true);

            _gameEvent.Raise();

            Assert.IsTrue(firstInvoked);
            Assert.IsTrue(secondInvoked);
        }

        [Test]
        public void Unsubscribe_StopsReceivingFutureRaises()
        {
            var callCount = 0;
            void Listener() => callCount++;

            _gameEvent.Subscribe(Listener);
            _gameEvent.Raise();
            _gameEvent.Unsubscribe(Listener);
            _gameEvent.Raise();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Subscribe_SameListenerTwice_InvokedOnlyOnce()
        {
            var callCount = 0;
            void Listener() => callCount++;

            _gameEvent.Subscribe(Listener);
            _gameEvent.Subscribe(Listener);
            _gameEvent.Raise();

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Listener_UnsubscribingItselfDuringRaise_DoesNotThrowOrSkipOtherListeners()
        {
            var otherInvoked = false;
            void SelfUnsubscribing() => _gameEvent.Unsubscribe(SelfUnsubscribing);
            _gameEvent.Subscribe(SelfUnsubscribing);
            _gameEvent.Subscribe(() => otherInvoked = true);

            Assert.DoesNotThrow(() => _gameEvent.Raise());
            Assert.IsTrue(otherInvoked);
        }
    }
}
