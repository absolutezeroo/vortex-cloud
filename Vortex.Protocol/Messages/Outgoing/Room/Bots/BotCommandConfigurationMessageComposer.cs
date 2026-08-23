using Orleans;
using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Outgoing.Room.Bots;

/// <summary>Fills the skill-configuration dialog with what the bot is currently set to.</summary>
[GenerateSerializer, Immutable]
public sealed record BotCommandConfigurationMessageComposer : IComposer
{
    [Id(0)]
    public required int BotId { get; init; }

    [Id(1)]
    public required int CommandId { get; init; }

    /// <summary>The command's own encoding, opaque to the server — a chatter's phrase list, a
    /// wander flag. Empty when the skill has never been configured.</summary>
    [Id(2)]
    public string Data { get; init; } = string.Empty;
}
