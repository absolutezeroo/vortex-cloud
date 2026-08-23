using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Game.Directory;

/// <summary>Starting a snowwar on a named server game, as opposed to the quick-join flow.</summary>
public record Game2StartSnowWarMessage : IMessageEvent
{
    /// <summary>The string the client passes to startServerGame(); its meaning is not established
    /// from the client alone, only that it is sent and that quick-join sends nothing.</summary>
    public required string GameName { get; init; }
}
