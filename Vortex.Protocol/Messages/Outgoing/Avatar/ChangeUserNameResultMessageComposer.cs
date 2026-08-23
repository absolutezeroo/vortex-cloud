using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Avatar;

/// <summary>
/// Answers <see cref="Incoming.Avatar.ChangeUserNameMessage"/>. Same payload shape as the check
/// result; only <see cref="NameChangeResultCode.Ok"/> advances the onboarding flow.
/// </summary>
[GenerateSerializer, Immutable]
public sealed record ChangeUserNameResultMessageComposer : IComposer
{
    [Id(0)]
    public required int ResultCode { get; init; }

    [Id(1)]
    public required string Name { get; init; }

    [Id(2)]
    public required ImmutableArray<string> NameSuggestions { get; init; }
}
