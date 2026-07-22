using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Only building type that produces units (GI-6). Adds a designated spawn point for
    /// soldiers it produces (GI-7) — the one thing that differs from <see cref="PowerPlant"/>.
    /// </summary>
    public class Barracks : BuildingBase
    {
        [SerializeField] private Transform spawnPoint;

        public override Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : base.SpawnPosition;
    }
}
