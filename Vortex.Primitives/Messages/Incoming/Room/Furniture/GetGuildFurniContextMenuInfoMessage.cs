using Vortex.Primitives.Networking;

namespace Vortex.Primitives.Messages.Incoming.Room.Furniture;

/// <summary>
/// Sent when the player opens the context menu of a guild-customized furni. AS3-verified:
/// <c>RoomObjectEventHandler</c> case <c>ROWRE_GUILD_FURNI_CONTEXT_MENU</c> sends
/// <c>(objectId, guildId)</c>, where the guild id comes from the furni's
/// <c>furniture_guild_customized_guild_id</c> model value.
/// </summary>
public record GetGuildFurniContextMenuInfoMessage : IMessageEvent
{
    public required int ObjectId { get; init; }
    public required int GuildId { get; init; }
}
