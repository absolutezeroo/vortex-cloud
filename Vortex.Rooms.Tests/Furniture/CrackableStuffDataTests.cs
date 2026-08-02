using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Furniture.Snapshots.StuffData;
using Vortex.Primitives.Furniture.StuffData;
using Xunit;

namespace Vortex.Rooms.Tests.Furniture;

/// <summary>
/// Format 7 was recognised by the enum but fell through to the legacy fallback, so a crackable's
/// counters had nowhere to live: the client was sent a bare string where it reads a string, a hit
/// count and a target, and the infostand's "hits remaining" could never be right. These lock the
/// mapping and the counters surviving a save/load, which is where the legacy fallback used to hide.
/// </summary>
public sealed class CrackableStuffDataTests
{
    private static readonly StuffDataFactory Factory = new();

    [Fact]
    public void TheFactory_BuildsCrackableDataForFormatSeven()
    {
        Factory
            .CreateStuffData(StuffDataType.CrackableKey)
            .Should()
            .BeAssignableTo<ICrackableStuffData>();
    }

    [Fact]
    public void Hits_AccumulateAndReachTheTarget()
    {
        ICrackableStuffData data = (ICrackableStuffData)
            Factory.CreateStuffData(StuffDataType.CrackableKey);

        data.SetTarget(3);

        data.AddHit().Should().Be(1);
        data.AddHit().Should().Be(2);
        data.AddHit().Should().Be(3);
        data.Target.Should().Be(3);
    }

    [Fact]
    public void ATargetBelowOne_IsFlooredSoTheFurnitureCannotPayOutUntouched()
    {
        ICrackableStuffData data = (ICrackableStuffData)
            Factory.CreateStuffData(StuffDataType.CrackableKey);

        data.SetTarget(-5);

        // The binding floors this too; a zero here would mean "already cracked" on the first render.
        data.Target.Should().Be(0);
        data.Hits.Should().Be(0);
    }

    [Fact]
    public void TheSnapshot_CarriesTheStateHitsAndTargetTheClientReads()
    {
        ICrackableStuffData data = (ICrackableStuffData)
            Factory.CreateStuffData(StuffDataType.CrackableKey);

        data.SetTarget(4);
        data.AddHit();
        data.SetState("2");

        CrackableStuffSnapshot snapshot = data.GetSnapshot()
            .Should()
            .BeOfType<CrackableStuffSnapshot>()
            .Subject;

        snapshot.Data.Should().Be("2");
        snapshot.Hits.Should().Be(1);
        snapshot.Target.Should().Be(4);
        (snapshot.StuffBitmask & 0xFF).Should().Be((int)StuffDataType.CrackableKey);
    }

    [Fact]
    public void TheCounters_SurviveAWriteThenRead()
    {
        ExtraData extraData = new(null);

        extraData.UpdateSection(
            ExtraDataSectionType.STUFF,
            new
            {
                Data = "3",
                Hits = 3,
                Target = 6,
            }
        );

        IStuffData read = Factory.CreateStuffDataFromJson(
            StuffDataType.CrackableKey,
            extraData.GetJsonString()
        );

        ICrackableStuffData crackable = read.Should().BeAssignableTo<ICrackableStuffData>().Subject;

        crackable.Hits.Should().Be(3);
        crackable.Target.Should().Be(6);
        read.GetLegacyString().Should().Be("3");
    }

    [Fact]
    public void FromStuffData_ProducesACrackableSnapshotCarryingBothCounters()
    {
        // The read path was covered, the write path was not: FromStuffData had no
        // ICrackableStuffData branch and fell through to its "unknown stuff data type" throw. The
        // seed left every crackable definition on another format, so nothing ever reached this
        // switch with crackable data and the gap stayed invisible -- repairing the seed alone would
        // have turned a harmless warning into an exception on the room's item-serialization path.
        IStuffData data = Factory.CreateStuffData(StuffDataType.CrackableKey);
        ICrackableStuffData crackable = data.Should().BeAssignableTo<ICrackableStuffData>().Subject;

        crackable.SetTarget(6);
        crackable.AddHit();
        crackable.AddHit();

        CrackableStuffSnapshot snapshot = Factory
            .FromStuffData(data)
            .Should()
            .BeOfType<CrackableStuffSnapshot>()
            .Subject;

        snapshot.Hits.Should().Be(2);
        snapshot.Target.Should().Be(6);
        snapshot.Data.Should().Be(data.GetLegacyString());
    }
}
