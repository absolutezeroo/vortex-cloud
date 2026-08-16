// Central route table for the dashboard SPA. Each section declares the capability (or capabilities)
// required to view it; svelte-spa-router enforces it through a `conditions` guard and the nav uses
// the same rule so operators only ever see authorized tools. A failed guard raises the router's
// `conditionsFailed` event, which App.svelte turns into the /access-denied view.
//
// Pages are loaded on demand. `load` is a thunk returning a dynamic import, which Vite turns into one
// chunk per page: opening /overview no longer downloads the catalogue editor, the furni picker and
// the survey authoring tool along with it. Only the two pages that must render without a round trip
// are imported eagerly -- the overview (every session lands on it) and the access-denied fallback
// (it is what the router falls back TO, including when a chunk cannot be fetched).

import { wrap } from 'svelte-spa-router/wrap';
import { get } from 'svelte/store';
import { identity } from './session.js';
import { hasDashboardCapability } from './permissions.js';
import { ROUTE_PERMISSIONS } from './dashboardPermissions.js';

import OverviewPage from '../pages/OverviewPage.svelte';
import AccessDeniedPage from '../pages/AccessDeniedPage.svelte';
import RouteLoading from '../components/RouteLoading.svelte';

// Display + permission metadata for the navigation sidebar. `group` buckets items in the sidebar
// (see AppShell.svelte) and is the DOMAIN the page is about -- players, rooms, economy -- not the
// kind of tool it is. Grouping by kind (investigate / stats / act) scattered every domain across
// three places: the quest editor sat under "Act" while quest stats sat under "Stats", and finding
// anything meant knowing which bucket a page had been filed under rather than what it was about.
// `writes: true` marks a page that can change server state; the sidebar badges it, which is the
// distinction the old grouping was really carrying.
// Order within a group is the nav order. Keep GROUP_ORDER in AppShell.svelte in sync.
// label/short are i18n keys (resolved via $t in AppShell.svelte), not display strings -- see
// lib/locales/{en,fr}.js's `nav` namespace, which must have a matching entry for every key here.
// `load` must be a literal `() => import('../pages/X.svelte')` -- Vite only code-splits imports it
// can see statically, so a computed path would silently fold every page back into one chunk.
export const NAV = [
  { path: '/overview', labelKey: 'nav.overview', shortKey: 'nav.overviewShort', group: 'Live', caps: ROUTE_PERMISSIONS.overview, component: OverviewPage },
  { path: '/infrastructure', labelKey: 'nav.infrastructure', shortKey: 'nav.infrastructureShort', group: 'Live', caps: ROUTE_PERMISSIONS.infrastructure, load: () => import('../pages/InfrastructurePage.svelte') },
  { path: '/performance', labelKey: 'nav.performance', shortKey: 'nav.performanceShort', group: 'Live', caps: ROUTE_PERMISSIONS.performance, load: () => import('../pages/PerformancePage.svelte') },
  { path: '/benchmark', labelKey: 'nav.benchmark', shortKey: 'nav.benchmarkShort', group: 'Live', caps: ROUTE_PERMISSIONS.benchmark, load: () => import('../pages/BenchmarkPage.svelte'), writes: true },
  { path: '/packets', labelKey: 'nav.packets', shortKey: 'nav.packetsShort', group: 'Live', caps: ROUTE_PERMISSIONS.packets, load: () => import('../pages/PacketsPage.svelte') },
  { path: '/incidents', labelKey: 'nav.incidents', shortKey: 'nav.incidentsShort', group: 'Live', caps: ROUTE_PERMISSIONS.incidents, load: () => import('../pages/IncidentsPage.svelte') },

  { path: '/investigation', labelKey: 'nav.investigation', shortKey: 'nav.investigationShort', group: 'Players', caps: ROUTE_PERMISSIONS.investigation, load: () => import('../pages/InvestigationPage.svelte') },
  { path: '/operations', labelKey: 'nav.operations', shortKey: 'nav.operationsShort', group: 'Players', caps: ROUTE_PERMISSIONS.operations, load: () => import('../pages/OperationsPage.svelte'), writes: true },
  { path: '/player-rewards', labelKey: 'nav.playerRewards', shortKey: 'nav.playerRewardsShort', group: 'Players', caps: ROUTE_PERMISSIONS.playerRewards, load: () => import('../pages/PlayerRewardsPage.svelte'), writes: true },
  { path: '/subscriptions', labelKey: 'nav.subscriptions', shortKey: 'nav.subscriptionsShort', group: 'Players', caps: ROUTE_PERMISSIONS.economy, load: () => import('../pages/SubscriptionsPage.svelte') },
  { path: '/moderation', labelKey: 'nav.moderation', shortKey: 'nav.moderationShort', group: 'Players', caps: ROUTE_PERMISSIONS.moderation, load: () => import('../pages/ModerationPage.svelte') },
  { path: '/moderation-actions', labelKey: 'nav.moderationActions', shortKey: 'nav.moderationActionsShort', group: 'Players', caps: ROUTE_PERMISSIONS.moderationActions, load: () => import('../pages/ModerationActionsPage.svelte'), writes: true },
  { path: '/cfh', labelKey: 'nav.cfh', shortKey: 'nav.cfhShort', group: 'Players', caps: ROUTE_PERMISSIONS.cfh, load: () => import('../pages/CfhQueuePage.svelte'), writes: true },
  { path: '/cfh-stats', labelKey: 'nav.cfhStats', shortKey: 'nav.cfhStatsShort', group: 'Players', caps: ROUTE_PERMISSIONS.cfhStats, load: () => import('../pages/CfhStatsPage.svelte') },
  { path: '/staff', labelKey: 'nav.staff', shortKey: 'nav.staffShort', group: 'Players', caps: ROUTE_PERMISSIONS.staff, load: () => import('../pages/StaffPage.svelte'), writes: true },

  { path: '/rooms', labelKey: 'nav.rooms', shortKey: 'nav.roomsShort', group: 'Rooms', caps: ROUTE_PERMISSIONS.rooms, load: () => import('../pages/RoomsPage.svelte') },
  { path: '/room-control', labelKey: 'nav.roomControl', shortKey: 'nav.roomControlShort', group: 'Rooms', caps: ROUTE_PERMISSIONS.roomControl, load: () => import('../pages/RoomControlPage.svelte'), writes: true },
  { path: '/navigator-config', labelKey: 'nav.navigatorConfig', shortKey: 'nav.navigatorConfigShort', group: 'Rooms', caps: ROUTE_PERMISSIONS.navigatorConfig, load: () => import('../pages/NavigatorConfigPage.svelte'), writes: true },
  { path: '/bots', labelKey: 'nav.bots', shortKey: 'nav.botsShort', group: 'Rooms', caps: ROUTE_PERMISSIONS.bots, load: () => import('../pages/BotsPage.svelte'), writes: true },
  { path: '/pets-stats', labelKey: 'nav.petsStats', shortKey: 'nav.petsStatsShort', group: 'Rooms', caps: ROUTE_PERMISSIONS.petsStats, load: () => import('../pages/PetsStatsPage.svelte') },
  { path: '/wired-stats', labelKey: 'nav.wiredStats', shortKey: 'nav.wiredStatsShort', group: 'Rooms', caps: ROUTE_PERMISSIONS.wiredStats, load: () => import('../pages/WiredStatsPage.svelte') },

  { path: '/economy', labelKey: 'nav.economy', shortKey: 'nav.economyShort', group: 'Economy', caps: ROUTE_PERMISSIONS.economy, load: () => import('../pages/EconomyPage.svelte') },
  { path: '/economy-trends', labelKey: 'nav.economyTrends', shortKey: 'nav.economyTrendsShort', group: 'Economy', caps: ROUTE_PERMISSIONS.economy, load: () => import('../pages/EconomyTrendsPage.svelte') },
  { path: '/catalog', labelKey: 'nav.catalog', shortKey: 'nav.catalogShort', group: 'Economy', caps: ROUTE_PERMISSIONS.catalog, load: () => import('../pages/CatalogPage.svelte'), writes: true },
  { path: '/catalog-purchases', labelKey: 'nav.catalogPurchases', shortKey: 'nav.catalogPurchasesShort', group: 'Economy', caps: ROUTE_PERMISSIONS.catalogPurchases, load: () => import('../pages/CatalogPurchasesStatsPage.svelte') },
  { path: '/marketplace', labelKey: 'nav.marketplace', shortKey: 'nav.marketplaceShort', group: 'Economy', caps: ROUTE_PERMISSIONS.economy, load: () => import('../pages/MarketplacePage.svelte') },
  { path: '/targeted-offers', labelKey: 'nav.targetedOffers', shortKey: 'nav.targetedOffersShort', group: 'Economy', caps: ROUTE_PERMISSIONS.targetedOffers, load: () => import('../pages/TargetedOffersPage.svelte'), writes: true },
  { path: '/targeted-offers-stats', labelKey: 'nav.targetedOffersStats', shortKey: 'nav.targetedOffersStatsShort', group: 'Economy', caps: ROUTE_PERMISSIONS.targetedOffersStats, load: () => import('../pages/TargetedOffersStatsPage.svelte') },
  { path: '/vouchers', labelKey: 'nav.vouchers', shortKey: 'nav.vouchersShort', group: 'Economy', caps: ROUTE_PERMISSIONS.vouchers, load: () => import('../pages/VouchersPage.svelte'), writes: true },
  { path: '/economy-extras', labelKey: 'nav.economyExtras', shortKey: 'nav.economyExtrasShort', group: 'Economy', caps: ROUTE_PERMISSIONS.economyExtras, load: () => import('../pages/EconomyExtrasPage.svelte'), writes: true },

  { path: '/quests', labelKey: 'nav.quests', shortKey: 'nav.questsShort', group: 'Content', caps: ROUTE_PERMISSIONS.quests, load: () => import('../pages/QuestsPage.svelte'), writes: true },
  { path: '/polls', labelKey: 'nav.polls', shortKey: 'nav.pollsShort', group: 'Content', caps: ROUTE_PERMISSIONS.polls, load: () => import('../pages/PollsPage.svelte'), writes: true },
  { path: '/achievements', labelKey: 'nav.achievements', shortKey: 'nav.achievementsShort', group: 'Content', caps: ROUTE_PERMISSIONS.achievements, load: () => import('../pages/AchievementsPage.svelte'), writes: true },
  // Same capability as /achievements: the statue is a view onto achievement progress, so a new one
  // would only mean granting it to every role before anyone could open the page.
  { path: '/achievement-resolutions', labelKey: 'nav.achievementResolutions', shortKey: 'nav.achievementResolutionsShort', group: 'Content', caps: ROUTE_PERMISSIONS.achievements, load: () => import('../pages/AchievementResolutionsPage.svelte') },
  { path: '/mystery-box', labelKey: 'nav.mysteryBox', shortKey: 'nav.mysteryBoxShort', group: 'Content', caps: ROUTE_PERMISSIONS.mysteryBox, load: () => import('../pages/MysteryBoxPage.svelte'), writes: true },
  { path: '/prize-pools', labelKey: 'nav.prizePools', shortKey: 'nav.prizePoolsShort', group: 'Content', caps: ROUTE_PERMISSIONS.prizePools, load: () => import('../pages/PrizePoolsPage.svelte'), writes: true },
  { path: '/collectibles', labelKey: 'nav.collectibles', shortKey: 'nav.collectiblesShort', group: 'Content', caps: ROUTE_PERMISSIONS.collectibles, load: () => import('../pages/CollectiblesPage.svelte'), writes: true },
  { path: '/furniture-definitions', labelKey: 'nav.furnitureDefinitions', shortKey: 'nav.furnitureDefinitionsShort', group: 'Content', caps: ROUTE_PERMISSIONS.furnitureDefinitions, load: () => import('../pages/FurnitureDefinitionsPage.svelte'), writes: true },

  { path: '/groups-stats', labelKey: 'nav.groupsStats', shortKey: 'nav.groupsStatsShort', group: 'Social', caps: ROUTE_PERMISSIONS.groupsStats, load: () => import('../pages/GroupsStatsPage.svelte') },
  { path: '/social', labelKey: 'nav.social', shortKey: 'nav.socialShort', group: 'Social', caps: ROUTE_PERMISSIONS.social, load: () => import('../pages/SocialPage.svelte'), writes: true },

  { path: '/audit', labelKey: 'nav.audit', shortKey: 'nav.auditShort', group: 'System', caps: ROUTE_PERMISSIONS.audit, load: () => import('../pages/AuditPage.svelte') },
  { path: '/config', labelKey: 'nav.config', shortKey: 'nav.configShort', group: 'System', caps: ROUTE_PERMISSIONS.config, load: () => import('../pages/ConfigPage.svelte'), writes: true },
  { path: '/api-explorer', labelKey: 'nav.apiExplorer', shortKey: 'nav.apiExplorerShort', group: 'System', caps: ROUTE_PERMISSIONS.apiExplorer, load: () => import('../pages/ApiExplorerPage.svelte') },
];

const canSee = (caps) => () => hasDashboardCapability(get(identity), caps);

// Pass the identity explicitly (e.g. from a reactive `$identity`) to recompute on changes; falls
// back to the current store value for non-reactive callers such as the router guards.
export function hasRouteAccess(item, who = get(identity)) {
  return hasDashboardCapability(who, item.caps);
}

// A chunk that 404s means the operator's tab predates a redeploy: the index they booted from points
// at hashed filenames the server no longer has (the build writes with emptyOutDir). One reload picks
// up the new index; the sessionStorage flag stops that from becoming a reload loop if the chunk is
// genuinely broken, in which case the router falls through to the access-denied view.
const RELOADED_KEY = 'vortex.dashboard.chunkReload';

async function loadPage(item) {
  try {
    const module = await item.load();
    sessionStorage.removeItem(RELOADED_KEY);
    return module;
  } catch (err) {
    if (sessionStorage.getItem(RELOADED_KEY) !== '1') {
      sessionStorage.setItem(RELOADED_KEY, '1');
      window.location.reload();
      // Never resolves -- the reload is already under way and rendering anything would flash.
      return new Promise(() => {});
    }

    console.error(`Failed to load page chunk for ${item.path}`, err);
    return AccessDeniedPage;
  }
}

// svelte-spa-router route table. The empty/root hash redirects to the overview entry point.
export const routes = {};

for (const item of NAV) {
  routes[item.path] = wrap({
    // The overview is bundled eagerly; every other entry resolves its chunk on first navigation.
    ...(item.component
      ? { component: item.component }
      : { asyncComponent: () => loadPage(item), loadingComponent: RouteLoading }),
    conditions: [canSee(item.caps)],
    userData: { route: item.path },
  });
}

// Root hash normalises to the overview entry point (App replaces '/' with '/overview' on boot, but
// guarding it here keeps the redirect honest if a user lands on '#/' directly).
routes['/'] = wrap({
  component: OverviewPage,
  conditions: [canSee(ROUTE_PERMISSIONS.overview)],
  userData: { route: '/overview' },
});
routes['/access-denied'] = AccessDeniedPage;
routes['*'] = AccessDeniedPage;
