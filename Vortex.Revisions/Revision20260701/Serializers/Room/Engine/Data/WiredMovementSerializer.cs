using Vortex.Primitives.Packets;
using Vortex.Primitives.Rooms.Snapshots.Wired;

namespace Vortex.Revisions.Revision20260701.Serializers.Room.Engine.Data;

internal class WiredMovementSerializer
{
    public static void SerializeUserMovement(IServerPacket packet, WiredUserMovementSnapshot item)
    {
        packet
            .WriteInteger(item.SourceX)
            .WriteInteger(item.SourceY)
            .WriteInteger(item.TargetX)
            .WriteInteger(item.TargetY)
            .WriteString(item.SourceZ.ToString())
            .WriteString(item.TargetZ.ToString())
            .WriteInteger(item.ObjectId.Value)
            .WriteInteger((int)item.MoveType)
            .WriteInteger(item.AnimationTime)
            .WriteInteger((int)item.BodyDirection)
            .WriteInteger((int)item.HeadDirection);

        // The client reads this presence boolean unconditionally and only then the value
        // (AS3 _SafeCls_3809.parseUserMove: `if(readBoolean()) jumpPower = readInteger()`).
        // Omitting it truncated the packet and threw "End of buffer" on the whole bundle.
        WriteOptionalInteger(packet, item.JumpPower);
    }

    public static void SerializeFloorItemMovement(
        IServerPacket packet,
        WiredFloorItemMovementSnapshot item
    )
    {
        packet
            .WriteInteger(item.SourceX)
            .WriteInteger(item.SourceY)
            .WriteInteger(item.TargetX)
            .WriteInteger(item.TargetY)
            .WriteString(item.SourceZ.ToString())
            .WriteString(item.TargetZ.ToString())
            .WriteInteger(item.ObjectId.Value)
            .WriteInteger(item.AnimationTime)
            .WriteInteger((int)item.Rotation);

        WriteOptionalInteger(packet, item.OvershootingDistance);
        WriteOptionalInteger(packet, item.CurveStrength);
    }

    public static void SerializeWallItemMovement(
        IServerPacket packet,
        WiredWallItemMovementSnapshot item
    )
    {
        packet
            .WriteInteger(item.ObjectId.Value)
            .WriteBoolean(item.IsDirectionRight)
            .WriteInteger(item.SourceX)
            .WriteInteger(item.SourceY)
            .WriteInteger(item.SourceOffsetX)
            .WriteInteger(item.SourceOffsetY)
            .WriteInteger(item.TargetX)
            .WriteInteger(item.TargetY)
            .WriteInteger(item.TargetOffsetX)
            .WriteInteger(item.TargetOffsetY)
            .WriteInteger(item.AnimationTime);
    }

    public static void SerializeUserDirection(IServerPacket packet, WiredUserDirectionSnapshot item)
    {
        packet
            .WriteInteger(item.ObjectId.Value)
            .WriteInteger((int)item.BodyRotation)
            .WriteInteger((int)item.HeadRotation);
    }

    private static void WriteOptionalInteger(IServerPacket packet, int? value)
    {
        packet.WriteBoolean(value.HasValue);

        if (value.HasValue)
        {
            packet.WriteInteger(value.Value);
        }
    }
}
