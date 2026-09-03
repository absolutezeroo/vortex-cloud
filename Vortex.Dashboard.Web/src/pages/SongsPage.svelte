<script>
  import { onMount } from 'svelte';
  import OpResult from '../components/OpResult.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import Drawer from '../components/Drawer.svelte';
  import Pagination from '../components/Pagination.svelte';
  import { apiGet } from '../lib/api.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { diffFields } from '../lib/changes.js';
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

  // Editing happens in the drawer, never in the page: a form spliced under the table pushes the
  // list off screen, which is the thing you were looking at when you decided to change a number.
  let drawer = $state(null);

  const ops = createWriteOps(async () => {
    drawer = null;
    await load();
  });

  function emptySong() {
    return { name: '', creator: '', lengthSeconds: '', officialSongId: '', data: '' };
  }

  // What the audit line says an edit actually changed, rather than just which row was touched. The
  // operator's own note stays optional; this is the half only the screen knew.
  const SONG_FIELDS = [
    { key: 'name', label: 'Name' },
    { key: 'creator', label: 'Creator' },
    { key: 'lengthSeconds', label: 'Length (s)' },
    { key: 'officialSongId', label: 'Official code' },
    { key: 'data', label: 'Composition' },
  ];

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsSongsManage));
  let pageCount = $derived(Math.max(1, Math.ceil(total / pageSize)));
  let canSave = $derived(
    Boolean(drawer?.form.name.trim()) && Number(drawer?.form.lengthSeconds) > 0
  );

  // Milliseconds on the wire and in the table; an operator reads 2:08 off a track. The conversion
  // is the server's on the way in, and this is the way out.
  function lengthLabel(song) {
    const seconds = Math.round(song.lengthMs / 1000);
    const minutes = Math.floor(seconds / 60);

    return `${minutes}:${String(seconds % 60).padStart(2, '0')}`;
  }

  function openCreate() {
    drawer = { mode: 'create', id: null, form: emptySong() };
  }

  function openEdit(song) {
    const form = {
        name: song.name,
      creator: song.creator,
      lengthSeconds: Math.round(song.lengthMs / 1000),
      officialSongId: song.officialSongId,
      data: song.data,
    };

    // The values as loaded, kept so the diff can say what they WERE.
    drawer = { mode: 'edit', id: song.id, before: { ...form }, form };
  }

  function save() {
    const body = {
      name: drawer.form.name,
      creator: drawer.form.creator,
      lengthSeconds: Number(drawer.form.lengthSeconds) || 0,
      officialSongId: drawer.form.officialSongId,
      data: drawer.form.data,
    };

    if (drawer.mode === 'edit') {
      ops.ask(
        '/api/v1/operations/songs/update',
        { ...body, songId: drawer.id },
        `${$t('songs.editSong')} #${drawer.id}`,
        $t('songs.updated'),
        { changes: diffFields(drawer.before, drawer.form, SONG_FIELDS) }
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
        <button type="button" class="success" onclick={openCreate}>{$t('songs.newSong')}</button>
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
    </div>

    <!-- Filtering has its own row above the table rather than sharing the heading. The search is
         the server's, so it runs on Enter instead of on every keystroke. -->
    <div class="filters">
      <input
        autocomplete="off"
        spellcheck="false"
        type="search"
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
                    <button type="button" class="ghost-button" onclick={() => openEdit(song)}>
                      {$t('common.edit')}
                    </button>
                    <button type="button" class="danger" onclick={() => remove(song)}>
                      {$t('songs.delete')}
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
{/if}

{#if drawer}
  <Drawer
    title={drawer.mode === 'create' ? $t('songs.newSong') : $t('songs.editSong')}
    eyebrow={$t('songs.title')}
    onclose={() => (drawer = null)}
  >
    <div class="drawer-form">
      <div class="op-field">
        <label for="song-name">{$t('songs.name')}</label>
        <input autocomplete="off" spellcheck="false" id="song-name" bind:value={drawer.form.name} />
      </div>
      <div class="op-field">
        <label for="song-creator">{$t('songs.creator')}</label>
        <input
          autocomplete="off"
          spellcheck="false"
          id="song-creator"
          bind:value={drawer.form.creator}
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
          bind:value={drawer.form.lengthSeconds}
        />
        <p class="field-hint">{$t('songs.lengthHint')}</p>
      </div>
      <div class="op-field">
        <label for="song-code">{$t('songs.officialSongId')}</label>
        <input
          autocomplete="off"
          spellcheck="false"
          id="song-code"
          bind:value={drawer.form.officialSongId}
        />
        <p class="field-hint">{$t('songs.officialSongIdHint')}</p>
      </div>
      <div class="op-field">
        <label for="song-data">{$t('songs.data')}</label>
        <textarea id="song-data" rows="4" bind:value={drawer.form.data}></textarea>
        <p class="field-hint">{$t('songs.dataHint')}</p>
      </div>
    </div>

    {#snippet actions()}
      <button
        type="button"
        class={drawer.mode === 'create' ? 'success' : ''}
        onclick={save}
        disabled={!canSave}
      >
        {$t('songs.save')}
      </button>
      <button type="button" class="ghost-button" onclick={() => (drawer = null)}>
        {$t('songs.cancel')}
      </button>
    {/snippet}
  </Drawer>
{/if}

<style>
  .filters {
    display: flex;
    flex-wrap: wrap;
    gap: 8px;
    margin-bottom: 10px;
  }

  .filters input {
    flex: 1 1 220px;
  }

  .drawer-form {
    display: grid;
    gap: 14px;
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

  .mono {
    font-family: var(--font-mono, monospace);
  }
</style>
