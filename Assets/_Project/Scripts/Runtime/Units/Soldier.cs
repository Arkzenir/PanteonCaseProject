namespace CaseGame.Units
{
    /// <summary>
    /// The one concrete soldier type. All 3 brief-mandated soldier variants (10/5/2 attack
    /// damage, GI-9) are data variants of this same class via different <c>UnitDefinition</c>
    /// assets and prefabs, not separate classes — see <see cref="SoldierBase"/>'s doc comment.
    /// </summary>
    public class Soldier : SoldierBase
    {
    }
}
