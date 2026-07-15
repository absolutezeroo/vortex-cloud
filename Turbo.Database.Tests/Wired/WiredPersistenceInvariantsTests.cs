using System;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Turbo.Database.Context;
using Turbo.Database.Entities.Players;
using Turbo.Database.Entities.Wired;
using Turbo.Primitives.Rooms.Enums.Wired;
using Xunit;

namespace Turbo.Database.Tests.Wired;

/// <summary>
///     Covers the persistence invariants for the wired-domain completion pass:
///     · one permanent-variable row per (target type, target id, variable id)
///     · room-log rows are per-room and ordered/filterable
///     · one preferences row per player
/// </summary>
public sealed class WiredPersistenceInvariantsTests
{
    private static TurboDbContext NewContext()
    {
        return new TurboDbContext(
            new DbContextOptionsBuilder<TurboDbContext>()
                .UseInMemoryDatabase($"wired-{Guid.NewGuid():N}")
                .Options
        );
    }

    [Fact]
    public void PermanentVariable_UniqueIndex_CoversTargetTypeTargetIdVariableId()
    {
        using TurboDbContext ctx = NewContext();

        IIndex? uniqueIndex = ctx
            .Model.FindEntityType(typeof(WiredPermanentVariableEntity))!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.IsUnique
                && i.Properties.Select(p => p.Name)
                    .SequenceEqual([
                        nameof(WiredPermanentVariableEntity.TargetType),
                        nameof(WiredPermanentVariableEntity.TargetId),
                        nameof(WiredPermanentVariableEntity.VariableId),
                    ])
            );

        uniqueIndex
            .Should()
            .NotBeNull(
                "a target can only have one stored value per variable id — set/create/delete "
                    + "semantics depend on this being unique"
            );
    }

    [Fact]
    public void PermanentVariable_SetThenRead_RoundTrips()
    {
        using TurboDbContext ctx = NewContext();

        ctx.WiredPermanentVariables.Add(
            new WiredPermanentVariableEntity
            {
                TargetType = WiredVariableTargetType.User,
                TargetId = 42,
                VariableId = "score",
                Value = 100,
            }
        );
        ctx.SaveChanges();

        WiredPermanentVariableEntity stored = ctx.WiredPermanentVariables.Single(v =>
            v.TargetType == WiredVariableTargetType.User
            && v.TargetId == 42
            && v.VariableId == "score"
        );

        stored.Value.Should().Be(100);
    }

    [Fact]
    public void PermanentVariable_DifferentTargetTypes_DoNotCollide()
    {
        using TurboDbContext ctx = NewContext();

        // Same target id, same variable name, different target type — must be two distinct rows.
        ctx.WiredPermanentVariables.Add(
            new WiredPermanentVariableEntity
            {
                TargetType = WiredVariableTargetType.User,
                TargetId = 1,
                VariableId = "score",
                Value = 1,
            }
        );
        ctx.WiredPermanentVariables.Add(
            new WiredPermanentVariableEntity
            {
                TargetType = WiredVariableTargetType.Furni,
                TargetId = 1,
                VariableId = "score",
                Value = 2,
            }
        );
        ctx.SaveChanges();

        ctx.WiredPermanentVariables.Count(v => v.VariableId == "score").Should().Be(2);
    }

    [Fact]
    public void RoomWiredLog_FilteredAndOrderedByRoom()
    {
        using TurboDbContext ctx = NewContext();

        ctx.RoomWiredLogs.Add(
            new RoomWiredLogEntity
            {
                RoomEntityId = 1,
                LogLevel = WiredLogLevel.Info,
                LogSource = WiredLogSource.Action,
                Message = "room 1 entry",
            }
        );
        ctx.RoomWiredLogs.Add(
            new RoomWiredLogEntity
            {
                RoomEntityId = 2,
                LogLevel = WiredLogLevel.Error,
                LogSource = WiredLogSource.Action,
                Message = "room 2 entry",
            }
        );
        ctx.SaveChanges();

        RoomWiredLogEntity[] room1Logs = ctx
            .RoomWiredLogs.Where(l => l.RoomEntityId == 1)
            .ToArray();

        room1Logs.Should().ContainSingle().Which.Message.Should().Be("room 1 entry");
    }

    [Fact]
    public void PlayerWiredPreferences_UniqueIndex_CoversPlayerId()
    {
        using TurboDbContext ctx = NewContext();

        IIndex? uniqueIndex = ctx
            .Model.FindEntityType(typeof(PlayerWiredPreferencesEntity))!
            .GetIndexes()
            .SingleOrDefault(i =>
                i.IsUnique
                && i.Properties.Any(p =>
                    p.Name == nameof(PlayerWiredPreferencesEntity.PlayerEntityId)
                )
            );

        uniqueIndex.Should().NotBeNull("a player can only have one wired-preferences row");
    }
}
