using CaseGame.Units;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Units
{
    public class ProjectileFactoryTests
    {
        private static UnitDefinition CreateDefinition(int maxHealth)
        {
            var definition = ScriptableObject.CreateInstance<UnitDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = maxHealth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return definition;
        }

        private static Soldier CreateTarget()
        {
            var go = new GameObject("Target");
            var soldier = go.AddComponent<Soldier>();
            soldier.Initialize(CreateDefinition(10));
            return soldier;
        }

        [Test]
        public void Launch_ActivatesInstanceAtGivenStartPosition()
        {
            var prefab = new GameObject("ProjectilePrefab").AddComponent<Projectile>();
            prefab.gameObject.SetActive(false);
            var container = new GameObject("Projectiles").transform;
            var factory = new ProjectileFactory(prefab, container);
            var target = CreateTarget();
            var startPosition = new Vector3(3f, 4f, 0f);

            factory.Launch(startPosition, target, damage: 5);

            var instance = container.GetChild(0);
            Assert.IsTrue(instance.gameObject.activeSelf);
            Assert.AreEqual(startPosition, instance.position);

            Object.DestroyImmediate(prefab.gameObject);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(target.Definition);
        }

        [Test]
        public void Launch_ParentsSpawnedInstanceUnderGivenContainer()
        {
            var prefab = new GameObject("ProjectilePrefab").AddComponent<Projectile>();
            prefab.gameObject.SetActive(false);
            var container = new GameObject("Projectiles").transform;
            var factory = new ProjectileFactory(prefab, container);
            var target = CreateTarget();

            factory.Launch(Vector3.zero, target, damage: 5);

            Assert.AreEqual(1, container.childCount);
            Assert.AreSame(container, container.GetChild(0).parent);

            Object.DestroyImmediate(prefab.gameObject);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(target.gameObject);
            Object.DestroyImmediate(target.Definition);
        }

        [Test]
        public void Launch_CalledConcurrently_CreatesDistinctInstancesRatherThanReusingOne()
        {
            var prefab = new GameObject("ProjectilePrefab").AddComponent<Projectile>();
            prefab.gameObject.SetActive(false);
            var container = new GameObject("Projectiles").transform;
            var factory = new ProjectileFactory(prefab, container);
            var targetA = CreateTarget();
            var targetB = CreateTarget();

            factory.Launch(Vector3.zero, targetA, damage: 5);
            factory.Launch(Vector3.one, targetB, damage: 5);

            // Neither projectile has arrived/returned to the pool yet, so both must still be
            // live, distinct instances rather than the second Launch reusing the first.
            Assert.AreEqual(2, container.childCount);

            Object.DestroyImmediate(prefab.gameObject);
            Object.DestroyImmediate(container.gameObject);
            Object.DestroyImmediate(targetA.gameObject);
            Object.DestroyImmediate(targetA.Definition);
            Object.DestroyImmediate(targetB.gameObject);
            Object.DestroyImmediate(targetB.Definition);
        }
    }
}
