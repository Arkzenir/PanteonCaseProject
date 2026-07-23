namespace CaseGame.Core
{
    /// <summary>
    /// Narrow, testable contract for the game's single cross-scene coordinator. Other systems
    /// should depend on this interface rather than the concrete <see cref="GameManager"/>
    /// singleton.
    /// </summary>
    public interface IGameManager
    {
        void LoadScene(string sceneName);
    }
}
