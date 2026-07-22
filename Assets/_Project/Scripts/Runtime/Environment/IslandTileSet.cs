using UnityEngine;
using UnityEngine.Tilemaps;

namespace CaseGame.Environment
{
    /// <summary>
    /// Designer-tunable palette of the 12 named tile pieces <see cref="IslandTilemapLayout"/>
    /// needs to procedurally reassemble the hand-painted island (Report 031's manual Tile
    /// Palette pass) for any grid size: 9 for the grass layer (4 corners, 4 edges, 1 repeating
    /// center — the same "9-slice" shape already used by the Tiny Swords tileset's own Flat/
    /// Elevated Ground pieces) and 3 for the cliff row beneath the bottom edge (left corner,
    /// repeating middle, right corner). Populated once from the human's own hand-painted
    /// reference via a throwaway editor script (Report 032) — a designer can still override any
    /// individual piece by hand afterward, same as any other SO-driven configuration here.
    /// </summary>
    [CreateAssetMenu(fileName = "IslandTileSet_New", menuName = "CaseGame/Environment/Island Tile Set")]
    public class IslandTileSet : ScriptableObject
    {
        [SerializeField] private TileBase centerTile;
        [SerializeField] private TileBase topEdgeTile;
        [SerializeField] private TileBase bottomEdgeTile;
        [SerializeField] private TileBase leftEdgeTile;
        [SerializeField] private TileBase rightEdgeTile;
        [SerializeField] private TileBase topLeftCornerTile;
        [SerializeField] private TileBase topRightCornerTile;
        [SerializeField] private TileBase bottomLeftCornerTile;
        [SerializeField] private TileBase bottomRightCornerTile;
        [SerializeField] private TileBase cliffLeftCornerTile;
        [SerializeField] private TileBase cliffMiddleTile;
        [SerializeField] private TileBase cliffRightCornerTile;

        public TileBase CenterTile => centerTile;
        public TileBase TopEdgeTile => topEdgeTile;
        public TileBase BottomEdgeTile => bottomEdgeTile;
        public TileBase LeftEdgeTile => leftEdgeTile;
        public TileBase RightEdgeTile => rightEdgeTile;
        public TileBase TopLeftCornerTile => topLeftCornerTile;
        public TileBase TopRightCornerTile => topRightCornerTile;
        public TileBase BottomLeftCornerTile => bottomLeftCornerTile;
        public TileBase BottomRightCornerTile => bottomRightCornerTile;
        public TileBase CliffLeftCornerTile => cliffLeftCornerTile;
        public TileBase CliffMiddleTile => cliffMiddleTile;
        public TileBase CliffRightCornerTile => cliffRightCornerTile;
    }
}
