using Turbo.Primitives.Networking;

namespace Turbo.Primitives.Messages.Incoming.Users;

public record UpdateGuildSettingsMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int GuildType { get; init; }
    public required int RightsLevel { get; init; }
}
