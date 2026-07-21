using UnityEngine;

namespace CaseGame.Grid
{
    /// <summary>
    /// Humble view over a <see cref="GridDefinition"/>: builds the runtime <see cref="GridModel"/>
    /// and draws Scene-view gizmos so the board can be visually verified before any placement
    /// or pathfinding systems consume it.
    /// </summary>
    public class GridView : MonoBehaviour
    {
        [SerializeField] private GridDefinition gridDefinition;
        [SerializeField] private Color gridLineColor = new Color(1f, 1f, 1f, 0.35f);

        private GridModel _gridModel;

        public GridModel GridModel => _gridModel;

        private void Awake()
        {
            _gridModel = new GridModel(gridDefinition);
        }

        private void OnDrawGizmos()
        {
            if (gridDefinition == null)
            {
                return;
            }

            var model = new GridModel(gridDefinition);
            Gizmos.color = gridLineColor;

            for (var x = 0; x <= model.Columns; x++)
            {
                var from = model.CellToWorld(new Vector2Int(x, 0));
                var to = model.CellToWorld(new Vector2Int(x, model.Rows));
                Gizmos.DrawLine(from, to);
            }

            for (var y = 0; y <= model.Rows; y++)
            {
                var from = model.CellToWorld(new Vector2Int(0, y));
                var to = model.CellToWorld(new Vector2Int(model.Columns, y));
                Gizmos.DrawLine(from, to);
            }
        }
    }
}
