using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Orleans;
using Vortex.Furniture.Providers;
using Vortex.Primitives;
using Vortex.Primitives.Action;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums.Wired;
using Vortex.Primitives.Rooms.Object;
using Vortex.Primitives.Rooms.Object.Furniture.Floor;
using Vortex.Primitives.Rooms.Snapshots.Wired;
using Vortex.Rooms.Object.Logic.Furniture.Floor.Wired.Triggers;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// Where a wired save stops being undoable.
/// </summary>
/// <remarks>
/// The whole of <c>ApplyWiredUpdateAsync</c> sat in one try/catch that answered <c>false</c>. But
/// the configuration is written, marked dirty and the cached snapshot dropped <em>before</em> the
/// room is told the stack changed — so a notification that threw reported a refusal for a save that
/// had happened. The handler reads that as "refused" and withholds <c>WiredSaveSuccess</c>: the
/// player was told their configuration did not take, reopened the box, and found it had.
/// </remarks>
public sealed class WiredSaveCommitPointTests
{
    private const int ObjectId = 91;

    [Fact]
    public async Task AFailingStackNotification_StillReportsTheSave()
    {
        BoxWhoseNotificationFails box = new(WiredTestBoxes.Context(ObjectId));

        bool saved = await box.ApplyWiredUpdateAsync(
                new ActionContext(ActionOrigin.Player, default, new PlayerId(1), new RoomId(55)),
                Update(),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        saved.Should().BeTrue("the configuration was written before the notification ran");
    }

    /// <summary>
    /// And the configuration really is in the box, rather than the method having returned true on
    /// its way past a mutation that never happened.
    /// </summary>
    [Fact]
    public async Task TheConfigurationSurvivesTheFailedNotification()
    {
        BoxWhoseNotificationFails box = new(WiredTestBoxes.Context(ObjectId));

        await box.ApplyWiredUpdateAsync(
                new ActionContext(ActionOrigin.Player, default, new PlayerId(1), new RoomId(55)),
                Update(stringParam: "written anyway"),
                CancellationToken.None
            )
            .ConfigureAwait(true);

        box.GetSnapshot().StringParam.Should().Be("written anyway");
    }

    private static WiredUpdateRequest Update(string stringParam = "") =>
        new()
        {
            Id = ObjectId,
            IntParams = [0, 0],
            StringParam = stringParam,
            StuffIds = [],
            StuffIds2 = [],
            DefinitionSpecifics = [],
            FurniSources = [],
            PlayerSources = [],
            VariableIds = [],
            TypeSpecifics = [],
        };

    /// <summary>
    /// A real wired box whose one deviation is that telling the room throws — the failure mode the
    /// catch used to swallow into a refusal.
    /// </summary>
    private sealed class BoxWhoseNotificationFails(IRoomFloorItemContext ctx)
        : WiredTriggerClickUser(
            FakeProxy.Create<IGrainFactory>(_ => null),
            new StuffDataFactory(),
            ctx
        )
    {
        protected override Task OnWiredStackChangedAsync(
            ActionContext ctx,
            List<int> ids,
            CancellationToken ct
        ) => Task.FromException(new InvalidOperationException("the room could not be told"));
    }
}
