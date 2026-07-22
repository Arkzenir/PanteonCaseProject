using UnityEngine;

namespace CaseGame.Grid
{
    /// <summary>
    /// Humble view over a <see cref="GridDefinition"/>: builds the runtime <see cref="GridModel"/>
    /// other systems consume, and renders the board's cell lines as a single combined mesh
    /// (<see cref="GridLineMeshBuilder"/>) so the grid is actually visible in Play Mode and in a
    /// build, not just as a Scene-view gizmo. <c>[ExecuteAlways]</c> plus a cheap per-Update
    /// signature check rebuilds the mesh in Edit Mode too, so tweaking <see cref="GridDefinition"/>
    /// values (or this component's own <see cref="lineMaterial"/>) previews immediately — <c>Awake</c>
    /// alone can't do this, since it never fires outside Play Mode (the same gotcha documented in
    /// ENVIRONMENT.md/decisions log elsewhere).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class GridView : MonoBehaviour
    {
        [SerializeField] private GridDefinition gridDefinition;
        [SerializeField] private Material lineMaterial;
        [SerializeField] private int sortingOrder = -1000;
        [SerializeField] private bool linesVisibleOnStart = true;

        private GridModel _gridModel;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private GridDefinition _lastBuiltDefinition;
        private float _lastBuiltCellSize;
        private int _lastBuiltColumns;
        private int _lastBuiltRows;
        private Vector2 _lastBuiltOrigin;
        private Color _lastBuiltLineColor;
        private float _lastBuiltLineThickness;

        public GridModel GridModel => _gridModel;
        public GridDefinition Definition => gridDefinition;
        public bool LinesVisible => _meshRenderer != null && _meshRenderer.enabled;

        /// <summary>Toggles whether the grid lines render at all — the feature's "toggleable on/off" requirement. Plain method rather than an event channel: this is local render state on this one instance, nothing else needs to react to it.</summary>
        public void SetLinesVisible(bool visible)
        {
            EnsureComponents();
            _meshRenderer.enabled = visible;
        }

        private void Awake()
        {
            EnsureComponents();
            _gridModel = gridDefinition != null ? new GridModel(gridDefinition) : null;
            _meshRenderer.enabled = linesVisibleOnStart;
            RebuildMesh();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            // Deferred: OnValidate runs mid-serialization, and Tilemap/Mesh writes internally use
            // SendMessage, which Unity forbids in that window (harmless "SendMessage cannot be
            // called during Awake, CheckConsistency, or OnValidate" warning otherwise).
            UnityEditor.EditorApplication.delayCall += DeferredRebuildMesh;
#endif
        }

#if UNITY_EDITOR
        private void DeferredRebuildMesh()
        {
            UnityEditor.EditorApplication.delayCall -= DeferredRebuildMesh;

            if (this == null)
            {
                return;
            }

            EnsureComponents();
            RebuildMesh();
        }
#endif

        private void Update()
        {
            if (!Application.isPlaying)
            {
                EnsureComponents();
                RebuildMeshIfDefinitionChanged();
            }
        }

        private void EnsureComponents()
        {
            if (_meshFilter == null)
            {
                _meshFilter = GetComponent<MeshFilter>();
            }

            if (_meshRenderer == null)
            {
                _meshRenderer = GetComponent<MeshRenderer>();
                _meshRenderer.sortingOrder = sortingOrder;
            }
        }

        private void RebuildMeshIfDefinitionChanged()
        {
            if (gridDefinition == null)
            {
                return;
            }

            var unchanged = _lastBuiltDefinition == gridDefinition &&
                             Mathf.Approximately(_lastBuiltCellSize, gridDefinition.CellSize) &&
                             _lastBuiltColumns == gridDefinition.Columns &&
                             _lastBuiltRows == gridDefinition.Rows &&
                             _lastBuiltOrigin == gridDefinition.OriginWorldPosition &&
                             _lastBuiltLineColor == gridDefinition.LineColor &&
                             Mathf.Approximately(_lastBuiltLineThickness, gridDefinition.LineThickness);

            if (!unchanged)
            {
                RebuildMesh();
            }
        }

        private void RebuildMesh()
        {
            _meshRenderer.sortingOrder = sortingOrder;
            _meshRenderer.sharedMaterial = lineMaterial;

            if (gridDefinition == null)
            {
                _meshFilter.sharedMesh = null;
                return;
            }

            var model = new GridModel(gridDefinition);
            _meshFilter.sharedMesh = GridLineMeshBuilder.BuildMesh(model, gridDefinition.LineThickness, gridDefinition.LineColor);

            _lastBuiltDefinition = gridDefinition;
            _lastBuiltCellSize = gridDefinition.CellSize;
            _lastBuiltColumns = gridDefinition.Columns;
            _lastBuiltRows = gridDefinition.Rows;
            _lastBuiltOrigin = gridDefinition.OriginWorldPosition;
            _lastBuiltLineColor = gridDefinition.LineColor;
            _lastBuiltLineThickness = gridDefinition.LineThickness;
        }
    }
}
