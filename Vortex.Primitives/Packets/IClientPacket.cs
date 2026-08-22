using System.Text;

namespace Vortex.Primitives.Packets;

public interface IClientPacket : IVortexPacket
{
    public int Remaining { get; }
    public bool End { get; }
    public int Position { get; }

    /// <summary>The payload as hex with the read position marked, for when a parser and the client
    /// stop agreeing. Bounded: it ends up in a log.</summary>
    public string ToHexDump(int maxBytes = 128);

    public byte PopByte();
    public byte[] PopBytes(int count);
    public bool PopBoolean();
    public short PopShort();
    public ushort PopUShort();
    public int PopInt();
    public long PopLong();
    public string PopString(Encoding? encoding = null);
}
