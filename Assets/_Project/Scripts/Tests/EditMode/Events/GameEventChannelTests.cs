using CaseGame.Events;
using NUnit.Framework;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Events
{
    public class GameEventChannelTests
    {
        private class TestIntEventChannel : GameEventChannel<int>
        {
        }

        private TestIntEventChannel _channel;

        [SetUp]
        public void SetUp()
        {
            _channel = ScriptableObject.CreateInstance<TestIntEventChannel>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_channel);
        }

        [Test]
        public void Raise_PassesPayloadToListener()
        {
            var received = 0;
            _channel.Subscribe(value => received = value);

            _channel.Raise(42);

            Assert.AreEqual(42, received);
        }

        [Test]
        public void Raise_WithNoListeners_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _channel.Raise(1));
        }

        [Test]
        public void Unsubscribe_StopsReceivingFutureRaises()
        {
            var callCount = 0;
            void Listener(int _) => callCount++;

            _channel.Subscribe(Listener);
            _channel.Raise(1);
            _channel.Unsubscribe(Listener);
            _channel.Raise(1);

            Assert.AreEqual(1, callCount);
        }

        [Test]
        public void Subscribe_SameListenerTwice_InvokedOnlyOnce()
        {
            var callCount = 0;
            void Listener(int _) => callCount++;

            _channel.Subscribe(Listener);
            _channel.Subscribe(Listener);
            _channel.Raise(1);

            Assert.AreEqual(1, callCount);
        }
    }
}
