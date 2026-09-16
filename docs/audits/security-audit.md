# Audit de sécurité — rooms, catalogue, surface d'autorisation

- **Révision auditée** : `c07ca65` sur `claude/vortex-cloud-beta-audit-apl5gn` (`main` = `0455032`).
- **Date** : 2026-09-15.
- **Question posée** : « les room, le catalogue, la sécurité — je veux un vrai audit complet sur lequel m'appuyer, car à chaque fois j'ai des surprises niveau sécurité. »
- **Nature** : audit en lecture seule, plus un contrôle mécanique livré avec le rapport (`scripts/hooks/check-authorization-surface.mjs`). Aucun code de production modifié.

---

## 1. La réponse en une page

**La bonne nouvelle d'abord, parce qu'elle change ce qu'il faut faire.** J'ai suivi à la main chacun des chemins où une faille coûterait cher — achat au catalogue, rachat de crédits, ouverture de cadeau, échange, manipulation de mobi, réglages de room, entrée en room, animaux, endpoints HTTP de compte. **Tous sont correctement protégés**, et plusieurs le sont avec une finesse qu'on ne voit pas souvent : le mobi-crédit se consomme *avant* de payer pour qu'un clic répété ne paie pas deux fois, le cadeau est protégé par la *propriété* et non par les droits de room (« sinon quiconque a les droits ouvre tous les cadeaux déposés chez lui »), et le débordement de `prix × quantité` a déjà été trouvé et corrigé.

Votre dépôt n'a donc pas un problème de *niveau* de sécurité. Il a un problème de **vérifiabilité** de sa sécurité, et c'est exactement ce qui produit des surprises.

> **La même question — « cet acteur a-t-il le droit ? » — reçoit dans ce dépôt 15 réponses différentes, combinées de 27 façons, sur 179 points de décision. Aucune n'est nommée pareil. Deux d'entre elles sont, en lecture de diff, indiscernables d'une méthode qui ne vérifie rien.**

Le cas qui résume tout :

```csharp
// Vortex.Rooms/Grains/RoomGrain.Furni.Interactive.cs
IRoomItem? item = await FindManipulableItemAsync(ctx, itemId);   // demande au SecurityModule
_state.ItemsById.TryGetValue(itemId, out IRoomItem? item);       // ne demande rien
```

Un identifiant d'écart. Le premier appelle `SecurityModule.CanManipulateFurniAsync` et rend `null` si l'acteur n'a pas les droits ; le second rend l'objet à tout le monde. Sur une revue de diff, **les deux lignes se ressemblent**. Onze méthodes du dépôt dépendent aujourd'hui de la première. Le jour où l'une est écrite avec la seconde, rien — ni le compilateur, ni les tests, ni la CI, ni l'œil — ne le dit.

C'est le même motif que l'audit wired, transposé à l'autorisation : *une règle qui existe, qui est respectée en pratique, et que rien ne vérifie*. La différence est qu'ici la règle est écrite noir sur blanc dans le code :

> « A handler is not a security boundary: the method is a member of a public grain interface, callable by anything in the cluster that can name the room (ROOMG-GATE-038). **The grain is the boundary.** »
> — `Vortex.Rooms/Grains/Modules/RoomSecurityModule.cs:265`

Cette règle est appliquée, et **testée pour exactement deux méthodes** (`StaffPowerGrainGateTests`). Les 119 autres méthodes de grain qui prennent un acteur reposent sur le fait que chaque auteur y a pensé.

**Ce que je livre** : `check-authorization-surface.mjs`, qui n'essaie pas de juger si une autorisation est correcte — c'est impossible avec 15 idiomes — mais qui **inventorie les 179 points de décision et la porte qui garde chacun**, et bloque quand cet inventaire bouge. Preuve : en remplaçant `FindManipulableItemAsync` par `TryGetValue` dans `SetCustomStackHeightAsync`, il sort `gated-lookup -> NONE` et `exit=2` (§6).

---

## 2. Méthode, et pourquoi les chiffres bruts mentent

J'ai commencé par la mesure évidente : combien de handlers de paquets vérifient `ctx.PlayerId` ? Réponse : **351 sur 559**. J'allais écrire « 208 handlers non gardés ».

C'était faux, et il faut le dire parce que c'est la leçon de méthode de cet audit. `AddItemToTradeMessageHandler` ne vérifie rien :

```csharp
await _roomService.AddTradeItemsAsync(ctx.AsActionContext(), [message.ItemId], ct);
```

…parce que `RoomService.AddTradeItemsAsync` (`RoomService.Trading.cs:44-58`) commence par `if (ctx.PlayerId <= 0 || ctx.RoomId <= 0) return;`. La garde est déléguée, et correctement. Sur les 209 handlers « non gardés » du premier comptage, la quasi-totalité délèguent à une couche qui garde.

**Chaque chiffre de ce rapport a donc été vérifié à la main, fichier contre fichier, avant d'être écrit.** Quand une affirmation n'a pas pu l'être, elle est marquée comme telle au §8. Les faux positifs successifs de mes propres détecteurs — `UpdateRoomSettingsAsync` (garde par `IsRoomOwnerAsync`), `PickUpPetAsync` (garde par `EnsurePetOwner`), `SetCustomStackHeightAsync` (garde par `FindManipulableItemAsync`), les endpoints du superviseur (garde par `AddEndpointFilter`) — sont eux-mêmes la preuve du défaut structurel : **si un détecteur écrit exprès pour les trouver s'y trompe quatre fois, une revue humaine s'y trompera aussi.**

---

## 3. Ce qui est vérifié sain

Un audit sur lequel s'appuyer doit dire ce qu'il a regardé **et trouvé correct**, sinon il ne sert qu'à inquiéter.

| Chemin | Protection constatée | Référence |
|---|---|---|
| Achat au catalogue | Prix lu côté serveur ; quantité bornée en bas (`Math.Max(1, …)`) **et en haut** (`DEFAULT_MAX_PURCHASE_SIZE`) ; `prix × quantité` en `long` | `CatalogPurchaseGrain.cs:53-62, 333-341` |
| Verrou de sécurité du compte | Re-vérifié côté serveur sur les 6 achats catalogue + 2 marketplace, avec le commentaire qui dit pourquoi | `SafetyLockGuard.cs` |
| Mobi-crédit | **Propriété** exigée, pas les droits de room ; l'objet est consommé *avant* que les crédits existent | `RoomGrain.Furni.Interactive.cs:431-466` |
| Cadeau | **Propriété** exigée ; commentaire explicite sur pourquoi pas les droits | idem `:468-484` |
| Manipulation de mobi | `SecurityModule.CanManipulateFurniAsync` via `FindManipulableItemAsync` | `RoomGrain.Furni.Interactive.cs:46-54` |
| Réglages, plan, catégorie, tags de room | `IsRoomOwnerAsync(actor)` en première ligne | `RoomGrain.Settings.cs:57`, `RoomGrain.FloorPlan.cs` |
| Animaux (ramasser, déplacer, nourrir…) | `EnsurePetOwner` lève `NoPermissionToManipulatePet` | `RoomPetSystem.cs:598-604` |
| Entrée en room | Bannissement, salle pleine, mot de passe, porte verrouillée → sonnette ; contourné seulement par `>= Rights`, ce qui est correct | `RoomService.cs:75, 108-140` |
| Échange | `PlayerId` **et** `RoomId` gardés au niveau service | `RoomService.Trading.cs` |
| Éditeur de mobi | Capacité `room.furni.edit` re-résolue à chaque requête ; le drapeau client ne décide que d'un bouton | `VortexApplyFurniEditMessageHandler.cs:40-47` (contrôle en `:45`) |
| WebApi `/api/user/**` | Les 26 routes authentifient, via `ctx.AccountId(sessions)` ou `SelectedPlayerAsync` ; `/api/ssotoken` aussi | `WebApiEndpoints.cs:800-805, 1837-1862` |
| Superviseur (`/start`, `/stop`, `/console`) | Filtre de jeton sur le groupe, + échange jeton→cookie `HttpOnly`/`SameSite=Strict` | `SupervisorEndpoints.cs:54` |
| Dashboard | `RequireAuthorization(capacité)` par endpoint | `DashboardEndpoints.cs` |
| Trame réseau | Longueur de corps déclarée bornée à 64 Ko, rejet explicite au-delà | `ClientPacketDecoder.cs:31-37` |
| Débit par session | Seau à jetons, 50 paquets/s soutenus, rafale 100, **avant** tout handler | `RateLimitConfig.cs`, `RateLimitBehavior` |

**Le `PlayerId` et le `RoomId` ne viennent jamais du client.** `MessageContext` les reçoit de `sessionGateway.GetPlayerId(SessionKey)` et du contexte ambiant résolu côté serveur (`MessageRegistry.cs:44-56`). C'est le point le plus important de toute cette liste, et il est bon.

---

## 4. Les défauts confirmés

### SEC-10 — Aucune limite de connexions, ni par IP ni globale (P1, confirmé)

**Ce qui existe** : une limite de débit **par session** (50 paquets/s, rafale 100).
**Ce qui n'existe pas** : quoi que ce soit qui borne le nombre de sessions. Recherche sur tout l'arbre de `MaxConnections`, `ConnectionLimit`, `MaxSessionsPerIp`, `PerIp` : **aucun résultat**. `appsettings.json` ne déclare sous `serverOptions` que les écouteurs (`ip`, `port`) — ni `maxConnectionNumber`, ni `backlog`.

**Conséquence** : la limite par session ne borne rien au niveau de l'hôte. 1 000 connexions depuis une machine = 50 000 paquets/s, chacun pouvant activer un grain et interroger la base. Le coût pour l'attaquant est une boucle `connect()`.

**Aggravant** : rien n'exige d'être authentifié pour envoyer des paquets (§5.2), donc ce débit est disponible **avant** tout login.

*Correction* : `maxConnectionNumber` dans les deux sections `serverOptions` (SuperSocket l'accepte nativement), plus un compteur de sessions par IP dans `SessionGateway.AddSessionAsync`, refusant au-delà d'un seuil configurable. *Critère* : la (N+1)ᵉ connexion d'une même IP est fermée immédiatement.

### SEC-11 — Le rachat de bon est un amplificateur non authentifié (P2, confirmé)

`Vortex.PacketHandlers/Catalog/RedeemVoucherMessageHandler.cs` en entier :

```csharp
string? code = message.Code;
if (string.IsNullOrWhiteSpace(code)) { return; }
IVoucherGrain voucher = grainFactory.GetVoucherGrain(code);
await voucher.RedeemAsync(ctx.PlayerId, ct);
```

Quatre choses, chacune vérifiée :

1. **Pas de garde `ctx.PlayerId <= 0`.** Une socket non authentifiée porte `PlayerId = -1` (`SessionGateway.cs:50-51`) et arrive ici.
2. **La clé du grain est la chaîne du client, telle quelle.** `GetVoucherGrain(code)` est un grain Orleans à clé chaîne : *une activation par code distinct essayé*. Le parseur fait `packet.PopString()` sans borne propre (`RedeemVoucherMessageParser.cs`) ; seule la taille de trame la limite, à 64 Ko.
3. **Une requête SQL par activation.** `VoucherGrain.OnActivateAsync` fait un `SELECT` sur `Vouchers` pour chaque code distinct.
4. **Aucun comptage de tentatives.** `TryRedeemAsync` vérifie bien le déjà-racheté, le plafond de rachats et l'existence du joueur — mais rien ne compte les *échecs*. Et sur un code inconnu, `RedeemAsync` active en plus `GetPlayerPresenceGrain(-1)` pour envoyer l'erreur.

**Conséquence** : force brute de codes sans verrouillage, à deux activations de grain et une requête SQL par essai, sans authentification. Composé avec SEC-10, c'est le vecteur le moins cher du dépôt.

*Correction* (par ordre d'effet) : refuser `ctx.PlayerId <= 0` ; borner la longueur et le jeu de caractères du code **avant** de nommer le grain (un code a un format connu) ; compter les échecs par joueur et par IP. *Critère* : un code de 200 caractères, ou le 11ᵉ échec en une minute, n'active aucun grain.

### SEC-12 — `ApplyFurniEditAsync` délègue l'autorisation à son appelant (structurel, confirmé, non exploitable aujourd'hui)

Deux règles opposées coexistent dans le même dépôt.

`RoomSecurityModule.cs:265` : « *A handler is not a security boundary… The grain is the boundary.* »
`RoomGrain.Furni.Edit.cs:28-38` (la phrase en `:36`) : « *the authorization question is answered once, **by the caller**, against `room.furni.edit`.* »

La seconde s'applique à la méthode la plus puissante de la room : réattribution de propriétaire, placement sur tuile bloquée, altitude libre, changement de définition. Son unique appelant actuel vérifie bien la capacité — **je l'ai lu** (`VortexApplyFurniEditMessageHandler.cs:45`). Ce n'est donc pas une faille : c'est une garantie qui repose sur une convention que rien n'applique, sur la méthode où elle coûterait le plus cher.

*Correction* : déplacer la résolution de capacité dans le grain (il a déjà `SecurityModule.HasCapabilityAsync`), et laisser celle du handler comme réponse rapide au client. C'est très exactement ce que le commentaire de `HasCapabilityAsync` prescrit pour les autres pouvoirs staff.

### SEC-15 — Le ticket SSO est rejouable sans limite, et la protection existe mais est livrée éteinte (P1, confirmé)

Le rapport de bêta notait AUTH-01 « ticket rejouable dans une fenêtre glissante de 30 s ». C'est plus grave que ça, et plus précis.

**La protection a été écrite.** `AuthenticationService.cs:90` consomme le ticket à la première utilisation quand `TicketSingleUse` est vrai, avec le bon commentaire : *« an observed ticket (proxy logs, browser history, unencrypted transport, Referer) can no longer be replayed at all »*.

**Elle est livrée éteinte, et rien ne la remplace :**

| Réglage | Défaut | Dans votre `appsettings.json` |
|---|---|---|
| `TicketSingleUse` | `false` (bool sans initialiseur) | **non déclaré** |
| `TicketAbsoluteLifetimeSeconds` | `null` (plafond désactivé) | **non déclaré** |
| `TicketTtlSeconds` | `30` | non déclaré |

La section `Vortex:Authentication` de `appsettings.json` ne contient **qu'une seule clé**, `IpHashSecret`, dont la valeur livrée est `"replace-with-a-production-secret"`.

**Conséquence, en suivant la branche `else` :** à chaque usage, l'expiration est *repoussée* de 30 s (`slidExpiry`). Le plafond absolu qui bornerait le total est `null`. Donc **un ticket observé peut être rejoué indéfiniment**, chaque rejeu prolongeant sa propre validité. Ce n'est pas une fenêtre de 30 secondes : c'est une fenêtre de 30 secondes qui se déplace aussi longtemps que l'attaquant s'en sert.

**Et aucun garde-fou ne le signale.** `AuthenticationConfigValidator` ne mentionne jamais `TicketSingleUse` (0 occurrence) et autorise explicitement le plafond absent : *« Leave it unset to disable the cap »*. La combinaison « pas d'usage unique **et** pas de plafond » est la seule qui soit dangereuse, et c'est la seule que la validation ne regarde pas.

Le défaut est documenté comme délibéré — *« Left default (TicketSingleUse = false) for compatibility with CMS integrations that reuse one ticket across reconnects »* — ce qui est une raison valable pour l'option, pas pour l'absence de plafond.

*Correction* (par ordre d'effet, aucune ne touche au code) : déclarer `TicketAbsoluteLifetimeSeconds` dans `appsettings.json` — le plafond borne le rejeu **sans casser** les intégrations CMS qui rejouent un ticket ; passer `TicketSingleUse` à `true` si votre CMS ne le fait pas ; remplacer `IpHashSecret`. Puis ajouter au validateur la règle qui manque : refuser le démarrage quand les deux protections sont absentes à la fois.

### SEC-13 — Deux écrans d'argent branchés sur des handlers vides (P3, confirmé)

`Vault/WithdrawCreditVaultMessageHandler` et `Marketplace/BuyMarketplaceTokensMessageHandler` ont pour corps entier `await ValueTask.CompletedTask`. Le client affiche l'écran, le joueur clique, rien ne se passe et rien ne le dit. Ce n'est pas une faille — c'est la classe « déclaré mais inerte » de l'audit wired, sur des écrans où le joueur croit manipuler de l'argent.

### SEC-14 — L'objet en main n'est pas validé (P3, confirmé, **délibérément non corrigé**)

`RoomHandItemModule.Give(playerId, itemId)` n'exige que `itemId > 0`. N'importe quel identifiant d'objet-en-main peut être placé dans la main d'un avatar. C'est cosmétique et temporaire (`HandItemDurationMs`), donc P3 — mais c'est une valeur du fil qui atteint un état diffusé à toute la room sans être vérifiée contre une liste connue.

**Laissé ouvert, et voici pourquoi.** Corriger demande une plage valide, et il n'en existe **aucune autorité** : ni énumération ni table dans le dépôt, rien dans le port TypeScript du client non plus (recherché sur `CarryItem` croisé avec `max|valid|range`). Inventer une borne risquerait de refuser des objets légitimes — une régression visible par les joueurs — pour fermer un défaut cosmétique, juste avant une réouverture. Le bon ordre est : établir la liste depuis le client ou depuis les données, *puis* borner.

---

## 5. Le défaut structurel : l'autorisation n'est pas inspectable

### 5.1 Quinze idiomes

Inventaire produit par le contrôle livré, sur 179 points de décision :

| Idiome | Occurrences | À quoi il ressemble |
|---|---:|---|
| *(aucun trouvé)* | 48 | — |
| `security-module` | 24 | `await SecurityModule.CanManipulateFurniAsync(ctx)` |
| `account-id` | 23 | `ctx.AccountId(sessions)` puis `Unauthorized()` |
| `actor-guard` | 16 | `if (ctx.PlayerId <= 0) return;` |
| `owner-compare` | 14 | `if (item.OwnerId != ctx.PlayerId) return null;` |
| `can-helper` | 13 | `CanEditContractAsync`, `CanUseChestAsync`, … |
| **`gated-lookup`** | **11** | **`FindManipulableItemAsync(ctx, itemId)`** |
| `selected-player` | 7 | `SelectedPlayerAsync(ctx, sessions, players, ct)` |
| `endpoint-filter` | 7 | `.AddEndpointFilter(RequireTokenAsync)` |
| `room-owner` | 6 | `if (!await IsRoomOwnerAsync(actor)) return false;` |
| `anonymous` | 3 | `.AllowAnonymous()` |
| **`ensure-helper`** | **2** | **`EnsurePetOwner(ctx, pet);`** |
| `require-authorization` | 2 | `.RequireAuthorization(Capabilities.…)` |
| `controller-level` | 1 | `GetControllerLevelAsync(ctx)` |
| *(implémentation non résolue)* | 2 | — |

**27 combinaisons distinctes.** Les deux en gras sont celles qui ne se voient pas :

- `gated-lookup` : la porte est **dans une recherche**. `FindManipulableItemAsync(ctx, id)` interroge le module de sécurité et rend `null` si l'acteur n'a pas les droits ; `_state.ItemsById.TryGetValue(id, out item)` ne demande rien. Un identifiant d'écart, sens opposé.
- `ensure-helper` : `EnsurePetOwner(ctx, pet);` ne rend rien et lève. Sur la ligne d'appel, **rien n'indique qu'une vérification a eu lieu** — ni valeur de retour, ni `if`, ni `await`.

### 5.2 Et pas de porte d'authentification dans le pipeline

Le chemin d'un paquet est `PackageHandler.HandleCoreAsync` → `MessageSystem.PublishAsync` → `MessageRegistry.PublishAsync` → handler. **Je les ai lus tous les trois : aucun ne refuse une session non authentifiée.** Le seul rempart est le `if (ctx.PlayerId <= 0) return;` que chaque handler écrit lui-même — 351 sur 559 le font, et parmi ceux qui ne le font pas, la plupart délèguent correctement (§2).

C'est le constat SES-02 du rapport de bêta, ici quantifié et confirmé jusqu'au bout. Il est de gravité modérée en soi : `PlayerId` vaut `-1`, et les chemins d'écriture s'arrêtent (`CreateRoomAsync` lève sur `Player -1 not found`, vérifié). Mais il transforme chaque handler non gardé en amplificateur — c'est le moteur de SEC-11.

### 5.3 Pourquoi cela produit des surprises, précisément

Une faille d'autorisation n'apparaît pas au moment où on l'écrit. Elle apparaît quand quelqu'un l'essaie. Entre les deux, la seule chose qui pourrait la signaler est une relecture — et ici la relecture ne le peut pas :

1. **Rien ne dit combien de portes il devrait y avoir.** Il n'existe pas de liste des points de décision, donc pas de notion de couverture.
2. **Deux idiomes sur quinze sont invisibles** dans un diff (§5.1).
3. **Le compilateur ne voit rien** : oublier une porte, c'est écrire *moins* de code, jamais du code invalide.
4. **Les tests ne voient rien** : un test vérifie qu'une action autorisée marche. Il faut écrire *exprès* le test de l'acteur non autorisé, et il existe **pour deux méthodes sur 121**.
5. **La CI ne tourne pas** : `VortexCloudFastCheck` est rouge pour d'autres raisons, donc les commits partent en `--no-verify` (constat QA-01 du rapport de bêta, toujours vrai).

Cinq filets, cinq trous, au même endroit.

---

## 6. Ce qui est livré, et la preuve

`scripts/hooks/check-authorization-surface.mjs` (+ `authorization-surface-baseline.json`).

```
check-authorization-surface: OK (179 entries: 121 room-grain methods, 58 HTTP endpoints;
15 distinct gate idioms).
```

**Ce qu'il ne fait pas** : juger si une autorisation est correcte. Avec 15 idiomes c'est hors d'atteinte, et prétendre le contraire donnerait un contrôle qui rassure à tort — le pire résultat possible pour un outil de sécurité.

**Ce qu'il fait** : inventorier les 179 points de décision **avec le nom de la porte qui garde chacun**, et bloquer quand cet inventaire bouge — une entrée **nouvelle**, ou une entrée dont la porte **a disparu**. Il ne demande pas d'avoir raison ; il demande d'être explicite, une fois, dans le diff où c'est gratuit.

**La preuve de régression.** J'ai simulé l'erreur exacte que ce contrôle existe pour attraper — remplacer la recherche gardée par la lecture brute dans `SetCustomStackHeightAsync` :

```
check-authorization-surface: the authorization surface moved.

  CHANGED  grain:IRoomFurni.Interactive.cs::SetCustomStackHeightAsync
           gated-lookup  ->  NONE
exit=2
```

Une modification d'un identifiant, qui ne casse ni le build ni un test ni une revue, et qui rendait la hauteur d'empilement de n'importe quel mobi modifiable par n'importe quel visiteur. Le fichier a été restauré immédiatement après ; `git status` est propre sur ce fichier.

La baseline porte une note par classe d'entrée, dont celle-ci, qui est la seule chose à retenir si vous ne lisez rien d'autre :

> `gate:NONE` — « Pour la plupart c'est correct : un avatar qui agit sur son propre avatar (danse, frappe, posture) n'a besoin d'aucune autorité au-delà d'être dans la room. Ce n'est **pas** automatiquement correct pour ce qui touche la propriété d'autrui, une monnaie, ou les réglages de la room. **Un nouveau `NONE` est celui qu'il faut lire deux fois.** »

**Adoption** : une ligne dans `VortexCloudFastCheck`, que je n'ai pas ajoutée — même raison qu'au wired : la cible est rouge, et le rapport devait livrer un contrôle sans toucher au code de production.

```xml
<Exec Command="node scripts/hooks/check-authorization-surface.mjs"
      WorkingDirectory="$(MSBuildThisFileDirectory)" />
```

---

## 7. Plan

**Avant la bêta**

1. **SEC-10** (2 h) — `maxConnectionNumber` sur les deux écouteurs + compteur par IP dans `SessionGateway`. *Critère* : la (N+1)ᵉ connexion d'une IP est fermée.
2. **SEC-11** (2 h) — garde `PlayerId`, format du code validé avant de nommer le grain, comptage des échecs. *Critère* : un code hors format n'active aucun grain.
3. **Brancher le contrôle** (10 min, après la réparation de la barrière du §5.3). Sans elle, il ne s'exécutera jamais.

**Peu après**

4. **SEC-12** (0,5 j) — la capacité `room.furni.edit` résolue dans le grain ; celle du handler devient une réponse rapide, pas la garantie.
5. **Le test qui manque** (1 j) — `StaffPowerGrainGateTests` est le bon modèle, appliqué à deux méthodes. Le porter aux ~20 méthodes de grain qui gardent autre chose que l'avatar de l'acteur : pour chacune, un acteur sans droit, et l'assertion que rien n'a bougé.
6. **SEC-13 / SEC-14** (2 h) — brancher ou retirer les deux handlers vides ; valider l'objet-en-main contre la liste des définitions.

**Structurel, quand vous voudrez**

7. **Réduire 15 idiomes à un** (2 à 3 j). La cible n'est pas de tout réécrire mais de rendre la porte **visible et nommée** partout : un `RoomAuthority.RequireAsync(ctx, …)` explicite, et surtout la fin de `gated-lookup` — une recherche qui autorise devrait s'appeler `FindItemIfAllowedAsync`, ou mieux, rendre l'autorisation et l'objet séparément. Le contrôle livré mesure l'avancement : le nombre d'idiomes distincts doit baisser à chaque étape.

---

## 8. Couverture et limites

**Vérifié à la main, des deux côtés** : les 15 chemins du §3, les 4 défauts du §4, le pipeline de paquets de bout en bout (`PackageHandler` → `MessageSystem` → `MessageRegistry` → handler), `RoomSecurityModule` en entier, `RoomService.Trading/Create/Doorbell`, `CatalogPurchaseGrain`, `VoucherGrain`, `RoomHandItemModule`, les 45 endpoints de `WebApiEndpoints.cs`, `SupervisorEndpoints.cs`, `ClientPacketDecoder`, `RateLimitConfig`, `SessionGateway`.

**Analysé mécaniquement** : 559 handlers de paquets, 121 méthodes de grain room prenant un acteur, 58 endpoints HTTP, 179 points de décision d'autorisation.

**Non vérifié — et il faut le savoir avant de s'appuyer sur ce rapport** :

- **Aucune exécution.** Pas de MySQL, pas d'émulateur, pas de client dans cet environnement. **Aucun des défauts n'a été exploité pour de vrai** : ils sont établis par lecture du code, y compris SEC-10 et SEC-11, dont l'effet réel dépend de votre hébergement (un pare-feu ou un reverse-proxy en amont peut déjà borner les connexions — je n'ai pas vu votre déploiement).
- **La cryptographie** (`Vortex.Crypto`, la poignée de main Diffie-Hellman, RC4) : non auditée. C'est un domaine où une revue superficielle est pire qu'aucune.
- **L'authentification et les sessions web** : `AuthenticationService`, `WebApiSessionStore`, hachage des mots de passe, gestion des cookies. Le rapport de bêta les couvre (AUTH-01 ticket SSO rejouable, SEC-01 pas de verrouillage de compte, sessions non révoquées au ban) ; je n'y suis pas retourné et **ces trois constats restent ouverts**.
- **Les 48 entrées `NONE`** : j'en ai lu une quinzaine (avatar, animaux, objets-en-main, cadeaux, mobi-crédit). Les autres — notamment `ClaimWelcomeGiftAsync`, `HitCrackableAsync`, `UseMysteryBoxAsync`, `GetWiredDataSnapshotByFloorItemIdAsync`, `AddPlayerToRoomAsync` — sont inventoriées mais **pas relues une par une**. C'est le premier endroit où continuer.
- **L'injection SQL** : non recherchée systématiquement. Le dépôt utilise EF Core avec LINQ paramétré partout où j'ai regardé, ce qui rend la classe improbable, mais « improbable » n'est pas « vérifié ».
- **Les plugins** : la surface d'extension (`Vortex.Plugins`) charge du code tiers dans le processus. Hors périmètre ici, et c'est un audit à part entière.

*(Les grains hors room figuraient dans cette liste ; ils sont désormais couverts, §10.)*


---

## 10. Les grains hors room

Ajouté après coup : le §8 listait les grains hors room comme non couverts. Ils le sont maintenant, et la règle que le dépôt énonce — « *callable by anything in the cluster that can name it* » — ne parlait jamais de rooms.

### 10.1 Résultat

**Aucun trou exploitable.** Sur 62 interfaces de grain :

| Surface | Constat |
|---|---|
| **20 grains à clé chaîne** | 19 sont des singletons (`SingletonGrainId.GLOBAL`). **Un seul** prend une chaîne du client : `IVoucherGrain`, déjà rapporté en SEC-11. La classe « clé de grain choisie par le client » est donc close, avec une seule instance. |
| **24 méthodes hors room prenant un acteur** | 15 sur `IGroupGrain`, 8 sur `IGroupForumGrain`, 1 sur `IPlayerGrain`. **Toutes gardées.** |
| **31 méthodes agissant sur un tiers nommé** (`targetPlayerId`) | Exclusion, promotion, bannissement, modération de forum : vérifiées, gardées. |

Les groupes sont le système le mieux gardé que j'aie lu dans ce dépôt. `KickCoreAsync` refuse même d'exclure le propriétaire, avec la raison écrite : *« a guild without an owner has nobody who can disband or repair it »*.

### 10.2 Mais sept idiomes de plus, dont trois invisibles

La couture du §5 ne s'arrête pas aux rooms. Les groupes répondent à la même question avec un vocabulaire **entièrement distinct**, que rien ne relie au précédent :

| Idiome | Exemple | Visible au point d'appel ? |
|---|---|---|
| comparaison en ligne | `group.OwnerPlayerEntityId != actorId` | oui |
| `IsAdminAsync(dbCtx, group, actorId, ct)` | | oui |
| matrice de permissions | `Allows(settings.ModPermission, role)`, `CanRead`, `PostPermission` | oui |
| **chargeur gardé** | `LoadIfAdminAsync(dbCtx, actor, ct)` → `null` si refusé | **non** |
| **chargeur gardé à tuple** | `LoadForModerationAsync(...)` → `(null, ForumRole.None)` | **non** |
| **enrobage de mutation** | `MutateAsAdminAsync(actor, group => { … }, ct)` | **non** |

Le deuxième mérite d'être regardé de près, parce qu'il est le plus trompeur du dépôt :

```csharp
(GroupEntity? group, ForumRole role) = await LoadForModerationAsync(dbCtx, actor, ct);
if (group is null) { return null; }
// `role` n'est plus jamais utilisé
```

Le rôle est extrait puis **jeté**. Le refus voyage sur le `group is null`, pas sur le rôle. Une relecture rapide y voit un chargement qui a échoué, pas une autorisation refusée — et quelqu'un qui « nettoierait » ce `role` inutilisé toucherait à la seule ligne qui dit que cette méthode est gardée.

Le troisième plie la porte dans un enrobage qui prend une lambda : au point d'appel, `UpdateBadgeAsync` ne montre qu'un acteur et une mutation, jamais une vérification.

### 10.3 Deux grains qui délèguent à leur appelant (forme SEC-12)

- **`StaffModerateThreadAsync(int actorPlayerId, …)`** ne vérifie **rien**. Son unique appelant de production est la route dashboard, qui exige `Capabilities.Dashboard.OpsGuildsManage`. Correct aujourd'hui.
- **`SetHotelMuteAsync(PlayerId targetPlayerId, DateTime? expiresUtc)`** — mute à l'échelle de l'hôtel, et **aucun paramètre d'acteur**. Le grain ne peut donc pas vérifier, même en principe. `ModMuteMessageHandler` résout bien `ModerationAction.Mute` avant d'appeler. *C'est la signature qui est le constat* : un acteur qu'on ne passe pas ne peut pas être vérifié.

### 10.4 Une fausse piste, et pourquoi elle compte

J'ai cru tenir une fuite de vie privée : `PlayerEntity.ProfileVisible` est appliqué avec soin côté site — profil privé = en-tête seul, jamais un 404, pour ne pas offrir un oracle d'énumération de pseudos — et **n'apparaît pas une seule fois** dans `Vortex.Players`, donc le profil complet part quand même sur le socket de jeu.

C'est un choix documenté, sur la propriété elle-même :

> « *It governs the WEB profile only. Nothing on the game socket reads it… calling this one "private" for that too would be the same promise broken a second time.* »

**Ce n'est donc pas un défaut, et c'est le septième cas de la session** où un détecteur pointe une décision délibérée. Le point n'est pas que je me sois trompé : c'est que la justification vivait dans un commentaire XML sur une propriété d'entité, trois projets plus loin que le handler concerné. Aucun outil ne pouvait la voir, et un relecteur pressé non plus.

### 10.5 Ce que ça change pour la refonte

`docs/audits/authorization-redesign.md` propose `[RequiresRoomAuthority]` sur les interfaces de grain room. **Le périmètre est à élargir** : `IGroupGrain` et `IGroupForumGrain` en ont autant besoin, et `SetHotelMuteAsync` montre le cas que l'attribut ne peut pas traiter seul — une méthode sans acteur doit d'abord en recevoir un.

Le contrôle livré couvre désormais cette surface : **220 entrées** (162 méthodes de grain prenant un acteur, 58 endpoints HTTP), **19 idiomes distincts** au lieu de 15.