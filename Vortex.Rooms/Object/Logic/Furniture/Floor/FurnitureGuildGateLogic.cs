using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The guild gate (<c>gld_gate</c>), the one guild furni whose state controls passage.
/// </summary>
/// <remarks>
/// This name is server-side only and deliberately diverges from the client. In the assets,
/// <c>gld_gate.nitro</c> declares <c>logicType: furniture_guild_customized</c> — the same as a guild
/// carpet — because the Flash client derives blocking from the visualization and the tile state the
/// server sends, not from a distinct logic class. Vortex resolves walkability server-side, so the
/// gate needs its own logic; folding it back into
/// <see cref="FurnitureGuildCustomizedLogic"/> would force gate semantics onto every recoloured
/// guild furni and make a guild carpet unwalkable outside state 1.
/// <para>
/// Access control rides on <see cref="FurnitureUsageType.Controller"/> rather than a bespoke
/// membership lookup: in a guild base the room's controller level already resolves to
/// <see cref="RoomControllerType.GroupRights"/> / <see cref="RoomControllerType.GroupAdmin"/> for
/// members, so "only the guild may open its own gate" falls out of the room security policy without
/// a second source of truth to keep in sync.
/// </para>
/// </remarks>
[RoomObjectLogic("furniture_guild_gate")]
public class FurnitureGuildGateLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureGateLogic(stuffDataFactory, ctx) { }
