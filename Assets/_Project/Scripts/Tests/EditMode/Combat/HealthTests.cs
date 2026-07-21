using System;
using CaseGame.Combat;
using NUnit.Framework;

namespace CaseGame.Tests.EditMode.Combat
{
    public class HealthTests
    {
        [Test]
        public void Constructor_SetsMaxAndCurrentHealth()
        {
            var health = new Health(10);

            Assert.AreEqual(10, health.MaxHealth);
            Assert.AreEqual(10, health.CurrentHealth);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void Constructor_NonPositiveMaxHealth_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(-5));
        }

        [Test]
        public void ApplyDamage_ReducesCurrentHealth()
        {
            var health = new Health(10);

            health.ApplyDamage(4);

            Assert.AreEqual(6, health.CurrentHealth);
        }

        [Test]
        public void ApplyDamage_RaisesDamagedEvent_WithAmountApplied()
        {
            var health = new Health(10);
            var receivedAmount = 0;
            health.Damaged += amount => receivedAmount = amount;

            health.ApplyDamage(3);

            Assert.AreEqual(3, receivedAmount);
        }

        [Test]
        public void ApplyDamage_ReducingToZero_SetsIsDeadAndRaisesDied()
        {
            var health = new Health(10);
            var died = false;
            health.Died += () => died = true;

            health.ApplyDamage(10);

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsTrue(health.IsDead);
            Assert.IsTrue(died);
        }

        [Test]
        public void ApplyDamage_ExceedingRemainingHealth_ClampsAtZero()
        {
            var health = new Health(10);

            health.ApplyDamage(999);

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void ApplyDamage_WhenAlreadyDead_DoesNothingAndDoesNotRaiseEventsAgain()
        {
            var health = new Health(10);
            health.ApplyDamage(10);
            var damagedCallCount = 0;
            var diedCallCount = 0;
            health.Damaged += _ => damagedCallCount++;
            health.Died += () => diedCallCount++;

            health.ApplyDamage(5);

            Assert.AreEqual(0, health.CurrentHealth);
            Assert.AreEqual(0, damagedCallCount);
            Assert.AreEqual(0, diedCallCount);
        }

        [Test]
        public void ApplyDamage_ZeroOrNegativeAmount_DoesNothing()
        {
            var health = new Health(10);
            var damagedCallCount = 0;
            health.Damaged += _ => damagedCallCount++;

            health.ApplyDamage(0);
            health.ApplyDamage(-1);

            Assert.AreEqual(10, health.CurrentHealth);
            Assert.AreEqual(0, damagedCallCount);
        }
    }
}
