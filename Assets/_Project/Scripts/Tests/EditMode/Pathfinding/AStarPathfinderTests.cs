using System.Collections.Generic;
using CaseGame.Grid;
using CaseGame.Pathfinding;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CaseGame.Tests.EditMode.Pathfinding
{
    public class AStarPathfinderTests
    {
        private static GridModel CreateGrid(int columns, int rows)
        {
            var definition = ScriptableObject.CreateInstance<GridDefinition>();
            var so = new SerializedObject(definition);
            so.FindProperty("cellSize").floatValue = 1f;
            so.FindProperty("columns").intValue = columns;
            so.FindProperty("rows").intValue = rows;
            so.FindProperty("originWorldPosition").vector2Value = Vector2.zero;
            so.ApplyModifiedPropertiesWithoutUndo();
            return new GridModel(definition);
        }

        private static void AssertIsValidPath(List<Vector2Int> path, GridModel grid, Vector2Int start, Vector2Int goal)
        {
            Assert.IsNotNull(path);
            Assert.AreEqual(start, path[0]);
            Assert.AreEqual(goal, path[path.Count - 1]);

            var visited = new HashSet<Vector2Int>();
            for (var i = 0; i < path.Count; i++)
            {
                Assert.IsTrue(visited.Add(path[i]), $"Path revisits cell {path[i]}");
                Assert.IsFalse(grid.IsOccupied(path[i]), $"Path passes through occupied cell {path[i]}");

                if (i > 0)
                {
                    var delta = path[i] - path[i - 1];
                    Assert.LessOrEqual(Mathf.Abs(delta.x), 1, "Step is not a single adjacent move");
                    Assert.LessOrEqual(Mathf.Abs(delta.y), 1, "Step is not a single adjacent move");
                    Assert.IsFalse(delta.x == 0 && delta.y == 0, "Step did not move at all");
                }
            }
        }

        [Test]
        public void FindPath_StraightLine_NoObstacles_ReturnsValidShortestPath()
        {
            var grid = CreateGrid(10, 10);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(0, 0), new Vector2Int(4, 0));

            AssertIsValidPath(path, grid, new Vector2Int(0, 0), new Vector2Int(4, 0));
            Assert.AreEqual(5, path.Count);
        }

        [Test]
        public void FindPath_DiagonalDestination_PrefersDiagonalMovement()
        {
            var grid = CreateGrid(10, 10);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(0, 0), new Vector2Int(3, 3));

            AssertIsValidPath(path, grid, new Vector2Int(0, 0), new Vector2Int(3, 3));
            // Pure-diagonal path is 4 cells; a 4-directional-only implementation would need 7.
            Assert.AreEqual(4, path.Count);
        }

        [Test]
        public void FindPath_StartEqualsGoal_ReturnsSingleCellPath()
        {
            var grid = CreateGrid(5, 5);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(2, 2), new Vector2Int(2, 2));

            Assert.AreEqual(1, path.Count);
            Assert.AreEqual(new Vector2Int(2, 2), path[0]);
        }

        [Test]
        public void FindPath_GoalOccupied_ReturnsNull()
        {
            var grid = CreateGrid(5, 5);
            grid.SetAreaOccupied(new Vector2Int(3, 3), Vector2Int.one, true);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(0, 0), new Vector2Int(3, 3));

            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_GoalOutOfBounds_ReturnsNull()
        {
            var grid = CreateGrid(5, 5);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(0, 0), new Vector2Int(99, 99));

            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_RoutesAroundObstacleWall_LikeWanderingAroundABuilding()
        {
            var grid = CreateGrid(10, 10);
            // Vertical wall at x=5 spanning y=0..8, leaving a gap only at y=9.
            for (var y = 0; y < 9; y++)
            {
                grid.SetAreaOccupied(new Vector2Int(5, y), Vector2Int.one, true);
            }

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(0, 0), new Vector2Int(9, 0));

            AssertIsValidPath(path, grid, new Vector2Int(0, 0), new Vector2Int(9, 0));
        }

        [Test]
        public void FindPath_NoPathExists_ReturnsNull()
        {
            var grid = CreateGrid(5, 5);
            // Fully enclose the goal cell (2,2) on all 4 orthogonal sides.
            grid.SetAreaOccupied(new Vector2Int(1, 2), Vector2Int.one, true);
            grid.SetAreaOccupied(new Vector2Int(3, 2), Vector2Int.one, true);
            grid.SetAreaOccupied(new Vector2Int(2, 1), Vector2Int.one, true);
            grid.SetAreaOccupied(new Vector2Int(2, 3), Vector2Int.one, true);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(0, 0), new Vector2Int(2, 2));

            Assert.IsNull(path);
        }

        [Test]
        public void FindPath_DoesNotCutDiagonalCornerThroughBlockedCells()
        {
            var grid = CreateGrid(8, 8);
            // Block the two orthogonal cells flanking the diagonal from (1,1) to (2,2),
            // leaving (2,2) itself free — a naive implementation would clip the corner. Uses
            // an 8x8 grid (not the tightest possible) so a valid detour actually exists;
            // a corner blocked flush against the grid edge would make the goal unreachable
            // instead of just detour-requiring, which would test the wrong thing.
            grid.SetAreaOccupied(new Vector2Int(2, 1), Vector2Int.one, true);
            grid.SetAreaOccupied(new Vector2Int(1, 2), Vector2Int.one, true);

            var path = AStarPathfinder.FindPath(grid, new Vector2Int(1, 1), new Vector2Int(2, 2));

            AssertIsValidPath(path, grid, new Vector2Int(1, 1), new Vector2Int(2, 2));
            // The direct diagonal step is illegal here, so the valid detour must take more than 2 cells.
            Assert.Greater(path.Count, 2);
        }

        [Test]
        public void FindApproachCell_TargetAdjacent_ReturnsNearestFreeCellWithinRange()
        {
            var grid = CreateGrid(10, 10);

            var approach = AStarPathfinder.FindApproachCell(grid, new Vector2Int(5, 5), new Vector2Int(2, 2), range: 1);

            Assert.IsTrue(approach.HasValue);
            Assert.AreEqual(1, Mathf.Max(Mathf.Abs(approach.Value.x - 2), Mathf.Abs(approach.Value.y - 2)));
            // Of the 8 cells adjacent to (2,2), (3,3) is closest to the attacker at (5,5).
            Assert.AreEqual(new Vector2Int(3, 3), approach.Value);
        }

        [Test]
        public void FindApproachCell_NeverReturnsTheTargetCellItself()
        {
            var grid = CreateGrid(5, 5);

            var approach = AStarPathfinder.FindApproachCell(grid, new Vector2Int(0, 0), new Vector2Int(2, 2), range: 3);

            Assert.AreNotEqual(new Vector2Int(2, 2), approach.Value);
        }

        [Test]
        public void FindApproachCell_AllCandidatesOccupied_ReturnsNull()
        {
            var grid = CreateGrid(5, 5);
            // Occupy every cell within range 1 of (2,2) except (2,2) itself.
            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                    {
                        continue;
                    }

                    grid.SetAreaOccupied(new Vector2Int(2 + dx, 2 + dy), Vector2Int.one, true);
                }
            }

            var approach = AStarPathfinder.FindApproachCell(grid, new Vector2Int(0, 0), new Vector2Int(2, 2), range: 1);

            Assert.IsNull(approach);
        }

        [Test]
        public void FindApproachCell_AlreadyWithinRange_ReturnsAttackersOwnCellIfFree()
        {
            var grid = CreateGrid(10, 10);

            // Attacker's own cell (3,2) is within range 2 of the target (2,2) and unoccupied, so
            // it's the globally-nearest candidate (distance 0) — no special-casing needed, this
            // falls out of the general nearest-candidate search.
            var approach = AStarPathfinder.FindApproachCell(grid, new Vector2Int(3, 2), new Vector2Int(2, 2), range: 2);

            Assert.AreEqual(new Vector2Int(3, 2), approach.Value);
        }
    }
}
