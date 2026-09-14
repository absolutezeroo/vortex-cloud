using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Avatars;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Grains;

namespace Vortex.Rooms.Wired.Variables.User;

/// <summary>
/// Which dance is running, 0 when standing still. A write that names no dance this hotel has is
/// refused rather than clamped — a builder who wrote 9 meant something, and silently making them
/// dance the Rollie would hide the mistake.
/// </summary>
public sealed class UserDanceVariable(RoomGrain roomGrain) : UserPlayerVariable(roomGrain)
{
    protected override string VariableName => "@dance";
    protected override WiredVariableGroupSubBandType SubBandType =>
        WiredVariableGroupSubBandType.Base;
    protected override ushort Order => 150;
    protected override WiredVariableFlags Flags =>
        WiredVariableFlags.HasValue
        | WiredVariableFlags.CanWriteValue
        | WiredVariableFlags.AlwaysAvailable
        | WiredVariableFlags.HasTextConnector;

    protected override Dictionary<WiredVariableValue, string> GetTextConnectors() =>
        Enum.GetValues<AvatarDanceType>()
            .ToDictionary(v => WiredVariableValue.Parse((int)v), v => v.ToString());

    protected override bool TryGetValueForAvatar(IRoomPlayer avatar, out WiredVariableValue value)
    {
        value = WiredVariableValue.Parse((int)avatar.DanceType);

        return true;
    }

    protected override Task<bool> SetValueForAvatarAsync(
        IWiredExecutionContext ctx,
        IRoomPlayer avatar,
        WiredVariableValue value
    ) =>
        Enum.IsDefined((AvatarDanceType)value.Value)
            ? _roomGrain.AvatarModule.SetAvatarDanceAsync(
                avatar.ObjectId,
                (AvatarDanceType)value.Value,
                CancellationToken.None
            )
            : Task.FromResult(false);
}
