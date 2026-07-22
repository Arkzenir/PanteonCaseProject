using System.Linq;
using CaseGame.Environment;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace CaseGame.Tests.EditMode.Environment
{
    public class IslandTilemapLayoutTests
    {
        private IslandTileSet _tileSet;
        private Tile _center, _top, _bottom, _left, _right, _topLeft, _topRight, _bottomLeft, _bottomRight;
        private Tile _cliffLeft, _cliffMiddle, _cliffRight;

        [SetUp]
        public void SetUp()
        {
            _tileSet = ScriptableObject.CreateInstance<IslandTileSet>();
            _center = MakeTile();
            _top = MakeTile();
            _bottom = MakeTile();
            _left = MakeTile();
            _right = MakeTile();
            _topLeft = MakeTile();
            _topRight = MakeTile();
            _bottomLeft = MakeTile();
            _bottomRight = MakeTile();
            _cliffLeft = MakeTile();
            _cliffMiddle = MakeTile();
            _cliffRight = MakeTile();

            var so = new SerializedObject(_tileSet);
            so.FindProperty("centerTile").objectReferenceValue = _center;
            so.FindProperty("topEdgeTile").objectReferenceValue = _top;
            so.FindProperty("bottomEdgeTile").objectReferenceValue = _bottom;
            so.FindProperty("leftEdgeTile").objectReferenceValue = _left;
            so.FindProperty("rightEdgeTile").objectReferenceValue = _right;
            so.FindProperty("topLeftCornerTile").objectReferenceValue = _topLeft;
            so.FindProperty("topRightCornerTile").objectReferenceValue = _topRight;
            so.FindProperty("bottomLeftCornerTile").objectReferenceValue = _bottomLeft;
            so.FindProperty("bottomRightCornerTile").objectReferenceValue = _bottomRight;
            so.FindProperty("cliffLeftCornerTile").objectReferenceValue = _cliffLeft;
            so.FindProperty("cliffMiddleTile").objectReferenceValue = _cliffMiddle;
            so.FindProperty("cliffRightCornerTile").objectReferenceValue = _cliffRight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tileSet);
            foreach (var tile in new[] { _center, _top, _bottom, _left, _right, _topLeft, _topRight, _bottomLeft, _bottomRight, _cliffLeft, _cliffMiddle, _cliffRight })
            {
                Object.DestroyImmediate(tile);
            }
        }

        private static Tile MakeTile()
        {
            return ScriptableObject.CreateInstance<Tile>();
        }

        private TileBase TileAt(System.Collections.Generic.List<IslandTilemapLayout.TilePlacement> placements, int x, int y)
        {
            return placements.First(p => p.Cell == new Vector3Int(x, y, 0)).Tile;
        }

        [Test]
        public void BuildGrass_ReturnsOnePlacementPerCell()
        {
            var placements = IslandTilemapLayout.BuildGrass(5, 4, _tileSet);

            Assert.AreEqual(20, placements.Count);
        }

        [Test]
        public void BuildGrass_PlacesCornersCorrectly()
        {
            var placements = IslandTilemapLayout.BuildGrass(5, 4, _tileSet);

            Assert.AreEqual(_bottomLeft, TileAt(placements, 0, 0));
            Assert.AreEqual(_bottomRight, TileAt(placements, 4, 0));
            Assert.AreEqual(_topLeft, TileAt(placements, 0, 3));
            Assert.AreEqual(_topRight, TileAt(placements, 4, 3));
        }

        [Test]
        public void BuildGrass_PlacesEdgesCorrectly()
        {
            var placements = IslandTilemapLayout.BuildGrass(5, 4, _tileSet);

            Assert.AreEqual(_bottom, TileAt(placements, 2, 0));
            Assert.AreEqual(_top, TileAt(placements, 2, 3));
            Assert.AreEqual(_left, TileAt(placements, 0, 1));
            Assert.AreEqual(_right, TileAt(placements, 4, 1));
        }

        [Test]
        public void BuildGrass_PlacesCenterEverywhereInside()
        {
            var placements = IslandTilemapLayout.BuildGrass(5, 4, _tileSet);

            Assert.AreEqual(_center, TileAt(placements, 2, 1));
            Assert.AreEqual(_center, TileAt(placements, 2, 2));
            Assert.AreEqual(_center, TileAt(placements, 1, 1));
            Assert.AreEqual(_center, TileAt(placements, 3, 2));
        }

        [Test]
        public void BuildGrass_DifferentGridSize_StillPlacesCornersAtNewExtents()
        {
            var placements = IslandTilemapLayout.BuildGrass(16, 16, _tileSet);

            Assert.AreEqual(256, placements.Count);
            Assert.AreEqual(_bottomLeft, TileAt(placements, 0, 0));
            Assert.AreEqual(_bottomRight, TileAt(placements, 15, 0));
            Assert.AreEqual(_topLeft, TileAt(placements, 0, 15));
            Assert.AreEqual(_topRight, TileAt(placements, 15, 15));
            Assert.AreEqual(_center, TileAt(placements, 8, 8));
        }

        [Test]
        public void BuildCliff_ReturnsOnePlacementPerColumn()
        {
            var placements = IslandTilemapLayout.BuildCliff(5, _tileSet);

            Assert.AreEqual(5, placements.Count);
        }

        [Test]
        public void BuildCliff_PlacesLeftAndRightCornersAtTheEnds()
        {
            var placements = IslandTilemapLayout.BuildCliff(5, _tileSet);

            Assert.AreEqual(_cliffLeft, TileAt(placements, 0, -1));
            Assert.AreEqual(_cliffRight, TileAt(placements, 4, -1));
        }

        [Test]
        public void BuildCliff_PlacesMiddleTileBetweenTheCorners()
        {
            var placements = IslandTilemapLayout.BuildCliff(5, _tileSet);

            Assert.AreEqual(_cliffMiddle, TileAt(placements, 1, -1));
            Assert.AreEqual(_cliffMiddle, TileAt(placements, 2, -1));
            Assert.AreEqual(_cliffMiddle, TileAt(placements, 3, -1));
        }

        [Test]
        public void BuildCliff_AllCellsAreOnRowMinusOne()
        {
            var placements = IslandTilemapLayout.BuildCliff(5, _tileSet);

            Assert.IsTrue(placements.All(p => p.Cell.y == -1));
        }
    }
}
