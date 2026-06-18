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
import RoomsPage from '../pages/RoomsPage.svelte';
import PacketsPage from '../pages/PacketsPage.svelte';
import IncidentsPage from '../pages/IncidentsPage.svelte';
import AuditPage from '../pages/AuditPage.svelte';
import ModerationPage from '../pages/ModerationPage.svelte';
import OperationsPage from '../pages/OperationsPage.svelte';
import AccessDeniedPage from '../pages/AccessDeniedPage.svelte';
import ApiExplorerPage from '../pages/ApiExplorerPage.svelte';

// Display + permission metadata for the navigation sidebar. Order is the nav order.
export const NAV = [
  { path: '/overview', label: 'Overview', short: 'Live health', caps: ROUTE_PERMISSIONS.overview, component: OverviewPage },
  { path: '/infrastructure', label: 'Infrastructure', short: 'Runtime and Orleans', caps: ROUTE_PERMISSIONS.infrastructure, component: InfrastructurePage },
  { path: '/investigation', label: 'Investigation', short: 'Players and items', caps: ROUTE_PERMISSIONS.investigation, component: InvestigationPage },
  { path: '/economy', label: 'Economy', short: 'Ledger', caps: ROUTE_PERMISSIONS.economy, component: EconomyPage },
  { path: '/rooms', label: 'Room inspector', short: 'Room timeline', caps: ROUTE_PERMISSIONS.rooms, component: RoomsPage },
  { path: '/packets', label: 'Packet center', short: 'Traffic', caps: ROUTE_PERMISSIONS.packets, component: PacketsPage },
  { path: '/incidents', label: 'Incident center', short: 'Signals', caps: ROUTE_PERMISSIONS.incidents, component: IncidentsPage },
  { path: '/audit', label: 'Audit feed', short: 'Security', caps: ROUTE_PERMISSIONS.audit, component: AuditPage },
  { path: '/moderation', label: 'Moderation', short: 'Stats and trends', caps: ROUTE_PERMISSIONS.moderation, component: ModerationPage },
  { path: '/operations', label: 'Operations', short: 'Admin actions', caps: ROUTE_PERMISSIONS.operations, component: OperationsPage },
  { path: '/api-explorer', label: 'API explorer', short: 'Routes and contract', caps: ROUTE_PERMISSIONS.apiExplorer, component: ApiExplorerPage },
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
