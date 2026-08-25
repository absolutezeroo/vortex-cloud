# VORTEX CLOUD — Architecture cible & workflow IA — V4

**Deep Audit Rewrite de la V3 « Deep Reconciliation » — remplace intégralement les V1, V2 et V3 pour toute planification d'implémentation.**

| | |
|---|---|
| **Objectif** | Spécification autoportante permettant à une IA senior d'auditer, planifier puis refactorer Vortex sans dériver de périmètre, sans perdre les invariants runtime, et sans laisser subsister les fenêtres de perte/duplication de valeur prouvées dans les parcours commerce. |
| **Principe directeur** | Conserver le runtime Orleans de Vortex tel quel ; faire évoluer l'existant (registre de logics, moteur Wired, hooks/gates/specs) ; traiter les frontières de commit cross-grain du commerce comme des frontières de panne ; réduire la surface post-pivot **avant** de l'instrumenter. |
| **Périmètre** | Commerce (Catalog/Gift/Targeted/Marketplace/Wallet), Rooms/Furniture/Wired, Inventory fulfillment, Player/Presence, Protocol/Specs, Reference Data, Persistence/Events, Plugins, Observabilité, Tests, Workflow IA. |
| **Hors périmètre** | Microservices, event sourcing global, CQRS/MediatR généralisé, Orleans Transactions comme substitut au protocole commerce, multi-silo tant que la thèse `MultiSiloReady` n'est pas levée, Dashboard/API/Security (chantier séparé). |
| **Baseline Vortex** | `afc485be58ffd983b8d96430efe8aed620ad0ade` (main). **Re-vérifié le 2026-08-25 après fetch : HEAD local = origin/main = baseline.** |
| **Références** | Skylight3 `14a9531`, Arceus `97db905`, Arceus Wired Plugin `c2e649c`, Daybreak `2dee52a` — tous re-vérifiés égaux à leur branche distante le 2026-08-25. |
| **Stack** | .NET 10 / `net10.0`, Orleans 10.2.1, EF Core 9.0.8 + Pomelo 9.0.0 épinglés ensemble (`AGENTS.md`) [V-AI01]. |
| **Méthode** | Chaque affirmation structurante de la V3 a été re-vérifiée **au niveau ligne** dans le code au baseline (§2.2, tableau de vérification). Les quatre références ont été relues aux SHA figés. Les affirmations Orleans/patterns sont recoupées sur Microsoft Learn (dates de consultation en Annexe C). Ce qui n'a pas pu être établi est `UNKNOWN` ou `NEEDS_EVIDENCE`. |
| **Date** | 25 août 2026 |

---

## Sommaire

1. Résumé exécutif
2. Baseline, vérification ligne à ligne de la V3, hiérarchie de preuve
3. Ce que la V4 corrige et précise par rapport à la V3
4. Synthèse des émulateurs de référence
5. Principes non négociables (V4)
6. Analyse par domaine (17 domaines, grille complète)
7. Architecture cible consolidée
8. Registre de décisions : verdicts sur V1→V3 et nouvelles décisions
9. Migration et ordre des PRs
10. Testing strategy
11. Observabilité
12. Workflow IA anti-dérive V4
13. Critères d'acceptation finaux
— CHANGES FROM V3
— OPEN QUESTIONS / NEEDS EVIDENCE
— FINAL RECOMMENDED REFACTOR ROADMAP
— Annexe A. Dépôts et révisions ; Annexe B. Index des sources code (niveau ligne) ; Annexe C. Références externes

---

# 1. Résumé exécutif

**Le finding central de la V3 est confirmé mot pour mot par le code — et il est même plus profond que la V3 ne l'écrivait.** L'audit ligne à ligne (§2.2) valide chacune des fenêtres de perte/duplication de valeur annoncées : dans `GrantCatalogOfferAsync`, les furnis et badges sont **committés** (`SaveChangesAsync`, ligne 333), synchronisés au cache, notifiés au client et publiés en événements (342–379) **avant** les créations de pets (395), bots (409) et effets (435) — chacune dans son propre commit ou son propre grain ; toute exception dans ces étapes tardives remonte à `ExecutePurchaseAsync` qui rembourse l'intégralité du prix [V-CAT02][V-ECO01]. L'état final « biens conservés + achat remboursé » est atteignable, et le code le documente lui-même sans le voir : le commentaire du bloc effets dit *« A throw here propagates to the wallet's ExecutePurchaseAsync so the purchase auto-refunds »* — un invariant conçu pour un grant atomique, appliqué à un grant qui a cessé de l'être à mesure que les familles de produits s'ajoutaient. Le harness de test ne peut pas représenter cette classe de panne : son fake `IInventoryGrain` est binaire (`GrantThrows ? throw : Task.CompletedTask`) [V-CAT03][V-CAT04].

Les autres fenêtres V3 sont toutes VERIFIED_CODE : Targeted Offers accorde chaque unité dans un commit séparé puis incrémente le compteur d'achat après coup [V-CAT06][V-INV03] ; Marketplace expose quatre fenêtres distinctes (retrait d'item avant insertion d'offre ; annulation committée avant restitution ; claim Sold avant grant avec compensation explicitement best-effort ; `CreditsOwed=0` committé avant crédit wallet) [V-MKT01] ; le wallet n'a **aucune** identité d'opération — `TryDebitAsync`/`CreditBackAsync` prennent des listes de requêtes sans OperationId, donc un retry de refund peut créditer deux fois et un crash entre débit (durable, transaction EF + execution strategy [V-ECO02]) et grant ne laisse aucune trace pour reprendre [V-ECO03]. Enfin `AddEffectAsync` insère inconditionnellement une ligne : un retry post-timeout duplique l'effet [V-EFF01] — preuve directe que « retryable after pivot » exige des receipts, pas seulement de la bonne volonté.

**Les quatre corrections que la V3 apportait à la V2 sont acceptées — toutes vérifiées.** (1) La règle V2 « tout `[AlwaysInterleave]` retourne un immuable » était trop étroite : `SendComposerAsync` est interleavé pour casser le deadlock Presence→Room→Presence et il est sûr parce qu'il n'effectue qu'une mutation synchrone de queue **sans await** avant `Task.CompletedTask` — le commentaire d'interface le dit exactement [V-PRES01][V-PRES02]. La bonne règle est « interleaving-safe by construction », en deux catégories. (2) Le `RoomItemFactory` proposé en V2 est retiré : `RoomItemsProvider` → `RoomObjectModule` → `RoomFurniModule` forment déjà la chaîne nommée matérialisation→attach→lifecycle [V-FUR04][V-FUR05][V-FUR06]. (3) L'exemption plugin du mode strict était une erreur : `RegisterLogic` écrase destructivement (`_logics[key] = reg`) et le disposable retire sans restaurer la registration écrasée — un unload de plugin laisse le furni core sur le fallback [V-FUR01]. (4) `WiredPendingStackExecution` est bien de l'état runtime mutable (`Version`, `DueAtMs`, `NextActionIndex`, `WaitingActionIndex`, `EffectsStarted` en `set`) [V-WIR03] ; le « plan » testable est une projection immuable séparée.

**Ce que la V4 ajoute à la V3** (détail §3) tient en une règle et deux consolidations. La règle : **réduire la surface post-pivot avant de l'instrumenter** — furnis, badges, pets et bots sont tous des lignes écrites via la même `_dbCtxFactory` dans le même grain ; leurs commits séparés sont un artefact d'implémentation, pas une contrainte [V-CAT02][V-INV01][V-INV02]. Consolider le grant local en **un seul commit** dans le turn d'`InventoryGrain` supprime la plus grande fenêtre « biens+refund » avant tout journal ; il ne reste en post-pivot que les étapes réellement cross-grain (effets, gift wrapping, notifications, événements). Les consolidations : le journal d'opération et l'outbox sélective partagent le même support (les transitions terminales du journal *sont* la source du relay d'événements critiques — une écriture durable, un relay, pas deux pipelines) ; et les receipts d'idempotence wallet s'insèrent dans la transaction que `TryDebitAsync` **ouvre déjà** (BeginTransaction + execution strategy vérifiés) — l'API évolue par ajout d'overloads porteurs d'OperationId, sans casse.

| **Priorité** | **Chantier** | **Nature** |
|---|---|---|
| P0-A | Commerce Consistency (Catalog/Gift/Targeted/Marketplace/Wallet) — caractérisation par fault-injection, réduction de surface, identité/journal/receipts, forward recovery | Correctness : pertes et duplications de valeur prouvées |
| P0-B | Wired : câblage `WiredMaxDepth` (20 config vs 8 const) + tests « knob lu » | Correctness config |
| P0-C | Golden tests, manifeste interleaving, guards d'architecture, baseline perf, STATE/ADR | Garde-fous avant extraction |
| P1 | Extraction Wired derrière `IWiredRoomHost` (capabilities) | Testabilité, sémantique identique |
| P1 | Fulfillment planner + stratégies **après** stabilisation du protocole commerce | Structure guidée par le pivot |
| P1 | Outbox critique fusionnée au journal d'opération | Fiabilité des conséquences métier |
| P1/P2 | Durcissement registre Furniture (collisions FAIL par défaut, métrique fallback) | Robustesse plugins |
| P2 | `FurnitureDefinitionSet` atomique ; provenance Specs `repo@sha` ; résorption baselines protocole | Chantiers courts |
| DEFER | PlayerPresence split ; multi-silo ; Economy Kernel générique ; shadow Wired (contingence ADR) | Déclencheurs mesurés |

**Ce que ce document ne demande pas** : pas d'Orleans Transactions pour le commerce (elles gèrent l'état transactionnel Orleans, pas EF/MySQL + side-effects multi-grains [M-ORL-04]) ; pas de grain par opération par défaut ; pas d'outbox généralisée au gameplay ; pas de réécriture du moteur Wired ; pas de RoomItemFactory ; pas d'enum parallèle aux logic names.

---

# 2. Baseline, vérification ligne à ligne de la V3, hiérarchie de preuve

## 2.1 Baseline = HEAD, re-vérifié contre les remotes

`git fetch` puis `rev-parse` sur les cinq dépôts le 2026-08-25 : HEAD local = branche distante = SHA figé partout (Vortex `afc485be…`, Skylight3 `14a9531b`, Arceus `97db905a`, plugin `c2e649c9`, Daybreak `2dee52a0`). Aucun drift à réconcilier ; toutes les conclusions valent pour le baseline **et** l'état courant. La règle « toute session future compare son HEAD au baseline et ne revalide que les `watched_paths` modifiés » (V3 §17) est conservée telle quelle.

## 2.2 Tableau de vérification — chaque affirmation structurante de la V3 relue dans le code

C'est l'apport d'audit propre à la V4 : la V3 affirmait, la V4 a relu. Colonne Statut : `VERIFIED` (exact), `VERIFIED+` (exact, et le code apporte plus que la V3 n'en disait), `REFINED` (vrai mais la V4 précise la forme).

| # | Affirmation V3 | Vérification (fichier : lignes, symbole) | Statut |
|---|---|---|---|
| A1 | Furni+badges committés avant pets/bots/effects ; exception tardive ⇒ refund total ⇒ « goods + refund » | `InventoryGrain.Furni.cs` : parsing 152–296 ; `AddRange` 300 ; badges 306–331 ; **`SaveChangesAsync` 333** ; cache/events/presence 342–379 ; pets 395, bots 409, effects 435 ; commentaire « auto-refunds » sur le bloc effets | **VERIFIED+** — le commentaire prouve que l'invariant refund a été conçu pour un grant atomique |
| A2 | Débit wallet durable au moment du grant | `PlayerWalletGrain.cs` : `TryDebitAsync` 55–130, DbContext neuf par tentative + `IExecutionStrategy` + `BeginTransactionAsync` + `SaveChangesAsync` (130) | VERIFIED |
| A3 | `CreditBackAsync` sans OperationId/dedup ; refund non idempotent ; crash débit→grant sans trace | `IPlayerWalletGrain.cs` : `TryDebitAsync(List<WalletDebitRequest>)`, `CreditBackAsync(List<WalletDebitRequest>)` — aucune identité d'opération sur tout le contrat ; `WalletPurchaseExtensions.cs` : compensation en mémoire, `LogCritical` si le refund échoue | VERIFIED |
| A4 | Harness catalogue tout-ou-rien, incapable de modéliser un grant partiel | `CatalogPurchaseHarness.cs` 174–188 : fake `IInventoryGrain` → `if (GrantThrows) throw; … return Task.CompletedTask` ; `CatalogPurchaseTests.cs` 226 `AGrantThatFails_RefundsTheBuyer` | VERIFIED |
| A5 | Targeted Offers : N grants séparés puis compteur après | `PlayerTargetedOfferGrain.cs` 85–111 : boucle `GrantFurnitureDefinitionAsync` par unité dans le scope compensé ; 124 `IncrementPurchaseCountAsync` après succès ; event « Non-transactional » commenté ; chaque grant unitaire = `Add` + `SaveChangesAsync` propre (`InventoryGrain.Furni.cs` 603–632) | VERIFIED |
| A6 | Marketplace : 4 fenêtres (list/cancel/buy/redeem) | `MarketplacePurchaseGrain.cs` : MakeOffer 70 `RemoveFurnitureAsync` → 99 `SaveChangesAsync` (insert offre) ; Cancel 127–128 `Cancelled`+commit → 132 `GrantFurnitureDefinitionAsync` ; Buy 189–194 claim atomique `ExecuteUpdate` Active→Sold+CreditsOwed → 211 grant → 221–233 revert best-effort commenté « Sold with no item delivered » ; Redeem 302–305 `CreditsOwed=0`+commit → 308–310 `GrantCreditsAsync` | **REFINED** — le claim Buy par `ExecuteUpdate` conditionnel est un **bon** pivot de concurrence ; le manque est la durabilité de la complétion, pas le claim (§6.8) |
| A7 | `SendComposerAsync` est `[AlwaysInterleave]`, sûr par mutation synchrone sans await, requis pour la liveness | `IPlayerPresenceGrain.cs` 17–26 : commentaire deadlock Presence→Room→Presence + « only enqueue … synchronously — no awaits » ; `PlayerPresenceGrain.cs` 112–136 : `EnqueueOutgoing` + `LogAndForget(ProcessOutgoingQueueAsync())` + `Task.CompletedTask` | VERIFIED — la règle V2 « retour immuable » est bien réfutée |
| A8 | Chaîne de création Furniture déjà nommée ⇒ RoomItemFactory redondant | `RoomItemsProvider.cs` 31–161 (matérialise `RoomFloorItem`/`RoomWallItem` depuis rows/snapshots) ; `RoomObjectModule.cs` ; `RoomFurniModule.Floor.cs` | VERIFIED |
| A9 | `RegisterLogic` écrase ; le dispose ne restaure pas la registration écrasée | `RoomObjectLogicProvider.cs` 51–68 : `_logics[key] = reg` ; disposable `TryRemove(KeyValuePair(key, reg))` — retrait conditionnel à « toujours courante », aucune restauration | VERIFIED — l'exemption plugin V2 est bien à rejeter |
| A10 | `WiredPendingStackExecution` = runtime mutable, pas un plan | `WiredPendingStackExecution.cs` 8–28 : init-only (Stack/Actions/Policy/Selected/SelectorPool/Signal/ProcessingContext) + `Version`, `DueAtMs`, `NextActionIndex`, `WaitingActionIndex`, `EffectsStarted` en `get; set;` | VERIFIED |
| A11 | Les boxes ont déjà un contrat sémantique de capabilities | `IWiredExecutionContext.cs` 32–88 : state updates, mouvements floor/wall/user, direction, composer room, chat/move/follow/figure/walk bots, hand items | VERIFIED |
| A12 | Bus d'événements in-process, handlers parallèles, erreurs isolées ; perte possible après commit | `EventRegistry.cs` 42–43 : `HandlerMode = Parallel` ; isolation par catch ; `CatalogPurchaseGrain.cs` 86–123 : `PublishAsync(CatalogPurchasedEvent)` **après** le succès d'`ExecutePurchaseAsync`, hors transaction | VERIFIED |
| A13 | `CatalogPurchasedEvent` alimente la progression (pas que des métriques) | `QuestProgressEventHandlers.cs` 86 `IEventHandler<CatalogPurchasedEvent>` ; **également** `DailyTaskProgressEventHandlers.cs` [V-PROG02] — consommateur supplémentaire non cité par la V3 | VERIFIED+ |
| A14 | Orleans : at-most-once par défaut ; retries ⇒ livraisons multiples possibles ; pas de dédup durable | Microsoft Learn, *Messaging delivery guarantees* [M-ORL-03], consulté 2026-08-25 | VERIFIED |
| A15 | (nouveau V4) Un retry d'`AddEffectAsync` duplique l'effet | `PlayerEffectGrain.cs` 71–91 : insert inconditionnel `PlayerEffects.Add(new …)` | **VERIFIED — preuve V4** que « retryable after pivot » exige des receipts par étape |

**Conclusion de la vérification** : la V3 est factuellement solide ; aucun de ses claims structurants n'est réfuté. Les verdicts V4 (§8) sont donc majoritairement KEEP, avec des MODIFY de *forme* (surface post-pivot, fusion journal/outbox, chemin d'évolution wallet, cadrage Marketplace Buy) — jamais des retours en arrière vers la V2.

## 2.3 Hiérarchie de preuve (inchangée V1→V3)

1. Capture officielle / comportement observé. 2. Client officiel. 3. Documentation officielle Habbo. 4. Code Vortex au SHA (autorité sur Vortex, jamais sur Habbo). 5. Émulateurs de référence (evidence comparative). 6. Sources communautaires spécialisées. 7. Patterns génériques. Un consensus d'émulateurs ne devient jamais une vérité Habbo ; l'inconnu reste `UNKNOWN` ; les choix de livraison sont `ASSUMED/BEST_EFFORT` dans `Vortex.Specs` avec justification. Le niveau 7 justifie des **techniques** (saga, outbox, idempotence) — dans ce document il fonde la *forme* du protocole commerce, jamais un comportement Habbo.

---

# 3. Ce que la V4 corrige et précise par rapport à la V3

La V3 a survécu à l'audit contradictoire ; les lignes ci-dessous sont des raffinements prouvés, pas des renversements.

| # | Point V3 | Verdict V4 | Correction / précision |
|---|---|---|---|
| B1 | Protocole commerce présenté « protocol-first » : OperationId + journal + receipts + recovery appliqués à la topologie actuelle | **MODIFY (règle préalable)** | **Réduire la surface post-pivot avant de l'instrumenter.** Furnis, badges, pets et bots sont des lignes écrites via la même `_dbCtxFactory` dans le même `InventoryGrain` ; leurs commits séparés (Furni 333, Pets 53, Bots 77) sont un artefact, pas une contrainte [V-CAT02][V-INV01][V-INV02]. Un batch local mono-commit supprime la plus grande fenêtre « goods+refund » ; le journal/receipts se dimensionne sur le **résidu** réellement cross-grain (effets [V-EFF01], gift, notifications, événements). La V3 disait « Inventory applies local batch » dans son diagramme sans en faire une règle de conception ni citer la preuve pets/bots. |
| B2 | Journal d'opération + outbox sélective = deux mécanismes durables | **MODIFY (fusion)** | Les événements critiques identifiés (CatalogPurchased → quêtes **et** daily tasks [V-PROG01][V-PROG02], Targeted, Marketplace) sont tous adossés à une opération. Les transitions terminales du journal deviennent la **source** du relay : une écriture durable par transition, un relay at-least-once, dédup consommateur par OperationId/EventId [M-AZ-03][M-AZ-04]. Une table outbox indépendante ne se crée que si un événement critique sans opération apparaît (aucun identifié au baseline). |
| B3 | « Wallet : debit/refund/credit reçoivent OperationId/StepKey » (prescription sans chemin) | **MODIFY (chemin concret)** | `TryDebitAsync` ouvre **déjà** une transaction EF avec execution strategy [V-ECO02] : le receipt s'insère dans cette même transaction (mutation + receipt atomiques). Évolution par **ajout** d'overloads porteurs de `CommerceOperationId`/StepKey retournant le résultat antérieur en replay ; les signatures actuelles restent pour les grants hors commerce pendant la migration, puis `CreditBackAsync` legacy est retiré. `WalletDebitResult` devient sérialisable pour être rejouable depuis le receipt. |
| B4 | Marketplace Buy listé comme fenêtre parmi d'autres | **REFINED** | Le claim `ExecuteUpdate` conditionnel Active→Sold [V-MKT01, l.189–194] est un **pivot de concurrence correct** — il empêche déjà la double vente. Le défaut n'est pas le claim mais la suite : compensation best-effort (revert Active) au lieu de forward recovery durable post-claim. Le fix V4 : claim = pivot journalisé ; après lui, complétion idempotente (grant + notification) rejouable ; le revert n'existe qu'avant pivot. Créditer ce qui est déjà bon évite de le « réparer ». |
| B5 | Catégories AlwaysInterleave (A : lecture immuable ; B : opération synchrone bornée de liveness) décrites en prose | **KEEP + mécanisation** | La propriété qui rend la catégorie B sûre est **l'absence d'await avant la fin de la mutation** [V-PRES01]. Le manifeste devient un fichier de données testé : chaque méthode `[AlwaysInterleave]`/`[Reentrant]`/`MayInterleave` du dépôt doit y figurer avec sa catégorie ; un test d'architecture échoue sur méthode non listée, et vérifie mécaniquement pour la catégorie B qu'aucun `await` ne précède la complétion (analyse du corps ou convention `Task.CompletedTask` retourné + revue). |
| B6 | Principe 11 « le CancellationToken de la requête ne gouverne pas la complétion post-pivot » | **KEEP + preuve du viol actuel** | Aujourd'hui `ct` de la requête traverse tout le grant (pets/bots/effects) — et le commentaire de `WalletPurchaseExtensions` identifie l'annulation comme **la cause la plus fréquente** d'échec du grant [V-ECO01]. Le refund a déjà fait le bon choix (`CancellationToken.None`) ; les étapes post-pivot doivent faire le même : token lié au shutdown de l'hôte, jamais à la connexion. |
| B7 | Targeted Offers : « count cohérent après recovery » exigé sans forme | **MODIFY (forme)** | Le compteur devient une étape journalisée de l'opération (receipt `operationId+step:count`), rejouable ; ou son incrément rejoint la transaction du pivot si le pivot est côté catalogue. La dérive de limite actuelle (grant réussi, crash avant `IncrementPurchaseCountAsync` l.124 [V-CAT06]) est couverte par le même mécanisme que le reste — pas de compteur « spécial ». |
| B8 | Sources outbox : [M-AZ-03] pointe un sample Cosmos DB | **REFINED (sourcing)** | La référence canonique est le guide Transactional Outbox de l'Architecture Center [M-AZ-04] ; le sample reste cité en illustration. Aucun impact de fond. |
| B9 | Structure du document : la V3 a abandonné la grille 17 domaines × 11 sous-sections de la mission | **RESTAURÉ** | La V4 rétablit la grille complète (§6) — les domaines stabilisés y sont volontairement denses et renvoient aux vérifications §2.2 au lieu de re-dérouler. |
| B10 | `AddEffectAsync` implicitement « retryable » dans les steps post-pivot | **NOUVELLE PREUVE** | Insert inconditionnel [V-EFF01] : un retry duplique la ligne. Chaque étape post-pivot obtient son receipt **ou** une écriture naturellement idempotente (upsert conditionnel) — décidé étape par étape dans la slice, jamais supposé. |

---

# 4. Synthèse des émulateurs de référence

Inchangée sur le fond depuis V2/V3 — re-vérifiée aux SHA figés, rappelée ici pour l'autoportance :

| **Projet** | **Leçon retenue** | **Rejet runtime** |
|---|---|---|
| Skylight3 | Lisibilité `Items/{Builders,Interactions}` ; produits catalogue **contributifs** — `ICatalogProduct.ClaimAsync(ICatalogTransactionContext)`, `CatalogProductBadge` → `context.Commands.AddBadge` [S-CAT01][S-CAT02] : la forme du futur planner, débarrassée de sa transaction | Thread dédié + `SpinLock` par room [S-RUN01][S-RUN02] ; `IDbContextTransaction` traversant l'achat entier [S-CAT03][S-CAT04] — un luxe mono-process **structurellement impossible entre grains**, et précisément ce que le protocole commerce V4 remplace par pivot+idempotence |
| Arceus | Registre d'interactions extensible, composant room explicite [A-FUR01] | String+reflection en chemin chaud ; concurrence manuelle |
| Arceus Wired Plugin | Phases de pipeline **nommées** (variables→selectors→conditions→effects→addons) et manager room-scoped [AP-WIR01..04] — la nomenclature des composants d'extraction §6.4 | `ConcurrentHashMap`/`Future`/thread manager ; sémantique embryonnaire (45 fichiers) — jamais une autorité Wired |
| Daybreak | `WiredServices` = catalogue de capabilities [D-WIR01] ; protections runtime (profondeur, rate-limit) [D-WIR03] ; retour d'expérience de coexistence legacy/new [D-WIR02] | Manager global/statique ; cache de piles invalidable [D-WIR04] (Vortex résout live sous turn unique — pas de fenêtre stale à gérer) ; mode parallèle = double mutation, contre-modèle de shadow |
| Vortex | Orleans + ownership fort, live stack, pipeline Wired le plus complet des quatre, hooks/gates/Specs industrialisés | À préserver ; extraire sans réintroduire la concurrence que les autres runtimes doivent gérer eux-mêmes |

Le but reste `best ideas + Vortex constraints`, jamais une moyenne des quatre. Un pattern n'entre que s'il résout une douleur vérifiée dans Vortex et respecte le modèle actor.

---

# 5. Principes non négociables (V4)

Les quinze principes V3 sont conservés ; les amendements sont en gras.

1. `RoomGrain` reste l'unique propriétaire de l'état mutable d'une room ; aucun Grain par item, Wired, avatar, pet, bot ou stack.
2. Orleans reste l'unique scheduler/concurrency boundary de la room. Pas de lock, `SemaphoreSlim`, Thread, `Task.Run` ou callback externe qui mute `RoomLiveState`.
3. `RoomGrain` reste non-réentrant. Tout interleave est **INTERLEAVING-SAFE BY CONSTRUCTION** : catégorie A (lecture pure d'un snapshot immuable/stable) ou catégorie B (opération synchrone bornée **sans await avant complétion**, requise pour la liveness — modèle : `SendComposerAsync` [V-PRES01]). **Toute méthode interleavée figure au manifeste testé (§12.3) ; hors A/B ⇒ ADR.**
4. Timers via `RegisterGrainTimer` ; délais métier en deadlines absolues sur horloge monotone, jamais « N ticks » — la période repart à la résolution du callback [M-ORL-02].
5. Les boxes Wired parlent via `IWiredContext`/`IWiredExecutionContext` ; aucune box n'obtient `RoomGrain`, un `DbContext` ou les collections de `RoomLiveState`. **Le moteur lui-même parle au grain via `IWiredRoomHost` (capabilities) : aucun `Dictionary`/`HashSet`/tableau mutable ne traverse la frontière.**
6. Le registre Furniture existant est la source unique des comportements. Pas de second registry, pas d'enum parallèle aux logic names DB. **Toute collision de clé échoue au chargement — core/core, plugin/core et plugin/plugin — tant qu'un override explicite à pile de registrations n'existe pas comme feature [V-FUR01].**
7. Tout budget configurable a exactement une source de vérité, un consommateur runtime, une métrique et un test de câblage (RFW-101 : `WiredMaxDepth` 20 config vs 8 const).
8. Tout cache/index dérivé déclare source de vérité, invalidation, rebuild, métrique. Une pile Wired n'est jamais cachée : résolue live au fire.
9. Les mutations `StuffData` restent centralisées sur `MarkDirty`/`PersistStuffData` ; aucun chemin d'écriture ne contourne l'invariant historique.
10. Tout flux qui déplace de la valeur déclare : `OperationId`, phases, **pivot explicite**, compensations avant pivot, étapes idempotentes après pivot, recovery owner, événements critiques. **Et d'abord : la surface post-pivot est minimisée par conception — tout ce qui peut rejoindre le commit local du pivot le rejoint (principe B1).**
11. Après un pivot, l'annulation de la requête cliente n'est pas un ordre de rollback : la complétion vit sous un token lié au shutdown, pas à la connexion. **Le refund actuel applique déjà cette règle (`CancellationToken.None`) — l'étendre aux étapes de livraison [V-ECO01].**
12. Les retries n'existent qu'avec idempotence : Orleans est at-most-once par défaut et ne déduplique pas durablement en présence de retries [M-ORL-03]. **Une étape post-pivot est soit couverte par receipt, soit naturellement idempotente — prouvé par test de replay, jamais supposé (contre-exemple : `AddEffectAsync` [V-EFF01]).**
13. Outbox sélective : uniquement les événements métier post-pivot dont la perte change progression/récompense/audit — **implémentée par le journal d'opération (relay sur transitions terminales), pas comme second pipeline** [M-AZ-04]. Room events/tick/Wired restent in-memory.
14. Pas de repository/UoW générique, CQRS/MediatR généralisé, event sourcing global, microservices, transaction ACID globale simulée, Economy framework, **ni Orleans Transactions pour le commerce** [M-ORL-04].
15. Tout comportement Habbo modifié passe d'abord par `Vortex.Specs` ; l'inconnu reste `UNKNOWN`.

---

# 6. Analyse par domaine

Grille complète rétablie (B9). Les domaines stabilisés depuis V2/V3 sont denses et renvoient aux vérifications §2.2 ; les domaines touchés par le P0 commerce sont détaillés. « Rien d'exploitable » = la référence a été relue et n'apporte rien que Vortex n'ait en mieux, ou rien de transposable sous Orleans.

## 6.1 Runtime / Orleans / concurrency

#### CURRENT VORTEX
Silo unique Orleans 10.2.1 ; `RoomGrain` non-réentrant ; tick `RegisterGrainTimer` à étapes ordonnées isolées par `RunTickStepAsync` (un throw n'abat ni le tick ni les flushes) [V-RUN01] ; deadlines absolues + `AdvanceBoundaryPast` pour toutes les cadences. Interleaves existants : lectures de snapshots immuables (`GetSnapshot` room/presence) et `SendComposerAsync` (catégorie B vérifiée, A7). Garde single-silo au démarrage, thèse documentée dans `OrleansHostConfig` [V-MULTI01].

#### WHAT VORTEX DOES WELL
Zéro `Task.Run`/lock/`SpinLock` dans `Vortex.Rooms` ; la sémantique période-après-callback des timers est déjà absorbée par les deadlines [M-ORL-02] ; la reprise post-throw par étape distingue Vortex des trois références.

#### PROBLEMS
(a) Aucun manifeste des méthodes interleavées : la sûreté de `SendComposerAsync` vit dans un commentaire d'interface [V-PRES01], pas dans un test — la règle V2 « retour immuable » aurait d'ailleurs interdit ce cas légitime. (b) L'ordre d'émission des composers `LogAndForget` vis-à-vis des turns suivants n'est pas contractualisé (OQ-5). (c) Rien n'empêche l'ajout d'un nouveau singleton process-local qui aggrave la dette multi-silo en silence.

#### SKYLIGHT3 LESSONS
Thread dédié + `SpinLock` par room [S-RUN01][S-RUN02] : la reconstruction manuelle de ce qu'Orleans donne par activation. Contre-exemple d'onboarding, rien à importer.

#### ARCEUS LESSONS
`IThreadManager` injecté jusque dans le pipeline Wired du plugin [AP-WIR04] : même leçon en Java.

#### DAYBREAK LESSONS
Protections runtime au niveau moteur (profondeur, rate-limit) [D-WIR03] : l'idée « le runtime se protège de ses joueurs » — Vortex l'a déjà (queue bornée, budgets, chain guard).

#### PATTERNS TO REJECT
`[Reentrant]` sur `RoomGrain` ; `RegisterTimer` legacy (interleave par défaut) ; tout lock « par sécurité » ; « N ticks » comme délai métier.

#### TARGET ARCHITECTURE
Inchangée + trois gardes : manifeste interleaving testé (§12.3 — catégories A/B, méthode non listée = échec du test d'architecture, catégorie B vérifiée « aucun `await` avant complétion ») ; test « aucun `[Reentrant]` sur RoomGrain » ; registre testé des composants single-silo (providers `ReloadAsync`, agrégateurs, memory streams) pour que la dette multi-silo cesse de croître en silence.

#### MIGRATION STRATEGY
Aucun déplacement : gel par tests, livré en P0-C (PR-G1).

#### TESTS REQUIRED
Manifeste interleaving complet et exact ; fake clock du tick ; « callback long ≠ chevauchement ni double tick » [M-ORL-02] ; singletons single-silo enregistrés.

#### RISKS
Faibles ; le risque est régressif (un `Task.Run` « temporaire » pendant une extraction) — les tests d'architecture sont la parade.

## 6.2 Rooms

#### CURRENT VORTEX
`RoomGrain` (cœur 613 l.) + ~28 partials ; Modules (Action/Furni/Map/Object/Security/Event/…) et Systems (Wired, Roller, Pathing, Pet, Bot, Chat, Banzai, Freeze, Trading, Moderation, GameTimer, minigames) composés dans l'activation, tous dans le turn [V-RUN01]. `RoomLiveState` + `RoomItemIndex` ; flushes bornés (`MaxDirtyItemsPerFlush=100`, `MaxTileHeightsPerFlush=200`) [V-WIR02].

#### WHAT VORTEX DOES WELL
La séparation Modules/Systems visée par la V1 est en place ; les partials `RoomGrain.Wired.*` sont déjà des façades de 39–405 lignes ; doorbell, modération, trades, mystery boxes hors du cœur.

#### PROBLEMS
Accrétion localisée dans `Furni.Interactive`/`Furni.Edit`/`Settings` (orchestrations détaillées) — pas de switch central de types (le dispatch passe par le registre), donc le chantier est une délégation progressive, pas une réécriture.

#### SKYLIGHT3 LESSONS
Arborescence `Items/{Builders,Interactions}` lisible en une lecture [S-CAT01 contexte] : contrainte de nommage pour la cible, pas d'implémentation à copier.

#### ARCEUS LESSONS
Composants room-scoped enregistrés sur la room [AP-WIR02] : Vortex fait l'équivalent par composition dans l'activation.

#### DAYBREAK LESSONS
Rien d'exploitable (Room God-object dérivé d'Arcturus).

#### PATTERNS TO REJECT
Re-fusionner les Systems dans le grain ; exposer `RoomGrain` entier aux behaviors ; viser une taille de fichier plutôt que l'absence de détails de comportements.

#### TARGET ARCHITECTURE
`RoomGrain` = activation/hydratation, ownership `RoomLiveState`, facets, ordre du tick, boundary de persistance ; les orchestrations Furni.\* migrent vers le Furniture Engine (§6.3), façades d'une ligne conservées.

#### MIGRATION STRATEGY
Une capacité à la fois (use/click, edit, …), sous golden tests ; jamais « tout Furni.\* d'un coup ».

#### TESTS REQUIRED
Golden place/move/rotate/pickup/use + dérivés map/height/pathing (inventaire des existants en P0-C) ; test d'architecture « aucun partial RoomGrain ne référence une classe concrète de logic ».

#### RISKS
Moyens : l'ordre mutation → map/index → event → dirty est facile à casser en déplaçant du code — tests d'ordre nommés avant tout déplacement (canon de style : les tests d'ordre du settlement [V-ECO04]).

## 6.3 Furniture / Room Items

#### CURRENT VORTEX
Type Object + capabilities plates (`FurnitureDefinitionSnapshot.LogicName`, `CanSit/CanLay/...`) ; `FurnitureLogic` (lifecycle, `PersistStuffDataAsync`/`MarkDirty` centralisé, invariant historique commenté) [V-FUR07] ; registre `RoomObjectLogicProvider` (clé (nom, famille), fallback `default_floor/wall` journalisé — « le warning est la to-do list », commentaire vérifié A9) [V-FUR01] ; scan `[RoomObjectLogic]` + factories `ActivatorUtilities` [V-FUR02] ; chaîne de création nommée `RoomItemsProvider` → `RoomObjectModule` → `RoomFurniModule` (A8) [V-FUR04][V-FUR05][V-FUR06] ; hook `check-logic-groups` dérivant le menu Dashboard des attributs [V-FUR03]. 29 logics floor spécialisées + 2 wall + 171 boxes Wired.

#### WHAT VORTEX DOES WELL
Ajout d'un behavior = classe + attribut + tests, zéro modification du cœur ; plugins par le même mécanisme ; contexte borné plutôt que RoomGrain ; le hook logic-groups est déjà un garde-fou de dérive générée.

#### PROBLEMS
(a) `RegisterLogic` écrase destructivement et le dispose ne restaure pas la registration écrasée (A9) : une collision plugin/core suivie d'un unload laisse le furni core sur le fallback — l'« exemption plugin » V2 est précisément le bug. (b) Fallback compté nulle part (la to-do list vit dans les logs). (c) Coût `ActivatorUtilities` par création : measure-first (création/hydratation, pas le tick) — OQ-7.

#### SKYLIGHT3 LESSONS
Builders séparés des interactions : satisfait par la chaîne A8 existante — d'où le REJECT du `RoomItemFactory` V2.

#### ARCEUS LESSONS
`RoomItemFactory` Map+Constructor réflexif [A-FUR01] : valide le choix Vortex (attribut+factory), rien de plus.

#### DAYBREAK LESSONS
`ItemManager` manuel géant [D-FUR01] : l'anti-modèle exact du scan par attribut.

#### PATTERNS TO REJECT
Enum parallèle aux logic names DB (REJECT confirmé) ; second registre ; réécriture de la hiérarchie `FurnitureLogic` ; factory décorative au-dessus de la chaîne A8.

#### TARGET ARCHITECTURE
Registre durci selon la matrice V3 vérifiée : collision core/core = FAIL au boot ; plugin/core = FAIL par défaut ; plugin/plugin = FAIL sauf override explicitement ordonné — l'override n'existe que comme feature à **pile de registrations** (priority/origin, restauration de la précédente au dispose), jamais comme écrasement [V-FUR01]. Métrique `furniture.logic.fallback{logic_name,family}`. Chaîne de création inchangée, documentée.

#### MIGRATION STRATEGY
Durcissement = ajouts sans déplacement (PR-F1) ; la pile de registrations n'est implémentée que si un plugin réel en a besoin (sinon FAIL suffit).

#### TESTS REQUIRED
Résolution par famille (étendre `RoomObjectLogicFamilyTests`) ; collision core/core, plugin/core, plugin/plugin ; hot-unload : la registration core redevient active (test qui échoue aujourd'hui — c'est le point) ; fallback compté ; round-trip StuffData par type.

#### RISKS
Faibles ; attention à ne pas casser le hot reload légitime (re-registration du même plugin après reload = même clé, reg différente — le test d'unload le couvre).

## 6.4 Wired

#### CURRENT VORTEX
`RoomWiredSystem` (1 236 l. + partial Variables 224 l.) + runtime `Vortex.Rooms/Wired/*` + 171 boxes par catégorie. Pipeline intégralement vérifié en V2 et re-confirmé : queue bornée 512 avec drops comptés/tick ; index de triggers par type + `_indexDirty` auto-réparant ; résolution **live** `BuildStackFromTileAsync` ordonnée par object id ; `MatchesEvent` → contexte (Event/Stack/Trigger/Signal) → sélection de base → selectors **unionnés** → add-ons `MutatePolicyAsync` → conditions `PrepareAsync` → `CanTrigger` **avant** l'évaluation des conditions (distinction not-fired vs conditions-false pour la branche négative) → allowance à fenêtre **glissante** → 7 modes de conditions non court-circuités → branches → FirstOnly/Random(historique par pile)/Unseen(cycle) → `PriorityQueue` par deadline à clés versionnées → délais par action + **revalidation de co-location** `IsOnTile` → hooks Before/AfterEffects → flush groupé ; execute-stacks : bypass délibéré des triggers/conditions cibles, sélection héritée, garde de cycle par tiles + `MaxCallChainDepth = 8` (const) [V-WIR01]. Boxes → `IWiredContext`/`IWiredExecutionContext` (capabilities vérifiées A11) [V-WIR05]. RFW-101 : `RoomConfig.WiredMaxDepth = 20` jamais lu [V-WIR02].

#### WHAT VORTEX DOES WELL
Fonctionnellement au-dessus des trois références réunies ; les invariants sont commentés dans le code (le moteur est sa propre spec sur plusieurs points) ; le choix live-vs-cache est correct sous turn unique — Daybreak, multithreadé, avait *besoin* de son index de piles invalidable [D-WIR04], Vortex non.

#### PROBLEMS
(a) RFW-101 (P0-B). (b) Le pipeline complet n'est testable qu'avec un `RoomGrain` quasi entier : `RoomWiredSystem(RoomGrain)` lit `_state`, `_roomConfig`, `MapModule`, `NowMs()` en direct — les briques feuilles sont très testées (33+ fichiers), l'orchestrateur non. (c) Observabilité : logs room + compteurs d'erreurs par type, mais pas de compteurs exportés, pas de `StopReason` normalisé, pas d'`ExecutionId` corrélant les chaînes. (d) La croissance (nouvelles primitives Wired 2.0 [H-COM-01][H-OFF-01]) justifie l'extraction — pas l'état actuel, lisible.

#### SKYLIGHT3 LESSONS
Rien au-delà du classement Triggers/Effects, déjà en place.

#### ARCEUS LESSONS
Phases **nommées** du pipeline plugin (variables→selectors→conditions→effects→addons) [AP-WIR04] : reprises comme frontières de composants et d'étapes de trace — pas le code (`ConcurrentHashMap`/`Future` rejetés).

#### DAYBREAK LESSONS
`WiredServices` = catalogue de capabilities [D-WIR01] : conforte la forme `IWiredRoomActions` ; budgets [D-WIR03] déjà présents ; le mode parallèle [D-WIR02] reste le contre-modèle du shadow (double mutation).

#### PATTERNS TO REJECT
Cache de piles résolues ; exécution hors turn ; scan global des boxes par événement ; `DbContext` dans une box ; exposer les collections de `RoomLiveState` à travers la frontière (garde-fou V3 conservé) ; réécriture parallèle du moteur.

#### TARGET ARCHITECTURE
Extraction aux coutures que le code dessine, au-dessus du host à capabilities :

```
Vortex.Rooms/Wired/Engine/
├── WiredEngine              (orchestrateur OnEvent/ProcessWired/Fire)      ordre 4
├── WiredTriggerIndex        (_triggersByEventType, _timedTriggers, repair) ordre 1
├── WiredStackResolver       (BuildStackFromTileAsync, IsOnTile)            ordre 1
├── WiredExecutionScheduler  (pending map, priority queue, versions)        ordre 2
├── WiredExecutionPolicy     (allowance windows, random, unseen, choice)    ordre 2
├── WiredSelectionEngine     (base, signal, selectors)                      ordre 3
└── WiredCallChainGuard      (tiles, depth CONFIGURÉE, cycles)              ordre 3

IWiredRoomHost (internal)
 ├─ IWiredRoomView        TryGetItem / EnumerateTileFloorStack / TryGetAvatar / ToIdx / NowMs
 ├─ IWiredRoomActions     move item/user, states, bots, composer room       (≈ IWiredExecutionContext côté moteur)
 └─ IWiredDiagnostics     WriteWiredLog / compteurs / trace opt-in
```

`RoomGrain` implémente le host ; les tests le fake-nt (le pattern `FakeFurniAccess` existe déjà). Aucun `Dictionary`/`HashSet`/array mutable ne traverse : `EnumerateTileFloorStack` rend une séquence matérialisée dans le turn, `TryGetItem` un objet, jamais la map. `WiredPendingStackExecution` reste l'état runtime (A10) ; la projection immuable `WiredExecutionPlanSnapshot` (StackId, TriggerId, branche, actions ordonnées, policy, sélections/signal, deadlines) n'existe que pour test/trace, jamais persistée, jamais un DSL.

#### MIGRATION STRATEGY
Ordre du tableau : (0) RFW-101 + compteurs minimaux (P0-B, indépendant) ; (1) host + consommation par interface (diff mécanique) ; (2) TriggerIndex + StackResolver ; (3) Scheduler + Policy (l'état runtime unseen/windows/random/pending part avec) ; (4) SelectionEngine + CallChainGuard ; (5) WiredEngine assembleur. Chaque étape : compile, parité verte, zéro changement de packet ; observabilité (§6.15) posée aux coutures au passage.

#### TESTS REQUIRED
La matrice de parité V2 intégralement, sur host fake + fake clock + RNG injecté : même-tile/tile-différente ; ordre physique sans add-on = sans effet observable ; add-on OR sur son périmètre ; branche négative jamais cumulée à la positive ; execute-stacks bypass cible + garde cycle/profondeur **avec profondeur venant de la config** ; signaux (sources héritées) ; fenêtre glissante (choix Vortex documenté, Habbo `UNKNOWN` — OQ-6) ; unseen cycle ; random anti-répétition ; delay clock-based (tick en retard ≠ durée altérée ≠ double exécution) ; queue pleine (drop compté, FIFO préservé) ; rebuild après add/remove/move/reconfigure et après trigger fantôme ; re-drain zero-delay intra-tick pinné.

#### RISKS
Élevés mais contenus (cœur gameplay) : `CanTrigger`-avant-conditions, re-drain intra-tick, auto-réparation d'index, anti-recalcul des choix — chacun a son test nommé avant tout déplacement.

## 6.5 Player / Presence / Sessions

#### CURRENT VORTEX
Ownership par grains dédiés (Player, Presence + partials, Wallet, Inventory, Navigator, Effect, Clothing, directories) ; `PlayerPresenceGrain.Room.cs` = 225 l. ; outbound joueur contractualisé via `SendComposerAsync` (A7) [V-PRES01][V-PRES02].

#### WHAT VORTEX DOES WELL
Le découpage est déjà meilleur pour Orleans que les compositions mono-process des références ; le seul interleave du contrat est documenté, borné et nécessaire (deadlock Presence→Room→Presence).

#### PROBLEMS
Aucun structurel. `SendComposerAsync` doit passer du commentaire au manifeste testé (§12.3) — c'est l'action unique du domaine.

#### SKYLIGHT3 / ARCEUS / DAYBREAK LESSONS
`User`/`IHabbo`/`HabboInventory` composés : tous confirment « composition explicite », réalisée chez Vortex par grains — ne pas fusionner pour imiter.

#### PATTERNS TO REJECT
Fusion des grains player ; modules-wrappers sans état propre ; split « pour faire comme Daybreak ».

#### TARGET ARCHITECTURE
Statu quo + critère de déclenchement écrit : un partial ne devient module interne que s'il dépasse ~500 l. **et** a un état propre **et** n'est testable qu'à travers le grain entier.

#### MIGRATION STRATEGY / TESTS REQUIRED / RISKS
Aucune migration. Tests : couverture transition-de-room conservée ; entrée manifeste catégorie B pour les deux surcharges `SendComposerAsync`. Risque nul tant que DEFER tenu.

## 6.6 Inventory

#### CURRENT VORTEX
`InventoryGrain` owner ; `GrantCatalogOfferAsync` (A1) : parsing multi-famille (152–296) → `AddRange` furnis + boucle badges avec garde `alreadyOwned` → **commit 333** → sync cache/client/events (342–379) → pets (395, commit propre Pets.cs:53 [V-INV01]) → bots (409, commit propre Bots.cs:77 [V-INV02]) → effets cross-grain (435, `AddEffectAsync` [V-EFF01]) — le commentaire l.428 arme le refund total sur toute exception tardive [V-CAT02]. Grant unitaire `GrantFurnitureDefinitionAsync` = Add + commit par unité [V-INV03].

#### WHAT VORTEX DOES WELL
Invariants métier encodés et commentés (guild extra-data sur `StringKey` seulement, quantité × `product.Quantity`, StuffData reconstruit du blob au grant — le bug « blank legacy default » est documenté dans le code) ; owner unique = la consolidation est purement locale.

#### PROBLEMS
Le dispatch monolithique (constat V2, inchangé) **et** — plus grave — la topologie de commits : quatre commits locaux séparés pour des lignes écrites via la même `_dbCtxFactory` dans le même grain, sous l'invariant refund d'un grant atomique qu'il n'est plus (A1). Chaque nouveau `ProductType` allonge la méthode et ajoute une fenêtre.

#### SKYLIGHT3 LESSONS
Modèle contributif `ICatalogProduct.ClaimAsync(ICatalogTransactionContext)` → `context.Commands.AddBadge` [S-CAT01][S-CAT02] : la forme des stratégies — chaque produit **contribue** des instructions typées à un plan, le coordinateur applique. Sa transaction EF globale [S-CAT03][S-CAT04] reste intransplantable entre grains : c'est le protocole commerce qui la remplace, pas une transaction simulée.

#### ARCEUS LESSONS
`ICatalogPurchaseHandler` : contrat d'achat séparé, sans plus.

#### DAYBREAK LESSONS
Inventory décomposé par famille : conforte le découpage par `ProductType`.

#### PATTERNS TO REJECT
Framework générique de workflows ; transaction EF cross-grain ; déplacer l'ownership hors d'`InventoryGrain` ; stratégies porteuses de `DbContext`/delegates (le plan est **des données**).

#### TARGET ARCHITECTURE
Deux mouvements ordonnés. **(1) Consolidation du grant local (B1, P0-A)** : furnis + badges + pets rows + bots rows rejoignent **un seul commit** dans le turn d'`InventoryGrain` (même factory, même grain — les commits séparés sont un artefact) ; restent post-commit les étapes réellement cross-grain : effets, notifications presence, événements, gift wrapping. La plus grande fenêtre « goods+refund » disparaît par conception avant tout journal. **(2) Structure (P1, après protocole)** : `Vortex.Inventory/Fulfillment/` — `ICatalogProductGrantStrategy` par `ProductType` (registre au démarrage) contribue à un `FulfillmentPlan` (listes typées : furni entities, badges, pet/bot requests, effect grants) ; `CatalogFulfillmentPlanner` = preflight **pur et déterministe** exécuté avant le débit (aucune erreur déterministe ne doit survivre au pivot) ; le plan est sérialisable si le journal d'opération en a besoin ; `InventoryGrain` applique le batch local en un commit, le coordinateur pilote le résidu.

#### MIGRATION STRATEGY
PR-C1 (caractérisation fault-injection) → PR-C4a (consolidation mono-commit, comportement client inchangé) → PR-C4b (journal/receipts sur le résidu) → PR-S1/S2 (stratégies + plan, Badge d'abord, puis Effect, Floor/Wall+guild, Bot/Pet) → suppression des branches.

#### TESTS REQUIRED
Fault-injection par étape committée (le harness binaire A4 ne suffit plus) : « furni+badge committés, pet échoue » ⇒ jamais « goods+refund » après consolidation ; parsing/quantités/guild-stamping par stratégie en pur ; plan attendu par offre (bundles, multi-produits) ; parité bout-en-bout sur 3 offres représentatives avant/après ; replay `OperationId` = no-op/résultat antérieur.

#### RISKS
Élevés (correctness monétaire) : quantité×produit, guild stamping, StuffData-du-blob sont des pièges à figer par tests **avant** la consolidation ; la consolidation change la taille de la transaction locale (mesurer sur gros bundles).

## 6.7 Catalog / Fulfillment (achats standard, cadeaux, targeted offers)

#### CURRENT VORTEX
`CatalogPurchaseGrain` orchestre : préparation → `ExecutePurchaseAsync` (86) → grant inventaire → `PublishAsync(CatalogPurchasedEvent)` (122) **après** succès, hors transaction (A12) [V-CAT01]. Gift : achat encapsulé puis wrapping en present pour le destinataire (fallback plain), recherche d'offre cross-catalogue commentée [V-CAT05]. Targeted : boucle de grants **unitaires** dans le scope compensé, `IncrementPurchaseCountAsync` après succès (A5), event analytics commenté « Non-transactional » [V-CAT06]. Snapshots catalogue immuables `Volatile.Write` [V-REF02].

#### WHAT VORTEX DOES WELL
Frontière achat/fulfillment nette ; snapshots atomiques exemplaires ; le commentaire « Couldn't afford it : the wallet auto-refunded any partial debit » montre que la compensation **pré-pivot** multi-devises est déjà pensée — c'est le post-pivot qui ne l'est pas.

#### PROBLEMS
(a) Targeted : N commits unitaires [V-INV03] dans un scope qui rembourse tout — k-1 produits gratuits possibles ; compteur incrémenté hors opération — dérive de limite sur crash (A5, B7). (b) Gift : le wrapping est une étape post-débit non journalisée — crash entre achat et wrapping = débit sans present. (c) `CatalogPurchasedEvent` publié hors transaction alimente quêtes **et** daily tasks [V-PROG01][V-PROG02] : perte possible après commit (§6.13). (d) Le `ct` de la requête traverse tout le grant (B6).

#### SKYLIGHT3 LESSONS
Voir §6.6 — contributif oui, transaction globale non.

#### ARCEUS LESSONS / DAYBREAK LESSONS
Rien d'exploitable.

#### PATTERNS TO REJECT
Recoupler achat et grant ; catalogue muté in-place ; notifications UI/events non critiques dans la zone qui décide refund/commit (règle V3 conservée).

#### TARGET ARCHITECTURE
Le coordinateur catalogue devient une opération commerce (§6.8) : préflight complet (offre, plan, définitions, parsings) **avant** débit ; pivot recommandé = débit validé après préflight (OQ-2 tranche la variante) ; batch local mono-commit ; résidu (effets, gift wrapping, notifications, événements critiques) = étapes journalisées idempotentes sous token de shutdown ; compteur targeted = étape journalisée (`operationId+step:count`) ou fusionnée au commit pivot (B7) ; `CatalogPurchasedEvent` critique relayé depuis la transition terminale du journal (B2).

#### MIGRATION STRATEGY
Ordre P0-A : caractérisation (PR-C1) → identité/journal (PR-C2) → wallet receipts (PR-C3) → Catalog/Gift/Targeted recovery (PR-C4) — détail §9.

#### TESTS REQUIRED
Crash/restart simulé à chaque phase (préflight, débit, batch local, chaque étape résiduelle, complétion) ; gift : crash entre achat et wrapping ⇒ reprise livre le present, jamais deux ; targeted : k-ième échec ⇒ zéro produit gratuit, count cohérent après recovery ; duplicate retry `OperationId` = no-op.

#### RISKS
Élevés : c'est le P0. Le préflight doit être réellement déterministe (toute validation dépendante d'un état concurrent — solde, stock — appartient au pivot, pas au préflight).

## 6.8 Economy / Wallet / Marketplace / Trading — le P0 Commerce Consistency

#### CURRENT VORTEX
Primitive commune `ExecutePurchaseAsync` : débit durable (transaction EF + execution strategy, DbContext neuf par tentative — commentaire anti demi-état vérifié A2 [V-ECO02]) → grant → compensation `CreditBackAsync` sur `CancellationToken.None` si le grant lève, `LogCritical` si le refund échoue [V-ECO01]. Contrat wallet **sans identité d'opération** : `TryDebitAsync(List<WalletDebitRequest>)`, `CreditBackAsync(List<...>)`, `GrantCreditsAsync(int)` (A3) [V-ECO03]. Marketplace : quatre flux, quatre fenêtres (A6) — MakeOffer retire l'item **puis** insère l'offre ; Cancel committe `Cancelled` **puis** rend l'item ; Buy claim atomique `ExecuteUpdate` Active→Sold+CreditsOwed **puis** grant, revert best-effort commenté « Sold with no item delivered » ; Redeem committe `CreditsOwed=0` **puis** crédite [V-MKT01]. `WiredTradeSettlement` : pivot documenté, refus motivé de la primitive commune, tests d'ordre nommés (`GoodsThatCannotBeSaved_GiveThePaymentBack`, `ARewardTheWalletRefuses_IsNotLoggedAsPaid`) [V-ECO04].

#### WHAT VORTEX DOES WELL
Trois acquis à créditer avant de corriger : le débit lui-même est exemplaire (retry-safe par construction) ; le claim `ExecuteUpdate` conditionnel du Buy est un **pivot de concurrence correct** qui empêche déjà la double vente (B4) ; le settlement Wired prouve que le dépôt sait concevoir un pivot, l'ordonner et le tester — c'est le canon de style du chantier, pas un cas à « harmoniser ».

#### PROBLEMS
Tableau des fenêtres, toutes VERIFIED_CODE (§2.2) :

| Flux | Fenêtre | État final possible |
|---|---|---|
| Catalog/Gift | commit furni+badges (333) → pets/bots/effects fail → refund total | biens conservés + achat remboursé (A1) |
| Targeted | k grants unitaires committés → k+1 fail → refund | k produits gratuits ; crash grant→count = dérive de limite (A5) |
| Marketplace MakeOffer | RemoveFurniture → insert offre | item retiré, offre absente |
| Marketplace Cancel | `Cancelled` committé → grant | item non restitué |
| Marketplace Buy | débit → claim Sold → grant | acheteur débité / offre Sold / item absent ; revert best-effort peut lui-même échouer |
| Marketplace Redeem | `CreditsOwed=0` committé → `GrantCreditsAsync` | crédits vendeur perdus |
| Wallet | refund/credit rejoué sans OperationId | double crédit (A3) ; retry `AddEffectAsync` duplique (A15) |
| Transversal | crash entre débit durable et grant | débit permanent, aucune trace pour reprendre |

Cause commune : chaque commit local est traité comme s'il était global ; l'annulation cliente (`ct`) traverse des étapes post-commit (B6) ; Orleans ne fournit ni dédup durable ni exactly-once — un retry livre potentiellement deux fois [M-ORL-03].

#### SKYLIGHT3 LESSONS
La transaction EF traversante [S-CAT03] est le luxe mono-process auquel le protocole V4 substitue pivot + idempotence — la citer dans la doc du protocole comme « pourquoi pas une transaction ».

#### ARCEUS LESSONS / DAYBREAK LESSONS
Rien d'exploitable (DAO/SQL classiques, mêmes fenêtres non traitées).

#### PATTERNS TO REJECT
Economy Kernel générique (deux topologies prouvées divergentes : purchase compensable vs settlement à pivot [V-ECO04]) ; Orleans Transactions pour ce problème (état transactionnel Orleans ≠ EF/MySQL + side-effects multi-grains [M-ORL-04]) ; transaction ACID globale simulée ; refund après pivot ; grain par opération par défaut ; retries sans receipts.

#### TARGET ARCHITECTURE
Protocole minimal, primitives partagées, orchestration par domaine :

```
CommerceOperationId (ULID)            CommerceOperationState
opérations : catalog_purchase,          Prepared → Debited → Pivoted
gift, targeted, marketplace_list/       → Completing → Completed
cancel/buy/redeem                       | FailedBeforePivot | NeedsIntervention
```

Chaque flux déclare : pivot ; étapes compensables avant ; étapes retryable+idempotentes après (receipt `(scope, owner, operationId, stepKey)` inséré **dans la même transaction** que la mutation quand elle est locale — le débit ouvre déjà cette transaction, B3) ; recovery owner (relance depuis le journal, indépendante de la connexion) ; événements critiques (relay depuis transitions terminales, B2). Réduction de surface d'abord (B1) : tout ce qui peut rejoindre le commit du pivot le rejoint. Application par flux : Catalog/Gift/Targeted §6.7 ; **MakeOffer** — insérer l'offre `PendingRemoval` puis retirer l'item puis activer (ou journaliser le retrait) : plus d'item évaporé ; **Cancel** — `Cancelled` = pivot journalisé, restitution = étape idempotente rejouable ; **Buy** — claim `ExecuteUpdate` = pivot (conservé tel quel), grant + notification = complétion idempotente rejouable, le revert n'existe **qu'avant** pivot (B4) ; **Redeem** — mise à zéro et crédit liés par receipt (`operationId+step:credit`), rejouable. Wallet : overloads additifs porteurs d'`OperationId/StepKey` retournant le résultat antérieur en replay, `WalletDebitResult` sérialisable ; signatures legacy conservées pendant la migration puis `CreditBackAsync` nu retiré (B3). Après épuisement des retries : `NeedsIntervention` + alerte corrélée — jamais de compensation inventée.

#### MIGRATION STRATEGY
PR-C1 caractérisation (les fenêtres du tableau deviennent des tests rouges) → PR-C2 identité/journal/états + observabilité → PR-C3 receipts wallet → PR-C4 Catalog/Gift/Targeted (consolidation B1 incluse) → PR-C5 Marketplace (quatre flux) → PR-C6 relay des événements critiques. Chaque PR laisse le comportement client inchangé hors fenêtres corrigées.

#### TESTS REQUIRED
Crash simulé à **chaque** frontière du tableau + recovery vérifiée sur l'état final métier (pas « refund appelé ») ; replays par `OperationId/StepKey` sur debit/refund/credit/grant/effect = une seule application, résultat stable ; `AGrantThatFails_RefundsTheBuyer` conservé pour le pré-pivot uniquement ; tests d'ordre du settlement intouchés (canon).

#### RISKS
Très élevés côté métier (valeur), moyens côté code : le protocole est additif et par flux. Risque principal : sur-générification — le garde-fou est le REJECT du kernel générique et la règle « troisième flux avant abstraction ».

## 6.9 Social / Progression / Collectibles

#### CURRENT VORTEX
Projets séparés (Social 23, Progression 37, Collectibles 8 fichiers) ; `QuestProgressEventHandlers` et `DailyTaskProgressEventHandlers` consomment `CatalogPurchasedEvent` (A13) [V-PROG01][V-PROG02].

#### WHAT VORTEX DOES WELL
Découpage fait ; les consommateurs de progression sont identifiés et localisés.

#### PROBLEMS
Les handlers ne dédupliquent pas par événement : quand le relay at-least-once arrive (B2), un événement rejoué incrémenterait deux fois une quête. La couverture de tests globale de ces projets reste à inventorier (OQ hérité, non bloquant).

#### SKYLIGHT3 / ARCEUS / DAYBREAK LESSONS
Rien d'exploitable de spécifique.

#### PATTERNS TO REJECT
Re-fusion dans Players ; outbox généralisée au gameplay.

#### TARGET ARCHITECTURE
Consommateurs des événements relayés idempotents par `OperationId/EventId` (garde locale par handler concerné — quêtes, daily tasks, audit) ; le reste du bus inchangé.

#### MIGRATION STRATEGY / TESTS REQUIRED / RISKS
Livré avec PR-C6 ; test : événement rejoué = progression inchangée ; risque faible.

## 6.10 Protocol / Revisions / PacketHandlers

#### CURRENT VORTEX
`Vortex.Protocol` 1 091 fichiers ; `Vortex.Revisions` (défaut `Revision20260701` embarqué, révisions additionnelles en plugin) ; handlers orchestration-only, hooks header-registry (14 baselinés) et wire-conflicts (23 baselinés) [V-AI01][V-AI03].

#### WHAT VORTEX DOES WELL
Frontière comptée, hookée, baselinée — l'état le plus industrialisé des quatre codebases.

#### PROBLEMS
Baselines = dette connue sans propriétaire de résorption (P2, entrée par entrée).

#### LESSONS (3 réfs)
Rien d'exploitable ; le doc d'architecture Daybreak reste une evidence secondaire fonctionnelle pour les specs.

#### PATTERNS TO REJECT
Logique métier dans les handlers ; payloads placeholder (interdits par contrat existant).

#### TARGET / MIGRATION / TESTS / RISKS
Statu quo + plan de résorption (PR-Q2) ; hooks déjà dans FastCheck/QualityGate ; risque faible.

## 6.11 Habbo Specs / behavioral conformance

#### CURRENT VORTEX
Moteur + CLI (`analyze`, `conflicts`, `unknowns --severity`, `bootstrap` reproductible, `validate` dans FastCheck) ; 741 unknowns classés ; blocs `verified:/manual:` survivant à la régénération ; `SpecWorkspace` détecte les arbres externes mais fusionne tout layout Arcturus-like sous l'origine `arcturus` [V-SPEC01].

#### WHAT VORTEX DOES WELL
La hiérarchie de preuve §2.3 est exécutable, pas déclarative ; specs diffables en PR.

#### PROBLEMS
Provenance non repository-aware : Daybreak (dérivé d'Arcturus) et Arcturus = une seule origine — deux preuves fusionnées.

#### LESSONS (3 réfs)
Les dépôts sont des **entrées** du moteur, pas des modèles pour lui.

#### PATTERNS TO REJECT
Vote majoritaire ; promotion communautaire en autorité.

#### TARGET ARCHITECTURE
Origines `reference:<repo>@<sha>` (détection : remote git, fichiers signatures) ; schéma V3 conservé : `client:flash:<rev>`, `client:nitro:<rev>`, `capture:official:<id>`, `reference:*@<sha>`, `vortex@<sha>` → claim matrix → conflicts/unknowns/confidence.

#### MIGRATION / TESTS / RISKS
Chantier isolé `SpecWorkspace` + re-scan (PR-Q1) ; fixtures Daybreak≠Arcturus ; non-régression `bootstrap` ; attention à migrer les origines existantes sans invalider les 741 unknowns.

## 6.12 Reference Data / caching

#### CURRENT VORTEX
`FurnitureDefinitionProvider` publie ById puis ByName en deux affectations non atomiques (dédoublonnage ByName commenté) [V-REF01] ; `CatalogSnapshotProvider` = pattern correct (build complet → `Volatile.Write`) [V-REF02].

#### WHAT VORTEX DOES WELL
Le pattern cible est déjà la norme locale ; il manque un provider.

#### PROBLEMS
Fenêtre de version mixte pendant un reload admin — probabilité faible, coût du fix quasi nul.

#### LESSONS / PATTERNS TO REJECT
Convergence Skylight snapshot ; rejeter : multi-index publiés séparément, verrous de lecture.

#### TARGET / MIGRATION / TESTS / RISKS
`FurnitureDefinitionSet { ById, ByName, Version }` publié en une écriture, version exportée en métrique ; diff mécanique (PR-Q1) ; test « lecteur ne voit que N ou N+1 » ; risque minime.

## 6.13 Persistence / Events

#### CURRENT VORTEX
EF Core 9 + Pomelo épinglés ; memory-first rooms : dirty tracking centralisé [V-FUR07], flushes bornés fin de tick + désactivation best-effort [V-RUN01] ; `Vortex.Events` in-process, `EventRegistry` `HandlerMode = Parallel` + isolation d'erreurs (A12) [V-EVT01][V-EVT02].

#### WHAT VORTEX DOES WELL
Trois niveaux d'état (statique/éphémère/persistant) effectifs ; flush borné anti-rafale ; le bus gameplay est simple et rapide — exactement ce qu'il doit être.

#### PROBLEMS
Deux besoins confondus par défaut : perte tolérée (rooms — fenêtre depuis le dernier flush, `OnDeactivate` non garanti en panne) et perte intolérable (conséquences métier post-pivot). Un crash entre commit métier et `PublishAsync` perd l'événement — prouvé par l'ordre du code (A12) ; c'est le problème dual-write état+événement que l'outbox résout [M-AZ-03][M-AZ-04].

#### LESSONS (3 réfs)
Rien d'exploitable (Skylight : transaction locale ; Arceus/Daybreak : DAO).

#### PATTERNS TO REJECT
Repository/UoW générique ; durabilité événementielle généralisée ; SaveChanges depuis behaviors ; second pipeline outbox indépendant du journal (B2).

#### TARGET ARCHITECTURE
Rooms : statu quo + documentation de la fenêtre de perte par domaine (items, pets, variables Wired permanentes) et de l'idempotence au rechargement. Commerce : journal d'opération durable (table simple, queryable pour ops/recovery — pas un grain par opération) dont les transitions terminales alimentent le relay at-least-once des événements critiques ; consommateurs concernés idempotents (§6.9). `EventSystem` inchangé pour tout le reste.

#### MIGRATION / TESTS / RISKS
PR-C2 (journal) + PR-C6 (relay) ; tests : crash commit→publish ⇒ l'événement critique part quand même (depuis le journal), événement rejoué dédupliqué ; round-trip persistance Wired conservé ; risque faible-moyen.

## 6.14 Plugins / extensibility

#### CURRENT VORTEX
Lifecycle/rollback/hot reload ; points d'extension réels : logics par attribut (même processor, ServiceProvider plugin composé avec l'host [V-FUR01][V-FUR02]), révisions protocole par plugin [V-AI01].

#### WHAT VORTEX DOES WELL
Extensible sans transformer Wired en plugin (contrats forts + hot path + garanties de turn — choix maintenu).

#### PROBLEMS
La collision de clés est le seul trou (A9) : écrasement destructif + dispose sans restauration ⇒ l'unload d'un plugin en collision laisse le core sur fallback.

#### ARCEUS LESSONS
Le plugin Wired prouve packets + interactions + composant room-scoped par plugin — Vortex offre l'équivalent mieux gardé ; exemple pour la doc plugin.

#### DAYBREAK LESSONS
`PluginManager` statique/global [D-PLUG01] : contre-exemple.

#### PATTERNS TO REJECT
Statics globaux ; exemption de collision (le bug, pas la feature) ; Wired en plugin.

#### TARGET / MIGRATION / TESTS / RISKS
Politique de collision §6.3 (FAIL partout, override = pile de registrations si un besoin réel émerge) ; PR-F1 ; test hot-unload-restaure ; risque faible.

## 6.15 Observability

#### CURRENT VORTEX
Métriques de tick par étape opt-in (« one boolean and nothing else »), log channel Wired par room + compteurs d'erreurs par type [V-WIR01] ; `Vortex.Observability` ; agrégateurs process-local (dette multi-silo connue [V-MULTI01]).

#### WHAT VORTEX DOES WELL
L'instrumentation du tick est au bon endroit et au bon coût ; le log Wired par room est déjà l'outil de debug in-game.

#### PROBLEMS
Wired : pas de compteurs exportés, pas de `StopReason` normalisé, pas d'`ExecutionId`. Commerce : **aucun** signal d'opération — une saga sans observabilité est « peut-être incohérente longtemps » (formule V3, conservée).

#### DAYBREAK LESSONS
Le debug-channel de `WiredServices` valide le log room-scoped ; rien à copier.

#### PATTERNS TO REJECT
Trace toujours-active ; logs texte massifs à la place de compteurs.

#### TARGET ARCHITECTURE
Wired : `ExecutionId/ParentExecutionId`, `StopReason` normalisé (no-match, condition-false, execution-limit, cycle, depth, queue-drop, stale-stack, target-missing, exception), compteurs received/ignored/dropped/processed + rebuilds + delayed-revalidation-cancelled, p95/p99 du step par room opt-in, trace room-scoped opt-in. Commerce : par opération — OperationId, flow, phase, pivot timestamp, step courant, attempts, last error, age, recovery state ; alertes : stuck post-pivot, receipt conflict, refund failure, relay backlog/age/dead-letter, `NeedsIntervention`. Version du `FurnitureDefinitionSet` exportée.

#### MIGRATION / TESTS / RISKS
Wired : posée aux coutures pendant l'extraction ; Commerce : dans PR-C2 (le journal EST la source des signaux). Tests : chaque StopReason atteint par un scénario ; chaque état d'opération observable. Risque faible.

## 6.16 Testing

#### CURRENT VORTEX
~120 fichiers Rooms.Tests (33+ Wired, fakes, tests d'ordre du settlement, round-trips), projets par domaine ; gates FastCheck/QualityGate + auto-test des hooks [V-AI03]. Harness catalogue binaire (A4) — le trou central.

#### WHAT VORTEX DOES WELL
La culture « tests d'ordre nommés qui pinnent une sémantique » existe ; les fakes Wired montrent la voie du host.

#### PROBLEMS
(a) Aucune classe de test ne sait dire « le step k a committé, le step k+1 lève » — les fenêtres §6.8 sont invisibles au harness. (b) Le pipeline Wired n'est pas testable sans grain. (c) Pas de benchmark baseline versionné.

#### LESSONS (3 réfs)
Rien d'exploitable (les trois références sont en dessous).

#### PATTERNS TO REJECT
`Thread.Sleep`/RNG global ; instancier un RoomGrain pour tester une box ; « refund appelé » comme preuve de sûreté d'un grant multi-produit.

#### TARGET ARCHITECTURE
Quatre étages : (1) **fault-injection commerce** — harness à étapes qui marque chaque step committé et lève au suivant, crash/restart simulé à chaque frontière, assertions sur l'état final métier + recovery + replays ; (2) briques (existant) ; (3) pipeline Wired sur host fake + fake clock + RNG injecté = matrice de parité complète, chaque règle taguée `VERIFIED_CODE | VERIFIED_CAPTURE | VERIFIED_PUBLIC_DOC | BEST_EFFORT` ; (4) tests d'architecture (manifeste interleaving §12.3, non-réentrance, no-lock/no-Task.Run, knobs câblés, partials sans logics concrètes, singletons single-silo, schéma STATE.yaml). Benchmarks versionnés : room vide/chargée, event storm, no-trigger ≈ O(1), hydratation de milliers d'items (OQ-7).

#### MIGRATION / RISKS
PR-C1 et PR-G1 d'abord (les tests précèdent les fixes et les extractions). Risque : golden tests qui figent un bug — parade : tagging d'evidence + `Specs analyze` avant de pinner une règle protocolaire.

## 6.17 AI development workflow

#### CURRENT VORTEX
`AGENTS.md` canonique (stack, skills par déclencheurs de chemins, règles Orleans, checklists) ; `CLAUDE.md` (ordre de contexte, automations) ; hooks + auto-test ; agents `grain-rules-reviewer`, `wire-truth-auditor` ; gates MSBuild ; Specs CLI ; `docs/patterns/` ; audit daté [V-AI01..V-AI04].

#### WHAT VORTEX DOES WELL
Les mécanismes d'application existent et sont **exécutables** — le workflow n'a jamais été le maillon faible ; il lui manque la mémoire.

#### PROBLEMS
(a) Pas d'état inter-sessions (STATE.yaml) ; (b) pas d'ADRs ; (c) pas de contrat de slice ; (d) rien ne dit quels audits restent valides quand le HEAD bouge ; (e) aucun reviewer « economy » pour les chemins commerce.

#### LESSONS (3 réfs)
Sans objet.

#### PATTERNS TO REJECT
Système parallèle dupliquant hooks/gates ; audits « éternellement valides » ou « systématiquement à refaire » ; une seule longue session.

#### TARGET ARCHITECTURE
§12 : infra existante + STATE.yaml à invalidation `watched_paths`/SHA (apport V3 conservé), ADRs, contrat de slice avec extension commerce, manifeste interleaving, reviewer economy.

#### MIGRATION / TESTS / RISKS
PR-G1 crée `docs/architecture-v4/` + ADR-000 ; QualityGate valide le schéma STATE ; risque : le théâtre de workflow — parade : tout artefact non branché à une gate est supprimé.

---

# 7. Architecture cible consolidée

## 7.1 Vue globale

```
Network / PacketHandlers                    (orchestration-only, hooké)
          │ Orleans call
   ┌──────┴────────────┬──────────────────────────┐
RoomGrain          Player grains             InventoryGrain
[owner, turn]      (Player/Presence/          │
   │                Wallet/Effect/…)     Fulfillment (planner+stratégies+plan)
   ├ Modules            │                      │
   ├ Systems       receipts wallet        batch local mono-commit
   │  └ WiredEngine ─► IWiredRoomHost          │
   ├ Furniture Engine (registre durci,    Coordinateurs commerce
   │  chaîne Provider→ObjectModule→       Catalog / Gift / Targeted /
   │  FurniModule, logics)                Marketplace  [pivot explicite]
   └ RoomLiveState + persistance               │
                                     CommerceOperation Journal
                                     (états, receipts, recovery)
                                          │ transitions terminales
                                     relay at-least-once ─► consumers
                                     (quêtes, daily tasks) idempotents
Reference Data : sets immuables Volatile.Write   ·   Vortex.Specs : oracle (repo@sha)
```

Le runtime Room reste local/actor et hautement cohérent ; les parcours cross-grain qui déplacent de la valeur deviennent des opérations explicitement recoverables. Tout reste dans le monolithe modulaire — les frontières de commit sont simplement traitées comme des frontières de panne.

## 7.2 Contrats structurants

**`IWiredRoomHost`** (internal `Vortex.Rooms`) = `IWiredRoomView` (TryGetItem, EnumerateTileFloorStack, TryGetAvatar, ToIdx, NowMs, `IWiredLimits` câblé) + `IWiredRoomActions` (mouvements item/user, states, bots, composer room — l'équivalent moteur d'`IWiredExecutionContext` [V-WIR05]) + `IWiredDiagnostics` (log, compteurs, trace). Chaque ajout au contrat = justification en PR ; aucune collection mutable ne traverse.

**Primitives commerce** (`Vortex.Primitives/Commerce/`) : `CommerceOperationId` (ULID), `CommerceOperationState`, `CommerceOperationDescriptor` (flow, pivot, steps déclarés), receipt `(scope, owner, operationId, stepKey)` ; journal par domaine dans `Vortex.Database` (table simple queryable). Les coordinateurs restent dans leurs projets (Catalog, Marketplace) ; **aucun** projet « Vortex.Economy ».

**Manifeste interleaving** (`docs/architecture-v4/interleaving-manifest.yaml`) : chaque méthode `[AlwaysInterleave]/[Reentrant]/MayInterleave` avec catégorie A/B, justification, propriétaire — consommé par un test d'architecture (§12.3).

Direction des dépendances inchangée : seuls les contrats réellement cross-project montent dans `Vortex.Primitives` ; les composants Wired restent internes à `Vortex.Rooms`.

---

# 8. Registre de décisions : verdicts V1→V3 et nouvelles décisions

## 8.1 Verdicts consolidés

| Décision (origine) | Verdict V4 | Rationale |
|---|---|---|
| RoomGrain unique owner (V1) | KEEP | Vérifié de bout en bout ; gelé par tests d'architecture. |
| Aucun grain par item/box/avatar (V1) | KEEP | Détruirait la résolution live et l'ordre des mutations. |
| Registre Furniture = existant durci (V2) | KEEP + HARDEN | Collision policy A9, métrique fallback ; pas de second registre. |
| Enum key Furniture (V1) | REJECT (confirmé) | LogicName DB/client = vocabulaire canonique. |
| RoomItemFactory (V2) | REJECT (confirmé V3) | Chaîne Provider→ObjectModule→FurniModule vérifiée A8. |
| Exemption plugin du mode strict (V2) | REJECT | Écrasement destructif sans restauration au dispose (A9) ; FAIL partout, override = pile de registrations. |
| Règle interleave « retour immuable » (V2) | REJECT (confirmé V3) | `SendComposerAsync` est le contre-exemple légitime (A7) ; remplacée par catégories A/B **mécanisées** (B5). |
| IWiredRoomAccess à collections (V2) | MODIFY → `IWiredRoomHost` capabilities | Aucune collection mutable ne traverse (garde-fou V3 conservé). |
| WiredPendingStackExecution = plan (V2) | MODIFY (confirmé V3) | Runtime mutable vérifié A10 ; projection `WiredExecutionPlanSnapshot` test/trace uniquement. |
| Shadow Wired (V1/V2) | CONTINGENCY (confirmé) | Extraction + parité suffisent ; parallèle Daybreak = double mutation [D-WIR02]. |
| TriggerIndex, live stack, scheduler, variables (V2) | KEEP | Vérifiés ; extraction sans changement sémantique. |
| WiredMaxDepth (V2 RFW-101) | FIX P0-B | 20 config vs 8 const ; câblage + test « knob lu » ; valeur via OQ-1. |
| Fulfillment = surtout structure (V2) | MODIFY (confirmé V3) | La cohérence précède la structure ; le plan devient l'entrée validée de l'opération. |
| Economy Kernel (V1→V3) | REJECT for now (confirmé) | Topologies divergentes prouvées [V-ECO01][V-ECO04] ; primitives de sûreté seulement. |
| **Commerce safety primitives (V3)** | **KEEP P0 — renforcé** | Toutes les fenêtres VERIFIED_CODE (§2.2) + preuves V4 (A15, V-PROG02). |
| **Réduction de surface post-pivot (V4, B1)** | **NEW — règle préalable** | Batch local mono-commit avant journal/receipts ; supprime la plus grande fenêtre par conception. |
| **Fusion journal/outbox (V4, B2)** | **NEW** | Transitions terminales = source du relay ; un pipeline durable, pas deux [M-AZ-03][M-AZ-04]. |
| **Chemin wallet par overloads (V4, B3)** | **NEW** | Receipt dans la transaction que `TryDebitAsync` ouvre déjà ; évolution additive, retrait du legacy en fin. |
| **Buy claim = pivot conservé (V4, B4)** | **NEW (cadrage)** | Le claim `ExecuteUpdate` est correct ; le fix est la complétion durable, pas le claim. |
| Outbox sélective (V3) | KEEP (forme B2) | Uniquement conséquences métier post-pivot critiques ; gameplay in-memory. |
| Orleans Transactions pour le commerce (V3) | REJECT (confirmé) | Hors périmètre du problème (EF/MySQL + side-effects multi-grains) [M-ORL-04]. |
| PlayerPresence split (V1→V3) | DEFER (confirmé) | Partials petits ; seuils écrits §6.5. |
| FurnitureDefinitionSet atomique (V2) | KEEP | Fix local, pattern déjà normé [V-REF01][V-REF02]. |
| Specs provenance repo@sha (V2/V3) | KEEP | Empêche la fusion Daybreak/Arcturus [V-SPEC01]. |
| Workflow STATE/ADR/slices + watched_paths (V2/V3) | KEEP + IMPROVE | Manifeste interleaving §12.3 + reviewer economy + extension commerce du contrat de slice. |
| Multi-silo (V1→V3) | DEFER (confirmé) | Thèse `MultiSiloReady` non levée [V-MULTI01] ; registre testé pour geler la dette. |
| Dashboard/API/Security | Chantier séparé (confirmé) | Jamais mélangé aux PRs de ce document. |

## 8.2 Décisions nouvelles V4 (identifiants pour les ADRs)

**D-V4-1** Réduction de surface post-pivot avant instrumentation (B1) — P0-A. **D-V4-2** Journal d'opération = source du relay d'événements critiques (B2) — P0-A/P1. **D-V4-3** Receipts wallet dans la transaction de débit existante, évolution par overloads (B3) — P0-A. **D-V4-4** Marketplace Buy : claim conservé comme pivot, forward recovery post-claim (B4) — P0-A. **D-V4-5** Manifeste interleaving mécanisé, catégorie B = « aucun await avant complétion » vérifié (B5) — P0-C. **D-V4-6** Token de complétion post-pivot lié au shutdown, jamais à la connexion (B6, preuve du viol actuel : `ct` traverse le grant) — P0-A. **D-V4-7** Compteur targeted = étape journalisée ou fusionnée au pivot (B7). **D-V4-8** Toute étape post-pivot prouvée idempotente par test de replay, jamais supposée (B10, contre-exemple A15). **D-V4-9** Politique de collision registre : FAIL core/core, plugin/core, plugin/plugin ; override = feature à pile de registrations (A9). **D-V4-10** Grille documentaire 17×11 rétablie comme format canonique des révisions futures (B9).

---

# 9. Migration et ordre des PRs

| Étape | PR | Contenu | Risque |
|---|---|---|---|
| P0-C | **PR-G1** | Workflow files (STATE.yaml, ADR-000, contrats de slice), manifeste interleaving + test, guards no-lock/no-Task.Run/non-réentrance, registre single-silo, benchmark baseline versionné, inventaire golden tests | Faible |
| P0-B | **PR-G2** | RFW-101 : câblage `WiredMaxDepth` (valeur OQ-1), test « tout knob Wired\* a un lecteur », compteurs Wired minimaux | Faible |
| P0-A | **PR-C1** | Caractérisation commerce par fault-injection : les 8 fenêtres §6.8 deviennent des tests (rouges) ; aucun fix structurel | Faible — révèle |
| P0-A | **PR-C2** | `CommerceOperationId` + journal + états + pivot model + observabilité d'opération | Moyen |
| P0-A | **PR-C3** | Receipts wallet : overloads `OperationId/StepKey`, replay = résultat antérieur, `WalletDebitResult` sérialisable | Élevé (correctness) |
| P0-A | **PR-C4** | Catalog/Gift/Targeted : préflight déterministe, **consolidation mono-commit (D-V4-1)**, résidu journalisé idempotent sous token shutdown, compteur targeted journalisé, forward recovery + tests crash/replay | Élevé |
| P0-A | **PR-C5** | Marketplace : quatre flux en machines d'états journalisées (MakeOffer réordonné, Cancel/Redeem receipts, Buy complétion durable post-claim) | Élevé |
| P1 | **PR-C6** | Relay des événements critiques depuis les transitions terminales + dédup consommateurs (quêtes, daily tasks) + backlog/age/dead-letter | Moyen |
| P1 | **PR-W1** | `IWiredRoomHost` + consommation par interface (diff mécanique, zéro sémantique) | Faible |
| P1 | **PR-W2** | Extraction TriggerIndex + StackResolver | Moyen |
| P1 | **PR-W3** | Extraction Scheduler + Policy (état runtime inclus) + diagnostics structurés | Élevé contenu |
| P1 | **PR-W4** | SelectionEngine + CallChainGuard + WiredEngine assembleur | Moyen |
| P1 | **PR-S1** | Fulfillment : stratégies pures + `FulfillmentPlan` + planner (préflight), Badge d'abord | Moyen |
| P1 | **PR-S2** | Migration Effect, Floor/Wall+guild, Bot/Pet ; suppression des branches | Moyen |
| P1/P2 | **PR-F1** | Durcissement registre Furniture (collision policy, métrique fallback, test hot-unload) | Faible |
| P2 | **PR-Q1** | `FurnitureDefinitionSet` atomique ; provenance Specs `repo@sha` | Faible |
| P2 | **PR-Q2** | Résorption baselines protocole ; doc fenêtre de perte persistance ; observabilité Wired complète si reliquat | Faible |
| Clôture | **PR-Z1** | Benchmarks finaux vs baseline, docs « ajouter un furni / un Wired / un flux commerce », revue ADRs, matrice d'acceptation | Faible |

Contraintes d'ordre : PR-C1 précède tout fix commerce (les tests rouges sont la spécification) ; PR-C2/C3 précèdent C4/C5 ; PR-S1 attend C4 (« sinon on nettoie une méthode puis on la re-découpe autour du pivot » — V3, conservé) ; PR-W1 peut courir en parallèle du commerce (fichiers disjoints) ; PR-G1/G2 immédiats. **Risques sémantiques nommés** (bloquants tant que non pinnés par test) : ordre `CanTrigger`→conditions ; re-drain zero-delay ; auto-réparation d'index ; fenêtre glissante ; ordre mutation→map→event→dirty ; quantité×produit et guild stamping ; StuffData-du-blob au grant ; pivot du settlement (jamais « harmonisé ») ; claim Buy conservé tel quel.

---

# 10. Testing strategy

**Fault-injection commerce d'abord.** Harness à étapes : chaque step déclare `Committed` quand son effet durable est appliqué ; le harness peut lever à l'étape k+1, tuer/redémarrer l'orchestration à chaque frontière, rejouer par `OperationId`. Assertions sur l'**état final métier** (soldes, inventaire, offres, compteurs, présence du present) et sur la recovery — jamais seulement « refund appelé ». Matrice bloquante : les 8 fenêtres §6.8 + gift wrapping + duplicate stepKey wallet (une seule application) + replay `AddEffectAsync` sous receipt (une seule ligne) + crash commit→publish (l'événement critique part depuis le journal ; rejoué = progression inchangée).

**Wired** : matrice de parité complète (§6.4) sur host fake + fake clock + RNG injecté ; profondeur venue de la config (preuve RFW-101).

**Furniture** : famille, collision policy (4 cas), hot-unload-restaure, fallback compté, lifecycle, StuffData round-trip par type.

**Architecture** : manifeste interleaving exact et complet (méthode non listée = échec ; catégorie B : aucun `await` avant complétion) ; RoomGrain non-`[Reentrant]` ; no-lock/no-Task.Run dans Rooms ; knobs câblés ; partials sans logics concrètes ; singletons single-silo enregistrés ; schéma STATE.yaml.

**Reference data** : lecteurs ne voient que version N ou N+1. **Specs** : origines Daybreak ≠ Arcturus + SHA ; bootstrap reproductible. **Benchmarks versionnés** : room vide/chargée, event storm, no-trigger O(1), hydratation massive (OQ-7) — baseline en PR-G1, comparaison en PR-Z1, régression significative refusée sans justification.

Tout test protocolaire nouveau passe par `Specs analyze` d'abord ; toute règle `BEST_EFFORT` visible dans le code obtient capture ou test + note de risque.

---

# 11. Observabilité

Reprise §6.15 en exigences : **Wired** — compteurs (received/ignored/dropped/processed, index size/rebuilds, chains stoppées par raison, delayed annulés à la revalidation), `StopReason` normalisé, `ExecutionId/ParentExecutionId` sur execute-stacks et signaux, p95/p99 du step par room opt-in, trace room-scoped opt-in donnant la chronologie logique (box id, StackIdentity, event type, sélections par source, résultats conditions+policy, effets choisis/sautés avec ordre/deadline/revalidation). **Commerce** — par opération : OperationId, flow, player/offer, phase, pivot timestamp, step courant, attempts, last error, age, recovery/compensation state ; alertes : stuck post-pivot, receipt conflict, refund failure, relay backlog/age/retries/dead-letter, `NeedsIntervention`. **Référence** — version des sets publiés. Les identifiants (box id, stack/tile id, trigger/action type, OperationId) figurent dans chaque signal ; le coût à l'arrêt reste « one boolean and nothing else » (norme du tick existant).

---

# 12. Workflow IA anti-dérive V4

Principe inchangé depuis V2 : **orchestrer l'existant** (hooks, gates, reviewers, skills, Specs CLI), n'ajouter que la mémoire et la décision. Apports V3 conservés (invalidation `watched_paths`), apports V4 : manifeste interleaving, extension commerce du slice, reviewer economy.

## 12.1 Mémoire persistante

```
docs/architecture-v4/
├── README.md                      (comment reprendre)
├── STATE.yaml                     (source de vérité, schéma validé par QualityGate)
├── ARCHITECTURE-V4.md             (ce document)
├── interleaving-manifest.yaml     (§12.3)
├── decisions/                     (ADR-000 = §8 ; un ADR par décision ensuite)
├── plans/                         (une slice active = un contrat)
└── reviews/                       (sorties reviewers par slice/epic)
```

```yaml
workflow_version: 4
baseline: { repository: absolutezeroo/vortex-cloud, commit: afc485be58ffd983b8d96430efe8aed620ad0ade }
references_verified: 2026-08-25
phase: p0            # p0 | commerce | wired | fulfillment | shorts | closing
active_slices: []
accepted_decisions: [ADR-000]
open_questions: [OQ-1, OQ-2, OQ-3, OQ-4, OQ-5, OQ-6, OQ-7, OQ-8]
audits:
  commerce:
    vortex_sha: afc485be58ffd983b8d96430efe8aed620ad0ade
    watched_paths: [Vortex.Catalog/**, Vortex.Marketplace/**, Vortex.Inventory/**,
                    Vortex.Players/Grains/PlayerWalletGrain.cs, Vortex.Players/Grains/PlayerEffectGrain.cs,
                    Vortex.Primitives/Players/Wallet/**, Vortex.Events/**]
    status: valid
  wired:
    vortex_sha: afc485be58ffd983b8d96430efe8aed620ad0ade
    watched_paths: [Vortex.Rooms/**, Vortex.Primitives/Rooms/**]
    status: valid
  furniture:
    vortex_sha: afc485be58ffd983b8d96430efe8aed620ad0ade
    watched_paths: [Vortex.Rooms/Providers/**, Vortex.Rooms/Object/**, Vortex.Furniture/**]
    status: valid
forbidden:
  - implementation_before_architecture_gate
  - split_room_into_item_or_wired_grains
  - manual_locks_inside_grains
  - reference_emulator_as_protocol_authority
  - shadow_engine_without_adr
  - behavior_change_inside_structural_slice
  - value_moving_slice_without_commerce_contract
```

Au SYNC : `git diff <audit_sha>..HEAD -- <watched_paths>` ; aucun fichier touché → audit `valid` ; touché → `stale` → revalidation **ciblée** (jamais une réanalyse complète pour un changement frontend).

## 12.2 ADRs et contrat de slice

Format ADR inchangé (Status/Context/Decision/Rejected alternatives/Consequences/Evidence, fichier+SHA) ; ADR-000 encode le registre §8 ; contredire un ADR accepté exige d'annoncer le conflit et une preuve nouvelle. Contrat de slice inchangé (Goal, Preconditions, Allowed/Forbidden files, Invariants, `behavior_change`, Semantic risks, Required tests, Abort/rollback, Done) + **extension commerce obligatoire** pour toute slice qui déplace de la valeur : OperationId, Pivot, CompensableBeforePivot, RetryableAfterPivot, IdempotencyKey/StepKey, RecoveryOwner, CriticalOutboxEvents, crash points testés. Un finding hors scope = `CROSS-DOMAIN FINDING` consigné dans STATE.yaml, jamais corrigé en douce.

## 12.3 Manifeste interleaving (D-V4-5)

`interleaving-manifest.yaml` : une entrée par méthode `[AlwaysInterleave]/[Reentrant]/MayInterleave` du dépôt — symbole, catégorie (`ImmutableRead` | `SynchronousBoundedLivenessOperation`), justification, propriétaire. Test d'architecture : (a) toute méthode portant l'attribut est listée ; (b) toute entrée listée porte l'attribut ; (c) catégorie B : le corps ne contient aucun `await` avant la complétion de la mutation (analyse du corps ; le pattern attendu est `mutation synchrone → LogAndForget(...) → Task.CompletedTask`, modèle `SendComposerAsync` [V-PRES02]). Entrées initiales : les deux surcharges `SendComposerAsync` (B), les `GetSnapshot` interleavés (A). Hors A/B ⇒ ADR avant merge.

## 12.4 Phases

| Phase | Action | Gate |
|---|---|---|
| SYNC | STATE + ADRs ; diff `audit_sha→HEAD` sur watched_paths ; stale ciblé | Baseline cohérent |
| PLAN | Contrat de slice (extension commerce si valeur) ; `Specs analyze` si Habbo ; invariants + fault points identifiés | Architecture gate |
| IMPLEMENT | Diff minimal ; hooks actifs ; aucun hors-scope | FastCheck |
| REVIEW | Reviewer selon chemins : `grain-rules-reviewer` (grains), `wire-truth-auditor` (wire), **`commerce-consistency-reviewer`** (nouveau : Catalog/Marketplace/Wallet/Inventory — vérifie pivot déclaré, receipts, token shutdown, pas de refund post-pivot) ; résultats persistés | QualityGate + STATE update |

---

# 13. Critères d'acceptation finaux

1. `RoomGrain` unique owner, non-réentrant, zéro synchronisation manuelle — gelé par tests d'architecture.
2. Toute méthode interleavée figure au manifeste avec catégorie vérifiée mécaniquement ; hors A/B ⇒ ADR (D-V4-5).
3. Tout budget Wired configurable a un lecteur runtime et un test ; RFW-101 clos, profondeur unique.
4. Le pipeline Wired complet s'exécute en test sur `IWiredRoomHost` fake + fake clock + RNG injecté, sans RoomGrain.
5. Live stack, branches, re-drain zero-delay, delays/revalidation, execute-stacks, cycle/depth sémantiquement identiques hors ADR ; matrice de parité verte, chaque règle taguée par niveau d'évidence.
6. Ajouter un behavior Furniture = classe + attribut + tests ; une collision de clé ne peut plus écraser en silence ; l'unload d'un plugin restaure l'état antérieur du registre.
7. Aucun `RoomItemFactory` redondant ; la chaîne Provider→ObjectModule→FurniModule est documentée.
8. Tout achat Catalog/Gift/Targeted/Marketplace porte un `OperationId`, un pivot explicite, une progression durable et une recovery ; **aucun crash point testé ne produit biens+refund, débit sans reprise, item évaporé, ou perte de valeur silencieuse**.
9. Le grant local d'une offre est un **seul commit** dans le turn d'`InventoryGrain` (D-V4-1) ; le résidu post-pivot est journalisé, idempotent, sous token de shutdown.
10. Wallet : debit/refund/credit rejoués avec le même `OperationId/StepKey` ne s'appliquent qu'une fois et renvoient le résultat antérieur.
11. Les événements métier critiques post-pivot partent depuis les transitions terminales du journal ; rejoués, ils ne modifient pas deux fois la progression (quêtes, daily tasks).
12. `GrantCatalogOfferAsync` n'est plus un dispatch monolithique ; les stratégies produisent un `FulfillmentPlan` testable, entrée validée de l'opération.
13. Reference data : ById/ByName publiés comme une seule version atomique ; version exportée.
14. `Vortex.Specs` distingue chaque référence par `repo@sha` ; aucun consensus promu en autorité.
15. Une nouvelle session reprend depuis STATE/ADRs ; seuls les audits aux `watched_paths` modifiés sont revalidés.
16. FastCheck/QualityGate, caractérisations et benchmarks de clôture verts, sans régression CPU/alloc significative non justifiée.

---

# CHANGES FROM V3

| Point V3 | Verdict | Décision V4 | Pourquoi |
|---|---|---|---|
| Finding commerce (fenêtres Catalog/Targeted/Marketplace/Wallet) | **KEEP — VERIFIED+** | Conservé intégralement, renforcé de preuves ligne (SaveChanges 333, commentaire « auto-refunds » 428, Pets 53/Bots 77, claim 189–194, receipts absents du contrat wallet) | Tableau §2.2 A1–A6 ; le code documente lui-même l'invariant périmé |
| Protocole minimal (OperationId, pivot, journal, receipts, recovery) | KEEP + règle préalable | **D-V4-1 : réduire la surface post-pivot avant d'instrumenter** — batch local mono-commit d'abord | Furnis/badges/pets/bots = même factory, même grain (B1) ; instrumenter une topologie artificielle serait du gaspillage |
| Journal d'opération + outbox sélective (deux mécanismes) | MODIFY (fusion) | **D-V4-2 : transitions terminales du journal = source du relay** ; pas de table outbox indépendante tant qu'aucun événement critique sans opération n'existe | Tous les événements critiques identifiés sont adossés à une opération (A13) [M-AZ-03][M-AZ-04] |
| « Wallet reçoit OperationId/StepKey » (prescription) | MODIFY (chemin) | **D-V4-3 : receipt dans la transaction que `TryDebitAsync` ouvre déjà ; overloads additifs ; retrait du legacy en fin** | A2 : la transaction existe ; l'évolution additive évite la casse |
| Marketplace Buy = fenêtre parmi d'autres | REFINED | **D-V4-4 : le claim `ExecuteUpdate` est un pivot correct à conserver ; le fix est la complétion durable post-claim** | A6 : le claim empêche déjà la double vente ; créditer ce qui est bon |
| Catégories interleave A/B (prose) | KEEP + mécanisation | **D-V4-5 : manifeste testé ; catégorie B vérifiée « aucun await avant complétion »** | A7 : la propriété de sûreté est mécanisable |
| Principe token/annulation post-pivot | KEEP + preuve | **D-V4-6** : le `ct` de la requête traverse aujourd'hui le grant, et le commentaire wallet identifie l'annulation comme cause n°1 d'échec | B6 [V-ECO01] |
| Targeted : « count cohérent » (exigence sans forme) | MODIFY | **D-V4-7 : compteur = étape journalisée ou fusionnée au pivot** | B7 ; pas de compteur « spécial » |
| Étapes post-pivot « idempotentes » (supposé) | KEEP + preuve requise | **D-V4-8 : idempotence prouvée par test de replay** — contre-exemple `AddEffectAsync` (insert inconditionnel) | A15 [V-EFF01] — nouvelle preuve V4 |
| Collision registry : FAIL + override explicite | KEEP | **D-V4-9** inchangé sur le fond, test hot-unload-restaure ajouté | A9 vérifié au code |
| Source outbox [M-AZ-03] = sample Cosmos | REFINED (sourcing) | Guide Architecture Center [M-AZ-04] promu référence canonique, sample en illustration | B8 |
| Abandon de la grille 17×11 | RESTAURÉ | **D-V4-10 : grille rétablie (§6), domaines stables denses** | Format canonique de la mission ; la V3 restait auditable mais moins comparable |
| `CatalogPurchasedEvent` → quêtes | KEEP + extension | Consommateur supplémentaire découvert : daily tasks [V-PROG02] — la dédup couvre les deux | A13 VERIFIED+ |
| Tout le reste (runtime, Rooms, Wired host/plan/shadow, Furniture, Presence DEFER, Specs, Reference Data, workflow watched_paths) | KEEP | Conservé tel quel, re-vérifié | §2.2 — aucun claim structurant réfuté |

---

# OPEN QUESTIONS / NEEDS EVIDENCE

| ID | Question | État / décision provisoire |
|---|---|---|
| OQ-1 | Profondeur de chaîne Wired : préserver 8 (const effective) ou activer 20 (config) ? | Instrumenter `depth-stop` (PR-G2) ; décision produit avant câblage final ; comportement Habbo officiel `UNKNOWN`. |
| OQ-2 | Pivot Catalog exact : débit validé après préflight complet (recommandation V4 — minimise le résidu grâce à D-V4-1) ou pivot plus tardif avec compensations complètes ? | Tranché dans la slice de conception PR-C2 ; les invariants (pivot explicite, testable) sont imposés, pas le point. |
| OQ-3 | Receipts : table commune minimale ou tables par owner ? | Primitive DB minimale réutilisable **si** insérable dans la même transaction que la mutation ; jamais de service central. |
| OQ-4 | Périmètre exact du relay critique : quels subscribers de `CatalogPurchasedEvent`/Targeted/Marketplace sont métier-critiques ? | Quêtes + daily tasks + audit requis d'abord [V-PROG01][V-PROG02] ; métriques best-effort séparables ; lister en PR-C6. |
| OQ-5 | Ordre d'émission des composers `LogAndForget` vis-à-vis des turns suivants | Probablement bénin (continuations même scheduler) ; à documenter ou tester avant de s'y appuyer. |
| OQ-6 | Sémantique officielle des allowances/ordres Wired modernes (fenêtre glissante vs fixe, etc.) | `UNKNOWN` sans capture/client ; choix Vortex documentés ; community evidence [H-COM-*] pour orienter les scénarios seulement. |
| OQ-7 | Coût `ActivatorUtilities` à l'hydratation de très grosses rooms | Benchmark PR-G1 avant toute optimisation du registre. |
| OQ-8 | Idempotence des variables Wired permanentes cross-room (écritures concurrentes multi-rooms) | Hors P0 commerce ; à instruire avant toute API publique de variables. |

---

# FINAL RECOMMENDED REFACTOR ROADMAP

| Prio | Chantier | Taille | Dépendances | Risque | PRs |
|---|---|---|---|---|---|
| P0-A | Commerce Consistency (caractérisation → identité/journal → receipts wallet → Catalog/Gift/Targeted avec consolidation D-V4-1 → Marketplace → relay critique) | XL | Tests fault-injection d'abord | Très élevé métier | C1–C6 |
| P0-B | Wired config correctness (RFW-101) + compteurs minimaux | S | — | Faible | G2 |
| P0-C | Garde-fous : workflow files, manifeste interleaving, guards, benchmark baseline, inventaire golden | M | — | Faible | G1 |
| P1 | Extraction Wired derrière `IWiredRoomHost` | XL | G1/G2 ; parallélisable avec P0-A | Élevé contenu | W1–W4 |
| P1 | Fulfillment planner + stratégies | M/L | C4 stabilisé | Moyen | S1–S2 |
| P1/P2 | Durcissement registre Furniture | S/M | — | Faible | F1 |
| P2 | `FurnitureDefinitionSet` atomique ; Specs `repo@sha` ; baselines protocole ; doc fenêtre de perte | S/M | — | Faible | Q1–Q2 |
| Clôture | Benchmarks finaux, docs, revue ADR, matrice d'acceptation | S | Tout | Faible | Z1 |
| DEFER | PlayerPresence modules ; multi-silo ; Economy Kernel générique | — | Seuils/déclencheurs §6.5, §6.1, §8.1 | — | ADR requis |
| CONTINGENCY | Shadow Wired plan-only | M | Réécriture sémantique future par ADR | — | — |

Dashboard/API/Security : chantier séparé, jamais mélangé (règle V1→V3 conservée).

---

# Annexe A. Dépôts, branches et révisions vérifiées (2026-08-25)

| Projet | Repository | Branche | Commit figé | Vérification |
|---|---|---|---|---|
| Vortex Cloud | absolutezeroo/vortex-cloud | main | `afc485be58ffd983b8d96430efe8aed620ad0ade` | `git fetch` + `rev-parse` : HEAD = origin = baseline |
| Skylight3 | aromaa/Skylight3 | master | `14a9531ba4368140d26435ae5b0819fd29d13592` | idem |
| Arceus Emulator | lmrick/arceus-emulator | main | `97db905ad985c738ee0555c87dd97523ddfe757d` | idem |
| Arceus Wired Plugin | lmrick/arceus-emulator-wireds-plugin | main | `c2e649c9fe281c4feef75138ccbc176bbea8f517` | idem |
| Habbo-Daybreak | habbo-cc/Habbo-Daybreak | develop | `2dee52a0ecd131828cf86c3b0e9e736d93f434eb` | idem |

Toute session future compare son HEAD au baseline et ne revalide que les audits dont les `watched_paths` ont changé (§12.1).

# Annexe B. Index des sources code (niveau ligne, vérifié le 2026-08-25)

Base Vortex : `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/` (abrégée `V/`). Les lignes citées sont celles vérifiées en §2.2.

## B.1 Vortex — commerce
- **[V-ECO01]** `V/Vortex.Primitives/Players/Wallet/WalletPurchaseExtensions.cs` — debit→grant→refund ; compensation sur `CancellationToken.None` (l'annulation = cause n°1 d'échec du grant, commentaire) ; `LogCritical` si le refund échoue.
- **[V-ECO02]** `V/Vortex.Players/Grains/PlayerWalletGrain.cs` — `TryDebitAsync` 55–130 : DbContext neuf par tentative + `IExecutionStrategy` + `BeginTransaction` + commit ; le débit est durable et retry-safe par construction.
- **[V-ECO03]** `V/Vortex.Primitives/Players/Grains/IPlayerWalletGrain.cs` — contrat sans identité d'opération (`TryDebitAsync(List<…>)` l.11, `CreditBackAsync(List<…>)` l.15, `GrantCreditsAsync(int)` l.18).
- **[V-ECO04]** `V/Vortex.Rooms/Grains/Systems/WiredTrading/WiredTradeSettlement.cs` — pivot documenté, refus motivé de la primitive commune, tests d'ordre nommés (canon de style).
- **[V-CAT01]** `V/Vortex.Catalog/Grains/CatalogPurchaseGrain.cs` — `ExecutePurchaseAsync` l.86, `PublishAsync(CatalogPurchasedEvent)` l.122 (après succès, hors transaction).
- **[V-CAT02]** `V/Vortex.Inventory/Grains/InventoryGrain.Furni.cs` — `GrantCatalogOfferAsync` : parsing 152–296 ; `AddRange` ~300 ; badges (garde `alreadyOwned`) 306–331 ; **`SaveChangesAsync` 333** ; sync cache/client/events 342–379 ; pets 395 ; bots 409 ; effets 435 ; commentaire « auto-refunds » l.428.
- **[V-CAT03]** `V/Vortex.Database.Tests/Catalog/CatalogPurchaseTests.cs` — `AGrantThatFails_RefundsTheBuyer` l.226 (pré-pivot uniquement).
- **[V-CAT04]** `V/Vortex.Database.Tests/Catalog/CatalogPurchaseHarness.cs` — fake `IInventoryGrain` binaire 174–190 (`GrantThrows ? throw : Task.CompletedTask`).
- **[V-CAT05]** `V/Vortex.Catalog/Grains/CatalogPurchaseGrain.Gift.cs` — achat l.66 puis wrapping en present (l.124, fallback plain) ; recherche d'offre cross-catalogue commentée l.186.
- **[V-CAT06]** `V/Vortex.Catalog/Grains/PlayerTargetedOfferGrain.cs` — boucle de grants unitaires 85–111 dans le scope compensé ; `IncrementPurchaseCountAsync` l.124 après succès ; event analytics « Non-transactional ».
- **[V-INV01]** `V/Vortex.Inventory/Grains/InventoryGrain.Pets.cs` — `CreatePetAsync` : commit propre l.53.
- **[V-INV02]** `V/Vortex.Inventory/Grains/InventoryGrain.Bots.cs` — `CreateBotAsync` : commit propre l.77.
- **[V-INV03]** `V/Vortex.Inventory/Grains/InventoryGrain.Furni.cs` — `GrantFurnitureDefinitionAsync` 603–635 : Add + commit **par unité** ; StuffData reconstruit du blob (bug « blank legacy default » documenté).
- **[V-MKT01]** `V/Vortex.Marketplace/Grains/MarketplacePurchaseGrain.cs` — MakeOffer : `RemoveFurnitureAsync` ~69 → insert+commit ~99 ; Cancel : `Cancelled`+commit 127–128 → grant 132 ; Buy : claim `ExecuteUpdate` Active→Sold+CreditsOwed 189–194 → grant 211 → revert best-effort 221–233 (« Sold with no item delivered », `CancellationToken.None`) ; Redeem : `CreditsOwed=0`+commit 302–305 → `GrantCreditsAsync` 308–310.
- **[V-EFF01]** `V/Vortex.Players/Grains/PlayerEffectGrain.cs` — `AddEffectAsync` 71–91 : insert inconditionnel (`PlayerEffects.Add`) — non idempotent au retry.
- **[V-EVT01]** `V/Vortex.Events/EventSystem.cs` — bus in-process. **[V-EVT02]** `V/Vortex.Events/Registry/EventRegistry.cs` — `HandlerMode = Parallel` l.42, isolation d'erreurs.
- **[V-PROG01]** `V/Vortex.Progression/Quests/Events/QuestProgressEventHandlers.cs` — `IEventHandler<CatalogPurchasedEvent>` l.86. **[V-PROG02]** `V/Vortex.Progression/Quests/Events/DailyTaskProgressEventHandlers.cs` — idem l.110 (consommateur supplémentaire, découverte V4).

## B.2 Vortex — runtime, rooms, wired, furniture
- **[V-RUN01]** `V/Vortex.Rooms/Grains/RoomGrain.cs` — tick `RegisterGrainTimer`, étapes isolées `RunTickStepAsync`, deadlines `AdvanceBoundaryPast`, flush désactivation best-effort.
- **[V-WIR01]** `V/Vortex.Rooms/Grains/Systems/RoomWiredSystem.cs` (+ `RoomWiredSystem.Variables.cs`) — pipeline complet §6.4 ; `MaxCallChainDepth = 8` (const) ; log channel + compteurs d'erreurs.
- **[V-WIR02]** `V/Vortex.Rooms/Configuration/RoomConfig.cs` — `WiredMaxDepth = 20` **non lu** (RFW-101) ; budgets 64/64, queue 512, tick 50 ms, flush caps.
- **[V-WIR03]** `V/Vortex.Rooms/Wired/WiredPendingStackExecution.cs` — init-only 8–19 + `Version/DueAtMs/NextActionIndex/WaitingActionIndex/EffectsStarted` en `get; set;` 21–28.
- **[V-WIR04]** `V/Vortex.Rooms/Wired/WiredExecutionContext.cs` — collecte des side-effects, flush groupé. **[V-WIR05]** `V/Vortex.Primitives/Rooms/Wired/IWiredExecutionContext.cs` — capabilities 32–88 (states, mouvements, composer, bots, hand items).
- **[V-FUR01]** `V/Vortex.Rooms/Providers/RoomObjectLogicProvider.cs` — `RegisterLogic` : `_logics[key] = reg` (écrasement) 51–68 ; dispose `TryRemove(KeyValuePair(key, reg))` sans restauration ; fallback commenté « the warning is the to-do list ».
- **[V-FUR02]** `V/Vortex.Rooms/Object/Logic/RoomObjectLogicFeatureProcessor.cs` — scan `[RoomObjectLogic]`, factories `ActivatorUtilities`, ServiceProvider plugin composé.
- **[V-FUR03]** `V/scripts/hooks/check-logic-groups.mjs` — menu Dashboard dérivé des attributs (garde anti-dérive générée).
- **[V-FUR04]** `V/Vortex.Rooms/Providers/RoomItemsProvider.cs` — matérialisation items depuis rows/snapshots. **[V-FUR05]** `V/Vortex.Rooms/Grains/Modules/RoomObjectModule.cs` — attach état+logic+index+map. **[V-FUR06]** `V/Vortex.Rooms/Grains/Modules/RoomFurniModule.Floor.cs` — place/move/use/edit lifecycle.
- **[V-FUR07]** `V/Vortex.Rooms/Object/Logic/Furniture/FurnitureLogic.cs` — lifecycle, `PersistStuffDataAsync`/`MarkDirty` centralisé, invariant historique commenté.

## B.3 Vortex — players, référence, specs, infra
- **[V-PRES01]** `V/Vortex.Primitives/Players/Grains/IPlayerPresenceGrain.cs` — 15–26 : rationale deadlock + « only enqueue … synchronously — no awaits » + `[AlwaysInterleave]`. **[V-PRES02]** `V/Vortex.Players/Grains/PlayerPresenceGrain.cs` — 112–137 : `EnqueueOutgoing` + `LogAndForget(ProcessOutgoingQueueAsync())` + `Task.CompletedTask`.
- **[V-REF01]** `V/Vortex.Furniture/Providers/FurnitureDefinitionProvider.cs` — deux index publiés séparément (fenêtre de version mixte). **[V-REF02]** `V/Vortex.Catalog/Providers/CatalogSnapshotProvider.cs` — build complet → `Volatile.Write` (pattern cible).
- **[V-SPEC01]** `V/Vortex.Specs/Sources/SpecWorkspace.cs` — détection des arbres, origine `arcturus` fusionnante (chantier `repo@sha`). **[V-SPEC02]** `V/Vortex.Specs/Analysis/Reference/ArcturusReferenceAnalyzer.cs` — analyzer de référence.
- **[V-MULTI01]** `V/Vortex.Main/Configuration/OrleansHostConfig.cs` — thèse single-silo motivée (providers `ReloadAsync`, agrégateurs, memory streams).
- **[V-PROTO01]** `V/tree/Vortex.Protocol` — 1 091 fichiers messages.
- **[V-AI01]** `V/AGENTS.md` — contrat canonique (stack épinglée, skills, règles Orleans, checklists). **[V-AI02]** `V/CLAUDE.md` — ordre de contexte, automations. **[V-AI03]** `V/scripts/hooks/` — post-edit, guard-emulator, check-header-registry (14 baselinés), check-wire-conflicts (23 baselinés), auto-test des hooks. **[V-AI04]** `V/.claude/agents/` — `grain-rules-reviewer`, `wire-truth-auditor`.
- **[V-AUD01]** `V/docs/audits/2026-07-02-full-technical-audit.md` — audit daté (source d'hypothèses, jamais du HEAD).

## B.4 Références (SHA figés, cf. Annexe A)
- **[S-RUN01]** `src/Skylight.Server/Game/Rooms/Room.cs` ; **[S-RUN02]** `.../Rooms/Scheduler/RoomTaskScheduler.cs` — thread dédié + SpinLocks.
- **[S-CAT01]** `src/Skylight.API/Game/Catalog/Products/ICatalogProduct.cs` ; **[S-CAT02]** `.../Products/CatalogProductBadge.cs` ; **[S-CAT03]** `.../Catalog/CatalogTransaction.cs` ; **[S-CAT04]** `.../CatalogTransaction.Context.cs` — modèle contributif + transaction EF locale.
- **[A-FUR01]** `emulator/src/main/java/habbo/rooms/components/objects/items/RoomItemFactory.java` — Map<String,Class> + cache Constructor.
- **[AP-WIR01]** `README.md` ; **[AP-WIR02]** `emulator.wireds/src/main/java/org/emulator/wireds/RoomInjector.java` ; **[AP-WIR03]** `.../component/WiredManager.java` ; **[AP-WIR04]** `.../component/WiredExecutionPipeline.java` — phases nommées, ConcurrentHashMap/Future/IThreadManager.
- **[D-WIR01]** `src/main/java/com/eu/habbo/habbohotel/wired/core/WiredServices.java` ; **[D-WIR02]** `.../WiredManager.java` (flags parallèle/exclusif) ; **[D-WIR03]** `.../WiredEngine.java` (depth 10, rate-limit) ; **[D-WIR04]** `.../RoomWiredStackIndex.java` (cache invalidable).
- **[D-FUR01]** `.../habbohotel/items/ItemManager.java` — registre manuel géant. **[D-PLUG01]** `.../plugin/PluginManager.java` — statics globaux.

# Annexe C. Références externes (consultées le 2026-08-25)

- **[M-ORL-01]** Microsoft Learn — *Orleans request scheduling / reentrancy* — https://learn.microsoft.com/en-us/dotnet/orleans/grains/request-scheduling — turns mono-thread, interleaving opt-in.
- **[M-ORL-02]** Microsoft Learn — *Timers and reminders* — https://learn.microsoft.com/en-us/dotnet/orleans/grains/timers-and-reminders — période mesurée à la résolution du callback ; `RegisterGrainTimer` non-interleaving par défaut ; `RegisterTimer` obsolète.
- **[M-ORL-03]** Microsoft Learn — *Messaging delivery guarantees* — https://learn.microsoft.com/en-us/dotnet/orleans/implementation/messaging-delivery-guarantees — at-most-once par défaut ; retries ⇒ livraisons multiples possibles ; aucune déduplication durable.
- **[M-ORL-04]** Microsoft Learn — *Orleans transactions* — https://learn.microsoft.com/en-us/dotnet/orleans/grains/transactions — état transactionnel Orleans (hors périmètre du problème commerce EF/MySQL + side-effects).
- **[M-AZ-01]** Azure Architecture Center — *Saga pattern* — https://learn.microsoft.com/en-us/azure/architecture/patterns/saga — compensable / **pivot** / retryable.
- **[M-AZ-02]** Azure Architecture Center — *Compensating Transaction pattern* — https://learn.microsoft.com/en-us/azure/architecture/patterns/compensating-transaction — enregistrer la progression ; étapes de compensation idempotentes et reprenables.
- **[M-AZ-03]** Microsoft Learn — *Transactional Outbox (sample Cosmos DB)* — https://learn.microsoft.com/en-us/samples/azure-samples/cosmos-db-design-patterns/transactional-outbox/ — illustration du dual-write état+événement.
- **[M-AZ-04]** Azure Architecture Center — *Transactional Outbox pattern (guide)* — https://learn.microsoft.com/en-us/azure/architecture/databases/guide/transactional-outbox-cosmos — référence canonique : atomicité état+outbox, relay at-least-once, consommateurs idempotents.
- **[M-AZ-05]** Azure Architecture Center — *Minimize coordination* — https://learn.microsoft.com/en-us/azure/architecture/guide/design-principles/minimize-coordination — idempotence et réduction de coordination.
- **[H-OFF-01]** Habbo Customer Support — *What is Wired Furni?* — https://help.habbo.com/hc/en-us/articles/360011620099-What-is-Wired-Furni. **[H-OFF-02]** — *What is Furni?* — https://help.habbo.com/hc/en-us/articles/360011512940-What-is-Furni.
- **[H-OFF-03]** Habbo.com — *Wired Variables are here!* — https://www.habbo.com/community/article/34067/wired-variables-are-here-2. **[H-OFF-04]** — *Wired 2.0 Dev Update* — https://www.habbo.com/community/article/35029/wired-2-0-dev-update (officiels, sémantique publique Variables/Wired 2.0).
- **[H-COM-01]** HabbGames — *HelpWired : Présentation du Batch 12* — https://habbgames.fr/news/6864-help-wired-presentation-du-batch-12. **[H-COM-02]** HabboWIRED — *Effet WIRED : Exécuter piles Wired* — https://habbowired.fr/Effet%20WIRED%20%3A%20Ex%C3%A9cuter%20piles%20Wired. **[H-COM-03]** HabboWIRED — *Présentation des Sélecteurs* — https://habbowired.fr/Pr%C3%A9sentation%20des%20S%C3%A9lecteurs. **[H-COM-04]** HabboWIRED — *WIRED Variable : Variable de contexte* — https://habbowired.fr/WIRED%20Variable%20%3A%20Variable%20de%20contexte. Communautaires : orientent les scénarios de test, jamais promus en autorité.

---

| **Conclusion consolidée** — La V3 a survécu à l'audit contradictoire : aucun de ses claims structurants n'est réfuté, et son finding commerce est plus profond que son propre texte — le code documente lui-même l'invariant refund conçu pour un grant atomique qu'il n'est plus (l.428). La V4 n'est donc pas un renversement mais un serrage : réduire la surface post-pivot avant de l'instrumenter (un commit local là où il y en a quatre), fusionner journal et outbox en un seul pipeline durable, faire entrer les receipts wallet dans la transaction qui existe déjà, conserver le claim Marketplace comme le pivot correct qu'il est, mécaniser le manifeste interleaving, et rétablir la grille documentaire qui rend chaque révision comparable à la précédente. Le runtime Room, lui, ne change pas : on extrait le moteur Wired derrière des capabilities, on durcit un registre qui existe, et on ne touche à rien de ce que le code fait déjà mieux que les trois références réunies. |
|---|

*Vortex Cloud — Architecture cible & workflow IA — V4 — Deep Audit Rewrite — 25 août 2026. Remplace les V1, V2 et V3 pour toute planification d'implémentation.*
