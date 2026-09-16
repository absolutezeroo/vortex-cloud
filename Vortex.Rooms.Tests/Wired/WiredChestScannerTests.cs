using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Vortex.Furniture.Providers;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Wired;
using Vortex.Primitives.Rooms.Wired.Variable;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Addons;
using Vortex.Rooms.Wired;
using Vortex.Rooms.Wired.Variables;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The chest scanner add-on: it counts what the chests hold and writes the number into a variable,
/// in the policy phase, so the stack's own conditions can then branch on it.
/// </summary>
/// <remarks>
/// The slots are the thing to pin. The add-on shares the item-type condition's layout — slot 0 the
/// item types, slot 1 the chests — and reading them the other way round counts the chest as a type
/// example, which answers zero and looks like an empty chest.
/// </remarks>
public sealed class WiredChestScannerTests
{
    private const int Chest = 10;

    private const int TypeExample = 20;

    private const int VariableBox = 30;

    private const int ScanAll = 0;

    private const int ScanPreviewedOnly = 1;

    [Fact]
    public async Task ItCountsTheChestsInSlotOne_ForTheTypesInSlotZero()
    {
        FakeChestAccess chests = new(held: 12);
        RecordingVariable variable = new();

        await Scanner(chests, variable, ScanAll)
            .MutatePolicyAsync(Processing(), CancellationToken.None);

        chests.Counted.Should().HaveCount(1);
        chests.Counted[0].ChestIds.Should().Equal(Chest);
        chests.Counted[0].KindExampleIds.Should().Equal(TypeExample);
    }

    [Fact]
    public async Task TheCountLandsOnTheChosenVariable_AsARoomWideValue()
    {
        FakeChestAccess chests = new(held: 12);
        RecordingVariable variable = new();

        await Scanner(chests, variable, ScanAll)
            .MutatePolicyAsync(Processing(), CancellationToken.None);

        variable.Written.Should().HaveCount(1);
        variable.Written[0].Value.Value.Should().Be(12);
        // A context variable is single-valued: it keys on target 0 and answers even when the stack
        // selected nobody.
        variable.Written[0].Key.TargetType.Should().Be(WiredVariableTargetType.Context);
        variable.Written[0].Key.TargetId.Should().Be(0);
        variable.Written[0].Replace.Should().BeTrue();
    }

    /// <summary>
    /// The second scanning mode asks for the previewed items only, which this hotel cannot answer —
    /// a preview is a display choice, partly random, recomputed on every look. Writing the full
    /// count instead would answer a different question, so nothing is written at all.
    /// </summary>
    [Fact]
    public async Task ThePreviewedItemsMode_WritesNothing()
    {
        FakeChestAccess chests = new(held: 12);
        RecordingVariable variable = new();

        await Scanner(chests, variable, ScanPreviewedOnly)
            .MutatePolicyAsync(Processing(), CancellationToken.None);

        chests.Counted.Should().BeEmpty();
        variable.Written.Should().BeEmpty();
    }

    [Fact]
    public async Task WithNoVariableChosen_ItScansNothingAndStillLetsTheStackRun()
    {
        FakeChestAccess chests = new(held: 12);
        RecordingVariable variable = new();

        bool carryOn = await Scanner(chests, variable, ScanAll, variableChosen: false)
            .MutatePolicyAsync(Processing(), CancellationToken.None);

        carryOn.Should().BeTrue();
        chests.Counted.Should().BeEmpty();
        variable.Written.Should().BeEmpty();
    }

    /// <summary>
    /// Two furni slots, because the form titles two — slot 0 "item types", anything else "chests".
    /// The picker indexes the list the server declares, so a slot short takes the dialog down.
    /// </summary>
    [Fact]
    public void ItDeclaresBothSlotsTheFormTitles()
    {
        TestScanner scanner = Scanner(new FakeChestAccess(), new RecordingVariable(), ScanAll);

        scanner.GetAllowedFurniSources().Should().HaveCount(2);
        scanner.GetAllowedFurniSources().Should().OnlyContain(sources => sources.Length > 0);
        scanner.GetMaxVariableIds().Should().Be(1);
        scanner.WiredCode.Should().Be(18);
    }

    // ---- harness -------------------------------------------------------------------------------

    private static TestScanner Scanner(
        FakeChestAccess chests,
        RecordingVariable variable,
        int mode,
        bool variableChosen = true
    )
    {
        WiredVariableId variableId = WiredVariableIdBuilder.CreateFromBoxId(VariableBox);
        FakeFurniAccess furni = new();

        furni.Variables[variableId] = variable.AsVariable();

        FakeRoomLookup lookup = new();

        foreach (int id in new[] { Chest, TypeExample })
        {
            RoomObjectId objectId = new(id);

            lookup.ItemsById[objectId] = FakeProxy.Create<IRoomItem>(call =>
                call.Method.Name == "get_ObjectId" ? objectId : null
            );
        }

        return new TestScanner(
            WiredTestBoxes.Context(chests: chests, lookup: lookup, furniAccess: furni),
            new WiredData
            {
                IntParams = [mode],
                StuffIds = [TypeExample],
                StuffIds2 = [Chest],
                VariableIds = variableChosen ? [variableId.ToString()] : [],
            }
        );
    }

    private static IWiredProcessingContext Processing() =>
        FakeProxy.Create<IWiredProcessingContext>(_ => null);

    /// <summary>A variable that accepts every write and remembers it.</summary>
    private sealed class RecordingVariable
    {
        public List<(
            WiredVariableKey Key,
            WiredVariableValue Value,
            bool Replace
        )> Written { get; } = [];

        public IWiredVariable AsVariable() =>
            FakeProxy.Create<IWiredVariable>(call =>
            {
                if (call.Method.Name != "GiveValueAsync" || call.Args is not [_, _, _])
                {
                    return null;
                }

                Written.Add(
                    (
                        (WiredVariableKey)call.Args[0]!,
                        (WiredVariableValue)call.Args[1]!,
                        (bool)call.Args[2]!
                    )
                );

                return Task.FromResult(true);
            });
    }

    private sealed class TestScanner : WiredAddonChestItemTypeScanner
    {
        public TestScanner(IRoomFloorItemContext ctx, WiredData data)
            : base(null!, new StuffDataFactory(), ctx)
        {
            data.AttatchRules(GetIntParamRules());
            _wiredData = data;
        }
    }
}
