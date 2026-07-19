using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vortex.Database.Context;
using Vortex.Database.Entities.Groups;
using Vortex.Database.Entities.Room;
using Vortex.Primitives.Groups.Enums;
using Xunit;

namespace Vortex.Database.Tests.Groups;

/// <summary>
/// Covers the DATA-MODEL §2 group data-model invariants for ROADMAP Story 5.2:
/// every created group gets exactly one default forum-settings row (§2.4), and the circular
/// <c>groups.room_id</c> &lt;-&gt; <c>rooms.group_id</c> link is configured non-cascade (§2.7).
/// </summary>
public sealed class GroupCreationTests
{
    private static VortexDbContext NewContext() =>
        new(
            new DbContextOptionsBuilder<VortexDbContext>()
                .UseInMemoryDatabase($"groups-{Guid.NewGuid():N}")
                .Options
        );

    [Fact]
    public void CreatingGroup_PersistsExactlyOneDefaultForumSettingsRow()
    {
        using VortexDbContext context = NewContext();

        // Act — create a group through the single creation path (the §2.4 invariant lives here).
        GroupEntity group = GroupFactory.Create(
            name: "Pixel Painters",
            badge: "b12345s09112",
            roomEntityId: 42,
            ownerPlayerEntityId: 7,
            type: GroupType.Open,
            colorOne: "#1E5B73",
            colorTwo: "#FFCC00"
        );

        context.Groups.Add(group);
        context.SaveChanges();

        // Assert — exactly one settings row, linked to the group, carrying the defaults.
        List<GroupForumSettingsEntity> settings = context
            .GroupForumSettings.Where(s => s.GroupEntityId == group.Id)
            .ToList();

        settings.Should().HaveCount(1, "the §2.4 invariant is one forum-settings row per group");

        GroupForumSettingsEntity row = settings.Single();
        row.GroupEntityId.Should().Be(group.Id);
        row.Enabled.Should().Be(GroupFactory.DefaultForumEnabled);
        row.ReadPermission.Should().Be(GroupFactory.DefaultReadPermission);
        row.PostPermission.Should().Be(GroupFactory.DefaultPostPermission);
        row.ThreadPermission.Should().Be(GroupFactory.DefaultThreadPermission);
        row.ModPermission.Should().Be(GroupFactory.DefaultModPermission);
    }

    [Fact]
    public void CircularRoomGroupLink_IsConfiguredNonCascade()
    {
        using VortexDbContext context = NewContext();

        IForeignKey groupToRoom = context
            .Model.FindEntityType(typeof(GroupEntity))!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(GroupEntity.RoomEntityId)));

        IForeignKey roomToGroup = context
            .Model.FindEntityType(typeof(RoomEntity))!
            .GetForeignKeys()
            .Single(fk => fk.Properties.Any(p => p.Name == nameof(RoomEntity.GroupEntityId)));

        groupToRoom
            .DeleteBehavior.Should()
            .NotBe(DeleteBehavior.Cascade, "groups.room_id must not cascade (§2.7)");
        roomToGroup
            .DeleteBehavior.Should()
            .NotBe(DeleteBehavior.Cascade, "rooms.group_id must not cascade (§2.7)");

        // Specifically the Restrict behavior we configured in VortexDbContext.OnModelCreating.
        groupToRoom.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
        roomToGroup.DeleteBehavior.Should().Be(DeleteBehavior.Restrict);
    }
}
