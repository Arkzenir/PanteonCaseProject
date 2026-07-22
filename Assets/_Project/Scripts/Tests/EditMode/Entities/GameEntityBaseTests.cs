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
        public void SetSelected_True_EnablesOutlineRenderer()
        {
            var (entity, outlineRenderer) = CreateEntityWithOutlineRenderer();
            var definition = CreateDefinition(10);
            entity.Initialize(definition);

            entity.SetSelected(true);

            Assert.IsTrue(outlineRenderer.enabled);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void SetSelected_FalseAfterTrue_DisablesOutlineRenderer()
        {
            var (entity, outlineRenderer) = CreateEntityWithOutlineRenderer();
            var definition = CreateDefinition(10);
            entity.Initialize(definition);

            entity.SetSelected(true);
            entity.SetSelected(false);

            Assert.IsFalse(outlineRenderer.enabled);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Initialize_ResetsLeftoverSelectionOutlineFromPreviousUse()
        {
            var (entity, outlineRenderer) = CreateEntityWithOutlineRenderer();
            var definition = CreateDefinition(10);
            entity.Initialize(definition);
            entity.SetSelected(true); // simulates a pooled instance released while still selected

            entity.Initialize(definition); // simulates the factory reusing it for a new instance

            Assert.IsFalse(outlineRenderer.enabled);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void Initialize_SetsOutlineRendererSpriteToMatchDefinition()
        {
            var (entity, outlineRenderer) = CreateEntityWithOutlineRenderer();
            var definition = CreateDefinition(10);
            var sprite = Sprite.Create(new Texture2D(1, 1), new Rect(0, 0, 1, 1), Vector2.zero);
            var so = new SerializedObject(definition);
            so.FindProperty("sprite").objectReferenceValue = sprite;
            so.ApplyModifiedPropertiesWithoutUndo();

            entity.Initialize(definition);

            Assert.AreSame(sprite, outlineRenderer.sprite);

            Object.DestroyImmediate(entity.gameObject);
            Object.DestroyImmediate(definition);
        }

        private static (TestEntity entity, SpriteRenderer outlineRenderer) CreateEntityWithOutlineRenderer()
        {
            var go = new GameObject("Entity");
            var outlineRenderer = new GameObject("Outline").AddComponent<SpriteRenderer>();
            outlineRenderer.transform.SetParent(go.transform);
            var entity = go.AddComponent<TestEntity>();
            var so = new SerializedObject(entity);
            so.FindProperty("outlineRenderer").objectReferenceValue = outlineRenderer;
            so.ApplyModifiedPropertiesWithoutUndo();
            return (entity, outlineRenderer);
        }
    }
}
