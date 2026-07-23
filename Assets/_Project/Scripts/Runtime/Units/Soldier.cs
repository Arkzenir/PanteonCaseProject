namespace CaseGame.Units
{
    /// <summary>
    /// The one concrete soldier type. All soldier variants (differing only in attack damage)
    /// are data variants of this same class via different <c>UnitDefinition</c> assets and
    /// prefabs, not separate classes — see <see cref="SoldierBase"/>'s doc comment.
    /// </summary>
    public class Soldier : SoldierBase
    {
    }
}
