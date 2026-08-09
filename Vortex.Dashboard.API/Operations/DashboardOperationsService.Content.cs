using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Content;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Content operations: achievement ladders, bots and hand items, NFT collections, the economy's
/// smaller tables, and direct player grants. Each routes through
/// <see cref="IContentAdminService"/> — never a direct DB write — which owns the live-cache reload
/// each domain needs, and each is audited with the operator's reason.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> SaveAchievementAsync(
        AchievementRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.AchievementId > 0
                ? "ops.content.achievement.update"
                : "ops.content.achievement.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.AchievementId,
                request.Name,
                request.Category,
            },
            work: async c =>
            {
                AchievementSpec spec = new(request.Name, request.Category, request.DisplayMethod);

                Throw(
                    request.AchievementId > 0
                        ? await _contentAdmin
                            .UpdateAchievementAsync(request.AchievementId, spec, c)
                            .ConfigureAwait(false)
                        : await _contentAdmin.CreateAchievementAsync(spec, c).ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> DeleteAchievementAsync(
        DeleteAchievementRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.achievement.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.AchievementId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .DeleteAchievementAsync(request.AchievementId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveAchievementLevelAsync(
        AchievementLevelRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.achievement.level",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.AchievementId,
                request.Level,
                request.BadgeCode,
                request.ProgressRequirement,
            },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .UpsertAchievementLevelAsync(
                            request.AchievementId,
                            new AchievementLevelSpec(
                                request.Level,
                                request.BadgeCode,
                                request.ProgressRequirement,
                                request.RewardAmount,
                                request.RewardType,
                                request.ScorePoints
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteAchievementLevelAsync(
        DeleteAchievementLevelRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.achievement.level.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.LevelId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .DeleteAchievementLevelAsync(request.LevelId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveHandItemAsync(
        HandItemRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.hand_item.save",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.HandItemId, request.Name },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .UpsertHandItemAsync(
                            new HandItemSpec(
                                request.HandItemId,
                                request.Name,
                                request.Nutrition,
                                request.Thirst
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteHandItemAsync(
        DeleteHandItemRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.hand_item.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Id },
            work: async c =>
                Throw(await _contentAdmin.DeleteHandItemAsync(request.Id, c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> UpdateBotAsync(
        BotRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.bot.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.BotId, request.Name },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .UpdateBotAsync(
                            request.BotId,
                            new BotSpec(request.Name, request.Motto, request.Figure),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteBotAsync(
        DeleteBotRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.bot.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.BotId },
            work: async c =>
                Throw(await _contentAdmin.DeleteBotAsync(request.BotId, c).ConfigureAwait(false)),
            ct
        );

    public Task<OperationResult> SaveCollectionAsync(
        CollectionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.CollectionId > 0
                ? "ops.content.collection.update"
                : "ops.content.collection.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.CollectionId, request.CollectionCode },
            work: async c =>
            {
                NftCollectionSpec spec = new(
                    request.CollectionCode,
                    request.Name,
                    request.BoostScore,
                    request.Status,
                    request.RewardProductCode,
                    request.BonusProductCode
                );

                Throw(
                    request.CollectionId > 0
                        ? await _contentAdmin
                            .UpdateCollectionAsync(request.CollectionId, spec, c)
                            .ConfigureAwait(false)
                        : await _contentAdmin.CreateCollectionAsync(spec, c).ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> DeleteCollectionAsync(
        DeleteCollectionRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.collection.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.CollectionId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .DeleteCollectionAsync(request.CollectionId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveCollectionItemAsync(
        CollectionItemRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.ItemId > 0
                ? "ops.content.collection.item.update"
                : "ops.content.collection.item.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.ItemId,
                request.CollectionId,
                request.ProductCode,
            },
            work: async c =>
            {
                NftCollectionItemSpec spec = new(
                    request.ProductCode,
                    request.ItemTypeId,
                    request.ProductTypeId,
                    request.Score,
                    request.Rarity,
                    request.SortOrder
                );

                Throw(
                    request.ItemId > 0
                        ? await _contentAdmin
                            .UpdateCollectionItemAsync(request.ItemId, spec, c)
                            .ConfigureAwait(false)
                        : await _contentAdmin
                            .CreateCollectionItemAsync(request.CollectionId, spec, c)
                            .ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> DeleteCollectionItemAsync(
        DeleteCollectionItemRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.collection.item.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.ItemId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .DeleteCollectionItemAsync(request.ItemId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveCurrencyAsync(
        CurrencyRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            request.CurrencyId > 0 ? "ops.content.currency.update" : "ops.content.currency.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.CurrencyId, request.Name },
            work: async c =>
            {
                CurrencySpec spec = new(
                    request.Name,
                    request.CurrencyType,
                    request.ActivityPointType,
                    request.Enabled,
                    request.StartingAmount
                );

                Throw(
                    request.CurrencyId > 0
                        ? await _contentAdmin
                            .UpdateCurrencyAsync(request.CurrencyId, spec, c)
                            .ConfigureAwait(false)
                        : await _contentAdmin.CreateCurrencyAsync(spec, c).ConfigureAwait(false)
                );
            },
            ct
        );

    public Task<OperationResult> SaveBuildersClubTierAsync(
        BuildersClubTierRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.builders_club.save",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.Level, request.FurniLimit },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .UpsertBuildersClubTierAsync(
                            new BuildersClubTierSpec(request.Level, request.FurniLimit),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteBuildersClubTierAsync(
        DeleteBuildersClubTierRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.builders_club.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.TierId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .DeleteBuildersClubTierAsync(request.TierId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> SaveRentableTermsAsync(
        RentableTermsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.rentable_terms.save",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.FurnitureId, request.Price },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .UpsertRentableSpaceTermsAsync(
                            new RentableSpaceTermsSpec(
                                request.FurnitureId,
                                request.Price,
                                request.CurrencyTypeId,
                                request.RentDurationSeconds,
                                request.RequiresHc
                            ),
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> DeleteRentableTermsAsync(
        DeleteRentableTermsRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.rentable_terms.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.TermsId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .DeleteRentableSpaceTermsAsync(request.TermsId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> GrantBadgeAsync(
        BadgeGrantRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.badge.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.BadgeCode },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .GrantBadgeAsync(request.PlayerId, request.BadgeCode, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> RevokeBadgeAsync(
        BadgeGrantRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.badge.revoke",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.BadgeCode },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .RevokeBadgeAsync(request.PlayerId, request.BadgeCode, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> GrantEffectAsync(
        EffectGrantRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.effect.grant",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.EffectId, request.DurationSeconds },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .GrantEffectAsync(
                            request.PlayerId,
                            request.EffectId,
                            request.DurationSeconds,
                            c
                        )
                        .ConfigureAwait(false)
                ),
            ct
        );

    public Task<OperationResult> RevokeEffectAsync(
        EffectGrantRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.content.effect.revoke",
            actor,
            request.Reason,
            targetPlayerId: request.PlayerId,
            roomId: null,
            detail: new { request.EffectId },
            work: async c =>
                Throw(
                    await _contentAdmin
                        .RevokeEffectAsync(request.PlayerId, request.EffectId, c)
                        .ConfigureAwait(false)
                ),
            ct
        );

    private static void Throw(ContentAdminResult result)
    {
        if (!result.Success)
        {
            throw new InvalidOperationException(result.ErrorCode);
        }
    }
}
