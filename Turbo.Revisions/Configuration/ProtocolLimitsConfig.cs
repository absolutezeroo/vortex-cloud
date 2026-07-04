namespace Turbo.Revisions.Configuration;

/// <summary>
///     Wire-safety bounds on client-declared collection sizes inside packet parsers. These guard
///     against malformed/hostile length-prefixed fields, not business policy (see
///     <c>Turbo.Players.Configuration.MessengerConfig</c> for the account-level friend cap).
/// </summary>
public sealed class ProtocolLimitsConfig
{
    public const string SECTION_NAME = "Turbo:Protocol";

    public int MaxFriendRemovalIds { get; init; } = 1000;
    public int MaxRoomTags { get; init; } = 100;
}
