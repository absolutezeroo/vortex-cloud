using Orleans;
using Vortex.Primitives.Networking;
using Vortex.Primitives.Preferences.Enums;

namespace Vortex.Primitives.Messages.Outgoing.Preferences;

/// <summary>
/// The outcome of an add or a remove, and the word it applied to. This is what actually moves a
/// word on or off the client's list — it applies nothing itself, which is also what keeps two
/// clients on one account consistent.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ModifyCustomFilterResultMessageComposer : IComposer
{
    [Id(0)]
    public required WordFilterModifyResultType Result { get; init; }

    [Id(1)]
    public required string Word { get; init; }
}
