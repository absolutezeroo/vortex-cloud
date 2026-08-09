<script>
  // Badges, avatar effects, chat styles and saved outfits. The emulator only ever reads these one
  // player at a time, so the hotel-wide view is the only place a broken grant shows: a badge held by
  // thousands, an effect nobody ever activated, a chat style owned by nobody.
  import { onMount } from 'svelte';
  import { apiGet } from '../lib/api.js';
  import { formatNumber, formatDate, formatDuration } from '../lib/format.js';
  import { isPermissionDeniedError } from '../lib/permissions.js';
  import { openPlayer } from '../lib/session.js';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import EntityLink from '../components/EntityLink.svelte';
  import PickerModal from '../components/PickerModal.svelte';
  import StatCard from '../components/StatCard.svelte';
  import { Award, Sparkles, MessageCircle, Shirt } from '@lucide/svelte';
  import { t } from '../lib/i18n.js';

  let loading = false;
  let forbidden = false;
  let error = '';
  let data = null;
  let player = null;
  let detail = null;
  let detailLoading = false;
  let picking = false;

  async function refresh() {
    loading = true;
    error = '';
    forbidden = false;

    try {
      data = await apiGet('/api/v1/player-rewards');
    } catch (err) {
      if (isPermissionDeniedError(err)) {
        forbidden = true;
        data = null;
        return;
      }

      error = err.message;
      data = null;
    } finally {
      loading = false;
    }
  }

  async function loadPlayer(picked) {
    player = picked;
    detail = null;
    detailLoading = true;

    try {
      detail = await apiGet(`/api/v1/player-rewards/${picked.id}`);
    } catch (err) {
      error = err.message;
    } finally {
      detailLoading = false;
    }
  }

  onMount(() => {
    void refresh();
  });
</script>

<section class="panel">
  <div class="panel-head"><h2>{$t('playerRewards.title')}</h2></div>
  <p class="muted">{$t('playerRewards.description')}</p>
  <div class="toolbar">
    <button type="button" on:click={refresh} disabled={loading}>{$t('common.refresh')}</button>
    <button type="button" class="ghost-button" on:click={() => (picking = true)}>
      {player ? player.name : $t('playerRewards.inspectPlayer')}
    </button>
  </div>

  {#if loading}
    <p class="muted">{$t('common.loading')}</p>
  {:else if forbidden}
    <AccessDeniedNotice message={$t('playerRewards.accessDenied')} />
  {:else if error}
    <p class="empty-state danger">{error}</p>
  {/if}
</section>

{#if data}
  <div class="metric-grid" style="margin-top: 12px;">
    <StatCard
      label={$t('playerRewards.badges')}
      value={formatNumber(data.totals.totalBadges)}
      sub={$t('playerRewards.equipped', { count: formatNumber(data.totals.equippedBadges) })}
    >
      <Award slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('playerRewards.distinctBadges')} value={formatNumber(data.totals.distinctBadgeCodes)}>
      <Award slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('playerRewards.playersWithBadges')} value={formatNumber(data.totals.playersWithBadges)}>
      <Award slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard
      label={$t('playerRewards.effects')}
      value={formatNumber(data.totals.totalEffects)}
      sub={$t('playerRewards.activated', { count: formatNumber(data.totals.activatedEffects) })}
    >
      <Sparkles slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard label={$t('playerRewards.chatStyles')} value={formatNumber(data.totals.chatStyleCount)}>
      <MessageCircle slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
    <StatCard
      label={$t('playerRewards.outfits')}
      value={formatNumber(data.totals.wardrobeOutfits)}
      sub={$t('playerRewards.wardrobeUsers', { count: formatNumber(data.totals.wardrobeUsers) })}
    >
      <Shirt slot="icon" size={15} strokeWidth={2} aria-hidden="true" />
    </StatCard>
  </div>

  <div class="split-grid" style="margin-top: 12px;">
    <div class="panel">
      <div class="panel-head"><h2>{$t('playerRewards.topBadgesTitle')}</h2></div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('playerRewards.colBadge')}</th>
              <th>{$t('playerRewards.colHolders')}</th>
              <th>{$t('playerRewards.colEquipped')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.topBadges || [] as row}
              <tr>
                <td><code>{row.badgeCode}</code></td>
                <td>{formatNumber(row.holders)}</td>
                <td>{formatNumber(row.equipped)}</td>
              </tr>
            {:else}
              <tr><td colspan="3" class="muted">{$t('playerRewards.noBadges')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>

    <div class="panel">
      <div class="panel-head"><h2>{$t('playerRewards.topEffectsTitle')}</h2></div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('playerRewards.colEffect')}</th>
              <th>{$t('playerRewards.colOwners')}</th>
              <th>{$t('playerRewards.colActivated')}</th>
              <th>{$t('playerRewards.colSelected')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.topEffects || [] as row}
              <tr>
                <td>{row.effectId}</td>
                <td>{formatNumber(row.owners)}</td>
                <td>{formatNumber(row.activated)}</td>
                <td>{formatNumber(row.selected)}</td>
              </tr>
            {:else}
              <tr><td colspan="4" class="muted">{$t('playerRewards.noEffects')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  </div>

  <div class="split-grid" style="margin-top: 12px;">
    <div class="panel">
      <div class="panel-head"><h2>{$t('playerRewards.chatStylesTitle')}</h2></div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('playerRewards.colStyleId')}</th>
              <th>{$t('playerRewards.colOwners')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.chatStyles || [] as row}
              <tr>
                <td>{row.clientStyleId}</td>
                <td>{formatNumber(row.owners)}</td>
              </tr>
            {:else}
              <tr><td colspan="2" class="muted">{$t('playerRewards.noChatStyles')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>

    <div class="panel">
      <div class="panel-head"><h2>{$t('playerRewards.topCollectorsTitle')}</h2></div>
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>{$t('playerRewards.colPlayer')}</th>
              <th>{$t('playerRewards.colBadgeCount')}</th>
            </tr>
          </thead>
          <tbody>
            {#each data.topCollectors || [] as row}
              <tr>
                <td><EntityLink type="player" id={row.playerId} label={row.playerName} {openPlayer} /></td>
                <td>{formatNumber(row.badges)}</td>
              </tr>
            {:else}
              <tr><td colspan="2" class="muted">{$t('playerRewards.noBadges')}</td></tr>
            {/each}
          </tbody>
        </table>
      </div>
    </div>
  </div>
{/if}

{#if player}
  <section class="panel" style="margin-top: 12px;">
    <div class="panel-head"><h2>{$t('playerRewards.playerTitle', { name: player.name })}</h2></div>
    {#if detailLoading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if detail}
      <div class="split-grid">
        <div>
          <h3 class="subhead">{$t('playerRewards.badges')}</h3>
          {#if (detail.badges || []).length === 0}
            <EmptyState message={$t('playerRewards.noBadges')} />
          {:else}
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>{$t('playerRewards.colBadge')}</th>
                    <th>{$t('playerRewards.colSlot')}</th>
                    <th>{$t('playerRewards.colEarned')}</th>
                  </tr>
                </thead>
                <tbody>
                  {#each detail.badges as badge}
                    <tr>
                      <td><code>{badge.badgeCode}</code></td>
                      <td>{badge.slotId ?? '—'}</td>
                      <td>{formatDate(badge.createdAt)}</td>
                    </tr>
                  {/each}
                </tbody>
              </table>
            </div>
          {/if}
        </div>

        <div>
          <h3 class="subhead">{$t('playerRewards.effects')}</h3>
          {#if (detail.effects || []).length === 0}
            <EmptyState message={$t('playerRewards.noEffects')} />
          {:else}
            <div class="table-wrap">
              <table>
                <thead>
                  <tr>
                    <th>{$t('playerRewards.colEffect')}</th>
                    <th>{$t('playerRewards.colDuration')}</th>
                    <th>{$t('playerRewards.colActivated')}</th>
                    <th>{$t('playerRewards.colSelected')}</th>
                  </tr>
                </thead>
                <tbody>
                  {#each detail.effects as effect}
                    <tr>
                      <td>{effect.effectId}{effect.subType ? `.${effect.subType}` : ''}</td>
                      <td>{effect.totalDuration ? formatDuration(effect.totalDuration) : '—'}</td>
                      <td>{effect.activatedAt ? formatDate(effect.activatedAt) : '—'}</td>
                      <td>{effect.isSelected ? $t('common.yes') : $t('common.no')}</td>
                    </tr>
                  {/each}
                </tbody>
              </table>
            </div>
          {/if}
        </div>
      </div>

      <h3 class="subhead">{$t('playerRewards.outfitsTitle')}</h3>
      {#if (detail.outfits || []).length === 0}
        <EmptyState message={$t('playerRewards.noOutfits')} />
      {:else}
        <div class="table-wrap">
          <table>
            <thead>
              <tr>
                <th>{$t('playerRewards.colSlot')}</th>
                <th>{$t('playerRewards.colFigure')}</th>
                <th>{$t('playerRewards.colGender')}</th>
              </tr>
            </thead>
            <tbody>
              {#each detail.outfits as outfit}
                <tr>
                  <td>{outfit.slotId}</td>
                  <td><code>{outfit.figure}</code></td>
                  <td>{outfit.gender}</td>
                </tr>
              {/each}
            </tbody>
          </table>
        </div>
      {/if}
    {/if}
  </section>
{/if}

{#if picking}
  <PickerModal
    kind="user"
    title={$t('playerRewards.inspectPlayer')}
    onSelect={(picked) => {
      picking = false;
      void loadPlayer(picked);
    }}
    onClose={() => (picking = false)}
  />
{/if}

<style>
  .subhead {
    margin: 14px 0 8px;
    font-size: 0.95rem;
  }
</style>
