using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Availability;

/// <summary>
/// The hotel has closed (header 3058) — when it opens again, and whether this player was thrown
/// out by the closure rather than leaving on their own.
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2152/_SafeCls_3608.as): two ints then a boolean.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record InfoHotelClosedMessageComposer : IComposer
{
    [Id(0)]
    public required int OpenHour { get; init; }

    [Id(1)]
    public required int OpenMinute { get; init; }

    /// <summary>True when the closure is what ejected this player.</summary>
    [Id(2)]
    public required bool UserThrownOutAtClose { get; init; }
}
