# Site web — tranche 1 : les news

Date : 2026-08-27
Statut : design validé, en attente de relecture

## Périmètre

Le site (`vortex-modern-client/packages/vortex-web`) existe déjà : port fidèle de habbo-web, Svelte 5
+ Vite + Tailwind, routeur en mode hash, sprite sheet et `fr.json` d'origine. Sa moitié éditoriale est
mockée dans `src/lib/mock.js`, qui annonce lui-même la suite : *« the shapes below are the ones a
habbo.com response has, so wiring a real endpoint later is a swap in one page »*.

**Le front du site n'est pas touché.** Il reste dans son dépôt et consomme le contrat défini ici.

Tout est livré dans **`Vortex.WebApi`**. `Vortex.Dashboard.Web` et `Vortex.Dashboard.API` ne sont pas
touchés.

| Livré ici (`Vortex.WebApi` + `Vortex.Database`) | Hors périmètre |
| --- | --- |
| 3 tables + migration EF | Tout `vortex-web` : `api.js`, `i18n.js`, sélecteur de langue, rendu des blocs |
| Lecture publique — ce que le site affiche | Toute page ou endpoint de la dashboard |
| Écriture protégée — comment les articles se peuplent | L'écran de rédaction lui-même |
| Listage des images `c_images` | — |
| Route OG `/article/{slug}` + service du `dist/` | — |

### Deux surfaces dans le même hôte

| | Préfixe | Auth | Rôle |
| --- | --- | --- | --- |
| Lecture | `/api/public/…` | aucune | ce que le site public affiche |
| Écriture | `/api/admin/…` | session + droit staff | ce qui peuple les articles |

Même hôte, même cookie, même validation : rien à dupliquer, pas de CORS. L'écran de rédaction se
construit ensuite contre `/api/admin/…` sans que le serveur bouge.

## Décisions

| Décision | Choix | Raison |
| --- | --- | --- |
| Étendue | Noyau éditorial | Brouillon/programmé/épinglé/catégories/archive. L'engagement (j'aime, commentaires) et les campagnes (récompenses réclamables) sont des tranches ultérieures : ni les mêmes risques, ni les mêmes surfaces. |
| Corps d'article | Blocs JSON typés | Zéro assainisseur, XSS impossible par construction. Markdown aurait ajouté Markdig + un sanitizer, et c'est le sanitizer qui serait devenu la frontière de sécurité du site public. |
| Écran de rédaction | TipTap, schéma restreint aux blocs | La pile de textareas n'était pas une surface d'écriture. TipTap est *headless* : son document est mappé vers les mêmes blocs (`articleBlocks.js`), donc le rédacteur gagne l'éditeur et la base ne gagne pas de HTML. CKEditor ne sait produire que du HTML, ce qui aurait ramené l'assainisseur que la ligne au-dessus refuse. |
| Multilingue | Site entier, langue au choix du visiteur | Une URL par article, langue résolue côté API. Pas d'URL par langue : le SEO multilingue (hreflang, canonical, sitemap, routeur en mode history) est un chantier à lui seul. |
| Service en prod | `Vortex.WebApi` sert le `dist/` | Même origine que `/api`, donc le cookie de session marche sans reverse proxy ; aucune infra en plus ; aucun couplage de build entre les deux dépôts ; et ça débloque la route OG. |
| Emplacement de la logique | Service EF, pas un grain | Un article est du contenu, pas de l'état de jeu vivant. `IWebApiArticleService` sur `IDbContextFactory<VortexDbContext>`, comme `WebApiPlayerService`. |

## Données

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

Ce qui est **éditorial** (quand ça sort, où ça se range, si c'est épinglé) reste sur l'article ; ce
qui est **rédigé** descend dans la traduction. Entités sous `Vortex.Database/Entities/Web/`, `DbSet`
dans `VortexDbContext`, migration EF (recette hors-ligne habituelle). Toutes héritent de
`VortexEntity` (`id`, `created_at`, `updated_at`, `deleted_at`).

### Pourquoi l'image d'en-tête est sur la traduction

`c_images/web_promo` contient 3 284 fichiers, dont **168 ont une variante de langue** :
`Schreibwerkstatt_DE_LargePromo.png`, `WebPromo_FanSites_FR.png`,
`article_webPromo_aprilfools14_fr.png`. Une image partagée afficherait une promo française en tête
d'un article allemand. Elle appartient donc au texte, pas à l'article.

### Pourquoi les langues sont une table et pas une option de configuration

C'est de la donnée métier réglable : une langue doit pouvoir s'ouvrir sans reconstruire ni
redémarrer. Même raison que `currency_types` ou `ServerConfigGrain`.

`label_json` sur les catégories est un dictionnaire `{"fr": "Campagnes", "en": "Campaigns"}` — une
table de traductions pour deux colonnes serait disproportionné, et la même règle de repli s'y
applique.

### « Programmé » n'est pas un état stocké

`status` a **trois** valeurs : `Draft`, `Published`, `Archived`. Un article programmé est un
`Published` dont `publish_at` est dans le futur. Conséquence : aucun service de fond, aucun timer,
rien à rattraper après un redémarrage. Le filtre public fait tout :

```sql
WHERE deleted_at IS NULL
  AND status = Published
  AND publish_at <= UTC_TIMESTAMP()
ORDER BY pinned DESC, publish_at DESC
```

Un article archivé sort du fil mais son URL reste lisible, comme sur habbo.com.

> **Piège.** `DeletedAt` vient de `VortexEntity` et **rien ne le filtre automatiquement**. Le fil
> public doit l'exclure explicitement — c'est exactement le trou déjà rencontré côté catalogue, où le
> runtime ne filtrait pas `DeletedAt`.

### La règle de repli, une seule fois

Traduction absente dans la langue demandée → **langue par défaut** (`web_languages.is_default`).
Cette règle vaut pour les articles et pour les libellés de catégorie. Le front applique la même à son
habillage (`lib/locales/<code>.json` manquant → fichier par défaut), mais ça, c'est chez lui.

Un article sans **aucune** traduction n'apparaît nulle part, y compris en langue par défaut.

> **Laissé ouvert : une traduction ne se publie pas seule.** Le repli sert la langue par défaut dès
> que la traduction manque, donc « la version anglaise n'est pas prête » et « la version anglaise est
> identique au français » sont indistinguables pour le lecteur, et une traduction en relecture part
> en ligne dès que l'article passe `Published`. Les rédactions multi-pays mettent un état par
> traduction pour ça — ici il faudrait un `publish_at` sur `web_article_translations`. Décision, pas
> oubli : à trancher quand une deuxième langue sera réellement rédigée.

## Lecture publique — `Vortex.WebApi`

Mappés dans `WebApiEndpoints` (tag `Content`), aucune session requise. Service
`IWebApiArticleService` sur `IDbContextFactory<VortexDbContext>`, `AsNoTracking`, pas de cache tant
que rien ne le réclame.

Cette section est **normative** : c'est ce que `src/lib/api.js` implémentera de l'autre côté.

### Résolution de la langue

Chaque endpoint accepte `?lang=<code>`. À défaut, l'en-tête `Accept-Language`. À défaut, la langue par
défaut. Un code inconnu ou désactivé est traité comme absent — jamais une erreur.

La réponse porte toujours `lang` (la langue effectivement servie) et `fallback` (vrai si le contenu
rendu vient de la langue par défaut faute de traduction), pour que le front puisse le signaler.

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

`category` est un code de catégorie ; absent ou `all` = tout. `page` commence à 1, `pageSize` vaut 10
par défaut et est plafonné à 50.

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

`items[]` reprend **exactement** les clés de `mock.js` : `id` est le slug, `image` et `thumbnail` sont
des chemins relatifs sous `c_images` (le front les préfixe déjà avec `IMAGES`), `date` est une date
ISO sans heure. `NewsList.svelte` consomme cette forme sans modification. `categories[]` inclut
l'entrée `tout` en tête, libellée dans la langue servie.

`publishedAt` s'ajoute à `date`, il ne le remplace pas — le front actuel lit `date` et continue de
marcher. C'est le même instant, zoné : un article publié à 23h à Paris est déjà le lendemain en UTC
et la veille à São Paulo, et une date nue ne permet pas au site de le rendre juste. Le `Z` est forcé
(`DateTime.SpecifyKind`) parce que MySQL rend un `DATETIME` en `Unspecified` et qu'un instant sans
zone est parsé comme heure locale par le navigateur — exactement le décalage que ce champ évite.

### `GET /api/public/articles/{slug}?lang=`

Même objet, plus :

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

Slug inconnu, article en brouillon, programmé pour plus tard, ou supprimé → **404**
`{"error": "article_not_found"}`. Un article archivé répond 200 : son URL reste lisible.

`related` : jusqu'à 3 articles publiés de la même catégorie, le plus récent d'abord, l'article courant
exclu ; complété par les plus récents toutes catégories si la catégorie n'en fournit pas assez.

### Les six types de blocs

Contrat fermé. Un type inconnu doit être **ignoré** par le front, jamais rendu brut.

| `type` | Champs | Rendu attendu |
| --- | --- | --- |
| `p` | `text` | un paragraphe |
| `h` | `text` | un sous-titre dans l'article |
| `list` | `items` (≥ 1 `text`), `ordered` optionnel | une liste à puces ou numérotée |
| `img` | `src` (relatif sous `c_images`), `caption` optionnelle | l'image pleine largeur, légende dessous |
| `btn` | `label`, `href` | un bouton ; `href` interne (`#/…`) ou externe |
| `hr` | — | un séparateur |

### `text` : chaîne ou suite de fragments

Un `text` est **soit** une chaîne nue, **soit** un tableau de fragments quand le rédacteur a mis quelque
chose en forme :

```json
{"type":"p","text":[{"t":"avant "},{"t":"gras","b":true},{"t":"ici","href":"#/hotel"}]}
```

| Clé | Sens |
| --- | --- |
| `t` | le texte du fragment ; un `\n` est un saut de ligne |
| `b` `i` `u` `s` | gras, italique, souligné, barré — des booléens, absents sinon |
| `href` | le fragment est un lien ; **mêmes règles que `btn.href`** |

Aucun champ ne contient de HTML : la mise en forme est de la donnée, que le lecteur transforme en
éléments. C'est ce qui permet à la dashboard d'offrir un vrai éditeur (TipTap, `ArticleBodyEditor.svelte`)
sans qu'un assainisseur redevienne la frontière de sécurité du site.

Le lecteur doit rendre le texte en `white-space: pre-wrap`, et **ignorer** une clé qu'il ne connaît pas.
Un `text` dont aucun fragment ne porte de caractère non blanc est refusé, comme une chaîne vide l'est.

## Écriture — `Vortex.WebApi`

Sous `/api/admin/…`, même hôte et même cookie de session que le reste de la WebApi. Pas d'interface
ici : seulement le contrat, pour que l'écran de rédaction soit constructible ensuite sans retoucher au
serveur.

### Autorisation

La session donne un `accountId` (`WebApiSessionStore`). Le droit se résout sur les tables staff qui
existent déjà — `player_account_roles` → `role_permissions` — contre une capability
`web.articles.manage` déclarée dans `Vortex.Primitives/Permissions/Capabilities.cs`. Sans le droit :
**403**. Non authentifié : **401**.

Ce n'est **pas** une capability `dashboard.*` : elle ne passe par aucune des quatre listes du
checklist dashboard, et `CapabilityDeclarationTests` ne la réclame donc pas dans
`Capabilities.Dashboard.All`.

### Endpoints

```
GET    /api/admin/articles?status=&category=&lang=&q=&page=   liste, brouillons compris
POST   /api/admin/articles                                    crée
GET    /api/admin/articles/{id}                               l'article et TOUTES ses traductions
PUT    /api/admin/articles/{id}                               champs éditoriaux
PUT    /api/admin/articles/{id}/translations/{lang}           titre, résumé, images, blocs
DELETE /api/admin/articles/{id}/translations/{lang}
DELETE /api/admin/articles/{id}                               deleted_at
GET/POST/PUT/DELETE  /api/admin/categories[/{id}]
GET/POST/PUT/DELETE  /api/admin/languages[/{id}]
GET    /api/admin/images?dir=&q=&page=
```

### Validation

Le serveur est la frontière ; l'interface aide, elle ne garantit rien.

- `body_json` : tableau de blocs dont chaque `type` est l'un des cinq. Type inconnu, champ requis
  absent, ou autre chose qu'un tableau → **400** `invalid_body`.
- `href` d'un bloc `btn` : seuls `#/…`, `/…`, `http://` et `https://` passent. `javascript:`, `data:`
  et le reste → **400** `invalid_href`.
- `header_image`, `thumbnail`, `src` d'un bloc `img` : chemin relatif sous `c_images`, sans `..`, sans
  schéma → **400** `invalid_image`.
- `slug` : minuscules, chiffres et tirets, unique. Collision → **409** `slug_taken`.
- `publish_at` requis dès que `status = Published`.
- Supprimer la langue par défaut, ou désactiver la dernière langue, est refusé (**409**).

### `GET /api/admin/images?dir=&q=&page=`

De quoi construire un sélecteur d'image. `dir` vaut `web_promo` ou `articles` — liste fermée, aucun
chemin arbitraire. Recherche et pagination obligatoires : 3 284 + 1 550 fichiers ne se servent pas
d'un coup.

C'est le portage de `DashboardApiService.TargetedOffers.cs:231` (`TargetedOfferImages`), y compris son
repli des variantes `.thumb.png` dans l'entrée principale.

```json
{"total": 3284, "page": 1, "items": [
  {"path": "/web_promo/Abobbados_promo.png", "thumb": "/web_promo/Abobbados_promo.thumb.png"}
]}
```

> **À faire avant de tester** : `appsettings.json:103` porte `"AssetsLocalRoot": "./assets"`. Sans le
> pointer sur `C:\Laragon\www\vortex-assets`, l'endpoint rend une liste vide — et une liste vide
> ressemble à un bug alors que c'est une configuration.

## Service du site et partage de lien — `Vortex.WebApi`

Option `Vortex:WebApi:SiteRoot`, validée par `WebApiConfigValidator` : absente = fonctionnalité
éteinte, présente mais introuvable = démarrage refusé, comme le reste de la configuration de ce
module. Quand elle est posée :

```
UseStaticFiles(SiteRoot)
MapFallbackToFile("index.html")
```

Même origine que `/api`, donc le cookie de session `HttpOnly` continue de marcher sans proxy.

### `GET /article/{slug}?lang=`

Le routeur du site est en mode hash : un lien partagé (`site/#/article/x`) n'envoie que `site/` au
serveur, et l'aperçu Discord affiche donc toujours l'accueil. Cette route corrige ça sans toucher au
routeur :

1. lit l'article dans la langue de `?lang=`, à défaut celle d'`Accept-Language`, à défaut la langue
   par défaut ;
2. renvoie `index.html` avec les balises Open Graph injectées ;
3. redirige le navigateur vers `#/article/{slug}`.

Le robot lit les balises, l'humain atterrit dans le SPA. Slug inconnu → l'index sans balises, la SPA
affiche son 404. Les valeurs injectées sont échappées (elles viennent d'un champ éditorial).

**Pourquoi `?lang=` et pas seulement l'en-tête.** Discord, Facebook, X et Slack vont chercher un lien
partagé sans `Accept-Language` utile. Une langue lue de l'en-tête seul rendait donc **tous** les
aperçus dans la langue par défaut, quelle que soit la langue du lecteur qui a partagé. C'est le seul
endroit où le choix « une URL, N langues » se paie visiblement, et le paramètre suffit à le régler
sans ouvrir le chantier des URL par langue.

Balises émises : `og:type`, `og:title`, `og:description`, `og:url` (query comprise, donc le lien
partagé est celui qui est prévisualisé), `og:image` si `AssetBaseUrl` est posé, `og:site_name` si
`Vortex:WebApi:SiteName` est posé, `article:published_time`, et `twitter:card`.

**Pas d'`og:locale`, délibérément.** La balise attend une langue *et* un territoire (`fr_FR`) ;
`web_languages` ne stocke qu'un code nu (`fr`). Émettre `fr` serait une balise malformée, et inventer
`FR` serait faux pour tout hôtel qui publie en français hors de France. Ça demande une colonne
territoire d'abord.

**Conséquence pour le front** : c'est cette URL **sans hash** que la page article doit afficher et
copier, avec la langue courante en `?lang=`. Un lien avec hash n'aura jamais d'aperçu.

## Vérification

`Vortex.WebApi.Tests` (16 tests d'intégration existants, `WebApiTestFactory`) :

- un brouillon n'apparaît pas dans le fil ;
- un `Published` dont `publish_at` est futur n'apparaît pas non plus ;
- l'épinglé passe devant un article plus récent ;
- pagination : `total` est le compte réel, `page=2` ne recoupe pas `page=1` ;
- slug inconnu → 404 ; article archivé → 200 ;
- `?lang=en` sans traduction anglaise sert le français avec `fallback: true` ;
- `?lang=xx` inconnu ne renvoie pas d'erreur ;
- un article sans aucune traduction n'apparaît pas ;
- un article dont `deleted_at` est renseigné n'apparaît pas.

`ArticleShareUrlTests`, pour la route de partage : le titre dans `og:title` et `og:site_name` posé ;
`?lang=en` l'emporte sur l'en-tête ; sans le paramètre l'en-tête décide encore ;
`article:published_time` se termine par `Z` ; un slug inconnu sert quand même le site, sans balises.

> Ces cas étaient annoncés ici et n'existaient pas : `WebApiSiteHosting.Map` n'était appelé que depuis
> l'hôte de production, donc la route n'était pas mappée sous le serveur de test et rien n'était
> atteignable. `WebApiTestFactory` l'appelle maintenant, dans le même ordre que `WebApiWebHost`.

Écriture, même projet de tests :

- sans session → 401 ; avec session mais sans le droit → 403 ;
- bloc de type inconnu → 400 ; `href` en `javascript:` → 400 ; `src` contenant `..` → 400 ;
- slug en double → 409 ;
- `status = Published` sans `publish_at` → 400 ;
- suppression de la langue par défaut → 409 ;
- `/api/admin/images?dir=../..` → 400.

Portail final : `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate`.

## Ce qui reste mocké après cette tranche

`mock.js` garde `BADGES`, `FRIENDS`, `GROUPS`, `DISCUSSIONS`, `ROOMS`, `PURSE`, `SHOP_SECTIONS`. La
suite, dans cet ordre :

2. **Apparts + profil public** — read-models sur `RoomEntity`, `RoomRatingEntity`, `PlayerBadgeEntity`,
   groupes, messagerie. Rien à construire côté données.
3. **Photos** — exige d'abord le vertical caméra dans l'émulateur : `PublishPhotoMessageHandler`,
   `PurchasePhotoMessageHandler`, `RenderRoomMessageHandler`, `PhotoCompetitionMessageHandler` et
   `RequestCameraConfigurationMessageHandler` sont **cinq stubs vides**, et il n'y a ni entité photo ni
   arbre `usercontent` derrière l'hôte d'assets.
4. **Bourse + messagerie** — `PlayerCurrencyEntity`, abonnements, messagerie.

`SHOP_SECTIONS` est hors trajectoire : c'est du vrai argent, donc une question de paiement, pas de
site.

## Hors périmètre, explicitement

- `Vortex.Dashboard.Web` et `Vortex.Dashboard.API` : aucune page, aucun endpoint, aucune capability
  `dashboard.*`. L'écran de rédaction se construira contre `/api/admin/…`, plus tard et ailleurs.
- J'aime, vues, commentaires (tranche « engagement »).
- Récompenses réclamables et liens vers catalogue / appart / quête (tranche « campagnes »).
- URL par langue, `hreflang`, `canonical`, `sitemap.xml`.
- Un rendu serveur complet du site.
- Le front du site : `api.js`, `i18n.js`, le sélecteur de langue, le rendu des blocs. Y compris
  récupérer les fichiers `<lang>.json` de habbo-web-l10n — seul `fr.json` est présent, les autres se
  prennent sur `images.habbo.com/habbo-web-l10n/` comme celui-là.
