using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Action;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A bar, a plant, a samovar: the player uses it, it plays its dispensing animation, and hands them
/// something to hold.
/// </summary>
/// <remarks>
/// <para>
/// Seven hundred and seventy-four definitions in this hotel bind to one of the three vending logic
/// names and, until now, to no class at all — so every one of them fell through to the family
/// default and did nothing when clicked. That is the largest single block of inert furniture in the
/// catalogue.
/// </para>
/// <para>
/// <b>What is known and what is not.</b> The behaviour is well attested: state 1 while dispensing,
/// a hand item after a beat, back to state 0. What no source in this workspace says is *which* item
/// a given machine gives — not the official client, not the furnidata, not any capture. So that is
/// a column an operator fills in (<c>furniture_definitions.vending_ids</c>) rather than a mapping
/// invented here, and a machine nobody has configured hands out nothing rather than something
/// plausible-looking.
/// </para>
/// <para>
/// The player has to be next to it. Habbo's own client walks you to a machine before the use
/// arrives, so this checks proximity rather than pathing anybody: a use from across the room is a
/// client that did not walk, and refusing it is cheaper than teaching furniture to move avatars.
/// </para>
/// </remarks>
[RoomObjectLogic("vendingmachine")]
[RoomObjectLogic("vendingmachine_no_sides")]
[RoomObjectLogic("vending")]
public class FurnitureVendingMachineLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    /// <summary>Idle. What the machine shows when nobody is using it.</summary>
    private const int IdleState = 0;

    /// <summary>Dispensing. The animation the client plays while it works.</summary>
    private const int DispensingState = 1;

    /// <summary>
    /// How long the dispensing animation runs before the machine goes back to idle.
    /// </summary>
    /// <remarks>
    /// ponytail: a constant, not a knob. Nothing tunes this per hotel and the number is the client's
    /// animation length rather than a policy — if a furni ever needs its own, it belongs on the
    /// definition beside `vending_ids` and not in configuration.
    /// </remarks>
    private const int AnimationMs = 1_500;

    /// <summary>
    /// The state is the animation, and an animation is not worth a database row — a machine caught
    /// mid-dispense by a restart should come back idle, not stuck showing a drink it never gave.
    /// </summary>
    protected override StuffPersistanceType _stuffPersistanceType =>
        StuffPersistanceType.RoomActive;

    public override async Task OnUseAsync(ActionContext ctx, int param, CancellationToken ct)
    {
        if (ctx.PlayerId <= 0 || GetState() == DispensingState)
        {
            // Already working. Re-entering would restart the animation and hand out a second drink
            // for one use, which is what a player double-clicking gets for free otherwise.
            return;
        }

        if (!IsWithinReach(ctx.PlayerId))
        {
            return;
        }

        int handItemId = PickHandItem();

        // The animation plays either way. A machine nobody has configured that also refused to
        // animate would be indistinguishable from the broken one this used to be; one that animates
        // and hands nothing is visibly a machine waiting for its `vending_ids`, which is what the
        // furniture admin page shows and what an operator can act on.
        await SetStateAsync(DispensingState);

        if (handItemId != 0)
        {
            await _ctx.RoomAs<IRoomAvatars>()
                .GiveCarryItemAsync(ctx.PlayerId, handItemId, CancellationToken.None)
                .ConfigureAwait(true);
        }

        // Back to idle after the client has had time to play the animation. Detached on purpose:
        // the room's turn must not wait most of a second for a drink.
        _ = ResetAfterAnimationAsync();
    }

    /// <remarks>
    /// Swallows on purpose and without a logger, which this context does not carry. The only way
    /// this fails is the room going away underneath it, and a machine whose reset never ran comes
    /// back idle anyway — the state is <see cref="StuffPersistanceType.RoomActive"/>.
    /// </remarks>
    private async Task ResetAfterAnimationAsync()
    {
        try
        {
            await Task.Delay(AnimationMs, CancellationToken.None).ConfigureAwait(false);

            if (GetState() == DispensingState)
            {
                await SetStateAsync(IdleState);
            }
        }
        catch (Exception)
        {
            // The room is gone. Nothing to reset and nowhere to say so.
        }
    }

    /// <summary>
    /// Whether the player is on the machine's own tile or standing against it.
    /// </summary>
    /// <remarks>
    /// Adjacency rather than the single tile in front, because a machine two tiles wide has more
    /// than one front and the client will have walked the player to whichever was nearest.
    /// </remarks>
    private bool IsWithinReach(PlayerId playerId)
    {
        if (!_ctx.Lookup.TryFindAvatarByPlayer(playerId, out IRoomAvatar? avatar))
        {
            return false;
        }

        return Math.Abs(avatar.X - _ctx.RoomObject.X) <= 1
            && Math.Abs(avatar.Y - _ctx.RoomObject.Y) <= 1;
    }

    /// <summary>
    /// One of the configured hand items, at random, or 0 when nothing is configured.
    /// </summary>
    /// <remarks>
    /// Random per use rather than cycling: a bar that hands out its list in order is a bar every
    /// player learns to game, and every implementation of this picks.
    /// </remarks>
    private int PickHandItem()
    {
        IReadOnlyList<int> ids = _ctx.Definition.VendingIds;

        return ids.Count == 0 ? 0 : ids[Random.Shared.Next(ids.Count)];
    }
}
