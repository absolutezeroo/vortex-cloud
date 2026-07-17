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

    /// <summary>Wire-safety ceiling on a single <c>AddItemsToTrade</c> batch. The real per-trade
    /// item cap is business policy enforced in the trade session; this only bounds the length-prefixed
    /// array so a hostile client can't force a huge allocation.</summary>
    public int MaxTradeItems { get; init; } = 1500;
}
