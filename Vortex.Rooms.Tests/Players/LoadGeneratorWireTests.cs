using System.Text.Json;
using FluentAssertions;
using Vortex.Benchmark;
using Vortex.LoadGen;
using Vortex.Primitives.Benchmark;
using Xunit;

namespace Vortex.Rooms.Tests.Players;

/// <summary>
/// The one line of JSON the load generator writes each second, and the emulator's reading of it.
/// </summary>
/// <remarks>
/// <para>
/// Two processes, no compiler between them. A field renamed on either side leaves a run that
/// produces no samples at all — no exception, no log, just an empty graph and a report full of
/// zeroes — because a line that will not parse is dropped on purpose, so one stray write cannot end
/// a run.
/// </para>
/// <para>
/// So the test serializes exactly as the generator does and hands the result to the reader on the
/// other side, which is the only place the two halves ever meet.
/// </para>
/// </remarks>
public sealed class LoadGeneratorWireTests
{
    [Fact]
    public void ASampleWrittenByTheGenerator_IsReadBackWholeByTheEmulator()
    {
        string line = JsonSerializer.Serialize(
            new LoadSample
            {
                Connected = 97,
                RttMedianMs = 1.25,
                RttP95Ms = 43.5,
                Packets = 12345,
                Bytes = 987654,
                Failures = 3,
            },
            Program.Wire
        );

        BenchmarkSample? parsed = LoadGeneratorHost.Parse(line);

        parsed.Should().NotBeNull();
        parsed!.ConnectedClients.Should().Be(97);
        parsed.RttMedianMs.Should().Be(1.25);
        parsed.RttP95Ms.Should().Be(43.5);
        parsed.PacketsReceived.Should().Be(12345);
        parsed.BytesReceived.Should().Be(987654);
        parsed.Failures.Should().Be(3);
    }

    /// <summary>
    /// Anything that is not a sample is skipped rather than fatal: the generator may write a line of
    /// its own one day, and losing a run over it would be the wrong trade.
    /// </summary>
    [Fact]
    public void ALineThatIsNotASample_IsIgnored()
    {
        LoadGeneratorHost.Parse("not json at all").Should().BeNull();
    }

    /// <summary>
    /// Both halves of the generator have to be deployed together — the launcher and the assembly it
    /// is a shim for.
    /// <para>
    /// This checks the directory the <em>tests</em> run in, not the emulator's, so it is not proof
    /// that a run will find it: an earlier version of this test passed green while the emulator's
    /// own output was missing the assembly. What it does catch is the half-copy itself, which is the
    /// mistake that actually happened — a reference that brings the launcher and leaves the code
    /// behind.
    /// </para>
    /// </summary>
    [Fact]
    public void TheGeneratorExecutable_IsShippedBesideTheHost()
    {
        LoadGeneratorHost.IsAvailable.Should().BeTrue(LoadGeneratorHost.ExecutablePath);
    }
}
