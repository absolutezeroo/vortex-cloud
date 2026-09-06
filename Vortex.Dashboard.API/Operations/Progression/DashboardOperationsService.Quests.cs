using System;
using System.Threading;
using System.Threading.Tasks;
using Vortex.Primitives.Quests.Admin;

namespace Vortex.Dashboard.API.Operations;

/// <summary>
/// Quest admin operations. Each routes through <see cref="Vortex.Primitives.Quests.IQuestAdminService"/>
/// (never a direct DB write), which reloads the live quest cache after committing, and emits a durable
/// audit event with the operator's reason — same contract as the catalog/targeted-offer operations.
/// </summary>
internal sealed partial class DashboardOperationsService
{
    public Task<OperationResult> CreateQuestAsync(
        CreateQuestRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.quest.create",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.CampaignCode,
                request.LocalizationCode,
                request.QuestType,
                request.RewardType,
                request.RewardAmount,
            },
            work: async c =>
            {
                QuestAdminResult result = await _questAdmin
                    .CreateAsync(
                        new QuestCreateSpec(
                            request.CampaignCode,
                            request.ChainCode,
                            request.LocalizationCode,
                            request.QuestType,
                            request.TargetType,
                            request.TargetValue,
                            request.Enabled,
                            request.TotalSteps,
                            request.RewardType,
                            request.RewardAmount,
                            request.CatalogPageName,
                            request.ImageVersion,
                            request.SortOrder,
                            request.Easy,
                            request.Seasonal,
                            request.SeasonalSeconds,
                            request.EndsAt
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> UpdateQuestAsync(
        UpdateQuestRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.quest.update",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new
            {
                request.QuestId,
                request.CampaignCode,
                request.RewardType,
                request.RewardAmount,
            },
            work: async c =>
            {
                QuestAdminResult result = await _questAdmin
                    .UpdateAsync(
                        request.QuestId,
                        new QuestUpdateSpec(
                            request.CampaignCode,
                            request.ChainCode,
                            request.LocalizationCode,
                            request.QuestType,
                            request.TargetType,
                            request.TargetValue,
                            request.Enabled,
                            request.TotalSteps,
                            request.RewardType,
                            request.RewardAmount,
                            request.CatalogPageName,
                            request.ImageVersion,
                            request.SortOrder,
                            request.Easy,
                            request.Seasonal,
                            request.SeasonalSeconds,
                            request.EndsAt
                        ),
                        c
                    )
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );

    public Task<OperationResult> DeleteQuestAsync(
        DeleteQuestRequest request,
        string actor,
        CancellationToken ct
    ) =>
        ExecuteAsync(
            "ops.quest.delete",
            actor,
            request.Reason,
            targetPlayerId: null,
            roomId: null,
            detail: new { request.QuestId },
            work: async c =>
            {
                QuestAdminResult result = await _questAdmin
                    .DeleteAsync(request.QuestId, c)
                    .ConfigureAwait(false);

                if (!result.Success)
                {
                    throw new InvalidOperationException(result.ErrorCode);
                }
            },
            ct
        );
}
