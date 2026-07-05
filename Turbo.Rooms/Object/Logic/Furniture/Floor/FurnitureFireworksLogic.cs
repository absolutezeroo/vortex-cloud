using System.Threading;
using System.Threading.Tasks;
using Turbo.Primitives.Action;
using Turbo.Primitives.Furniture.Enums;
using Turbo.Primitives.Furniture.Providers;
using Turbo.Primitives.Rooms.Object.Furniture.Floor;
using Turbo.Primitives.Rooms.Object.Logic;

namespace Turbo.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A "charge" is a transient effect (the client renders the firework animation for the requested
/// state), not a fact worth remembering across room reactivations — so unlike most floor items,
/// this stays <see cref="StuffPersistanceType.RoomActive"/> instead of persisting to <c>ExtraData</c>.
/// </summary>
[RoomObjectLogic("fireworks")]
public class FurnitureFireworksLogic(IStuffDataFactory stuffDataFactory, IRoomFloorItemContext ctx)
    : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        if (param < 0 || param >= _ctx.Definition.TotalStates)
        {
            return;
        }

        await SetStateAsync(param);
    }
}
