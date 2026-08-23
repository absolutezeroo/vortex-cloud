using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// Whether this player already has something in the guide system, asked before the help window
/// opens.
///
/// <see cref="StatusCode"/> drives three different screens: 0 opens the new-help window, 1 shows
/// the pending ticket carried in <see cref="PendingTicket"/>, and anything else is a feedback
/// message the client looks up from <c>statusCode - 2</c>. Sending nothing at all is not a fourth
/// option — the help window opens on this reply, so silence is a window that never appears.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuideReportingStatusMessageComposer : IComposer
{
    [Id(0)]
    public required int StatusCode { get; init; }

    /// <summary>Read by the client only when <see cref="StatusCode"/> is 1, and it must be present
    /// exactly then: the parser branches on the status before touching these bytes.</summary>
    [Id(1)]
    public GuidePendingTicket? PendingTicket { get; init; }
}

/// <summary>
/// The pending ticket block. Its own tail depends on <see cref="TicketType"/>, and one branch
/// depends on <see cref="IsGuide"/> as well — see the serializer.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record GuidePendingTicket
{
    /// <summary>0 and 2 carry the other party only, 1 adds the description, 3 adds the room name
    /// and is written only for a requester. Anything else carries nothing after the header.</summary>
    [Id(0)]
    public required int TicketType { get; init; }

    [Id(1)]
    public required int SecondsAgo { get; init; }

    /// <summary>Whether the player being told is the guide rather than the one who asked. It is not
    /// only a display flag: for ticket type 3 it decides whether the three strings exist at all.</summary>
    [Id(2)]
    public required bool IsGuide { get; init; }

    [Id(3)]
    public required string OtherPartyName { get; init; }

    [Id(4)]
    public required string OtherPartyFigure { get; init; }

    /// <summary>Written for ticket type 1 only.</summary>
    [Id(5)]
    public string Description { get; init; } = string.Empty;

    /// <summary>Written for ticket type 3 only, and then only to a requester.</summary>
    [Id(6)]
    public string RoomName { get; init; } = string.Empty;
}
