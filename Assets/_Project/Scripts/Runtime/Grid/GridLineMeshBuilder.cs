using System.Collections.Generic;
using UnityEngine;

namespace CaseGame.Grid
{
    /// <summary>
    /// Pure geometry for rendering a <see cref="GridModel"/>'s cell boundaries as a single
    /// combined mesh — one draw call for the entire board, regardless of its size, instead of
    /// one <see cref="LineRenderer"/> per line (which would cost one draw call per line, easily
    /// blowing GI-12's &lt;20 SetPass-call budget on a real-sized board — see decisions log
    /// #18/#49 for the same draw-call-conscious reasoning applied to sprites). Every line
    /// segment becomes a thin quad (2 triangles) with a shared color baked into its vertices,
    /// read by <c>GridLines.shader</c> (plain vertex-color pass-through — there's no sprite
    /// texture to sample here, unlike this project's other custom shaders).
    /// </summary>
    public static class GridLineMeshBuilder
    {
        public readonly struct Segment
        {
            public Segment(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }

            public Vector2 Start { get; }
            public Vector2 End { get; }
        }

        /// <summary>Every column boundary (0..Columns) and row boundary (0..Rows), each spanning the full opposite axis — (Columns+1)+(Rows+1) segments total, matching the grid's old gizmo-drawing logic exactly.</summary>
        public static List<Segment> BuildSegments(GridModel grid)
        {
            var segments = new List<Segment>(grid.Columns + grid.Rows + 2);

            for (var x = 0; x <= grid.Columns; x++)
            {
                segments.Add(new Segment(grid.CellToWorld(new Vector2Int(x, 0)), grid.CellToWorld(new Vector2Int(x, grid.Rows))));
            }

            for (var y = 0; y <= grid.Rows; y++)
            {
                segments.Add(new Segment(grid.CellToWorld(new Vector2Int(0, y)), grid.CellToWorld(new Vector2Int(grid.Columns, y))));
            }

            return segments;
        }

        /// <summary>The 4 corner vertices of the thin quad representing <paramref name="segment"/> at the given <paramref name="thickness"/> (world units), centered on the segment's own line. Pure so the quad math is directly testable independent of a live <see cref="Mesh"/>.</summary>
        public static void ComputeQuadVertices(Segment segment, float thickness, out Vector3 v0, out Vector3 v1, out Vector3 v2, out Vector3 v3)
        {
            var direction = (segment.End - segment.Start).normalized;
            var normal = new Vector2(-direction.y, direction.x) * (thickness * 0.5f);

            v0 = (Vector3)(segment.Start - normal);
            v1 = (Vector3)(segment.Start + normal);
            v2 = (Vector3)(segment.End + normal);
            v3 = (Vector3)(segment.End - normal);
        }

        /// <summary>Builds the single combined mesh for every line in <paramref name="grid"/> — 4 vertices/2 triangles per segment, the same <paramref name="color"/> baked into every vertex.</summary>
        public static Mesh BuildMesh(GridModel grid, float thickness, Color color)
        {
            var segments = BuildSegments(grid);
            var vertices = new Vector3[segments.Count * 4];
            var colors = new Color[vertices.Length];
            var triangles = new int[segments.Count * 6];

            for (var i = 0; i < segments.Count; i++)
            {
                ComputeQuadVertices(segments[i], thickness, out var v0, out var v1, out var v2, out var v3);

                var vertexOffset = i * 4;
                vertices[vertexOffset + 0] = v0;
                vertices[vertexOffset + 1] = v1;
                vertices[vertexOffset + 2] = v2;
                vertices[vertexOffset + 3] = v3;

                colors[vertexOffset + 0] = color;
                colors[vertexOffset + 1] = color;
                colors[vertexOffset + 2] = color;
                colors[vertexOffset + 3] = color;

                var triangleOffset = i * 6;
                triangles[triangleOffset + 0] = vertexOffset + 0;
                triangles[triangleOffset + 1] = vertexOffset + 1;
                triangles[triangleOffset + 2] = vertexOffset + 2;
                triangles[triangleOffset + 3] = vertexOffset + 0;
                triangles[triangleOffset + 4] = vertexOffset + 2;
                triangles[triangleOffset + 5] = vertexOffset + 3;
            }

            var mesh = new Mesh { name = "GridLines" };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
