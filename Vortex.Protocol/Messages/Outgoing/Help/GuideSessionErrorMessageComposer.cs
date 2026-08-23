using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Help;

/// <summary>
/// The request went nowhere.
/// </summary>
/// <remarks>
/// The client subtracts one before switching on the code, so 1 is its "rejected" branch and 2 and 3
/// are its "not enough guardians" one. Sending 0 would fall through to a default that tells the
/// player nothing.
/// </remarks>
[GenerateSerializer, Immutable]
public sealed record GuideSessionErrorMessageComposer : IComposer
{
    [Id(0)]
    public required int ErrorCode { get; init; }
}
