namespace Vortex.Primitives.Players.Providers;

/// <summary>
/// Resolves the level the profile shows from a player's achievement score. The client has no ladder
/// of its own — it renders the number the server sends — so this is the only thing that decides it.
/// </summary>
public interface IAccountLevelProvider
{
    /// <summary>The player's level; never below 1, which is what a new account shows.</summary>
    int ResolveLevel(int achievementScore);
}
