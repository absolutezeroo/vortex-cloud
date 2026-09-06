using FluentAssertions;
using Vortex.Habbicons;
using Vortex.Primitives.Habbicons;
using Xunit;

namespace Vortex.Rewards.Tests;

/// <summary>
/// The string tying the dashboard's Habbicon admin service to this project without a reference.
/// </summary>
/// <remarks>
/// The same shape as <see cref="RewardTrackCacheNameTests"/>, and for the same reason: the admin
/// service reloads the catalogue by name through <c>IReferenceDataReloader</c>, which answers "no
/// such cache" rather than throwing. A rename of <see cref="HabbiconCatalog"/> would therefore leave
/// every album edit reporting success and never taking effect.
/// </remarks>
public sealed class HabbiconCacheNameTests
{
    [Fact]
    public void The_catalog_answers_to_the_name_the_admin_service_reloads() =>
        typeof(HabbiconCatalog)
            .Name.Should()
            .Be(
                IHabbiconCatalog.CacheName,
                "the dashboard reloads it by this name and is told nothing if it is wrong"
            );
}
