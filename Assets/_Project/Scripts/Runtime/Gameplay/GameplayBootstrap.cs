using CaseGame.Buildings;
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
        [SerializeField] private PlacementController placementController;
        [SerializeField] private SelectionController selectionController;
        [SerializeField] private UnitProductionController unitProductionController;

        private void Start()
        {
            var grid = gridView.GridModel;
            var buildingFactory = new BuildingFactory(buildingsContainer);
            var unitFactory = new UnitFactory(unitsContainer);

            placementController.Initialize(grid, buildingFactory);
            selectionController.Initialize(grid);
            unitProductionController.Initialize(unitFactory, grid);
        }
    }
}
