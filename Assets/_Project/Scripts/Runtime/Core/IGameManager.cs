namespace CaseGame.Core
{
    /// <summary>
    /// Narrow, testable contract for the game's single cross-scene coordinator. Other systems
    /// should depend on this interface rather than the concrete <see cref="GameManager"/>
    /// Singleton, per CLAUDE.md's "isolate behind an interface" rule for brief-mandated
    /// singletons.
    /// </summary>
    public interface IGameManager
    {
        void LoadScene(string sceneName);
    }
}
