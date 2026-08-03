using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// Guild-customized floor furni (badges, banners, carpets, tiles, seating). The logic name matches
/// the client's <c>FurnitureGuildCustomizedLogic</c> binding, which reads the guild id, badge code
/// and both recolours out of the item's string stuff data — see
/// <see cref="Primitives.Groups.GuildFurniStuffData"/> for that layout, stamped at purchase time.
/// </summary>
/// <remarks>
/// Multi-state, not a gate. This used to derive from <see cref="FurnitureGateLogic"/>, which
/// overrides <c>CanWalk()</c> to "state == 1" and forces
/// <see cref="FurnitureUsageType.Controller"/>. Nothing in the database referenced this logic yet,
/// so the mismatch was invisible — but the definitions it is meant to serve include walkable,
/// many-stated furni (<c>gld_carpet</c> and <c>gld_tile1</c>/<c>gld_tile2</c> are walkable with 11
/// and 4 states), which the gate rules would have made impassable outside a single state and
/// unusable by ordinary guild members. The client agrees: its
/// <c>FurnitureGuildCustomizedLogic extends FurnitureMultiStateLogic</c>, with no gate behaviour.
/// The one guild furni that really is a gate has its own logic, see
/// <see cref="FurnitureGuildGateLogic"/>.
/// </remarks>
[RoomObjectLogic("furniture_guild_customized")]
public class FurnitureGuildCustomizedLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx) { }
