<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate } from '../lib/format.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';

  let data = null;
  let error = '';
  let forbidden = false;
  let loading = true;

  let search = '';
  let domainFilter = 'all';
  let methodFilter = 'all';

  let copiedPath = '';

  $: routes = data?.routes || [];
  $: groups = data?.groups || [];
  $: methodUsage = data?.methodUsage || [];
  $: domains = ['all', ...new Set(groups.map((group) => group.domain).sort())];
  $: methods = ['all', ...new Set(methodUsage.map((item) => item.method).sort())];

  $: normalizedSearch = String(search || '').toLowerCase().trim();
  $: normalizedDomain = String(domainFilter || 'all').toLowerCase();
  $: normalizedMethod = String(methodFilter || 'all').toLowerCase();

  // normalizedSearch/normalizedDomain/normalizedMethod must appear as literal identifiers in this
  // statement -- Svelte's reactive dependency tracking for `$:` only scans the statement's own
  // expression text, it doesn't trace into called functions' bodies. Reading them only inside
  // matchesSearch/matchesDomain/matchesMethod (as this used to) makes `filtered` invisible to
  // changes in the search box or the domain/method selects: it silently never re-ran after the
  // first load, so every filter control on this page looked wired up but did nothing.
  $: filtered = routes.filter(
    (route) =>
      matchesSearch(route, normalizedSearch) &&
      matchesDomain(route, normalizedDomain) &&
      matchesMethod(route, normalizedMethod)
  );

  $: groupedByDomain = groupBy(filtered, (route) => route.domain || 'misc');
  $: maxDomain = Math.max(1, ...groups.map((group) => group.routeCount || 0));
  $: maxMethod = Math.max(1, ...methodUsage.map((entry) => entry.count || 0));

  function matchesSearch(route, normalizedSearchValue) {
    if (!normalizedSearchValue) return true;

    const payload = [
      route.domain || '',
      route.path || '',
      route.displayName || '',
      ...(route.capabilities || []),
      ...(route.tags || []),
    ]
      .join(' ')
      .toLowerCase();

    return payload.includes(normalizedSearchValue);
  }

  function matchesDomain(route, normalizedDomainValue) {
    return (
      normalizedDomainValue === 'all' ||
      (route.domain || 'legacy').toLowerCase() === normalizedDomainValue
    );
  }

  function matchesMethod(route, normalizedMethodValue) {
    return (
      normalizedMethodValue === 'all' ||
      route.methods.some((method) => (method || '').toLowerCase() === normalizedMethodValue)
    );
  }

  function selectDomain(domain) {
    domainFilter = domain;
  }

  function methodClass(method) {
    const normalized = String(method || '').toUpperCase();

    if (normalized === 'GET') return 'method-badge method-badge--get';
    if (normalized === 'POST') return 'method-badge method-badge--post';
    if (normalized === 'DELETE') return 'method-badge method-badge--delete';
    if (normalized === 'PUT') return 'method-badge method-badge--put';
    return 'method-badge';
  }

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/meta/endpoints');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        error = '';
      } else {
        error = err.message;
      }
    } finally {
      loading = false;
    }
  }

  async function copyCurl(path, method) {
    const command = `curl -X ${method} '${window.location.origin}${path}'`;

    if (navigator?.clipboard?.writeText) {
      await navigator.clipboard.writeText(command);
      copiedPath = path;

      setTimeout(() => {
        if (copiedPath === path) copiedPath = '';
      }, 1200);
    }
  }

  function groupBy(values, selector) {
    const output = new Map();

    for (const value of values) {
      const key = selector(value);
      const list = output.get(key);

      if (list) {
        list.push(value);
      } else {
        output.set(key, [value]);
      }
    }

    return [...output.entries()].sort((a, b) => a[0].localeCompare(b[0]));
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>API explorer</h2>
    <button type="button" on:click={refresh} disabled={loading}>
      {loading ? 'Actualisation…' : 'Actualiser'}
    </button>
  </div>

  <p class="muted">
    Endpoints discoverables depuis <strong>/api/v1/meta/endpoints</strong>, avec
    domaines, méthodes, droits applicables et exposition legacy/v1.
  </p>

  <div class="split-grid" style="margin-top: 12px;">
    <article>
      <span>Génération</span>
      <strong>{formatDate(data?.generatedAt)}</strong>
      <small class="muted">heure UTC de la collecte</small>
    </article>
    <article>
      <span>Total routes</span>
      <strong>{routes.length}</strong>
      <small class="muted">points d'entrée opérationnels</small>
    </article>
  </div>

  <div class="toolbar" style="margin-top: 12px;">
    <input bind:value={search} placeholder="Filtrer par path, domaine, tag, capability..." />
    <select bind:value={domainFilter}>
      {#each domains as value}
        <option value={value}>{value}</option>
      {/each}
    </select>
    <select bind:value={methodFilter}>
      {#each methods as value}
        <option value={value}>{value}</option>
      {/each}
    </select>
    <a href="/swagger" class="ghost-button" target="_blank" rel="noreferrer">Swagger</a>
  </div>

  {#if forbidden}
    <AccessDeniedNotice message="Vous n'avez pas l'autorisation d'accéder à l'explorateur d'API." />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {:else if filtered.length === 0}
    <p class="empty-state">Aucun endpoint ne correspond aux filtres.</p>
  {/if}

  <div class="split-grid" style="margin-top: 12px; align-items: start;">
    <article class="panel">
      <h3>Répartition par domaine</h3>
      <div class="bar-chart">
        {#each groups as entry}
          <div class="bar-row">
            <div class="bar-label">{entry.domain}</div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`width: ${maxDomain > 0 ? (entry.routeCount / maxDomain) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted">{entry.routeCount}</span>
          </div>
        {:else}
          <p class="muted">Aucun domaine détecté.</p>
        {/each}
      </div>
    </article>

    <article class="panel">
      <h3>Répartition par méthode</h3>
      <div class="bar-chart">
        {#each methodUsage as entry}
          <div class="bar-row">
            <div class="bar-label">
              <span class={methodClass(entry.method)}>{entry.method}</span>
            </div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`width: ${maxMethod > 0 ? (entry.count / maxMethod) * 100 : 0}%; background: linear-gradient(90deg, rgba(var(--ok-rgb), 0.95), rgba(var(--ok-rgb), 0.55));`}
              ></div>
            </div>
            <span class="muted">{entry.count}</span>
          </div>
        {:else}
          <p class="muted">Aucune méthode détectée.</p>
        {/each}
      </div>
    </article>
  </div>
</section>

<section class="panel" style="margin-top: 12px;">
  <div class="panel-head">
    <h3>Routes ({filtered.length})</h3>
    {#if domainFilter !== 'all'}
      <button type="button" class="ghost-button" on:click={() => selectDomain('all')}>
        ✕ {domainFilter}
      </button>
    {/if}
  </div>
  <p class="eyebrow" style="margin: 4px 0 10px;">
    La première ligne d'une route est celle recommandée pour automatisation / client. Cliquer un
    domaine ci-dessous filtre la liste sur ce domaine.
  </p>

  <div class="domain-quicknav">
    {#each groups as g}
      <button
        type="button"
        class="domain-pill"
        class:active={normalizedDomain === g.domain.toLowerCase()}
        on:click={() => selectDomain(g.domain)}
      >
        {g.domain} <small>{g.routeCount}</small>
      </button>
    {/each}
  </div>

  {#each groupedByDomain as [domain, domainRoutes] (domain)}
    <h4 class="domain-heading">{domain} <span class="muted">({domainRoutes.length})</span></h4>
    <div class="table-wrap">
      <table>
        <thead>
          <tr>
            <th>Path</th>
            <th>Méthodes</th>
            <th>Auth</th>
            <th>Capabilities</th>
            <th>Tags</th>
            <th>Legacy</th>
            <th>Commande</th>
          </tr>
        </thead>
        <tbody>
          {#each domainRoutes as route}
            <tr>
              <td><code>{route.path}</code></td>
              <td>
                {#each route.methods as method}
                  <span class={methodClass(method)}>{method}</span>
                {/each}
              </td>
              <td>{route.requiresAuth ? 'oui' : 'non'}</td>
              <td class="truncate" title={route.capabilities?.join(', ')}>
                {#if route.capabilities?.length}
                  {route.capabilities.join(', ')}
                {:else}
                  -
                {/if}
              </td>
              <td>{route.tags?.join(', ') || '-'}</td>
              <td>{route.isLegacy ? 'legacy' : 'v1'}</td>
              <td>
                <button
                  type="button"
                  class="ghost-button"
                  on:click={() => copyCurl(route.path, route.methods[0] || 'GET')}
                >
                  {copiedPath === route.path ? 'Copié' : 'Copier'}
                </button>
              </td>
            </tr>
          {/each}
        </tbody>
      </table>
    </div>
  {:else}
    <p class="empty-state">Aucune route après filtrage.</p>
  {/each}
</section>

<style>
  .domain-quicknav {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin: 10px 0 16px;
  }

  .domain-pill {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    border: 1px solid var(--line-strong);
    border-radius: 999px;
    background: var(--surface-strong);
    color: var(--muted-strong);
    padding: 6px 12px;
    font-size: 0.8rem;
    font-weight: 700;
  }

  .domain-pill:hover {
    border-color: rgba(var(--accent-rgb), 0.5);
    color: var(--ink);
  }

  .domain-pill.active {
    border-color: rgba(var(--accent-rgb), 0.58);
    background: var(--accent-soft);
    color: var(--ink);
  }

  .domain-pill small {
    color: var(--muted);
    font-weight: 700;
  }

  .domain-pill.active small {
    color: var(--accent-strong);
  }

  .domain-heading {
    margin: 18px 0 8px;
    padding-top: 12px;
    border-top: 1px solid var(--line);
    color: var(--ink);
    font-size: 0.92rem;
  }

  .domain-heading:first-of-type {
    margin-top: 4px;
    padding-top: 0;
    border-top: 0;
  }

  .method-badge {
    border: 1px solid var(--line-strong);
    border-radius: 999px;
    padding: 0.12rem 0.55rem;
    margin-right: 0.3rem;
    font-size: 0.72rem;
    font-weight: 700;
    letter-spacing: 0.01em;
  }

  .method-badge--get {
    border-color: rgba(var(--ok-rgb), 0.5);
    color: var(--ok);
    background: var(--success-bg);
  }

  .method-badge--post {
    border-color: rgba(var(--accent-rgb), 0.5);
    color: var(--accent);
    background: var(--accent-soft);
  }

  .method-badge--delete {
    border-color: rgba(var(--danger-rgb), 0.5);
    color: var(--danger);
    background: var(--danger-bg);
  }

  .method-badge--put {
    border-color: rgba(var(--warning-rgb), 0.5);
    color: var(--warning);
    background: var(--warning-bg);
  }
</style>
