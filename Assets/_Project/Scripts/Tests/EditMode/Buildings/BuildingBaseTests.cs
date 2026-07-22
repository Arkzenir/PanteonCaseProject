using CaseGame.Buildings;
using CaseGame.Grid;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Buildings
{
    public class BuildingBaseTests
    {
        // Health/IDamageable/death-callback behavior is shared GameEntityBase behavior,
        // covered once in Tests/EditMode/Entities/GameEntityBaseTests.cs — not duplicated here.

        private class TestBuilding : BuildingBase
        {
        }

        private static GridModel CreateGrid(int columns, int rows)
        {
            var gridDefinition = ScriptableObject.CreateInstance<GridDefinition>();
            var so = new SerializedObject(gridDefinition);
            so.FindProperty("cellSize").floatValue = 1f;
            so.FindProperty("columns").intValue = columns;
            so.FindProperty("rows").intValue = rows;
            so.FindProperty("originWorldPosition").vector2Value = Vector2.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
            return new GridModel(gridDefinition);
        }

        [Test]
        public void Definition_ReturnsStronglyTypedBuildingDefinition()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("maxHealth").intValue = 10;
            so.ApplyModifiedPropertiesWithoutUndo();

            building.Initialize(definition);

            Assert.AreSame(definition, building.Definition);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void SpawnPosition_DefaultsToTransformPosition()
        {
            var go = new GameObject("Building");
            go.transform.position = new Vector3(3f, 4f, 0f);
            var building = go.AddComponent<TestBuilding>();

            Assert.AreEqual(go.transform.position, building.SpawnPosition);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FootprintOrigin_BeforeSetPlacement_IsNull()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();

            Assert.IsNull(building.FootprintOrigin);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SetPlacement_RecordsFootprintOrigin()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var grid = CreateGrid(10, 10);

            building.SetPlacement(grid, new Vector2Int(2, 3));

            Assert.AreEqual(new Vector2Int(2, 3), building.FootprintOrigin);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ApplyDamage_KillingAPlacedBuilding_ReleasesItsFootprint()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var defSo = new SerializedObject(definition);
            defSo.FindProperty("maxHealth").intValue = 10;
            defSo.FindProperty("footprint").vector2IntValue = new Vector2Int(2, 2);
            defSo.ApplyModifiedPropertiesWithoutUndo();
            building.Initialize(definition);
            var grid = CreateGrid(10, 10);
            grid.SetAreaOccupied(new Vector2Int(3, 3), new Vector2Int(2, 2), true);
            building.SetPlacement(grid, new Vector2Int(3, 3));

            building.ApplyDamage(10);

            Assert.IsTrue(grid.IsAreaFree(new Vector2Int(3, 3), new Vector2Int(2, 2)));
            Assert.IsNull(building.FootprintOrigin);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }

        [Test]
        public void ApplyDamage_KillingABuildingNeverPlaced_DoesNotThrow()
        {
            var go = new GameObject("Building");
            var building = go.AddComponent<TestBuilding>();
            var definition = ScriptableObject.CreateInstance<BuildingDefinition>();
            var defSo = new SerializedObject(definition);
            defSo.FindProperty("maxHealth").intValue = 10;
            defSo.ApplyModifiedPropertiesWithoutUndo();
            building.Initialize(definition);

            Assert.DoesNotThrow(() => building.ApplyDamage(10));

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(definition);
        }
    }
}
