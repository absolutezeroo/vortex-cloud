# Website — slice 1: the news

Date: 2026-08-27
Status: design approved, pending review

## Scope

The site (`vortex-modern-client/packages/vortex-web`) already exists: a faithful port of habbo-web, Svelte 5
+ Vite + Tailwind, hash-mode router, original sprite sheet and `fr.json`. Its editorial half is
mocked in `src/lib/mock.js`, which announces the sequel itself: *"the shapes below are the ones a
habbo.com response has, so wiring a real endpoint later is a swap in one page"*.

**The site's front end is not touched.** It stays in its repository and consumes the contract defined here.

Everything is delivered in **`Vortex.WebApi`**. `Vortex.Dashboard.Web` and `Vortex.Dashboard.API` are not
touched.

| Delivered here (`Vortex.WebApi` + `Vortex.Database`) | Out of scope |
| --- | --- |
| 3 tables + EF migration | All of `vortex-web`: `api.js`, `i18n.js`, language picker, block rendering |
| Public read — what the site displays | Any dashboard page or endpoint |
| Protected write — how articles get populated | The authoring screen itself |
| Listing the `c_images` images | — |
| OG route `/article/{slug}` + serving the `dist/` | — |

### Two surfaces in the same host

| | Prefix | Auth | Role |
| --- | --- | --- | --- |
| Read | `/api/public/…` | none | what the public site displays |
| Write | `/api/admin/…` | session + staff right | what populates the articles |

Same host, same cookie, same validation: nothing to duplicate, no CORS. The authoring screen is then
built against `/api/admin/…` without the server moving.

## Decisions

| Decision | Choice | Reason |
| --- | --- | --- |
| Extent | Editorial core | Draft/scheduled/pinned/categories/archive. Engagement (likes, comments) and campaigns (claimable rewards) are later slices: neither the same risks nor the same surfaces. |
| Article body | Typed JSON blocks | Zero sanitizer, XSS impossible by construction. Markdown would have added Markdig + a sanitizer, and the sanitizer would have become the public site's security boundary. |
| Authoring screen | TipTap, schema restricted to the blocks | A stack of textareas was not a writing surface. TipTap is *headless*: its document maps to the same blocks (`articleBlocks.js`), so the writer gains the editor and the database does not gain HTML. CKEditor can only produce HTML, which would have brought back the sanitizer the line above refuses. |
| Multilingual | Whole site, language chosen by the visitor | One URL per article, language resolved API-side. No per-language URL: multilingual SEO (hreflang, canonical, sitemap, history-mode router) is a project of its own. |
| Serving in production | `Vortex.WebApi` serves the `dist/` | Same origin as `/api`, so the session cookie works with no reverse proxy; no extra infrastructure; no build coupling between the two repositories; and it unblocks the OG route. |
| Where the logic lives | EF service, not a grain | An article is content, not live game state. `IWebApiArticleService` over `IDbContextFactory<VortexDbContext>`, like `WebApiPlayerService`. |

## Data

```
web_languages
  id · code ("fr") · label ("Français") · is_default · enabled · sort_order

web_articles
  id · slug UNIQUE · category_id FK · status · publish_at · pinned · author_name
  INDEX (status, publish_at)

web_article_translations
  id · article_id FK · language_code · title · summary · body_json (longtext)
            · header_image · thumbnail
  UNIQUE (article_id, language_code)

web_article_categories
  id · code ("campagnes") · label_json · sort_order · enabled
```

What is **editorial** (when it goes out, where it is filed, whether it is pinned) stays on the article; what
is **written** goes down into the translation. Entities under `Vortex.Database/Entities/Web/`, `DbSet`
in `VortexDbContext`, EF migration (the usual offline recipe). All inherit from
`VortexEntity` (`id`, `created_at`, `updated_at`, `deleted_at`).

### Why the header image lives on the translation

`c_images/web_promo` contains 3,284 files, of which **168 have a language variant**:
`Schreibwerkstatt_DE_LargePromo.png`, `WebPromo_FanSites_FR.png`,
`article_webPromo_aprilfools14_fr.png`. A shared image would show a French promo at the top of a
German article. So it belongs to the text, not to the article.

### Why languages are a table and not a configuration option

This is tunable business data: a language must be able to open without a rebuild or a
restart. Same reason as `currency_types` or `ServerConfigGrain`.

`label_json` on the categories is a dictionary `{"fr": "Campagnes", "en": "Campaigns"}` — a
translation table for two columns would be disproportionate, and the same fallback rule applies to it.

### "Scheduled" is not a stored state

`status` has **three** values: `Draft`, `Published`, `Archived`. A scheduled article is a
`Published` whose `publish_at` is in the future. Consequence: no background service, no timer,
nothing to catch up after a restart. The public filter does it all:

```sql
WHERE deleted_at IS NULL
  AND status = Published
  AND publish_at <= UTC_TIMESTAMP()
ORDER BY pinned DESC, publish_at DESC
```

An archived article leaves the feed but its URL stays readable, as on habbo.com.

> **Trap.** `DeletedAt` comes from `VortexEntity` and **nothing filters it automatically**. The public
> feed must exclude it explicitly — that is exactly the hole already hit on the catalog side, where the
> runtime did not filter `DeletedAt`.

### The fallback rule, once only

Translation missing in the requested language → **default language** (`web_languages.is_default`).
That rule holds for articles and for category labels. The front end applies the same to its
chrome (`lib/locales/<code>.json` missing → default file), but that is its business.

An article with **no** translation at all appears nowhere, including in the default language.

> **Left open: a translation cannot be published on its own.** The fallback serves the default language as
> soon as the translation is missing, so "the English version is not ready" and "the English version is
> identical to the French one" are indistinguishable to the reader, and a translation under review goes
> live as soon as the article turns `Published`. Multi-country newsrooms put a state per
> translation for that — here it would need a `publish_at` on `web_article_translations`. A decision, not an
> oversight: to be settled when a second language is actually written.

## Public read — `Vortex.WebApi`

Mapped in `WebApiEndpoints` (tag `Content`), no session required. Service
`IWebApiArticleService` over `IDbContextFactory<VortexDbContext>`, `AsNoTracking`, no cache as long
as nothing asks for one.

This section is **normative**: it is what `src/lib/api.js` will implement on the other side.

### Language resolution

Every endpoint accepts `?lang=<code>`. Failing that, the `Accept-Language` header. Failing that, the default
language. An unknown or disabled code is treated as absent — never an error.

The response always carries `lang` (the language actually served) and `fallback` (true if the rendered
content comes from the default language for lack of a translation), so the front end can flag it.

### `GET /api/public/languages`

```json
{
  "default": "fr",
  "items": [
    {"code": "fr", "label": "Français"},
    {"code": "en", "label": "English"}
  ]
}
```

### `GET /api/public/articles?category=&lang=&page=&pageSize=`

`category` is a category code; absent or `all` = everything. `page` starts at 1, `pageSize` defaults to 10
and is capped at 50.

```json
{
  "lang": "fr",
  "page": 1,
  "pageSize": 10,
  "total": 37,
  "categories": [
    {"id": "tout", "label": "Tout"},
    {"id": "campagnes", "label": "Campagnes"}
  ],
  "items": [
    {
      "id": "abobbados",
      "category": "campagnes",
      "title": "Abobbados débarque en ville",
      "summary": "La famille la plus redoutée de l'hôtel ouvre ses portes.",
      "image": "/web_promo/Abobbados_largepromo.png",
      "thumbnail": "/web_promo/Abobbados_promo.png",
      "date": "2026-08-24",
      "publishedAt": "2026-08-24T21:00:00.0000000Z",
      "author": "Vortex",
      "pinned": true,
      "fallback": false
    }
  ]
}
```

`items[]` uses **exactly** the keys from `mock.js`: `id` is the slug, `image` and `thumbnail` are
relative paths under `c_images` (the front end already prefixes them with `IMAGES`), `date` is an ISO
date with no time. `NewsList.svelte` consumes this shape unmodified. `categories[]` includes
the `tout` entry first, labelled in the language served.

`publishedAt` is added to `date`, it does not replace it — the current front end reads `date` and keeps
working. It is the same instant, zoned: an article published at 11 pm in Paris is already the next day in UTC
and the previous day in São Paulo, and a bare date does not let the site render it correctly. The `Z` is forced
(`DateTime.SpecifyKind`) because MySQL returns a `DATETIME` as `Unspecified` and an instant with no
zone is parsed as local time by the browser — exactly the offset this field avoids.

### `GET /api/public/articles/{slug}?lang=`

Same object, plus:

```json
{
  "body": [
    {"type": "p",   "text": "Les Abobbados ont posé leurs valises…"},
    {"type": "h",   "text": "Le programme de la semaine"},
    {"type": "img", "src": "/web_promo/ABOBBADOS_P03.png", "caption": "Le quartier"},
    {"type": "btn", "label": "Voir au catalogue", "href": "#/hotel"},
    {"type": "hr"}
  ],
  "related": [
    {"id": "habboween", "title": "10 ans de Habboween"}
  ]
}
```

Unknown slug, draft article, scheduled for later, or deleted → **404**
`{"error": "article_not_found"}`. An archived article answers 200: its URL stays readable.

`related`: up to 3 published articles from the same category, most recent first, the current article
excluded; topped up with the most recent across all categories if the category does not supply enough.

### The six block types

A closed contract. An unknown type must be **ignored** by the front end, never rendered raw.

| `type` | Fields | Expected rendering |
| --- | --- | --- |
| `p` | `text` | a paragraph |
| `h` | `text` | a subheading inside the article |
| `list` | `items` (≥ 1 `text`), optional `ordered` | a bulleted or numbered list |
| `img` | `src` (relative under `c_images`), optional `caption` | the image full width, caption below |
| `btn` | `label`, `href` | a button; `href` internal (`#/…`) or external |
| `hr` | — | a separator |

### `text`: string or sequence of fragments

A `text` is **either** a bare string **or** an array of fragments when the writer has formatted
something:

```json
{"type":"p","text":[{"t":"avant "},{"t":"gras","b":true},{"t":"ici","href":"#/hotel"}]}
```

| Key | Meaning |
| --- | --- |
| `t` | the fragment's text; a `\n` is a line break |
| `b` `i` `u` `s` | bold, italic, underline, strikethrough — booleans, absent otherwise |
| `href` | the fragment is a link; **same rules as `btn.href`** |

No field contains HTML: formatting is data, which the reader turns into
elements. That is what lets the dashboard offer a real editor (TipTap, `ArticleBodyEditor.svelte`)
without a sanitizer becoming the site's security boundary again.

The reader must render text with `white-space: pre-wrap`, and **ignore** a key it does not know.
A `text` in which no fragment carries a non-whitespace character is refused, just as an empty string is.

## Write — `Vortex.WebApi`

Under `/api/admin/…`, same host and same session cookie as the rest of the WebApi. No UI
here: only the contract, so the authoring screen can be built later without touching the
server again.

### Authorization

The session gives an `accountId` (`WebApiSessionStore`). The right resolves against the staff tables that
already exist — `player_account_roles` → `role_permissions` — against a `web.articles.manage`
capability declared in `Vortex.Primitives/Permissions/Capabilities.cs`. Without the right:
**403**. Unauthenticated: **401**.

It is **not** a `dashboard.*` capability: it goes through none of the four lists in the
dashboard checklist, and `CapabilityDeclarationTests` therefore does not require it in
`Capabilities.Dashboard.All`.

### Endpoints

```
GET    /api/admin/articles?status=&category=&lang=&q=&page=   list, drafts included
POST   /api/admin/articles                                    create
GET    /api/admin/articles/{id}                               the article and ALL its translations
PUT    /api/admin/articles/{id}                               editorial fields
PUT    /api/admin/articles/{id}/translations/{lang}           title, summary, images, blocks
DELETE /api/admin/articles/{id}/translations/{lang}
DELETE /api/admin/articles/{id}                               deleted_at
GET/POST/PUT/DELETE  /api/admin/categories[/{id}]
GET/POST/PUT/DELETE  /api/admin/languages[/{id}]
GET    /api/admin/images?dir=&q=&page=
```

### Validation

The server is the boundary; the UI helps, it guarantees nothing.

- `body_json`: an array of blocks each of whose `type` is one of the five. Unknown type, required field
  missing, or anything other than an array → **400** `invalid_body`.
- A `btn` block's `href`: only `#/…`, `/…`, `http://` and `https://` pass. `javascript:`, `data:`
  and the rest → **400** `invalid_href`.
- `header_image`, `thumbnail`, an `img` block's `src`: relative path under `c_images`, no `..`, no
  scheme → **400** `invalid_image`.
- `slug`: lowercase, digits and hyphens, unique. Collision → **409** `slug_taken`.
- `publish_at` required as soon as `status = Published`.
- Deleting the default language, or disabling the last language, is refused (**409**).

### `GET /api/admin/images?dir=&q=&page=`

Enough to build an image picker. `dir` is `web_promo` or `articles` — a closed list, no
arbitrary path. Search and pagination are mandatory: 3,284 + 1,550 files are not served
in one go.

It is the port of `DashboardApiService.TargetedOffers.cs:231` (`TargetedOfferImages`), including its
folding of `.thumb.png` variants into the main entry.

```json
{"total": 3284, "page": 1, "items": [
  {"path": "/web_promo/Abobbados_promo.png", "thumb": "/web_promo/Abobbados_promo.thumb.png"}
]}
```

> **To do before testing**: `appsettings.json:103` carries `"AssetsLocalRoot": "./assets"`. Without
> pointing it at `C:\Laragon\www\vortex-assets`, the endpoint returns an empty list — and an empty list
> looks like a bug when it is configuration.

## Serving the site and link sharing — `Vortex.WebApi`

Option `Vortex:WebApi:SiteRoot`, validated by `WebApiConfigValidator`: absent = feature
off, present but not found = startup refused, like the rest of this module's configuration. When it is set:

```
UseStaticFiles(SiteRoot)
MapFallbackToFile("index.html")
```

Same origin as `/api`, so the `HttpOnly` session cookie keeps working with no proxy.

### `GET /article/{slug}?lang=`

The site's router is in hash mode: a shared link (`site/#/article/x`) only sends `site/` to the
server, so the Discord preview always shows the home page. This route fixes that without touching the
router:

1. reads the article in the language from `?lang=`, failing that the one from `Accept-Language`, failing that the
   default language;
2. returns `index.html` with the Open Graph tags injected;
3. redirects the browser to `#/article/{slug}`.

The crawler reads the tags, the human lands in the SPA. Unknown slug → the index with no tags, the SPA
shows its 404. The injected values are escaped (they come from an editorial field).

**Why `?lang=` and not just the header.** Discord, Facebook, X and Slack fetch a shared
link with no useful `Accept-Language`. A language read from the header alone therefore rendered **all**
previews in the default language, whatever the language of the reader who shared it. It is the only
place where the "one URL, N languages" choice is visibly paid for, and the parameter is enough to settle it
without opening the per-language URL project.

Tags emitted: `og:type`, `og:title`, `og:description`, `og:url` (query included, so the shared
link is the one previewed), `og:image` if `AssetBaseUrl` is set, `og:site_name` if
`Vortex:WebApi:SiteName` is set, `article:published_time`, and `twitter:card`.

**No `og:locale`, deliberately.** The tag expects a language *and* a territory (`fr_FR`);
`web_languages` only stores a bare code (`fr`). Emitting `fr` would be a malformed tag, and inventing
`FR` would be wrong for any hotel publishing in French outside France. It needs a territory column
first.

**Consequence for the front end**: it is this **hash-free** URL that the article page must display and
copy, with the current language in `?lang=`. A link with a hash will never get a preview.

## Verification

`Vortex.WebApi.Tests` (16 existing integration tests, `WebApiTestFactory`):

- a draft does not appear in the feed;
- a `Published` whose `publish_at` is in the future does not appear either;
- the pinned one comes before a more recent article;
- pagination: `total` is the real count, `page=2` does not overlap `page=1`;
- unknown slug → 404; archived article → 200;
- `?lang=en` with no English translation serves French with `fallback: true`;
- an unknown `?lang=xx` does not return an error;
- an article with no translation at all does not appear;
- an article whose `deleted_at` is set does not appear.

`ArticleShareUrlTests`, for the share route: the title in `og:title` and `og:site_name` set;
`?lang=en` wins over the header; without the parameter the header still decides;
`article:published_time` ends with `Z`; an unknown slug still serves the site, with no tags.

> These cases were announced here and did not exist: `WebApiSiteHosting.Map` was only called from
> the production host, so the route was not mapped under the test server and nothing was
> reachable. `WebApiTestFactory` now calls it, in the same order as `WebApiWebHost`.

Write, same test project:

- no session → 401; session but no right → 403;
- unknown block type → 400; `javascript:` `href` → 400; `src` containing `..` → 400;
- duplicate slug → 409;
- `status = Published` with no `publish_at` → 400;
- deleting the default language → 409;
- `/api/admin/images?dir=../..` → 400.

Final gate: `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate`.

## What stays mocked after this slice

`mock.js` keeps `BADGES`, `FRIENDS`, `GROUPS`, `DISCUSSIONS`, `ROOMS`, `PURSE`, `SHOP_SECTIONS`. The
sequel, in this order:

2. **Rooms + public profile** — read models over `RoomEntity`, `RoomRatingEntity`, `PlayerBadgeEntity`,
   groups, messaging. Nothing to build on the data side.
3. **Photos** — requires the camera vertical in the emulator first: `PublishPhotoMessageHandler`,
   `PurchasePhotoMessageHandler`, `RenderRoomMessageHandler`, `PhotoCompetitionMessageHandler` and
   `RequestCameraConfigurationMessageHandler` are **five empty stubs**, and there is neither a photo entity nor a
   `usercontent` tree behind the asset host.
4. **Purse + messaging** — `PlayerCurrencyEntity`, subscriptions, messaging.

`SHOP_SECTIONS` is off the trajectory: it is real money, hence a payment question, not a
site one.

## Out of scope, explicitly

- `Vortex.Dashboard.Web` and `Vortex.Dashboard.API`: no page, no endpoint, no `dashboard.*`
  capability. The authoring screen will be built against `/api/admin/…`, later and elsewhere.
- Likes, views, comments (the "engagement" slice).
- Claimable rewards and links to catalog / room / quest (the "campaigns" slice).
- Per-language URLs, `hreflang`, `canonical`, `sitemap.xml`.
- Full server-side rendering of the site.
- The site's front end: `api.js`, `i18n.js`, the language picker, block rendering. Including
  fetching the `<lang>.json` files from habbo-web-l10n — only `fr.json` is present, the others are
  taken from `images.habbo.com/habbo-web-l10n/` like that one.
