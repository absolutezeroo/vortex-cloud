namespace Vortex.Primitives.Moderation;

/// <summary>
/// What became of one id in a pick request. A pick is not all-or-nothing: the client sends whole
/// bundles of ids and two moderators can reach for the same one, so every id gets its own verdict.
/// </summary>
/// <param name="IssueId">The ticket the moderator asked for.</param>
/// <param name="Acquired">True when this call is the one that took it.</param>
/// <param name="PickerPlayerId">
/// Who holds it now — the caller when <paramref name="Acquired"/> is true, otherwise the moderator
/// who got there first. Zero when the ticket no longer exists or was already closed, which the
/// client renders as an unattributed failure rather than naming somebody.
/// </param>
/// <param name="PickerPlayerName">Display name for <paramref name="PickerPlayerId"/>.</param>
public readonly record struct CfhTicketPickOutcome(
    int IssueId,
    bool Acquired,
    int PickerPlayerId,
    string PickerPlayerName
);
