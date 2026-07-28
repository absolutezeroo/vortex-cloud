using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// Guild-customized floor furni (guild gates, badges, banners). The logic name matches the client's
/// <c>FurnitureGuildCustomizedLogic</c> binding, which reads the guild id, badge code and both
/// recolours out of the item's string stuff data — see <see cref="Primitives.Groups.GuildFurniStuffData"/>
/// for that layout, stamped at purchase time.
/// </summary>
/// <remarks>
/// Access control rides on <see cref="FurnitureUsageType.Controller"/> rather than a bespoke
/// membership lookup: in a guild base the room's controller level already resolves to
/// <see cref="RoomControllerType.GroupRights"/> / <see cref="RoomControllerType.GroupAdmin"/> for
/// members, so "only the guild may open its own gate" falls out of the room security policy without
/// a second source of truth to keep in sync.
/// </remarks>
[RoomObjectLogic("furniture_guild_customized")]
public class FurnitureGuildCustomizedLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureGateLogic(stuffDataFactory, ctx) { }
