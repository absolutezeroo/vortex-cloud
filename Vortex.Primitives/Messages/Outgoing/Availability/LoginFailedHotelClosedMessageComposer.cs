using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Availability;

/// <summary>
/// Login was refused because the hotel is closed (header 698), and when it opens again.
///
/// Shape from WIN63's parser (unknowns/_SafePkg_2152/_SafeCls_2990.as): two ints. Note this is a
/// different message from <see cref="InfoHotelClosedMessageComposer"/>, which carries a third
/// field; the two parsers are distinct classes in the same package.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record LoginFailedHotelClosedMessageComposer : IComposer
{
    [Id(0)]
    public required int OpenHour { get; init; }

    [Id(1)]
    public required int OpenMinute { get; init; }
}
