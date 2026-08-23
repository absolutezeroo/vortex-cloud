using Vortex.Primitives.Networking;

namespace Vortex.Protocol.Messages.Incoming.Users;

public record UpdateGuildColorsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int PrimaryColorId { get; init; }
    public required int SecondaryColorId { get; init; }
}
