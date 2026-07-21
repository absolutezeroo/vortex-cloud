// Central route table for the dashboard SPA. Each section declares the capability (or capabilities)
// required to view it; svelte-spa-router enforces it through a `conditions` guard and the nav uses
// the same rule so operators only ever see authorized tools. A failed guard raises the router's
// `conditionsFailed` event, which App.svelte turns into the /access-denied view.

import { wrap } from 'svelte-spa-router/wrap';
import { get } from 'svelte/store';
import { identity } from './session.js';
import { hasDashboardCapability } from './permissions.js';
import { ROUTE_PERMISSIONS } from './dashboardPermissions.js';

import OverviewPage from '../pages/OverviewPage.svelte';
import InfrastructurePage from '../pages/InfrastructurePage.svelte';
import InvestigationPage from '../pages/InvestigationPage.svelte';
import EconomyPage from '../pages/EconomyPage.svelte';
import EconomyTrendsPage from '../pages/EconomyTrendsPage.svelte';
import MarketplacePage from '../pages/MarketplacePage.svelte';
import SubscriptionsPage from '../pages/SubscriptionsPage.svelte';
import RoomsPage from '../pages/RoomsPage.svelte';
import PacketsPage from '../pages/PacketsPage.svelte';
import IncidentsPage from '../pages/IncidentsPage.svelte';
import AuditPage from '../pages/AuditPage.svelte';
import ModerationPage from '../pages/ModerationPage.svelte';
import ModerationActionsPage from '../pages/ModerationActionsPage.svelte';
import CfhQueuePage from '../pages/CfhQueuePage.svelte';
import RoomControlPage from '../pages/RoomControlPage.svelte';
import VouchersPage from '../pages/VouchersPage.svelte';
import CatalogPage from '../pages/CatalogPage.svelte';
import FurnitureDefinitionsPage from '../pages/FurnitureDefinitionsPage.svelte';
import OperationsPage from '../pages/OperationsPage.svelte';
import AccessDeniedPage from '../pages/AccessDeniedPage.svelte';
import ApiExplorerPage from '../pages/ApiExplorerPage.svelte';
import GroupsStatsPage from '../pages/GroupsStatsPage.svelte';
import PetsStatsPage from '../pages/PetsStatsPage.svelte';
import CfhStatsPage from '../pages/CfhStatsPage.svelte';
import CatalogPurchasesStatsPage from '../pages/CatalogPurchasesStatsPage.svelte';
import WiredStatsPage from '../pages/WiredStatsPage.svelte';
import TargetedOffersPage from '../pages/TargetedOffersPage.svelte';
import TargetedOffersStatsPage from '../pages/TargetedOffersStatsPage.svelte';
import QuestsPage from '../pages/QuestsPage.svelte';
import QuestsStatsPage from '../pages/QuestsStatsPage.svelte';

// Display + permission metadata for the navigation sidebar. Order is the nav order within each
// group. `group` buckets items in the sidebar (see AppShell.svelte) — Live: auto-refreshing health
// signals; Investigate: lookup/forensics tools; Stats: read-only cross-domain analytics (no
// per-record lookup, no mutation); Act: pages that can change server state; Dev: raw API access.
// Keep this in sync with GROUP_ORDER in AppShell.svelte.
// label/short are i18n keys (resolved via $t in AppShell.svelte), not display strings -- see
// lib/locales/{en,fr}.js's `nav` namespace, which must have a matching entry for every key here.
export const NAV = [
  { path: '/overview', labelKey: 'nav.overview', shortKey: 'nav.overviewShort', group: 'Live', caps: ROUTE_PERMISSIONS.overview, component: OverviewPage },
  { path: '/infrastructure', labelKey: 'nav.infrastructure', shortKey: 'nav.infrastructureShort', group: 'Live', caps: ROUTE_PERMISSIONS.infrastructure, component: InfrastructurePage },
  { path: '/packets', labelKey: 'nav.packets', shortKey: 'nav.packetsShort', group: 'Live', caps: ROUTE_PERMISSIONS.packets, component: PacketsPage },
  { path: '/incidents', labelKey: 'nav.incidents', shortKey: 'nav.incidentsShort', group: 'Live', caps: ROUTE_PERMISSIONS.incidents, component: IncidentsPage },
  { path: '/investigation', labelKey: 'nav.investigation', shortKey: 'nav.investigationShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.investigation, component: InvestigationPage },
  { path: '/rooms', labelKey: 'nav.rooms', shortKey: 'nav.roomsShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.rooms, component: RoomsPage },
  { path: '/audit', labelKey: 'nav.audit', shortKey: 'nav.auditShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.audit, component: AuditPage },
  { path: '/moderation', labelKey: 'nav.moderation', shortKey: 'nav.moderationShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.moderation, component: ModerationPage },
  { path: '/economy', labelKey: 'nav.economy', shortKey: 'nav.economyShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: EconomyPage },
  { path: '/economy-trends', labelKey: 'nav.economyTrends', shortKey: 'nav.economyTrendsShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: EconomyTrendsPage },
  { path: '/marketplace', labelKey: 'nav.marketplace', shortKey: 'nav.marketplaceShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: MarketplacePage },
  { path: '/subscriptions', labelKey: 'nav.subscriptions', shortKey: 'nav.subscriptionsShort', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: SubscriptionsPage },
  { path: '/groups-stats', labelKey: 'nav.groupsStats', shortKey: 'nav.groupsStatsShort', group: 'Stats', caps: ROUTE_PERMISSIONS.groupsStats, component: GroupsStatsPage },
  { path: '/pets-stats', labelKey: 'nav.petsStats', shortKey: 'nav.petsStatsShort', group: 'Stats', caps: ROUTE_PERMISSIONS.petsStats, component: PetsStatsPage },
  { path: '/cfh-stats', labelKey: 'nav.cfhStats', shortKey: 'nav.cfhStatsShort', group: 'Stats', caps: ROUTE_PERMISSIONS.cfhStats, component: CfhStatsPage },
  { path: '/catalog-purchases', labelKey: 'nav.catalogPurchases', shortKey: 'nav.catalogPurchasesShort', group: 'Stats', caps: ROUTE_PERMISSIONS.catalogPurchases, component: CatalogPurchasesStatsPage },
  { path: '/targeted-offers-stats', labelKey: 'nav.targetedOffersStats', shortKey: 'nav.targetedOffersStatsShort', group: 'Stats', caps: ROUTE_PERMISSIONS.targetedOffersStats, component: TargetedOffersStatsPage },
  { path: '/quests-stats', labelKey: 'nav.questsStats', shortKey: 'nav.questsStatsShort', group: 'Stats', caps: ROUTE_PERMISSIONS.questsStats, component: QuestsStatsPage },
  { path: '/wired-stats', labelKey: 'nav.wiredStats', shortKey: 'nav.wiredStatsShort', group: 'Stats', caps: ROUTE_PERMISSIONS.wiredStats, component: WiredStatsPage },
  { path: '/operations', labelKey: 'nav.operations', shortKey: 'nav.operationsShort', group: 'Act', caps: ROUTE_PERMISSIONS.operations, component: OperationsPage },
  { path: '/moderation-actions', labelKey: 'nav.moderationActions', shortKey: 'nav.moderationActionsShort', group: 'Act', caps: ROUTE_PERMISSIONS.moderationActions, component: ModerationActionsPage },
  { path: '/cfh', labelKey: 'nav.cfh', shortKey: 'nav.cfhShort', group: 'Act', caps: ROUTE_PERMISSIONS.cfh, component: CfhQueuePage },
  { path: '/room-control', labelKey: 'nav.roomControl', shortKey: 'nav.roomControlShort', group: 'Act', caps: ROUTE_PERMISSIONS.roomControl, component: RoomControlPage },
  { path: '/vouchers', labelKey: 'nav.vouchers', shortKey: 'nav.vouchersShort', group: 'Act', caps: ROUTE_PERMISSIONS.vouchers, component: VouchersPage },
  { path: '/catalog', labelKey: 'nav.catalog', shortKey: 'nav.catalogShort', group: 'Act', caps: ROUTE_PERMISSIONS.catalog, component: CatalogPage },
  { path: '/targeted-offers', labelKey: 'nav.targetedOffers', shortKey: 'nav.targetedOffersShort', group: 'Act', caps: ROUTE_PERMISSIONS.targetedOffers, component: TargetedOffersPage },
  { path: '/quests', labelKey: 'nav.quests', shortKey: 'nav.questsShort', group: 'Act', caps: ROUTE_PERMISSIONS.quests, component: QuestsPage },
  { path: '/furniture-definitions', labelKey: 'nav.furnitureDefinitions', shortKey: 'nav.furnitureDefinitionsShort', group: 'Act', caps: ROUTE_PERMISSIONS.furnitureDefinitions, component: FurnitureDefinitionsPage },
  { path: '/api-explorer', labelKey: 'nav.apiExplorer', shortKey: 'nav.apiExplorerShort', group: 'Dev', caps: ROUTE_PERMISSIONS.apiExplorer, component: ApiExplorerPage },
];

const canSee = (caps) => () => hasDashboardCapability(get(identity), caps);

// Pass the identity explicitly (e.g. from a reactive `$identity`) to recompute on changes; falls
// back to the current store value for non-reactive callers such as the router guards.
export function hasRouteAccess(item, who = get(identity)) {
  return hasDashboardCapability(who, item.caps);
}

// svelte-spa-router route table. The empty/root hash redirects to the overview entry point.
export const routes = {};

for (const item of NAV) {
  routes[item.path] = wrap({
    component: item.component,
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
