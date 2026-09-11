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

    /// <summary>
    /// The other direction of the same seam, and the more dangerous one.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The plan goes emulator to generator, and every behaviour beyond walking and chatting is
    /// carried in it: which items may be moved, who may be written to, what may be bought. A field
    /// that fails to cross does not throw — it deserializes to zero or an empty array, and the drive
    /// loop reads that as "this behaviour is switched off". The run then measures walking and
    /// chatting at full speed and reports success, which is indistinguishable from a run that was
    /// asked for exactly that.
    /// </para>
    /// <para>
    /// So the assertion is field by field, on values that are all distinct: a plan where two fields
    /// share a value would pass while they were swapped.
    /// </para>
    /// </remarks>
    [Fact]
    public void APlanWrittenByTheEmulator_IsReadBackWholeByTheGenerator()
    {
        string json = JsonSerializer.Serialize(
            new LoadGeneratorPlan
            {
                Host = "10.0.0.5",
                Port = 30001,
                RoomId = 31,
                DurationSeconds = 300,
                RampSeconds = 60,
                WalkIntervalMs = 2000,
                ChatIntervalMs = 8000,
                Tickets = ["t1", "t2"],
                WalkTargets =
                [
                    [3, 4],
                ],
                MoveIntervalMs = 5000,
                UseIntervalMs = 6000,
                BuyIntervalMs = 7000,
                MessageIntervalMs = 9000,
                CreateRoomIntervalMs = 30000,
                FurnitureIds = [11, 12, 13],
                PlayerIds = [21, 22],
                CatalogOffers =
                [
                    [41, 42],
                ],
                RoomModelName = "model_x",
            },
            LoadGeneratorHost.Wire
        );

        LoadPlan? plan = JsonSerializer.Deserialize<LoadPlan>(json, Program.Wire);

        plan.Should().NotBeNull();
        plan!.Host.Should().Be("10.0.0.5");
        plan.Port.Should().Be(30001);
        plan.RoomId.Should().Be(31);
        plan.DurationSeconds.Should().Be(300);
        plan.RampSeconds.Should().Be(60);
        plan.WalkIntervalMs.Should().Be(2000);
        plan.ChatIntervalMs.Should().Be(8000);
        plan.Tickets.Should().Equal("t1", "t2");
        plan.WalkTargets.Should().HaveCount(1);
        plan.WalkTargets[0].Should().Equal(3, 4);

        plan.MoveIntervalMs.Should().Be(5000);
        plan.UseIntervalMs.Should().Be(6000);
        plan.BuyIntervalMs.Should().Be(7000);
        plan.MessageIntervalMs.Should().Be(9000);
        plan.CreateRoomIntervalMs.Should().Be(30000);
        plan.FurnitureIds.Should().Equal(11, 12, 13);
        plan.PlayerIds.Should().Equal(21, 22);
        plan.CatalogOffers.Should().HaveCount(1);
        plan.CatalogOffers[0].Should().Equal(41, 42);
        plan.RoomModelName.Should().Be("model_x");
    }
}
