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

// Display + permission metadata for the navigation sidebar. Order is the nav order within each
// group. `group` buckets items in the sidebar (see AppShell.svelte) — Live: auto-refreshing health
// signals; Investigate: lookup/forensics tools; Act: pages that can change server state; Dev: raw
// API access. Keep this in sync with GROUP_ORDER in AppShell.svelte.
export const NAV = [
  { path: '/overview', label: 'Overview', short: 'Live health', group: 'Live', caps: ROUTE_PERMISSIONS.overview, component: OverviewPage },
  { path: '/infrastructure', label: 'Infrastructure', short: 'Runtime and Orleans', group: 'Live', caps: ROUTE_PERMISSIONS.infrastructure, component: InfrastructurePage },
  { path: '/packets', label: 'Packet center', short: 'Traffic', group: 'Live', caps: ROUTE_PERMISSIONS.packets, component: PacketsPage },
  { path: '/incidents', label: 'Incident center', short: 'Signals', group: 'Live', caps: ROUTE_PERMISSIONS.incidents, component: IncidentsPage },
  { path: '/investigation', label: 'Investigation', short: 'Players and items', group: 'Investigate', caps: ROUTE_PERMISSIONS.investigation, component: InvestigationPage },
  { path: '/rooms', label: 'Room inspector', short: 'Room timeline', group: 'Investigate', caps: ROUTE_PERMISSIONS.rooms, component: RoomsPage },
  { path: '/audit', label: 'Audit feed', short: 'Security', group: 'Investigate', caps: ROUTE_PERMISSIONS.audit, component: AuditPage },
  { path: '/moderation', label: 'Moderation stats', short: 'Stats and trends', group: 'Investigate', caps: ROUTE_PERMISSIONS.moderation, component: ModerationPage },
  { path: '/economy', label: 'Economy ledger', short: 'Raw wallet log', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: EconomyPage },
  { path: '/economy-trends', label: 'Spend trends', short: 'Per-currency, day/month/year', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: EconomyTrendsPage },
  { path: '/marketplace', label: 'Marketplace', short: 'Player sales', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: MarketplacePage },
  { path: '/subscriptions', label: 'Subscriptions', short: 'HC / Builders Club', group: 'Investigate', caps: ROUTE_PERMISSIONS.economy, component: SubscriptionsPage },
  { path: '/operations', label: 'Operations', short: 'Admin actions', group: 'Act', caps: ROUTE_PERMISSIONS.operations, component: OperationsPage },
  { path: '/moderation-actions', label: 'Moderation actions', short: 'Ban, mute, trading lock', group: 'Act', caps: ROUTE_PERMISSIONS.moderationActions, component: ModerationActionsPage },
  { path: '/cfh', label: 'CFH queue', short: 'Tickets', group: 'Act', caps: ROUTE_PERMISSIONS.cfh, component: CfhQueuePage },
  { path: '/room-control', label: 'Room control', short: 'Active rooms', group: 'Act', caps: ROUTE_PERMISSIONS.roomControl, component: RoomControlPage },
  { path: '/vouchers', label: 'Vouchers', short: 'Redeemable codes', group: 'Act', caps: ROUTE_PERMISSIONS.vouchers, component: VouchersPage },
  { path: '/catalog', label: 'Catalog', short: 'Pages, offers, products', group: 'Act', caps: ROUTE_PERMISSIONS.catalog, component: CatalogPage },
  { path: '/furniture-definitions', label: 'Furniture definitions', short: 'Sprites and physics', group: 'Act', caps: ROUTE_PERMISSIONS.furnitureDefinitions, component: FurnitureDefinitionsPage },
  { path: '/api-explorer', label: 'API explorer', short: 'Routes and contract', group: 'Dev', caps: ROUTE_PERMISSIONS.apiExplorer, component: ApiExplorerPage },
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
