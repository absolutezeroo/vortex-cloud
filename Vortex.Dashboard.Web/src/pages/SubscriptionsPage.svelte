<script>
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatDate, formatNumber } from '../lib/format.js';
  import EntityLink from '../components/EntityLink.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Sparkles, Timer, ShoppingBag } from '@lucide/svelte';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer, openItem } from '../lib/session.js';
  import { t, translate } from '../lib/i18n.js';

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
        clubError = translate('subscriptions.accessDenied');
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
    <h2>{$t('subscriptions.title')}</h2>
    <button type="button" on:click={refresh}>{$t('common.refresh')}</button>
  </div>
  <p class="muted">{$t('subscriptions.description')}</p>

  {#if clubError}
    <p class="empty-state danger">{clubError}</p>
  {/if}
</section>

<section class="split-grid" style="margin-top: 12px;">
  <section class="panel">
    <div class="panel-head">
      <h2>{$t('subscriptions.subsTitle')}</h2>
    </div>

    {#if clubError}
      <p class="empty-state danger">{clubError}</p>
    {:else if !clubStats}
      <p class="empty-state">{$t('subscriptions.noSubData')}</p>
    {:else}
      <div class="split-grid">
        <div class="chart-wrap">
          <div
            class="donut"
            style={`background: conic-gradient(var(--ok) 0% ${Math.min(100, activeRate * 100)}%, var(--line) ${Math.min(100, activeRate * 100)}% 100%);`}
          ></div>
          <div>
            <p class="muted">{$t('subscriptions.activeRate')}</p>
            <strong>{formatNumber(activeRate * 100, 2)}%</strong>
            <small class="muted">
              {$t('subscriptions.activeOfTotal', { active: clubStats?.totals?.activeSubscriptions || 0, total: clubStats?.totals?.totalSubscriptions || 0 })}
            </small>
          </div>
        </div>

        <div class="metric-grid compact">
          <StatCard label={$t('subscriptions.totalSubs')} value={clubStats?.totals?.totalSubscriptions || 0} sub={$t('subscriptions.inDatabase')} accent>
            <Sparkles slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
          <StatCard label={$t('subscriptions.expiring7d')} value={clubStats?.totals?.expiringIn7Days || 0} sub={$t('subscriptions.priorityRenewal')} accent>
            <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
          <StatCard label={$t('subscriptions.expiring30d')} value={clubStats?.totals?.expiringIn30Days || 0} sub={$t('subscriptions.window30d')} accent>
            <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
          </StatCard>
          <StatCard label={$t('subscriptions.renewalRate')} sub={$t('subscriptions.actionsCount', { renewals: clubStats?.lifecycle?.totals?.renewals || 0, purchases: clubStats?.lifecycle?.totals?.purchases || 0 })} accent>
            <Sparkles slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
            <span slot="value">{formatNumber((clubStats?.lifecycle?.totals?.renewalShare || 0) * 100, 2)}%</span>
          </StatCard>
        </div>
      </div>

      <p class="eyebrow" style="margin: 10px 0;">{$t('subscriptions.byType')}</p>
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
              >{$t('subscriptions.activeOfTotalDays', { active: row.active, total: row.total, days: formatNumber(row.averageRemainingDays, 2) })}</span>
          </div>
        {:else}
          <p class="muted">{$t('subscriptions.noTypeBreakdown')}</p>
        {/each}
      </div>
    {/if}
  </section>

  <section class="panel">
    <div class="panel-head">
      <h2>{$t('subscriptions.lifecycleTitle')}</h2>
    </div>

    {#if clubError}
      <p class="empty-state danger">{clubError}</p>
    {:else if !clubStats}
      <p class="empty-state">{$t('subscriptions.noLifecycleData')}</p>
    {:else}
      <div class="metric-grid compact">
        <StatCard label={$t('subscriptions.purchases')} value={clubStats?.lifecycle?.totals?.purchases || 0} sub={$t('subscriptions.newSubs')} accent>
          <ShoppingBag slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
        </StatCard>
        <StatCard label={$t('subscriptions.renewals')} value={clubStats?.lifecycle?.totals?.renewals || 0} sub={$t('subscriptions.topUps')} accent>
          <Sparkles slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
        </StatCard>
        <StatCard label={$t('subscriptions.expirations')} value={clubStats?.lifecycle?.totals?.expired || 0} sub={$t('subscriptions.churn')} accent>
          <Timer slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
        </StatCard>
      </div>

      <p class="eyebrow" style="margin: 10px 0;">
        {$t('subscriptions.window', { since: clubStats?.window?.since || '-', until: clubStats?.window?.until || '-' })}
      </p>

      <div class="split-grid">
        <div class="bar-chart">
          <h3>{$t('subscriptions.purchases')}</h3>
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
            <p class="muted">{$t('subscriptions.noPoints')}</p>
          {/each}
        </div>

        <div class="bar-chart">
          <h3>{$t('subscriptions.renewals')}</h3>
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
            <p class="muted">{$t('subscriptions.noPoints')}</p>
          {/each}
        </div>
      </div>

      <div class="bar-chart" style="margin-top: 10px;">
        <h3>{$t('subscriptions.expirations')}</h3>
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
            <p class="muted">{$t('subscriptions.noPoints')}</p>
          {/each}
      </div>

      <p class="eyebrow" style="margin: 10px 0;">{$t('subscriptions.subscriptionDuration')}</p>
      <div class="bar-chart">
        {#each byMonths as row}
          <div class="bar-row">
            <div class="bar-label">
              {row.months ? `${row.months} ${$t('subscriptions.months')}` : $t('subscriptions.unknown')}
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
          <p class="muted">{$t('subscriptions.noMonthBreakdown')}</p>
        {/each}
      </div>

      <div class="panel" style="margin-top: 14px;">
        <h3>{$t('subscriptions.recentEvents')}</h3>
        <table>
          <thead>
            <tr>
              <th>{$t('subscriptions.colWhen')}</th>
              <th>{$t('subscriptions.colAction')}</th>
              <th>{$t('subscriptions.colActor')}</th>
              <th>{$t('subscriptions.colMonths')}</th>
              <th>{$t('subscriptions.colTotalMonths')}</th>
              <th>{$t('subscriptions.colRenewal')}</th>
              <th>{$t('subscriptions.colVip')}</th>
              <th>{$t('subscriptions.colCredit')}</th>
            </tr>
          </thead>
          <tbody>
            {#each recentEvents as row}
              <tr>
                <td>{formatDate(row.occurredAt)}</td>
                <td>{row.action}</td>
                <td>{row.actorPlayerName || `#${row.actorPlayerId}`}</td>
                <td>{row.months || '-'}</td>
                <td>{row.totalMonths || '-'}</td>
                <td>{row.isRenewal === true ? $t('subscriptions.yes') : row.isRenewal === false ? $t('subscriptions.no') : '-'}</td>
                <td>{row.isVip === true ? $t('subscriptions.yes') : row.isVip === false ? $t('subscriptions.no') : '-'}</td>
                <td>{row.creditCost || '-'}</td>
              </tr>
            {:else}
              <tr>
                <td colspan="8" class="muted">{$t('subscriptions.noRecentFlow')}</td>
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
    <h2>{$t('subscriptions.upcomingExpirations')}</h2>
  </div>

  {#if !clubStats || topExpiring.length === 0}
    <p class="muted">{$t('subscriptions.noUpcoming')}</p>
  {:else}
    <table>
      <thead>
        <tr>
          <th>{$t('subscriptions.colPlayer')}</th>
          <th>{$t('subscriptions.colType')}</th>
          <th>{$t('subscriptions.colLevel')}</th>
          <th>{$t('subscriptions.colExpires')}</th>
          <th>{$t('subscriptions.colDaysRemaining')}</th>
          <th>{$t('subscriptions.colTotalMonths')}</th>
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
            <td colspan="6" class="muted">{$t('subscriptions.noDataToRefresh')}</td>
          </tr>
        {/each}
      </tbody>
    </table>
  {/if}
</section>
