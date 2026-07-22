using CaseGame.Entities;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Entities
{
    public class GameEntityBaseTests
    {
        private class TestDefinition : GameEntityDefinition
        {
        }

        private class TestEntity : GameEntityBase
        {
        }

        private static TestDefinition CreateDefinition(int maxHealth)
        {
            var definition = ScriptableObject.CreateInstance<TestDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = maxHealth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        [Test]
        public void Initialize_SetsDefinitionAndHealthFromDefinition()
        {
            var go = new GameObject("Entity");
            var entity = go.AddComponent<TestEntity>();
            var definition = CreateDefinition(50);

            entity.Initialize(definition);

            Assert.AreSame(definition, entity.Definition);
            Assert.AreEqual(50, entity.MaxHealth);
            Assert.AreEqual(50, entity.CurrentHealth);
            Assert.IsFalse(entity.IsDead);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ApplyDamage_ForwardsToHealth_ReducesCurrentHealth()
        {
            var go = new GameObject("Entity");
            var entity = go.AddComponent<TestEntity>();
            var definition = CreateDefinition(50);
            entity.Initialize(definition);

            entity.ApplyDamage(20);

            Assert.AreEqual(30, entity.CurrentHealth);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ApplyDamage_ReducingToZero_SetsIsDeadAndInvokesOnDiedCallback()
        {
            var go = new GameObject("Entity");
            var entity = go.AddComponent<TestEntity>();
            var definition = CreateDefinition(10);
            var diedCalled = false;

            entity.Initialize(definition, () => diedCalled = true);
            entity.ApplyDamage(10);

            Assert.IsTrue(entity.IsDead);
            Assert.IsTrue(diedCalled);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Initialize_WithoutOnDiedCallback_DoesNotThrowWhenKilled()
        {
            var go = new GameObject("Entity");
            var entity = go.AddComponent<TestEntity>();
            var definition = CreateDefinition(10);

            entity.Initialize(definition);

            Assert.DoesNotThrow(() => entity.ApplyDamage(10));
            Assert.IsTrue(entity.IsDead);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void SetSelected_True_TintsSpriteRenderer()
        {
            var (entity, spriteRenderer) = CreateEntityWithSpriteRenderer();
            var definition = CreateDefinition(10);
            entity.Initialize(definition);

            entity.SetSelected(true);

            Assert.AreNotEqual(Color.white, spriteRenderer.color);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void SetSelected_FalseAfterTrue_RestoresWhite()
        {
            var (entity, spriteRenderer) = CreateEntityWithSpriteRenderer();
            var definition = CreateDefinition(10);
            entity.Initialize(definition);

            entity.SetSelected(true);
            entity.SetSelected(false);

            Assert.AreEqual(Color.white, spriteRenderer.color);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Initialize_ResetsLeftoverSelectionTintFromPreviousUse()
        {
            var (entity, spriteRenderer) = CreateEntityWithSpriteRenderer();
            var definition = CreateDefinition(10);
            entity.Initialize(definition);
            entity.SetSelected(true); // simulates a pooled instance released while still selected

            entity.Initialize(definition); // simulates the factory reusing it for a new instance

            Assert.AreEqual(Color.white, spriteRenderer.color);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        private static (TestEntity entity, SpriteRenderer spriteRenderer) CreateEntityWithSpriteRenderer()
        {
            var go = new GameObject("Entity");
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            var entity = go.AddComponent<TestEntity>();
            var so = new SerializedObject(entity);
            so.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            return (entity, spriteRenderer);
        }
    }
}
