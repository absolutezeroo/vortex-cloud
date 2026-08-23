using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Availability;

/// <summary>
/// The hotel is about to close (header 184).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2152/_SafeCls_2483.as): a single int.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record InfoHotelClosingMessageComposer : IComposer
{
    [Id(0)]
    public required int MinutesUntilClosing { get; init; }
}
