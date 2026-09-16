# Refonte de l'autorisation — document de conception

- **Statut** : conception. **Aucun code de production n'est modifié par ce document.**
- **Révision de référence** : `62844a3` sur `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date** : 2026-09-15.
- **Prérequis de lecture** : `docs/audits/security-audit.md` (le constat que ce document propose de corriger).
- **Portée** : la frontière d'authentification des paquets, et la frontière d'autorisation des grains room. Pas la cryptographie, pas les sessions web, pas les plugins.

---

## 1. Ce qu'on corrige, en une page

L'audit de sécurité n'a trouvé **aucun chemin exploitable** sur les trajets qui comptent. Le défaut n'est pas le niveau de sécurité : c'est que la sécurité **n'est pas inspectable**.

> La même question — « cet acteur a-t-il le droit ? » — reçoit **15 réponses différentes, en 27 combinaisons, sur 179 points de décision**. Deux d'entre elles sont, en lecture de diff, indiscernables d'une méthode qui ne vérifie rien.

```csharp
IRoomItem? item = await FindManipulableItemAsync(ctx, itemId);   // interroge le SecurityModule
_state.ItemsById.TryGetValue(itemId, out IRoomItem? item);       // n'interroge rien
```

**Le diagnostic tient en une phrase** : l'autorisation est aujourd'hui *une étape qu'on se rappelle d'écrire*. Rien n'oblige la question à être posée ; l'oublier, c'est écrire *moins* de code, jamais du code invalide. Le compilateur ne voit rien, les tests ne voient rien (il faut écrire *exprès* le test de l'acteur non autorisé, et il existe pour 2 méthodes sur 121), et aucun outil ne peut calculer la couverture puisqu'il n'existe aucune liste des points de décision.

**Le principe de la refonte tient en une autre** :

> Transformer l'autorisation d'une **étape mémorisée** en une **déclaration que le framework applique et qu'un script peut compter.**

Deux portes, la même forme, aux deux frontières réelles du système.

---

## 2. Le point décisif : les deux mécanismes existent déjà

C'est ce qui rend cette refonte raisonnable avant une réouverture plutôt que téméraire. Elle n'introduit **aucune infrastructure nouvelle** : elle applique deux motifs que votre serveur exécute déjà en production.

| Mécanisme | Déjà utilisé par | Pour la refonte |
|---|---|---|
| `IIncomingGrainCallFilter` | `ObservabilityGrainCallFilter` (`Vortex.Observability/Runtime/`), enregistré en DI | `AuthorizationGrainCallFilter` |
| `IMessageBehavior<IMessageEvent>` + `[Order(int.MinValue)]` | `RateLimitBehavior` (`Vortex.Messages/Behaviors/`) | `AuthenticationBehavior` |

Le filtre d'observabilité lit déjà `context.InterfaceMethod` — c'est très exactement ce qu'il faut pour lire un attribut posé sur la méthode d'interface. Et `RateLimitBehavior` est enregistré pour `IMessageEvent` lui-même, en s'appuyant sur `EnableInheritanceDispatch` pour couvrir tous les types concrets sans les énumérer : c'est la mécanique dont la porte d'authentification a besoin, déjà éprouvée sur le même chemin.

---

## 3. Porte 1 — l'authentification, dans le pipeline de paquets

### 3.1 L'état actuel

Le chemin d'un paquet est `PackageHandler.HandleCoreAsync` → `MessageSystem.PublishAsync` → `MessageRegistry.PublishAsync` → handler. **Aucun des trois ne refuse une session non authentifiée.** Le seul rempart est le `if (ctx.PlayerId <= 0) return;` que chaque handler écrit lui-même : **351 sur 559** le font.

C'est le bon comportement obtenu par la mauvaise méthode. Il est répété 351 fois, absent 208 fois, et rien ne dit laquelle des 208 absences est délibérée.

### 3.2 La cible

```csharp
[Order(int.MinValue + 1)]   // juste après RateLimitBehavior
public sealed class AuthenticationBehavior(IVortexMetrics metrics)
    : IMessageBehavior<IMessageEvent>
{
    public ValueTask InvokeAsync(
        IMessageEvent env, MessageContext ctx, Func<ValueTask> next, CancellationToken ct)
    {
        if (ctx.PlayerId > 0 || PreAuthentication.Allows(env.GetType()))
        {
            return next();
        }

        metrics.PacketDropped("unauthenticated");
        return ValueTask.CompletedTask;
    }
}
```

et, sur les seuls handlers qui tournent avant le login :

```csharp
[PreAuthentication("Établit la session : c'est ce paquet qui fournit le PlayerId.")]
public class SSOTicketMessageHandler(...) : IMessageHandler<SSOTicketMessage>
```

### 3.3 L'ensemble pré-auth est petit, et c'est tout l'intérêt

Huit handlers, tous dans `Vortex.PacketHandlers/Handshake/`, dont **sept ne mentionnent jamais `ctx.PlayerId`** — ce qui est la preuve mécanique qu'ils n'en ont pas besoin :

| Handler | `ctx.PlayerId` | Rôle |
|---|---:|---|
| `ClientHelloMessageHandler` | 0 | poignée de main |
| `VersionCheckMessageHandler` | 0 | poignée de main |
| `InitDiffieHandshakeMessageHandler` | 0 | échange de clés |
| `CompleteDiffieHandshakeMessageHandler` | 0 | échange de clés |
| `UniqueIdMessageHandler` | 0 | identifiant machine |
| `PongMessageHandler` | 0 | battement de cœur |
| `DisconnectMessageHandler` | 0 | fermeture |
| `SSOTicketMessageHandler` | 5 | **établit** la session |

`InfoRetrieveMessageHandler`, qui vit dans le même dossier, utilise `ctx.PlayerId` deux fois : il est post-auth et ne doit pas être dans la liste.

Une liste de huit lignes, relisible d'un coup d'œil, remplace une propriété aujourd'hui invisible et répartie sur 559 fichiers.

### 3.4 Ce que ça achète

- Les **208** handlers sans garde deviennent inatteignables avant login — y compris `RedeemVoucher`, qui est la racine de SEC-11.
- Les **351** gardes manuelles deviennent redondantes. On peut les supprimer, ou les laisser : elles ne coûtent rien et documentent l'intention. **Recommandation : les supprimer par lots, après la porte, jamais avant.**
- Un nouveau handler est authentifié **par défaut**. C'est le renversement qui compte : aujourd'hui l'oubli ouvre, demain l'oubli ferme.

### 3.5 Le risque, et comment le tenir

**Le risque réel** : un handler légitimement pré-auth qu'on oublie de déclarer ⇒ plus personne ne peut se connecter. C'est un risque de *disponibilité*, pas de sécurité, et il se manifeste au premier login en développement — pas en production.

**Le tenir** : un test qui envoie la séquence de connexion complète sur une session non authentifiée et vérifie qu'elle aboutit. Si la liste est incomplète, ce test rougit avant le déploiement.

---

## 4. Porte 2 — l'autorisation, à la frontière du grain

### 4.1 La règle existe déjà, écrite noir sur blanc

```
« A handler is not a security boundary: the method is a member of a public grain interface,
  callable by anything in the cluster that can name the room (ROOMG-GATE-038).
  The grain is the boundary. »
                          — Vortex.Rooms/Grains/Modules/RoomSecurityModule.cs:265
```

Elle est appliquée, et **testée pour exactement deux méthodes** (`StaffPowerGrainGateTests`). Les 119 autres méthodes de grain room prenant un acteur reposent sur le fait que chaque auteur y a pensé. Et une quatrième, `ApplyFurniEditAsync`, énonce explicitement **l'inverse** (SEC-12).

La refonte ne change pas la règle. Elle la rend **structurelle** au lieu de mémorisée.

### 4.2 La cible

Sur l'**interface** — c'est-à-dire là où une revue de diff regarde :

```csharp
public interface IRoomSettings : IGrainWithIntegerKey
{
    [RequiresRoomAuthority(RoomRequirement.Owner)]
    Task<bool> UpdateRoomSettingsAsync(PlayerId actor, RoomSettingsUpdate update, CancellationToken ct);

    [RequiresRoomAuthority(RoomRequirement.Rights)]
    Task SetRoomTagsAsync(PlayerId actor, ImmutableArray<string> tags, CancellationToken ct);
}

public interface IRoomAvatars : IGrainWithIntegerKey
{
    [NoRoomAuthority("Agit sur l'avatar de l'acteur lui-même ; être dans la room suffit.")]
    Task SetAvatarDanceAsync(ActionContext ctx, int danceId, CancellationToken ct);
}
```

et un filtre qui applique, sur le modèle exact de `ObservabilityGrainCallFilter` :

```csharp
public sealed class AuthorizationGrainCallFilter(...) : IIncomingGrainCallFilter
{
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (context.InterfaceMethod?.GetCustomAttribute<RequiresRoomAuthorityAttribute>()
            is not { } required)
        {
            await context.Invoke().ConfigureAwait(false);   // [NoRoomAuthority] ou hors périmètre
            return;
        }

        // L'acteur se lit dans les arguments : PlayerId actor, ou ActionContext ctx.
        // La convention est validée au démarrage (§4.3), donc ici elle tient.
        if (!await Authority.GrantsAsync(context, required.Requirement))
        {
            throw new VortexException(VortexErrorCodeEnum.NoPermission);
        }

        await context.Invoke().ConfigureAwait(false);
    }
}
```

`RoomRequirement` est l'ensemble **nommé** des exigences, et sa mise en œuvre est le `RoomSecurityModule` d'aujourd'hui, inchangé : `Owner` → `IsRoomOwnerAsync`, `Rights` → `CanManipulateFurniAsync`, `Capability(x)` → `HasCapabilityAsync`, `ItemOwner` → la comparaison de propriétaire. **La refonte ne redéfinit aucune règle métier** — c'est la condition pour qu'elle soit sûre à faire avant une réouverture.

### 4.3 Le morceau qui change tout : la validation au démarrage

```csharp
// Au démarrage du silo : énumère les méthodes de toutes les interfaces de grain room qui
// prennent un acteur, et refuse de démarrer si l'une ne déclare rien.
IReadOnlyList<MethodInfo> undeclared = RoomAuthoritySurface.FindUndeclared();

if (undeclared.Count > 0)
{
    throw new InvalidOperationException(
        "Ces méthodes de grain room prennent un acteur sans déclarer "
      + "[RequiresRoomAuthority] ni [NoRoomAuthority] :\n  "
      + string.Join("\n  ", undeclared.Select(m => $"{m.DeclaringType!.Name}.{m.Name}")));
}
```

C'est le passage de :

> « rien ne peut calculer la couverture »

à :

> « la couverture est totale, ou le processus ne démarre pas ».

Le contrôle `check-authorization-surface.mjs` livré avec l'audit cesse alors d'être un inventaire heuristique et devient une **assertion**, vérifiable hors ligne comme au démarrage.

C'est aussi la réponse à la question que vous avez laissée à mon jugement : **refuser de démarrer** plutôt que refuser l'appel. Une méthode non déclarée est une erreur de programmation, pas un événement d'exécution ; elle doit coûter un démarrage raté en développement, jamais une fonctionnalité morte en silence chez un joueur.

### 4.4 Ce que ça achète

- Les deux idiomes invisibles **disparaissent de la frontière** : la porte remonte sur l'interface, visible dans le diff.
- SEC-12 se résout par construction : `ApplyFurniEditAsync` porte `[RequiresRoomAuthority(RoomRequirement.Capability(Capabilities.Room.FurniEdit))]` et la garantie cesse de dépendre de la bonne volonté de son appelant.
- Une nouvelle méthode de grain room **ne peut pas** être ajoutée sans que quelqu'un écrive, en une ligne, ce qu'elle exige — ou pourquoi elle n'exige rien.
- La phrase « le grain est la frontière » devient vraie mécaniquement, et pas seulement en commentaire.

---

## 5. Plan d'exécution

Cinq étapes, chacune buildée et testée séparément, chacune arrêtable. L'ordre n'est pas négociable : les portes avant les suppressions, toujours.

| # | Étape | Coût | Ce que ça ferme |
|---|---|---|---|
| 1 | **SEC-10 + SEC-11** — plafond de sessions par IP et global ; garde + format + comptage sur les bons | ~0,5 j | les deux défauts exploitables |
| 2 | **Porte 1** — `AuthenticationBehavior` + `[PreAuthentication]` sur les 8 | ~0,5 j | les 208 handlers non gardés |
| 3 | **Porte 2** — `RoomRequirement`, attributs, filtre, validation au démarrage, **sans migrer aucun appelant** | ~1 j | l'infrastructure, à vide |
| 4 | **Déclarer les 121 méthodes** — une ligne chacune, par famille d'interface | ~1,5 j | la couverture passe à 100 % |
| 5 | **Retirer les gardes désormais redondantes** + re-baseline des contrôles + tests de refus | ~1 j | la duplication, et la dérive future |

**Total : 4 à 5 jours**, dont les deux premiers referment ce qui est réellement exploitable. Si vous vous arrêtez après l'étape 2, vous avez déjà l'essentiel du gain de sécurité ; les étapes 3 à 5 achètent la *non-régression*, c'est-à-dire l'absence de surprises futures.

L'étape 4 est la plus longue et la moins risquée : elle n'écrit aucune logique, seulement des déclarations, et la validation du §4.3 dit exactement quand elle est finie.

---

## 6. Les pièges, trouvés en écrivant le code

J'ai commencé l'implémentation des étapes 1 et 2 avant de la retirer à votre demande ; l'arbre est propre. Ce qu'elle a appris mérite d'être écrit, parce que ce sont les endroits où une reprise se trompera.

**Le décompte par IP doit être clé sur l'adresse d'*admission*, pas sur le contexte.** À la fermeture, `ISessionContext.RemoteIpAddress` peut déjà être nul : décrémenter en relisant l'adresse depuis le contexte fuit un jeton par déconnexion, jusqu'à ce que le plafond refuse tout le monde. Il faut une seconde table `SessionKey → adresse admise`.

**L'incrément et le test doivent être un seul pas atomique.** `AddOrUpdate` puis comparaison du résultat, jamais « lire, comparer, incrémenter » : deux connexions arrivant sur le dernier jeton passeraient toutes les deux.

**Un refus doit rendre son jeton.** Sinon un attaquant refusé consomme quand même le plafond de tous ceux qui partagent son adresse — le refus devient l'attaque.

**`AddSessionAsync` doit renvoyer sa décision.** Aujourd'hui elle rend `Task` ; il faut `Task<bool>` et deux appelants (`SuperSocketHostBuilderExtensions`, `NetworkManager`) qui ferment le transport sur refus. Fermer depuis l'intérieur de la passerelle est plus court et moins honnête : l'appelant possède le transport.

**La longueur du code de bon n'est pas un réglage, c'est un fait.** `VoucherEntity.Code` est `[MaxLength(64)]` : une chaîne plus longue ne peut correspondre à aucune ligne. La refuser **avant** de nommer le grain ne peut donc rien casser qui aurait pu marcher — c'est ce qui rend la validation sûre à appliquer à des codes déjà en circulation. Restreindre le *jeu de caractères*, en revanche, casserait des codes existants qu'on ne peut pas énumérer sans la base.

**Le refus de bon doit être indiscernable.** Répondre « tu es limité » plutôt que « code inconnu » donne au devineur le seul bit dont il a besoin pour se cadencer.

---

## 7. Ce que cette refonte ne résout pas

À dire clairement, pour que le document ne promette pas plus qu'il ne tient.

- **Elle ne vérifie pas qu'une exigence est la *bonne*.** Déclarer `[RequiresRoomAuthority(RoomRequirement.Rights)]` là où il fallait `Owner` passe toutes les portes. Elle garantit qu'une décision a été **prise et écrite**, pas qu'elle est juste. C'est une amélioration énorme sur « aucune décision visible », et ce n'est pas la même chose que la justesse.
- **Elle ne couvre pas les grains hors room.** `IPlayerGrain`, `IPlayerWalletGrain`, les grains de catalogue et de marketplace ont la même propriété d'être appelables par tout le cluster. Le même attribut s'y étend sans rien changer au mécanisme, mais l'inventaire est à refaire et ce document ne l'a pas fait.
- **Elle ne touche ni la crypto, ni les sessions web, ni les plugins.** Les trois constats ouverts du rapport de bêta (AUTH-01 ticket SSO rejouable, SEC-01 pas de verrouillage de compte et sessions non révoquées au ban) restent entiers et ne sont pas adressés ici.
- **Elle ne remplace pas les tests de refus.** `StaffPowerGrainGateTests` reste le bon modèle : pour chaque exigence, un acteur qui ne l'a pas, et l'assertion que rien n'a bougé. La porte empêche l'oubli ; le test vérifie l'intention.

---

## 8. Critères de validation

Ce à quoi on reconnaît que c'est fini, sans avoir à croire quiconque sur parole.

1. `dotnet build Vortex.Cloud.sln` : 0 erreur. La référence d'avant travaux est verte (vérifié sur `62844a3`).
2. La suite complète passe, **sans test désactivé ni mis en quarantaine**.
3. Le silo démarre. S'il refuse, il nomme les méthodes non déclarées — et c'est le comportement attendu, pas une panne.
4. Un client se connecte et joue : la séquence de login complète aboutit sur une session non authentifiée (c'est le test qui garde la liste `[PreAuthentication]`).
5. `node scripts/hooks/check-authorization-surface.mjs` : plus aucune entrée `NONE` non déclarée ; le nombre d'idiomes distincts a baissé — c'est la mesure d'avancement de l'étape 5.
6. La (N+1)ᵉ connexion d'une même adresse est fermée immédiatement.
7. Un code de bon de 200 caractères n'active aucun grain, et le 11ᵉ échec en une minute non plus.
8. Un acteur sans droits appelant directement une méthode de grain protégée est refusé **par le filtre**, en ne passant par aucun handler.
