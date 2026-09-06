using FluentAssertions;
using Vortex.Primitives.RewardTracks;
using Vortex.RewardTracks;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The one string that ties the dashboard's admin service to this project without a reference.
/// </summary>
/// <remarks>
/// Authoring content is the dashboard's job, so <c>RewardTrackAdminService</c> lives there and
/// reloads through <c>IReferenceDataReloader</c>, which names caches by their implementing type's
/// name. That means a rename of <see cref="RewardTrackCatalog"/> would leave the admin service
/// asking for a cache that no longer exists — and the reloader answers "no such cache" rather than
/// throwing, so every content write would go on succeeding while quietly never taking effect.
/// <para>
/// This is the assertion that turns that into a build failure instead.
/// </para>
/// </remarks>
public sealed class RewardTrackCacheNameTests
{
    [Fact]
    public void The_catalogue_answers_to_the_name_the_admin_service_reloads() =>
        typeof(RewardTrackCatalog)
            .Name.Should()
            .Be(
                IRewardTrackCatalog.CacheName,
                "the dashboard reloads the catalogue by this name and is told nothing if it is wrong"
            );
}
