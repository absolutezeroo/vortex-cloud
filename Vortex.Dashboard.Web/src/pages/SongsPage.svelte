<script>
  import { onMount } from 'svelte';
  import { Pencil, Plus, Trash2 } from '@lucide/svelte';
  import OpResult from '../components/OpResult.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { formatNumber } from '../lib/format.js';
  import { isPermissionDeniedError, hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  let songs = $state([]);
  let total = $state(0);
  let page = $state(1);
  let pageSize = $state(50);
  let search = $state('');

  let loading = $state(false);
  let denied = $state(false);
  let error = $state('');

  // One form for both create and edit: an operator editing a song is filling in the same six fields
  // they filled in to create it, and two forms side by side would be two places to keep in step.
  let editingId = $state(null);
  let form = $state(emptySong());

  const ops = createWriteOps(async () => {
    form = emptySong();
    editingId = null;
    await load();
  });

  function emptySong() {
    return { name: '', creator: '', lengthSeconds: '', officialSongId: '', data: '' };
  }

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsSongsManage));
  let pageCount = $derived(Math.max(1, Math.ceil(total / pageSize)));

  // Milliseconds on the wire and in the table; an operator reads 2:08 off a track. The conversion
  // is the server's on the way in, and this is the way out.
  function lengthLabel(song) {
    const seconds = Math.round(song.lengthMs / 1000);
    const minutes = Math.floor(seconds / 60);

    return `${minutes}:${String(seconds % 60).padStart(2, '0')}`;
  }

  function edit(song) {
    editingId = song.id;
    form = {
      name: song.name,
      creator: song.creator,
      lengthSeconds: Math.round(song.lengthMs / 1000),
      officialSongId: song.officialSongId,
      data: song.data,
    };
  }

  function save() {
    const body = {
      name: form.name,
      creator: form.creator,
      lengthSeconds: Number(form.lengthSeconds) || 0,
      officialSongId: form.officialSongId,
      data: form.data,
    };

    if (editingId) {
      ops.ask(
        '/api/v1/operations/songs/update',
        { ...body, songId: editingId },
        $t('songs.editSong'),
        $t('songs.updated')
      );
    } else {
      ops.ask('/api/v1/operations/songs', body, $t('songs.newSong'), $t('songs.created'));
    }
  }

  function remove(song) {
    ops.ask(
      '/api/v1/operations/songs/delete',
      { songId: song.id },
      `${$t('songs.delete')} — ${song.name}`,
      $t('songs.deleteConfirm'),
      { danger: true }
    );
  }

  async function load() {
    loading = true;
    error = '';
    try {
      const params = new URLSearchParams({ page: String(page) });

      if (search.trim()) params.set('search', search.trim());

      const data = await apiGet(`/api/v1/songs?${params}`);

      songs = data.items ?? [];
      total = data.total ?? 0;
      pageSize = data.pageSize ?? 50;
      denied = false;
    } catch (e) {
      if (isPermissionDeniedError(e)) denied = true;
      else error = e?.message ?? String(e);
    } finally {
      loading = false;
    }
  }

  onMount(load);
</script>

<section class="panel">
  <div class="panel-head">
    <h2>{$t('songs.title')}</h2>
    <div class="head-actions">
      <button type="button" class="warning" onclick={load} disabled={loading}>
        {$t('common.refresh')}
      </button>
      {#if canManage}
        <button
          type="button"
          class="ghost-button"
          onclick={() =>
            ops.ask(
              '/api/v1/operations/songs/reload',
              {},
              $t('songs.reload'),
              $t('songs.reloaded')
            )}
        >
          {$t('songs.reload')}
        </button>
      {/if}
    </div>
  </div>
  <p class="muted">{$t('songs.description')}</p>
</section>

{#if denied}
  <AccessDeniedNotice message={$t('songs.accessDenied')} />
{:else}
  {#if error}
    <EmptyState kind="error" message={error} />
  {/if}
  <OpResult result={$ops.result} />

  <section class="panel">
    <div class="panel-head">
      <h2>{$t('songs.title')} <span class="muted">({formatNumber(total)})</span></h2>
      <div class="head-actions">
        <input
          autocomplete="off"
          spellcheck="false"
          class="search-input"
          placeholder={$t('songs.searchPlaceholder')}
          bind:value={search}
          onkeydown={(e) => {
            if (e.key === 'Enter') {
              page = 1;
              load();
            }
          }}
        />
      </div>
    </div>

    {#if loading}
      <EmptyState kind="loading" />
    {:else if songs.length === 0}
      <EmptyState message={$t('songs.empty')} />
    {:else}
      <div class="table-scroll">
        <table>
          <thead>
            <tr>
              <th>#</th>
              <th>{$t('songs.name')}</th>
              <th>{$t('songs.creator')}</th>
              <th>{$t('songs.length')}</th>
              <th>{$t('songs.officialSongId')}</th>
              <th>{$t('songs.disks')}</th>
              {#if canManage}<th></th>{/if}
            </tr>
          </thead>
          <tbody>
            {#each songs as song (song.id)}
              <tr>
                <td class="muted">{song.id}</td>
                <td>{song.name}</td>
                <td>{song.creator || '—'}</td>
                <td>{lengthLabel(song)}</td>
                <td class="mono">{song.officialSongId || '—'}</td>
                <td>
                  {#if song.diskCount === 0}
                    <span class="status-badge status-badge--warn" title={$t('songs.noDisks')}>
                      {$t('songs.noDisksBadge')}
                    </span>
                  {:else}
                    {formatNumber(song.diskCount)}
                    {#if song.loadedInJukeboxes > 0}
                      <span class="muted">
                        ({formatNumber(song.loadedInJukeboxes)} {$t('songs.loadedInJukeboxes')})
                      </span>
                    {/if}
                  {/if}
                </td>
                {#if canManage}
                  <td class="row-actions">
                    <button type="button" class="ghost-button" onclick={() => edit(song)}>
                      <Pencil size={14} /> {$t('common.edit')}
                    </button>
                    <button type="button" class="danger" onclick={() => remove(song)}>
                      <Trash2 size={14} /> {$t('songs.delete')}
                    </button>
                  </td>
                {/if}
              </tr>
            {/each}
          </tbody>
        </table>
      </div>

      <Pagination
        page={page}
        pageCount={pageCount}
        total={total}
        pageSize={pageSize}
        label={$t('songs.title')}
        prevLabel={$t('common.prev')}
        nextLabel={$t('common.next')}
        pageWord={$t('common.page')}
        disabled={loading}
        onchange={(next) => {
          page = next;
          load();
        }}
      />
    {/if}
  </section>

  {#if canManage}
    <section class="panel">
      <div class="panel-head">
        <h2>{editingId ? $t('songs.editSong') : $t('songs.newSong')}</h2>
        {#if editingId}
          <button
            type="button"
            class="ghost-button"
            onclick={() => {
              editingId = null;
              form = emptySong();
            }}
          >
            {$t('songs.cancel')}
          </button>
        {/if}
      </div>

      <div class="op-grid">
        <div class="op-field">
          <label for="song-name">{$t('songs.name')}</label>
          <input autocomplete="off" spellcheck="false" id="song-name" bind:value={form.name} />
        </div>
        <div class="op-field">
          <label for="song-creator">{$t('songs.creator')}</label>
          <input
            autocomplete="off"
            spellcheck="false"
            id="song-creator"
            bind:value={form.creator}
          />
        </div>
        <div class="op-field">
          <label for="song-length">{$t('songs.lengthSeconds')}</label>
          <input
            autocomplete="off"
            spellcheck="false"
            id="song-length"
            type="number"
            min="1"
            bind:value={form.lengthSeconds}
          />
          <p class="field-hint">{$t('songs.lengthHint')}</p>
        </div>
        <div class="op-field">
          <label for="song-code">{$t('songs.officialSongId')}</label>
          <input autocomplete="off" spellcheck="false" id="song-code" bind:value={form.officialSongId} />
          <p class="field-hint">{$t('songs.officialSongIdHint')}</p>
        </div>
        <div class="op-field op-field--wide">
          <label for="song-data">{$t('songs.data')}</label>
          <textarea id="song-data" rows="3" bind:value={form.data}></textarea>
          <p class="field-hint">{$t('songs.dataHint')}</p>
        </div>
      </div>

      <div class="head-actions">
        <button
          type="button"
          onclick={save}
          disabled={!form.name.trim() || !(Number(form.lengthSeconds) > 0)}
        >
          <Plus size={14} /> {$t('songs.save')}
        </button>
      </div>
    </section>
  {/if}
{/if}

<style>
  .search-input {
    min-width: 18rem;
  }

  .row-actions {
    display: flex;
    gap: 0.4rem;
    justify-content: flex-end;
  }

  .field-hint {
    margin: 0.25rem 0 0;
    font-size: 0.78rem;
    opacity: 0.7;
  }

  .op-field--wide {
    grid-column: 1 / -1;
  }

  .mono {
    font-family: var(--font-mono, monospace);
  }
</style>
