# Audit de lisibilité et de propreté

- **Révision** : `6666f2d` sur `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date** : 2026-09-15.
- **Nature** : audit en lecture seule. Aucun code modifié.
- **Avertissement de méthode** : une première passe de ce rapport donnait une note par axe sur la base d'**une seule mesure** par axe. Plusieurs de ses conclusions étaient fausses. Le §5 liste ce que la passe profonde a corrigé, parce que c'est le résultat le plus utile de cet audit : **sur ce dépôt, une mesure unique ment presque toujours**, et le §4 explique pourquoi.

---

## 1. La réponse en une page

**Le dépôt est propre sur la forme, et inégal sur le fond.**

La forme est excellente et ce n'est pas un compliment de politesse : 84 % des fichiers font moins de 100 lignes, la densité de commentaires est de 13 % dont 11 % de documentation XML, il n'y a que **deux** blocs de code commenté dans 403 000 lignes, **quatre** vrais marqueurs de dette, et les murs d'architecture tiennent (7 vérifiés, 0 fuite).

Le fond l'est moins, et pour une raison qui n'a rien d'esthétique :

> **`partial`, le balayage d'assembly et le découpage en petits fichiers rendent invisibles trois choses : la taille réelle d'une classe, l'utilisation réelle d'un type, et la duplication réelle d'une déclaration.** Ce sont exactement les trois angles morts qui ont produit les bugs des audits précédents.

Les trois faits qui le montrent :

1. `RoomGrain` fait **6 865 lignes sur 31 fichiers** et expose **211 méthodes publiques**, dont **177 avec un corps réel**. On ne voit jamais sa taille.
2. **Six mécanismes distincts enregistrent des types par balayage d'assembly.** Supprimer un type « inutilisé » compile parfaitement et casse une fonctionnalité en silence. Toute analyse de code mort est donc inutilisable ici — y compris les 397 constats « unused » de Qodana.
3. **173 blocs de 12 lignes sont dupliqués dans 3 fichiers ou plus.** Dans `Vortex.Rooms`, ce sont les déclarations de sources wired recopiées verbatim sur 8 à 9 boîtes — **le véhicule exact** du bug documenté dans l'audit wired, où une copie a divergé sans que rien ne compare.

C'est le même motif que partout ailleurs dans ce projet : *une règle appliquée ici, absente chez le voisin, et rien qui compare les deux.*

---

## 2. Ce qui est sain, mesuré

| Mesure | Valeur |
|---|---:|
| Code écrit à la main (hors migrations, `obj/`, `bin/`) | 402 775 lignes / 6 324 fichiers |
| Fichiers ≤ 100 lignes | **5 321 (84 %)** |
| Fichiers > 1 000 lignes | **15 (0,24 %)** |
| Densité de commentaires | **13 %**, dont **11 % de `///`** (39 320 lignes) |
| Blocs de code commenté | **2** |
| Vrais marqueurs TODO/FIXME | **4** |
| Murs d'architecture | 7 vérifiés, **0 fuite** |
| Handlers touchant `VortexDbContext` | **0** — la règle dure de `CLAUDE.md` est tenue |
| Formatage | csharpier 1.2.6 imposé par hook, arbre propre |
| Documentation | 151 fichiers, 25 463 lignes |

**Les commentaires méritent une mention à part.** Ils expliquent presque toujours *pourquoi*, en nommant le bug évité :

> « Consumed before the credits exist, for the reason the crackable is: the reverse order turns one coin still standing on the floor into as many payouts as it can be clicked. »

C'est de la documentation de décision, et elle a une valeur opérationnelle directe : c'est ce qui m'a permis, dans l'audit de sécurité, de distinguer un oubli d'un choix délibéré.

**Les 119 « TODO » n'en sont pas.** 115 sont la même ligne — `// TODO: add properties if/when identified` — sur des composers dont la charge utile officielle est inconnue. C'est la doctrine des specs appliquée, pas de la dette. Il en reste **quatre**, dont un `// TODO hmm`.

---

## 3. Ce qui mérite d'être repris

### 3.1 Deux classes-dieu que `partial` dissimule

| Classe | Lignes | Fichiers |
|---|---:|---:|
| `RoomGrain` | **6 865** | 31 |
| `DashboardEndpoints` | **6 369** | 36 |
| `RoomPetSystem` | 3 481 | 8 |
| `PlayerGrain` | 2 049 | 6 |

Plus `WebApiEndpoints.cs` : **2 121 lignes dans un seul fichier**, 45 endpoints, sans `partial` pour l'excuser.

Le point sur `RoomGrain` n'est pas sa taille brute mais sa composition : **211 méthodes publiques, dont 34 seulement délèguent** à un module ou un système. **177 ont un corps réel.** La décomposition existe (`SecurityModule`, `FurniModule`, `MapModule`, `PetSystem`…) mais l'essentiel de la logique est resté dans le grain.

`RoomGrain` est un grain Orleans à concurrence par tour : chaque méthode publique est un point d'entrée sérialisé sur la même activation. 211, c'est une surface que personne ne tient en tête — et c'est très précisément la surface que l'audit de sécurité a dû inventorier à la main faute de pouvoir la calculer.

*Direction* : famille par famille, jamais d'un bloc. Déplacer `RoomGrain.Settings.*` vers un `RoomSettingsSystem` sur le modèle de `RoomPetSystem`, qui prouve que le motif marche déjà ici.

### 3.2 Deux cent soixante et une méthodes de 80 lignes ou plus

Ma première passe n'avait mesuré les méthodes longues **qu'à l'intérieur de `RoomGrain`**, en avait trouvé 8, et en avait conclu que tout allait bien. Sur l'arbre entier, hors déclarations de type et hors les 17 tables d'enregistrement de `Vortex.Revisions` (qui sont longues par nature) :

```
   481  MapUser                   Vortex.WebApi/Hosting/WebApiEndpoints.cs:442
   418  HandleAsync               Vortex.PacketHandlers/Handshake/SSOTicketMessageHandler.cs:75
   347  LooksLikeMappingEntry     Vortex.Specs/Yaml/YamlReader.cs:157
   346  SettleContractAsync       .../WiredTrading/WiredTradeSettlement.cs:101
   289  ProcessRollersAsync       .../Systems/RoomRollerSystem.cs:36
   258  OnModelCreating           Vortex.Database/Context/VortexDbContext.cs:421
   237  ApplyWiredUpdateAsync     .../Wired/FurnitureWiredLogic.cs:313
```

**`SSOTicketMessageHandler` mérite d'être nommé** : 750 lignes de fichier, dont un `HandleAsync` de **418 lignes** — alors que `CLAUDE.md` impose « keep packet handlers orchestration-only ». C'est le handler de connexion, donc le plus critique du serveur, et le seul endroit où l'audit de sécurité a dû lire 400 lignes d'affilée pour répondre à « que se passe-t-il au login ? ».

Cinq handlers dépassent 150 lignes ; les 554 autres sont sobres. Là encore : la règle existe, elle est tenue presque partout, et rien ne signale les cinq exceptions.

### 3.3 La duplication est trois fois plus étendue que ma première mesure

Première passe : « 18 handlers sur 383 dans des familles identiques — c'est bon. » Mesure réelle, fenêtre glissante de 12 lignes normalisées sur **tout** le C# écrit à la main :

```
  blocs identiques dans >= 3 fichiers distincts : 173
    326 occurrences  Vortex.PacketHandlers
    217 occurrences  Vortex.Rooms
    147 occurrences  Vortex.Rooms.Tests
```

Et le contenu compte plus que le nombre. Dans `Vortex.Rooms` (44 blocs), ce qui se duplique, ce sont les **déclarations de sources wired** :

```csharp
public override List<WiredFurniSourceType[]> GetAllowedFurniSources() =>
    [[ WiredFurniSourceType.SelectedItems, WiredFurniSourceType.SelectorItems,
       WiredFurniSourceType.SignalItems,   WiredFurniSourceType.TriggeredItem ]];

public override List<WiredPlayerSourceType[]> GetAllowedPlayerSources() =>
    [[ WiredPlayerSourceType.TriggeredUser, WiredPlayerSourceType.SelectorUsers,
       WiredPlayerSourceType.SignalUsers ]];
```

recopié verbatim sur 8 à 9 boîtes (`WiredActionGiveVariable`, `WiredActionRemoveVariable`, `WiredAddonSelectorFilter`, `WiredAddonVariablePlaceholder`, `WiredAddonVariableSortFilter`…).

**Ce n'est pas de la duplication cosmétique.** C'est le mécanisme de livraison du bug que l'audit wired a documenté : `wf_slc_users_with_var` déclare 1 règle de paramètre là où son jumeau `wf_slc_furni_with_var` en déclare 5. Quand une déclaration est copiée huit fois, la copie qui diverge ne se voit pas. Une constante partagée (`WiredSources.StandardFurniAndPlayers`) rendrait l'exception visible par construction.

### 3.4 Cent quatre colonnes de texte sans borne

Sur 273 propriétés `string` des entités, **104 (38 %) n'ont ni `[MaxLength]` ni `[StringLength]`**, réparties sur 53 entités. En MySQL, c'est `longtext`. Qodana le signale à 95 occurrences.

Le cas concret :

```csharp
// Vortex.Database/Entities/Room/RoomEntity.cs:23
[Column("name")] public required string Name { get; set; }   // aucune borne

// Vortex.Rooms/Grains/RoomGrain.Settings.cs:77
entity.Name = update.Name;                                    // vient du fil, tel quel

// Vortex.Rooms/RoomService.Create.cs:94
Name = trimmedName,                                           // .Trim() seulement
```

Un nom de room peut donc atteindre la taille d'une trame — 64 Ko — et être stocké tel quel. Alors que le même dépôt écrit, deux fichiers plus loin :

```csharp
entity.Name = name.Length > 100 ? name[..100] : name;   // RoomAdvertisementService.cs:47
return trimmed.Length > 25 ? trimmed[..25] : trimmed;   // les tags, RoomGrain.Settings.cs:660
```

La règle est écrite deux fois et manque sur le champ le plus exposé des trois.

### 3.5 Cinquante-trois gardes que le contrat dit impossibles

```csharp
if (ctx is null || ctx.PlayerId <= 0 || ctx.RoomId <= 0 || message.Id <= 0)
```

`Nullable` est `enable` dans `Directory.Build.props` et `MessageContext` est non-nullable : le contrat dit que `ctx is null` ne peut pas se produire. **53 handlers sur 559** portent ce test ; 506 ne le portent pas. Inoffensif — mais c'est une troisième variante du même symptôme : un idiome défensif appliqué à 9 % des cas, sans règle qui décide.

*(Nuance : la répartition non générique du pipeline peut théoriquement contourner l'analyse de nullabilité. Je ne peux donc pas dire « code mort », seulement « le contrat dit que ça ne peut pas arriver ».)*

### 3.6 Un artefact généré de 37 Mo dans l'historique git

```
   36,9 Mo  docs/qodana.sarif.json
    3,2 Mo  tools/catalog_converter/data/asset_logic.json
    0,9 Mo  Vortex.Database/Seeds/furni_logic_bindings.sql
```

`docs/qodana.sarif.json` est un rapport d'analyse **généré**, versionné, plus gros que tout le reste du texte du dépôt réuni. Chaque régénération ajoute une copie complète à l'historique, que tout clone traîne définitivement. Son contenu est utile — c'est lui qui m'a mis sur la piste des colonnes sans borne — mais sa place est dans un artefact de CI.

### 3.7 Trente noms obfusqués, et de gros composants Svelte

**30 fichiers `class_NNNN*.cs`** dans `Vortex.Revisions` (12 dans `Help`, 4 dans `Clothing`…), noms AS3 obfusqués reportés tels quels. Le choix est défendable — inventer un nom sémantique faux serait pire — mais ces trente-là ne portent aucune explication, alors que le port TypeScript du client documente systématiquement le sien (« *Name derived: the AS3 class is obfuscated as `_SafeCls_4182`* »). Une ligne suffirait à transformer une bizarrerie en décision.

**Côté dashboard** (157 fichiers, 50 021 lignes) : cinq pages Svelte entre 1 244 et 1 933 lignes, `CollectiblesPage.svelte` en tête. Même symptôme que `RoomGrain`, sans `partial` pour le cacher.

---

## 4. Pourquoi une mesure unique ment sur ce dépôt

C'est la conclusion la plus utile de cet audit, et elle vaut au-delà de la propreté.

**Six mécanismes distincts enregistrent des types par balayage d'assembly** :

```
  Vortex.Runtime/AssemblyProcessing/AssemblyExplorer.cs     handlers, comportements
  Vortex.Signals/SignalTranslatorFeatureProcessor.cs        ISignalTranslator<>
  Vortex.Rooms/Providers/RoomObjectLogicProvider.cs         [RoomObjectLogic]
  Vortex.Dashboard.API/Hosting/DashboardWebHost.cs
  Vortex.Database/Extensions/ModelBuilderExtensions.cs
  Vortex.Database/Commerce/CommerceRelayService.cs
```

Conséquence directe : **un type peut n'être nommé nulle part et être pourtant indispensable.** Sur 1 375 types publics dont le nom n'apparaît dans aucun autre fichier, la quasi-totalité sont vivants — handlers résolus par balayage, tests découverts par réflexion xUnit, records de requête liés par ASP.NET, traducteurs de signaux, variables de contexte wired.

Trois conséquences pratiques :

1. **Les 397 constats « unused » de Qodana** (259 `UnusedAutoPropertyAccessor` + 138 `NotAccessedPositionalProperty`) ne sont pas exploitables tels quels. J'en avais moi-même relevé un faux positif en audit de sécurité : Qodana déclare « *Class `RateLimitBehavior` is never used* » alors que c'est le rate-limiter de tout le trafic entrant.
2. **Un nettoyage « supprimons ce qui n'est pas référencé » est dangereux ici** : ça compile, et ça casse en silence. C'est, une fois de plus, exactement la classe de défaut de ce projet.
3. **Les rapports d'analyse statique doivent être vérifiés cas par cas.** Sur les 4 catégories Qodana que j'ai ouvertes : `UnlimitedStringLength` (95) est **réelle et sérieuse** ; `ConditionIsAlwaysTrueOrFalse` (129) est réelle mais bénigne (§3.5) ; `UnusedAutoPropertyAccessor` est un mélange, dont certains cas sont **documentés comme délibérés** dans le code ; `AccessToDisposedClosure` (32) est un **faux positif systématique** sur l'idiome `await using` + lambda EF — les deux cas que j'ai ouverts, dans deux fichiers différents, invoquent la lambda *dans* la portée du `using`.

---

## 5. Ce que la passe profonde a corrigé

Publié parce que ça dit quels chiffres méritent confiance.

| Première passe (une mesure par axe) | Après vérification |
|---|---|
| « Duplication : bon — 18 handlers sur 383 » | **173 blocs** dupliqués dans ≥ 3 fichiers, dont 44 dans `Vortex.Rooms` — et ce sont les déclarations wired, cause directe d'un bug déjà documenté |
| « Code mort : excellent — 2 blocs » | Exact pour les blocs commentés, mais je n'avais pas cherché ailleurs : **261 méthodes ≥ 80 lignes** |
| Méthodes longues mesurées dans `RoomGrain` seulement (8 trouvées) | **261** sur l'arbre, dont un `HandleAsync` de **418 lignes** au login |
| Qodana : 2 catégories sur 10 ouvertes | 4 ouvertes ; une entière (`AccessToDisposedClosure`) se révèle du bruit |
| Types inutilisés : non mesuré | 1 375 candidats, **quasi tous vivants** — et c'est *ça*, le résultat |
| « Handlers : orchestration-only respecté » | Vrai pour `VortexDbContext` (0 violation), faux pour la taille : 5 handlers ≥ 150 lignes |

Deux erreurs de mesure ont aussi été attrapées et corrigées en route : mon détecteur de code commenté ignorait les blocs `/* */` (il annonçait 0 alors qu'il y en a 2), et mon détecteur de méthodes longues comptait les constructeurs primaires comme des méthodes (il annonçait `PluginManager` à 823 lignes, qui est la classe).

---

## 6. Ordre de reprise conseillé

1. **Les bornes de chaîne** (0,5 j) — `[MaxLength]` sur les 104, troncature sur le nom de room. Plus petit effort, et ça ferme une entrée non bornée avant la réouverture.
2. **Sortir le SARIF de git** (10 min) — `.gitignore` + artefact de CI.
3. **Factoriser les déclarations wired dupliquées** (0,5 j) — des constantes partagées pour les jeux de sources standard. Ce n'est pas du confort : c'est ce qui rend visible la boîte qui diverge.
4. **Découper `SSOTicketMessageHandler`** (0,5 j) — 418 lignes sur le chemin de connexion, contre la règle du dépôt lui-même.
5. **`RoomGrain`, famille par famille** (continu) — commencer par `Settings` (146 lignes dans une seule méthode). `RoomPetSystem` est le modèle.
6. **Les deux blocs morts et les 30 `class_NNNN`** (1 h) — supprimer, ou écrire sur place pourquoi ils restent.

---

## 7. Limites

- **Rien n'a été exécuté** : ni build, ni tests, ni couverture. Le ratio tests/production (85 324 / 317 509) est un **volume de lignes**, pas une couverture. Un projet sans projet de test n'est pas forcément non couvert — sa logique peut l'être depuis `Vortex.Rooms.Tests` — et je ne l'ai pas mesuré. Le seul fait dur est que `Vortex.PacketHandlers.Tests` contient **un fichier** couvrant la fonction pure d'un seul handler sur 559, et que **25 projets n'ont aucun projet de test**, dont `Vortex.Catalog`, `Vortex.Inventory`, `Vortex.Marketplace`, `Vortex.Networking` et `Vortex.Protocol`.
- **La duplication est mesurée sur 12 lignes normalisées** : elle ne voit ni les blocs plus courts, ni la duplication logique réécrite différemment, ni le front Svelte (mesuré en volume seulement).
- **Les 2 466 constats Qodana** : 4 catégories sur 10 ouvertes, quelques cas chacune. Les 6 autres ne sont pas jugées.
- **La qualité des commentaires est une appréciation** appuyée sur une lecture large mais non exhaustive ; la densité, elle, est mesurée.
- **Le front dashboard** n'a fait l'objet d'aucune revue de code, d'accessibilité ou de dépendances.
- L'auto-test des hooks rapporte **2 échecs**, tous deux liés au registre d'en-têtes, qui a besoin du dump AS3 absent du dépôt client. Le contrôle de formatage passe.
