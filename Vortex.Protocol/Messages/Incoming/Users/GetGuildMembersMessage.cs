using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Users;

public record GetGuildMembersMessage : IMessageEvent
{
    public required int GroupId { get; init; }
    public required int PageIndex { get; init; }
    public required string UserNameFilter { get; init; }

    /// <summary>0 = everyone, 1 = pending requests, 2 = filtered by name (client search modes).</summary>
    public required int SearchType { get; init; }
}
