using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The guild forum terminal (<c>guild_forum</c>). Carries the same guild stuff data as every other
/// recoloured guild furni — see <see cref="Primitives.Groups.GuildFurniStuffData"/> — and adds no
/// server-side behaviour of its own.
/// </summary>
/// <remarks>
/// Name taken from the client: <c>guild_forum.nitro</c> declares
/// <c>logicType: furniture_group_forum_terminal</c>, and <c>RoomObjectFactory.as</c> binds that
/// string to a class extending <c>FurnitureGuildCustomizedLogic</c>. Opening the forum is entirely
/// client-side — that class sets <c>furniture_internal_link = "groupforum/&lt;guildId&gt;"</c> from
/// the guild id in the stuff data and dispatches <c>ROWRE_INTERNAL_LINK</c> on use — so the server
/// only has to deliver the stuff data and stay out of the way. In particular it must not inherit
/// <see cref="FurnitureGateLogic"/>: gate semantics would gate the terminal on its state and
/// restrict use to room controllers, neither of which the client expects.
/// </remarks>
[RoomObjectLogic("furniture_group_forum_terminal")]
public class FurnitureGroupForumTerminalLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx) { }
