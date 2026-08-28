# Éditer les gamedata depuis la dashboard — conception

**Date** : 2026-08-28
**État** : proposée, non implémentée

Rendre modifiables depuis la dashboard les quatre fichiers que le client télécharge :
`external_variables.json`, `external_flash_texts.json`, `furnidata_json.json`, `productdata_json.json`.
Et le faire **multilingue dès le départ**, parce que le client sait le faire et que rien ne l'utilise.

`figuredata.xml` et `figuremap.xml` sont **hors périmètre** — décision explicite, pas un oubli.

---

## 1. Ce qui existe aujourd'hui

Les fichiers vivent sur l'hôte d'assets, `C:/Laragon/www/vortex-assets/gamedata/`. Cette racine est
déjà configurée côté dashboard (`DashboardAssetUrls.LocalRoot`) et `gamedata` figure déjà dans
`HotelAssetRoots`.

| Fichier | Taille | Forme | Entrées |
| --- | --- | --- | --- |
| `external_variables.json` | 42 Ko | map plate | 709 |
| `external_flash_texts.json` | 1 071 Ko | map plate | 12 529 |
| `productdata_json.json` | 3 Mo | `productdata.product[]` | — |
| `furnidata_json.json` | **38 Mo** | `roomitemtypes` / `wallitemtypes` | **55 836** |

`hashes.php` publie `{name, url, hash: md5_file(...)}` et `.htaccess` réécrit `^<name>/.+$` vers le
fichier. Le client demande `<url>/<hash>`. **Conséquence gratuite : toute écriture change le md5,
donc invalide le cache client.** Aucun travail de cache-busting à faire, mais `hashes.php` doit
connaître chaque fichier servi — y compris les nouveaux fichiers par langue (§4).

Les dossiers `gamedata/en/` et `gamedata/fr/` existent et sont **vides**.

---

## 2. Le multilingue — ce que le client cible sait faire

Autorité : `vortex-modern-client`. La source 2016 est lisible, le client cible WIN63-2026 est
vérifié par présence de symboles.

`HabboLocalizationManager.configureLocalizationLocations()` boucle `k = 1, 2, 3…` tant que
`localization.<k>` existe, et lit pour chacun :

| Clé | Rôle |
| --- | --- |
| `localization.<k>` | l'**id** de la langue |
| `localization.<k>.code` | le code (`fr`) |
| `localization.<k>.name` | le nom affiché (`Français`) |
| `localization.<k>.url` | **l'URL du fichier de textes de cette langue** |

puis `registerLocalizationDefinition(id, name, url, code)`.
`requestLocalizationInit()` charge ensuite `external.texts.txt` : c'est la **base**, la langue par défaut.

Le basculement se fait par **`activateLocalizationDefinition(id)`**, qui rend la définition active
*et recharge son URL*. Son unique appelant est `ChatInputWidgetHandler`, sur la commande de chat
**`:lang <id>`**.

> `:lang` reçoit l'**id**, pas le code. On pose donc `localization.<k>` = le code lui-même, pour que
> le joueur tape `:lang fr`. Sans ça la fonctionnalité existe et reste introuvable.

### Ce que la cible a perdu

| Mécanisme | 2016 | Cible WIN63-2026 |
| --- | --- | --- |
| Registre `localization.<k>` | ✅ | ✅ `configureLocalizationLocations`, `registerLocalizationDefinition`, `localization.1` présents |
| `external.texts.txt` | ✅ | ✅ |
| `external.override.texts.txt` (2ᵉ couche) | ✅ | ❌ **absent** |
| `language_selection.enabled` | ✅ | ❌ **absent — clé morte dans le dump actuel** |

Pas de couche d'override sur la cible : **une modification de texte s'écrit dans le fichier de sa
langue**, il n'y a pas de fichier de surcharge à côté du dump.

### La frontière, écrite noir sur blanc

`furnidata.load.url` et `productdata.load.url` sont des propriétés simples, chargées **une fois,
telles quelles**. Il n'existe ni registre équivalent ni substitution `%lang%` dans le client
(vérifié : aucune occurrence). Les noms de furnis vivent dans furnidata (seulement 4 clés
`furni_*_name|desc` dans les textes, donc ce n'est pas la voie).

**Le client n'offre aucun moyen de servir un furnidata par langue.** Furnidata et productdata sont
donc éditables en **une seule langue**. Contourner ça demanderait de servir un contenu différent à la
même URL selon le joueur — une décision d'infrastructure sur l'hôte d'assets, hors de cette
conception. Cette limite est une propriété du client, pas un raccourci.

---

## 3. Modèle d'écriture

Le dump est **figé** (décision de l'exploitant : aucun ré-import de dump officiel). Le fichier est
donc la vérité, édité en place.

Chaque écriture, sans exception :

1. copie de sauvegarde horodatée ;
2. écriture dans un fichier temporaire ;
3. **relecture et parse du temporaire** ;
4. remplacement atomique du fichier réel.

L'étape 3 est le cœur : un fichier que le client télécharge ne doit jamais pouvoir rester cassé. Si
le parse échoue, le fichier réel n'a pas bougé et l'opération est refusée.

Les sauvegardes vont dans `gamedata_backups/`, **frère** de `gamedata/` et donc hors des trois
racines servies par `HotelAssets` — sinon les sauvegardes seraient téléchargeables par n'importe qui.

### Concurrence

Chaque écriture porte le `mtime` attendu du fichier. S'il a bougé, refus. Plusieurs personnes
touchent l'hôtel, et une écriture perdue sur un fichier de 55 836 entrées ne se voit pas.

### Cache et coût

`GamedataDocumentStore` parse à la première demande et garde en mémoire, invalidé à l'écriture.
Recherche et pagination **côté serveur** : la page ne reçoit jamais le fichier.

> `ponytail:` réécriture intégrale des 38 Mo à chaque enregistrement de furni (~1 s de disque).
> C'est le prix de « le fichier est la vérité ». Passer à une écriture incrémentale seulement si
> l'attente devient gênante.

---

## 4. Les langues

**Une seule liste de langues pour tout l'hôtel.** `web_languages` existe déjà (`Code`, `Name`,
`IsDefault`, `Enabled`), créée pour les news. On la réutilise comme **liste**. Pas de seconde table :
deux listes de langues divergent, toujours.

Mais `web_languages.Enabled` signifie « publiable sur le site » et rien d'autre. Une langue peut être
ouverte sur le site sans que ses 12 529 textes client soient traduits, et l'inverse est vrai aussi.
Les deux états sont donc **distincts** : le site garde `Enabled`, le client est activé séparément
depuis cet onglet, et la présence du bloc `localization.<k>` dans `external_variables.json` **est**
cet état — il n'y a pas de second drapeau en base à tenir synchronisé avec le fichier.

Activer une langue **pour le client** produit, en une opération :

1. le bloc `localization.<k>` / `.code` / `.name` / `.url` dans `external_variables.json` —
   **généré, jamais tapé à la main**, et renuméroté de 1 à N à chaque changement (le client s'arrête
   au premier trou) ;
2. le fichier `gamedata/<code>/external_flash_texts.json`, initialisé depuis la langue par défaut ;
3. l'entrée correspondante dans `hashes.php`.

Désactiver une langue retire son bloc et son entrée de hash ; **le fichier de textes est conservé**
(le travail de traduction ne se perd pas sur un clic).

La langue par défaut reste servie par `external.texts.txt`, c'est-à-dire le
`external_flash_texts.json` racine.

---

## 5. Identité des entrées

| Fichier | Clé |
| --- | --- |
| `external_variables.json` | la clé |
| `external_flash_texts.json` | la clé + le code langue |
| `productdata_json.json` | le code produit |
| `furnidata_json.json` | **`(kind, index)`** |

Pour furnidata, ni `id` ni `classname` n'identifient une entrée :

- 55 836 entrées pour **55 254 ids distincts** et **51 425 classnames distincts** ;
- 577 ids sont partagés entre `roomitemtypes` et `wallitemtypes` — deux espaces de noms, légitime ;
- **5 ids sont dupliqués à l'intérieur même de `roomitemtypes`** (p. ex. `2170666`) : deux entrées,
  même id, même liste. Défaut du dump ; on ignore laquelle le client retient.

D'où : la clé est la position dans le tableau, et **la suppression d'une entrée furni n'est pas
supportée** (les index glisseraient). Édition et ajout en fin de liste seulement.

---

## 6. Cohérence furnidata ↔ base

`furniture_definitions` est alimentée *depuis* furnidata, jamais l'inverse. Les deux décrivent les
mêmes meubles et partagent 7 champs :

| furnidata | `furniture_definitions` |
| --- | --- |
| `id` | `SpriteId` |
| `classname` | `Name` |
| `xdim` / `ydim` | `Width` / `Length` |
| `cansiton` / `canstandon` / `canlayon` | `CanSit` / `CanWalk` / `CanLay` |

Changer `xdim` sans changer `Width` fait dessiner au client un meuble 2×1 dont le serveur réserve
1×1. Ni le build, ni les tests, ni l'écran ne le signalent.

Un onglet **Cohérence** liste ces désaccords et propose « aligner la base » par ligne.

Jointure : `id` + `kind` → `SpriteId` + `ProductType` (`roomitemtypes` → sol, `wallitemtypes` → mur).
L'index unique de la table est `(SpriteId, ProductType, FurniCategory)`, donc `(SpriteId, ProductType)`
peut encore désigner plusieurs lignes. **Une jointure ambiguë est listée comme ambiguë, jamais
tranchée au hasard** — et les 5 doublons du §5 y apparaissent aussi.

`Name` est non unique par conception (3533 doublons) : ne jamais joindre dessus, utiliser
`FurnitureDefinitionLookup` pour toute résolution par classname.

---

## 7. Surface HTTP

Lectures, sous `/api/v1/gamedata` :

| Route | Rend |
| --- | --- |
| `GET /files` | les 4 fichiers : taille, nombre d'entrées, `mtime` |
| `GET /entries?file=&lang=&search=&page=` | entrées paginées, recherche côté serveur |
| `GET /languages` | les langues, leur bloc `localization.<k>` et l'état de leur fichier |
| `GET /coherence?page=` | les désaccords du §6 |

Écritures, sous `/api/v1/operations/gamedata/…`, via `DashboardOperationsService` (donc auditées, et
capturées par l'intercepteur avant/après) :
`entry/save`, `entry/delete` (vars et textes seulement), `furni/save`, `language/enable`,
`language/disable`, `coherence/align`.

`file` est une **énumération fermée de 4 valeurs**, jamais un chemin. Aucune concaténation de chemin
issue du réseau.

---

## 8. Interface

Une page, cinq onglets : **Variables · Textes · Furnidata · Produits · Cohérence**.

Chaque onglet : une recherche, un tableau paginé, un tiroir d'édition. Réutilise `createResource`,
`createWriteOps`, `Pagination`, `Drawer`, `Tabs`, `EmptyState` — tous existants.

L'onglet **Textes** est le seul multilingue : une clé par ligne, **une colonne par langue activée**,
les traductions manquantes signalées. C'est cette vue qui rend le travail de traduction faisable ;
un sélecteur de langue obligerait à comparer de mémoire.

Capability `gamedata.manage`, déclarée dans les **quatre** fichiers du checklist `AGENTS.md`.

---

## 9. Pièges connus

- **DI de la dashboard** : `GamedataDocumentStore` doit être ajouté à
  `DashboardWebHost.ForwardedServiceTypes`. Un service non forwardé est lu comme un corps de requête
  et tue la dashboard **entière** au démarrage.
- **Sauvegardes hors des racines servies** (§3), sinon elles sont publiques.
- **Renumérotation de `localization.<k>`** : le client s'arrête au premier index manquant. Un trou
  rend invisibles toutes les langues suivantes.
- **`hashes.php` est généré** à partir de la liste des langues ; l'oublier fait que le client ne voit
  jamais qu'un fichier a changé.
- `.htaccess` doit réécrire les chemins par langue (`^<code>/external_flash_texts/.+$`).

---

## 10. Tests

- le chemin d'écriture : sauvegarde créée, temporaire parsé, remplacement atomique — **et le refus
  laissant le fichier réel intact quand le contenu produit ne parse pas** ;
- le refus sur `mtime` obsolète ;
- la génération du bloc `localization.<k>` : contiguïté de 1 à N après activation *et* désactivation ;
- la jointure de cohérence, dont un cas ambigu et un cas de doublon ;
- le refus d'un `file` hors énumération.

---

## 11. Découpage

| Tranche | Contenu |
| --- | --- |
| 1 | `GamedataDocumentStore` + écriture sûre + registre de langues (`localization.<k>`, fichiers par langue, `hashes.php`) + onglet Variables |
| 2 | Onglet Textes multilingue |
| 3 | Furnidata + Produits + onglet Cohérence |

La tranche 1 porte toute la machinerie d'écriture et le multilingue, c'est-à-dire les deux choses
dont le reste dépend.

---

## 12. Laissé ouvert

- **Furnidata et productdata restent monolingues** (§2). Le client ne permet pas autre chose.
- Le ré-import d'un dump officiel plus récent n'est pas traité : l'exploitant a indiqué que le dump
  est figé. Si ça change, les modifications faites ici seront écrasées et il faudra une couche de
  surcharge — que la cible ne fournit plus pour les textes.
