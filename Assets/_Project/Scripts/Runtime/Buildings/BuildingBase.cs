using CaseGame.Entities;
using UnityEngine;

namespace CaseGame.Buildings
{
    /// <summary>
    /// Building-specific base on top of <see cref="GameEntityBase"/>. All the actual behavior
    /// (Health, IDamageable, sprite, death callback) lives on the shared base; this just
    /// re-exposes <see cref="Definition"/> as the strongly-typed <see cref="BuildingDefinition"/>
    /// for consumers (e.g. a future Info Panel reading <c>ProducibleUnits</c>).
    /// </summary>
    public abstract class BuildingBase : GameEntityBase
    {
        public new BuildingDefinition Definition => (BuildingDefinition)base.Definition;

        /// <summary>Where this building's produced units appear (GI-7). Defaults to the building's own position; <see cref="Barracks"/> overrides it with a dedicated spawn point. Virtual so callers (e.g. Info Panel unit production) never need to type-check for "is this a Barracks" — any building capable of producing units can be asked this generically (requirement 2's modularity mandate).</summary>
        public virtual Vector3 SpawnPosition => transform.position;
    }
}
