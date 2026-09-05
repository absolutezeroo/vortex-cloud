using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Object.Logic;
using Vortex.Protocol.Messages.Outgoing.Room.Engine;
using Vortex.Rooms.Object.Avatars.Player;

namespace Vortex.Rooms.Object.Logic.Furniture.Floor;

/// <summary>
/// A clothing-change booth (<c>fball_gate</c>, the football kit changer). Stepping onto it puts the
/// outfit it holds for your gender on; stepping onto it again takes it off.
/// <para>
/// <b>The key is the client's.</b> <c>furniture_clothing_change</c> is what the furnidata carries
/// for <c>fball_gate</c>, and the name the client resolves to its own
/// <c>FurnitureClothingChangeLogic</c> — which double-clicks into the clothing widget and reads this
/// item's legacy string as <c>&lt;boy&gt;,&lt;girl&gt;</c>. Without a class on that key the booth
/// bound to the default floor logic: it reported no walk-on and dressed nobody.
/// </para>
/// <para>
/// <b>What is known and what is not.</b> The two-outfit string and the widget are the client's, and
/// so is the write path — <c>SetClothingChangeData</c> (header 1220) already stores one gender's
/// look through the room grain. The dressing itself is NOT in the client: it renders whatever figure
/// the server broadcasts, so which parts a booth replaces, and the fact that a second step undresses
/// you, are the reference emulator's behaviour. Evidence, not authority; the split lives in
/// <see cref="ClothingChangeData"/> where it can be corrected in one place.
/// </para>
/// <para>
/// Nothing here is persisted. The worn look lives on the room avatar and dies with the visit, so a
/// booth can never write a kit into somebody's saved figure.
/// </para>
/// </summary>
[RoomObjectLogic("furniture_clothing_change")]
public sealed class FurnitureClothingChangeLogic(
    IStuffDataFactory stuffDataFactory,
    IRoomFloorItemContext ctx
) : FurnitureFloorLogic(stuffDataFactory, ctx)
{
    /// <summary>A booth you cannot step onto can never dress anybody, whatever its definition row
    /// happens to say.</summary>
    public override bool CanWalk() => true;

    public override async Task OnWalkOnAsync(IRoomAvatarContext ctx, CancellationToken ct)
    {
        await base.OnWalkOnAsync(ctx, ct);

        // Bots and pets have no wardrobe and no gender to pick a side with.
        if (ctx.RoomObject is not RoomPlayerAvatar player)
        {
            return;
        }

        if (!player.RemoveBoothOutfit())
        {
            string outfit = ClothingChangeData.LookFor(GetLegacyString(), player.Gender);

            if (string.IsNullOrEmpty(outfit))
            {
                // Nobody has configured this side of the booth yet. Dressing them in nothing would
                // leave a floating head.
                return;
            }

            player.WearBoothOutfit(ClothingChangeData.Dress(player.Figure, outfit));
        }

        // The room, not just the wearer: everyone standing there has to see the kit go on. The
        // achievement score is the avatar's own copy — the same number RoomAvatarSerializer already
        // sent every client at room entry — so this field re-states what they hold rather than
        // guessing at a player total the room does not carry.
        await _ctx.SendComposerToRoomAsync(
            new UserChangeMessageComposer
            {
                ObjectId = player.ObjectId,
                Figure = player.Figure,
                Gender = player.Gender,
                CustomInfo = player.Motto,
                AchievementScore = player.ActivityPoints,
            }
        );
    }
}
