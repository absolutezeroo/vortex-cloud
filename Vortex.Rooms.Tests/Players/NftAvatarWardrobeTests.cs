using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Orleans;
using Vortex.Database.Context;
using Vortex.Database.Entities.Players;
using Vortex.Players.Grains;
using Vortex.Primitives.Orleans.Snapshots.Players;
using Vortex.Primitives.Players;
using Vortex.Primitives.Players.Enums;
using Vortex.Primitives.Players.Grains;
using Vortex.Primitives.Rooms.Enums;
using Vortex.Tests.Support;
using Xunit;

namespace Vortex.Rooms.Tests.Players;

/// <summary>
/// The avatars a player can wear whole, and the two rules the client will not forgive.
/// <para>
/// The first is the token: the editor finds the tile to light up by matching the worn outfit's token
/// against the wardrobe's, and nothing else. The second is the fallback: it is the look the player
/// came in with, and it is the only way back out of a costume — so swapping one avatar for another
/// must not overwrite it with the costume they are already wearing.
/// </para>
/// </summary>
public sealed class NftAvatarWardrobeTests
{
    private const int PlayerId = 7701;
    private const string OwnFigure = "hd-180-1.ch-210-66";
    private const string VampireFigure = "hd-185-2.ch-3030-82.lg-275-64";
    private const string PirateFigure = "hd-190-10.ch-255-92";

    [Fact]
    public async Task AnAvatarNobodyGaveThem_IsRefused()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        NftOutfitSnapshot? worn = await harness
            .Grain.WearAsync(harness.UngrantedCopyId, CancellationToken.None)
            .ConfigureAwait(true);

        worn.Should().BeNull("owning a copy is the whole permission model");
    }

    [Fact]
    public async Task WearingNothing_AnswersNullRatherThanBlanks()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        NftOutfitSnapshot? worn = await harness
            .Grain.GetWornAsync(CancellationToken.None)
            .ConfigureAwait(true);

        // Empty strings would tell the client an avatar *is* worn -- its test is against null, and a
        // string read off a packet never is. It would then open the editor on an empty fallback,
        // which loads nothing at all.
        worn.Should().BeNull();
    }

    [Fact]
    public async Task TheWornToken_IsTheOneTheWardrobeListed()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        NftOutfitSnapshot? worn = await harness
            .Grain.WearAsync(harness.VampireCopyId, CancellationToken.None)
            .ConfigureAwait(true);

        ImmutableArray<NftAvatarSnapshot> wardrobe = await harness
            .Grain.GetWardrobeAsync(CancellationToken.None)
            .ConfigureAwait(true);

        string listed = wardrobe.Single(a => a.CopyId == harness.VampireCopyId).TokenId;

        // The editor looks the worn avatar up by token to decide which tile is selected; a mismatch
        // leaves it unable to find any, and the tab opens with nothing highlighted.
        worn!.TokenId.Should().Be(listed);
    }

    [Fact]
    public async Task SwappingOneAvatarForAnother_KeepsTheLookTheyArrivedIn()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.WearAsync(harness.VampireCopyId, CancellationToken.None)
            .ConfigureAwait(true);

        NftOutfitSnapshot? worn = await harness
            .Grain.WearAsync(harness.PirateCopyId, CancellationToken.None)
            .ConfigureAwait(true);

        // Not the vampire: the way out of a costume must lead home, not to the previous costume.
        worn!.FallbackFigure.Should().Be(OwnFigure);
        worn.FallbackGender.Should().Be("M");
    }

    [Fact]
    public async Task PuttingOneOn_ChangesTheFigureTheyWalkAroundIn()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness
            .Grain.WearAsync(harness.VampireCopyId, CancellationToken.None)
            .ConfigureAwait(true);

        harness.FigureSetTo.Should().Be(VampireFigure);
    }

    [Fact]
    public async Task AnUnlistedAvatar_LeavesTheWardrobeAndCannotBeWorn()
    {
        Harness harness = await Harness.CreateAsync().ConfigureAwait(true);

        await harness.DisableAvatarAsync(harness.PirateAvatarId).ConfigureAwait(true);

        ImmutableArray<NftAvatarSnapshot> wardrobe = await harness
            .Grain.GetWardrobeAsync(CancellationToken.None)
            .ConfigureAwait(true);

        NftOutfitSnapshot? worn = await harness
            .Grain.WearAsync(harness.PirateCopyId, CancellationToken.None)
            .ConfigureAwait(true);

        wardrobe.Should().NotContain(a => a.CopyId == harness.PirateCopyId);
        worn.Should().BeNull("the copy is still theirs, but the avatar is no longer offered");
    }

    private sealed class TestDbContextFactory(DbContextOptions<VortexDbContext> options)
        : IDbContextFactory<VortexDbContext>
    {
        public VortexDbContext CreateDbContext() => new(options);
    }

    private sealed class Harness
    {
        private readonly DbContextOptions<VortexDbContext> _options;

        private Harness(PlayerNftWardrobeGrain grain, DbContextOptions<VortexDbContext> options)
        {
            Grain = grain;
            _options = options;
        }

        public PlayerNftWardrobeGrain Grain { get; }

        public int VampireCopyId { get; private init; }

        public int PirateCopyId { get; private init; }

        public int PirateAvatarId { get; private init; }

        /// <summary>A copy belonging to somebody else, which is what an invented packet looks like.</summary>
        public int UngrantedCopyId { get; private init; }

        public string? FigureSetTo { get; private set; }

        public async Task DisableAvatarAsync(int avatarId)
        {
            await using VortexDbContext db = new(_options);

            NftAvatarEntity avatar = await db
                .NftAvatars.FirstAsync(a => a.Id == avatarId)
                .ConfigureAwait(true);

            avatar.Enabled = false;

            await db.SaveChangesAsync().ConfigureAwait(true);
        }

        public static async Task<Harness> CreateAsync()
        {
            DbContextOptions<VortexDbContext> options =
                new DbContextOptionsBuilder<VortexDbContext>()
                    .UseInMemoryDatabase($"nft-wardrobe-{Guid.NewGuid():N}")
                    .Options;

            int vampireCopyId;
            int pirateCopyId;
            int pirateAvatarId;
            int ungrantedCopyId;

            await using (VortexDbContext seed = new(options))
            {
                seed.Players.Add(
                    new PlayerEntity
                    {
                        Id = PlayerId,
                        Name = "tester",
                        Figure = OwnFigure,
                        Gender = AvatarGenderType.Male,
                        PlayerStatus = PlayerStatusType.Offline,
                        PlayerPerks = PlayerPerkFlags.None,
                    }
                );

                seed.Players.Add(
                    new PlayerEntity
                    {
                        Id = PlayerId + 1,
                        Name = "somebody-else",
                        Figure = OwnFigure,
                        Gender = AvatarGenderType.Male,
                        PlayerStatus = PlayerStatusType.Offline,
                        PlayerPerks = PlayerPerkFlags.None,
                    }
                );

                NftAvatarEntity vampire = new()
                {
                    AvatarCode = "halloween_2026_vampire",
                    Figure = VampireFigure,
                    Gender = "M",
                    ContractKey = "habbo:avatar",
                };

                NftAvatarEntity pirate = new()
                {
                    AvatarCode = "summer_2026_pirate",
                    Figure = PirateFigure,
                    Gender = "M",
                    ContractKey = "habbo:avatar_genesis",
                };

                seed.NftAvatars.Add(vampire);
                seed.NftAvatars.Add(pirate);
                await seed.SaveChangesAsync().ConfigureAwait(true);

                PlayerNftAvatarEntity vampireCopy = new()
                {
                    PlayerEntityId = PlayerId,
                    NftAvatarEntityId = vampire.Id,
                    SerialNumber = 1,
                };

                PlayerNftAvatarEntity pirateCopy = new()
                {
                    PlayerEntityId = PlayerId,
                    NftAvatarEntityId = pirate.Id,
                    SerialNumber = 1,
                };

                PlayerNftAvatarEntity otherPlayersCopy = new()
                {
                    PlayerEntityId = PlayerId + 1,
                    NftAvatarEntityId = vampire.Id,
                    SerialNumber = 2,
                };

                seed.PlayerNftAvatars.Add(vampireCopy);
                seed.PlayerNftAvatars.Add(pirateCopy);
                seed.PlayerNftAvatars.Add(otherPlayersCopy);
                await seed.SaveChangesAsync().ConfigureAwait(true);

                vampireCopyId = vampireCopy.Id;
                pirateCopyId = pirateCopy.Id;
                pirateAvatarId = pirate.Id;
                ungrantedCopyId = otherPlayersCopy.Id;
            }

            Harness? harness = null;

            IGrainFactory grainFactory = FakeProxy.Create<IGrainFactory>(call =>
                call.Method.Name == nameof(IGrainFactory.GetGrain)
                    ? FakeProxy.Create<IPlayerGrain>(playerCall =>
                        playerCall.Method.Name switch
                        {
                            nameof(IPlayerGrain.GetSummaryAsync) => Task.FromResult(BuildSummary()),
                            nameof(IPlayerGrain.SetFigureAsync) => RecordFigure(playerCall),
                            _ => null,
                        }
                    )
                    : null
            );

            static PlayerSummarySnapshot BuildSummary() =>
                new()
                {
                    PlayerId = new PlayerId(PlayerId),
                    Name = "tester",
                    Motto = string.Empty,
                    Figure = OwnFigure,
                    Gender = AvatarGenderType.Male,
                    AchievementScore = 0,
                    CreatedAt = DateTime.UtcNow,
                };

            object? RecordFigure(ProxyCall call)
            {
                harness!.FigureSetTo = call.Args?[0] as string;

                return Task.CompletedTask;
            }

            harness = new Harness(
                GrainActivationContext.CreateWithIntegerKey<PlayerNftWardrobeGrain>(
                    PlayerId,
                    new TestDbContextFactory(options),
                    grainFactory,
                    NullLogger<PlayerNftWardrobeGrain>.Instance
                ),
                options
            )
            {
                VampireCopyId = vampireCopyId,
                PirateCopyId = pirateCopyId,
                PirateAvatarId = pirateAvatarId,
                UngrantedCopyId = ungrantedCopyId,
            };

            return harness;
        }
    }
}
