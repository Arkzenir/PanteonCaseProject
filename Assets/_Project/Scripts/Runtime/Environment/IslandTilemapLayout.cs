using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CaseGame.Environment
{
    /// <summary>
    /// Pure logic for procedurally reassembling the hand-painted island (Report 031) at any
    /// grid size: a 9-slice grass layer (4 corners, 4 edges, repeating center) covering
    /// <c>columns</c>×<c>rows</c> cells starting at (0,0) — matching <c>GridModel</c>'s own cell
    /// coordinates exactly, so the island always sits under the gameplay grid regardless of its
    /// size — plus one cliff row at <c>y = -1</c> (directly beneath the grass's bottom row,
    /// "facing the player," human-specified) spanning the same <c>columns</c> width, corner
    /// pieces at each end and the repeating middle piece between them.
    /// </summary>
    public static class IslandTilemapLayout
    {
        public readonly struct TilePlacement
        {
            public TilePlacement(Vector3Int cell, TileBase tile)
            {
                Cell = cell;
                Tile = tile;
            }

            public Vector3Int Cell { get; }
            public TileBase Tile { get; }
        }

        /// <summary>The grass layer's tile for every cell in a <paramref name="columns"/>×<paramref name="rows"/> island, corners/edges/center per <paramref name="tileSet"/>. A 1-wide or 1-tall island degenerates gracefully (corners take priority over edges) rather than throwing, though the tileset has no dedicated single-width piece for that case.</summary>
        public static List<TilePlacement> BuildGrass(int columns, int rows, IslandTileSet tileSet)
        {
            var placements = new List<TilePlacement>(Mathf.Max(0, columns * rows));

            for (var x = 0; x < columns; x++)
            {
                for (var y = 0; y < rows; y++)
                {
                    placements.Add(new TilePlacement(new Vector3Int(x, y, 0), GrassTileAt(x, y, columns, rows, tileSet)));
                }
            }

            return placements;
        }

        /// <summary>The cliff row directly beneath the grass's bottom row (<c>y = -1</c>), spanning the same <paramref name="columns"/> width — left/right corner pieces at each end, the repeating middle piece everywhere between.</summary>
        public static List<TilePlacement> BuildCliff(int columns, IslandTileSet tileSet)
        {
            var placements = new List<TilePlacement>(Mathf.Max(0, columns));

            for (var x = 0; x < columns; x++)
            {
                TileBase tile;
                if (x == 0)
                {
                    tile = tileSet.CliffLeftCornerTile;
                }
                else if (x == columns - 1)
                {
                    tile = tileSet.CliffRightCornerTile;
                }
                else
                {
                    tile = tileSet.CliffMiddleTile;
                }

                placements.Add(new TilePlacement(new Vector3Int(x, -1, 0), tile));
            }

            return placements;
        }

        private static TileBase GrassTileAt(int x, int y, int columns, int rows, IslandTileSet tileSet)
        {
            var isLeft = x == 0;
            var isRight = x == columns - 1;
            var isBottom = y == 0;
            var isTop = y == rows - 1;

            if (isBottom && isLeft)
            {
                return tileSet.BottomLeftCornerTile;
            }

            if (isBottom && isRight)
            {
                return tileSet.BottomRightCornerTile;
            }

            if (isTop && isLeft)
            {
                return tileSet.TopLeftCornerTile;
            }

            if (isTop && isRight)
            {
                return tileSet.TopRightCornerTile;
            }

            if (isBottom)
            {
                return tileSet.BottomEdgeTile;
            }

            if (isTop)
            {
                return tileSet.TopEdgeTile;
            }

            if (isLeft)
            {
                return tileSet.LeftEdgeTile;
            }

            if (isRight)
            {
                return tileSet.RightEdgeTile;
            }

            return tileSet.CenterTile;
        }
    }
}
