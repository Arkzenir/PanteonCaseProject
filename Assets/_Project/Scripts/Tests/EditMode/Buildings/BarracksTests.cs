using CaseGame.Buildings;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BarracksTests
    {
        [Test]
        public void SpawnPosition_SpawnPointAssigned_ReturnsSpawnPointPosition()
        {
            var go = new GameObject("Barracks");
            go.transform.position = new Vector3(1f, 1f, 0f);
            var barracks = go.AddComponent<Barracks>();
            var spawnPointGo = new GameObject("SpawnPoint");
            spawnPointGo.transform.SetParent(go.transform);
            spawnPointGo.transform.position = new Vector3(5f, 6f, 0f);
            var so = new SerializedObject(barracks);
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPointGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(new Vector3(5f, 6f, 0f), barracks.SpawnPosition);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SpawnPosition_NoSpawnPointAssigned_FallsBackToTransformPosition()
        {
            var go = new GameObject("Barracks");
            go.transform.position = new Vector3(2f, 3f, 0f);
            var barracks = go.AddComponent<Barracks>();

            Assert.AreEqual(new Vector3(2f, 3f, 0f), barracks.SpawnPosition);

            Object.DestroyImmediate(go);
        }
    }
}
