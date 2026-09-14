using System;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Text;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;

namespace Vortex.Rooms.Wired.Variables;

public static class WiredVariableIdBuilder
{
    public static WiredVariableId CreateFromBoxId(int boxId) =>
        new(
            ((ulong)((int)WiredVariableIdSourceType.Database & 0b1_1111) << 48) | HashBoxId48(boxId)
        );

    /// <summary>
    /// The id of a variable an add-on derives from the box's own — distinct per slot, stable across
    /// restarts, and in the same band as the parent so the two sort together.
    /// </summary>
    /// <remarks>
    /// Stability is the whole requirement. The client keys a room's variables by id and a wired box
    /// names the ones it reads by id, so a derived variable that came back under a new number after
    /// a restart would silently unbind every box pointing at it. Hashing the slot alongside the box
    /// gives that; a counter would not.
    /// </remarks>
    public static WiredVariableId CreateFromBoxSubId(int boxId, int slot) =>
        new(
            ((ulong)((int)WiredVariableIdSourceType.Database & 0b1_1111) << 48)
                | HashBoxSubId48(boxId, slot)
        );

    public static WiredVariableId CreateInternalOrdered(
        WiredVariableTargetType targetType,
        string name,
        WiredVariableGroupSubBandType subBand,
        ushort order
    ) => new(CreateOrdered(targetType, name, MapGroupBand(targetType), subBand, order));

    private static WiredVariableGroupBandType MapGroupBand(WiredVariableTargetType targetType) =>
        targetType switch
        {
            WiredVariableTargetType.Furni => WiredVariableGroupBandType.Furni,
            WiredVariableTargetType.User => WiredVariableGroupBandType.User,
            WiredVariableTargetType.Global => WiredVariableGroupBandType.Global,
            WiredVariableTargetType.Context => WiredVariableGroupBandType.Context,
            _ => WiredVariableGroupBandType.Other,
        };

    public static ulong CreateOrdered(
        WiredVariableTargetType targetType,
        string name,
        WiredVariableGroupBandType groupBand,
        WiredVariableGroupSubBandType subBand,
        ushort order
    )
    {
        ushort tie16 = HashTie16(targetType, groupBand, subBand, order, name);

        ulong id =
            ((ulong)(byte)groupBand << 40)
            | ((ulong)(byte)subBand << 32)
            | ((ulong)order << 16)
            | tie16;

        return id;
    }

    private static ushort HashTie16(
        WiredVariableTargetType targetType,
        WiredVariableGroupBandType groupBand,
        WiredVariableGroupSubBandType subBand,
        ushort order,
        string name
    )
    {
        XxHash64 hasher = new XxHash64();

        WriteInt32BE(ref hasher, (int)targetType);
        WriteByte(ref hasher, (byte)groupBand);
        WriteByte(ref hasher, (byte)subBand);
        WriteInt32BE(ref hasher, order);
        WriteString(ref hasher, name);

        return (ushort)(hasher.GetCurrentHashAsUInt64() & 0xFFFF);
    }

    private static ulong HashBoxSubId48(int boxId, int slot)
    {
        XxHash64 hasher = new XxHash64();

        WriteInt32BE(ref hasher, boxId);
        WriteInt32BE(ref hasher, slot);

        return hasher.GetCurrentHashAsUInt64() & 0x0000_FFFF_FFFF_FFFFUL;
    }

    private static ulong HashBoxId48(int boxId)
    {
        XxHash64 hasher = new XxHash64();

        Span<byte> buf = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buf, boxId);

        hasher.Append(buf);

        return hasher.GetCurrentHashAsUInt64() & 0x0000_FFFF_FFFF_FFFFUL;
    }

    private static void WriteByte(ref XxHash64 hasher, byte value)
    {
        Span<byte> b = [value];

        hasher.Append(b);
    }

    private static void WriteInt32BE(ref XxHash64 hasher, int value)
    {
        Span<byte> buf = stackalloc byte[4];

        BinaryPrimitives.WriteInt32BigEndian(buf, value);

        hasher.Append(buf);
    }

    private static void WriteString(ref XxHash64 hasher, string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);

        WriteInt32BE(ref hasher, byteCount);

        Span<byte> tmp = byteCount <= 256 ? stackalloc byte[byteCount] : new byte[byteCount];

        Encoding.UTF8.GetBytes(value.AsSpan(), tmp);

        hasher.Append(tmp);
    }
}
