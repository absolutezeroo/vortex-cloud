using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Orleans;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Primitives.Rooms.Wired;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;

/// <summary>"WIRED Text Add-on: Username placeholder" — <c>$(name)</c> becomes the name of the
/// user the pile is acting on.</summary>
[RoomObjectLogic("wf_xtra_text_output_username")]
public class WiredAddonUsernamePlaceholder(
    IGrainFactory grainFactory,
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : WiredAddonTextPlaceholder(grainFactory, stuffDataFactory, ctx)
{
    public override int WiredCode => (int)WiredAddonType.USERNAME_PLACEHOLDER;

    public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
        [
            [
                WiredPlayerSourceType.TriggeredUser,
                WiredPlayerSourceType.SelectorUsers,
                WiredPlayerSourceType.SignalUsers,
            ],
        ];

    protected override async Task<IReadOnlyList<string>> ResolveValuesAsync(
        IWiredExecutionContext ctx,
        CancellationToken ct
    )
    {
        IWiredSelectionSet selection = await ctx.GetEffectiveSelectionAsync(this, ct);
        List<string> names = [];

        foreach (int playerId in selection.SelectedPlayerIds)
        {
            // A player who left between the trigger and the text contributes nothing rather than an
            // empty slot in the middle of the sentence.
            if (_ctx.Lookup.TryFindAvatarByPlayer(playerId, out IRoomAvatar? avatar))
            {
                names.Add(avatar.Name);
            }
        }

        return names;
    }
}
