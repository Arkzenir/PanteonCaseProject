using System.Collections.Generic;
using CaseGame.Grid;
using UnityEngine;

namespace CaseGame.Pathfinding
{
    /// <summary>
    /// Custom grid-based A* pathfinding (brief-mandated algorithm choice — decisions log #3;
    /// no Unity NavMesh). Stateless: takes a <see cref="GridModel"/> and start/goal cells,
    /// returns the shortest path as a list of cells, or null if no path exists. Movement is
    /// 8-directional (diagonal cost √2) with corner-cutting prevented — a diagonal step is
    /// rejected if either flanking orthogonal cell is blocked, so a path can't clip through a
    /// building's corner. This is how units "wander around buildings" (GI-7/GI-8) instead of
    /// cutting straight through them.
    /// </summary>
    public static class AStarPathfinder
    {
        private const float OrthogonalCost = 1f;
        private const float DiagonalCost = 1.4142136f; // sqrt(2)

        private static readonly Vector2Int[] Directions =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        private class Node
        {
            public Vector2Int Cell;
            public Node Parent;
            public float GCost;
            public float HCost;
            public float FCost => GCost + HCost;
        }

        /// <summary>Returns the path from start to goal (inclusive), or null if goal is unreachable.</summary>
        public static List<Vector2Int> FindPath(GridModel grid, Vector2Int start, Vector2Int goal)
        {
            if (grid.IsOccupied(goal))
            {
                return null;
            }

            if (start == goal)
            {
                return new List<Vector2Int> { start };
            }

            var startNode = new Node { Cell = start, GCost = 0f, HCost = Heuristic(start, goal) };
            var open = new List<Node> { startNode };
            var openLookup = new Dictionary<Vector2Int, Node> { { start, startNode } };
            var closed = new HashSet<Vector2Int>();

            while (open.Count > 0)
            {
                var current = GetLowestFCost(open);

                if (current.Cell == goal)
                {
                    return ReconstructPath(current);
                }

                open.Remove(current);
                openLookup.Remove(current.Cell);
                closed.Add(current.Cell);

                foreach (var neighborCell in GetWalkableNeighbors(grid, current.Cell))
                {
                    if (closed.Contains(neighborCell))
                    {
                        continue;
                    }

                    var isDiagonal = neighborCell.x != current.Cell.x && neighborCell.y != current.Cell.y;
                    var tentativeG = current.GCost + (isDiagonal ? DiagonalCost : OrthogonalCost);

                    if (!openLookup.TryGetValue(neighborCell, out var neighborNode))
                    {
                        neighborNode = new Node
                        {
                            Cell = neighborCell,
                            GCost = tentativeG,
                            HCost = Heuristic(neighborCell, goal),
                            Parent = current,
                        };
                        open.Add(neighborNode);
                        openLookup.Add(neighborCell, neighborNode);
                    }
                    else if (tentativeG < neighborNode.GCost)
                    {
                        neighborNode.GCost = tentativeG;
                        neighborNode.Parent = current;
                    }
                }
            }

            return null;
        }

        private static Node GetLowestFCost(List<Node> open)
        {
            var best = open[0];
            for (var i = 1; i < open.Count; i++)
            {
                var candidate = open[i];
                if (candidate.FCost < best.FCost || (Mathf.Approximately(candidate.FCost, best.FCost) && candidate.HCost < best.HCost))
                {
                    best = candidate;
                }
            }

            return best;
        }

        private static IEnumerable<Vector2Int> GetWalkableNeighbors(GridModel grid, Vector2Int cell)
        {
            foreach (var direction in Directions)
            {
                var neighbor = cell + direction;
                if (grid.IsOccupied(neighbor))
                {
                    continue;
                }

                if (direction.x != 0 && direction.y != 0)
                {
                    var horizontal = new Vector2Int(cell.x + direction.x, cell.y);
                    var vertical = new Vector2Int(cell.x, cell.y + direction.y);
                    if (grid.IsOccupied(horizontal) || grid.IsOccupied(vertical))
                    {
                        continue;
                    }
                }

                yield return neighbor;
            }
        }

        /// <summary>Octile distance — admissible/consistent heuristic matching the 8-directional movement costs above.</summary>
        private static float Heuristic(Vector2Int a, Vector2Int b)
        {
            var dx = Mathf.Abs(a.x - b.x);
            var dy = Mathf.Abs(a.y - b.y);
            return (dx + dy) + (DiagonalCost - 2f * OrthogonalCost) * Mathf.Min(dx, dy);
        }

        private static List<Vector2Int> ReconstructPath(Node goalNode)
        {
            var path = new List<Vector2Int>();
            var current = goalNode;
            while (current != null)
            {
                path.Add(current.Cell);
                current = current.Parent;
            }

            path.Reverse();
            return path;
        }
    }
}
