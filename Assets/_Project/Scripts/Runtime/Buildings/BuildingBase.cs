using CaseGame.Entities;

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
    }
}
