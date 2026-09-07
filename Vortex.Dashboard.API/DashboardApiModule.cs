using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vortex.Dashboard.API.Admin.Catalogue;
using Vortex.Dashboard.API.Admin.Hotel;
using Vortex.Dashboard.API.Admin.Progression;
using Vortex.Dashboard.API.Api;
using Vortex.Dashboard.API.Hosting;
using Vortex.Dashboard.API.Http;
using Vortex.Dashboard.API.Infrastructure;
using Vortex.Dashboard.API.Operations;
using Vortex.Dashboard.API.Security;
using Vortex.Observability.Configuration;
using Vortex.Primitives.Authentication;
using Vortex.Primitives.Catalog;
using Vortex.Primitives.Content;
using Vortex.Primitives.Fishing;
using Vortex.Primitives.Furniture;
using Vortex.Primitives.Habbicons;
using Vortex.Primitives.Hosting;
using Vortex.Primitives.MysteryBox;
using Vortex.Primitives.Navigator;
using Vortex.Primitives.Permissions;
using Vortex.Primitives.Plugins;
using Vortex.Primitives.Polls;
using Vortex.Primitives.Prizes;
using Vortex.Primitives.Quests;
using Vortex.Primitives.RewardTracks;
using Vortex.Primitives.Sound;

namespace Vortex.Dashboard.API;

/// <summary>
/// Registers the independent admin dashboard API: authentication/session services, the asset store,
/// the read JSON API, the operations layer and the self-hosted HTTP front controller. It consumes
/// Observability (metrics, audit sinks, health/incidents) and Database but owns no observability
/// pipeline of its own — the audit and error-grouping writers live in <c>ObservabilityModule</c> so
/// they run regardless of whether the dashboard is enabled.
/// <para>
/// <b>How this project is laid out, and the one rule it breaks.</b> Three of its folders —
/// <c>Api</c>, <c>Hosting</c>, <c>Operations</c> — are each one very large partial class, and its
/// parts are grouped into <c>Catalogue</c>, <c>Progression</c>, <c>Hotel</c>, <c>Platform</c> and
/// <c>Safety</c>, the same five families <c>Admin</c> uses. Every part of a partial class must
/// declare the <em>same</em> namespace, so those subfolders deliberately do not appear in it: a file
/// in <c>Operations/Catalogue/</c> is still <c>Vortex.Dashboard.API.Operations</c>. Folder equals
/// namespace everywhere else in this repository, and it cannot here without splitting the class.
/// </para>
/// <para>
/// <c>Admin</c> is the exception that keeps the rule: it holds sixteen separate classes rather than
/// one, so its subfolders <em>are</em> namespaces.
/// </para>
/// </summary>
public sealed class DashboardApiModule : IHostPluginModule
{
    public string Key => "turbo-dashboard-api";

    public void ConfigureServices(IServiceCollection services, HostApplicationBuilder builder)
    {
        // ObservabilityModule binds the same section; both register the validator through
        // TryAddEnumerable so it runs exactly once regardless of module order.
        services
            .AddOptions<ObservabilityConfig>()
            .Bind(builder.Configuration.GetSection(ObservabilityConfig.SECTION_NAME))
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<
                IValidateOptions<ObservabilityConfig>,
                ObservabilityConfigValidator
            >()
        );

        // Shared with WebApiModule; whichever module registers first wins.
        services.TryAddSingleton<RequiredServiceGuard>();

        services.TryAddSingleton<DashboardSessionStore>();
        // Registered as the shared interface too, so a password change reaches these sessions
        // without the password service knowing how many session stores exist.
        services.AddSingleton<IAccountSessionRevoker>(sp =>
            sp.GetRequiredService<DashboardSessionStore>()
        );
        services.TryAddSingleton<DashboardAuthService>();
        services.TryAddSingleton<DashboardAssetStore>();
        services.TryAddSingleton<DashboardAssetUrls>();
        // Singleton because it caches a 38 MB parsed furnidata; a per-request instance would parse it
        // again on every keystroke of a search.
        services.TryAddSingleton<GamedataDocumentStore>();
        // Reads the Habbicon spritesheet's metadata off the same asset pack, and caches it until the
        // operator swaps the pack.
        services.TryAddSingleton<HabbiconArtwork>();
        services.TryAddSingleton<DashboardAuditEmitter>();
        services.TryAddSingleton<DashboardApiService>();
        services.TryAddSingleton<DashboardMonitoringReads>();
        // What every dashboard write goes through. A collaborator, so a subject that becomes its
        // own operations class takes it directly instead of inheriting a mechanism.
        services.TryAddSingleton<OperationRunner>();
        // The catalogue, as its own two classes rather than a slice of two god services. Three
        // dependencies for the reads, two for the writes, both visible in their constructors.
        services.TryAddSingleton<CatalogReads>();
        services.TryAddSingleton<CatalogOperations>();
        // Polls, likewise: two dependencies for the reads, two for the writes.
        services.TryAddSingleton<PollReads>();
        services.TryAddSingleton<PollOperations>();
        // Quest content: the reads need nothing but a context.
        services.TryAddSingleton<QuestContentReads>();
        services.TryAddSingleton<QuestContentOperations>();
        services.TryAddSingleton<ArticleReads>();
        services.TryAddSingleton<ArticleOperations>();
        services.TryAddSingleton<SongReads>();
        services.TryAddSingleton<SongOperations>();
        services.TryAddSingleton<FishingReads>();
        services.TryAddSingleton<FishingOperations>();
        services.TryAddSingleton<NavigatorReads>();
        services.TryAddSingleton<NavigatorOperations>();
        services.TryAddSingleton<FurnitureReads>();
        services.TryAddSingleton<FurnitureOperations>();
        services.TryAddSingleton<QuestReads>();
        services.TryAddSingleton<QuestOperations>();
        services.TryAddSingleton<TargetedOfferReads>();
        services.TryAddSingleton<TargetedOfferOperations>();
        services.TryAddSingleton<PrizePoolReads>();
        services.TryAddSingleton<PrizePoolOperations>();
        services.TryAddSingleton<MysteryBoxReads>();
        services.TryAddSingleton<MysteryBoxOperations>();
        services.TryAddSingleton<StaffReads>();
        services.TryAddSingleton<StaffOperations>();
        services.TryAddSingleton<ContentOperations>();
        services.TryAddSingleton<RewardOperations>();
        services.TryAddSingleton<HabbiconReads>();
        services.TryAddSingleton<RewardTrackReads>();
        services.TryAddSingleton<GamedataReads>();
        services.TryAddSingleton<GamedataOperations>();
        services.TryAddSingleton<BenchmarkReads>();
        services.TryAddSingleton<BenchmarkOperations>();
        services.TryAddSingleton<BackupOperations>();
        services.TryAddSingleton<ConsoleOperations>();
        services.TryAddSingleton<PrivacyOperations>();
        services.TryAddSingleton<DashboardOperationsService>();

        // Authoring content is the dashboard's job, not the emulator's: the hotel runs campaigns,
        // it does not write them. So the admin service lives here and is registered here, and a
        // host that does not load this module has no content-authoring path at all -- which is what
        // being an optional plugin means. It builds on what the domain publishes: the catalogue, the
        // content rules in Vortex.Primitives.RewardTracks.Content, and IReferenceDataReloader.
        services.TryAddSingleton<IRewardTrackAdminService, RewardTrackAdminService>();
        services.TryAddSingleton<IStaffAdminService, StaffAdminService>();
        services.TryAddSingleton<ICatalogAdminService, CatalogAdminService>();
        services.TryAddSingleton<ITargetedOfferAdminService, TargetedOfferAdminService>();
        services.TryAddSingleton<IFishingAdminService, FishingAdminService>();
        services.TryAddSingleton<IFurnitureAdminService, FurnitureAdminService>();
        services.TryAddSingleton<ISongAdminService, SongAdminService>();
        services.TryAddSingleton<IHabbiconAdminService, HabbiconAdminService>();
        services.TryAddSingleton<INavigatorAdminService, NavigatorAdminService>();
        services.TryAddSingleton<IContentAdminService, ContentAdminService>();
        services.TryAddSingleton<IMysteryBoxAdminService, MysteryBoxAdminService>();
        services.TryAddSingleton<IPollAdminService, PollAdminService>();
        services.TryAddSingleton<IPrizePoolAdminService, PrizePoolAdminService>();
        services.TryAddSingleton<IQuestAdminService, QuestAdminService>();
        services.TryAddSingleton<IQuestContentAdminService, QuestContentAdminService>();
        services.TryAddSingleton<IWebArticleAdminService, WebArticleAdminService>();

        // The dashboard runs as a self-contained ASP.NET Core (Kestrel) app inside the generic host.
        services.AddHostedService<DashboardWebHost>();
    }
}
