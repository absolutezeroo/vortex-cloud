using System.Collections.Generic;
using Vortex.Authentication.Permissions;

namespace Vortex.Authentication.Moderation;

/// <summary>
/// Default CFH category/topic bootstrap, seeded only into a fresh (empty) catalog — see
/// <see cref="CfhCatalogSeederService"/>. <see cref="TopicSeed.BanPresetIndex"/> links a topic to
/// one of the <see cref="Vortex.Primitives.Permissions.SanctionPresetKind.Ban"/> presets seeded by
/// <see cref="SanctionPresetSeederService"/>, giving
/// CloseIssueDefaultActionMessageHandler/DefaultSanctionMessageHandler a duration to apply — the
/// admin dashboard is expected to be where this gets tuned to match real moderation policy.
/// </summary>
internal static class DefaultCfhCatalog
{
    public sealed record TopicSeed(string Name, string? Consequence, int? BanPresetIndex);

    public sealed record CategorySeed(string Name, IReadOnlyList<TopicSeed> Topics);

    public static readonly IReadOnlyList<CategorySeed> All =
    [
        new CategorySeed(
            "Bullying",
            [
                new TopicSeed("Harassment", "Ban 1 day", BanPresetIndex: 1),
                new TopicSeed("Hate speech", "Ban 3 days", BanPresetIndex: 2),
            ]
        ),
        new CategorySeed(
            "Bad behaviour",
            [
                new TopicSeed("Inappropriate language", "Ban 2 hours", BanPresetIndex: 0),
                new TopicSeed("Inappropriate room or look", "Ban 1 day", BanPresetIndex: 1),
            ]
        ),
        new CategorySeed(
            "Scamming",
            [
                new TopicSeed("Trading scam", "Ban 1 week", BanPresetIndex: 3),
                new TopicSeed("Impersonation", "Ban 3 days", BanPresetIndex: 2),
            ]
        ),
        new CategorySeed(
            "Other",
            [
                new TopicSeed("Bug abuse", "Ban 1 week", BanPresetIndex: 3),
                new TopicSeed("Spam or advertising", "Ban 2 hours", BanPresetIndex: 0),
                new TopicSeed("Other", Consequence: null, BanPresetIndex: null),
            ]
        ),
    ];
}
