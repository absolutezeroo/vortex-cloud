using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Advertisement;

/// <summary>
/// Why a room ad was rejected, and the text the filter left behind (header 2396).
///
/// Shape from WIN63's parser (unknowns/_SafePkg_1719/_SafeCls_2955.as): an int and a string. The
/// error code selects which of the two inputs gets the error - 0 the event name, 1 the description
/// - and <c>FilteredText</c> is put back into that input, so the player sees what survived
/// filtering rather than losing what they typed.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record RoomAdErrorEventMessageComposer : IComposer
{
    /// <summary>0 = the name was rejected, 1 = the description was.</summary>
    [Id(0)]
    public required int ErrorCode { get; init; }

    /// <summary>The player's text after filtering, written back into the offending input.</summary>
    [Id(1)]
    public required string FilteredText { get; init; }
}
