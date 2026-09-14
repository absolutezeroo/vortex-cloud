# Vortex Cloud — Audit de préparation à la bêta

- **Révision auditée** : `04550328f8d696358d477cc835c7d86dffb2b8bc` (`main`, 2026-09-14 05:31 +0200), branche de travail `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date de l'audit** : 2026-09-14.
- **Nature** : audit technique et architectural en lecture seule du code, complété par la compilation, l'exécution de la suite de tests, des vérifications ciblées et la lecture des journaux d'intégration continue. Aucun fichier de production n'a été modifié.
- **Auteur** : Claude (session d'audit), à la demande du propriétaire du dépôt.

---

## 1. Verdict global

**Non prêt en l'état.** Le projet devient « prêt sous conditions » une fois les cinq défauts P1 corrigés, le portail qualité remis au vert et les prérequis d'exploitation P2 en place (sections 10 et 11).

Ce verdict repose sur trois faits établis, pas sur une impression :

1. **Deux contournements de permissions d'entrée en room et une perte de données joueur sont démontrés par le code** (ROOM-01, ROOM-02, ROOM-03) : un ami peut être suivi dans une room verrouillée, à mot de passe ou pleine ; un sonneur peut entrer sans réponse du propriétaire avec un paquet forgé ; supprimer une room abandonne définitivement tous les mobis qu'elle contient.
2. **Un joueur connecté mais silencieux plus de deux minutes devient un fantôme** (SES-01) : sorti de sa room, invisible pour ses amis, puis muet pour le serveur jusqu'à reconnexion. L'onglet navigateur mis en arrière-plan est le déclencheur le plus courant.
3. **Le portail qualité est rouge sur `main` depuis au moins huit commits et les pushs sont faits en `--no-verify`** (QA-01). Le filet de sécurité que le dépôt s'est construit ne protège plus rien tant que ce n'est pas réparé.

À l'inverse, les fondations économiques sont solides et ont été vérifiées de bout en bout : débit conditionnel transactionnel, journal de commerce avec pivot explicite, claims conditionnels sur la propriété des mobis (échange, marketplace, placement, ramassage), autorisations de modération présentes dans 23 handlers sur 23. La suite de tests (3 869 tests, 0 échec) et la compilation sont saines. Le problème de la bêta n'est pas la qualité générale du code, qui est élevée ; ce sont quelques trous précis dans des parcours essentiels et l'absence d'une base d'exploitation.

Ce que l'audit **n'a pas pu valider** et qui pèse sur le verdict : aucune base MySQL n'était disponible (pas d'exécution de l'émulateur, pas de migration appliquée, pas de test de charge), les sources du client officiel ne sont pas à côté du dépôt (la vérification de dérive de format wire est aveugle), et les huit agents de domaine prévus ont été interrompus par une limite d'API : l'audit a été mené manuellement, avec une couverture explicitement partielle sur plusieurs domaines (section 4).

---

## 2. Contexte, environnement et hypothèses

| Élément | Valeur constatée |
|---|---|
| Stack | .NET SDK 10.0 (`global.json`, `rollForward: latestFeature`), C# `net10.0`, Orleans 10.2.1, EF Core 9.0.8 + Pomelo MySQL 9.0.0 (pin délibéré, `AGENTS.md`), SuperSocket 2.1.0, ASP.NET Core minimal APIs, Svelte/Vite (dashboard) |
| Taille | 63 projets dans `Vortex.Cloud.sln` ; ~480 000 lignes C# hors migrations (Rooms 66 k, Dashboard.API 50 k, Primitives 33 k, Revisions 28 k, PacketHandlers 27 k) ; 302 fichiers de migration ; 557 handlers de paquets ; 54 grains Orleans ; 1 240 parsers et 1 143 fichiers de protocole |
| Tests | 18 projets de tests, 3 869 tests exécutés localement, 0 échec (section 9) |
| Environnement d'audit | Conteneur Linux sans SDK .NET préinstallé : SDK 10.0.401 installé dans `/root/.dotnet` ; Node 22.22 ; **aucun serveur MySQL** ; daemon Docker inaccessible ; pas de sources AS3 ni d'émulateurs de référence à côté du dépôt |
| Historique git | 50 commits visibles (12→14 septembre 2026), historique réimporté : `docs/codebase/.documentation-state.json` référence un commit absent de l'historique local |
| Hypothèse de déploiement | Thèse **mono-silo** assumée par le dépôt (`CONTEXT.md`, `VortexEmulator.RefuseAnUndeclaredSecondSiloAsync`, `Vortex.Main/VortexEmulator.cs:36-70`) : caches, agrégateurs et streams en mémoire sont locaux au processus. L'audit évalue la bêta sur un seul nœud |
| Hypothèse client | Client cible WIN63-202607011411 (`docs/completeness/STATE.yaml`), transport TCP (Flash) et WebSocket (navigateur) ; chiffrement RSA-1024/DH-384/RC4 imposé par le client |
| Périmètre exclu | Le plugin d'exemple (`../turbo-sample-plugin`, hors dépôt), le contenu des specs Habbo (3 923 fichiers générés, validés mais non lus), les jeux de room (Freeze/Banzai/Football) et les animaux au-delà de leur structure |

Conventions de confiance utilisées dans tout le document :

- **Confirmé** : démontré par le code lu (fichier:ligne cité) ou reproduit par une commande.
- **À vérifier** : risque argumenté dont une condition (runtime, base, client) n'a pas pu être vérifiée dans cet environnement.
- **Amélioration** : proposition de conception avec un bénéfice explicite, sans défaut démontré.

Priorités : P0 critique, P1 bloquant bêta, P2 important, P3 mineur (définitions de la demande d'audit).

---

## 3. Cartographie

### 3.1 Composants et responsabilités

| Couche | Projets | Rôle vérifié dans le code |
|---|---|---|
| Hôte | `Vortex.Main` | Composition root (`Program.cs`) : culture invariante, configuration `VORTEX__`, `AddOrleans` (`Extensions/HostApplicationBuilderExtensions.cs`), 21 modules `AddHostPlugin<T>`, migrations optionnelles au démarrage (`Program.cs:198`), `VortexEmulator` (chargement des données de référence par étages puis ouverture du réseau), commandes console |
| Réseau | `Vortex.Networking`, `Vortex.Crypto` | Deux hôtes SuperSocket (TCP 30000, WS 30001) construits par `NetworkManager` ; `SessionGateway` (dictionnaires session↔joueur, observer Orleans par session) ; `ClientPacketDecoder` (RC4, borne 64 Ko) ; `PackageHandler` (parser de la révision → `MessageSystem`) ; handshake RSA/DH → RC4 |
| Pipeline | `Vortex.Messages`, `Vortex.Pipeline`, `Vortex.Runtime` | `MessageSystem.PublishAsync` (scope de trace, métriques, résolution de la room active par appel grain) → `MessageRegistry` (`EnvelopeHost`, contexte par paquet, handlers en parallèle) ; `RateLimitBehavior` (token bucket par session) ; chargement d'assemblies/plugins |
| Protocole | `Vortex.Protocol`, `Vortex.Revisions`, `Vortex.Primitives/Messages` | Primitives de lecture (`ClientPacket`), révision embarquée `Revision20260701` (parsers, sérialiseurs, `Headers.cs`), contrats de messages |
| Handlers | `Vortex.PacketHandlers` (43 domaines) | Orchestration : garde d'entrée, appel de grain via `GrainFactoryExtensions`, réponse via `ctx.SendComposerAsync` ou `PlayerPresenceGrain.SendComposerAsync` |
| Domaines | `Vortex.Players`, `Rooms`, `Catalog`, `Inventory`, `Marketplace`, `Social`, `Progression`, `RewardTracks`, `Habbicons`, `Collectibles`, `Fishing`, `Navigator`, `Furniture`, `Authentication`, `Shop` | 54 grains + services ; état vivant en mémoire dans les grains, persistance EF Core par `IDbContextFactory<VortexDbContext>` (aucun `[PersistentState]` Orleans) |
| Persistance | `Vortex.Database` | `VortexDbContext` (31 dossiers d'entités), 302 migrations, filtre global de soft delete, `CommerceJournal` (opérations/reçus), sauvegarde `mysqldump`, audit des changements d'entités (dashboard) |
| Surfaces HTTP | `Vortex.WebApi` (site : inscription, connexion, ticket SSO, avatars, articles, `/health`, `/metrics`), `Vortex.Dashboard.API` + `Vortex.Dashboard.Web` (plan de contrôle opérateur : sessions serveur, MFA, capabilities, audit), `Vortex.Supervisor` (processus superviseur à jeton) | Hôtes Kestrel séparés (8080 / 9000 / 5250), conteneurs DI séparés |
| Observabilité | `Vortex.Observability`, `Vortex.Logging` | Métriques `System.Diagnostics.Metrics` (28 instruments Vortex) exposées Prometheus, contexte de corrélation, regroupement d'erreurs, agrégateurs live, détection d'incidents, audit ; journal console uniquement |
| Outillage | `Vortex.Specs`, `Vortex.Specs.Cli`, `Vortex.Benchmark`, `Vortex.LoadGen`, `scripts/hooks/*` | Specs comportementales générées, micro-benchmarks, générateur de charge hors processus, sept contrôles hors compilateur (capabilities, registre d'en-têtes, murs d'architecture, groupes logiques, conflits wire, hooks) |

### 3.2 Grains Orleans (54) et propriété des états

| Famille | Grains | Clé | Durée de vie |
|---|---|---|---|
| Joueur | `PlayerGrain` (profil, modération, garde-robe, préférences), `PlayerPresenceGrain` (routage session ↔ room, file sortante), `PlayerWalletGrain`, `PlayerClothingGrain`, `PlayerEffectGrain`, `PlayerNavigatorGrain`, `PlayerMysteryBoxGrain`, `InventoryGrain`, `MessengerGrain`, `PlayerQuizGrain`, `Player*` (achievements, badges, quêtes, tâches, sondages, prix, reward tracks, habbicons, fishing, NFT, vault, mint) | id joueur | Orleans (`GrainCollectionAge` = 2 min, `OrleansHostConfig.cs:23`) |
| Room | `RoomGrain` (8 200 lignes en 38 partials + 15 modules + 30 systèmes), `RoomPersistenceGrain` (write-behind 2 s), `RoomDirectoryGrain` (`[KeepAlive]`, balayage 5 min) | id room | `DelayDeactivation` 30 min à chaque entrée, `DeactivateOnIdle` par le balayage quand la population est nulle (`RoomGrain.cs:228-235`, `RoomDirectoryGrain.cs:177`) |
| Commerce | `CatalogPurchaseGrain` (par joueur), `LtdRaffleGrain` (par série), `VoucherGrain` (par code), `MarketplacePurchaseGrain`, `MarketplaceSearchGrain`, `PlayerTargetedOfferGrain`, `TargetedOfferManagerGrain` | joueur / série / code | Orleans |
| Singletons `[KeepAlive]` (16) | `PlayerDirectoryGrain`, `RoomDirectoryGrain`, `ModerationQueueGrain`, `GuideDirectoryGrain`, managers de référence (quêtes, achievements, sondages, prix, communauté, NFT, fishing, mystery box, offres ciblées) | `global` | vie du silo |
| Social | `GroupGrain`, `GroupDirectoryGrain`, `GroupForumGrain`, `MessengerGrain` | id groupe / joueur | Orleans |

Concurrence : aucun `[Reentrant]`, six méthodes interleavées consignées dans `docs/architecture-v4/interleaving-manifest.yaml` (2 × `IPlayerPresenceGrain.SendComposerAsync`, 4 lectures pures sur `IRoomCore`), asserties par `Vortex.Hosting.Tests/Architecture/InterleavingManifestTests.cs`. Aucun verrou manuel, aucun `.Result`/`.Wait()`, aucun `ConfigureAwait(false)` dans les grains (vérifié par grep, section 9).

### 3.3 Flux principaux (tels qu'implémentés)

1. **Connexion et authentification** : socket accepté → `SessionGateway.AddSessionAsync` (observer Orleans créé) → `ClientHello`/`InitDiffie`/`CompleteDiffie` (RC4 activé) → `SSOTicket` → `AuthenticationService.GetPlayerIdFromTicketAsync` (ticket en DB, TTL 30 s glissant) → ban vérifié → `SessionGateway.AddSessionToPlayerAsync` (ancienne session du joueur fermée, `PlayerPresenceGrain.RegisterSessionObserverAsync`) → 15 lectures de grains en parallèle → séquence de composers de login. Pendant tout ce handler `ctx.PlayerId` vaut −1 (contexte construit avant le binding).
2. **Dispatch d'un paquet** : `PackageHandler.HandleCoreAsync` → parser de la révision → `MessageSystem.PublishAsync` (**un appel `GetActiveRoomAsync` au grain de présence par paquet** pour les métriques, `MessageSystem.cs:50,85`) → `MessageRegistry` (contexte `PlayerId` = −1 si non authentifié, `MessageRegistry.cs:48`) → `RateLimitBehavior` (50 paquets/s, burst 100, par session) → handler(s).
3. **Entrée en room** : `OpenFlatConnection` → `RoomService.OpenRoomForPlayerIdAsync` (`RoomService.cs:60-150`) : ban de room (DB) → `ClearActiveRoomAsync` → `SetPendingRoomAsync(roomId, approved: true)` (**avant** les contrôles, ligne 93) → `EnsureRoomActiveAsync` → room pleine / mot de passe (pour un non-ayant droit) → porte verrouillée : `RegisterDoorbellRingAsync` et retour → sinon `CompleteRoomEntryAsync` : événement annulable, raid protection, `RoomReady` + heightmaps + mobis + avatars envoyés, puis **en dernier** `PlayerPresenceGrain.SetActiveRoomAsync` (directory, abonnement au stream `RoomStream/<id>`, `RoomGrain.CreateAvatarFromPlayerAsync`).
4. **Sortie de la room vers les clients** : `RoomGrain.SendComposerToRoomAsync` → stream mémoire Orleans → `PlayerPresenceGrain.OnNextAsync` de chaque occupant → file bornée (500) → observer → socket. Une room n'écrit jamais sur un socket.
5. **Achat catalogue** : `PurchaseFromCatalog` → `CatalogPurchaseGrain.PurchaseOfferFromCatalogAsync` (`CatalogPurchaseGrain.cs:45-244`) : quantité bornée (`:62`), offre résolue côté serveur, prix recalculé, événement annulable, journal `Prepared` → `WalletPurchaseExtensions.ExecutePurchaseAsync` → `PlayerWalletGrain.TryDebitAsync` (stratégie d'exécution + transaction + `UPDATE ... WHERE Amount >= cost`, `PlayerWalletGrain.cs:66-135,335-400`) → `InventoryGrain.GrantCatalogOfferAsync` (**pivot** = un seul `SaveChangesAsync`, `InventoryGrain.Furni.cs:161-260`, ADR-001) → `Pivoted` → `CompleteWithRelayAsync` (événement écrit avec la transition terminale, relayé par `CommerceRelayService` si la publication échoue).
6. **Mobis** : placement = `CanManipulateFurniAsync` ou espace loué → claim conditionnel `RoomFurnitureLocationStore.ClaimIntoRoomAsync` → état room → retrait de la vue inventaire (`RoomActionModule.Floor.cs:26-130`) ; ramassage = `GetFurniPickupTypeAsync` (≥ GroupAdmin, retour au propriétaire) → `ReleaseFromRoomAsync` conditionnel → vue inventaire (`RoomActionModule.cs:36-110`) ; positions/rotations/extra data en write-behind 2 s par `RoomPersistenceGrain` (fenêtre de perte documentée dans `docs/architecture-v4/persistence-loss-window.md`).
7. **Échange** : `RoomTradingSystem` → `TryPersistOwnershipSwapAsync` (`RoomTradingSystem.cs:566-660`) : stratégie d'exécution + transaction, `ClaimForTradeAsync` conditionnel (propriétaire, hors room, hors coffre wired, hors jukebox, non supprimé), comptes vérifiés, Relics NFT dans la même transaction avec ligne de ledger.
8. **Marketplace** : listing = ligne d'offre `PendingRemoval` → retrait de la vue → **claim conditionnel soft-delete** de la ligne de mobi (`MarketplacePurchaseGrain.cs:147-200`) → `Active`. Le « known issue » consigné dans `docs/codebase/06-economy/marketplace.md` (ligne non retirée durablement) est **corrigé à HEAD**.
9. **Arrêt** : `VortexEmulator.StopAsync` arrête les deux hôtes réseau (5 s chacun) ; Orleans désactive ensuite les grains (`RoomGrain.OnDeactivateAsync` : arrêt des jeux, drain des mobis et des animaux, retrait du directory ; `PlayerGrain.OnDeactivateAsync` : écriture du profil). `HostOptions.ShutdownTimeout` n'est pas configuré (30 s par défaut).

---

## 4. Couverture de l'audit

Les agents de domaine prévus (huit chercheurs spécialisés du dépôt) ont tous été interrompus par une limite de session de l'API avant d'avoir lu un seul fichier. L'audit a donc été mené manuellement, en priorisant les parcours essentiels et les invariants économiques. La matrice ci-dessous dit précisément ce qui a été lu.

| Domaine | Statut | Ce qui a été analysé | Ce qui ne l'a pas été |
|---|---|---|---|
| Runtime Orleans | **Analysé** (partiel sur les 54 grains) | Configuration silo, `PlayerPresenceGrain` (5 partials), `RoomGrain.cs` (activation, tick, désactivation, hydratation), `RoomPersistenceGrain`, `RoomDirectoryGrain`, `PlayerGrain` (désactivation, écriture), `PlayerWalletGrain`, `CatalogPurchaseGrain`, `VoucherGrain` (rédemption), `MarketplacePurchaseGrain` (listing, redeem), manifeste d'interleaving, grep verrous/blocages/`.Ignore()` | `MessengerGrain` (bornes seulement), `GroupGrain`, `ModerationQueueGrain`, `LtdRaffleGrain`, grains de progression/collectibles/fishing/habbicons (structure seulement) |
| Connexions et sessions | **Analysé** | `SessionGateway`, `SessionContext`/`WebSocketSessionContext`, `NetworkManager` (heartbeat WS), `PackageHandler`, `ClientPacketDecoder`, `MessageSystem`, `MessageRegistry`, `RateLimitBehavior`/`TokenBucketRateLimiter`, `EnvelopeHost` (dispatch), handshake (6 handlers), `SSOTicketMessageHandler`, `AuthenticationService`, génération de ticket WebApi, `DiffieService` | Options SuperSocket (défauts non surchargés), contrôle d'Origin WS (absent du code : à vérifier côté proxy), `PackageEncoder` lu mais non testé |
| Rooms et gameplay | **Partiel** | Entrée/sortie (service + grain + présence), doorbell, `FollowFriend`, autorisations mobis (place/move/pickup/wired), `RoomSecurityModule` (niveau de contrôle, pickup), settings/suppression de room, `RoomChatSystem` (flood, longueur), `RoomGrain.FloorPlan` (bornes), `RoomConfig` (budgets wired, limites), `RoomTradingSystem` (settlement), `RoomFurnitureLocationStore` | Moteur wired (`Vortex.Rooms/Wired/**`, 141 fichiers), jeux (`Games/**`, 64 fichiers), animaux et bots (30 systèmes), rollers, jukebox, crackables, mystery box, raid protection interne, 105 handlers Room (échantillon de 8) |
| Économie et inventaire | **Analysé** sur les parcours essentiels | Achat catalogue de bout en bout, wallet (débit, crédit unique), inventaire (grant, vue/durable), marketplace (listing, redeem), échange, bons, journal et relais de commerce, ADR-001/002, matrice d'acceptation V4 | Cadeaux, LTD, offres ciblées, club, vault, mystery box, crafting, NFT/mint, reward tracks, habbicons, fishing (tests existants lus dans la matrice d'acceptation, code non relu), boutique site (`Vortex.Shop`, désactivée par défaut : signature HMAC et tolérance vérifiées par grep) |
| Persistance | **Partiel** | Enregistrement EF (pool, retry, AutoDetect), transactions et stratégies d'exécution (3 sites), index uniques du snapshot (tickets, reçus, bons, badges, noms), filtre global soft delete, `MigrationHelper`, sauvegarde, comptage des migrations destructives/SQL brut, fenêtre de perte documentée | Lecture des 302 migrations, cohérence snapshot/modèle (`has-pending-model-changes` non exécuté : pas de base), longueurs de colonnes exhaustives, requêtes N+1 sur les chemins chauds, comportement MySQL réel (tests sur SQLite/InMemory) |
| Sécurité et surfaces HTTP | **Partiel** | Dashboard : authentification, politiques par capability, cookie, CSP, rate limit login, MFA step-up, Swagger ; WebApi : rate limits, cookie, CORS, inscription (nom, unicité, plafond d'avatars), sessions ; `/metrics` (loopback ou jeton en temps constant) ; superviseur (jeton validé, temps constant) ; validateurs de secrets (`CHANGE_ME`) ; hachage BCrypt (facteur 12, `Task.Run`) | Enumération exhaustive des endpoints dashboard (répartis dans des partials : seuls le fichier racine et un fichier de comptes lus), IDOR endpoint par endpoint, sanitation des articles, upload/traversal d'assets, dépendances front-end |
| Protocole et handlers | **Partiel** | Primitives `ClientPacket` (bornes), grep des 1 240 parsers (préallocation, boucles), 22 parsers bornés par la taille de paquet, `RateLimitConfig`, 40 handlers sans garde d'authentification (liste), 27 handlers agissant sur un id de room client (autorisation vérifiée pour settings/suppression), 23 handlers modérateur (capabilities), motto, `unknowns --severity critical` (127), baselines d'en-têtes et de conflits wire | Lecture des sérialiseurs (placeholders), classification des 61 conflits wire baselinés (les sources AS3 sont absentes), handlers Help/CFH, GroupForums, Camera, Navigator (au-delà des limites `Take`) |
| Exploitation et qualité | **Analysé** | Démarrage (migrations optionnelles, AutoDetect, validateurs), arrêt, Dockerfile (non-root, healthcheck), compose (dev), CI (`quality.yml`, 8 derniers runs, logs du run 34803028084), gate locale (csharpier, 5 contrôles node, hooks, specs, npm lint/test), scan de vulnérabilités, `/health`, métriques, tracing, journalisation, sauvegarde, `LoadGen` (rôle) | Exécution de l'émulateur, migrations sur MySQL, test de charge, restauration de sauvegarde, plugins (chargement/déchargement non exercé), supervision réelle |

---

## 5. Synthèse des risques

### 5.1 Ce qui bloque l'ouverture (P1)

| Risque | Cause racine | Effet joueur |
|---|---|---|
| Un joueur silencieux devient un fantôme (SES-01) | Le grain de présence est le seul lien entre socket et monde, mais rien ne le maintient actif : il est collecté après 2 min sans message, et sa désactivation défait la room et l'observer | Sorti de room sans le savoir, absent pour ses amis, puis toutes les réponses du serveur perdues jusqu'à reconnexion |
| Entrée en room sans passer par la porte (ROOM-01, ROOM-02) | Les gardes d'entrée (ban, pleine, mot de passe, sonnette, raid, événement annulable, capacité) vivent dans `RoomService`, pas dans le grain ; deux chemins appellent le grain directement | Rooms privées ouvertes aux amis et aux clients modifiés ; rooms pleines dépassées ; avatars fantômes |
| Suppression de room = perte des mobis (ROOM-03) | Soft delete de la room sans retour des mobis à l'inventaire | Un joueur qui supprime une room perd tout ce qu'elle contenait, définitivement |
| Portail qualité contourné (QA-01) | Trois contrôles cassés (registre d'en-têtes en mode plafond, conflits wire aveugles, auto-tests des hooks) rendent la gate rouge ; les commits partent en `--no-verify` | Aucune régression n'est plus arrêtée avant `main` |

### 5.2 Ce qui doit être maîtrisé pendant la bêta (P2)

- **Abus réseau** : pas de garde d'authentification centrale (40 handlers sans garde, requêtes DB déclenchables avant SSO), rate limit par session seulement, pas de plafond par IP ni de délai avant SSO, deux parsers allouant depuis un compteur client (SES-02, NET-01, PROTO-01).
- **Tickets SSO rejouables** dans une fenêtre glissante de 30 s, non liés à l'IP à l'usage (AUTH-01).
- **États incohérents à l'entrée** quand la création d'avatar échoue : population fantôme, room qui tique à 20 Hz sans fin (ROOM-04).
- **Exploitation** : sauvegardes désactivées et jamais restaurées, journaux console seulement, reprise commerce post-pivot manuelle sans runbook, aucun déploiement de production défini, TLS à terminer devant le WebSocket (OPS-01 à OPS-04).
- **Persistance** : 302 migrations dont 121 destructives, aucune lane de test sur MySQL réel, rollback = restauration (DB-01).
- **Dépendance vulnérable** dans quatre projets de tests qui fera échouer l'étape CI dédiée dès que la gate passera (QA-02).

### 5.3 Ce qui est solide (à préserver)

- Argent et objets : débit conditionnel transactionnel, pivot journalisé, claims conditionnels sur cinq chemins de propriété, reçus idempotents par index unique, relais d'événements at-least-once avec déduplication côté consommateurs (matrice d'acceptation V4 vérifiée contre le code pour le catalogue, la marketplace et l'échange).
- Modération et droits : 23/23 handlers modérateur vérifient une capability ; settings, suppression, placement, ramassage et wired vérifient le niveau de contrôle dans le grain.
- Plan de contrôle : sessions serveur révocables, cookie `HttpOnly`/`Secure`/`SameSite=Strict`, CSP, politiques par capability générées depuis la liste canonique, MFA step-up, rate limit login, audit.
- Qualité intrinsèque : 3 869 tests verts, csharpier propre, specs validées, analyzers en erreurs sur les diagnostics critiques, murs d'architecture mécanisés.

---

## 6. Tableau des constats

| ID | Titre | Catégorie | Priorité | Confiance | Références principales | Impact |
|---|---|---|---|---|---|---|
| SES-01 | Désactivation du `PlayerPresenceGrain` après 2 min d'inactivité : session vivante orpheline | Défaut | **P1** | Confirmé (code) | `PlayerPresenceGrain.cs:194-226`, `PlayerPresenceGrain.Room.cs:106-165`, `OrleansHostConfig.cs:23`, `HostApplicationBuilderExtensions.cs:95`, `MessageSystem.cs:50,85`, `SuperSocketHostBuilderExtensions.cs:107`, `NetworkingConfig.cs:39` | Joueur sorti de room, vu hors ligne, puis muet jusqu'à reconnexion |
| ROOM-01 | `FollowFriend` entre dans la room sans gardes ni payload d'entrée | Défaut | **P1** | Confirmé (code) | `FollowFriendMessageHandler.cs:84`, `RoomService.cs:60-150`, `RoomGrain.Avatar.cs:24-70`, `RoomAvatarModule.cs:91` | Contournement ban/pleine/mot de passe/sonnette/raid, avatar fantôme, client désynchronisé |
| ROOM-02 | `GetRoomEntryData` admet un sonneur sans réponse du propriétaire | Défaut | **P1** | Confirmé (code) | `RoomService.cs:93,144`, `GetRoomEntryDataMessageHandler.cs:46-50`, `header-registry-baseline.json` (id 1250) | Entrée dans une room verrouillée avec un paquet forgé |
| ROOM-03 | Suppression de room : les mobis restent rattachés à la room supprimée | Défaut | **P1** | Confirmé (code) | `RoomGrain.Settings.cs:204-266`, `InventoryFurnitureLoader.cs:64`, consommateurs de `RoomDeletedEvent` | Perte définitive des mobis d'une room supprimée |
| QA-01 | Portail qualité rouge sur `main`, pushs en `--no-verify` | Prérequis d'exploitation | **P1** | Confirmé (logs CI + reproduction locale) | runs 316→323 de `quality.yml`, `check-header-registry.mjs:191-204`, `check-wire-conflicts.mjs`, `scripts/hooks/__test/run.mjs` | Aucune régression n'est arrêtée ; la gate ne peut plus être exigée |
| SES-02 | Aucune garde d'authentification centrale dans le pipeline | Faiblesse architecturale | P2 | Confirmé (code) | `MessageRegistry.cs:48`, 40 handlers (liste §7), `RedeemVoucherMessageHandler.cs`, `VoucherGrain.cs:129-175` | Requêtes DB et activations de grains pour un id −1 avant SSO ; garde oubliée = faille |
| NET-01 | Limites d'abus réseau incomplètes (par session seulement) | Risque | P2 | Confirmé (code) | `RateLimitConfig.cs`, `TokenBucketRateLimiter.cs`, `NetworkManager.cs:242`, `appsettings.json` (`serverOptions`) | N connexions = N × 50 paquets/s ; sessions pré-SSO illimitées ; Origin WS non contrôlé |
| PROTO-01 | Préallocation depuis un compteur client | Défaut | P2 | Confirmé (code) | `AcceptFriendMessageParser.cs:14`, `DeclineFriendMessageParser.cs:22` (contre-exemple `FishingParsers.cs:63`) | Allocation jusqu'à 8 Go tentée par paquet de 8 octets |
| AUTH-01 | Ticket SSO rejouable dans la fenêtre glissante | Risque | P2 | Confirmé (code) | `AuthenticationConfig.cs:34`, `AuthenticationService.cs:29-130`, `WebApiAuthService.cs:201-250` | Vol de session par ticket observé (30 s glissants, IP non vérifiée) |
| ROOM-04 | `SetActiveRoomAsync` laisse un état incohérent si la création d'avatar échoue | Défaut | P2 | Confirmé (code) | `PlayerPresenceGrain.Room.cs:45-103`, `RoomGrain.Avatar.cs:24-70`, `RoomDirectoryGrain.cs:177` | Population fantôme, stream orphelin, room qui tique indéfiniment |
| QA-02 | Dépendance de test vulnérable (SQLitePCLRaw 2.1.10, High) | Prérequis d'exploitation | P2 | Confirmé (scan) | `Directory.Packages.props`, `quality.yml:67`, 4 projets de tests | L'étape « Scan for vulnerable packages » échouera dès que la gate passera |
| OPS-01 | Sauvegardes désactivées par défaut, aucune restauration testée | Prérequis d'exploitation | P2 | Confirmé (code + config) | `appsettings.json` (`Vortex:Database:Backup`), `DatabaseBackupService.cs`, `DatabaseBackupScheduler.cs` | Aucune reprise possible après corruption ou erreur d'opération |
| OPS-02 | Journalisation console uniquement ; volume pilotable par un client | Prérequis d'exploitation | P2 | Confirmé (code) | `Vortex.Logging/*`, `PackageHandler.cs:88` | Pas d'historique d'incident, saturation possible des journaux |
| OPS-03 | Reprise commerce post-pivot manuelle sans runbook ni action opérateur | Prérequis d'exploitation | P2 | Confirmé (code) | `CommerceRelayService.cs` (`EscalateAsync`), ADR-001 (« resuming it is not yet automatic ») | Un joueur débité sans livraison attend une intervention manuelle non documentée |
| OPS-04 | Aucun déploiement de production défini | Prérequis d'exploitation | P2 | Confirmé (dépôt) | `docker-compose.yml` (dev), `Dockerfile`, `README.md` « Real secrets outside development », `HostApplicationBuilderExtensions.cs:40-70` | Secrets, TLS WS, mode single-node, migrations : décisions non prises |
| DB-01 | Hygiène des migrations et absence de lane MySQL réelle | Risque | P2 | Confirmé (comptage) / à vérifier (MySQL) | `Vortex.Database/Migrations/*` (302, 121 destructives, 37 SQL brut), `scripts/sql/recover_half_applied_*`, tests SQLite/InMemory | Migration partielle irrécupérable, comportements MySQL non testés |
| SEC-01 | Comptes : pas de verrouillage, sessions web non révoquées au ban | Faiblesse | P2 | Confirmé (code) | `AccountAuthenticator.cs`, `WebApiAppConfigurator.cs:90-140`, `WebApiSessionStore.cs` (commentaire « Nothing calls it on a ban ») | Force brute lente possible ; visiteur banni conserve sa session web |
| PERF-01 | Un appel grain par paquet entrant pour les métriques | Faiblesse | P3 | Confirmé (code) | `MessageSystem.cs:50,85-113` | Latence et charge ajoutées sur le chemin chaud (mouvement, chat) |
| NET-02 | TCP sans heartbeat, `PongTimeout` = 0 | Risque | P3 | Confirmé (code) | `SuperSocketHostBuilderExtensions.cs:99-141`, `NetworkingConfig.cs:39` | Sessions mortes sans FIN conservées, joueurs « en ligne » à tort |
| ROOM-05 | Snapshot d'entrée envoyé avant l'abonnement au stream | Risque | P3 | À vérifier | `RoomService.cs:300-360`, `GetRoomEntryDataMessageHandler.cs:189`, `PlayerPresenceGrain.Room.cs:66-79` | Événements de room perdus pendant l'entrée (avatar fantôme, mobi manquant) |
| DB-02 | Lot de flush empoisonné par une ligne supprimée physiquement | Risque | P3 | À vérifier | `RoomPersistenceGrain.cs:130-190` | Positions d'une room plus jamais persistées après une suppression SQL directe |
| PROTO-02 | Longueurs non bornées côté serveur (motto, nom/description de room) ; garde `PlayerId < 0` | Faiblesse | P3 | Confirmé (code) | `ChangeMottoMessageHandler.cs:22`, `PlayerGrain.cs:183`, `RoomGrain.Settings.cs:51-120` | Exceptions MySQL loggées ; contenu non filtré |
| SEC-02 | Mot de passe de room en clair ; Swagger monté inconditionnellement | Faiblesse | P3 | Confirmé (code) | `RoomGrain.Settings.cs:80`, `RoomGrain.cs` (snapshot `Password`), `DashboardWebHost.cs:664-669` | Exposition en cas de fuite DB/snapshot ; surface de découverte |
| OBS-01 | Traces non exportées ; `/health` limité | Faiblesse | P3 | Confirmé (code) | `VortexTelemetry.cs:8`, `WebApiEndpoints.cs:240-275` | Diagnostic incomplet en production |
| DATA-01 | Caches et chargements non bornés | Faiblesse | P3 | Confirmé (code) | `PlayerDirectoryGrain.cs:26-27`, `InventoryFurnitureLoader.cs:64` | Mémoire proportionnelle au nombre de joueurs consultés / d'objets possédés |
| DOC-01 | Dérive des documents contractuels et des baselines | Faiblesse | P3 | Confirmé | `docs/orleans.md`, `CONTEXT.md`, `AGENTS.md` (« 23 » conflits, « 14 » ids), `README.md` (« SDK 9.x »), `wire-conflicts-baseline.json` (61), `header-registry-baseline.json` (3) | Les contrats guident les outils d'IA et les relecteurs vers des hypothèses fausses |
| SPEC-01 | 127 handlers muets (unknowns critiques) | Fonctionnalité manquante | P3 | Confirmé (CLI) | `dotnet run --project Vortex.Specs.Cli -- unknowns --severity critical` | Fonctionnalités acceptées silencieusement (caméra, compétitions, campagnes…) |
| ORL-01 | `RoomDirectoryGrain` `[KeepAlive]` sans reaper | Risque | P3 | Confirmé (code) | `RoomDirectoryGrain.cs:69-78`, `docs/codebase/03-orleans/lifecycle-concurrency.md` | Entrées orphelines jusqu'au redémarrage si une room meurt sans `OnDeactivateAsync` |

---

## 7. Analyses détaillées

Chaque constat suit le même plan : conditions et chemin d'exécution, comportement, impact, protections recherchées, reproduction ou validation, correction recommandée.

### SES-01 — Désactivation du `PlayerPresenceGrain` après deux minutes d'inactivité (P1, confirmé)

**Chemin.** `PlayerPresenceGrain` est un grain Orleans ordinaire : ni `[KeepAlive]`, ni `DelayDeactivation` (les deux seuls usages de `DelayDeactivation`/`DeactivateOnIdle` du dépôt sont dans `RoomGrain.cs:228-235`). `GrainCollectionOptions.CollectionAge` vaut 2 minutes par défaut (`OrleansHostConfig.cs:23`, appliqué dans `HostApplicationBuilderExtensions.cs:95`). Une activation sans message pendant cette durée est collectée, et `OnDeactivateAsync` (`PlayerPresenceGrain.cs:194-226`) vide la file, appelle `UnregisterSessionObserverAsync` (`:111`) qui appelle `ClearActiveRoomAsync` (`PlayerPresenceGrain.Room.cs:106-165` : retrait de l'avatar de la room, événement `PlayerLeftRoomEvent`, retrait du directory, désabonnement du stream), puis met `_sessionObserver` à `null`.

Ce qui touche ce grain en temps normal : `MessageSystem.ResolveRoomIdAsync` (`MessageSystem.cs:50,85-113`) fait un `GetActiveRoomAsync` à **chaque paquet entrant**, et tout `SendComposerAsync` adressé au joueur. Autrement dit, la vie du grain dépend du trafic du client. Or :

- le listener TCP n'a **aucun heartbeat** : `UsePingPong` n'est pas appelé (`NetworkManager.cs:242` n'existe que pour le WS) et `RunHeartbeatAsync` du builder TCP est un corps vidé (`SuperSocketHostBuilderExtensions.cs:99-141`, `return Task.CompletedTask` ligne 107) ;
- le listener WS envoie un `PING` après 30 s de silence, mais `PongTimeout` vaut 0 (`NetworkingConfig.cs:39`) et, par choix documenté, un onglet gelé qui ne répond plus n'est **pas** fermé.

**Comportement déduit.** Un client dont la socket reste ouverte mais qui n'envoie rien pendant plus de deux minutes (onglet navigateur mis en arrière-plan et gelé par Chrome, machine en veille, coupure réseau sans FIN) subit : sortie de room côté serveur (les autres occupants le voient partir), `IsOnlineAsync` = `false` (amis informés hors ligne) alors que `SessionGateway._playerToSession` le garde en ligne, puis, à la réactivation suivante (dès que le client renvoie un paquet), un grain neuf avec `_sessionObserver == null` : `ProcessOutgoingQueueAsync` ne draine rien (`PlayerPresenceGrain.cs:230-262`), la file se remplit jusqu'à 500 (`PlayerPresenceConfig.MaxOutgoingQueueSize`) puis `EnqueueOutgoing` remplace tout par un `CloseConnectionMessageComposer` (`:150-192`) qui n'est jamais livré non plus. Le client est **muet** : il envoie, le serveur traite, rien ne revient. Seul un nouveau SSO (`SessionGateway.AddSessionToPlayerAsync`, `SessionGateway.cs:117`) réenregistre un observer.

**Protections recherchées.** Aucun ré-enregistrement hors SSO ; aucun `OnActivateAsync` qui redemande l'observer au gateway ; aucune réconciliation périodique entre `SessionGateway` et les grains ; aucun test dans `Vortex.Players.Tests` ou `Vortex.Hosting.Tests` n'active un `PlayerPresenceGrain` puis attend la collecte.

**Reproduction (environnement de test).** Régler `Vortex:Orleans:GrainCollectionAge` à `00:01:00`, connecter un client WS, entrer dans une room, bloquer les envois du client (couper les `PONG`, ou suspendre le processus client) pendant trois minutes, reprendre : observer côté serveur `PlayerLeftRoomEvent` puis, au premier paquet suivant, l'absence de toute réponse (`Vortex.packets.dropped` ne bouge pas, la file interne grossit). Test automatisable avec `Microsoft.Orleans.TestingHost` : activer le grain, enregistrer un observer factice, avancer l'horloge de collecte, vérifier `IsOnlineAsync`.

**Correction recommandée.** Lier la vie de l'activation à la session : `RegisterSessionObserverAsync` appelle `DelayDeactivation(TimeSpan.MaxValue)` (ou un `[KeepAlive]` conditionnel via `DelayDeactivation` renouvelé) et `UnregisterSessionObserverAsync` appelle `DeactivateOnIdle()` ; en complément, `OnActivateAsync` interroge `ISessionGateway` (déjà disponible en DI dans le silo) pour récupérer l'observer courant du joueur, ce qui rend la réactivation auto-réparante. Ajouter un test TestingHost. Voir l'évolution B (§8).

### ROOM-01 — `FollowFriend` contourne les gardes d'entrée et n'envoie pas le payload (P1, confirmé)

**Chemin.** `FollowFriendMessageHandler.cs:84` appelle `selfPresence.SetActiveRoomAsync(activeRoom.RoomId, ct)` directement. Toutes les gardes d'entrée vivent dans `RoomService.OpenRoomForPlayerIdAsync` / `CompleteRoomEntryAsync` (`RoomService.cs:60-150` puis `:152-361`) : ban de room (`IRoomModerationStore.IsBannedAsync`), room pleine (`PlayersMax`), mot de passe, porte verrouillée → sonnette, événement annulable `PlayerEnteringRoomEvent`, raid protection (`EvaluateEntryAsync`), et l'envoi de `OpenConnection`, `RoomReady`, cartes, mobis, avatars. `RoomGrain.CreateAvatarFromPlayerAsync` (`RoomGrain.Avatar.cs:24-70`) et `RoomAvatarModule.CreateAvatarFromPlayerAsync` (`RoomAvatarModule.cs:91`) ne vérifient **rien** : ni capacité, ni porte, ni ban.

**Comportement déduit.** Un joueur ami de quelqu'un présent dans une room verrouillée, à mot de passe ou pleine est ajouté à la room côté serveur (avatar créé, directory incrémenté, stream abonné) **sans** que son client reçoive un seul paquet d'entrée : il reste dans la vue hôtel ou dans son ancienne room (d'où il a été retiré par `ClearActiveRoomAsync`), pendant que les occupants voient un avatar immobile à la porte. Ses paquets de room suivants (`ctx.RoomId` = room de l'ami) agissent dans cette room.

**Impact.** Contournement de la confidentialité des rooms (verrou, mot de passe, sonnette), de la limite de population et de la raid protection, par une action légitime du client officiel ; désynchronisation client/serveur. Exposition : tout joueur ayant un ami.

**Protections recherchées.** `IsFriendAsync` seulement. Aucune garde dans le grain ; aucun test de `Vortex.Rooms.Tests` n'exerce ce handler.

**Reproduction.** Compte A propriétaire d'une room à mot de passe, compte B ami de A dans une autre room ; B envoie `FollowFriend(A)` depuis la liste d'amis : côté serveur B apparaît dans la room de A (`GetRoomPopulationAsync`), côté client B rien ne change.

**Correction.** Remplacer l'appel par une réponse `RoomForwardMessageComposer { RoomId }` (le client enverra alors `OpenFlatConnection`, exactement comme `CreateFlatMessageHandler.cs` le fait après une création). Fix local d'une ligne ; la correction structurelle est l'évolution A (§8).

### ROOM-02 — `GetRoomEntryData` admet un sonneur sans réponse du propriétaire (P1, confirmé)

**Chemin.** `RoomService.OpenRoomForPlayerIdAsync` appelle `SetPendingRoomAsync(roomId, approved: true)` **avant** les contrôles (`RoomService.cs:93`). Pour une porte verrouillée, il enregistre la sonnette et retourne (`:144-148`) sans réinitialiser le pending. `GetRoomEntryDataMessageHandler.cs:46-50` lit `GetPendingRoomAsync()` et ne teste que `roomId <= 0`, jamais `Approved` ; il envoie ensuite tout le payload d'entrée et appelle `SetActiveRoomAsync` (`:189`).

**Comportement déduit.** Un client qui sonne puis envoie immédiatement `GetRoomEntryData` (en-tête 1250 dans `Headers.cs`) entre dans la room verrouillée sans réponse. La baseline `scripts/hooks/header-registry-baseline.json` note que « aucun message de cette forme n'existe dans le client WIN63 » : le client officiel ne l'émet pas, un client modifié ou un injecteur de paquets (usage courant dans l'écosystème des rétros) le peut. La sonnette reste en attente : au timeout (`RoomGrain.Doorbell.cs`, 20 s) le serveur envoie `FlatAccessDenied` et `SetPendingRoomAsync(Invalid)` à un joueur déjà dans la room.

**Impact.** Même famille que ROOM-01 (contournement de la sonnette, de la raid protection, de l'événement annulable et de la capacité). Exposition : client modifié.

**Correction.** Immédiate : dans `GetRoomEntryDataMessageHandler`, exiger `pendingRoom.Approved` **et** ne poser `approved: true` qu'à la fin de `CompleteRoomEntryAsync` (ou dans `AnswerDoorbellAsync` après admission) ; sinon retirer le handler du registre (id mort pour le client officiel). Structurelle : évolution A.

### ROOM-03 — Suppression de room : les mobis restent rattachés à la room supprimée (P1, confirmé)

**Chemin.** `RoomGrain.DeleteRoomAsync` (`RoomGrain.Settings.cs:204-266`) vérifie le propriétaire, pose `DeletedAt` sur la room, retire les avatars, retire la room du directory, publie `RoomDeletedEvent` et se désactive. Aucun `UPDATE furniture SET room_id = NULL` ; les seuls consommateurs de `RoomDeletedEvent` sont les handlers d'audit (`Vortex.Observability/Events/RoomLifecycleAuditHandlers.cs`). Le chargeur d'inventaire ne liste que les lignes `RoomEntityId == null` (`InventoryFurnitureLoader.cs:64` et prédicat cité dans `docs/codebase/06-economy/inventory.md`), et la room supprimée ne peut plus être ouverte (filtre global de soft delete, `ModelBuilderExtensions.cs:48`, `HydrateRoomStateAsync` → `RoomNotFound`).

**Comportement.** Tout mobi (et animal/bot placé) d'une room supprimée devient inaccessible à son propriétaire, sans avertissement. Le client officiel présente la suppression comme une action normale ; sur Habbo, les objets reviennent dans l'inventaire.

**Impact.** Perte de données joueur définitive sur un parcours courant (les joueurs suppriment des rooms). Tickets support garantis, réparations SQL manuelles.

**Correction.** Dans la même transaction que le soft delete : `UPDATE furniture SET room_id = NULL WHERE room_id = @room` (le propriétaire de chaque ligne est déjà `player_id`, donc chacun récupère les siens), idem pour `pets`/`bots`, puis `InventoryGrain.ReloadFurnitureAsync` des propriétaires concernés ; ou refuser la suppression tant que la room contient des objets (le client affiche l'erreur). Ajouter un test dans `Vortex.Rooms.Tests` (room avec mobis de deux propriétaires → suppression → les deux inventaires les listent).

### QA-01 — Portail qualité rouge sur `main`, pushs en `--no-verify` (P1, confirmé)

**Faits.** Les runs 316 à 323 de `.github/workflows/quality.yml` (13-14 septembre) sont tous en échec sur les trois OS. Le log du job Ubuntu du run 34803028084 montre la cause : `node scripts/hooks/check-header-registry.mjs` sort avec le code 2 (`Directory.Build.targets(38,5): error MSB3073`) après avoir signalé `CustomStackingHeightUpdateMessageComposer = 9201` au-dessus du plafond 4101. Les messages des commits `4ca7c8a`, `482acb1` et `0455032` disent explicitement `--no-verify`. Reproduit localement (section 9) : `check-header-registry.mjs` sort 2 (mode plafond, `check-header-registry.mjs:191-204` : le repli « plafond » **ignore la baseline** alors que cet id y est consigné comme injoignable), `check-wire-conflicts.mjs` sort 2 (« parsed no conflicts — the CLI output format changed, this check is blind »), et `scripts/hooks/__test/run.mjs` compte trois auto-tests en échec.

**Impact.** Trois contrôles mécanisés de la gate sont cassés, donc la gate est rouge quoi que fasse un contributeur ; la réponse observée est de la contourner. Toutes les régressions que FastCheck et QualityGate devaient arrêter passent (dashboard capabilities, murs d'architecture, dérive wire, hooks). La règle « Required validation before completion » d'`AGENTS.md` n'est plus tenable.

**Correction.** (1) `check-header-registry.mjs` : appliquer la baseline (`unreachable`) aussi en mode plafond, ou retirer la mapping 9201 si elle est morte ; (2) `check-wire-conflicts.mjs` : ré-aligner le parseur sur la sortie actuelle de `Vortex.Specs.Cli -- conflicts` (ou faire lire au script les fichiers de conflits directement) et **échouer bruyamment quand il est aveugle plutôt qu'exit 2 silencieux ou exit 0 trompeur** ; (3) réparer les auto-tests des hooks ; (4) rendre la branche `main` protégée par le statut de la gate et bannir `--no-verify` du flux de travail. Critère : run vert sur les trois OS, y compris l'étape de scan (QA-02).

### SES-02 — Aucune garde d'authentification centrale (P2, confirmé)

`MessageRegistry.CreateContextAsync` (`MessageRegistry.cs:48`) construit un `MessageContext` avec `PlayerId = -1` quand la session n'a pas passé le SSO et dispatche quand même. Sur 557 handlers, 350 contiennent une garde de la forme `PlayerId <= 0` ; **40 utilisent `ctx.PlayerId` sans garde** : `RedeemVoucherMessageHandler` (deux requêtes DB par paquet et activation d'un `PlayerPresenceGrain(-1)` pour la réponse, `VoucherGrain.cs:129-175`), `CreateFlatMessageHandler` (exception `InvalidOperationException` loggée après une requête), `MoveAvatarMessageHandler`, `QuitMessageHandler`, `OpenFlatConnectionMessageHandler`, quinze handlers Navigator, `GetNftAssetInventoryMessageHandler`, trois handlers NewNavigator, `SetNewNavigatorWindowPreferencesMessageHandler`, `GetGuildFurniContextMenuInfoMessageHandler`, etc. Les services aval attrapent le cas la plupart du temps (joueur −1 introuvable), mais chaque handler reste responsable de sa garde, et le handler SSO lui-même doit se souvenir que `ctx.PlayerId` reste −1 pendant toute son exécution (bug passé consigné dans son commentaire, `SSOTicketMessageHandler.cs`).

**Correction.** Un `IMessageBehavior<IMessageEvent>` ordonné juste après `RateLimitBehavior` qui rejette (et compte) tout message non marqué `[AllowUnauthenticated]` quand `ctx.PlayerId <= 0` ; marquer les 9 messages du handshake. Le contexte peut alors garantir un `PlayerId` valide et les 350 gardes deviennent redondantes. Évolution C (§8).

### NET-01 — Limites d'abus réseau incomplètes (P2, confirmé)

Le seul limiteur est par session (`RateLimitConfig`: 50/s, burst 100 ; `TokenBucketRateLimiter` : un seau par `SessionKey`). Rien ne borne le nombre de connexions par adresse, la durée d'une session non authentifiée, ni le nombre de sessions total (options SuperSocket non surchargées dans `appsettings.json`/`NetworkingConfig`). Aucun contrôle d'`Origin` n'existe dans `Vortex.Networking` pour le listener WS : un site tiers peut ouvrir une WebSocket vers le hotel depuis un navigateur (cross-site WebSocket hijacking limité, puisque le ticket SSO est nécessaire, mais consommation de ressources). Combiné à SES-02, un attaquant obtient N × 50 requêtes/s vers la base avec N sockets sans jamais s'authentifier.

**Correction.** Plafond de connexions simultanées par IP et global (SuperSocket `MaxConnectionNumber` + compteur par IP dans `SessionGateway.AddSessionAsync`), délai maximal avant SSO (fermer après 30 s sans `SSOTicket`), contrôle d'`Origin` configurable sur le listener WS, et limiteur par IP en plus du limiteur par session.

### PROTO-01 — Préallocation depuis un compteur client (P2, confirmé)

`AcceptFriendMessageParser.cs:14` et `DeclineFriendMessageParser.cs:22` font `new List<int>(friendsCount)` avec `friendsCount` lu tel quel (`PopInt`, 32 bits signés). Un paquet de 8 octets déclenche une tentative d'allocation de `4 × friendsCount` octets (jusqu'à 8 Go) avant que `Ensure` ne rejette la lecture suivante ; à 50 paquets/s par session, cela suffit à saturer le tas d'objets volumineux et le ramasse-miettes. `FishingParsers.cs:63` montre le bon patron (`Math.Clamp(packet.PopInt(), 0, MaxTimelineLength)`). Les 22 autres parsers qui bouclent sur un compteur sont bornés de fait par `MaxPacketBodyBytes` (64 Ko) via `ClientPacket.Ensure`.

**Correction.** Une primitive `PopCount(maxItems, bytesPerItem)` sur `IClientPacket` qui borne par `Remaining / bytesPerItem`, utilisée par les trois parsers concernés et adoptée par le walkthrough « add a feature ».

### AUTH-01 — Ticket SSO rejouable (P2, confirmé)

Le ticket est fort (deux GUID, 256 bits, `WebApiAuthService.cs:230`) mais `TicketSingleUse` vaut `false` par défaut (`AuthenticationConfig.cs:34`) : chaque usage **prolonge** l'expiration de 30 s (`AuthenticationService.cs:90-125`) sans plafond absolu (`TicketAbsoluteLifetimeSeconds` non défini), et l'adresse IP enregistrée à l'émission n'est pas comparée à l'usage. Un ticket observé (proxy, historique, `Referer`, transport non chiffré) permet de prendre la session, ce qui déconnecte la victime (`AddSessionToPlayerAsync` ferme l'ancienne session) — visible mais efficace. Le taux d'émission est limité (`SsoTokenRateLimitPolicy`).

**Correction.** `TicketSingleUse = true` par défaut pour la bêta (la reconnexion redemande un ticket au site, ce qui est le flux normal du client), `TicketAbsoluteLifetimeSeconds` = 60 au cas où le mode glissant est conservé, et liaison optionnelle à l'IP (avec tolérance pour les NAT).

### ROOM-04 — `SetActiveRoomAsync` laisse un état incohérent si la création d'avatar échoue (P2, confirmé)

`PlayerPresenceGrain.Room.cs:45-103` pose `ActiveRoomId`, appelle `RoomDirectoryGrain.AddPlayerToRoomAsync` (`:66`), s'abonne au stream (`:79`), puis `CreateAvatarFromPlayerAsync` (`:95`). Si cette dernière renvoie `false` (toute exception y est avalée et convertie en `false`, `RoomGrain.Avatar.cs:24-70`) ou si `SubscribeAsync`/`GetSummaryAsync` lèvent, l'état reste « dans la room » côté présence et directory sans avatar. Aucun appelant ne lit le booléen (`GetRoomEntryDataMessageHandler.cs:189`, `RoomService.cs:360`, `FollowFriendMessageHandler.cs:84`). Conséquences : population fantôme dans le navigateur, `RoomDirectoryGrain.CheckRoomsAsync` (`RoomDirectoryGrain.cs:177`) considère la room peuplée et la maintient active **indéfiniment** (tick à 20 Hz, timers, mémoire), stream abonné pour un joueur absent.

**Correction.** Ordonner : créer l'avatar d'abord, puis directory et stream ; sur échec, tout défaire (retour à `-1`, `RemovePlayerFromRoomAsync`, désabonnement) et renvoyer `false` au handler, qui répond `CantConnect`. Couvert par l'évolution A.

### QA-02 — Dépendance de test vulnérable (P2, confirmé)

`dotnet list Vortex.Cloud.sln package --vulnerable --include-transitive` signale `SQLitePCLRaw.lib.e_sqlite3 2.1.10` (High, GHSA-2m69-gcr7-jv3q) via `Microsoft.EntityFrameworkCore.Sqlite 9.0.8` dans `Vortex.Rooms.Tests`, `Vortex.Database.Tests`, `Vortex.Players.Tests`, `Vortex.Dashboard.Tests` (section 9). Le pas CI « Scan for vulnerable packages » (`quality.yml:67`) échoue sur « has the following vulnerable packages » ; il est aujourd'hui masqué parce que la gate échoue avant. Aucune exposition runtime (tests seulement).

**Correction.** Pin explicite de `SQLitePCLRaw.bundle_e_sqlite3`/`SQLitePCLRaw.lib.e_sqlite3` à la version corrigée dans `Directory.Packages.props` (transitive pinning déjà activé).

### OPS-01 à OPS-04 — Prérequis d'exploitation (P2, confirmés)

- **OPS-01 Sauvegardes.** `Vortex:Database:Backup:Enabled` est `false` et `MysqlDumpPath` vide par défaut ; `DatabaseBackupService` (mysqldump `--single-transaction --routines --events`, mot de passe via `MYSQL_PWD`) écrit localement dans `backups/`, la rétention supprime au-delà de `RetentionCount`, un échec n'est qu'un `LogError`. Aucune procédure de restauration, aucun test de restauration, pas de copie hors machine, pas de PITR (binlog). Pour une bêta avec données persistantes, c'est la première chose à faire exister.
- **OPS-02 Journaux.** `Vortex.Logging` ne fournit qu'un formatter console et un provider vers la console serveur du dashboard (`ServerConsoleLoggerProvider`) ; aucun sink fichier ni rotation. En conteneur, le driver de logs Docker suffit si configuré ; sur machine nue avec le superviseur, le tampon vaut 2 000 lignes (`appsettings.json`). `PackageHandler.HandleCoreAsync` (`PackageHandler.cs:60-90`) journalise en `Error` avec un hexdump de 128 octets **chaque** paquet invalide : un client peut produire 50 lignes d'erreur par seconde, plus un enregistrement dans le puits d'erreurs.
- **OPS-03 Reprise commerce.** Le journal détecte et **escalade** (`CommerceRelayService.EscalateAsync` → `NeedsIntervention`, `LogCritical`) mais ne répare rien, par conception (ADR-001 : « resuming it is not yet automatic, and that is the next slice »). Il n'existe ni runbook ni action dashboard pour : lister les opérations en `Debited`/`Pivoted`/`NeedsIntervention`, relivrer, rembourser. Sans cela, un incident DB pendant un pic d'achats se termine en support manuel via SQL.
- **OPS-04 Déploiement.** `docker-compose.yml` est explicitement un poste de développement (secrets publics, `DOTNET_ENVIRONMENT=Development`, HTTP en clair autorisé). Hors développement, l'hôte refuse de démarrer sans `Vortex:Orleans:ClusteringProvider/GrainStorageProvider = adonet` (qui exige les scripts SQL Orleans appliqués à la main) ou `AllowUnclusteredOutsideDevelopment = true` (`HostApplicationBuilderExtensions.cs:40-70`). Les validateurs rejettent les `CHANGE_ME` (crypto, IP hash, superviseur, boutique) : bien. Mais rien ne décrit l'environnement de bêta : où termine TLS pour le WebSocket (`wss://`, indispensable au client navigateur et à la confidentialité du ticket SSO), comment sont injectés les secrets, quel mode de migration (`MigrateOnStartup` mono-nœud ou `dotnet ef` avant déploiement), quelle rétention des journaux, quelle supervision.

### DB-01 — Hygiène des migrations et absence de lane MySQL réelle (P2, confirmé/à vérifier)

302 fichiers de migration depuis février 2026, dont 121 contiennent `DropTable`/`DropColumn` et 37 du SQL brut ; `scripts/sql/recover_half_applied_habbicon_migration.sql` et `resync_habbicon_migration_history.sql` témoignent d'au moins un incident de migration à moitié appliquée (MySQL ne transactionne pas le DDL). `MigrationHelper.ApplyStartupMigrationsAsync` (`Program.cs:198`) journalise les ids en attente puis `MigrateAsync` sans verrou multi-hôte (documenté). Tous les tests de persistance tournent sur SQLite ou InMemory ; les comportements MySQL (collation `utf8mb4_unicode_ci` qui rend `Name` insensible à la casse, `ExecuteUpdate` conditionnels, DDL non transactionnel, `strict mode` sur les longueurs) ne sont jamais exercés en CI. Aucun chemin de retour arrière n'existe (les migrations `Down` de 302 étapes ne sont pas une stratégie) : le rollback est la restauration d'une sauvegarde, ce qui renvoie à OPS-01. Non vérifié faute de base : `dotnet ef migrations has-pending-model-changes`.

**Correction.** Avant l'ouverture : figer un **baseline** de schéma (squash des 302 migrations en une migration initiale + script idempotent généré), documenter « migration = avant déploiement, hôte arrêté, sauvegarde préalable », et ajouter une lane de tests d'intégration MySQL (Testcontainers ou service CI) pour les chemins à claims conditionnels et pour l'application des migrations depuis zéro.

### SEC-01 — Comptes : pas de verrouillage, sessions non révoquées au ban (P2, confirmé)

`AccountAuthenticator.VerifyCredentialsAsync` fait un BCrypt (facteur 12, hash factice pour les comptes inconnus, hors thread) sans compteur d'échecs ; les seuls freins sont les limiteurs à fenêtre fixe des endpoints (`WebApiAppConfigurator.cs:90-140`, `DashboardEndpoints.cs:118`), par défaut par client. Les sessions web et dashboard sont en mémoire (`AccountSessionStore`, 256 bits, expiration configurée) : perdues au redémarrage (déconnexion de tous les visiteurs du site à chaque déploiement) et, comme le note `WebApiSessionStore.cs` lui-même, jamais révoquées lors d'un bannissement (« a banned visitor keeps browsing until the cookie expires »). MFA : présent côté dashboard avec step-up ; côté site, TOTP si enrôlé.

**Correction.** Verrouillage progressif par compte (délai croissant après N échecs) et par IP, révocation des sessions web/dashboard dans le chemin de ban (le `IAccountSessionRevoker` existe déjà), et, si les déploiements sont fréquents, sessions persistées (table) pour éviter la déconnexion massive.

### Constats P3 (résumés)

- **PERF-01.** `MessageSystem.ResolveRoomIdAsync` (`MessageSystem.cs:85-113`) paie un aller-retour Orleans par paquet pour renseigner `roomId` dans les métriques ; c'est aussi, par accident, ce qui maintient le grain de présence en vie (SES-01). Remplacer par une lecture du cache local du gateway (la room active peut être publiée par le grain vers `SessionGateway` à chaque changement) et mesurer sur `MoveAvatar`.
- **NET-02.** TCP sans heartbeat et `PongTimeout = 0` : une socket morte sans FIN reste « en ligne » jusqu'au keepalive OS (souvent 2 h). Activer le keepalive TCP côté serveur (SuperSocket `KeepAliveOptions`) et un `PongTimeout` long (10 min) qui distingue l'onglet gelé du câble coupé.
- **ROOM-05 (à vérifier).** Le payload d'entrée (`Objects`, `Items`, `Users`) est envoyé avant l'abonnement au stream (`RoomService.cs:300-360`, `GetRoomEntryDataMessageHandler.cs:189`, `PlayerPresenceGrain.Room.cs:79`) : un `UserRemove`/`ObjectAdd` publié dans la fenêtre est perdu pour l'entrant. Mesurer la fenêtre en test d'intégration ; correction : s'abonner d'abord puis émettre le snapshot (ou re-synchroniser après abonnement).
- **DB-02 (à vérifier).** `FlushDirtyItemsAsync` attache un lot de 100 entités et sauvegarde en une fois (`RoomPersistenceGrain.cs:130-190`) ; une ligne absente (suppression SQL directe, purge) fait lever `DbUpdateConcurrencyException` pour tout le lot, réessayé à chaque tick sans jamais isoler l'élément fautif. À confirmer sur MySQL ; correction : retirer du lot l'entité qui échoue après N tentatives et la journaliser.
- **PROTO-02.** `SetMottoAsync` (`PlayerGrain.cs:183`) et `UpdateRoomSettingsAsync` (`RoomGrain.Settings.cs:51-120`) n'imposent aucune longueur (colonnes `varchar(512)`/`varchar(50)` : MySQL strict rejette, exception loggée) ni filtre de mots ; `ChangeMottoMessageHandler.cs:22` teste `< 0` au lieu de `<= 0`. Le chat borne à 100 caractères (`RoomChatSystem.cs:28`) avec contrôle de flood.
- **SEC-02.** Mot de passe de room en clair en base et dans `RoomSnapshot` (norme Habbo, mais évitable par hachage côté serveur) ; `UseSwagger()` inconditionnel sur le dashboard (`DashboardWebHost.cs:664`) et le WebApi : à désactiver hors développement.
- **OBS-01.** `TracingEnabled` produit des `ActivitySource` jamais exportés (aucun exporter enregistré, `VortexTelemetry.cs:8`) ; `/health` (`WebApiEndpoints.cs:240-275`) ne teste que la base et le garde de services, pas les listeners jeu ni le silo. Ajouter un exporter OTLP et une sonde « accepte un handshake » pour l'orchestrateur.
- **DATA-01.** `PlayerDirectoryGrain` conserve indéfiniment `id↔nom` de chaque joueur consulté (`:26-27`) ; l'inventaire charge tout (`InventoryFurnitureLoader.cs:64`). Acceptable en bêta ; borner (LRU) et paginer avant la montée en charge.
- **DOC-01.** `docs/orleans.md` décrit des `[PersistentState]`/`PlayerStore` inexistants ; `CONTEXT.md` promet un fan-out multi-session que le code interdit (une session par joueur, `SessionGateway._playerToSession`) ; `AGENTS.md`/`CLAUDE.md` parlent de 23 conflits wire et 14 ids baselinés (réels : 61 et 3) ; `README.md` dit « SDK 9.x ». Ces documents pilotent les outils d'IA du dépôt : leur dérive produit des changements fondés sur de fausses prémisses.
- **SPEC-01.** 127 handlers « reach no domain operation and send nothing » (caméra, compétitions, campagnes, publicités, calendrier saisonnier…). Aucun n'est sur un parcours essentiel, mais chacun est une fonctionnalité que le client propose et que le serveur ignore en silence : à afficher comme telles (message client) ou à retirer du registre.
- **ORL-01.** `RoomDirectoryGrain` est `[KeepAlive]` et ne se nettoie que par `RemoveActiveRoomAsync` ; une room morte sans `OnDeactivateAsync` (kill du silo) laisse une entrée jusqu'au redémarrage — bénin en mono-nœud puisque le redémarrage vide tout.

---

## 8. Évolutions architecturales proposées

Chaque évolution est classée : **avant la bêta**, **à planifier rapidement**, ou **ultérieure**. Le critère est celui de la demande : coût maintenant contre coût après l'arrivée des joueurs et des données persistantes.

### A. Une seule porte d'entrée en room, tenue par le grain (avant la bêta)

**Limites de l'existant.** Les gardes d'entrée (ban, capacité, mot de passe, sonnette, raid, événement annulable) sont dans `RoomService` (service sans état) alors que l'état qu'elles protègent est dans `RoomGrain`. Tout chemin qui atteint le grain sans passer par le service (`FollowFriend`, `GetRoomEntryData`, demain un téléport wired, un « aller à » modérateur, une action dashboard) contourne tout (ROOM-01, ROOM-02). L'état de présence (`Pending`/`Approved`/`Active`) est posé de façon optimiste avant les contrôles et n'est pas défait en cas d'échec (ROOM-04).

**Options.**
1. *Corriger chaque appelant* (une ligne pour `FollowFriend`, une condition pour `GetRoomEntryData`, un rollback dans `SetActiveRoomAsync`). Coût minimal, mais la faille structurelle demeure : le prochain appelant la rouvrira.
2. *Déplacer l'admission dans le grain* : `IRoomCore.TryAdmitAsync(ActionContext, AdmissionRequest) → AdmissionDecision` (Admitted / Full / WrongPassword / Banned / RingDoorbell / Refused(raison)), qui applique dans l'ordre ban → capacité → porte → raid → événement annulable et, en cas d'admission, crée l'avatar dans le même tour. `RoomService` ne fait plus que traduire la décision en composers et piloter la présence. `SetActiveRoomAsync` n'existe qu'avec une décision `Admitted` et devient transactionnel (avatar créé → directory → stream, rollback complet sinon). `FollowFriend` répond `RoomForward`.
3. *Machine d'états de présence explicite* (enum `None → Pending(room) → Admitted(room) → Active(room)`) portée par le grain de présence, chaque transition validée. Complément de l'option 2.

**Recommandation.** Option 2 + 3. Bénéfices : une faille de classe entière fermée, testabilité (le grain se teste en TestingHost sans service), lisibilité du parcours d'entrée (aujourd'hui réparti sur trois fichiers et deux grains). Compromis : `RoomGrain` grossit d'une méthode et d'un type de décision ; le doorbell garde son état dans le grain (déjà le cas). Risque : régression sur les huit cas d'entrée → couvrir par une table de tests (ouverte, pleine, mot de passe bon/mauvais, verrouillée avec/sans réponse, bannie, raid, événement annulé). Coût maintenant : 2 à 3 jours ; après ouverture : identique en code mais chaque contournement découvert entre-temps est un incident de confidentialité.

**Transition.** Introduire `TryAdmitAsync` à côté de l'existant, y router `OpenRoomForPlayerIdAsync` puis `AnswerDoorbellAsync`, retirer les appels directs à `SetActiveRoomAsync` hors décision, puis supprimer l'ancien chemin. Aucune migration de données.

### B. La vie du grain de présence suit la session (avant la bêta)

**Limites.** Le grain de présence est le pivot du routage (observer, room active, file sortante) mais sa durée de vie est celle d'un cache : 2 minutes sans message (SES-01). Le gateway (`SessionGateway`) croit un joueur en ligne que le grain croit hors ligne.

**Options.**
1. *`DelayDeactivation` piloté par la session* : `RegisterSessionObserverAsync` → `DelayDeactivation(TimeSpan.MaxValue)` ; `UnregisterSessionObserverAsync` → `DeactivateOnIdle()`. Trois lignes, résout le symptôme, Orleans garantit la sémantique.
2. *Réactivation auto-réparante* : `OnActivateAsync` interroge `ISessionGateway.GetSessionObserver(GetPlayerSession(id))` et se ré-enregistre. Résout aussi la réactivation après une désactivation explicite ou un crash de l'activation.
3. *Réconciliation périodique* gateway ↔ grains (`IsOnlineAsync` vs `_playerToSession`) qui journalise et répare les écarts : filet de sécurité et métrique d'incohérence.

**Recommandation.** 1 + 2, avec 3 comme métrique (`Vortex.presence.mismatch`). Bénéfices : un joueur connecté ne peut plus devenir fantôme ; la mémoire des présences est bornée par le nombre de sessions, ce qui est le bon invariant. Compromis : les grains de présence des joueurs connectés ne sont plus jamais collectés (voulu). Risque : un `DelayDeactivation` oublié à la déconnexion garderait des activations ; couvert par `OnDeactivateAsync` et par la réconciliation. Coût : une demi-journée plus un test TestingHost.

### C. Garde d'authentification et d'autorisation dans le pipeline (avant la bêta)

**Limites.** 557 handlers, 350 gardes copiées, 40 absentes (SES-02). Le contexte peut porter un `PlayerId` invalide jusque dans les grains. Le handler SSO travaille avec un contexte périmé.

**Recommandation.** Un `AuthenticationBehavior` global (ordre juste après `RateLimitBehavior`) : refuse tout message dont le type n'est pas marqué `[AllowUnauthenticated]` quand la session n'est pas liée ; compte les refus (`Vortex.packets.dropped{reason=unauthenticated}`) ; ferme la session après N refus. Puis rendre `MessageContext.PlayerId` non négatif par construction et supprimer progressivement les gardes locales. Ajouter ensuite, sur le même mécanisme, une déclaration d'autorisation par message (`[RequiresCapability(...)]`) pour les 23 handlers modérateur, ce qui rend l'autorisation lisible et testable en un endroit. Coût : un jour ; bénéfice : une classe de faille fermée et 350 lignes de garde retirables.

### D. Réparer le portail qualité et le rendre incontournable (avant la bêta)

Détaillé en QA-01/QA-02. Ajouter : protection de branche sur `main` conditionnée au statut de la gate, exécution de `dotnet list package --vulnerable` dans FastCheck (elle ne coûte rien), et un principe : un contrôle « aveugle » échoue ou s'annonce comme tel dans un statut séparé, jamais en exit 2 anonyme.

### E. Fondation d'exploitation (avant la bêta)

Regroupe OPS-01 à OPS-04, OBS-01, NET-02. Livrables : (1) manifeste de déploiement de bêta (compose « prod » ou systemd : secrets par `env_file`/secret store, `AllowUnclusteredOutsideDevelopment=true` **ou** adonet documenté, `MigrateOnStartup` mono-nœud, reverse proxy TLS devant 30001 et 9000/8080, Swagger désactivé) ; (2) journaux structurés vers stdout capturés par le driver de logs avec rotation, et niveau `Warning` pour `PackageHandler` avec échantillonnage du hexdump ; (3) sauvegardes activées, copiées hors machine, **restauration répétée** sur un environnement de test ; (4) exporter OTLP ou, à défaut, tableau Prometheus/Grafana branché sur `/metrics` avec alertes sur `LogCritical` (escalades commerce), `Vortex.packets.dropped`, latence de tick ; (5) runbook : redémarrage à chaud (ce qui est perdu : rooms en mémoire, présences, abonnements), joueur fantôme, opération commerce en `NeedsIntervention` (requête, relivraison, remboursement via les grains, jamais SQL direct), migration ratée à mi-chemin (`scripts/sql/recover_half_applied_*` généralisé).

### F. Suppression de room = retour des objets (avant la bêta)

Détaillé en ROOM-03. Une transaction : soft delete de la room + `room_id = NULL` pour les mobis, animaux, bots + rechargement des vues inventaire des propriétaires ; test d'intégration.

### G. Persistance : baseline de schéma et lane MySQL (à planifier rapidement)

Détaillé en DB-01. Le squash des migrations est d'autant moins cher qu'il est fait **avant** la première base de production ; après, il faut gérer des bases à divers niveaux. La lane MySQL (Testcontainers) protège les claims conditionnels, la collation et les migrations depuis zéro. Compromis : temps de CI (+3 à 5 min) ; à limiter aux tests marqués `[Trait("db","mysql")]`.

### H. Validation d'entrée déclarative pour le protocole (à planifier rapidement)

Détaillé en PROTO-01/PROTO-02. Une primitive de lecture bornée et un attribut de longueur sur les champs `string` des messages entrants (`[MaxLength]` lu par le pipeline) évitent de compter sur chaque handler. Unifier le filtre de mots (chat, motto, noms de room, forum) derrière un service unique.

### I. Contrats et documentation vérifiés mécaniquement (à planifier rapidement)

Détaillé en DOC-01. Les nombres cités dans `AGENTS.md`/`CLAUDE.md` (conflits, ids baselinés) doivent être calculés par les scripts et non recopiés ; `docs/orleans.md` et `CONTEXT.md` doivent être réécrits ou remplacés par des liens vers `docs/codebase/` (qui est exact sur ces points). Un test de fraîcheur (le générateur de docs compare son commit à `HEAD`) évite la dérive silencieuse.

### J. Multi-silo et streams durables (ultérieur)

La thèse mono-silo est cohérente et gardée par `RefuseAnUndeclaredSecondSiloAsync`. Ne pas investir avant d'avoir mesuré la capacité d'un nœud avec `Vortex.LoadGen` sur un environnement de test. Conditions de réouverture : dépassement mesuré d'un nœud, ou exigence de haute disponibilité. Inventaire des composants à rendre cluster-aware déjà tenu dans `OrleansHostConfig.MultiSiloReady` et `docs/architecture-v4/single-silo-inventory.yaml`.

Ce que l'audit **ne recommande pas** : event sourcing global, CQRS généralisé, découpage de `RoomGrain` en grains par objet (interdit par ADR-000 et à raison : le tour unique de la room est le modèle de concurrence), remplacement du moteur de permissions. Aucun de ces chantiers ne répond à un défaut constaté.

---

## 9. Validations réalisées

Toutes les commandes ont été exécutées dans le conteneur d'audit (`/root/.dotnet`, SDK 10.0.401, Node 22.22), sans base de données.

| Validation | Commande | Résultat |
|---|---|---|
| Restauration | `dotnet tool restore && dotnet restore Vortex.Cloud.sln` | OK ; avertissements NU1903 (SQLitePCLRaw 2.1.10) sur 4 projets de tests |
| Compilation | `dotnet build Vortex.Cloud.sln --no-restore` | **OK**, 0 erreur, 7 avertissements, 3 min 47 |
| Tests | `dotnet test <projet> --no-build` pour les 18 projets de tests | **3 869 réussis, 0 échec, 0 ignoré** (Rooms 1 478, Signals 437, Players 349, Revisions 295, Database 220, Specs 217, Dashboard 215, Hosting 138, WebApi 134, Rewards 94, Authentication 68, Crypto 64, Navigator 54, Supervisor 36, Plugins 31, Shop 19, Pipeline 13, PacketHandlers 7) ; TRX dans l'environnement d'audit |
| Format | `dotnet csharpier check .` | OK (6 539 fichiers) |
| Contrôles hors compilateur | `node scripts/hooks/check-dashboard-capabilities.mjs` | OK (64 capabilities, 52 routes, 3 486 clés de locale) |
| | `node scripts/hooks/check-header-registry.mjs` | **Échec, exit 2** : mode plafond (sources client absentes), id 9201 signalé bien que baseliné |
| | `node scripts/hooks/check-architecture-walls.mjs` | OK (7 murs, 0 fuite baselinée) |
| | `node scripts/hooks/check-logic-groups.mjs` | OK (261 clés, 14 groupes) |
| | `node scripts/hooks/check-wire-conflicts.mjs` | **Échec, exit 2** : « parsed no conflicts — the CLI output format changed, this check is blind » |
| | `node scripts/hooks/__test/run.mjs` | **3 auto-tests en échec** (deux liés au registre d'en-têtes, un au probe csharpier) |
| Specs | `dotnet run --project Vortex.Specs.Cli -- validate` | OK (3 923 fichiers, 0 erreur, 0 avertissement) |
| | `dotnet run --project Vortex.Specs.Cli -- unknowns --severity critical` | 127 inconnues critiques (handlers muets) |
| Front-end | `npm run lint` / `npm run test` (Vortex.Dashboard.Web) | OK (svelte-check 0 erreur, 16 avertissements ; 4 vérifications outillées OK) |
| Vulnérabilités | `dotnet list Vortex.Cloud.sln package --vulnerable --include-transitive` | SQLitePCLRaw.lib.e_sqlite3 2.1.10 (High) dans Rooms/Database/Players/Dashboard.Tests |
| CI | `mcp github actions_list` / `get_job_logs` (run 34803028084) | 8 derniers runs en échec ; cause : `check-header-registry.mjs` exit 2 dans FastCheck ; étape de scan ignorée |
| Inventaires par grep | grains (54), `[KeepAlive]` (16), attributs d'interleaving (6 + manifeste), handlers (557 / 350 gardés / 40 non gardés), parsers préallouant (3), verrous/blocages/`.Ignore()` dans les grains (0), `ConfigureAwait(false)` dans les grains (0), transactions (3 sites), index uniques (126), migrations destructives (121) et SQL brut (37) | Voir §7 |

**Vérifications impossibles dans cet environnement, avec le prérequis manquant :**

- Exécution de l'émulateur, application des 302 migrations, `dotnet ef migrations has-pending-model-changes`, mesure de démarrage, reproduction dynamique de SES-01/ROOM-01/ROOM-02/ROOM-03 → **serveur MySQL** (le daemon Docker n'est pas accessible dans le conteneur).
- Classification des 61 conflits wire baselinés, vérification des sérialiseurs contre le client → **sources AS3 du client WIN63** à côté du dépôt.
- Test de charge et capacité (`Vortex.LoadGen`), fenêtre de perte réelle, coût du tick → **environnement de test avec base et client**. Aucune capacité en joueurs n'est avancée dans ce rapport ; les seuls chiffres existants sont des micro-benchmarks (`docs/architecture-v4/benchmarks/`).
- Comportement du keepalive TCP, gel d'onglet, reconnexion → **client réel**.

---

## 10. Plan de travail avant la bêta

Ordonné par dépendances ; chaque élément porte son critère d'acceptation. Les durées sont des ordres de grandeur pour une personne connaissant le dépôt.

### Phase 0 — Remettre le filet en place (1 à 2 jours)

1. **QA-01** Réparer `check-header-registry.mjs` (baseline appliquée en mode plafond ou mapping 9201 retirée), `check-wire-conflicts.mjs` (parseur ré-aligné, échec explicite si aveugle), auto-tests des hooks. *Acceptation* : `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudQualityGate` vert localement et `quality.yml` vert sur les trois OS.
2. **QA-02** Pin de SQLitePCLRaw corrigé. *Acceptation* : étape « Scan for vulnerable packages » verte.
3. **Gouvernance** Protection de branche sur `main` par la gate ; `--no-verify` banni du flux. *Acceptation* : règle activée, PR obligatoire.
4. **DOC-01 (partie chiffres)** `AGENTS.md`/`CLAUDE.md`/`README.md` corrigés (61 conflits, 3 ids, SDK 10, une session par joueur). *Acceptation* : nombres dérivés des fichiers de baseline.

### Phase 1 — Fermer les défauts P1 et les P2 de sécurité (5 à 8 jours)

5. **Évolution A** (ROOM-01, ROOM-02, ROOM-04) : `TryAdmitAsync` dans le grain, machine d'états de présence, `FollowFriend` → `RoomForward`, `GetRoomEntryData` conditionné à `Approved` (ou retiré). *Acceptation* : table de tests d'entrée (8 cas) verte en TestingHost ; un client forgé envoyant `GetRoomEntryData` pendant une sonnette reçoit `CantConnect` ; `GetRoomPopulationAsync` revient à 0 après un échec d'entrée.
6. **Évolution B** (SES-01) : présence liée à la session + réactivation auto-réparante + métrique d'écart. *Acceptation* : test TestingHost « observer enregistré, âge de collecte dépassé, `IsOnlineAsync` reste vrai » ; scénario manuel onglet gelé 5 min → le joueur reste dans sa room et reçoit les messages à la reprise.
7. **Évolution F** (ROOM-03) : suppression de room restitue mobis/animaux/bots. *Acceptation* : test d'intégration deux propriétaires ; aucun mobi avec `room_id` pointant une room `DeletedAt IS NOT NULL` après suppression.
8. **Évolution C** (SES-02) : `AuthenticationBehavior` + `[AllowUnauthenticated]` sur les 9 messages de handshake. *Acceptation* : test pipeline « message non marqué sur session non liée → rejeté, compteur incrémenté » ; les 40 handlers listés ne sont plus atteignables sans SSO.
9. **PROTO-01** primitive de lecture bornée, appliquée aux deux parsers. *Acceptation* : test parser avec `friendsCount = int.MaxValue` → liste vide ou exception sans allocation (mesure `GC.GetTotalAllocatedBytes`).
10. **AUTH-01** `TicketSingleUse = true`, `TicketAbsoluteLifetimeSeconds` défini. *Acceptation* : deux SSO successifs avec le même ticket → le second est refusé (test existant à étendre dans `Vortex.Authentication.Tests`).
11. **NET-01** plafond de connexions par IP et global, délai avant SSO, contrôle d'Origin WS configurable. *Acceptation* : tests de `SessionGateway` ; 200 sockets sans SSO depuis une IP → refus au-delà du plafond, fermeture après le délai.

### Phase 2 — Base d'exploitation (5 à 8 jours, en parallèle de la phase 1)

12. **OPS-04** manifeste de déploiement de bêta (secrets, TLS devant WS/HTTP, `AllowUnclusteredOutsideDevelopment` ou adonet, Swagger désactivé, `MigrateOnStartup` décidé). *Acceptation* : démarrage hors `Development` réussi avec la configuration versionnée (hors secrets), `ListenerSecurity` sans opt-in insecure.
13. **OPS-01** sauvegardes activées, copie hors machine, **restauration répétée** sur un environnement de test. *Acceptation* : procédure écrite et exécutée deux fois, durée mesurée.
14. **OPS-02 / OBS-01** journaux structurés persistants avec rotation, hexdump échantillonné, exporter OTLP ou tableau Prometheus avec alertes (LogCritical, drops, latence de tick, échecs de flush). *Acceptation* : un `LogCritical` de commerce déclenche une alerte visible.
15. **OPS-03** runbook opérations + action dashboard « opérations commerce en attente » (lister, relivrer via les grains, rembourser). *Acceptation* : scénario « crash entre débit et grant » joué en test, résolu sans SQL.
16. **NET-02** keepalive TCP + `PongTimeout` long. *Acceptation* : socket coupée sans FIN détectée en moins de 15 min.
17. **SEC-01** verrouillage progressif, révocation des sessions au ban. *Acceptation* : tests d'authentification.

### Phase 3 — Avant l'ouverture (3 à 5 jours)

18. **Évolution G** (DB-01) : baseline de schéma (squash) + lane MySQL (Testcontainers) sur les claims conditionnels et les migrations depuis zéro. *Acceptation* : CI verte avec la lane ; une base vide migrée en une étape.
19. **Test de charge** avec `Vortex.LoadGen` sur l'environnement de bêta : 100 puis 300 clients synthétiques, entrée en room, mouvement, chat, achats. *Acceptation* : latence de tick p95 < 25 ms sur une room de 50, aucune fuite mémoire sur 2 h, aucune escalade commerce ; chiffres consignés dans `docs/architecture-v4/benchmarks/`.
20. **Répétition d'incident** : arrêt brutal pendant une session de construction (mesure de la fenêtre de perte réelle), redémarrage, restauration de sauvegarde. *Acceptation* : runbook exécuté, écarts corrigés.
21. **PROTO-02 / SEC-02** longueurs et filtre unifiés, mot de passe de room haché, Swagger hors développement.

---

## 11. Checklist d'ouverture

Critères observables ; chacun est vérifiable par une commande, une requête ou une observation.

**Lancement**

- [ ] `quality.yml` vert sur les trois OS pour le commit déployé ; aucun `--no-verify` depuis la phase 0.
- [ ] Hôte démarré hors `Development` avec la configuration de bêta versionnée ; aucun `CHANGE_ME` ; `ListenerSecurity` sans opt-in insecure ; TLS actif devant 30001, 8080, 9000.
- [ ] `TicketSingleUse = true` ; `Vortex:Orleans:GrainCollectionAge` documenté ; présence liée à la session (test TestingHost vert).
- [ ] Tests d'entrée en room (8 cas) verts ; `FollowFriend` répond `RoomForward` ; `GetRoomEntryData` refusé hors admission.
- [ ] Suppression de room restitue les objets (test d'intégration vert).
- [ ] Sauvegarde automatique active, dernière restauration réussie datée de moins de 7 jours.
- [ ] Journaux persistants avec rotation ; alerte sur `LogCritical` testée.
- [ ] `/health` « Healthy », `/metrics` accessible au scraper avec jeton, tableau de bord avec : sessions, joueurs en ligne, rooms actives, latence de tick, `Vortex.packets.dropped` par raison, opérations commerce par état.
- [ ] Runbook disponible : redémarrage, joueur fantôme, opération commerce bloquée, migration à mi-chemin, restauration.
- [ ] Test de charge exécuté sur l'environnement de bêta avec chiffres consignés.
- [ ] Limites d'abus actives : rate limit par session **et** par IP, plafond de connexions, délai pré-SSO.

**Reprise après incident**

- [ ] Redémarrage à chaud : durée mesurée, rooms rechargées depuis la base, joueurs reconnectés par nouveau ticket (le site en émet un nouveau) ; ce qui est perdu est connu (positions non flushées < 2 s + fenêtre de lot, présences, abonnements).
- [ ] Après une indisponibilité de la base : `RoomPersistenceGrain` rejoue ses lots (positions), `CommerceRelayService` relaie les événements, les opérations `NeedsIntervention` sont listées et traitées.
- [ ] Après un crash entre débit et grant : opération en `Debited` visible, remboursement ou livraison effectué par le runbook, joueur informé.
- [ ] Restauration de sauvegarde : procédure exécutée, écart entre sauvegarde et incident annoncé aux joueurs.

---

## 12. Travaux différés

| Sujet | Justification du report | Condition de déclenchement |
|---|---|---|
| Multi-silo (évolution J), streams durables, PubSubStore ADO.NET | Thèse mono-nœud cohérente et gardée ; aucun besoin mesuré | Capacité d'un nœud atteinte (mesure phase 3) ou exigence de haute disponibilité |
| PERF-01 (appel grain par paquet) | Coût non mesuré ; maintien accidentel de la présence à traiter d'abord (évolution B) | Latence p95 des paquets `MoveAvatar`/`Chat` mesurée au-dessus de l'objectif |
| ROOM-05 (snapshot avant abonnement) | Fenêtre de quelques millisecondes, symptôme rare et réparé par une réentrée | Rapports de mobis/avatars manquants à l'entrée, ou lors de l'évolution A si le réordonnancement est gratuit |
| DB-02 (lot de flush empoisonné) | Ne survient que sur suppression physique hors code ; à confirmer sur MySQL | Lane MySQL en place (évolution G) |
| DATA-01 (caches et inventaires non bornés) | Volumes de bêta faibles | Plus de 50 000 joueurs consultés ou inventaires > 20 000 objets |
| SPEC-01 (127 handlers muets) | Hors parcours essentiels | Retours joueurs sur les fonctionnalités concernées ; à afficher comme indisponibles d'ici là |
| Modernisation du chiffrement du transport jeu | Contraint par le client (RSA-1024, DH-384, RC4) ; TLS devant le WS couvre le navigateur | Changement de client |
| Plugins en production (rechargement à chaud, isolement) | Le plugin d'exemple n'est pas requis ; fuite mémoire documentée au rechargement | Premier plugin déployé en bêta : rechargement à chaud interdit, redémarrage seulement |
| Évolution I (documentation vérifiée mécaniquement) | Aucun impact joueur ; impact sur la qualité des changements assistés par IA | Dès la phase 0 pour les chiffres ; test de fraîcheur en phase 3 |
| ORL-01 (`RoomDirectoryGrain` sans reaper) | Bénin en mono-nœud (vidé au redémarrage) | Passage multi-silo |

### Travaux restants de l'audit lui-même

Pour compléter la couverture (§4), dans l'ordre d'utilité : moteur wired (limites d'exécution en situation, coût CPU d'un spam de boîtes), jeux de room et animaux (exceptions dans le tick, timers orphelins), énumération endpoint par endpoint du dashboard (autorisation, IDOR, pagination), échantillon élargi des 105 handlers Room et des handlers Help/GroupForums, lecture des sérialiseurs à placeholders, classification des 61 conflits wire avec les sources client, cohérence snapshot/modèle et N+1 sur MySQL, exécution réelle de l'émulateur et test de charge. Les reproductions décrites en §7 sont le point de reprise.
