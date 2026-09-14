using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Furniture.Enums;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Rules;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// A box whose parameter count varies declares a few fixed rules and one tail rule standing for
/// all the rest. Normalisation and repair both honoured that tail; attaching did not, and handed
/// <see cref="WiredData"/> the fixed rules alone.
/// <para>
/// The live symptom, on every room load: the level-up add-on sends five ints for an exponential
/// curve, <c>GetIntParam</c> looks the RULE up before the value, and index 2 of a two-entry list
/// threw — so the whole add-on was caught, logged as "malformed params" and derived nothing. Its
/// own <c>Param(index, fallback)</c> guard could not help: it checks the params, and it was the
/// rules that were short.
/// </para>
/// </summary>
public sealed class WiredTailParamRuleTests
{
    private const int BoxId = 421;

    [Fact]
    public async Task ParamsPastTheFixedRules_AreReadable_WhenTheBoxDeclaresATail()
    {
        TailBox box = Hydrate([7, 2, 100, 20, 50], tail: true);

        await box.LoadWiredAsync(CancellationToken.None);

        box.Read(0).Should().Be(7);
        box.Read(1).Should().Be(2);

        // The three the tail covers. These are what threw.
        box.Read(2).Should().Be(100);
        box.Read(3).Should().Be(20);
        box.Read(4).Should().Be(50);
    }

    [Fact]
    public async Task AShortList_IsPaddedAndItsRulesGrowWithIt()
    {
        // Two params persisted, five rules' worth of room: the repair pads the list, so the rules
        // have to be re-attached at the new length or the padding is unreadable.
        TailBox box = Hydrate([7, 2], tail: true);

        await box.LoadWiredAsync(CancellationToken.None);

        box.Params.Count.Should().BeGreaterThanOrEqualTo(2);

        for (int i = 0; i < box.Params.Count; i++)
        {
            box.Invoking(b => b.Read(i)).Should().NotThrow();
        }
    }

    [Fact]
    public async Task WithoutATail_TheFixedRulesStillBoundWhatIsReadable()
    {
        // The other half of the contract: a box with no tail rule must not silently accept extra
        // params, and the repair trims the list back to its two rules.
        TailBox box = Hydrate([7, 2, 100], tail: false);

        await box.LoadWiredAsync(CancellationToken.None);

        box.Params.Should().HaveCount(2);
    }

    private static TailBox Hydrate(List<int> intParams, bool tail)
    {
        WiredData data = new() { IntParams = intParams };
        ExtraData extra = new(
            JsonSerializer.Serialize(
                new Dictionary<string, WiredData> { [ExtraDataSectionType.WIRED] = data }
            )
        );

        return new TailBox(WiredTestBoxes.Context(objectId: BoxId, extraData: extra), tail);
    }

    /// <summary>The level-up add-on's own rule shape: a mask, a mode, then however many the curve
    /// takes.</summary>
    private sealed class TailBox(IRoomFloorItemContext ctx, bool tail)
        : FurnitureWiredLogic(null!, new StuffDataFactory(), ctx)
    {
        public override WiredType WiredType => WiredType.Addon;

        public override int WiredCode => 1001;

        public List<int> Params => _wiredData.IntParams;

        public int Read(int index) => _wiredData.GetIntParam<int>(index);

        public override List<IWiredParamRule> GetIntParamRules() =>
            [new WiredRangeParamRule(0, 255, 0), new WiredRangeParamRule(0, 2, 1)];

        public override IWiredParamRule? GetIntParamTailRule() =>
            tail ? new WiredRangeParamRule(0, int.MaxValue, 0) : null;
    }
}
