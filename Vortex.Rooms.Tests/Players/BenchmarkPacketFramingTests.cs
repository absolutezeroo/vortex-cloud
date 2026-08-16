using System;
using System.Buffers.Binary;
using System.Text;
using FluentAssertions;
using Vortex.Benchmark;
using Xunit;

namespace Vortex.Rooms.Tests.Players;

/// <summary>
/// The load test's synthetic client writes its own frames, and this is what they have to look like.
/// </summary>
/// <remarks>
/// <para>
/// Nothing else would catch a mistake here. The client deliberately understands no composer, so a
/// wrongly framed packet does not fail loudly — the server's decoder reads a nonsense length,
/// declares the session invalid and drops it, and the run reports a hotel that "could not take four
/// hundred players" when in truth it was never asked properly.
/// </para>
/// <para>
/// The expectations below are read off <c>ClientPacketDecoder</c>, which takes the first four bytes
/// as a big-endian length covering the two-byte header, and <c>ClientPacket.PopString</c>, which
/// reads a big-endian <c>ushort</c> and that many UTF-8 bytes.
/// </para>
/// </remarks>
public sealed class BenchmarkPacketFramingTests
{
    [Fact]
    public void AnEmptyPacket_IsSixBytes_AndItsLengthCountsTheHeader()
    {
        byte[] frame = new BenchmarkPacketWriter(544).ToArray();

        frame.Length.Should().Be(6);
        BinaryPrimitives.ReadInt32BigEndian(frame).Should().Be(2, "the length covers the header");
        BinaryPrimitives.ReadInt16BigEndian(frame.AsSpan(4)).Should().Be(544);
    }

    [Fact]
    public void AnIntIsFourBytes_BigEndian()
    {
        byte[] frame = new BenchmarkPacketWriter(2364).Int(7).Int(-1).ToArray();

        BinaryPrimitives.ReadInt32BigEndian(frame).Should().Be(10);
        BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(6)).Should().Be(7);
        BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(10)).Should().Be(-1);
    }

    [Fact]
    public void AStringIsALengthPrefixedRunOfUtf8()
    {
        byte[] frame = new BenchmarkPacketWriter(882).String("ticket").ToArray();

        BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(6)).Should().Be(6);
        Encoding.UTF8.GetString(frame.AsSpan(8, 6)).Should().Be("ticket");
        BinaryPrimitives.ReadInt32BigEndian(frame).Should().Be(10, "2 header + 2 prefix + 6 text");
    }

    /// <summary>
    /// The prefix counts bytes, not characters. A hotel with accented names would otherwise send a
    /// length short of what follows, and every packet after it on that socket would be read from the
    /// wrong offset — the session desynchronises for good rather than failing once.
    /// </summary>
    [Fact]
    public void AMultiByteStringIsMeasuredInBytes()
    {
        byte[] frame = new BenchmarkPacketWriter(3034).String("héllo").ToArray();

        BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(6)).Should().Be(6);
        BinaryPrimitives.ReadInt32BigEndian(frame).Should().Be(10);
        frame.Length.Should().Be(14);
    }

    /// <summary>
    /// The exact shape of the login packet, since it is the one that has to be right before anything
    /// else can be measured: <c>SSOTicketMessageParser</c> reads a string then an int.
    /// </summary>
    [Fact]
    public void TheLoginPacket_MatchesWhatTheParserReads()
    {
        byte[] frame = new BenchmarkPacketWriter(882).String("abc").Int(0).ToArray();

        BinaryPrimitives.ReadInt32BigEndian(frame).Should().Be(11);
        BinaryPrimitives.ReadInt16BigEndian(frame.AsSpan(4)).Should().Be(882);
        BinaryPrimitives.ReadUInt16BigEndian(frame.AsSpan(6)).Should().Be(3);
        Encoding.UTF8.GetString(frame.AsSpan(8, 3)).Should().Be("abc");
        BinaryPrimitives.ReadInt32BigEndian(frame.AsSpan(11)).Should().Be(0);
    }
}
