<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';

  let clubStats = null;
  let clubError = '';

  $: byType = clubStats?.byType || [];
  $: lifecycle = clubStats?.lifecycle?.timeline || [];
  $: byMonths = clubStats?.lifecycle?.byMonths || [];
  $: recentEvents = clubStats?.lifecycle?.recentEvents || [];
  $: topExpiring = clubStats?.topExpiring || [];
  $: byTypeScale = Math.max(1, ...byType.map((row) => Number(row.total || 0)));
  $: lifecycleScale = Math.max(
    1,
    ...lifecycle.map((point) => Math.max(point.purchases || 0, point.renewals || 0, point.expired || 0)),
  );
  $: byMonthsScale = Math.max(
    1,
    ...byMonths.map((point) => Math.max(point.total || 0, point.purchases || 0, point.renewals || 0)),
  );
  $: activeRate = Number(clubStats?.totals?.activeRate || 0);

  async function refresh() {
    clubError = '';

    try {
      clubStats = await apiGet('/api/v1/economy/subscriptions');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        clubError = 'Vous n\'avez pas l\'autorisation d\'accéder aux métriques abonnements.';
      } else {
        clubError = err.message;
      }

      clubStats = null;
    }
  }

  onMount(refresh);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>Subscriptions</h2>
    <button type="button" on:click={refresh}>Refresh</button>
  </div>
  <p class="muted">HC/Builders Club subscription lifecycle: active rate, renewals, expirations.</p>

  {#if clubError}
    <p class="empty-state danger">{clubError}</p>
  {/if}
</section>

<section class="split-grid" style="margin-top: 12px;">
  <section class="panel">
    <div class="panel-head">
      <h2>Abonnements</h2>
    </div>

    {#if clubError}
      <p class="empty-state danger">{clubError}</p>
    {:else if !clubStats}
      <p class="empty-state">Aucune donnée abonnement pour le moment.</p>
    {:else}
      <div class="split-grid">
        <div class="chart-wrap">
          <div
            class="donut"
            style={`background: conic-gradient(var(--ok) 0% ${Math.min(100, activeRate * 100)}%, var(--line) ${Math.min(100, activeRate * 100)}% 100%);`}
          ></div>
          <div>
            <p class="muted">Taux d'abonnements actifs</p>
            <strong>{formatNumber(activeRate * 100, 2)}%</strong>
            <small class="muted">
              {clubStats?.totals?.activeSubscriptions || 0} actifs / {clubStats?.totals?.totalSubscriptions || 0} total
            </small>
          </div>
        </div>

        <div class="metric-grid compact">
          <article>
            <span>Total abonnements</span>
            <strong>{clubStats?.totals?.totalSubscriptions || 0}</strong>
            <small>en base</small>
          </article>
          <article>
            <span>Expirent sous 7j</span>
            <strong>{clubStats?.totals?.expiringIn7Days || 0}</strong>
            <small>renouvellement prioritaire</small>
          </article>
          <article>
            <span>Expirent sous 30j</span>
            <strong>{clubStats?.totals?.expiringIn30Days || 0}</strong>
            <small>fenêtre 30 jours</small>
          </article>
          <article>
            <span>Taux de renouvellement</span>
            <strong>{formatNumber((clubStats?.lifecycle?.totals?.renewalShare || 0) * 100, 2)}%</strong>
            <small>
              {clubStats?.lifecycle?.totals?.renewals || 0}/{clubStats?.lifecycle?.totals?.purchases || 0} actions
            </small>
          </article>
        </div>
      </div>

      <p class="eyebrow" style="margin: 10px 0;">Répartition par type</p>
      <div class="bar-chart">
        {#each byType as row}
          <div class="bar-row">
            <div class="bar-label">{row.type}</div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`width: ${byTypeScale > 0 ? (Number(row.total) / byTypeScale) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted"
              >{row.active}/{row.total} actifs • {formatNumber(row.averageRemainingDays, 2)} j restants</span>
          </div>
        {:else}
          <p class="muted">Aucune répartition par type pour l'instant.</p>
        {/each}
      </div>
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head">
      <h2>Lifecycle HC / BC</h2>
    </div>

    {#if clubError}
      <p class="empty-state danger">{clubError}</p>
    {:else if !clubStats}
      <p class="empty-state">Aucune donnée lifecycle pour l'instant.</p>
    {:else}
      <div class="metric-grid compact">
        <article>
          <span>Achats</span>
          <strong>{clubStats?.lifecycle?.totals?.purchases || 0}</strong>
          <small>abonnements nouveaux</small>
        </article>
        <article>
          <span>Renouvellements</span>
          <strong>{clubStats?.lifecycle?.totals?.renewals || 0}</strong>
          <small>recharges</small>
        </article>
        <article>
          <span>Expirations</span>
          <strong>{clubStats?.lifecycle?.totals?.expired || 0}</strong>
          <small>défections</small>
        </article>
      </div>

      <p class="eyebrow" style="margin: 10px 0;">
        Fenêtre : {clubStats?.window?.since || '-'} → {clubStats?.window?.until || '-'}
      </p>

      <div class="split-grid">
        <div class="bar-chart">
          <h3>Achats</h3>
          {#each lifecycle as point}
            <div class="bar-row">
              <div class="bar-label">{point.label}</div>
              <div class="bar-track">
                <div
                  class="bar-fill"
                  style={`background: linear-gradient(90deg, rgba(var(--accent-rgb), 0.95), rgba(var(--accent-rgb), 0.55)); width: ${lifecycleScale > 0 ? (point.purchases / lifecycleScale) * 100 : 0}%;`}
                ></div>
              </div>
              <span class="muted">{point.purchases}</span>
            </div>
          {:else}
            <p class="muted">Pas de points.</p>
          {/each}
        </div>

        <div class="bar-chart">
          <h3>Renouvellements</h3>
          {#each lifecycle as point}
            <div class="bar-row">
              <div class="bar-label">{point.label}</div>
              <div class="bar-track">
                <div
                  class="bar-fill"
                  style={`background: linear-gradient(90deg, rgba(var(--ok-rgb), 0.95), rgba(var(--ok-rgb), 0.55)); width: ${lifecycleScale > 0 ? (point.renewals / lifecycleScale) * 100 : 0}%;`}
                ></div>
              </div>
              <span class="muted">{point.renewals}</span>
            </div>
          {:else}
            <p class="muted">Pas de points.</p>
          {/each}
        </div>
      </div>

      <div class="bar-chart" style="margin-top: 10px;">
        <h3>Expirations</h3>
        {#each lifecycle as point}
          <div class="bar-row">
            <div class="bar-label">{point.label}</div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`background: linear-gradient(90deg, rgba(var(--danger-rgb), 0.95), rgba(var(--danger-rgb), 0.55)); width: ${lifecycleScale > 0 ? (point.expired / lifecycleScale) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted">{point.expired}</span>
          </div>
          {:else}
            <p class="muted">Pas de points.</p>
          {/each}
      </div>

      <p class="eyebrow" style="margin: 10px 0;">Durée d'abonnement (mois)</p>
      <div class="bar-chart">
        {#each byMonths as row}
          <div class="bar-row">
            <div class="bar-label">
              {row.months ? `${row.months} mois` : 'Inconnu'}
            </div>
            <div class="bar-track">
              <div
                class="bar-fill"
                style={`background: linear-gradient(90deg, rgba(173, 114, 227, 0.95), rgba(173, 114, 227, 0.55)); width: ${byMonthsScale > 0 ? (row.total / byMonthsScale) * 100 : 0}%;`}
              ></div>
            </div>
            <span class="muted">{row.total}</span>
          </div>
        {:else}
          <p class="muted">Aucune répartition par mois.</p>
        {/each}
      </div>

      <div class="panel" style="margin-top: 14px;">
        <h3>Récent: achats/renouvellements HC</h3>
        <table>
          <thead>
            <tr>
              <th>Quand</th>
              <th>Action</th>
              <th>Acteur</th>
              <th>Mois</th>
              <th>Total mois</th>
              <th>Renouvellement</th>
              <th>VIP</th>
              <th>Crédit</th>
            </tr>
          </thead>
          <tbody>
            {#each recentEvents as row}
              <tr>
                <td>{formatDate(row.occurredAt)}</td>
                <td>{row.action}</td>
                <td>{row.actorPlayerName || `joueur #${row.actorPlayerId}`}</td>
                <td>{row.months || '-'}</td>
                <td>{row.totalMonths || '-'}</td>
                <td>{row.isRenewal === true ? 'oui' : row.isRenewal === false ? 'non' : '-'}</td>
                <td>{row.isVip === true ? 'oui' : row.isVip === false ? 'non' : '-'}</td>
                <td>{row.creditCost || '-'}</td>
              </tr>
            {:else}
              <tr>
                <td colspan="8" class="muted">Pas de flux récent sur la fenêtre sélectionnée.</td>
              </tr>
            {/each}
          </tbody>
        </table>
      </div>
    {/if}
  </section>
</section>

<section class="panel" style="margin-top: 16px;">
  <div class="panel-head">
    <h2>Prochains expirations (14 jours)</h2>
  </div>

  {#if !clubStats || topExpiring.length === 0}
    <p class="muted">Aucun abonnement à renouveler prochainement.</p>
  {:else}
    <table>
      <thead>
        <tr>
          <th>Joueur</th>
          <th>Type</th>
          <th>Niveau</th>
          <th>Expire</th>
          <th>Jours restants</th>
          <th>Total mois</th>
        </tr>
      </thead>
      <tbody>
        {#each topExpiring as row}
          <tr>
            <td>
              <EntityLink id={row.playerId} label={row.playerName || `player #${row.playerId}`} {openPlayer} {openItem} />
            </td>
            <td>{row.type}</td>
            <td>{row.level}</td>
            <td>{formatDate(row.expiresAt)}</td>
            <td>{formatNumber(row.remainingDays, 1)}</td>
            <td>{row.totalMonths}</td>
          </tr>
        {:else}
          <tr>
            <td colspan="6" class="muted">No data to refresh.</td>
          </tr>
        {/each}
      </tbody>
    </table>
  {/if}
</section>
