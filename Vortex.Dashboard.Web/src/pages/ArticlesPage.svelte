<script>
  // The website's news, written here and read by Vortex.WebApi's public endpoints.
  //
  // Three jobs on one page, so three tabs rather than three routes: an editor who adds a category
  // does it while writing the article that needed it, and a language is opened once and never
  // thought about again.
  //
  // Two things about the model are worth knowing before reading the rest:
  //
  //   - "Scheduled" is NOT a status. An article waiting for its date is Published with a publishAt
  //     in the future, and the server filters on the date. The list marks it so an editor is not
  //     told "Published" about something nobody can read yet.
  //   - The text, the summary AND the pictures belong to the TRANSLATION, not the article. Habbo's
  //     own promo art is per-language (WebPromo_FanSites_FR.png, Schreibwerkstatt_DE_LargePromo.png),
  //     so a shared header image would put French artwork on a German article.
  import { apiGet } from '../lib/api.js';
  import { createResource } from '../lib/resource.js';
  import { createWriteOps } from '../lib/writeOps.js';
  import { diffFields } from '../lib/changes.js';
  import { hasDashboardCapability } from '../lib/permissions.js';
  import { CAPABILITIES } from '../lib/dashboardPermissions.js';
  import { identity } from '../lib/session.js';
  import { t } from '../lib/i18n.js';

  import AccessDeniedNotice from '../components/AccessDeniedNotice.svelte';
  import ArticleBodyEditor from '../components/ArticleBodyEditor.svelte';
  import AssetImage from '../components/AssetImage.svelte';
  import ConfirmReasonModal from '../components/ConfirmReasonModal.svelte';
  import Drawer from '../components/Drawer.svelte';
  import EmptyState from '../components/EmptyState.svelte';
  import Modal from '../components/Modal.svelte';
  import OpResult from '../components/OpResult.svelte';
  import PageHeader from '../components/PageHeader.svelte';
  import Pagination from '../components/Pagination.svelte';
  import Tabs from '../components/Tabs.svelte';

  import {
    Newspaper,
    Tags,
    Languages,
    CalendarClock,
    Image,
  } from '@lucide/svelte';

  let canManage = $derived(hasDashboardCapability($identity, CAPABILITIES.opsArticlesManage));

  let active = $state('articles');

  // --- list ----------------------------------------------------------------------------------

  let status = $state('');
  let category = $state('');
  let language = $state('');
  let search = $state('');
  let page = $state(1);

  // The filters ARE the identity of this read, so they are the cache key: going back to a filter
  // already looked at is served from cache instead of re-queried.
  const articles = createResource(
    () => ['articles', status, category, language, search, page],
    () => {
      const params = new URLSearchParams();
      if (status) params.set('status', status);
      if (category) params.set('category', category);
      if (language) params.set('lang', language);
      if (search) params.set('q', search);
      params.set('page', String(page));

      return apiGet(`/api/v1/articles?${params}`);
    }
  );

  const meta = createResource(
    () => ['articles-meta'],
    () => apiGet('/api/v1/articles/meta')
  );

  let categories = $derived(meta.data?.categories ?? []);
  let languages = $derived(meta.data?.languages ?? []);
  let enabledLanguages = $derived(languages.filter((l) => l.enabled));
  let defaultLanguageCode = $derived(languages.find((l) => l.isDefault)?.code ?? '');
  let imageBase = $derived(meta.data?.imageBase ?? '');
  let imageDirectories = $derived(meta.data?.imageDirectories ?? ['web_promo']);

  function refreshAll() {
    articles.refresh();
    meta.refresh();
    if (editing?.id) loadArticle(editing.id);
  }

  const ops = createWriteOps(refreshAll);

  // --- editor --------------------------------------------------------------------------------

  // `editing` is the article being written; null means the list is showing on its own. A new
  // article starts as an object with id 0 so the same form serves both cases.
  let editing = $state(null);
  let activeLang = $state('');
  let translations = $state({});
  let loadingArticle = $state(false);

  // Two halves of the drawer: what is written, and how it is filed. The writing opens first.
  let editorTab = $state('content');

  // Set the moment somebody edits the slug themselves; from then on the title stops driving it.
  let slugTouched = $state(false);

  let editorTabs = $derived([
    { id: 'content', label: $t('articles.tabContent'), icon: Newspaper },
    { id: 'publication', label: $t('articles.tabPublication'), icon: CalendarClock },
  ]);

  // What was on screen before the operator touched it. Kept so every write can be audited as
  // "field: before → after" without anybody typing a reason.
  let editingBefore = $state(null);
  let translationsBefore = $state({});
  let categoryBefore = $state(null);
  let languageBefore = $state(null);

  function blankArticle() {
    return {
      id: 0,
      slug: '',
      category: categories[0]?.code ?? '',
      status: 'Draft',
      publishAt: '',
      pinned: false,
      author: $identity?.name ?? $identity?.email ?? '',
    };
  }

  function blankTranslation() {
    return { title: '', summary: '', body: [], headerImage: '', thumbnail: '' };
  }

  function startNew() {
    editorTab = 'content';
    slugTouched = false;
    editing = blankArticle();
    editingBefore = blankArticle();
    translations = {};
    translationsBefore = {};
    activeLang = enabledLanguages[0]?.code ?? '';
    ensureTranslation(activeLang);
  }

  async function loadArticle(id) {
    loadingArticle = true;
    editorTab = 'content';
    slugTouched = true;

    try {
      const detail = await apiGet(`/api/v1/articles/${id}`);

      editingBefore = {
        id: detail.id,
        slug: detail.slug,
        category: detail.category,
        status: detail.status,
        publishAt: toLocalInput(detail.publishAt),
        pinned: detail.pinned,
        author: detail.author,
      };

      editing = {
        id: detail.id,
        slug: detail.slug,
        category: detail.category,
        status: detail.status,
        // <input type="datetime-local"> wants a local, second-less string; the API speaks ISO UTC.
        publishAt: toLocalInput(detail.publishAt),
        pinned: detail.pinned,
        author: detail.author,
      };

      const next = {};

      for (const row of detail.translations ?? []) {
        next[row.lang] = {
          title: row.title,
          summary: row.summary,
          body: parseBody(row.body),
          headerImage: row.headerImage,
          thumbnail: row.thumbnail,
        };
      }

      translations = next;
      translationsBefore = structuredClone(next);
      activeLang = Object.keys(next)[0] ?? enabledLanguages[0]?.code ?? '';
      ensureTranslation(activeLang);
    } finally {
      loadingArticle = false;
    }
  }

  // A body that will not parse is shown as an empty one rather than throwing: the row was edited
  // outside the application, and losing the editor over it helps nobody.
  function parseBody(raw) {
    try {
      const parsed = JSON.parse(raw || '[]');
      return Array.isArray(parsed) ? parsed : [];
    } catch {
      return [];
    }
  }

  function ensureTranslation(lang) {
    if (lang && !translations[lang]) {
      translations = { ...translations, [lang]: blankTranslation() };
    }
  }

  function toLocalInput(iso) {
    if (!iso) return '';

    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) return '';

    const pad = (n) => String(n).padStart(2, '0');

    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
  }

  let draft = $derived(activeLang ? translations[activeLang] ?? blankTranslation() : blankTranslation());

  function patchDraft(patch) {
    translations = { ...translations, [activeLang]: { ...draft, ...patch } };
  }

  /**
   * The title, typed. On a new article the slug follows it until somebody edits the slug by hand:
   * a writer types "Abobbados débarque en ville" and the URL becomes abobbados-debarque-en-ville
   * without them ever visiting the Publication tab.
   */
  function onTitleInput(value) {
    patchDraft({ title: value });

    if (!editing.id && !slugTouched && activeLang === defaultLanguageCode) {
      editing.slug = slugify(value);
    }
  }

  function slugify(value) {
    return (value ?? '')
      // Decompose accents so "débarque" becomes "debarque" rather than losing the letter entirely.
      .normalize('NFD')
      .replace(/[̀-ͯ]/g, '')
      .toLowerCase()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 128);
  }

  // --- image picker --------------------------------------------------------------------------

  // What the picker will do with the path it is given: 'header', 'thumbnail', or — from inside the
  // body editor, which owns its own blocks — the function that puts it there. One modal for all of
  // them; three near-identical pickers is how they drift.
  let picking = $state(null);
  let pickerDir = $state('web_promo');
  let pickerSearch = $state('');
  let pickerPage = $state(1);

  const images = createResource(
    () => ['article-images', pickerDir, pickerSearch, pickerPage, picking !== null],
    () => {
      if (picking === null) return Promise.resolve({ items: [], total: 0 });

      const params = new URLSearchParams({ dir: pickerDir, page: String(pickerPage) });
      if (pickerSearch) params.set('q', pickerSearch);

      return apiGet(`/api/v1/articles/images?${params}`);
    }
  );

  function openPicker(target) {
    picking = target;
    pickerPage = 1;
  }

  function choose(path) {
    if (picking === 'header') patchDraft({ headerImage: path });
    else if (picking === 'thumbnail') patchDraft({ thumbnail: path });
    else if (typeof picking === 'function') picking(path);

    picking = null;
  }

  // Stored paths are relative to the asset host's c_images tree, which is where the site reads them
  // from; the editor needs the whole URL to show anything.
  function previewUrl(path) {
    return imageBase && path ? `${imageBase}${path}` : '';
  }

  // --- writes --------------------------------------------------------------------------------

  // The audited reason is BUILT, never typed: the page's summary plus the fields that actually
  // changed. An audit line reads "Save the article abobbados — Status: Draft → Published" instead of
  // whatever an operator felt like writing in a mandatory box.
  let articleFields = $derived([
    { key: 'slug', label: $t('articles.slug') },
    { key: 'category', label: $t('articles.category') },
    { key: 'status', label: $t('articles.status') },
    { key: 'publishAt', label: $t('articles.publishAt') },
    { key: 'pinned', label: $t('articles.pinnedLabel') },
    { key: 'author', label: $t('articles.author') },
  ]);

  let translationFields = $derived([
    { key: 'title', label: $t('articles.articleTitle') },
    { key: 'summary', label: $t('articles.summary') },
    { key: 'headerImage', label: $t('articles.headerImage') },
    { key: 'thumbnail', label: $t('articles.thumbnail') },
  ]);

  let categoryFields = $derived([
    { key: 'code', label: $t('articles.code') },
    { key: 'labels', label: $t('articles.labels') },
    { key: 'sortOrder', label: $t('articles.sortOrder') },
    { key: 'enabled', label: $t('articles.enabled') },
  ]);

  let languageFields = $derived([
    { key: 'code', label: $t('articles.code') },
    { key: 'label', label: $t('articles.label') },
    { key: 'isDefault', label: $t('articles.defaultLanguage') },
    { key: 'enabled', label: $t('articles.enabled') },
    { key: 'sortOrder', label: $t('articles.sortOrder') },
  ]);

  function saveArticle() {
    ops.ask(
      '/api/v1/operations/articles',
      {
        articleId: editing.id,
        slug: editing.slug,
        category: editing.category,
        status: editing.status,
        // datetime-local has no zone; the server stores UTC, so it is converted here rather than
        // guessed there.
        publishAt: editing.publishAt ? new Date(editing.publishAt).toISOString() : null,
        pinned: editing.pinned,
        author: editing.author,
        translation: articleTranslationPayload,
      },
      editing.id ? $t('articles.saveArticle') : $t('articles.createArticle'),
      $t('articles.saveArticleSummary', { slug: editing.slug }),
      {
        key: 'article',
        changes: [
          ...diffFields(editingBefore, editing, articleFields),
          ...diffFields(
            translationsBefore[activeLang] ?? blankTranslation(),
            draft,
            translationFields
          ),
        ],
        onSuccess: (result) => { if (!editing.id && result?.id) loadArticle(result.id); },
      }
    );
  }

  // Both halves in one request: the article's filing and the language being written. The server
  // saves them in the same audited operation, so a rejected body takes the whole save down rather
  // than leaving an article whose text quietly did not land.
  let articleTranslationPayload = $derived(
    activeLang && draft.title
      ? {
          lang: activeLang,
          title: draft.title,
          summary: draft.summary,
          body: JSON.stringify(draft.body ?? []),
          headerImage: draft.headerImage,
          thumbnail: draft.thumbnail,
        }
      : null
  );

  let canSave = $derived(Boolean(editing?.slug && editing?.category && draft.title));

  function deleteTranslation() {
    ops.ask(
      '/api/v1/operations/articles/translation/delete',
      { articleId: editing.id, lang: activeLang },
      $t('articles.deleteTranslation'),
      $t('articles.deleteTranslationSummary', { lang: activeLang, slug: editing.slug }),
      { key: 'translation' }
    );
  }

  function deleteArticle(row) {
    ops.ask(
      '/api/v1/operations/articles/delete',
      { articleId: row.id },
      $t('articles.deleteArticle'),
      $t('articles.deleteArticleSummary', { slug: row.slug })
    );
  }

  // --- categories and languages ---------------------------------------------------------------

  // Null means no drawer open. Same shape as the article editor above: a form only exists while it
  // is being filled, and never as a panel the list has to be scrolled past.
  let categoryDraft = $state(null);
  let languageDraft = $state(null);

  const blankCategory = () => ({ id: 0, code: '', sortOrder: 0, enabled: true });

  // The labels dictionary, edited as one text box per language rather than as raw JSON: the person
  // filing an article under "Campagnes" is a writer, and `{"fr":"…","en":"…"}` is not a field a
  // writer should ever be shown.
  let categoryLabels = $state({});

  function openCategory(item) {
    categoryDraft = item ? { ...item } : blankCategory();
    categoryBefore = item ? { ...item } : blankCategory();
    categoryLabels = parseLabels(item?.labels);
  }

  function parseLabels(raw) {
    try {
      const parsed = JSON.parse(raw || '{}');
      return parsed && typeof parsed === 'object' && !Array.isArray(parsed) ? parsed : {};
    } catch {
      return {};
    }
  }

  /** The labels a table cell can read, in the order the languages are listed. */
  function labelList(raw) {
    const parsed = parseLabels(raw);

    return languages
      .filter((lang) => parsed[lang.code])
      .map((lang) => ({ code: lang.code, text: parsed[lang.code] }));
  }

  function openLanguage(item) {
    languageDraft = item ? { ...item } : blankLanguage();
    languageBefore = item ? { ...item } : blankLanguage();
  }

  const blankLanguage = () => ({
    id: 0,
    code: '',
    label: '',
    isDefault: false,
    enabled: true,
    sortOrder: 0,
  });

  function saveCategory() {
    const labels = JSON.stringify(
      // Blank boxes are absent labels, not empty ones: an empty string would read as a translated
      // label that happens to say nothing, and the site would print it.
      Object.fromEntries(
        Object.entries(categoryLabels).filter(([, text]) => (text ?? '').trim().length > 0)
      )
    );

    ops.ask(
      '/api/v1/operations/articles/category',
      {
        categoryId: categoryDraft.id,
        code: categoryDraft.code,
        labels,
        sortOrder: categoryDraft.sortOrder,
        enabled: categoryDraft.enabled,
      },
      $t('articles.saveCategory'),
      $t('articles.saveCategorySummary', { code: categoryDraft.code }),
      {
        key: 'category',
        changes: diffFields(
          categoryBefore,
          { ...categoryDraft, labels },
          categoryFields
        ),
        onSuccess: () => (categoryDraft = null),
      }
    );
  }

  function saveLanguage() {
    ops.ask(
      '/api/v1/operations/articles/language',
      {
        languageId: languageDraft.id,
        code: languageDraft.code,
        label: languageDraft.label,
        isDefault: languageDraft.isDefault,
        enabled: languageDraft.enabled,
        sortOrder: languageDraft.sortOrder,
      },
      $t('articles.saveLanguage'),
      $t('articles.saveLanguageSummary', { code: languageDraft.code }),
      {
        key: 'language',
        changes: diffFields(languageBefore, languageDraft, languageFields),
        onSuccess: () => (languageDraft = null),
      }
    );
  }

  let tabs = $derived([
    { id: 'articles', label: $t('articles.tabArticles'), icon: Newspaper, count: articles.data?.total },
    { id: 'categories', label: $t('articles.tabCategories'), icon: Tags, count: categories.length },
    { id: 'languages', label: $t('articles.tabLanguages'), icon: Languages, count: languages.length },
  ]);
</script>

<section class="panel">
  <PageHeader title={$t('articles.title')} description={$t('articles.description')}>
    {#snippet actions()}
      <button type="button" onclick={refreshAll} disabled={articles.loading} class="warning">
        {$t('common.refresh')}
      </button>
    {/snippet}
  </PageHeader>
</section>

<!-- Outside the header panel: the strip draws its own frame, and nesting it in one gave it a second
     border. Same placement as every other tabbed page. -->
<Tabs {tabs} bind:active storageKey="articles" />

{#if articles.forbidden || meta.forbidden}
  <section class="panel">
    <AccessDeniedNotice message={$t('articles.accessDenied')} />
  </section>
{:else if active === 'articles'}
  <section class="panel">
    <form class="toolbar-grid" onsubmit={(event) => { event.preventDefault(); page = 1; articles.refresh(); }}>
      <label>
        {$t('articles.status')}
        <select bind:value={status}>
          <option value="">{$t('articles.anyStatus')}</option>
          <option value="Draft">{$t('articles.statusDraft')}</option>
          <option value="Published">{$t('articles.statusPublished')}</option>
          <option value="Archived">{$t('articles.statusArchived')}</option>
        </select>
      </label>
      <label>
        {$t('articles.category')}
        <select bind:value={category}>
          <option value="">{$t('articles.anyCategory')}</option>
          {#each categories as item (item.code)}
            <option value={item.code}>{item.code}</option>
          {/each}
        </select>
      </label>
      <label>
        {$t('articles.language')}
        <select bind:value={language}>
          <option value="">{$t('articles.anyLanguage')}</option>
          {#each languages as item (item.code)}
            <option value={item.code}>{item.label}</option>
          {/each}
        </select>
      </label>
      <label>
        {$t('articles.search')}
        <input autocomplete="off" spellcheck="false" bind:value={search} placeholder={$t('articles.searchPlaceholder')} />
      </label>
    </form>
  </section>

  <!-- Filtering and reading are two things: the toolbar keeps its own panel so the list below is a
       list, not a form with a table stuck to the bottom of it. -->
  <section class="panel">
    <div class="panel-head">
      <h2><Newspaper size={17} strokeWidth={2} aria-hidden="true" /> {$t('articles.tabArticles')}</h2>
      {#if canManage}
        <button type="button" class="success" onclick={startNew}>{$t('articles.newArticle')}</button>
      {/if}
    </div>

    {#if articles.loading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if articles.error}
      <p class="empty-state danger" role="alert">{articles.error}</p>
    {:else if !(articles.data?.items ?? []).length}
      <EmptyState message={$t('articles.empty')} />
    {:else}
      <table>
        <thead>
          <tr>
            <th>{$t('articles.colTitle')}</th>
            <th>{$t('articles.colCategory')}</th>
            <th>{$t('articles.colStatus')}</th>
            <th>{$t('articles.colDate')}</th>
            <th>{$t('articles.colLanguages')}</th>
            {#if canManage}<th>{$t('common.actions')}</th>{/if}
          </tr>
        </thead>
        <tbody>
          {#each articles.data.items as row (row.id)}
            <tr>
              <td>
                <strong>{row.title || row.slug}</strong>
                <small class="muted block">/{row.slug}</small>
              </td>
              <td>{row.category}</td>
              <td>
                {#if row.scheduled}
                  <span class="chip warn">{$t('articles.statusScheduled')}</span>
                {:else}
                  <span class="chip">{$t(`articles.status${row.status}`)}</span>
                {/if}
                {#if row.pinned}<span class="chip">{$t('articles.pinned')}</span>{/if}
              </td>
              <td>{row.publishAt ? new Date(row.publishAt).toLocaleString() : '—'}</td>
              <td>
                {#each row.languages as code (code)}
                  <span class="chip">{code}</span>
                {:else}
                  <span class="chip warn">{$t('articles.noTranslation')}</span>
                {/each}
              </td>
              {#if canManage}
                <td>
                  <button type="button" class="ghost-button" onclick={() => loadArticle(row.id)}>
                    {$t('common.edit')}
                  </button>
                  <button type="button" class="ghost-button danger" onclick={() => deleteArticle(row)}>
                    {$t('common.delete')}
                  </button>
                </td>
              {/if}
            </tr>
          {/each}
        </tbody>
      </table>

      <Pagination
        page={articles.data.page}
        pageCount={Math.max(1, Math.ceil(articles.data.total / articles.data.pageSize))}
        total={articles.data.total}
        pageSize={articles.data.pageSize}
        label={$t('articles.tabArticles')}
        prevLabel={$t('common.prev')}
        nextLabel={$t('common.next')}
        pageWord={$t('common.page')}
        disabled={articles.loading}
        onchange={(next) => (page = next)}
      />
    {/if}
  </section>

  {#if editing}
    <!-- A drawer, not a panel under the table: the form is long (article fields, then a translation
         per language, then its blocks) and splicing it into the page pushes the list off screen. -->
    <Drawer
      title={editing.id ? $t('articles.editorTitle') : $t('articles.newArticle')}
      eyebrow={$t('articles.title')}
      width={860}
      onclose={() => (editing = null)}
    >
      {#if loadingArticle}
        <p class="muted">{$t('common.loading')}</p>
      {/if}

      <!-- Writing first, filing second. A writer opens this to type a title and a paragraph; the
           slug, the category and the publication date are what happens to the piece afterwards, and
           putting six of them ahead of the title pushed the body off the bottom of the drawer. -->
      <Tabs tabs={editorTabs} bind:active={editorTab} />

      {#if editorTab === 'content'}
        <!-- One tab per enabled language, each saying whether it has been written yet. A missing
             translation is not an error: the public site falls back to the default language. -->
        <div class="lang-strip">
          {#each enabledLanguages as lang (lang.code)}
            <button
              type="button"
              class="lang"
              class:active={activeLang === lang.code}
              class:missing={!translations[lang.code]}
              onclick={() => { activeLang = lang.code; ensureTranslation(lang.code); }}
            >
              {lang.label}
              {#if lang.isDefault}<span class="chip">{$t('articles.defaultLanguage')}</span>{/if}
              {#if !translations[lang.code]}<span class="chip warn">{$t('articles.missing')}</span>{/if}
            </button>
          {/each}
        </div>

        <div class="op-field">
          <label for="translation-title">{$t('articles.articleTitle')}</label>
          <input autocomplete="off" id="translation-title" value={draft.title} oninput={(e) => onTitleInput(e.target.value)} />
          {#if !editing.id && editing.slug}
            <small class="muted">{$t('articles.slugPreview', { slug: editing.slug })}</small>
          {/if}
        </div>
        <div class="op-field">
          <label for="translation-summary">{$t('articles.summary')}</label>
          <textarea id="translation-summary" rows="3" value={draft.summary} oninput={(e) => patchDraft({ summary: e.target.value })}></textarea>
        </div>

        <div>
          <!-- Path, picker and preview on ONE row. Stacked, two image fields ate 250px of a drawer
               and pushed the body — the thing being written — off the bottom. -->
          <div class="op-field">
            <label for="translation-header">{$t('articles.headerImage')}</label>
            <div class="image-row">
              <input autocomplete="off" spellcheck="false" id="translation-header" value={draft.headerImage} oninput={(e) => patchDraft({ headerImage: e.target.value })} placeholder="/web_promo/…" />
              <button type="button" class="ghost-button" onclick={() => openPicker('header')} aria-label={$t('articles.browseImages')}>
                <Image size={14} strokeWidth={2} aria-hidden="true" />
                {$t('articles.browse')}
              </button>
              {#if previewUrl(draft.headerImage)}
                <AssetImage src={previewUrl(draft.headerImage)} alt="" size={36} />
              {/if}
            </div>
          </div>
          <div class="op-field">
            <label for="translation-thumbnail">{$t('articles.thumbnail')}</label>
            <div class="image-row">
              <input autocomplete="off" spellcheck="false" id="translation-thumbnail" value={draft.thumbnail} oninput={(e) => patchDraft({ thumbnail: e.target.value })} placeholder={$t('articles.thumbnailHelp')} />
              <button type="button" class="ghost-button" onclick={() => openPicker('thumbnail')} aria-label={$t('articles.browseImages')}>
                <Image size={14} strokeWidth={2} aria-hidden="true" />
                {$t('articles.browse')}
              </button>
              {#if previewUrl(draft.thumbnail)}
                <AssetImage src={previewUrl(draft.thumbnail)} alt="" size={36} />
              {/if}
            </div>
          </div>
        </div>

        <h3 class="drawer-section">{$t('articles.body')}</h3>
        <p class="muted">{$t('articles.bodyHelp')}</p>

        <!--
          Keyed on the article AND the language: the editor is uncontrolled, so this is what makes
          it re-read the body when the writer switches to another language or opens another article.
          Without the key the German text would be typed over the French document.
        -->
        {#key `${editing.id ?? 'new'}:${activeLang}`}
          <ArticleBodyEditor
            value={draft.body}
            resolveUrl={previewUrl}
            onpickimage={openPicker}
            onchange={(body) => patchDraft({ body })}
          />
        {/key}

        {#if editing.id && translationsBefore[activeLang]}
          <!-- Away from Save, and ghost rather than solid: removing a language is a rare act, and
               the destructive button should not be the biggest thing under a writer's cursor. -->
          <p>
            <button type="button" class="ghost-button danger" onclick={deleteTranslation}>
              {$t('articles.deleteTranslation')}
            </button>
          </p>
        {/if}
      {:else}
        <div class="op-field">
          <label for="article-slug">{$t('articles.slug')}</label>
          <input autocomplete="off" spellcheck="false" id="article-slug" value={editing.slug} oninput={(e) => { slugTouched = true; editing.slug = e.target.value; }} placeholder="abobbados" />
          <small class="muted">{$t('articles.slugHelp')}</small>
        </div>
        <div class="op-field">
          <label for="article-category">{$t('articles.category')}</label>
          <select id="article-category" bind:value={editing.category}>
            {#each categories as item (item.code)}
              <option value={item.code}>{item.code}</option>
            {/each}
          </select>
        </div>
        <div class="op-field">
          <label for="article-status">{$t('articles.status')}</label>
          <select id="article-status" bind:value={editing.status}>
            <option value="Draft">{$t('articles.statusDraft')}</option>
            <option value="Published">{$t('articles.statusPublished')}</option>
            <option value="Archived">{$t('articles.statusArchived')}</option>
          </select>
        </div>
        <div class="op-field">
          <label for="article-publish-at">{$t('articles.publishAt')}</label>
          <input autocomplete="off" type="datetime-local" id="article-publish-at" bind:value={editing.publishAt} />
          <small class="muted">{$t('articles.publishAtHelp')}</small>
        </div>
        <div class="op-field">
          <label for="article-author">{$t('articles.author')}</label>
          <input autocomplete="off" spellcheck="false" id="article-author" bind:value={editing.author} />
        </div>
        <div class="op-checkbox-field">
          <input type="checkbox" id="article-pinned" bind:checked={editing.pinned} />
          <label for="article-pinned">{$t('articles.pinnedLabel')}</label>
        </div>
      {/if}

      {#if $ops.results?.article}<OpResult result={$ops.results.article} />{/if}

      <!-- ONE save. The article and the language being written go up in the same request, so a
           writer never has to work out which of two "Save" buttons kept their paragraph. -->
      {#snippet actions()}
        {#if canManage}
          <button type="button" onclick={saveArticle} disabled={!canSave}>
            {editing.id ? $t('articles.saveArticle') : $t('articles.createArticle')}
          </button>
          {#if !canSave}
            <span class="muted">{$t('articles.saveBlocked')}</span>
          {/if}
        {/if}
      {/snippet}
    </Drawer>
  {/if}
{:else if active === 'categories'}
  <section class="panel">
    <div class="panel-head">
      <h2><Tags size={17} strokeWidth={2} aria-hidden="true" /> {$t('articles.tabCategories')}</h2>
      {#if canManage}
        <button type="button" class="success" onclick={() => openCategory(null)}>
          {$t('articles.newCategory')}
        </button>
      {/if}
    </div>

    {#if !categories.length}
      <EmptyState message={$t('articles.noCategories')} />
    {:else}
    <table>
      <thead>
        <tr>
          <th>{$t('articles.code')}</th>
          <th>{$t('articles.labels')}</th>
          <th>{$t('articles.sortOrder')}</th>
          <th>{$t('articles.enabled')}</th>
          {#if canManage}<th>{$t('common.actions')}</th>{/if}
        </tr>
      </thead>
      <tbody>
        {#each categories as item (item.id)}
          <tr>
            <td>{item.code}</td>
            <td>
              {#each labelList(item.labels) as entry (entry.code)}
                <span class="chip">{entry.code}</span> {entry.text}
              {:else}
                <span class="muted">{$t('articles.noLabels')}</span>
              {/each}
            </td>
            <td>{item.sortOrder}</td>
            <td>{item.enabled ? $t('common.yes') : $t('common.no')}</td>
            {#if canManage}
              <td>
                <button type="button" class="ghost-button" onclick={() => openCategory(item)}>
                  {$t('common.edit')}
                </button>
                <button
                  type="button"
                  class="ghost-button danger"
                  onclick={() => ops.ask('/api/v1/operations/articles/category/delete', { categoryId: item.id }, $t('articles.deleteCategory'), $t('articles.deleteCategorySummary', { code: item.code }))}
                >
                  {$t('common.delete')}
                </button>
              </td>
            {/if}
          </tr>
        {/each}
      </tbody>
    </table>
    {/if}
  </section>

  {#if canManage && categoryDraft}
    <Drawer
      title={categoryDraft.id ? $t('common.edit') : $t('articles.newCategory')}
      eyebrow={$t('articles.tabCategories')}
      onclose={() => (categoryDraft = null)}
    >
      <div class="op-field">
        <label for="category-code">{$t('articles.code')}</label>
        <input autocomplete="off" spellcheck="false" id="category-code" bind:value={categoryDraft.code} placeholder="campagnes" />
      </div>
      <!-- One box per language. What is stored is still a dictionary, but nobody types braces. -->
      {#each enabledLanguages as lang (lang.code)}
        <div class="op-field">
          <label for="category-label-{lang.code}">{$t('articles.labelFor', { language: lang.label })}</label>
          <input
            autocomplete="off"
            id="category-label-{lang.code}"
            value={categoryLabels[lang.code] ?? ''}
            oninput={(e) => (categoryLabels = { ...categoryLabels, [lang.code]: e.target.value })}
            placeholder={lang.isDefault ? 'Campagnes' : ''}
          />
        </div>
      {/each}
      <small class="muted">{$t('articles.labelsHelp')}</small>
      <div class="op-field">
        <label for="category-order">{$t('articles.sortOrder')}</label>
        <input autocomplete="off" type="number" id="category-order" bind:value={categoryDraft.sortOrder} />
      </div>
      <div class="op-checkbox-field">
        <input type="checkbox" id="category-enabled" bind:checked={categoryDraft.enabled} />
        <label for="category-enabled">{$t('articles.enabled')}</label>
      </div>

      {#if $ops.results?.category}<OpResult result={$ops.results.category} />{/if}

      {#snippet actions()}
        <button type="button" onclick={saveCategory} disabled={!categoryDraft.code}>
          {$t('articles.saveCategory')}
        </button>
      {/snippet}
    </Drawer>
  {/if}
{:else}
  <section class="panel">
    <div class="panel-head">
      <h2><Languages size={17} strokeWidth={2} aria-hidden="true" /> {$t('articles.tabLanguages')}</h2>
      {#if canManage}
        <button type="button" class="success" onclick={() => openLanguage(null)}>
          {$t('articles.newLanguage')}
        </button>
      {/if}
    </div>
    <p class="muted">{$t('articles.languagesHelp')}</p>

    {#if !languages.length}
      <EmptyState message={$t('articles.noLanguages')} />
    {:else}
    <table>
      <thead>
        <tr>
          <th>{$t('articles.code')}</th>
          <th>{$t('articles.label')}</th>
          <th>{$t('articles.defaultLanguage')}</th>
          <th>{$t('articles.enabled')}</th>
          <th>{$t('articles.sortOrder')}</th>
          {#if canManage}<th>{$t('common.actions')}</th>{/if}
        </tr>
      </thead>
      <tbody>
        {#each languages as item (item.id)}
          <tr>
            <td>{item.code}</td>
            <td>{item.label}</td>
            <td>{item.isDefault ? $t('common.yes') : $t('common.no')}</td>
            <td>{item.enabled ? $t('common.yes') : $t('common.no')}</td>
            <td>{item.sortOrder}</td>
            {#if canManage}
              <td>
                <button type="button" class="ghost-button" onclick={() => openLanguage(item)}>
                  {$t('common.edit')}
                </button>
                <button
                  type="button"
                  class="ghost-button danger"
                  disabled={item.isDefault}
                  title={item.isDefault ? $t('articles.defaultLanguageLocked') : ''}
                  onclick={() => ops.ask('/api/v1/operations/articles/language/delete', { languageId: item.id }, $t('articles.deleteLanguage'), $t('articles.deleteLanguageSummary', { code: item.code }))}
                >
                  {$t('common.delete')}
                </button>
              </td>
            {/if}
          </tr>
        {/each}
      </tbody>
    </table>
    {/if}
  </section>

  {#if canManage && languageDraft}
    <Drawer
      title={languageDraft.id ? $t('common.edit') : $t('articles.newLanguage')}
      eyebrow={$t('articles.tabLanguages')}
      onclose={() => (languageDraft = null)}
    >
      <div class="op-field">
        <label for="language-code">{$t('articles.code')}</label>
        <input autocomplete="off" spellcheck="false" id="language-code" bind:value={languageDraft.code} placeholder="fr" />
      </div>
      <div class="op-field">
        <label for="language-label">{$t('articles.label')}</label>
        <input autocomplete="off" id="language-label" bind:value={languageDraft.label} placeholder="Français" />
        <small class="muted">{$t('articles.labelHelp')}</small>
      </div>
      <div class="op-field">
        <label for="language-order">{$t('articles.sortOrder')}</label>
        <input autocomplete="off" type="number" id="language-order" bind:value={languageDraft.sortOrder} />
      </div>
      <div class="op-checkbox-field">
        <input type="checkbox" id="language-default" bind:checked={languageDraft.isDefault} />
        <label for="language-default">{$t('articles.defaultLanguage')}</label>
      </div>
      <div class="op-checkbox-field">
        <input type="checkbox" id="language-enabled" bind:checked={languageDraft.enabled} />
        <label for="language-enabled">{$t('articles.enabled')}</label>
      </div>

      {#if $ops.results?.language}<OpResult result={$ops.results.language} />{/if}

      {#snippet actions()}
        <button type="button" onclick={saveLanguage} disabled={!languageDraft.code || !languageDraft.label}>
          {$t('articles.saveLanguage')}
        </button>
      {/snippet}
    </Drawer>
  {/if}
{/if}

{#if picking !== null}
  <Modal
    title={$t('articles.pickImage')}
    eyebrow={$t('articles.imagesCount', { count: images.data?.total ?? 0 })}
    width={760}
    column
    labelledBy="article-image-picker-title"
    onclose={() => (picking = null)}
  >
    <div class="picker-toolbar">
      <select bind:value={pickerDir} onchange={() => (pickerPage = 1)}>
        {#each imageDirectories as dir (dir)}
          <option value={dir}>{dir}</option>
        {/each}
      </select>
      <input
        autocomplete="off"
        spellcheck="false"
        bind:value={pickerSearch}
        oninput={() => (pickerPage = 1)}
        placeholder={$t('articles.searchImages')}
      />
    </div>

    {#if images.loading}
      <p class="muted">{$t('common.loading')}</p>
    {:else if !(images.data?.items ?? []).length}
      <!-- Almost always a configuration, not an empty folder: AssetsLocalRoot has to point at the
           asset tree or this endpoint has nothing to list. -->
      <EmptyState message={$t('articles.noImages')} />
    {:else}
      <div class="gallery-grid">
        {#each images.data.items as image (image.path)}
          <button type="button" class="gallery-item" onclick={() => choose(image.path)} title={image.path}>
            <AssetImage src={previewUrl(image.thumb)} alt={image.path} size={80} />
            <span class="gallery-name">{image.path.split('/').pop()}</span>
          </button>
        {/each}
      </div>

      <div class="op-actions">
        <button type="button" class="ghost-button" onclick={() => (pickerPage = Math.max(1, pickerPage - 1))} disabled={pickerPage === 1}>
          {$t('common.prev')}
        </button>
        <span class="muted">{pickerPage}</span>
        <button
          type="button"
          class="ghost-button"
          onclick={() => (pickerPage += 1)}
          disabled={pickerPage * (images.data?.pageSize ?? 60) >= (images.data?.total ?? 0)}
        >
          {$t('common.next')}
        </button>
      </div>
    {/if}
  </Modal>
{/if}

<ConfirmReasonModal
  open={Boolean($ops.pending)}
  title={$ops.pending?.title ?? ''}
  changes={$ops.pending?.changes ?? []}
  noteOnly={$ops.pending?.noteOnly ?? false}
  summary={$ops.pending?.summary ?? ''}
  confirmLabel={$ops.pending?.title ?? $t('common.confirm')}
  busy={$ops.busy}
  error={$ops.error}
  onconfirm={ops.confirm}
  oncancel={() => ops.cancel()}
/>

<style>
  .block {
    border: 1px solid var(--line);
    border-radius: 8px;
    padding: 10px;
    margin-bottom: 10px;
    background: var(--surface-strong);
  }

  .block-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;
  }

  .block-actions {
    display: flex;
    gap: 4px;
  }

  .lang-strip {
    display: flex;
    flex-wrap: wrap;
    gap: 6px;
    margin-bottom: 12px;
  }

  .lang {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: var(--surface-strong);
    color: var(--muted);
    padding: 6px 10px;
    font-weight: 700;
  }

  .lang.active {
    background: var(--button-bg);
    color: var(--button-ink);
  }

  /* A language nobody has written yet still shows: hiding it is how a translation gets forgotten. */
  .lang.missing:not(.active) {
    opacity: 0.7;
  }

  /* Path, picker and preview read as one control rather than three stacked ones. */
  .image-row {
    display: grid;
    grid-template-columns: minmax(0, 1fr) auto auto;
    align-items: center;
    gap: 8px;
  }

  /* The drawer's section headings: without a rule above them, "Corps" reads as another field label
     instead of the start of the part being written. */
  .drawer-section {
    margin: 18px 0 6px;
    padding-top: 14px;
    border-top: 1px solid var(--line);
  }

  .picker-toolbar {
    display: flex;
    gap: 8px;
    margin-bottom: 10px;
  }

  .picker-toolbar input {
    flex: 1 1 auto;
    min-width: 0;
  }

  .gallery-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(120px, 1fr));
    gap: 10px;
    overflow-y: auto;
    padding: 4px;
    margin: 8px 0;
  }

  .gallery-item {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 6px;
    padding: 8px;
    border: 1px solid var(--line);
    border-radius: 8px;
    background: var(--surface-strong);
    cursor: pointer;
    color: inherit;
    font: inherit;
  }

  .gallery-item:hover {
    border-color: var(--accent);
  }

  .gallery-name {
    font-size: 0.72rem;
    color: var(--muted);
    max-width: 100%;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
</style>
