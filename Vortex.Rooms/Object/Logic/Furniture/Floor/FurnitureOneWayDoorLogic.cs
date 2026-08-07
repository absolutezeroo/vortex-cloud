using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// The one-way gate: step in one side, come out the other, and no walking back through it.
/// </summary>
/// <remarks>
/// Its open/closed is transient and deliberately never written to stuff data. A gate is only "open"
/// for the instant somebody passes through it, so persisting that state would leave a gate stuck
/// open across a room reload if the server stopped mid-pass — and would broadcast a stuff-data
/// refresh on every use for something the client already learns from the dedicated
/// <c>OneWayDoorStatus</c> packet.
/// <para>
/// The inherited use-toggle is off for the same reason: <c>total_states</c> is 1 on all four
/// definitions, so advancing the state is a no-op that would still persist and broadcast.
/// </para>
/// </remarks>
[RoomObjectLogic("furniture_one_way_door")]
public class FurnitureOneWayDoorLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    /// <summary>Shown as closed; what the gate reports when nobody is passing.</summary>
    public const int StatusClosed = 0;

    /// <summary>Shown as open, for the moment somebody is going through.</summary>
    public const int StatusOpen = 1;

    public override Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct) =>
        Task.CompletedTask;
}
