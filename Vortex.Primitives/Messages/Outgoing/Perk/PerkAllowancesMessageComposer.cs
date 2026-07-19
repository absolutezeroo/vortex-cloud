using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Perk;

[GenerateSerializer, Immutable]
public sealed record PerkAllowancesMessageComposer : IComposer
{
    [Id(0)]
    public required ImmutableArray<PerkAllowanceItem> Perks { get; init; }
}

[GenerateSerializer, Immutable]
public sealed record PerkAllowanceItem
{
    [Id(0)]
    public required string Code { get; init; }

    [Id(1)]
    public required string ErrorMessage { get; init; }

    [Id(2)]
    public required bool IsAllowed { get; init; }
}
