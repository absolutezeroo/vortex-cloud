using System.Collections.Immutable;
using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Outgoing.Room.Bots;

/// <summary>Every configured skill on a bot, replacing whatever the client held.</summary>
[GenerateSerializer, Immutable]
public sealed record BotSkillListUpdateMessageComposer : IComposer
{
    [Id(0)]
    public required int BotId { get; init; }

    [Id(1)]
    public ImmutableArray<BotSkillEntry> Skills { get; init; } =
        ImmutableArray<BotSkillEntry>.Empty;
}

[GenerateSerializer, Immutable]
public sealed record BotSkillEntry
{
    [Id(0)]
    public required int CommandId { get; init; }

    [Id(1)]
    public string Data { get; init; } = string.Empty;
}
