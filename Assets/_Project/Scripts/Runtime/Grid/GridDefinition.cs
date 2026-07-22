using UnityEngine;

namespace CaseGame.Grid
{
    /// <summary>
    /// Designer-tunable layout for the game board grid: cell size, extents, and world origin.
    /// Cell size and board extents are intentionally not hardcoded anywhere else in the
    /// project — they are set here once final art/visuals are chosen.
    /// </summary>
    [CreateAssetMenu(fileName = "GridDef_New", menuName = "CaseGame/Grid/Grid Definition")]
    public class GridDefinition : ScriptableObject
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private int columns = 20;
        [SerializeField] private int rows = 20;
        [SerializeField] private Vector2 originWorldPosition = Vector2.zero;
        [SerializeField] private Color lineColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private float lineThickness = 0.025f;

        public float CellSize => cellSize;
        public int Columns => columns;
        public int Rows => rows;
        public Vector2 OriginWorldPosition => originWorldPosition;
        public Color LineColor => lineColor;
        public float LineThickness => lineThickness;

        private void OnValidate()
        {
            cellSize = Mathf.Max(cellSize, 0.01f);
            columns = Mathf.Max(columns, 1);
            rows = Mathf.Max(rows, 1);
            lineThickness = Mathf.Max(lineThickness, 0.001f);
        }
    }
}
