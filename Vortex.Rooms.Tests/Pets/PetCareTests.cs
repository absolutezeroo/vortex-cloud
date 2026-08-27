using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Streams;
using Vortex.Database.Context;
using Vortex.Primitives.Action;
using Vortex.Primitives.Events;
using Vortex.Primitives.Furniture.Providers;
using Vortex.Primitives.Observability;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Pets.Providers;
using Vortex.Primitives.Pets.Snapshots;
using Vortex.Primitives.Players;
using Vortex.Primitives.Rooms;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Primitives.Rooms.Grains;
using Vortex.Primitives.Rooms.Providers;
using Vortex.Primitives.Rooms.Snapshots;
using Vortex.Rooms.Configuration;
using Vortex.Rooms.Grains;
using Vortex.Rooms.Grains.Systems;
using Vortex.Rooms.Tests.Support;
using Vortex.Rooms.Wired.Logs;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Pets;

/// <summary>
/// Caring for a pet — respecting it, feeding it a supplement, giving it a command — all funnel into
/// the same experience grant, and all three are reachable by any player standing in the room. The
/// rules that keep that from being farmable are the daily respect cap and the level caps; both live
/// in config and neither is enforced by the client.
/// </summary>
public sealed class PetCareTests
{
    private const int RoomId = 10;
    private const int OwnerId = 1;
    private const int PetId = 500;
    private const int EnergyCap = 100;

    [Fact]
    public async Task RespectingAPet_RaisesItsRespectAndItsExperience()
    {
        Harness harness = new(Pet());

        PetSnapshot? updated = await harness
            .System.RespectPetAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Respect.Should().Be(1);
        updated.RespectTodayCount.Should().Be(1);
        updated.Experience.Should().Be(new PetConfig().RespectXpReward);
    }

    [Fact]
    public async Task TheDailyRespectCap_StopsTheFarm()
    {
        PetConfig config = new();
        Harness harness = new(
            Pet(respect: 9, respectToday: config.RespectDailyCapPerPet, resetOn: Today)
        );

        PetSnapshot? updated = await harness
            .System.RespectPetAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Respect.Should().Be(9, "the cap was already reached today");
        updated.RespectTodayCount.Should().Be(config.RespectDailyCapPerPet);
        updated.Experience.Should().Be(0, "a refused respect must not pay experience either");
    }

    [Fact]
    public async Task ANewDay_ClearsTheRespectCount()
    {
        PetConfig config = new();
        Harness harness = new(
            Pet(respect: 9, respectToday: config.RespectDailyCapPerPet, resetOn: Today.AddDays(-1))
        );

        PetSnapshot? updated = await harness
            .System.RespectPetAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Respect.Should().Be(10);
        updated.RespectTodayCount.Should().Be(1, "yesterday's tally does not carry over");
        updated.RespectLastResetDate.Should().Be(Today);
    }

    [Fact]
    public async Task RespectingAPetThatIsNotInTheRoom_DoesNothing()
    {
        Harness harness = new(Pet());

        PetSnapshot? updated = await harness
            .System.RespectPetAsync(harness.Context, PetId + 1, CancellationToken.None)
            .ConfigureAwait(true);

        updated.Should().BeNull();
    }

    [Fact]
    public async Task ASupplement_RestoresEnergyAndPaysExperience()
    {
        PetConfig config = new();
        Harness harness = new(Pet(energy: 10));

        PetSnapshot? updated = await harness
            .System.GiveSupplementToPetAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Energy.Should().Be(10 + config.SupplementEnergyBoost);
        updated.Experience.Should().Be(config.SupplementXpReward);
    }

    [Fact]
    public async Task ASupplement_CannotPushEnergyPastTheLevelCap()
    {
        Harness harness = new(Pet(energy: EnergyCap - 1));

        PetSnapshot? updated = await harness
            .System.GiveSupplementToPetAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Energy.Should().Be(EnergyCap);
    }

    [Fact]
    public async Task ACommand_PaysItsOwnExperience()
    {
        Harness harness = new(Pet());

        PetSnapshot? updated = await harness
            .System.GrantPetCommandXpAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Experience.Should().Be(new PetConfig().CommandXpReward);
    }

    [Fact]
    public async Task EnoughExperience_LevelsThePetUp()
    {
        // The stub levels a pet every ten points; a respect is worth five, so the second one crosses.
        Harness harness = new(Pet(experience: 6));

        PetSnapshot? updated = await harness
            .System.RespectPetAsync(harness.Context, PetId, CancellationToken.None)
            .ConfigureAwait(true);

        updated!.Experience.Should().Be(11);
        updated.Level.Should().Be(2);
    }

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private static PetSnapshot Pet(
        int energy = 100,
        int experience = 0,
        int respect = 0,
        int respectToday = 0,
        DateOnly? resetOn = null
    ) =>
        new()
        {
            PetId = PetId,
            OwnerId = new PlayerId(OwnerId),
            RoomId = RoomId,
            Name = "Biscuit",
            Type = 0,
            Race = 1,
            Color = "FFFFFF",
            Gender = AvatarGenderType.Male,
            Level = 1,
            Experience = experience,
            Energy = energy,
            Nutrition = 100,
            Respect = respect,
            RespectTodayCount = respectToday,
            RespectLastResetDate = resetOn,
            X = 1,
            Y = 1,
            Z = 0,
            Direction = Rotation.South,
        };

    /// <summary>
    /// A room grain built outside a silo with the pet already resident, so the care paths run
    /// without a database or a room tick. Pets are pushed straight into the live state and marked
    /// loaded — the loading path itself is covered by the feeding tests, which do use a database.
    /// </summary>
    private sealed class Harness
    {
        public Harness(PetSnapshot pet)
        {
            RoomGrain grain = GrainActivationContext.CreateWithIntegerKey<RoomGrain>(
                RoomId,
                FakeProxy.Create<IDbContextFactory<VortexDbContext>>(_ => null),
                FakeProxy.Create<IFurnitureDefinitionProvider>(_ => null),
                FakeProxy.Create<IStuffDataFactory>(_ => null),
                Options.Create(new RoomConfig()),
                NullLogger<IRoomGrain>.Instance,
                FakeProxy.Create<IRoomModelProvider>(_ => null),
                FakeProxy.Create<IRoomItemsProvider>(_ => null),
                FakeProxy.Create<IRoomObjectLogicProvider>(_ => null),
                FakeProxy.Create<IRoomAvatarProvider>(_ => null),
                FakeProxy.Create<IRoomWiredVariablesProvider>(_ => null),
                RoomGrainStubs.NoListeners(),
                FakeProxy.Create<IGrainFactory>(_ => null),
                FakeProxy.Create<IEventPublisher>(_ => null),
                RoomGrainStubs.NeverCancels(),
                FakeProxy.Create<IPermissionService>(_ => null),
                FakeProxy.Create<IVortexMetrics>(_ => null),
                FakeProxy.Create<IRoomModerationStore>(_ => null),
                BuildPetLevelProvider(),
                FakeProxy.Create<IPetCommandProvider>(_ => null),
                FakeProxy.Create<IPetVocalProvider>(_ => null),
                new RoomWiredLogChannel()
            );

            // Every care path broadcasts; the stream is armed during activation, which never runs
            // here, so it is stubbed rather than left null.
            grain._roomOutbound = FakeProxy.Create<IAsyncStream<RoomOutbound>>(_ => null);

            grain._state.IsPetsLoaded = true;
            grain._state.PetsById[pet.PetId] = pet;

            // The avatar push labels the pet with its owner's name, which the room caches after one
            // lookup through the player directory. Seeding the cache keeps that lookup out of a test
            // about respect and experience.
            grain._state.OwnerNamesById[pet.OwnerId] = "Owner";

            System = grain.PetSystem;
        }

        public RoomPetSystem System { get; }

        public ActionContext Context { get; } =
            ActionContext.CreateForPlayer(new PlayerId(OwnerId), new RoomId(RoomId));

        private static IPetLevelProvider BuildPetLevelProvider() =>
            FakeProxy.Create<IPetLevelProvider>(call =>
                call.Method.Name switch
                {
                    nameof(IPetLevelProvider.GetEnergyCapForLevel) => EnergyCap,
                    nameof(IPetLevelProvider.GetNutritionCapForLevel) => 100,
                    nameof(IPetLevelProvider.GetMaxLevel) => 20,
                    // A level every ten points, so a test can name an experience total and know the
                    // level it implies without importing the real progression table.
                    nameof(IPetLevelProvider.GetLevelForExperience) => 1
                        + ((int)call.Args![1]! / 10),
                    _ => null,
                }
            );
    }
}
