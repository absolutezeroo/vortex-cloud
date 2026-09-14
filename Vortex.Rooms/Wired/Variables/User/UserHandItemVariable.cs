using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// What is in the avatar's hand, 0 when empty. A hand item expires on the room clock rather than
/// being taken away, so this goes back to 0 on its own — a chain that reads it a few seconds later
/// may well see nothing, and that is the item having been drunk, not a lost write.
/// </summary>
/// <remarks>
/// Writing 0 empties the hand and anything else fills it, both through the hand-item module, which
/// is where the expiry and the broadcast live. Only a player can be handed something: the module is
/// keyed by account, so a pet or a bot has no hand to reach for and the write refuses rather than
/// pretending.
/// </remarks>
public sealed class UserHandItemVariable(RoomGrain roomGrain) : UserVariable<IRoomAvatar>(roomGrain)
{
    protected override string VariableName => "@handitem";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 90;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable;

    protected override bool TryGetValueForAvatar(IRoomAvatar avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse(avatar.CarryItemId);

        return true;
    }

    protected override Task<bool> SetValueForAvatarAsync(
        IWiredExecutionContext ctx,
        IRoomAvatar avatar,
        WiredVariableValue value
    )
    {
        if (avatar is not IRoomPlayer player || value < 0)
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(
            value == 0
                ? _roomGrain.HandItemModule.Drop(player.PlayerId)
                : _roomGrain.HandItemModule.Give(player.PlayerId, value)
        );
    }
}
