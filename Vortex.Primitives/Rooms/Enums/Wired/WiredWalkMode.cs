namespace Vortex.Primitives.Rooms.Enums.Wired;

/// <summary>
/// What happens to a user's walk in progress when a wired effect relocates them. Mirrors the client's
/// <c>user_move.walkmode</c> radio group, labelled "If the user was walking:".
/// </summary>
public enum WiredWalkMode
{
    /// <summary>
    /// "Keep walking if moved closer to target" — resume towards the original goal only when the move
    /// left the user nearer to it than they were; otherwise the walk is dropped. The client default.
    /// </summary>
    KeepIfCloser = 0,

    /// <summary>"Keep walking" — always resume towards the original goal from wherever the user landed.</summary>
    Keep = 1,

    /// <summary>"Stop walking" — drop the walk entirely; the user stays where the effect put them.</summary>
    Stop = 2,
}
