using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record UpdateGuildSettingsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int GuildType { get; init; }
    public required int RightsLevel { get; init; }
}
