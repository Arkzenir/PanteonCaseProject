using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// The building type that produces units. Adds a dedicated spawn point for the soldiers
    /// it produces — the one thing that differs from <see cref="PowerPlant"/>.
    /// </summary>
    public class Barracks : BuildingBase
    {
        [SerializeField] private Transform spawnPoint;

        public override Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : base.SpawnPosition;
    }
}
