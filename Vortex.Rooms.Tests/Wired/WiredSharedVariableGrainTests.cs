using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Vortex.Database.Context;
using Vortex.Primitives.Rooms.Snapshots.Wired.Variables;
using Vortex.Rooms.Grains;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Wired;

/// <summary>
/// The home a shared wired variable's values finally have.
/// </summary>
/// <remarks>
/// Shared was the one availability a variable box offered with nowhere to put anything: a
/// room-active value lives in the owning room's memory and a persistent one on its own furni row,
/// and a referencing room can see neither. One activation per variable makes this the single writer,
/// which is what lets two rooms agree on a number at all.
/// </remarks>
public sealed class WiredSharedVariableGrainTests
{
    private const long VariableKey = 4242;

    private const string User7 = "u:7";

    [Fact]
    public async Task AValueSurvivesTheActivationThatWroteIt()
    {
        IDbContextFactory<VortexDbContext> factory = Factory();

        WiredSharedVariableGrain first = Grain(factory);
        await first.OnActivateAsync(CancellationToken.None);

        (await first.GiveAsync(User7, 42, replace: false, CancellationToken.None))
            .Should()
            .BeTrue();

        // A second activation is the other room, or this one after an unload: it reads rows, not
        // the memory of the activation that wrote them.
        WiredSharedVariableGrain second = Grain(factory);
        await second.OnActivateAsync(CancellationToken.None);

        ImmutableArray<WiredSharedValueSnapshot> all = await second.GetAllAsync(
            CancellationToken.None
        );

        all.Should().ContainSingle().Which.Value.Should().Be(42);
    }

    [Fact]
    public async Task GiveRefusesAnExistingKeyUnlessAskedToReplace()
    {
        WiredSharedVariableGrain grain = await ActivatedAsync();

        await grain.GiveAsync(User7, 1, replace: false, CancellationToken.None);

        (await grain.GiveAsync(User7, 2, replace: false, CancellationToken.None))
            .Should()
            .BeFalse("the in-memory stores answer the same way, and a box relies on it");

        (await grain.GiveAsync(User7, 2, replace: true, CancellationToken.None)).Should().BeTrue();

        (await grain.GetAllAsync(CancellationToken.None))
            .Should()
            .ContainSingle()
            .Which.Value.Should()
            .Be(2);
    }

    [Fact]
    public async Task SetRefusesAKeyThatWasNeverCreated()
    {
        WiredSharedVariableGrain grain = await ActivatedAsync();

        (await grain.SetAsync(User7, 5, CancellationToken.None))
            .Should()
            .BeFalse("Set updates, Give creates -- the split the change-variable box depends on");

        (await grain.GetAllAsync(CancellationToken.None)).Should().BeEmpty();
    }

    /// <summary>The creation time is the first write's and the update time moves, so the "variable
    /// age" condition reads a shared variable the way it reads a local one.</summary>
    [Fact]
    public async Task TheCreationTimeSurvivesLaterWrites()
    {
        WiredSharedVariableGrain grain = await ActivatedAsync();

        await grain.GiveAsync(User7, 1, replace: false, CancellationToken.None);

        WiredSharedValueSnapshot first = (await grain.GetAllAsync(CancellationToken.None)).Single();

        await grain.SetAsync(User7, 2, CancellationToken.None);

        WiredSharedValueSnapshot second = (
            await grain.GetAllAsync(CancellationToken.None)
        ).Single();

        second.CreatedAtMs.Should().Be(first.CreatedAtMs);
        second.UpdatedAtMs.Should().BeGreaterThanOrEqualTo(first.UpdatedAtMs);
    }

    [Fact]
    public async Task RemoveTakesTheValueAwayForEveryone()
    {
        IDbContextFactory<VortexDbContext> factory = Factory();

        WiredSharedVariableGrain grain = Grain(factory);
        await grain.OnActivateAsync(CancellationToken.None);
        await grain.GiveAsync(User7, 1, replace: false, CancellationToken.None);

        (await grain.RemoveAsync(User7, CancellationToken.None)).Should().BeTrue();
        (await grain.RemoveAsync(User7, CancellationToken.None))
            .Should()
            .BeFalse("it is already gone");

        WiredSharedVariableGrain reloaded = Grain(factory);
        await reloaded.OnActivateAsync(CancellationToken.None);

        (await reloaded.GetAllAsync(CancellationToken.None)).Should().BeEmpty();
    }

    private static async Task<WiredSharedVariableGrain> ActivatedAsync()
    {
        WiredSharedVariableGrain grain = Grain(Factory());

        await grain.OnActivateAsync(CancellationToken.None);

        return grain;
    }

    private static WiredSharedVariableGrain Grain(IDbContextFactory<VortexDbContext> factory) =>
        GrainActivationContext.CreateWithIntegerKey<WiredSharedVariableGrain>(
            VariableKey,
            factory,
            NullLogger<WiredSharedVariableGrain>.Instance
        );

    private static IDbContextFactory<VortexDbContext> Factory()
    {
        DbContextOptions<VortexDbContext> options = new DbContextOptionsBuilder<VortexDbContext>()
            .UseInMemoryDatabase($"wired-shared-{Guid.NewGuid():N}")
            .Options;

        return new Factories(options);
    }

    private sealed class Factories(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }
}
