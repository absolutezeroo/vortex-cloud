# Vortex Cloud — Roadmap (format BMAD, brownfield)

Roadmap d'achèvement structurée en **epics → stories → critères d'acceptation**, ordonnée
par dépendances, avec une **Definition of Done** applicable à chaque story. Projet
existant (brownfield) : on ne repart pas de zéro, on layer les epics manquants sur une
base dont le moteur est solide mais dont la connexion client est majoritairement vide.

---

## 1. État actuel (constat brownfield)

**Solide, à ne pas reconstruire :** moteur de room (A\* réel, ~105 fichiers wired,
furniture logic, map module), système d'observability event-driven (corrélation Orleans,
audit, ledger, OTel-ready), moteur de permissions (PermissionSet/IPermissionService/
Capabilities, policies pures `RoomSecurityPolicy`/`ModerationPolicy`), architecture
Orleans correcte (handlers fins, grains isolés, streams).

**Le déséquilibre central :** le moteur est profond, mais les **handlers** qui exposent
ce moteur au client sont majoritairement des stubs. Taux de remplissage mesurés :

| Domaine | Remplissage |
|---|---|
| Handshake | 44 % |
| Catalog | 39 % |
| Users | 26 % |
| Room | 23 % |
| Moderator (outil staff) | 21 % |
| Inventory | 19 % |
| Navigator | 8 % |
| RoomSettings / Sound / Camera | 0 % |

**Couverture de test :** surface WebApi testée (16 tests d'intégration sur la branche
`feat/webapi-aspnetcore-migration`), cœur de jeu / économie / permissions **non testés**.

**En cours :** migration WebApi → ASP.NET Core (branche feature, à merger).

**Dette identifiée :** gestion d'erreur de grain repoussée (`// TODO handle exceptions`),
~118 fichiers hors format csharpier, rétention RGPD de l'audit absente, accès dashboard
distant non sécurisé.

---

## 2. Principes directeurs

1. **Inside-out, pas outside-in.** Finir le cœur jouable avant d'élargir la périphérie.
2. **Tranches verticales finies à 100 %**, pas de la largeur scaffoldée.
3. **Tester le sensible en priorité** (économie, permissions) — étendre le harnais qui
   existe déjà, ne pas le réinventer.
4. **Un stub est une dette, pas un acquis.** Chaque handler vide est une promesse non
   tenue.
5. **Stratégies, pas des TODO par site** (erreur de grain, placement, etc.).

---

## 3. Vue d'ensemble des epics

| # | Epic | Objectif | Statut | Dépend de |
|---|---|---|---|---|
| 0 | Socle de qualité | Harnais de test cœur, stratégie d'erreur, format vert | À lancer (transverse) | — |
| 1 | Boucle de jeu jouable | login→room→walk→chat→furni→navigator→leave à 100 % | Partiel | 0 (en //) |
| 2 | Permissions & modération | Compléter droits group + outil staff + tests policies | Quasi fait | 1 |
| 3 | WebApi ASP.NET | Merger, finir, durcir, supprimer HttpListener | En cours | — |
| 4 | Économie & inventaire | Achat/cadeau/revente bout-en-bout + ledger testé | Partiel | 1 |
| 5 | Social | Amis, messagerie, groupes | Partiel | 1 |
| 6 | Trading | Échange sécurisé bout-en-bout | Stub | 4 |
| 7 | Ops & conformité | Rétention RGPD, dashboard distant, OTel export | Partiel | — |

**Ordre recommandé :** 0 (en parallèle de tout) → 3 (presque fini, fermer la boucle) →
1 (la priorité absolue) → 2 → 4 → 5 → 6 → 7.

---

## 4. Epics détaillés

### EPIC 0 — Socle de qualité (transverse)

**Objectif :** poser les multiplicateurs qui rendent tout le reste rapide et sûr. Se mène
en parallèle des autres epics, pas avant eux.

**Story 0.1 — Étendre le harnais de test au cœur du domaine**
*En tant que* dev, *je veux* tester les policies pures et la logique de grain critique,
*afin de* refactorer sans régression silencieuse sur l'économie et les droits.
- [ ] Nouveau projet `Turbo.Permissions.Tests` (ou équivalent) calqué sur
      `Turbo.WebApi.Tests` (xunit + FluentAssertions, déjà maîtrisé).
- [ ] Tests unitaires de `RoomSecurityPolicy.ResolveControllerLevel` (tous les cas :
      System, superuser, ModerateAny, BuildAny, owner explicite, rights, none).
- [ ] Tests unitaires de `ModerationPolicy.IsAllowed` (chaque action × capability
      spécifique × ModerateAny × wildcard × refus).
- [ ] Mise en place Orleans TestKit pour au moins un grain (room) en preuve de concept.
- [ ] Cible : ces tests tournent dans le quality gate.

**Story 0.2 — Stratégie d'erreur et de résilience de grain**
*En tant que* dev, *je veux* une politique unique de gestion d'échec dans les grains,
*afin que* l'état d'un grain ne soit jamais laissé incohérent.
- [ ] Définir le contrat : que se passe-t-il quand une opération de grain échoue
      (rollback de l'état en mémoire ? log structuré ? event d'erreur observability ?).
- [ ] Remplacer les `// TODO handle exceptions` de `RoomGrain.Furni.cs` (et autres) par
      cette stratégie.
- [ ] Auditer les 24 blocs catch existants : aucun ne doit avaler une erreur en silence.

**Story 0.3 — Gate de format vert**
- [ ] `csharpier check` passe sur l'intégralité (les ~118 fichiers non conformes
      reformatés).
- [ ] Hook pre-commit bloquant si format KO.

**DoD Epic 0 :** le quality gate exécute des tests sur les policies et au moins un grain ;
plus aucun `// TODO handle exceptions` ; format vert.

---

### EPIC 1 — Boucle de jeu jouable (PRIORITÉ ABSOLUE)

**Objectif :** un joueur se connecte, navigue, entre dans une room, marche, chatte,
manipule du mobilier, règle sa room, repart — **sans jamais taper un packet mort**. C'est
la tranche verticale qui transforme « moteur impressionnant » en « jeu jouable ».

**Story 1.1 — Navigator complet (8 % → 100 %)**
*En tant que* joueur, *je veux* parcourir, rechercher et ouvrir des rooms, *afin de*
pouvoir entrer dans le jeu — c'est la porte d'entrée, et c'est le domaine le plus vide.
- [ ] Tous les handlers `Turbo.PacketHandlers/Navigator` (+ `NewNavigator`) implémentés.
- [ ] Catégories, recherche, favoris, mes rooms, rooms populaires renvoient de vraies
      données via les grains.
- [ ] Création de room fonctionnelle de bout en bout.

**Story 1.2 — Entrée / sortie / état de room complets**
- [ ] Handlers d'entrée, de sortie, de heightmap/état initial implémentés.
- [ ] Le joueur reçoit l'état complet de la room à l'entrée (avatars, mobilier, droits).

**Story 1.3 — Mobilier : poser / déplacer / ramasser / utiliser (Room 23 % → 100 %)**
*En tant que* joueur avec les droits, *je veux* manipuler le mobilier, *afin de* décorer
et interagir.
- [ ] Handlers place/move/rotate/pickup/use branchés sur `RoomSecurityModule`.
- [ ] Les checks passent par `CanManipulateFurniAsync`/`CanPlaceFurniAsync`/`CanUseFurniAsync`.
- [ ] Stuff data mis à jour et diffusé aux clients de la room.

**Story 1.4 — Paramètres de room (RoomSettings 0 % → 100 %)**
- [ ] Handlers de réglage (nom, description, droits, accès, max users, etc.) implémentés.
- [ ] Gated par ownership/contrôleur via `GetControllerLevelAsync`.

**Story 1.5 — Finir `RoomSecurityModule`**
- [ ] Remplacer `isGroupRoom = false` hardcodé par la vraie détection.
- [ ] Implémenter `canGroupDecorate` et les branches GroupRights/GroupAdmin.
- [ ] Lever le `// TODO placement rules?` de `CanPlaceFurniAsync`.

**Story 1.6 — Audio de room (Sound 0 %)** *(basse priorité dans l'epic)*
- [ ] Handlers traxx/jukebox si la feature est ciblée pour la v1, sinon descoper
      explicitement.

**DoD Epic 1 :** un compte de test peut faire login → navigator → entrer → marcher →
chatter → poser/déplacer/ramasser un meuble → régler la room → repartir, intégralement,
sans handler stub sur ce chemin. Les droits sont vérifiés à chaque étape.

---

### EPIC 2 — Permissions & modération (quasi fait, à compléter)

**Objectif :** fermer les derniers trous du système de droits déjà câblé.

**Story 2.1 — Login lit/assigne les rôles depuis la DB**
- [ ] Vérifier le chemin compte → `player_account_roles` (le seeder `DefaultRoles` existe).
- [ ] Un nouveau compte reçoit le rôle par défaut adéquat (pas Administrator).
- [ ] Le rang de session client est cohérent avec les capabilities, mais n'est qu'un
      indice UI (la décision serveur reste `IPermissionService`).

**Story 2.2 — Droits en group room**
- [ ] `isGroupRoom` réel + `canGroupDecorate` + niveaux GroupMember/GroupRights/GroupAdmin
      effectifs dans `RoomSecurityModule`.

**Story 2.3 — Outil de modération staff (Moderator 21 % → 100 %)**
*En tant que* staff, *je veux* l'outil de modération (CFH/tickets, alertes, sanctions),
*afin de* gérer l'hôtel.
- [ ] Handlers `Turbo.PacketHandlers/Moderator` implémentés, gated par les capabilities
      `Capabilities.Moderation.*`.
- [ ] Chaque action émet son event d'audit (catégorie Moderation), réussites ET refus.

**Story 2.4 — Tests des policies** *(recouvre Story 0.1, à ne pas dupliquer)*
- [ ] Couverture complète de `RoomSecurityPolicy` et `ModerationPolicy`.

**DoD Epic 2 :** tout droit en jeu passe par une capability **testée** ; le staff opère
partout ; les group rooms sont gérées ; l'outil de modération est fonctionnel et audité.

---

### EPIC 3 — Migration WebApi ASP.NET Core (en cours)

**Objectif :** une seule surface HTTP publique, sous ASP.NET, durcie et testée.

**Story 3.1 — Merger la branche feature**
- [ ] `feat/webapi-aspnetcore-migration` revue et mergée dans `main`.
- [ ] Les 16 tests d'intégration tournent dans le gate.

**Story 3.2 — Finir et vérifier le durcissement**
- [ ] Tous les endpoints migrés (parité avec l'ancien HttpListener).
- [ ] Rate limiting strict vérifié sur `/login`, `/registration/new`, `/ssotoken` (test
      429 déjà présent).
- [ ] CORS explicite + HTTPS/HSTS activables par config.

**Story 3.3 — Supprimer le legacy**
- [ ] `WebApiService.cs` (HttpListener) et `WebApiResponseWriter` retirés.

**DoD Epic 3 :** plus aucun `HttpListener` dans le projet ; surface publique sous ASP.NET,
testée et durcie.

---

### EPIC 4 — Économie & inventaire (l'argent est sensible)

**Objectif :** acheter, recevoir, revendre — chaque mouvement de monnaie passant par le
ledger **audité et testé**.

**Story 4.1 — Inventory complet (19 % → 100 %)**
- [ ] Handlers `Turbo.PacketHandlers/Inventory` implémentés (consulter, déplacer vers la
      room, etc.).

**Story 4.2 — Achat catalogue bout-en-bout (Catalog 39 % → 100 %)**
*En tant que* joueur, *je veux* acheter au catalogue, *afin de* obtenir du mobilier.
- [ ] Flux d'achat complet : page → offre → débit (via ledger) → ajout inventaire.
- [ ] Gestion du club (HC) et des prix membres.

**Story 4.3 — Tests du ledger économique (CRITIQUE)**
*En tant que* dev, *je veux* tester chaque mouvement de monnaie, *afin d'*éviter
duplication/perte de currency silencieuse.
- [ ] Tests sur les opérations du ledger (débit, crédit, idempotence, refus solde
      insuffisant).
- [ ] Invariant testé : aucun chemin ne crédite/débite sans entrée de ledger.

**Story 4.4 — Cadeaux (Gifts)**
- [ ] Flux cadeau complet : achat cadeau → emballage → livraison → ouverture, audité.

**Story 4.5 — Marketplace**
- [ ] Flux complet : mise en vente → recherche → achat → payout, chaque étape au ledger.

**DoD Epic 4 :** acheter / recevoir un cadeau / revendre fonctionne ; chaque transaction
monétaire est dans le ledger ET couverte par un test.

---

### EPIC 5 — Social

**Objectif :** amis, messagerie, groupes fonctionnels.

**Story 5.1 — Amis & messagerie**
- [ ] Handlers `FriendList` (+ messenger) : demandes, acceptation, messages, présence.

**Story 5.2 — Groupes**
- [ ] Création de groupe, gestion des membres, forums (`GroupForums`) — gated par les
      droits de groupe (cf. Epic 2).

**DoD Epic 5 :** un joueur peut ajouter un ami, échanger des messages, créer et gérer un
groupe.

---

### EPIC 6 — Trading

**Objectif :** échange d'objets entre joueurs, sécurisé et atomique.

**Story 6.1 — Flux d'échange complet**
- [ ] Ouverture, ajout/retrait d'objets, lock des deux côtés, confirmation, commit
      **atomique** (les deux inventaires changent ou aucun).
- [ ] Anti-triche : validation serveur de la possession réelle des objets à chaque étape.
- [ ] Event d'audit de l'échange (item_events).
- [ ] Tests de l'atomicité et des cas d'échec (déconnexion en plein trade, objet retiré).

**DoD Epic 6 :** deux joueurs échangent des objets de façon atomique et auditée ;
impossible de dupliquer ou perdre un objet.

---

### EPIC 7 — Ops & conformité

**Objectif :** finaliser la production-readiness de la périphérie déjà solide.

**Story 7.1 — Rétention / purge RGPD de l'audit**
- [ ] Politique de rétention configurable + purge planifiée des tables d'audit.

**Story 7.2 — Accès dashboard distant sécurisé**
- [ ] Reverse proxy / TLS devant le dashboard ASP.NET (aujourd'hui en localhost).

**Story 7.3 — Export OTel** *(si désiré)*
- [ ] Brancher l'ActivitySource/meter existant sur un exporter OpenTelemetry.

**DoD Epic 7 :** audit conforme RGPD ; dashboard accessible à distance en sécurité ;
métriques exportables.

---

## 5. Definition of Done globale (par story)

Une story n'est **Done** que si **tout** est vrai :

1. **Code** : handler/feature implémenté, branché sur les grains via les interfaces, zéro
   logique métier dans les handlers, aucun nouveau stub introduit.
2. **Droits** : toute action sensible passe par une capability (`IPermissionService` /
   policies), jamais par un flag client de confiance.
3. **Audit** : les actions à enjeu (modération, monnaie, items) émettent leur event
   observability, réussite ET refus.
4. **Tests** : au minimum les fonctions pures et les invariants sensibles sont couverts ;
   pour une surface HTTP, un test d'intégration de contrat par route.
5. **Erreur** : les chemins d'échec sont gérés selon la stratégie de l'Epic 0, pas par des
   TODO.
6. **Format** : `csharpier check` vert.
7. **Stubs** : le compteur de handlers stub du domaine concerné a baissé, jamais monté.

---

## 6. Le piège à éviter (rappel stratégique)

Le risque de ce projet n'est pas la compétence — le moteur et l'observability le prouvent.
Le risque est **de mourir d'ampleur** : continuer à construire des systèmes gratifiants
(plus de dashboard, OTel, nouveaux domaines) pendant que la boucle de jeu reste à ~20 %.

La règle d'or de cette roadmap : **ne pas démarrer un epic de périphérie tant que l'Epic 1
(boucle jouable) n'est pas Done.** Un hôtel auquel on peut jouer à 100 % sur un périmètre
réduit vaut infiniment mieux qu'un hôtel à 20 % avec une périphérie de classe mondiale.
