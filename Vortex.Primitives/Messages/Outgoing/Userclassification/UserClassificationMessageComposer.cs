using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Moderation;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Userclassification;

/// <summary>
/// The answer to a staff <c>:uc</c> / <c>:anew</c> command: the players that matched, and the label
/// to show beside each. Opens the client's user-classification window.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record UserClassificationMessageComposer : IComposer
{
    [Id(0)]
    public ImmutableArray<UserClassificationEntry> Entries { get; init; } =
        ImmutableArray<UserClassificationEntry>.Empty;
}
