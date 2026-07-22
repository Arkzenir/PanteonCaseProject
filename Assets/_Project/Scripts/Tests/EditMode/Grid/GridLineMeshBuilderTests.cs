using CaseGame.Grid;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Grid
{
    public class GridLineMeshBuilderTests
    {
        private static GridModel CreateGrid(float cellSize, int columns, int rows)
        {
            var definition = ScriptableObject.CreateInstance<GridDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("cellSize").floatValue = cellSize;
            so.FindProperty("columns").intValue = columns;
            so.FindProperty("rows").intValue = rows;
            so.FindProperty("originWorldPosition").vector2Value = Vector2.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
            return new GridModel(definition);
        }

        [Test]
        public void BuildSegments_ReturnsOneSegmentPerColumnAndRowBoundary()
        {
            var grid = CreateGrid(1f, 3, 2);

            var segments = GridLineMeshBuilder.BuildSegments(grid);

            // (columns+1) vertical + (rows+1) horizontal boundaries.
            Assert.AreEqual((3 + 1) + (2 + 1), segments.Count);
        }

        [Test]
        public void BuildSegments_FirstColumnBoundary_SpansFullGridHeight()
        {
            var grid = CreateGrid(2f, 3, 2);

            var segments = GridLineMeshBuilder.BuildSegments(grid);

            Assert.AreEqual(new Vector2(0f, 0f), segments[0].Start);
            Assert.AreEqual(new Vector2(0f, 4f), segments[0].End); // rows(2) * cellSize(2)
        }

        [Test]
        public void ComputeQuadVertices_HorizontalSegment_ProducesSymmetricThicknessAlongY()
        {
            GridLineMeshBuilder.ComputeQuadVertices(
                new GridLineMeshBuilder.Segment(new Vector2(0f, 0f), new Vector2(10f, 0f)),
                thickness: 0.2f,
                out var v0, out var v1, out var v2, out var v3);

            Assert.AreEqual(new Vector3(0f, -0.1f, 0f), v0);
            Assert.AreEqual(new Vector3(0f, 0.1f, 0f), v1);
            Assert.AreEqual(new Vector3(10f, 0.1f, 0f), v2);
            Assert.AreEqual(new Vector3(10f, -0.1f, 0f), v3);
        }

        [Test]
        public void ComputeQuadVertices_VerticalSegment_ProducesSymmetricThicknessAlongX()
        {
            GridLineMeshBuilder.ComputeQuadVertices(
                new GridLineMeshBuilder.Segment(new Vector2(0f, 0f), new Vector2(0f, 5f)),
                thickness: 0.4f,
                out var v0, out var v1, out var v2, out var v3);

            Assert.AreEqual(0.2f, v0.x, 0.0001f);
            Assert.AreEqual(-0.2f, v1.x, 0.0001f);
            Assert.AreEqual(-0.2f, v2.x, 0.0001f);
            Assert.AreEqual(0.2f, v3.x, 0.0001f);
        }

        [Test]
        public void BuildMesh_VertexAndTriangleCounts_MatchSegmentCount()
        {
            var grid = CreateGrid(1f, 3, 2);
            var segmentCount = GridLineMeshBuilder.BuildSegments(grid).Count;

            var mesh = GridLineMeshBuilder.BuildMesh(grid, 0.05f, Color.white);

            Assert.AreEqual(segmentCount * 4, mesh.vertexCount);
            Assert.AreEqual(segmentCount * 6, mesh.triangles.Length);

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BuildMesh_BakesTheGivenColorIntoEveryVertex()
        {
            var grid = CreateGrid(1f, 1, 1);
            var color = new Color(0.2f, 0.4f, 0.6f, 0.8f);

            var mesh = GridLineMeshBuilder.BuildMesh(grid, 0.05f, color);

            foreach (var vertexColor in mesh.colors)
            {
                Assert.AreEqual(color, vertexColor);
            }

            Object.DestroyImmediate(mesh);
        }
    }
}
