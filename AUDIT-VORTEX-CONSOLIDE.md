# Audit technique — Vortex Cloud Emulator

**Rapport consolidé unique.** Remplace et intègre les 12 rapports de lot.

---

## Sommaire

1. [Version auditée et méthode](#1-version-auditée-et-méthode)
2. [Executive Summary](#2-executive-summary)
3. [Architecture actuelle](#3-architecture-actuelle)
4. [Carte des projets et dépendances](#4-carte-des-projets-et-dépendances)
5. [Les trois défauts d'architecture](#5-les-trois-défauts-darchitecture)
6. [Findings — CRITICAL](#6-findings--critical)
7. [Findings — HIGH](#7-findings--high)
8. [Findings — MEDIUM](#8-findings--medium)
9. [Findings — LOW / INFO](#9-findings--low--info)
10. [Points forts à conserver](#10-points-forts-à-conserver--keep)
11. [Architecture cible](#11-architecture-cible)
12. [Roadmap](#12-roadmap-de-refonte)
13. [Matrice de couverture](#13-matrice-de-couverture)
14. [Notes de prévention](#14-notes-de-prévention--éviter-cette-classe-de-problèmes)

---

## 1. Version auditée et méthode

| | |
|---|---|
| Source | `vortex-cloud-main.zip` (archive fournie, branche `main`) |
| Commit HEAD | **Non déterminable** — l'archive ne contient pas `.git`. Fournir le SHA pour reproduire. |
| Date | 31 août 2026 |
| Runtime | .NET 10 · Orleans **10.2.1** · EF Core **9.0.8** (épinglé : Pomelo n'a pas de build EF 10) |
| Base | MySQL via Pomelo 9.0.0 / MySqlConnector 2.4.0 |
| Projets | 50 `.csproj`, dont **37 dans le périmètre Emulator** |
| Volume total | 5 457 fichiers `.cs` · 1,27 M lignes, dont **947 k de migrations générées** |
| Périmètre audité | **1 835 fichiers · 167 495 lignes** (hors migrations, Dashboard, tests, Protocol/Revisions/PacketHandlers exclus par consigne) |
| Lignes couvertes | **≈ 85 000** en lecture ou traçage de chaîne (≈ 51 %) |
| Tests | ~2 080 tests, 14 projets, ~60 k lignes |

### Méthode

L'audit s'est déroulé en deux temps, et la distinction compte pour lire les résultats.

**Passe 1 — par domaine** (findings 001 à 033). Cartographie, balayages exhaustifs d'anti-patterns sur l'ensemble du dépôt, lecture des fichiers structurants de chaque domaine. Produit des constats d'architecture.

**Passe 2 — par lot, avec lecture de fichiers** (findings 034 à 061). Douze lots, chacun avec son journal de couverture déclaré : ce qui a été lu, ce qui ne l'a pas été, et les soupçons vérifiés comme faux. Produit des exploits concrets.

Les deux passes ne trouvent pas la même chose. La première a identifié que la propriété d'un objet n'a pas de primitive unique ; la seconde a trouvé qu'on peut voler la moitié d'un échange en mintant un meuble au préalable.

### Limitations

- **Pas de compilation ni d'exécution** : aucun restore NuGet possible. Analyse statique uniquement. Les constats de performance sont **raisonnés, non mesurés**. Les seuls chiffres cités (63,2 ms au repos, 11 Hz sous charge) proviennent des commentaires du dépôt.
- **Pas d'historique Git** : impossible de dater les régressions.
- **≈ 82 000 lignes non lues**, déclarées par lot : les 55 conditions/sélecteurs wired individuels, `FurnitureWiredLogic` (907 l.), `CfhTicketService` (653 l.), les services admin de contenu, le corps des tests.
- **Deux identifiants retirés après vérification** : `030` (course d'édition NFT) et `047` (consommation d'objet) ont été rédigés comme findings puis annulés — vérification faite, les mécanismes étaient corrects. Ils sont devenus `KEEP-020` et une note de portée. La numérotation garde les trous, volontairement.

### Note sur les audits préexistants

Le dépôt contient `AUDIT-ARCHITECTURE-GLOBALE.md`, `CONSOLIDATION.md`, `ROADMAP.md` et des identifiants internes (`ORL-01`, `ORL-02`, `SEC-06`, `RFW-101`, `ADR-002`). Je ne les ai **pas lus**, délibérément, pour ne pas hériter de leurs angles morts. Une partie des constats ci-dessous y figure peut-être déjà.

---

## 2. Executive Summary

### Ce que le dépôt fait bien

Balayages exhaustifs sur les 167 495 lignes du périmètre :

| Anti-pattern | Occurrences |
|---|---|
| `async void` | **0** |
| `.Result` / `.Wait()` / `.GetAwaiter().GetResult()` | **0** |
| `NotImplementedException` | 2 |
| Lignes de code commenté | 5 |
| `[Obsolete]` | 0 |
| `Task.Run` | 10, toutes justifiées |
| Primitives de synchronisation dans les grains room | **0** |
| Dépendances circulaires entre projets | **0** |
| Logique métier égarée dans `Vortex.Primitives` | **1 fichier sur 745** |
| Accès base/inventaire/portefeuille dans `Object/Logic` | **0 sur 219 fichiers** |

Le modèle acteur Orleans est compris et appliqué. La saga économique est de niveau production. Les commentaires expliquent le *pourquoi* et gardent la mémoire des bugs corrigés.

### Bilan chiffré

**59 findings** (numérotés jusqu'à 061, deux retirés) : **2 CRITICAL · 18 HIGH · 31 MEDIUM · 8 LOW/INFO**
**45 constats `KEEP`.**

### Le diagnostic en une phrase

> **Ce qui a été raisonné dans ce dépôt est excellent ; ce qui a été supposé ne l'est pas.**

La garde anti-cycle du moteur wired distingue deux protections que la plupart des implémentations confondent. L'index unique `(product_code, serial_number)` est utilisé comme mécanisme de départage, pas comme décoration. La saga commerce écrit son reçu dans la transaction qu'il atteste. Ces décisions sont meilleures que la moyenne du secteur.

Et à côté : la taille d'une sélection de joueurs wired n'a jamais été posée comme question, donc elle est infinie. Le même index unique n'a pas été reporté sur les séries LTD. La même saga n'a pas été étendue aux neuf autres flux de valeur.

**Neuf affirmations de garantie ont été relevées dans les commentaires. Six se sont révélées fausses ou trop larges ; trois se sont vérifiées exactes.** C'est le meilleur indicateur de l'endroit où chercher.

### Verdict

**Ne pas refondre. Corriger.** L'architecture room/Orleans est fondamentalement la bonne — le grain unique par room est la bonne frontière de concurrence, le découpage en facettes est justifié, la séparation logique de mobilier / capacités est exemplaire. Les défauts sont localisés, identifiés, et réparables sans réécriture.

---

## 3. Architecture actuelle

**Modèle** : monolithe modulaire .NET 10. Hôte unique (`Vortex.Main`) assemblant ~16 modules via `AddHostPlugin<T>`. Le silo Orleans, le listener SuperSocket, la Dashboard API et la WebApi tournent **dans le même processus**.

**Rôle d'Orleans** : modèle de **concurrence et de placement** — pas de persistance.

- **Zéro `[PersistentState]` dans tout le dépôt.**
- Chaque grain réhydrate son état par requête EF Core dans `OnActivateAsync`.
- Seul `PubSubStore` est configuré, requis par les streams.
- Le code l'assume explicitement.

Choix cohérent et défendable — MySQL reste la source de vérité unique. Sa conséquence : **Orleans ne reconstruit jamais d'état ; c'est SQL qui le fait.**

**67 interfaces de grains** inventoriées :

- **~30 grains joueur** (`IGrainWithIntegerKey`, clé = playerId)
- **13 facettes room** (`IRoomCore`, `IRoomAvatars`, `IRoomMap`, `IRoomFurni`, `IRoomPets`, `IRoomWired`, `IRoomSecurity`, `IRoomSettings`, `IRoomModeration`, `IRoomTrading`, `IRoomMysteryBox`, `IRoomDoorbell`, `IRoomCrackable`) — **toutes implémentées par une seule classe `RoomGrain`**, plus `RoomPersistenceGrain`
- **15 singletons globaux** (`IGrainWithStringKey`, clé unique) — autant de points de sérialisation cluster-wide

**Réentrance** : très peu. `[AlwaysInterleave]` sur 5 méthodes seulement, toutes `[ReadOnly]`. Aucun `[Reentrant]`, aucun `[StatelessWorker]`. Usage rigoureux.

**Chemin d'un paquet entrant**
```
SuperSocket → PackageHandler (corrélation) → Vortex.Messages (dispatch, rate limit)
  → Vortex.PacketHandlers → service applicatif → grain
```

**Chemin d'une diffusion room**
```
RoomGrain → memory stream (ROOM_STREAM, clé = roomId)
  → N × PlayerPresenceGrain.OnNextAsync
  → file sortante → ISessionContextObserver → session TCP
```

C'est le choix le plus structurant : **une diffusion coûte un message de grain par destinataire.**

---

## 4. Carte des projets et dépendances

| Couche | Projets | Responsabilité |
|---|---|---|
| **Noyau sans dépendance** | `Primitives` (25 k), `Runtime`, `Specs` | Types de domaine, 67 interfaces de grains, snapshots, IDs typés |
| **Protocole** | `Protocol` (15 k), `Revisions` (27 k), `Messages`, `Networking`, `Pipeline` | Composers/parsers, sérialiseurs par révision, SuperSocket, sessions, dispatch |
| **Persistance** | `Database` (7,5 k réels), `Crypto` | 160 entités, `VortexDbContext`, `CommerceJournal`, audit, backup |
| **Domaines métier** | `Rooms` (54 k), `Players` (9 k), `Progression` (7,4 k), `Social` (5,6 k), `Catalog` (4 k), `Inventory` (2,3 k), `Fishing`, `Collectibles`, `Marketplace`, `Navigator`, `Furniture`, `Authentication` | Grains + services |
| **Transverse** | `Observability` (7,4 k), `Logging`, `Events`, `Plugins` | OTel, métriques, audit, bus d'événements, plugins |
| **Composition** | `Main`, `PacketHandlers` (26 k), `WebApi` | Hôte, handlers, endpoints |

Graphe **acyclique**, couche `Primitives` correctement isolée.

```
Primitives ──┬─→ Protocol ──→ Revisions
             ├─→ Database ──→ (tous les domaines métier)
             ├─→ Crypto
             └─→ Runtime ──→ Pipeline ──→ Events / Messages

Rooms   → Database, Events, Furniture, Logging, Runtime, Primitives, Protocol
Players → Crypto, Database, Events, Logging, Messages, Primitives, Protocol
Main    → (tout)
```

**Observation notable** : `Vortex.Rooms` ne référence **pas** `Vortex.Players`. Le couplage room ↔ joueur passe exclusivement par les interfaces de grains dans `Primitives`. **Bonne décision** — la room ne connaît le joueur que par contrat.

---

## 5. Les trois défauts d'architecture

**20 des 59 findings se regroupent en trois défauts.** Les corriger comme trois chantiers, pas comme vingt bugs.

### A. La propriété d'un objet n'a pas de primitive unique

`ECON-INV-001` · `ECON-ITM-004` · `ECON-WAL-003` · `ROOM-PER-005` · `SCHEMA-FURNI-035` · `NFT-MINT-029` · `RSYS-CONSUME-049` · `ECON-LTD-057`

La propriété d'un objet est mutée par **cinq agrégats sérialisés par cinq grains différents** :

| Chemin | Grain sérialisant | Garde |
|---|---|---|
| `RoomTradingSystem` | RoomGrain (roomId) | `ToListAsync` snapshot, sans `DeletedAt` |
| `MarketplacePurchaseGrain` | grain joueur | `ExecuteUpdate` conditionnel ✅ |
| `WiredTradeSettlement` | RoomGrain (roomId) | `ToListAsync` snapshot |
| `InventoryGrain` | grain joueur | **aucune — mutation mémoire seule** ❌ |
| `RoomPersistenceGrain` | grain par room | aucune (écriture aveugle) |

**Il n'existe aucun point de sérialisation par objet.** Partout où le dépôt écrit un `ExecuteUpdate` conditionnel avec test du `rowsAffected` — marketplace, mint NFT — c'est correct. Partout ailleurs, c'est un lire-modifier-écrire non gardé.

**La primitive correcte existe déjà dans le dépôt.** Elle n'est pas généralisée.

### B. La saga commerce couvre 2 flux de valeur sur 11

`ECON-CHEST-015` · `ECON-MINT-031` · `RSYS-PRIZE-050` · `PROG-REWARD-032` · `ECON-RENT-055` · `ECON-LTD-057`

| Couvert par la saga | Non couvert |
|---|---|
| Catalogue | Coffres wired |
| Marketplace | Mint NFT |
| | Trophée mystère |
| | Crackable |
| | Mystery box |
| | Cadeau de bienvenue |
| | Récompenses de progression |
| | Locations d'espaces |
| | Tirages LTD |
| | Bons (vouchers) |

Le projet s'est doté d'un cadre transactionnel de qualité production — journal, reçus idempotents avec index unique, outbox, escalade — et l'a appliqué aux deux flux les plus visibles, pendant que **neuf autres déplacent de la valeur à côté**.

### C. Des E/S dans le tour d'un grain qui devrait rester court

`ROOM-TICK-011` · `WIRED-FANOUT-051` · `PET-TICK-044` · `INFRA-EVENT-058` · `PERS-HYD-014`

Quatre portes d'entrée distinctes vers le même symptôme — la room se fige :

1. Le tick appelle des grains externes (`RoomPersistenceGrain`) et le flush pets ouvre un `DbContext`.
2. Les actions wired à valeur bouclent sans borne sur les joueurs sélectionnés, avec une E/S par joueur.
3. Chaque publication d'événement métier est **awaitée** depuis le tour du grain.
4. La réhydratation de room fait 5 requêtes séquentielles dont un roster de guilde non paginé.

### Motif transversal — aucune enveloppe de ressources par joueur

`RSVC-CREATE-053` · `ROOMG-WIRED-037` · `WIRED-SCHED-017`

Trois chemins par lesquels un client fait croître un stockage serveur **sans borne** : lignes `rooms`, lignes `wired_permanent_variables`, entrées du scheduler en mémoire. Le projet borne soigneusement ce qu'il a pensé à borner et ne borne rien de ce qu'il n'a pas pensé à borner.

---

## 6. Findings — CRITICAL

### `ECON-INV-001` — L'`InventoryGrain` ne persiste rien : duplication de mobilier reproductible

**Persistence · Economy · Domain** — Portée cross-cutting — **Priorité : immédiatement**

`Vortex.Inventory/Grains/Modules/InventoryFurniModule.cs:48-66` · `Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs:118-138`

```csharp
public Task<bool> RemoveFurnitureAsync(RoomObjectId itemId, CancellationToken ct)
{
    if (!_state.FurnitureById.Remove(itemId, out IFurnitureItem? item))
        return Task.FromResult(false);
    return Task.FromResult(true);
}
```

Aucun accès base. La source de vérité de l'inventaire est la requête du chargeur :
`PlayerEntityId == p && RoomEntityId == null && WiredChestEntityId == null && DeletedAt == null`.

**Chaîne vérifiée en cinq maillons indépendants :**

1. `InventoryFurniModule` ne touche pas la base — lecture intégrale.
2. `MarketplacePurchaseGrain` ne référence **jamais** `DbSet<FurnitureEntity>` — grep exhaustif ; ses deux seuls `ExecuteUpdateAsync` portent sur `MarketplaceOffers`.
3. `GrantFurnitureDefinitionCopiesAsync` fait `new FurnitureEntity` — il **crée une nouvelle ligne**.
4. Les quatre prédicats du chargeur sont exactement ceux que la ligne conserve.
5. `GrainCollectionAge = 2 minutes`.

**Exploitation** : lister un objet rare → attendre 2 min ou se reconnecter → l'objet est de retour dans l'inventaire → le trader, le placer, ou le relister. Un acheteur achète l'offre → une nouvelle ligne est créée. **Deux objets là où il y en avait un.** Sans concurrence, sans timing.

Le commentaire du code affirme l'inverse — *« A marketplace offer holds no furniture row of its own between listing and delivery: the item exists as the offer »* — et c'est cette croyance qui a produit le bug.

**Chemins d'exploitation secondaires** : `ECON-CHEST-016` (mettre en gage dans un coffre wired un objet déjà en vente) et `NFT-MINT-029` (mint).

**Correction** — Immédiat : `ExecuteUpdateAsync` conditionnel avec test du `rowsAffected`. Structurel : remplacer les trois colonnes nullables par une colonne d'emplacement explicite avec contrainte.

Le modèle correct existe déjà : le retrait de coffre (`WiredTradeSettlement.Withdrawals.cs:196-232`) écrit en base **puis** recharge le cache.

---

### `ORL-DIR-002` — `RoomDirectoryGrain` : singleton cluster, fan-out non borné, cycle d'appels

**Orleans · Concurrency · Performance** — Portée module → architecture — **Priorité : avant production**

`Vortex.Rooms/Grains/RoomDirectoryGrain.cs`

Grain `[KeepAlive]`, `IGrainWithStringKey` à clé unique → **une activation pour tout le cluster**. Reçoit chaque activation/désactivation de room, chaque entrée/sortie de joueur, et chaque `GetRoomPopulationAsync()`. Toutes les 5 minutes, `CheckRoomsAsync` fait un `Task.WhenAll` sur **toutes** les rooms actives.

**Trois défauts cumulés :**

1. **Point de sérialisation unique** — toutes les entrées de room du hôtel passent par une file de tours non réentrante.
2. **Fan-out non borné** — à 10 000 rooms, 10 000 appels sortants simultanés, et le tour du directory reste bloqué pendant tout le `WhenAll`. Toutes les entrées/sorties du cluster gèlent.
3. **Cycle d'appels** — `RoomGrain.GetRoomPopulationAsync()` → directory ; `directory.CheckRoomsAsync` → `RoomGrain.DelayRoomDeactivationAsync()`. Deux grains non réentrants s'appelant mutuellement : deadlock résolu par le timeout Orleans (30 s).

Signal corroborant : la métrique `RoomDirectoryCallCompleted` existe déjà. Le point chaud est soupçonné.

**Correction** — (a) faire porter la population par le `RoomGrain` (il connaît ses occupants) ; (b) sharder par bucket de roomId ; (c) supprimer le sweep, redondant avec `DelayDeactivation` ; (d) s'il est conservé, le borner par lots et le rendre `[OneWay]`.

---

## 7. Findings — HIGH

### `ECON-WAL-003` — Débit du portefeuille sans garde SQL
`PlayerWalletGrain.ProcessDebitRequestAsync:330-370` — Lecture snapshot non verrouillante, test en C#, puis `entity.Amount -= changedBy`. **Aucun jeton de concurrence dans tout le schéma** (vérifié). La correction repose entièrement sur l'invariant « le wallet grain est le seul écrivain » — vrai aujourd'hui, garanti par rien. Tombe sur une activation double transitoire.
**Correction** : `UPDATE ... SET amount = amount - @cost WHERE id = @id AND amount >= @cost`, `rowsAffected == 0` ⇒ solde insuffisant.

### `ECON-ITM-004` — Aucun verrou par objet
Cinq chemins, cinq grains, gardes en lecture snapshot sans `FOR UPDATE`. Voir §5.A.
**Correction** : primitive partagée `TryClaimAsync(itemIds, from, to)` en `ExecuteUpdate` conditionnel.

### `ROOM-PER-005` — Course inter-rooms : perte de mobilier
Un `RoomPersistenceGrain` par room, flush à 2 s. Ramasser en room A puis poser en room B en moins de 2 s : B écrit `RoomEntityId = B` à t+1 s, A écrit `NULL` à t+2 s. **Deux grains écrivent la même ligne sans ordre garanti.** L'objet disparaît.

### `NET-PRE-006` — File sortante définitivement bloquée
`PlayerPresenceGrain.ProcessOutgoingQueueAsync:189-208` — pas de `try/finally`. Une levée de `SendComposerAsync` laisse `_isProcessingQueue = true` pour la durée de l'activation. **Le joueur reste connecté et simulé, mais son client ne reçoit plus rien.** Correction : une ligne. **Priorité : immédiatement.**

### `NET-PRE-007` — Abandon du paquet le plus ancien
Le protocole est ordonné et cumulatif. Jeter un `ObjectAdd` en conservant un `ObjectUpdate` qui le référence désynchronise le client **définitivement et silencieusement**.
**Correction** : traiter le dépassement comme une condition fatale de session.

### `DIST-BGS-008` — MONO-SILO : 12 `BackgroundService` en N exemplaires

| Service | Conséquence multi-silo |
|---|---|
| `CommerceRelayService` | Republication d'événements économiques ×N |
| `DatabaseBackupScheduler` | **N `mysqldump` simultanés** |
| 4 × seeders | Courses de seed au démarrage |
| `ForensicsRetentionService` | Suppressions concurrentes |
| `ErrorGroupingWriterService` | Course sur création de groupes |
| `ClubMetricsRefreshService`, `RoomPerformanceAggregator` | Agrégation silo-locale présentée comme globale |

Vérifié : `GetUnrelayedAsync` + `MarkRelayedAsync` **sans réclamation atomique**.
**Correction** : grains singletons avec `RegisterGrainTimer`, ou bail par ligne.

### `DIST-CACHE-009` — MONO-SILO : caches et annuaire de sessions silo-locaux
`CatalogPurchaseGrain` lit les prix via un singleton DI dont `ReloadAsync` ne recharge que le processus appelant → **les autres silos vendent à l'ancien prix indéfiniment**. `SessionGateway` : la détection de double-login ne fonctionne pas entre silos, et `RegisterSessionObserverAsync` écrase l'observateur précédent sans contrôle.

### `ECON-CHEST-015` — Coffres wired : mobilier et crédits sans journal
`SettleContractAsync` débite le portefeuille, déplace des lignes, ajuste `chest.Credits` — **sans `ICommerceJournal`, sans reçu, sans outbox**. La compensation n'existe que dans un `catch`. Sans reçu, un rejeu n'est pas idempotent.

### `WIRED-SCHED-017` — File d'exécutions wired non bornée
`WiredExecutionScheduler` — `_pending`/`_schedule` sans plafond, alors que la file d'événements (512) et les budgets par tick (64) en ont un. Si 64 événements planifient chacun 2 exécutions et que 64 sont drainées, croissance nette de **~1 280 entrées/seconde**. Chaque entrée retient `Stack`, `Actions`, `Selected`, `SelectorPool`, `ProcessingContext`. **Épuisement mémoire depuis une seule room, constructible par un joueur.**

### `TEST-MOCK-022` — Les tests vérifient les interactions, pas les invariants
Toute la famille marketplace assert sur des listes alimentées par un **faux grain d'inventaire**, jamais sur la table `furniture` :
```
MarketplaceListingTests.cs:63     _removed.Should().Equal([ITEM_ID]);
MarketplaceListingTests.cs:89     _removed.Should().BeEmpty();
MarketplaceWindowTests.cs:121,141,149
MarketplaceClaimRaceTests.cs:129,182
```
Ce sont exactement les tests censés couvrir le flux porteur de `ECON-INV-001`, et **chacun simule le composant défaillant**. Ratio : 88 fichiers de test utilisent des faux, 49 une base réelle. Le ratio n'est pas le problème — la frontière est mal placée sur les flux de propriété.
**Correction** : pour tout flux qui déplace de la valeur, assertion sur l'**état persisté**. Test canonique manquant : *« après une mise en vente, `LoadByPlayerIdAsync` ne renvoie plus l'objet »*.

### `SOC-FANOUT-027` — Fan-out de présence messenger non borné
À chaque connexion/déconnexion, **un appel de grain par ami**, sans borne. Une liste Habbo atteint 1 100 entrées. `MessengerGrain.OnActivateAsync` fait `HydrateAsync` (**une requête base**) *et* enregistre un timer — et l'appel active le grain de **chaque ami, même hors ligne**.
**Une connexion = jusqu'à 1 100 activations, 1 100 requêtes SQL, 1 100 timers.** Sur un redémarrage à 500 reconnexions : jusqu'à 500 000 activations en quelques secondes. C'est très probablement l'explication du chiffre que `RoomConfig` documente lui-même : *« a run during a login storm managed 11 [Hz] »*.

### `NFT-MINT-029` — La garde de mint s'appuie sur un cache non fiable
`PlayerMintGrain.cs:207-232` — la réclamation abandonne délibérément la condition `RoomEntityId IS NULL`, avec ce commentaire : *« The real guard is the inventory snapshot read above »*. Or ce « snapshot » est le `Dictionary` en mémoire dont `ECON-INV-001` établit qu'il n'est pas synchronisé. Le diagnostic qui a motivé le choix est juste (c'est `ROOM-PER-005` qui rendait la garde SQL inutilisable) ; **le remplacement n'est pas sain**.

### `SCHEMA-SOFTDEL-034` — Soft-delete universel sans filtre global
**153 des 160 entités** héritent de `VortexEntity`, qui porte `DeletedAt`. Et il n'existe **aucun `HasQueryFilter`**. Mesure : sur 661 requêtes LINQ, **415 (62 %) ne filtrent pas `DeletedAt`**.
*Honnêteté sur le chiffre* : 415 est une borne haute — insertions, vues admin, accès par clé primaire n'en ont pas besoin. Le point structurel tient : la convention est tenue par discipline, 415 fois.
**Correction** : `HasQueryFilter` global + `IgnoreQueryFilters()` explicite. Inverse la charge de la preuve.

### `SCHEMA-FURNI-035` — 12 chemins voient les objets supprimés
Dont `RoomTradingSystem.cs:598` et `:602` — l'échange de propriété. `IsOfferPersistable` vérifie `PlayerEntityId` et `RoomEntityId`, **pas `DeletedAt`**.

**Chaîne d'exploitation** (croise trois lots) :
1. Le joueur minte un meuble en Relic. `NFT-MINT-029` pose `DeletedAt`, laisse `PlayerEntityId`.
2. Le cache de l'`InventoryGrain` contient encore l'objet (`ECON-INV-001`).
3. Il l'engage dans un échange. `ValidateOfferAsync` lit ce cache → passe.
4. `IsOfferPersistable` ne teste pas `DeletedAt` → passe.
5. L'échange commet.
6. Le chargeur de la contrepartie, lui, **filtre** `DeletedAt` → l'objet n'apparaît jamais chez elle.

**Le joueur garde le Relic et encaisse la contrepartie. L'autre reçoit un fantôme.**

### `ROOMG-WIRED-037` — Écriture arbitraire des variables wired persistantes
`RoomGrain.Wired.PermanentVariables.cs:83-137` · `WiredSetObjectVariableValueMessageHandler.cs`

`SetPermanentVariableAsync` ne prend **aucun acteur** et ne fait **aucun contrôle**. Le handler ne vérifie que `ctx.PlayerId > 0 && ctx.RoomId > 0`.

Le point décisif : **la requête n'est pas cadrée par la room.**
```csharp
WHERE TargetType == @t AND TargetId == @id AND VariableId == @v
```
Le grain est obtenu par `GetRoomWired(ctx.RoomId)`, mais cette identité **ne contraint jamais l'écriture**. `targetType` et `targetId` viennent du paquet, et `WiredVariableTargetType.User = 1`.

**Impact** :
1. Tout joueur connecté peut écrire ou **supprimer** (hard delete) la variable de n'importe quelle cible du hôtel.
2. Ces variables alimentent `WiredConditionVariableValue`, `WiredConditionHasVariable`, `WiredConditionVariableAge`, `WiredSelectorItemsWithVariable` — qui gardent des chaînes dont les actions incluent `KickUser`, `MuteUser`, téléporteurs et logique de coffres. **Écrire la variable d'un tiers, c'est forger la garde d'une chaîne wired le concernant.**
3. `VariableId` est une chaîne libre : rien ne borne le nombre de variables distinctes créables. Saturation de stockage.

### `ROOMM-TILE-041` — Coordonnées client non bornées, indexation directe
Chaîne complète vérifiée :
```
MoveAvatarMessage.TargetX/TargetY  (int bruts du réseau)
  → MoveAvatarMessageHandler.cs:39
  → RoomService.ClickTileAsync      (ne vérifie que PlayerId > 0 && RoomId > 0)
  → RoomMapModule.ClickTileAsync:
        int idx = ToIdx(x, y);                       // y * Width + x, aucune validation
        RoomTileFlags tile = _state.TileFlags[idx];  // indexation directe d'un tableau nu
```
C'est le paquet le plus émis du jeu.

1. Hors plage → `IndexOutOfRangeException`. Paquet trivial à forger, coût d'exception + log à chaque envoi.
2. **Repliement d'indice** : `x = Width, y = 3` dans une room large de 10 donne `idx 40` = tuile `(0,4)`. Le joueur déclenche le listener **d'une tuile qu'il n'a jamais cliquée**, depuis n'importe où. Les déclencheurs wired de tuile deviennent forgeables.
3. `y * Width + x` avec un grand `y` → dépassement d'entier.

**L'ironie** : ce piège exact est décrit et **correctement traité 40 lignes plus haut**, dans `GetTileIdForSize`, avec le contre-exemple chiffré en commentaire.
*Atténuation* : `WalkAvatarToAsync`, appelé juste après, valide. Le **déplacement** est sain ; c'est la publication de l'événement de clic qui ne l'est pas.
**Correction** : `if (!InBounds(x, y)) return;` — une ligne.

### `RSYS-CRACK-048` — Le crackable se détruit sans rien donner
```csharp
if (!await ConsumeItemAsync(...)) return;                       // l.88
PrizeEntrySnapshot? prize = await ...PickAsync(...);            // l.98
if (prize is null) { LogWarning(...); return; }                 // l.103
```
Si `PickAsync` renvoie `null` — pool vide, `PoolCode` mal configuré, grain indisponible — **le crackable est détruit et rien n'est accordé**. Perte sèche, signalée par une ligne de log serveur et rien côté client.

Le commentaire justifie l'ordre (*« the reverse order would let a repeated click … mint prizes »*) : raisonnement juste, conclusion trop large. Le **tirage** n'a aucun effet de bord ; seule la **remise** devait suivre la consommation.

**Trois contre-exemples dans le même dépôt** — trophée mystère, cadeau de bienvenue, bon — tirent le lot avant de consommer. Le crackable est le seul des quatre à faire l'inverse. Ce n'est pas un choix de conception, c'est une divergence.

### `WIRED-FANOUT-051` — Fan-out wired non borné, déclenchable par contenu utilisateur
Les **trois** actions wired porteuses de valeur ont la même boucle non bornée sur `SelectedPlayerIds` :
```
WiredActionGiveCurrencyFromChest.cs:124   → PayOutChestCreditsAsync
WiredActionGiveFurniFromChest.cs:133      → PayOutChestItemsAsync
WiredActionInitiateTransaction.cs:159     → transaction par joueur
```
La variante **mobilier** est la pire. `PayOutWiredChestItemsAsync` fait, par joueur : `CreateDbContextAsync` → requête → mutation de propriété → insertion au registre → `SaveChangesAsync` → **`ReloadFurnitureAsync`** (rechargement intégral de l'inventaire) → poussée client.

Une boîte « donner du mobilier à tous les utilisateurs » dans une room de 100 joueurs = **100 × (écriture + SELECT complet d'inventaire + appel de grain + push)**, séquentiellement, dans le tour du grain.

`WiredSelectorMaxAreaSize = 100` et `WiredSelectedItemsLimit = 20` bornent les **meubles**. Rien ne borne la sélection de **joueurs**.

**Premier défaut de performance déclenchable par contenu utilisateur.** Reclasse `ROOM-TICK-011` : ce n'est plus une limite de capacité, c'est une surface d'abus.

### `RSVC-CREATE-053` — Création de room sans aucune validation
`CreateFlatMessageHandler` ne valide **rien** — pas même `ctx.PlayerId > 0`. Le service non plus.

1. **Aucun quota de rooms par joueur.** Vérifié par recherche exhaustive. Et **le protocole expose déjà la limite** : `CanCreateRoomMessageComposer` porte un champ `RoomLimit`, sérialisé. Le contrat annonce une limite que le serveur ne calcule jamais.
2. **Aucune limite de débit spécifique.** Le rate limiting est global par session, pas par type de message. À 10 paquets/s : ~36 000 rooms/heure.
3. **`maxPlayers` du paquet, jamais borné.** Sert de porte d'entrée : `isFull = PlayersMax > 0 && avatars >= PlayersMax`. Un `maxPlayers` négatif ou nul **désactive la limite de population**.
4. `categoryId` non vérifié → échec FK → exception non gérée sur un paquet forgeable.
5. Le nom n'est pas passé au filtre de mots, alors que le dépôt en possède un.

### `PLAYER-TIME-054` — Heure locale dans la logique de frontière de journée
Balayage exhaustif : **10 occurrences de `DateTime.Now`** hors tests, dont 8 dans trois grains — alors que **tout le reste utilise `UtcNow`**, y compris toutes les colonnes d'horodatage.

```csharp
// RespectBudget.TryConsume, appelé avec DateTime.Now
DateTime today = now.Date;
int given = resetDate?.Date == today ? givenToday : 0;
// PlayerQuestGrain.cs:542
DateTime today = DateTime.Now.Date;
```

1. **Horodatages mixtes** — `AcceptedAt`/`CompletedAt = DateTime.Now` dans des colonnes dont les autres écrivains utilisent `UtcNow`. Sur un serveur UTC+2, les horodatages de quêtes sont 2 h en avance.
2. **Frontière de journée mobile** — les remises à zéro (respects, quêtes) tombent à minuit **local**, qui se déplace deux fois par an. Ce sont les compteurs les plus intéressants à farmer.
3. **Divergence multi-silo** — le grain migre ; s'il se réactive sur un silo d'un autre fuseau, la date saute.

**Périmètre précisé** : `PlayerDailyTaskGrain` et `CommunityGoalGrain` utilisent `UtcNow` partout. Cela **aggrave** le constat — le sous-système Progression est incohérent **avec lui-même** : deux fichiers voisins, tous deux porteurs de compteurs quotidiens, sans la même définition de « aujourd'hui ».

**L'ironie** : `Program.cs` épingle explicitement `CultureInfo.InvariantCulture` avec la justification *« The protocol is culture-free, the host it runs on is not »*. Le même raisonnement s'applique mot pour mot au fuseau horaire, et n'a pas été fait.

### `INFRA-EVENT-058` — Chaque événement métier prolonge le tour du grain
`EventRegistry.cs:42-43` — `HandlerMode = Parallel`, `MaxHandlerDegreeOfParallelism = null`.

Le problème n'est pas le parallélisme : c'est que **`PublishAsync` est awaité depuis l'intérieur des tours de grain** (`RoomGrain`, `PlayerWalletGrain`, `InventoryGrain`, `RoomTradingSystem`, `PlayerPresenceGrain`).

**Poser un meuble bloque la room** jusqu'à ce que progression de quête, succès, audit et forensique aient répondu — plusieurs ouvrant un `DbContext`. C'est `ROOM-TICK-011` par une autre porte, et celle-ci s'ouvre sur presque **toutes les actions joueur**.

Charge mesurée : 4 à 9 handlers sur les événements chauds. Ce n'est pas une explosion de threads, c'est de la latence systématique.

---

## 8. Findings — MEDIUM

| ID | Constat | Correction |
|---|---|---|
| `ROOM-GOD-010` | `RoomGrain` : 13 facettes, 23 dépendances, 25 sous-systèmes, 6 532 l. sur 29 partiels. Couplage inter-modules total et non typé. **Le grain unique est la bonne frontière — ne pas le découper.** | `RoomContext` + interfaces internes étroites par capacité. Le grain reste un. |
| `ROOM-TICK-011` | Tick non réentrant ; appels de grains externes et `DbContext` atteignables. `RoomConfig` documente 63,2 ms au repos et **11 Hz sous tempête de connexions**. | Interdire tout appel sortant synchrone dans le tick. |
| `ORL-STREAM-012` | Memory streams pour le fan-out : non durables, réservés au dev par la doc Orleans. 1 message de grain **par destinataire**. | Multi-silo : agréger par silo. |
| `PERS-RCP-013` | `commerce_receipts` / `commerce_operations` sans rétention. Millions de lignes/mois. | Purge des `Completed` ; rétention longue pour `NeedsIntervention`. |
| `PERS-HYD-014` | Réhydratation : 5 requêtes séquentielles dont `GroupMembers.ToListAsync()` **sans pagination**. | Paralléliser ; roster à la demande. |
| `ECON-CHEST-016` | Un objet en vente satisfait la clause de mise en gage des coffres (mêmes 4 prédicats). | Résolu par `ECON-INV-001`. |
| `CHEST-BOUND-018` | **Aucune limite de capacité de coffre wired.** Tout le contenu chargé à chaque opération, dans le tour du grain. | Plafond + pagination. |
| `PERF-INV-019` | `ReloadFurnitureAsync` (rechargement intégral) appelé sur 4 sites. Pour 50 000 objets, une requête complète par clic. | Delta, après Phase 1. |
| `PERF-PATH-020` | A* : `PriorityQueue` + `Dictionary` + `HashSet` + jusqu'à 4 096 `Node` **classe** par appel. L'algorithme est **correct**. | `Node[]` de structs pré-alloué avec tampon de génération. |
| `BUG-PATH-021` | `MaxPathNodes = 4096` < tuiles d'une grande room (76×76 = 5 776). Au-delà, `FindPath` retourne `[]` **silencieusement**. | Aligner sur `Width*Height`, ou métrique à la saturation. |
| `OBS-RT-023` | Pas d'`AddRuntimeInstrumentation()` : **GC, thread pool, métriques runtime non exportés**. | `OpenTelemetry.Instrumentation.Runtime`. |
| `PERS-IDX-024` | Index FK simples présents ; **aucun index composite** pour la requête d'inventaire à 4 prédicats. | Index couvrant `(player_id, room_id, wired_chest_id, deleted_at)`. |
| `ARCH-PRIM-025` | 745 fichiers référencés par tous : toute modification de contrat recompile la solution. | Scinder en `Vortex.<Domaine>.Contracts`. |
| `ARCH-MAIN-026` | `Vortex.Main` référence `Benchmark` et `LoadGen` en production. | Retirer ou conditionner. |
| `OBS-QUEUE-028` | Aucune profondeur de file exposée : événements wired, `_pending` scheduler, file sortante présence, `_dirtyItems`. Les quatre files dont la saturation cause les symptômes ci-dessus. | Une jauge par file. |
| `ECON-MINT-031` | L'achat de jetons de mint débite **sans `CommerceOperationId`** ; compensation en `catch` seul. | Brancher sur `ICommerceJournal`. |
| `PROG-REWARD-032` | Récompenses de quêtes/succès/tâches versées **sans identifiant d'opération**. Le drapeau est sauvegardé **avant** le versement (bonne direction), mais une perte est **silencieuse**. | Outbox ou reçu idempotent. |
| `SCHEMA-CASCADE-036` | 131 FK en cascade, dont **67 vers `PlayerEntity`**. Dormantes tant qu'on soft-delete — jusqu'au premier hard delete. Symétriquement : un joueur soft-supprimé garde ses lignes dans 67 tables pour toujours. | `Restrict` par défaut (comme le fait déjà `PluginDbContextBase`). |
| `ROOMG-GATE-038` | Point d'application des permissions incohérent : 3 méthodes délèguent au handler. Or ce sont des membres d'interfaces de grains **publiques**, appelables depuis n'importe où. Le contrôle du handler n'est pas une frontière de sécurité. | Le grain est la frontière : toute méthode mutante prend un acteur et se garde. |
| `ROOMG-WIREDMOD-039` | `KickUserFromWiredAsync` / `MuteUserFromWiredAsync` exposées sans acteur. La docstring décrit une convention d'appel que le type ne garantit pas. Appelants actuels vérifiés : internes. Risque **latent**. | Sortir de l'interface de grain, garder sur `IRoomFurniAccess`. |
| `ROOMM-ORIGIN-042` | `ActionOrigin.System = 0` — **valeur par défaut** d'un `readonly record struct`. `System` court-circuite tout (`GetControllerLevelAsync` → `Moderator` sans consulter les permissions). **Vérifié : toutes les constructions fixent `Origin`. Aucune faille active.** Piège de conception : la valeur la plus privilégiée est la valeur par défaut d'un struct qui traverse les frontières de grains. | `None = 0` (refus), `System = 1`. |
| `PET-TICK-044` | `FlushDirtyPetsAsync` s'exécute **dans le tick**, ouvre un `DbContext`, `SaveChangesAsync`. Le mobilier a résolu ce problème avec `RoomPersistenceGrain` ; les pets non. Fréquence 60 s → pic périodique. | `PetPersistenceGrain`, ou généraliser l'existant. |
| `PET-ALLOC-045` | `OrderBy(...).ToArray()` à chaque tick pet (500 ms). À comparer avec `RoomAvatarTickSystem` : zéro LINQ. | Liste d'ids triée, invalidée à l'ajout/retrait. |
| `RSYS-CONSUME-049` | `ConsumeItemAsync` soft-supprime par `Attach` aveugle, sans `WHERE DeletedAt IS NULL`. La garde (retrait de `ItemsById`) est **20 lignes et 2 awaits après** l'écriture. **Vérifié : les 3 méthodes `[AlwaysInterleave]` sont `[ReadOnly]`, aucun chemin de réentrance actif.** Mais rendre `RoomGrain` `[Reentrant]` est l'optimisation naturelle face à `ROOM-TICK-011` — ce jour-là, ce code duplique des lots. | `ExecuteUpdateAsync` conditionnel + `rowsAffected`. |
| `RSYS-PRIZE-050` | Quatre chemins de lots consomment du mobilier et créditent **sans journal**. | Chantier unique avec `ECON-CHEST-015`. |
| `WIRED-ALLOC-052` | Trois `WiredSelectionSet` alloués et copiés **par action exécutée**, plus le contexte. Jusqu'à 64 × 4 objets, 20 fois/seconde/room. | Copy-on-write ou pooling. |
| `ECON-RENT-055` | La location appelle `ExecutePurchaseAsync` **sans `CommerceOperationId`**. Septième famille hors saga. | Chantier unique. |
| `ECON-LTD-057` | Attribution de série LTD en lire-modifier-écrire, et **aucun index unique sur `(SeriesEntityId, SerialNumber)`**. **Contre-exemple dans le même dépôt** : `NftAssetEntity` porte exactement ce filet, et `PlayerMintGrain` s'en remet explicitement à lui. Le raisonnement a été mené une fois, correctement, et n'a pas été reporté sur le mécanisme de rareté le plus valorisé d'un hôtel Habbo. | Index unique + `ExecuteUpdate` conditionnel. |
| `PRIM-SESSION-061` | `AccountSessionStore` : table de sessions **en mémoire de processus**, partagée Dashboard + WebApi. Deux instances derrière un répartiteur → jeton émis par A invalide sur B, silencieusement. | Store partagé le jour où plus d'une instance sert du HTTP. |

---

## 9. Findings — LOW / INFO

| ID | Constat |
|---|---|
| `NAV-POP-033` | Le Navigator interroge la base directement (12 `CreateDbContextAsync`, tous avec `Take(limit)`) et **ne consulte jamais** `RoomDirectoryGrain`. Bon pour la charge (voir `KEEP-025`), mais les populations affichées ne viennent pas de la source vivante. |
| `ROOMG-SOFTDEL-040` | `RoomRights` et `WiredPermanentVariables` supprimées par `Remove()` — hard delete — alors qu'elles héritent de `VortexEntity` et que les requêtes amont filtrent `DeletedAt`. Convention appliquée en lecture, ignorée en écriture. Perte d'auditabilité sur l'attribution de droits. |
| `ROOMM-TILECALL-043` | Reste à auditer tous les `ToIdx()` suivis d'une indexation directe de `TileFlags` / `TileFloorStacks` / `TileEncodedHeights`, pour vérifier qu'aucun autre chemin client ne reproduit `ROOMM-TILE-041`. **Non fait.** |
| `PET-DOC-046` | `RoomPetSystem.Placement.cs:149` affirme *« same as furniture position uses the timer-flush pattern »*. **Faux** — le mobilier flush depuis un grain séparé, les pets en ligne. C'est vraisemblablement cette croyance qui a fait considérer `PET-TICK-044` comme résolu. |
| `RENT-TIMER-056` | Chaque espace louable enregistre un timer de grain dont le délai vaut la durée de location — potentiellement plusieurs jours. **Question non tranchée par lecture statique** : un timer actif prolonge-t-il l'activation ? Si oui, 10 000 espaces = 10 000 activations résidentes. L'expiration fonctionnelle ne dépend pas du timer (les 3 chemins de lecture testent `RentedUntil > now`). **À mesurer ; je ne l'affirme pas.** |
| `INFRA-AWAIT-059` | 16 `ConfigureAwait(false)` dans le chemin de publication. Dans un tour de grain, cela sort la continuation vers le pool de threads. **Non tranchable statiquement** : si un handler touche l'état du grain émetteur, c'est une course. L'échantillon lu suggère que non. `EnvelopeHost` est une brique générique où `ConfigureAwait(false)` est le choix correct — la correction est côté appelant. |
| `GAME-CAP-060` | `TryConsumeUse` traite `cap <= 0` comme illimité. Une boîte wired configurée à 0 laisse créditer sans limite, et `AddScore` additionne des `int` — un score entretenu déborde. Sans conséquence économique (le score n'est pas convertible). |
| *(nom de room)* | `rooms.name` est `varchar(512)`, diffusé dans chaque résultat de navigateur, là où Habbo en utilise ~60. Amplification ×8, sans gravité. |

---

## 10. Points forts à conserver — `KEEP`

L'ampleur des findings ne doit pas laisser croire à un projet à réécrire. **45 constats.**

### Économie et transactions
- **`KEEP-001`** — **La saga commerce.** `CommerceJournal` + `commerce_receipts` avec index unique `(operation_id, step_key)` **utilisé comme mécanisme** — l'échec d'insertion *est* la détection de rejeu. Reçu écrit dans la **même transaction** que la mutation. Outbox pour les événements dus. Escalade des opérations bloquées. Meilleur que la quasi-totalité des émulateurs. **Les corrections `ECON-*` s'y branchent, ne le remplacent pas.**
- **`KEEP-002`** — Ordonnancement de la saga d'achat « du moins au plus irréversible », pivot identifié en commentaire, règle « rien après le pivot ne rembourse ».
- **`KEEP-003`** — Revente marketplace : `ExecuteUpdateAsync` conditionnel avec vérification du `rowsAffected`. **Exactement la primitive qui manque partout ailleurs.**
- **`KEEP-004`** — Commit de trade : transaction unique couvrant mobilier **et** actifs NFT, ledger dans la même transaction. *« Une chaise et un Relic doivent être tout ou rien. »*
- **`KEEP-010`** — Plafond de quantité d'achat avec analyse du dépassement d'entier (prix négatif → biens gratuits). Refus plutôt que clamp.
- **`KEEP-020`** — **La course d'édition NFT est correctement traitée.** Deux joueurs visant la dernière copie lisent le même total, calculent le même `SerialNumber`, et l'index unique départage ; la conversion du perdant est annulée. Le commentaire décrit exactement ce mécanisme.
- **`KEEP-021`** — `CommerceReplayGuard.FirstDeliveryAsync` dans les consommateurs quêtes et tâches : la livraison at-least-once du relais est **effectivement dédupliquée en aval**, avec des clés distinctes par consommateur.
- **`KEEP-026`** — `VoucherGrain` : réclamation **avant** le crédit, ordre documenté comme délibéré.
- **`KEEP-027`** — `LtdRaffleGrain` refuse les entrées quand le stock est épuisé plutôt que d'encaisser puis d'échouer (*« silently losing their credits »*).
- **`KEEP-028`** — `RentableSpaceGrain` charge le meuble **avant** le débit, et lève plutôt que de tomber silencieusement — deux régressions corrigées et documentées.
- **`KEEP-029`** — `PayOutWiredChestCreditsAsync` relit le coffre à chaque appel : pas de duplication en mode « donner tout ». Compensation présente si le crédit n'atterrit pas.

### Room runtime
- **`KEEP-005`** — Boucle de tick : timer one-shot réarmé sur grille d'époque (évite la dérive de phase), `AdvanceBoundaryPast` en O(1), isolation et instrumentation par étape, journalisation à intervalle, `finally` garantissant le réarmement.
- **`KEEP-006`** — `RoomAvatarTickSystem` : **zéro LINQ, zéro allocation de liste** dans le chemin le plus chaud.
- **`KEEP-008`** — Chemin de retrait de coffre : écriture base **puis** rechargement du cache. **Le modèle correct, déjà présent.**
- **`KEEP-009`** — Drain de `RoomPersistenceGrain.OnDeactivateAsync` : borné par le **progrès**, pas par un compteur.
- **`KEEP-030`** — `RoomPersistenceGrain` retire de la file *après* le `SaveChanges` réussi.
- **`KEEP-031`** — `GetTileIdForSize` borne sur les **coordonnées**, pas sur l'indice aplati, avec le contre-exemple chiffré. Classe de bug subtile, correctement traitée.
- **`KEEP-032`** — `InBounds(x,y)` en comparaison non signée : un seul test, gère le négatif.
- **`KEEP-033`** — Le tick **bots** est propre : aucune E/S base sur le chemin périodique. Exactement ce que `PET-TICK-044` réclame pour les pets, déjà atteint dans le même dossier.
- **`KEEP-034`** — `ProcessPetsAsync` isole chaque pet dans un `try/catch` avec son id. `ApplyOfflineDecay` au chargement évite le pet immortel après redémarrage.
- **`KEEP-035`** — `RoomSecurityModule` sépare décision et application : résolution déléguée à des **politiques pures** (`RoomSecurityPolicy`, `RoomModerationPolicy`, `ModerationPolicy`). Testable sans room, sans base, sans silo.
- **`KEEP-036`** — La modération staff n'implique **pas** de droits de construction — séparation explicite et commentée. Distinction que beaucoup d'émulateurs ratent.
- **`KEEP-037`** — `RateRoomAsync` : check-then-insert atomique par construction (une activation par room), exclut le propriétaire.
- **`KEEP-038`** — Les 11 méthodes de `RoomGrain.Settings` passent **toutes** par `IsRoomOwnerAsync`. Aucune omission.
- **`KEEP-039`** — Les 3 méthodes `[AlwaysInterleave]` sont toutes `[ReadOnly]`. Usage rigoureux de la réentrance.

### Moteur wired
- **`KEEP-007`** — `WiredExecutionScheduler` : versionnage des entrées pour la suppression paresseuse dans un `PriorityQueue`. Raisonnement juste et rare.
- **`KEEP-040`** — **`WiredCallChainGuard` : deux protections distinctes et explicitement non confondues** — un ensemble de tuiles qui rend un cycle *impossible*, et une limite de profondeur qui borne le coût d'une chaîne large mais légitime. La plupart des implémentations confondent les deux en un compteur.
- **`KEEP-041`** — Les deux protections sont **comptées** avec un motif nommé (`DEPTH`, `CYCLE`, `REVALIDATION`, `QUEUE_DROP`). *« Why did my wired stop is otherwise unanswerable from outside the room. »* Observabilité pensée depuis le support.
- **`KEEP-042`** — Re-validation de co-localisation à l'exécution, avec le refus écrit dans le log de room de la partie. Le commentaire nomme le rapport de bug résolu.
- **`KEEP-043`** — `CurrentExecutionId` / `ParentExecutionId` portés sur chaque ligne de log : un journal plat devient une chronologie traçable.
- **`KEEP-044`** — Le commentaire de `WiredMaxDepth` documente une correction de dette exemplaire : la propriété affichait 20, le système appliquait 8. Le défaut a été aligné sur le **comportement réel** — *« sinon ce serait un changement de comportement silencieux déguisé en refactoring »*.

### Structure et couches
- **`KEEP-045`** — **Séparation des couches exemplaire.** Sur les **219 fichiers** de logique de mobilier, **aucun** ne touche `VortexDbContext`, `IInventoryGrain` ni `IPlayerWalletGrain`. Toute mutation passe par des interfaces de capacité étroites. **C'est l'exact inverse de `ROOM-GOD-010` — le modèle que la refonte doit généraliser.**
- **`KEEP-046`** — **744 fichiers sur 745** de `Vortex.Primitives` sont de purs contrats. Le projet de contrats est réellement un projet de contrats.
- **`KEEP-018`** — Pagination correcte dans `Vortex.Social` (`Skip`/`Take`), forums, historique messenger. Contraste net avec `PERS-HYD-014`.
- **`KEEP-024`** — `Vortex.Fishing` : aucun `ToListAsync` non borné, aucun dictionnaire d'instance non borné, un seul timer. Le domaine le plus propre du dépôt.
- **`KEEP-047`** — `ClaimWelcomeGiftAsync` : tirage avant consommation, rien n'est consommé, unicité garantie par `GrantOnceAsync` qui vérifie **en base** puis insère. **Persisté**, donc survit à la désactivation du grain.

### Infrastructure
- **`KEEP-011`** — `IDbContextFactory` partout, `AsNoTracking()` systématique, `IExecutionStrategy` avec `DbContext` neuf par tentative. **Aucun N+1 détecté.**
- **`KEEP-012`** — Garde de démarrage multi-silo nommant les composants fautifs. Refuser de démarrer plutôt qu'être silencieusement incohérent.
- **`KEEP-013`** — Culture invariante forcée au démarrage, avec justification protocolaire.
- **`KEEP-014`** — Dette legacy quasi nulle : 2 `NotImplementedException`, 5 lignes commentées, 0 `[Obsolete]`. Les 127 TODO sont dans `Vortex.Protocol` (hors périmètre).
- **`KEEP-016`** — `ConfigurePullingAgent` à 10 ms avec le diagnostic exact : les 100 ms par défaut injectaient une latence aléatoire dans la cadence de marche rendue par le client.
- **`KEEP-019`** — `AssemblyLoadContext(isCollectible: true)`, déchargement via `UnloadAndWaitAsync` avec timeout de 5 s, fuite antérieure documentée.
- **`KEEP-048`** — Le hot-reload est doublement conditionné (`IsDevelopment()` **et** `HotReloadEnabled`), la première condition non contournable par configuration.
- **`KEEP-049`** — Isolation d'erreurs **par handler** dans `EnvelopeHost` : un handler qui lève ne prive pas les autres de l'événement.
- **`KEEP-050`** — `BoundedHelper.RunAsync` existe déjà pour le parallélisme borné. L'outil est là, simplement pas branché.
- **`KEEP-051`** — `TokenBucketRateLimiter` : balayage sur N appels plutôt que sur minuterie, *« so an idle process spends nothing »*.
- **`KEEP-052`** — `PluginDbContextBase` force `DeleteBehavior.Restrict` sur toutes les FK des plugins — la bonne décision, appliquée aux assemblies tierces mais pas au schéma principal.
- **`KEEP-053`** — Aucune monnaie en `decimal`/`double`/`float` : toutes les devises sont des `int`. Correct pour Habbo.
- **`KEEP-054`** — `AccountSessionStore` : jeton = 256 bits du générateur cryptographique, avec le raisonnement explicite contre le GUID (*« random but says so by accident rather than by contract »*), et la duplication supprimée documentée.
- **`KEEP-055`** — `HookHavocSimulation` n'utilise **pas** `Random` mais un PRNG déterministe : la simulation doit être rejouable à l'identique côté client.
- **`KEEP-056`** — `ModerationQueueGrain._inspectedRooms` borné par le nombre de modérateurs, retiré à la déconnexion, **et documenté comme tel**.
- **`KEEP-057`** — `RespectBudget` est une règle **pure** extraite du grain *« so the reset/limit edges can be unit-tested »*. La structure est juste — c'est l'horloge qu'on lui passe qui ne l'est pas, et c'est grâce à cette extraction que la correction tient en un paramètre.
- **`KEEP-025`** — Le Navigator interroge la base directement au lieu de passer par le directory. C'est ce qui empêche l'écran le plus sollicité d'amplifier `ORL-DIR-002`.

### Tests
- **`KEEP-015`** — `VortexClusterFixture` : `TestCluster` Orleans **réel**, avec une docstring qui explique *pourquoi* un silo est nécessaire (activation unique, exécution par tours, sérialisation) et pourquoi il ne remplace pas `GrainActivationContext`.
- **`KEEP-058`** — `CommerceFaultHarness` permet d'armer une étape **précise** d'un octroi pour qu'elle échoue, au lieu du « ça jette ou ça ne jette pas » habituel. La docstring nomme le défaut de l'ancien harnais.
- **`KEEP-059`** — `WiredPersistenceInvariantsTests` et `RentableSpaceInvariantsTests` : **la notion de test d'invariant existe déjà dans le dépôt.** Elle n'a simplement pas été appliquée à la propriété du mobilier.
- **`KEEP-017`** — De vrais tests d'idempotence commerciale (`ADebitReplayedUnderTheSameOperation_ChargesOnce`, `Unwinding_IsIdempotent`).

---

## 11. Architecture cible

Spécifique à un émulateur Habbo .NET + Orleans. **Elle diffère peu de l'existante — c'est le résultat qui compte.**

### Ce qui ne change pas
- Un grain par room comme frontière de concurrence — **c'est la bonne frontière** : occupants, carte et mobilier forment un seul domaine de cohérence.
- Un grain par joueur pour portefeuille, inventaire, présence.
- Orleans comme modèle de concurrence, MySQL comme source de vérité unique.
- La saga commerce, les facettes de grain room, la séparation logique de mobilier / capacités.

### Ce qui change
1. **Une couche de propriété d'objet unique** — `IFurnitureLedger.TryClaimAsync(...)` en `ExecuteUpdate` conditionnel. Tout déplacement passe par là. **Seul changement qui ferme définitivement la classe `ECON-*`.**
2. **Emplacement explicite en base** — `furnitures.location` + `location_ref`. Un objet a exactement un emplacement, garanti par contrainte.
3. **`InventoryGrain` = cache en lecture seule** — il observe le ledger, ne mute rien.
4. **Tick sans appel sortant synchrone** — `[OneWay]` ou file drainée hors tick.
5. **Directory shardé, population portée par la room.**
6. **Diffusion agrégée par silo.**
7. **Présence messenger par projection**, pas par activation du grain de chaque ami.
8. **Tâches périodiques globales = grains singletons.**
9. **Caches de référence invalidés par stream.**
10. **Enveloppe de ressources par joueur** — quotas de rooms, de variables wired, d'exécutions planifiées.
11. **Contrats de grains scindés par domaine.**

### Comparaison

| Axe | Actuel | Cible |
|---|---|---|
| Frontière de concurrence room | Grain unique ✅ | Inchangé |
| Facettes de grain room | 13 facettes, une activation ✅ | Inchangé |
| Logique de mobilier | Capacités étroites ✅ | Inchangé, **généralisé aux modules** |
| Propriété d'objet | 5 chemins, gardes snapshot ❌ | Ledger unique, claim conditionnel |
| Emplacement d'objet | 3 colonnes nullables implicites ❌ | Colonne explicite + contrainte |
| Inventaire | Écrivain en mémoire seule ❌ | Cache en lecture |
| Portefeuille | Read-modify-write ⚠️ | `UPDATE … WHERE amount >= cost` |
| Saga commerce | 2 flux sur 11 ❌ | 11 sur 11 |
| Diffusion room | 1 message / joueur ⚠️ | 1 message / silo |
| Présence messenger | 1 activation / ami ❌ | Projection |
| Tâches globales | `BackgroundService` ❌ | Grains singletons |
| Quotas par joueur | Aucun ❌ | Enveloppe explicite |
| Tests économiques | Mocks d'interaction ❌ | Invariants sur base réelle |

---

## 12. Roadmap de refonte

### Phase 0 — Bugs critiques et invariants *(jours)*
`NET-PRE-006` (une ligne) · `ROOMM-TILE-041` (une ligne) · `RSYS-CRACK-048` (inversion de deux appels) · `WIRED-SCHED-017` (plafond, calqué sur la file d'événements) · `BUG-PATH-021` · `ROOMG-WIRED-037` (contrôle d'accès) · **tests d'invariant sur base réelle**.

**Ordre impératif** : écrire les tests d'invariant **en premier**. Ils doivent échouer sur `ECON-INV-001`. C'est le filet de la Phase 1.

*Risque* faible · *Dépendances* aucune

### Phase 1 — Fondations de propriété *(2–4 semaines)* — **la phase qui compte**
Migration vers l'emplacement explicite · `IFurnitureLedger` · réécriture des 5 chemins · `ECON-INV-001`, `ECON-ITM-004`, `ROOM-PER-005`, `SCHEMA-FURNI-035`, `NFT-MINT-029`, `RSYS-CONSUME-049`, `ECON-WAL-003`, `ECON-LTD-057` · `SCHEMA-SOFTDEL-034` (`HasQueryFilter`) · `PERS-IDX-024`

*Risque* **élevé** (migration de données) · *Dépendances* Phase 0 · *Bénéfice* ferme la duplication

### Phase 2 — Room runtime *(2–3 semaines)*
`ROOM-TICK-011` · `WIRED-FANOUT-051` · `INFRA-EVENT-058` · `PET-TICK-044` · `ORL-DIR-002` · `SOC-FANOUT-027` · `PERF-PATH-020` · `PERS-HYD-014` · `CHEST-BOUND-018` · `PERF-INV-019` · `WIRED-ALLOC-052` · `PET-ALLOC-045`

*Dépendances* Phase 1 pour le delta d'inventaire · *Bénéfice* capacité par silo, fin des freezes

### Phase 3 — Économie / persistance *(1–2 semaines)*
`ECON-CHEST-015` · `ECON-MINT-031` · `RSYS-PRIZE-050` · `PROG-REWARD-032` · `ECON-RENT-055` — **un seul chantier** : étendre `ICommerceJournal` à toute consommation ou création d'objet. Plus `PERS-RCP-013`, `PLAYER-TIME-054`, `RSVC-CREATE-053`.

### Phase 4 — Multi-silo *(4–6 semaines)*
`DIST-BGS-008` · `DIST-CACHE-009` · `ORL-STREAM-012` · `PRIM-SESSION-061` · levée de `MultiSiloReady`

*Risque* **élevé** · **Ne pas entamer avant.** Le multi-silo amplifie chaque défaut de cohérence restant — `ECON-WAL-003` passe de « fenêtre quasi nulle » à « ouverte à chaque partition réseau ».

### Phase 5 — Performance *(2 semaines)*
**Mesurer d'abord.** `ARCH-PRIM-025` (temps de build).

### Phase 6 — Durcissement *(continu)*
`OBS-RT-023` · `OBS-QUEUE-028` · `ROOMG-GATE-038` · `ROOMG-WIREDMOD-039` · `ROOMM-ORIGIN-042` · `ROOMG-SOFTDEL-040` · `SCHEMA-CASCADE-036` · `ARCH-MAIN-026` · tests multi-silo

---

## 13. Matrice de couverture

| Domaine | Inspecté | Profondeur | Findings | Lot |
|---|---|---|---|---|
| Cartographie projets / dépendances | Oui | Complète | ARCH-PRIM-025, ARCH-MAIN-026 | P1 |
| Configuration Orleans / hosting | Oui | Profonde | ORL-STREAM-012, DIST-BGS-008 | P1 |
| Inventaire des grains (67) | Oui | Complète | ORL-DIR-002 | P1 |
| Schéma DB (160 entités) | Oui | Profonde | SCHEMA-034/035/036, PERS-IDX-024 | M |
| `RoomGrain.*` (29 partiels) | Oui | Profonde | ROOMG-037/038/039/040 | A |
| `Grains/Modules` | Oui | Profonde | ROOMM-041/042/043 | B |
| Pets & bots | Oui | Profonde | PET-044/045/046 | C |
| Systèmes room (chat, roller, box, crackable) | Oui | Profonde | RSYS-048/049/050 | E |
| `Object/Logic` (219 fichiers) | Oui | Balayage exhaustif + lecture ciblée | WIRED-FANOUT-051 | F |
| Moteur wired | Oui | Profonde | WIRED-051/052, WIRED-SCHED-017 | G |
| Services room racine | Oui | Ciblée | RSVC-CREATE-053 | H |
| Players & Social | Oui | Profonde | PLAYER-TIME-054, ECON-RENT-055, RENT-TIMER-056, SOC-FANOUT-027 | J |
| Catalog, Progression, Collectibles, Fishing | Oui | Profonde | ECON-LTD-057, NFT-MINT-029, ECON-MINT-031, PROG-REWARD-032 | K |
| Infra (Pipeline, Events, Networking, Plugins) | Oui | Profonde | INFRA-058/059 | L |
| Jeux | Oui | Ciblée | GAME-CAP-060 | D |
| `Vortex.Primitives` (745 fichiers) | Oui | Balayage exhaustif | PRIM-SESSION-061 | I |
| Suite de tests | Oui | Ciblée | TEST-MOCK-022 | N |
| Économie (wallet, catalogue, marketplace, trade) | Oui | Profonde | ECON-001/003/004/015/016 | P1 |
| Inventaire | Oui | Profonde | ECON-INV-001, PERF-INV-019 | P1 |
| Networking / sessions / présence | Oui | Profonde | NET-PRE-006/007, DIST-CACHE-009 | P1 |
| Concurrence (balayage global) | Oui | Complète | — *(résultats propres)* | P1 |
| Pathfinding / carte | Oui | Profonde | PERF-PATH-020, BUG-PATH-021 | P1/B |
| Observabilité | Oui | Moyenne | OBS-RT-023, OBS-QUEUE-028 | P1 |
| Navigator | Oui | Balayage | NAV-POP-033 | P1 |
| Modération / CFH | Oui | Balayage | — *(état borné)* | P1 |
| Code mort / legacy | Oui | Complète | — *(dette quasi nulle)* | P1 |

**Non lu (≈ 82 000 lignes)** : les 55 conditions/sélecteurs/addons wired individuels · `FurnitureWiredLogic.cs` (907 l.) · `CfhTicketService.cs` (653 l.) · `RoomModerationStore.cs` (283 l.) · les 6 Providers de room · `ContentAdminService` et les services admin de contenu · `RoomAvatarModule` au-delà du chemin de marche · `RoomChatSystem`, `RoomRollerSystem` · `NetworkManager.cs` · le corps des tests.

---

## 14. Notes de prévention — éviter cette classe de problèmes

Cette section est le vrai livrable à long terme. Les 59 findings sont datés ; ces règles ne le sont pas.

### 14.1 La règle qui aurait évité le bug critique

> **Une mutation de propriété qui ne touche pas la base n'existe pas.**

`ECON-INV-001` vient d'une méthode nommée `RemoveFurnitureAsync` qui ne retire rien de durable. Le nom promettait une persistance que le corps ne fournissait pas, et quatre appelants l'ont crue.

**Règle applicable** : toute méthode dont le nom contient `Remove`, `Add`, `Grant`, `Consume`, `Transfer`, `Claim` et qui porte sur un actif du joueur doit soit écrire en base, soit être renommée pour dire qu'elle ne le fait pas (`RemoveFromCacheAsync`). Un analyseur Roslyn maison peut faire respecter ça.

### 14.2 La primitive unique de mutation

Le dépôt possède **la bonne primitive** (`MarketplacePurchaseGrain`, `PlayerMintGrain`) et **la mauvaise** (partout ailleurs). La différence :

```csharp
// ✅ correct — la base arbitre
int claimed = await db.X.Where(x => x.Id == id && x.State == expected)
                        .ExecuteUpdateAsync(u => u.SetProperty(...), ct);
if (claimed == 0) return Failed;

// ❌ incorrect — le grain arbitre, et il n'est seul que par hypothèse
var row = await db.X.FirstOrDefaultAsync(x => x.Id == id, ct);
if (row.State != expected) return Failed;
row.State = next;
await db.SaveChangesAsync(ct);
```

**Règle** : toute transition d'état d'un actif (propriété d'objet, solde, numéro de série, état d'offre, stock) passe par une primitive partagée en `ExecuteUpdate` conditionnel avec test du `rowsAffected`. Jamais de lire-modifier-écrire, **même quand un grain garantit la sérialisation** — parce que la garantie tombe sur une double activation, et parce qu'elle n'est écrite nulle part.

**Corollaire schéma** : chaque ressource rare porte un index unique qui matérialise sa rareté. `NftAssetEntity` l'a, `LtdRaffleEntryEntity` ne l'a pas. Un index unique est un invariant que la base fait respecter même quand le code se trompe.

### 14.3 Les commentaires qui affirment une garantie

C'est l'outil de détection le plus rentable de tout cet audit. **Neuf affirmations relevées, six fausses.**

| Affirmation | Verdict |
|---|---|
| *« the item exists as the offer »* (marketplace) | ❌ → `ECON-INV-001` |
| *« The real guard is the inventory snapshot »* (mint) | ❌ → `NFT-MINT-029` |
| *« same as furniture position uses the timer-flush pattern »* (pets) | ❌ → `PET-DOC-046` |
| *« Consume first: the reverse order would let a repeated click mint prizes »* (crackable) | ⚠️ juste mais trop large → `RSYS-CRACK-048` |
| *« Called directly on the grain from inside its own turn »* (wired mod) | ⚠️ convention non garantie par le type → `ROOMG-WIREDMOD-039` |
| *« The budget resets whenever the stored reset date is not the current day »* | ⚠️ « the current day » non défini de façon stable → `PLAYER-TIME-054` |
| *« Bounds must be checked on the coordinates, not on the flattened index »* | ✅ exacte — et non appliquée ailleurs → `ROOMM-TILE-041` |
| *« the unique index on (product_code, serial_number) is what actually settles it »* | ✅ exacte |
| *« the grain's once-per-player claim is what stops this one taking it again »* | ✅ exacte, persistée en base |
| *« The tile set makes a cycle impossible »* | ✅ exacte |

**Règle** : tout commentaire de la forme *« X is what guarantees Y »*, *« nothing can Z »*, *« this is safe because… »* doit être accompagné soit d'un test qui vérifie la garantie, soit d'une contrainte de base qui la matérialise. Un commentaire de garantie non testé est une dette qui se paie plus tard, avec intérêts — parce qu'il **empêche** la relecture critique.

En revue de code : chercher ces formulations et demander « où est le test ? ».

### 14.4 La frontière du mock

`TEST-MOCK-022` explique pourquoi le bug critique a survécu à ~2 080 tests.

```csharp
// ❌ vérifie une interaction — passe même si le collaborateur est cassé
_removed.Should().Equal([ITEM_ID]);

// ✅ vérifie un invariant — échoue si le collaborateur est cassé
var stillOwned = await loader.LoadByPlayerIdAsync(sellerId, ct);
stillOwned.Should().NotContain(i => i.ItemId == ITEM_ID);
```

**Règle** : pour tout flux qui déplace de la valeur, la frontière du mock doit se situer **en dessous de la base, pas au-dessus**. On peut simuler le réseau, l'horloge, le hasard. On ne simule pas le composant qui persiste.

**Trois invariants à écrire une fois et à rejouer sur chaque flux** :
1. La somme des objets par emplacement est constante à travers toute opération.
2. Aucun objet n'est simultanément dans deux emplacements.
3. La masse monétaire ne varie que par les mouvements journalisés.

Le dépôt sait déjà faire ça (`WiredPersistenceInvariantsTests`, `RentableSpaceInvariantsTests`). Il faut l'étendre.

### 14.5 Les bornes qu'on oublie de poser

Le motif est net : **le projet borne soigneusement ce qu'il a pensé à borner et ne borne rien d'autre.**

Bornes présentes : profondeur de chaîne wired, file d'événements wired, budgets par tick, sélection de meubles, nœuds de pathfinding, taille de lot de flush.
Bornes absentes : file d'exécutions wired, sélection de **joueurs**, capacité de coffre, nombre de rooms par joueur, nombre de variables wired, amis notifiés à la connexion, roster de guilde chargé.

**Règle** : pour toute collection alimentée directement ou indirectement par un client, se poser trois questions à l'écriture :
1. Qui décide de sa taille ? Si la réponse est « le joueur », il faut un plafond.
2. Que se passe-t-il au plafond ? Rejet, éviction, ou erreur — mais jamais un silence.
3. Le plafond est-il **compté** ? Une borne atteinte sans métrique est un incident invisible.

Le moteur wired fait exactement ça pour sa file d'événements — `Diagnostics.ChainStopped(QUEUE_DROP)`. C'est le modèle à copier partout.

### 14.6 Ne rien exécuter de long dans un tour de grain

Quatre findings, une cause. Un tour de grain Orleans est **exclusif** : tout ce qui s'y passe bloque tous les autres messages du même grain.

**Règle** : dans le corps d'un grain, sont interdits sur les chemins chauds :
- l'ouverture d'un `DbContext` (sauf activation) ;
- un appel de grain **awaité** dont le résultat n'est pas nécessaire à la réponse ;
- toute boucle dont la longueur dépend d'une entrée client.

Les effets de bord sortants partent en `[OneWay]`, ou dans une file drainée par un timer, ou dans un grain de persistance dédié. **Le dépôt a déjà fait ce travail pour le mobilier** (`RoomPersistenceGrain`) — c'est le modèle.

### 14.7 Frontières de sécurité : le grain, pas le handler

`ROOMG-WIRED-037` est arrivé parce que le contrôle d'accès a été placé dans le handler de paquet pour trois méthodes, et oublié pour la quatrième.

**Règle** : une méthode d'interface de grain est **publique à l'échelle du cluster**. Le contrôle du handler est une optimisation, jamais la protection. Toute méthode mutante prend un `ActionContext` et se garde elle-même.

**Corollaire** : la valeur par défaut d'un enum d'autorisation doit être **le refus**. `ActionOrigin.System = 0` fait de la valeur la plus privilégiée le défaut d'un struct sérialisé entre silos (`ROOMM-ORIGIN-042`).

### 14.8 Une seule horloge

`PLAYER-TIME-054` : deux fichiers voisins, deux définitions de « aujourd'hui ».

**Règle** : `DateTime.Now` est interdit dans le code serveur. Un analyseur peut l'imposer (`BannedApiAnalyzers` avec `BannedSymbols.txt`). Si un fuseau « hôtel » est nécessaire pour les frontières de journée, il est **configuré une fois** et appliqué au même endroit pour tous les compteurs.

Le dépôt a déjà eu ce raisonnement pour la culture (`CultureInfo.InvariantCulture` épinglé au démarrage, avec la justification). Il suffisait de l'étendre.

### 14.9 Ce qui est étendu, ce qui ne l'est pas

Le dépôt contient plusieurs mécanismes **corrects mais partiellement appliqués** :

| Mécanisme | Appliqué à | Manquant sur |
|---|---|---|
| Saga commerce (journal + reçus) | Catalogue, marketplace | 9 autres flux de valeur |
| `ExecuteUpdate` conditionnel | Marketplace, mint NFT | Trade, coffres, wallet, LTD, consume |
| Index unique de rareté | `NftAssetEntity` | `LtdRaffleEntryEntity` |
| Grain de persistance dédié | Mobilier | Pets |
| `DateTime.UtcNow` | ~99 % du dépôt | 3 grains |
| Tests d'invariant | Wired, espaces louables | Propriété du mobilier |
| Capacités étroites | `Object/Logic` (219 fichiers) | Modules internes du grain |
| Pagination | `Vortex.Social` | Roster de guilde à l'hydratation |

**Règle de revue** : quand on introduit un mécanisme transversal, écrire dans le même commit **la liste des endroits qui devraient l'utiliser et ne l'utilisent pas encore**, dans un fichier suivi. Sans cette liste, l'extension n'arrive jamais — parce que personne ne sait qu'elle manque.

### 14.10 Méthode d'audit, pour la prochaine fois

Ce qui a marché, à réutiliser :

1. **Balayages exhaustifs avant lecture.** Compter les `async void`, les `DateTime.Now`, les `ToListAsync` sans borne sur *tout* le dépôt donne une carte de risque en quelques minutes et évite de lire au hasard.
2. **Chercher les commentaires de garantie** (§14.3). Rendement le plus élevé de l'audit.
3. **Chercher les chemins jumeaux divergents.** `RSYS-CRACK-048` et `ECON-LTD-057` ont été trouvés parce que le dépôt contenait sa propre référence correcte à côté de la version fautive. Quand deux chemins font la même chose dans un ordre différent, l'un des deux a tort.
4. **Déclarer les soupçons levés, pas seulement les findings.** Neuf hypothèses vérifiées comme fausses figurent dans les rapports de lot. Un audit qui ne note que ce qu'il trouve donne une fausse image de sa propre fiabilité.
5. **Vérifier avant de publier.** Un balayage automatique a signalé 106 contrôles d'accès manquants ; vérification faite, **trois** étaient réels. Publier les 106 aurait été impressionnant et faux.
6. **Écrire les constats sur disque au fil de l'eau**, pas en fin de course : un audit de cette taille dépasse toute fenêtre de contexte, et les rapports de lot sont ce qui permet de reprendre.

---

*Fin du rapport. 59 findings, 45 `KEEP`, 12 rapports de lot en annexe.*
