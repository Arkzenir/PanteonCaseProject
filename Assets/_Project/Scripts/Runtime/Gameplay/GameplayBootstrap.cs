using CaseGame.Buildings;
using CaseGame.CameraControl;
using CaseGame.Environment;
using CaseGame.Grid;
using CaseGame.Placement;
using CaseGame.Selection;
using CaseGame.Units;
using UnityEngine;

namespace CaseGame.Gameplay
{
    /// <summary>
    /// Gameplay.unity's composition root: the one place that constructs the plain-C# Factories
    /// (which need a real container Transform) and calls each already-tested controller's
    /// explicit <c>Initialize</c> — <see cref="PlacementController"/>, <see cref="SelectionController"/>,
    /// <see cref="UnitProductionController"/> were all deliberately built with lifecycle-independent
    /// initialization (never Awake-wired) specifically because Awake ordering between them and
    /// whatever owns the <see cref="GridModel"/>/Factories isn't guaranteed — <c>Start()</c> is,
    /// since it runs after every object's Awake in the scene. This class's only job is that
    /// wiring; it holds no gameplay logic of its own.
    /// </summary>
    public class GameplayBootstrap : MonoBehaviour
    {
        [SerializeField] private GridView gridView;
        [SerializeField] private Transform buildingsContainer;
        [SerializeField] private Transform unitsContainer;
        [SerializeField] private Transform projectilesContainer;
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private PlacementController placementController;
        [SerializeField] private SelectionController selectionController;
        [SerializeField] private UnitProductionController unitProductionController;
        [SerializeField] private CameraController cameraController;
        [SerializeField] private TerrainCompositor terrainCompositor;

        private void Start()
        {
            var grid = gridView.GridModel;
            var buildingFactory = new BuildingFactory(buildingsContainer);
            var unitFactory = new UnitFactory(unitsContainer);
            var projectileFactory = new ProjectileFactory(projectilePrefab, projectilesContainer);

            placementController.Initialize(grid, buildingFactory);
            selectionController.Initialize(grid, projectileFactory);
            unitProductionController.Initialize(unitFactory, grid);

            // Camera pan/zoom stays within the environment's water backdrop (Report 031,
            // human-requested) — computed from the same GridModel + GridDefinition.TerrainMargin
            // the terrain itself is painted with (TerrainBounds, decisions log #78), so the two
            // can never drift out of sync.
            var margin = gridView.Definition.TerrainMargin;
            var (boundsMin, boundsMax) = TerrainBounds.Compute(grid, margin);
            cameraController.SetBounds(boundsMin, boundsMax);

            // Bakes the 3 Tilemap terrain layers into one SRP-batchable quad and hides the
            // source Tilemaps — see decisions log #78 for why this is needed at all.
            terrainCompositor.Bake(grid, margin);
        }
    }
}
