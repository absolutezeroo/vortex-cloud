# Signaux de progression — conception

**Date** : 2026-09-06 (v4)
**État** : proposée, non implémentée
**Périmètre** : reward tracks, tâches quotidiennes, succès. Le wired reçoit la grammaire, pas le
bus (§7.4). Historique des révisions en §16.

Un seul vocabulaire de faits, une seule traduction des événements du domaine, et trois systèmes de
progression qui s'y branchent au lieu d'en écrire chacun le sien.

Tout ce que cette conception affirme sur le code existant a été vérifié en le lisant, fichier par
fichier ; les chemins sont cités pour que ce soit re-vérifiable.

---

## 1. Ce qui existe aujourd'hui

### 1.1 Trois traductions du même événement

`Vortex.Primitives/Events` déclare **137 événements de domaine**. Trois familles de handlers les
traduisent, chacune avec son vocabulaire :

| Famille | Fichier | Handlers | Lignes | Vocabulaire |
| --- | --- | --- | --- | --- |
| Reward tracks | `Vortex.RewardTracks/Events/RewardTrackEventHandlers.cs` | 22 | 778 | action + montant + `target` + **faits** |
| Tâches quotidiennes | `Vortex.Progression/Quests/Events/DailyTaskProgressEventHandlers.cs` | 7 | 132 | `QuestTypes.*` + montant |
| Succès | `Vortex.Progression/Achievements/Events/AchievementProgressEventHandlers.cs` | 8 | 145 | `AchievementNames.*` + montant |

**Cinq événements sont traduits trois fois** — `PlayerEnteredRoomEvent`, `PlayerFigureChangedEvent`,
`PlayerMottoChangedEvent`, `ItemPlacedEvent`, `RespectGivenEvent` — et deux le sont deux fois
(`CatalogPurchasedEvent` : tracks + tâches ; `FriendRequestAcceptedEvent` : tâches + succès).
Dix-neuf handlers pour sept événements, 1 055 lignes en tout.

> **Conséquence mesurable** : enrichir un événement ne profite aujourd'hui qu'au système dont on a
> édité le handler. Les deux autres continuent d'ignorer la donnée qui vient d'arriver.

**115 des 137 événements n'alimentent aucun système de progression.** Le plafond n'a jamais été le
moteur de filtres, c'est la surface de traduction.

### 1.2 Deux des trois familles n'ont aucune porte

`RewardTrackSignal.SendAsync` commence par `if (!catalog.IsActionInteresting(action)) return;`, et
`RewardTrackCatalog` explique pourquoi : les entrées d'appart, les lignes de chat et les poses de
meubles arrivent constamment ; une action qu'aucun contenu ne mentionne coûte une recherche de hash
et s'arrête là.

**Les tâches quotidiennes et les succès n'ont pas cette porte.** `DailyTaskRoomEntryHandler` et
`AchievementRoomEntryHandler` appellent leur grain à **chaque** entrée d'appart, inconditionnellement.
Les appels sont `[OneWay]`, donc l'arrivée n'attend pas — mais chacun active un grain par joueur
(`PlayerDailyTaskGrain`, `PlayerAchievementGrain`), avec son chargement d'état, que le contenu du
jour mentionne ou non l'entrée d'appart.

> Aujourd'hui, une entrée d'appart = trois activations de handler + deux appels de grain
> inconditionnels + une recherche de hash. Cette conception ne peut qu'en retirer (§4.4).

### 1.3 La carte des faits est recopiée à la main

`RewardTrackActionFacts` décrit, action par action, les faits que le handler correspondant émet.
L'éditeur du dashboard la lit pour ne proposer que des filtres qui peuvent matcher
(`DashboardApiService.RewardTracks.cs:49`), et le validateur de contenu la lit pour refuser les
autres (`RewardTrackSequenceRules.cs:75,96`, `filter_fact_not_emitted_by_action`).

Son propre commentaire affirme que `RewardTrackActionFactsTests` empêche les deux listes de diverger.
**Ce test n'existe pas** — aucun fichier du dépôt ne référence `RewardTrackActionFacts` hors de ces
trois sites. Les deux listes avaient donc divergé sur **neuf actions** : la carte annonçait des
faits qu'aucun handler n'émettait, l'éditeur les proposait, et un filtre écrit dessus ne pouvait
jamais matcher.

| Action | Faits annoncés et jamais émis |
| --- | --- |
| `create_room` | `room` |
| `chat_with_someone` | `room` |
| `dance`, `wave` | `room` |
| `complete_trade` | `player` |
| `buy_from_catalogue` | `offer` |
| `use_habbicon` | `habbicon`, `room` |
| `complete_habbicon_collection` | `collection` |
| `pet_level` | `pet` |
| `wear_badge` | `badge` |

*(Corrigé et commité, `0c36265d5`.)*

La raison de fond n'est pas la négligence : **la traduction est soudée à un handler Orleans**, qui
demande un `IGrainFactory` et un `IRewardTrackCatalog` pour être instancié. On ne peut pas
l'exécuter dans un test unitaire sans monter la moitié du silo — c'est pour ça que le test annoncé
n'a jamais été écrit.

### 1.4 Ce que les handlers font d'inhabituel

Relevé en lisant les 22 handlers un par un. Chaque ligne est une contrainte sur la forme du
traducteur (§4) :

| Handler | Particularité |
| --- | --- |
| Achat catalogue | **Deux actions** par événement (`buy_from_catalogue` puis `spend_credits`, montants différents), et un **garde de rejeu** (`CommerceReplayGuard`, clé `"reward-track"`) *avant* la traduction, avec une troisième dépendance (`ICommerceJournal`) |
| Échange conclu | **Deux joueurs** : un signal par participant, l'autre comme fait `player` |
| Badges équipés | **N signaux** : un par badge de `ImmutableArray<string> BadgeCodes` |
| Meuble déplacé | **Deux handlers sur le même événement** : `move_item` toujours, plus `rotate_item` quand `RotatedInPlace`. Volontaire — une rotation *est* un déplacement, et l'exclure ferait compter moins les tâches `move_item` existantes |
| Geste | `dance` **ou** `wave`, rien pour un geste inconnu |
| Chat | Rien pour un chuchotement (« un chuchotement à soi-même farmerait la tâche ») |
| Niveau de familier | Le **montant est le niveau**, pas 1 — pour le mode `Highest`. Le crédit va au propriétaire, pas à celui qui a nourri |
| Habbicon utilisé | `RoomId` vaut **0** en conversation privée — émis tel quel aujourd'hui |
| Pas sur un meuble | **Le signal le plus fréquent du hall, de loin** : une fois par case, pour chaque avatar. Publié `PublishDetached` depuis le tick d'appart (`RoomAvatarTickSystem.cs:247`), dont le commentaire compte explicitement sur la porte d'intérêt de l'autre côté |
| Quête terminée, niveau de succès | Les reward tracks **consomment des événements que produisent** les quêtes et les succès — les deux futurs consommateurs |

### 1.5 Le garde de rejeu est par consommateur

`CommerceReplayGuard.FirstDeliveryAsync(journal, operationId, consumer, ct)`
(`Vortex.Primitives/Commerce/CommerceReplayGuard.cs`) enregistre un pas `RELAY:<consumer>` dans le
journal de commerce et répond `true` à la première livraison seulement. Les reward tracks passent
`"reward-track"`, les tâches quotidiennes `"daily-task"`. La clé porte **le nom du consommateur**,
et il faut le préserver : sans elle, le premier des trois à traiter une republication priverait les
deux autres de leur première livraison.

Ce qu'il ne fait **pas** : garantir la reprise après échec. Le reçu est écrit *avant* l'appel
métier, `InvokeOneAsync` avale l'exception du handler, et `CommerceRelayService` fait
`PublishAsync` puis `MarkRelayedAsync` — l'opération est marquée relayée même si un consommateur a
échoué dedans. C'est un garde contre la republication, pas un accusé de traitement (§7.3).

`RewardTrackCatalogPurchaseHandler` l'appelle **une seule fois**, puis émet ses deux signaux. Cette
cardinalité est ce que §3.1 préserve.

Un `operationId` vide ou non-GUID répond `true` sans rien écrire : le garde est inoffensif pour un
événement qui n'est pas rejouable — c'est-à-dire pour tous sauf l'achat catalogue.

### 1.6 Ce que le pipeline sait déjà faire — vérifié dans `Vortex.Pipeline`

- **Découverte** : `EnvelopeFeatureProcessor` balaye chaque assembly et enregistre tout type
  **public, concret, non générique** qui implémente un `IEventHandler<X>` fermé. Un type non public
  est ignoré *avec un avertissement* (`WarnNonPublic`).
- **Activation** : `ActivatorUtilities.CreateFactory` — **une instance par invocation**, résolue
  depuis le service provider, disposée après. Un handler n'a pas d'état entre deux événements.
- **Plugins** : `PluginManager` fait tourner le même `AssemblyProcessor` sur les assemblys de
  plugins avec le provider du plugin, et `InvokeOneAsync` enveloppe celui-ci dans un
  `CompositeServiceProvider(plugin, hôte)` — un handler de plugin **peut** résoudre un service de
  l'hôte. Le lot d'enregistrements est un `IDisposable` : au déchargement du plugin, ses handlers
  sont retirés (`EnvelopeHost`, `RemoveAll`).
- **Dispatch** : `HandlerMode = Parallel`, `Task.WhenAll` ; chaque handler est isolé
  (`OnHandlerInvokeError`), un handler qui tombe ne fait tomber ni l'action ni les autres.
- **Publication imbriquée** : `PublishAsync` ne porte **aucun garde de réentrance**. Un contexte
  neuf est créé par publication (corrélation lue depuis l'`AsyncLocal`). Aucun handler du dépôt ne
  publie aujourd'hui — c'est un pattern nouveau, mais rien ne l'interdit.
- **Rien ne s'y ajoute en douce** : aucun handler « attrape-tout » `IEventHandler<IEvent>` n'existe
  (le dispatch par héritage n'enverrait donc le signal à personne d'autre) ; un seul
  `IEventBehavior` existe, sur un événement de groupe ; `OnNoHandlerRegistered` n'est pas branché
  (pas de journal pour un événement sans abonné).

> **Il n'y a donc rien à construire côté infrastructure.** Le signal est un événement de plus.

---

## 2. Ce qu'on construit — et ce qu'on ne construit pas

**On construit :**

1. Un contrat de signal canonique, publié comme un `IEvent` ordinaire (§3).
2. Des **traducteurs** : une classe par événement de domaine, fonction pure sans dépendance, qui
   **se décrit elle-même** (§4).
3. Un `IAssemblyFeatureProcessor` de ~60 lignes qui découvre les traducteurs, les enregistre comme
   handlers et alimente le vocabulaire — le seul composant qui voit les plugins et leur déchargement
   (§4.5).
4. Un registre de faits **typés**, d'où l'éditeur déduit ses contrôles et ses opérateurs (§5).
5. Un évaluateur et un validateur de filtres partagés (§6).
6. **La porte d'intérêt**, reprise et élargie — listée ici parce que l'oublier transformerait cette
   conception en régression de performance sur les trois chemins les plus chauds du hall (§4.4).
7. Les consommateurs : reward tracks d'abord, puis tâches quotidiennes et succès (§7).

**On ne construit pas :**

- **Pas de nouveau bus.** Le pipeline d'événements existant suffit (§1.6).
- **Pas de réécriture du contenu existant.** Les codes `QuestTypes.*` et `AchievementNames.*`
  restent ; chaque consommateur garde sa table de correspondance code → action. Les valeurs des
  clés de faits et des actions existantes **ne changent pas** : elles sont en base.
- **Pas de versionnage de schéma à l'exécution.** Le vocabulaire est additif (§5.3).
- **Le wired n'est pas un consommateur du bus** (§7.4).
- **Pas d'enrichissement qui coûte une lecture** (§4.3).
- **Pas de double marche** ancien handler + nouveau consommateur, même pour comparer (§11.2).

---

## 3. Le contrat

```csharp
namespace Vortex.Primitives.Signals;

/// <summary>Ce qu'un joueur vient de faire, dit une seule fois pour tout le monde.</summary>
public sealed record ProgressSignal(
    long PlayerId,
    string Action,          // SignalActions.*
    int Amount,             // 1 pour un acte ; N pour "a dépensé N crédits" ; le niveau pour pet_level
    string? Target,         // de quoi le signal parle principalement (§3.3)
    ImmutableArray<SignalFact> Facts
);

public readonly record struct SignalFact(string Key, string Value);

/// <summary>
/// Ce qu'un événement du domaine a produit : un lot, pas un signal. Un événement source en produit
/// souvent plusieurs (§4.1), et c'est le lot qui porte l'identité de livraison.
/// </summary>
public sealed record ProgressSignalsRaised(
    string DeliveryId,      // l'OperationId de l'événement source, ou vide s'il n'est pas rejouable
    ImmutableArray<ProgressSignal> Signals
) : IEvent;
```

### 3.1 Pourquoi le lot, et pas un événement par signal

`DeliveryId` est sur l'enveloppe, pas sur le signal, et c'est **la seule forme qui préserve la
sémantique actuelle**.

`RewardTrackCatalogPurchaseHandler` appelle le garde de rejeu **une fois**, puis émet deux signaux
(`buy_from_catalogue` et `spend_credits`). Un consommateur qui gardait par *signal* écrirait le reçu
`RELAY:reward-track` sur le premier et **verrait le deuxième rejeté** : `TryRecordStepAsync` insère
une ligne `(OperationId, StepKey)` sous contrainte d'unicité, donc le second appel avec la même clé
répond `false`. Le crédit dépensé ne serait jamais compté. Un lot par événement source rétablit
exactement le comportement d'aujourd'hui : **un garde, une fois, pour tout ce que l'événement a
produit.**

Deux effets secondaires, tous deux bons : le surcoût passe de **+1 enveloppe par action à +1 par
événement traduit** (les vingt badges d'un `BadgesEquippedEvent` voyagent ensemble), et un
consommateur voit d'un coup tout ce que l'acte a produit au lieu de le reconstituer.

Le traducteur dit d'où vient l'identité, parce que lui seul sait quel champ de son événement la
porte :

```csharp
public interface ISignalTranslator<TEvent> where TEvent : IEvent
{
    static abstract ImmutableArray<SignalShape> Shapes { get; }
    ImmutableArray<ProgressSignal> Translate(TEvent e);

    /// <summary>Vide par défaut : la plupart des événements ne sont pas rejouables.</summary>
    string DeliveryIdOf(TEvent e) => string.Empty;
}
```

Un seul traducteur le surcharge aujourd'hui, celui de l'achat catalogue.

### 3.2 Les valeurs

**Les valeurs restent des chaînes.** C'est déjà le cas (`RewardTrackFactSnapshot`), c'est ce que la
capture inter-étapes sérialise sur la ligne du joueur, et un `object` typé forcerait chaque
consommateur à connaître le type de chaque fait. Le **type** vit dans le registre (§5), pas dans la
valeur : il sert à l'éditeur, pas au moteur.

**Un identifiant absent est omis, jamais émis à `0`.** `HabbiconUsedEvent.RoomId` vaut 0 en
conversation privée, une catégorie absente vaut 0 : le traducteur n'émet alors pas le fait. Un
filtre sur un fait absent échoue en fermé (§6), donc « n'importe quel appart sauf le 12 » ne matche
pas une conversation privée — ce que « room = 0 » aurait cassé.

**La règle s'arrête aux identifiants et aux optionnels.** Un `FactKind.Number` a le droit de valoir
zéro : « a reçu 0 respect au total » est une valeur, pas une absence. Le constructeur de faits le
dit dans ses noms — `IdIfAny(...)` omet le zéro, `Number(...)` l'écrit.

`SignalActions` reprend les constantes de `RewardTrackActions` **avec les mêmes chaînes** : du contenu
les nomme en base.

### 3.3 `Target` : un champ, et un fait ajouté au même endroit pour tout le monde

`Target` est déjà un paramètre de premier rang de `IPlayerRewardTrackGrain.ProgressAsync`, et il y
fait **deux choses qu'aucun fait ne fait** :

1. c'est ce que le `Parameter` d'une tâche compare — le mécanisme des tâches d'avant les séquences,
   qui continue de fonctionner sans être réécrit ;
2. c'est **la clé de déduplication du mode distinct** : « visiter 20 apparts différents » compte 20
   `Target` distincts.

Mais il est **aussi** un fait aujourd'hui : `RewardTrackSignal.SendAsync` fait
`if (target is not null) facts = facts.Insert(0, new(RewardTrackFacts.Target, target));`, et
`RewardTrackActionFacts` liste `Target` pour douze actions. Du contenu filtre donc dessus, et
l'éditeur le propose sous le libellé « Cible ».

> **Cette insertion est centralisée, jamais recopiée dans les traducteurs.** Un traducteur renseigne
> `Target` et rien d'autre ; l'hôte (§4.5) ajoute le fait `target` avant de publier, exactement où
> `SendAsync` le fait aujourd'hui. Laisser 21 traducteurs s'en souvenir, c'est écrire la prochaine
> dérive du §1.3 — et la première version de cette spec l'avait déjà oubliée dans son propre exemple.

Comme la cible change de nature selon l'action — un id d'appart ici, un joueur là, une offre pour le
catalogue, un code de badge pour un badge — **`SignalShape` porte un `TargetKind`** :

```csharp
public sealed record SignalShape(
    string Action,
    ImmutableArray<FactKey> Facts,
    FactKind? TargetKind = null   // null = cette action n'a pas de cible
);
```

Le vocabulaire expose alors `target` comme un **fait virtuel typé par action** : picker d'apparts sur
`create_room`, picker de joueurs sur `give_respect`, picker d'offres sur `buy_from_catalogue`. Ce
qui répond au passage à la question restée ouverte deux tours plus tôt — pourquoi « Cible » n'avait
pas de picker : parce que son type n'était écrit nulle part.

---

## 4. Le traducteur

### 4.1 Forme

```csharp
public sealed class RoomCreatedTranslator : ISignalTranslator<RoomCreatedEvent>
{
    // Une forme par action produite -- l'achat catalogue en déclare deux.
    // TargetKind dit ce qu'est la cible ; le fait "target" est ajouté par l'hôte (§3.3).
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(SignalActions.CreateRoom,
            [Facts.Room, Facts.RoomName, Facts.RoomDescription, Facts.Category, Facts.Model],
            TargetKind: FactKind.RoomId),
    ];

    public ImmutableArray<ProgressSignal> Translate(RoomCreatedEvent e) =>
        [new(e.OwnerId.Value, SignalActions.CreateRoom, 1,
            Target: Id(e.RoomId),
            Facts: SignalFacts.Build()
                .Id(Facts.Room, e.RoomId)
                .Text(Facts.RoomName, e.Name)
                .Text(Facts.RoomDescription, e.Description)
                .IdIfAny(Facts.Category, e.CategoryId)      // omis quand 0 (§3.2)
                .Text(Facts.Model, e.ModelName))];
}
```

**Aucun constructeur, aucune dépendance.** Un traducteur est une fonction pure et rien d'autre. Le
`ISignalTranslator<TEvent>` exige `Shapes` comme membre **statique abstrait** d'interface (C# 11) :
oublier de le déclarer est une erreur de compilation, pas une découverte en production.

**`Translate` renvoie zéro, un ou plusieurs signaux** — pas un seul. Ce n'est pas de la
généralisation gratuite, c'est ce que les handlers actuels font déjà (§1.4) :

| Handler | Ce qu'il émet |
| --- | --- |
| Achat catalogue | **deux actions**, `buy_from_catalogue` puis `spend_credits` si `CreditCost > 0` |
| Échange conclu | **deux joueurs**, chacun avec l'autre comme fait `player` |
| Badges équipés | **un signal par badge** de la liste |
| Meuble déplacé | `move_item` **toujours**, plus `rotate_item` quand `RotatedInPlace` — un seul traducteur remplace les deux handlers |
| Geste | `dance` **ou** `wave`, et rien pour un geste inconnu |
| Chat | rien pour un chuchotement |

Une signature à un seul retour aurait forcé ces six-là à rester des handlers écrits à la main,
c'est-à-dire hors du test générique — exactement la population où les dérives se sont produites.

Le tableau vide remplace les `if (...) return;` en tête des handlers actuels. Chaque `Shape` déclare
**l'union** des faits que les signaux de cette action peuvent porter, et le test §10.1 exige que
chaque clé annoncée soit couverte par au moins un signal produit.

### 4.2 Deux propriétés qui justifient tout le reste

1. **`Shapes` est la seule source de vérité.** Le dashboard et le validateur les lisent, plus une
   carte parallèle. Un fait proposé dans l'éditeur mais jamais émis devient impossible à écrire.
2. **`Translate` est une fonction pure.** Un test générique parcourt tous les traducteurs, leur
   donne un événement fabriqué et vérifie que chaque clé annoncée sort réellement — **un seul test
   pour les 137 événements** (§10.1). C'est précisément ce que la forme actuelle rendait impossible.

### 4.3 La règle de richesse

> Un traducteur ne publie que ce que l'événement **porte déjà**.

Enrichir se fait **au point de publication**, là où la donnée est en main. `RoomCreatedEvent` en est
l'exemple : nom, description, catégorie et modèle ont coûté trois lignes dans `RoomService.Create`
parce que l'entité était sous la main.

Si un fait exigerait une lecture base ou un appel de grain, **il n'entre pas dans le vocabulaire**.
C'est déjà le raisonnement écrit pour `room_owner`, absent de l'entrée d'appart parce que
`PlayerEnteredRoomEvent` ne le porte pas et que le lire coûterait un appel de grain sur un chemin
d'arrivée qui a déjà été lent. La règle devient générale, et un fait écarté pour cette raison est
**documenté comme écarté**, jamais déclaré.

Corollaire : `RespectReceivedEvent` porte `RespectTotal`, `PetLeveledUpEvent` porte `RoomId` — ce
qui est déjà là est gratuit et se déclare ; ce qui n'y est pas se plombe d'abord à la source.

### 4.4 La porte d'intérêt — à ne surtout pas perdre en route

Publier le signal inconditionnellement supprimerait la porte du §1.2 et paierait une enveloppe par
action pour toujours — y compris une fois par case marchée par chaque avatar du hall. **L'hôte de
traducteur (§4.5) la reprend, élargie, et l'évalue avant tout le reste** : avant `Translate`, avant
la moindre allocation.

```csharp
public interface ISignalInterest { bool AnyConsumerCares(string action); }

/// <summary>Une source d'intérêt. Un singleton DI, PAS le consommateur lui-même.</summary>
public interface ISignalInterestSource { ImmutableHashSet<string> Actions { get; } }
```

> **L'intérêt n'est pas porté par le handler.** `EnvelopeFeatureProcessor` ne fait pas des handlers
> des services : il construit un activateur et l'enregistre dans le registre, et l'instance est
> créée puis jetée à chaque invocation (§1.6). Faire implémenter `ISignalInterestSource` par
> `RewardTrackSignalConsumer` ne rendrait donc **aucune instance** joignable par `ISignalInterest` —
> la v3 le supposait, et c'était faux.

Chaque système enregistre un **singleton dédié** — `RewardTrackSignalInterest`,
`DailyTaskSignalInterest`, `AchievementSignalInterest` — dans son propre module, à côté de ses autres
services. `SignalInterest` reçoit l'`IEnumerable<ISignalInterestSource>` par injection et interroge
leurs ensembles vivants, sans union mise en cache : trois recherches de hash, et aucun problème
d'invalidation. Le consommateur et sa source d'intérêt lisent le même index ; ils ne sont simplement
pas le même objet.

Ce que chacun expose : les reward tracks ont déjà l'ensemble (`_index.Actions`, remplacé
atomiquement par `ReloadAsync` aux quatre sites d'écriture de `RewardTrackAdminService`) — la source
le relit à chaque appel, donc un rechargement de contenu est vu immédiatement. Les succès ont un
ensemble **statique** : les huit actions de leur table (§7.2). Les tâches quotidiennes n'ont **aucun
index de contenu aujourd'hui** (§1.2) : l'étape ② leur en donne un, construit sur les
`quest_type_code` des tâches publiées, ou à défaut l'ensemble statique de leur table.

La porte se ferme sur l'**action**, et un traducteur peut en produire plusieurs : elle est évaluée
sur l'union des actions de ses `Shapes`. Le tri fin — « cette action-ci n'intéresse personne » —
reste au consommateur, où il est déjà (`IsActionInteresting` avant l'appel de grain, et
`TasksFor` dans le grain lui-même).

> Sans cette porte, la conception est une régression de performance. Avec elle, elle en est
> l'inverse : deux consommateurs qui n'en avaient pas en héritent.

### 4.5 Découverte et hébergement — un processeur, pas de magie

Le traducteur n'est pas un handler ; il n'a ni constructeur ni dépendance. Ce qui le branche au
pipeline est un `SignalTranslatorFeatureProcessor : IAssemblyFeatureProcessor`, enregistré à côté
d'`EventFeatureProcessor` et tourné par le même `AssemblyProcessor` — donc sur l'hôte **et sur
chaque plugin**, avec le même `IDisposable` de retrait au déchargement.

Pour chaque type public concret implémentant `ISignalTranslator<TEvent>`, il :

1. lit `Shapes` (statique) par réflexion, **valide** ses clés (§8.1) et l'enregistre dans le
   vocabulaire (§5) ;
2. instancie le traducteur **une fois** (`Activator.CreateInstance`, constructeur sans paramètre —
   un traducteur avec un constructeur à paramètres est refusé avec un message clair, pas ignoré) ;
3. construit `SignalTranslatorHost<TEvent>(traducteur, IEventPublisher, ISignalInterest)` **une
   fois**, et l'enregistre via `EnvelopeHost.RegisterHandler(typeof(TEvent), sp, _ => host,
   invoker)` — les deux API sont publiques et `EnvelopeInvokerFactory.CreateHandlerInvoker` résout
   `HandleAsync` sur le type de l'hôte, qui implémente `IEventHandler<TEvent>` ;
4. rend un disposable qui retire **le handler et les formes** ensemble.

`SignalTranslatorHost<TEvent>.HandleAsync` fait, dans cet ordre :

1. **la porte d'intérêt** sur l'union des actions des `Shapes` — avant toute allocation ;
2. `Translate` ;
3. pour chaque signal, **l'insertion du fait `target`** quand `Target` est non nul (§3.3) ;
4. `DeliveryIdOf`, puis **une** publication de `ProgressSignalsRaised` avec le lot.

C'est le seul code non trivial du projet, et il est écrit une fois. Les points 1 et 3 sont les deux
règles que la v3 confiait implicitement aux 21 traducteurs, c'est-à-dire à leur mémoire.

Deux conséquences mesurables : **l'hôte est un singleton**, donc les traducteurs ne coûtent aucune
activation par événement — là où chaque handler actuel est instancié et disposé à chaque
invocation (§1.6) ; et **le provider de plugin n'entre jamais en jeu** pour un traducteur. Le
`CompositeServiceProvider(plugin, hôte)` du §1.6 n'existe qu'à l'intérieur d'`InvokeOneAsync` : ce
qu'un processeur résout **au moment de l'enregistrement** depuis le provider du plugin ne voit pas
les services de l'hôte. L'hôte singleton, construit par le processeur avec les services de l'hôte
qu'il a reçus par injection, contourne la question — c'est le même piège que celui qui a déjà fait
tomber le dashboard entier au démarrage, fermé avant d'exister.

---

## 5. Le registre des faits

### 5.1 Un fait est typé

```csharp
public sealed record FactKey(string Key, FactKind Kind, string LabelKey);

public enum FactKind
{
    Text,           // nom, description, mission
    Number,         // un compte, un niveau, un total
    PlayerId,       // → picker joueur
    RoomId,         // → picker appart
    FurnitureId,    // → picker mobilier + sprite
    CategoryId,     // → liste des catégories navigateur
    BadgeCode,      // → picker badge
    OfferId,        // → picker offre catalogue
    Enum,           // valeurs fermées : sol/mur
    OpaqueId,       // un id vivant : objet posé, familier. Saisie libre, aucun annuaire.
}
```

`Facts` déclare les clés du cœur avec **les chaînes actuelles** (`room`, `def`, `item`, `kind`,
`player`, `offer`, `habbicon`, `collection`, `pet`, `badge`, `name`, `desc`, `category`, `model`).

### 5.2 Ce que le type décide

| `FactKind` | Contrôle dans l'éditeur | Opérateurs proposés |
| --- | --- | --- |
| `Text` | champ texte | `Contains`, `Equals`, `NotEquals` |
| `Number` | champ nombre | `Equals`, `NotEquals` |
| `PlayerId` / `RoomId` / `FurnitureId` / `OfferId` / `BadgeCode` | **picker** + libellé et sprite | `Equals`, `NotEquals`, `OneOf` |
| `CategoryId` | liste des catégories | `Equals`, `NotEquals`, `OneOf` |
| `Enum` | select des valeurs déclarées | `Equals`, `NotEquals` |
| `OpaqueId` | champ texte + référence `$N` | `Equals`, `NotEquals` |

C'est exactement ce qui est aujourd'hui câblé en dur dans `RewardTracksPage.svelte` (`PICKER_KINDS`,
le select sol/mur). Ça remonte dans le contrat, donc **tâches quotidiennes et succès héritent des
pickers sans écrire une ligne** le jour où ils gagnent des filtres.

### 5.3 Gouvernance : additif seulement

Les clés de faits **et les codes d'action** sont stockés en chaînes dans le contenu :
`RewardTrackStepFilterEntity.FactKey` pour les unes, la colonne d'action des tâches pour les autres,
et les tables équivalentes des futurs consommateurs.

> **On ajoute des faits et des actions, on n'en renomme jamais, on n'en supprime jamais.** Ce qui
> n'a plus de sens est marqué `[Obsolete]`, disparaît de l'éditeur, et continue d'être évalué pour
> le contenu qui l'utilise déjà.

La règle vaut pour **les deux** vocabulaires. La v3 ne verrouillait que les faits, alors qu'elle
écrivait elle-même que les chaînes d'actions sont en base : un `create_room` renommé casserait
autant de contenu qu'un `room` renommé. Le test §10.3 fige les deux listes.

---

## 6. Les opérateurs

`Equals`, `NotEquals`, `OneOf` existent ; `Contains = 3` est ajouté et déjà commité :

```
Contains   // sous-chaîne, insensible à la casse, comparaison ordinale
```

Sans lui, `Text` est inutilisable : une égalité exacte sur un nom d'appart libre est un filtre qui
ne se déclenche jamais. Comparaison **ordinale insensible à la casse**, pas culturelle : le même
contenu doit matcher les mêmes noms sur tous les silos, quelle que soit la locale du processus.

Le tableau §5.2 est appliqué **des deux côtés** : l'éditeur ne propose que les opérateurs autorisés,
le validateur refuse les autres. Un `Contains` sur un `RoomId` est une erreur de contenu, pas un
filtre exotique.

**Sémantique inchangée et volontairement conservée** : un filtre sur un fait que le signal ne porte
pas échoue en fermé. Y compris `NotEquals` — « n'importe quel appart sauf le tien » est une
affirmation sur un appart, et une action qui n'en nomme aucun ne l'a pas rendue vraie. C'est aussi
la parade au piège classique du modèle canonique : un consommateur qui ignore un fait nouveau ne se
met pas à matcher n'importe quoi.

L'évaluateur est `TaskProgressRules.StepMatches`, déplacé tel quel dans Primitives : logique pure,
sans dépendance.

---

## 7. Les consommateurs

### 7.1 Forme et invariant

```csharp
public sealed class RewardTrackSignalConsumer(IGrainFactory grains, IRewardTrackCatalog catalog, ICommerceJournal journal)
    : IEventHandler<ProgressSignalsRaised>
{
    public async ValueTask HandleAsync(ProgressSignalsRaised e, EventContext ctx, CancellationToken ct)
    {
        // Trier d'abord : le garde de rejeu ne doit pas être dépensé pour un lot qui ne
        // m'intéresse pas, et il ne doit être dépensé qu'UNE fois pour celui qui m'intéresse.
        var mine = e.Signals.Where(s => s.PlayerId > 0 && catalog.IsActionInteresting(s.Action));
        if (!mine.Any()) return;

        if (!await CommerceReplayGuard.FirstDeliveryAsync(journal, e.DeliveryId, "reward-track", ct))
            return;

        foreach (ProgressSignal s in mine)
            await grains.GetPlayerRewardTrackGrain(s.PlayerId)
                .ProgressAsync(s.Action, s.Amount, s.Target, s.Facts.ToSnapshots(), ct);
    }
}
```

Un consommateur par système, découvert et isolé par le registre existant comme n'importe quel
handler. **L'ordre compte** : filtrer, puis garder, puis traiter. Garder avant de filtrer
consommerait le reçu d'un lot dont aucun signal ne concerne ce consommateur, et une republication
ultérieure — après qu'un opérateur a publié du contenu qui, lui, s'y intéresse — serait rejetée.

> **Invariant : un consommateur fait exactement les mêmes appels de grain, avec les mêmes attributs,
> que les handlers qu'il remplace.** `IPlayerDailyTaskGrain.ProgressAsync` et
> `IPlayerAchievementGrain.ProgressAsync` sont `[OneWay]` pour une raison de réentrance documentée
> (« le grain se bloquerait derrière son propre événement ») ; `IPlayerRewardTrackGrain.ProgressAsync`
> ne l'est pas. Rien de tout ça ne bouge. Le traducteur, lui, **n'appelle aucun grain**.

### 7.2 Correspondance des vocabulaires

Tâches quotidiennes et succès **gardent leurs codes**. Chacun tient sa table `action → son code`,
lue une fois. Relevé exact de ce que leurs 15 handlers font aujourd'hui :

**Tâches quotidiennes** (`DailyTaskSignalConsumer`, garde de rejeu `"daily-task"`) :

| Action | `QuestTypes.*` | Note |
| --- | --- | --- |
| `enter_other_users_room` | `RoomEntry` | |
| `change_figure` | `AvatarLooks` | |
| `change_motto` | `MottoChange` | |
| `give_respect` | `RespectGiven` | |
| `friend_added` | `FriendListSize` | **les deux joueurs** — le traducteur émet deux signaux |
| `place_item` | `PlaceItem` | |
| `buy_from_catalogue` | `CatalogPurchase` | |

**Succès** (`AchievementSignalConsumer`) :

| Action | `AchievementNames.*` | Note |
| --- | --- | --- |
| `login` | `Login` | **`ProgressDailyAsync`**, au plus une fois par jour civil — la table porte un drapeau `daily` |
| `enter_other_users_room` | `RoomEntry` | |
| `change_motto` | `Motto` | |
| `change_figure` | `AvatarLooks` | |
| `friend_added` | `FriendListSize` | les deux joueurs, comme ci-dessus |
| `place_item` | `RoomDecoFurniCount` | |
| `give_respect` | `RespectGiven` | |
| `receive_respect` | `RespectEarned` | le `PlayerId` du signal est **celui qui reçoit** |

Trois de ces actions n'ont **pas de traducteur aujourd'hui** : `friend_added`
(`FriendRequestAcceptedEvent`, deux signaux), `login` (`PlayerLoggedInEvent`) et `receive_respect`
(`RespectReceivedEvent`, avec `RespectTotal` gratuit comme fait `Number`). L'étape ② les écrit — ce
sont trois fichiers de dix lignes, mais ils **manquaient** à la v2 de cette spec.

Ce que ② ne touche pas : `IPlayerQuestGrain.ProgressAsync` est aussi appelé **directement depuis
des handlers de paquets** (`ChatMessageHandler`, `DanceMessageHandler`,
`AvatarExpressionMessageHandler`, `FriendRequestQuestCompleteMessageHandler`) — un quatrième
chemin, hors événements. Il reste tel quel ; le ramener sur les signaux est une étape ④ possible, et
elle règlerait au passage une entorse à « les handlers de paquets orchestrent seulement ».

**Règle anti-boucle** : les reward tracks traduisent `QuestCompletedEvent` et
`AchievementLevelUpEvent`, que produisent les deux futurs consommateurs. Aucune table ne doit
abonner un consommateur à une action produite par ses propres événements. Un test le vérifie sur
les tables (§10.2).

### 7.3 Ce que le garde de rejeu garantit — et ce qu'il ne garantit pas

Le garde reste **par consommateur**, avec la clé qu'il a aujourd'hui, et il est appelé **une fois
par lot** (§3.1). Le traducteur ne déduplique jamais — il est pur, il n'a pas de journal.

> **Le reçu est un garde contre la republication, pas un accusé de traitement.** La v3 affirmait
> qu'« un consommateur qui a échoué doit revoir la livraison ». C'est faux, et ça l'est déjà
> aujourd'hui : `FirstDeliveryAsync` écrit le reçu **avant** l'appel métier, et
> `EnvelopeHost.InvokeOneAsync` capture l'exception du handler **sans la relancer**. Un consommateur
> peut donc faire reçu ✓ → `ProgressAsync` ✗ et ne jamais rien revoir. Pire, `CommerceRelayService`
> fait `PublishAsync(rebuilt)` puis `MarkRelayedAsync` : l'opération est marquée relayée même si un
> consommateur a échoué à l'intérieur.

Ce que la clé par consommateur garantit réellement, et qui reste indispensable : **le reçu d'un
consommateur ne supprime pas la première livraison d'un autre.** Sans elle, le premier des trois à
traiter le lot les priverait tous les deux.

Ce que ça ne garantit pas : la reprise après échec. Une vraie garantie demanderait une boîte de
réception idempotente au niveau du grain consommateur — écrire le reçu et appliquer la progression
dans la même transaction. C'est un sujet séparé, il préexiste à cette refonte, et l'étape ① le
**conserve tel quel** plutôt que de mélanger une migration sans risque observable avec un changement
de sémantique de livraison.

Un `DeliveryId` vide fait passer le garde sans rien écrire (§1.5), ce qui est le cas de tous les
événements sauf l'achat catalogue.

### 7.4 Le wired n'est pas un consommateur

Le wired est évalué **en direct dans le grain de l'appart**, sur l'état vivant de la pièce, pas sur
un flux d'événements. Il prend **la grammaire** (le registre de faits, les opérateurs, l'évaluateur,
les contrôles de l'éditeur) et fournit ses faits lui-même depuis l'état du grain. Le faire consommer
le bus signifierait faire transiter chaque pas d'un avatar par le pipeline d'événements : c'est non.

Le sens inverse existe déjà et ne change pas : l'action wired `PROGRESS_REWARD_TRACK` appelle
`IPlayerRewardTrackGrain.ProgressTaskAsync`, qui nomme une piste et une tâche et **contourne
délibérément l'index d'actions**. Le wired écrit dans les reward tracks sans passer par un signal,
parce qu'il ne décrit pas quelque chose que le joueur a fait — il ordonne une progression.

### 7.5 Ordre, concurrence, et où ça tourne

Le registre dispatche en parallèle (`Task.WhenAll`), donc deux signaux d'un même joueur peuvent être
traités dans le désordre. Ce n'est pas nouveau : c'est déjà vrai des trois familles d'aujourd'hui.
**Le point de sérialisation est le grain du joueur** — Orleans exécute ses appels un à la fois, et
c'est là que la capture inter-étapes (`StepCaptures`) est lue puis réécrite.

Une séquence multi-étapes est donc sûre au sens où deux signaux ne s'écrasent pas, mais **l'ordre de
deux actions quasi simultanées n'est pas garanti**. « Pose un canapé puis marche dessus » en deux
dixièmes de seconde peut échouer à s'ordonner. Comportement actuel, conservé, et écrit ici pour que
personne ne découvre plus tard que la conception l'avait promis autrement.

Où ça tourne : dans les seuls grains d'appart et de joueur, **62 sites attendent `PublishAsync`**
dans le tour du grain appelant, contre **7 `PublishDetached`** dans tout le dépôt — dont le pas
sur un meuble. La chaîne
traducteur → publication imbriquée → consommateurs s'exécute donc **dans le tour du grain
publiant** pour la majorité des événements, comme les handlers d'aujourd'hui. Les consommateurs y
font les mêmes appels `[OneWay]` qu'avant (§7.1) ; le traducteur y ajoute une fonction pure et une
enveloppe.

---

## 8. Où vit le code

| Projet | Contenu | Dépend de |
| --- | --- | --- |
| `Vortex.Primitives/Signals/` | `ProgressSignal`, `ProgressSignalsRaised`, `SignalActions`, `Facts`, `FactKind`, `SignalShape`, `FilterOperator`, l'évaluateur, les règles de validation, **`ISignalVocabulary`**, `ISignalInterest`, `ISignalInterestSource`, `ISignalTranslator<T>` | rien |
| **`Vortex.Signals`** *(nouveau)* | les 21 (+3) traducteurs, `SignalTranslatorHost<T>`, `SignalTranslatorFeatureProcessor`, `SignalVocabulary` (implémente `ISignalVocabulary`) | Primitives, Events, Pipeline, Runtime |
| `Vortex.RewardTracks` | son consommateur ; `RewardTrackSequenceRules` reçoit un `ISignalVocabulary` en paramètre au lieu de lire une carte statique | Primitives |
| `Vortex.Progression` | ses deux consommateurs + les deux tables | Primitives |
| `Vortex.Dashboard.API` | sert `ISignalVocabulary.Shapes` à l'éditeur | Primitives |

**Aucun cycle, et personne ne référence `Vortex.Signals`** : consommateurs, validateur et dashboard
ne connaissent que l'interface dans Primitives, servie par injection. C'est ce qui corrige la v2,
où le validateur (dans RewardTracks) aurait dû lire un registre vivant dans un projet qu'il n'a
pas le droit de référencer. Le nouveau projet doit être référencé par `Vortex.Main` pour que le
balayage d'assembly le voie, et rangé dans le dossier de solution qui va bien (commit `14226133b`).

### 8.1 Un plugin peut ajouter un traducteur

`PluginManager` fait tourner **le même `AssemblyProcessor`** que l'hôte sur les assemblys de
plugins (§1.6). Le processeur du §4.5 y découvre donc les traducteurs d'un plugin exactement comme
ceux du cœur, **sans que le cœur soit modifié** — et retire leurs formes et leurs handlers au
déchargement, par le même disposable. C'est l'argument le plus fort en faveur de cette forme : la
modularité n'est pas à construire, elle est héritée du chargeur de plugins.

Deux règles en découlent :

1. **Espace de noms — validé, jamais réécrit.** Un plugin écrit ses clés **déjà qualifiées**
   (`acme:trophy`) dans ses `Shapes` comme dans ce que `Translate` retourne ; le processeur
   **vérifie** le préfixe au chargement contre la clé du `PluginManifest` et refuse l'assembly
   sinon. Il ne réécrit rien.

   > Réécrire au chargement était une incohérence de la v3 : le vocabulaire aurait porté
   > `acme:trophy` pendant que l'hôte publiait le `trophy` brut rendu par `Translate`, et les
   > filtres n'auraient jamais matché. Valider plutôt que réécrire supprime la question — et laisse
   > un plugin **réutiliser les faits du cœur** (`room`, `player`) sans qu'ils deviennent
   > accidentellement `acme:room`. Un plugin peut donc dire « dans tel appart » avec le même
   > vocabulaire que tout le monde.

   Une clé non préfixée qui n'existe pas dans le cœur est refusée ; une collision entre deux plugins
   est impossible par construction, puisque le préfixe est la clé du plugin.
2. **La règle « on ne renomme jamais » (§5.3) vaut aussi pour eux**, mais le dépôt ne peut pas la
   verrouiller par un test. Elle est écrite dans le contrat du plugin : un plugin qui renomme une de
   ses clés casse le contenu écrit dessus, et c'est à lui d'assumer sa migration.

Les libellés (`LabelKey`) d'un plugin ne peuvent pas entrer dans les locales du dashboard : la
`FactKey` porte un `Label` de repli, affiché tel quel.

---

## 9. Le dashboard devient générique

`RewardTrackActionOptions` renvoie aujourd'hui `{ name, wired, facts: string[] }`, où `facts` sort
de la carte manuelle et `wired` d'un `HashSet` tenu à la main. Il renverra `ISignalVocabulary`,
faits **typés** :

```json
{ "name": "create_room", "hasProducer": true,
  "facts": [
    { "key": "room",     "kind": "RoomId",     "labelKey": "facts.room" },
    { "key": "name",     "kind": "Text",       "labelKey": "facts.roomName" },
    { "key": "category", "kind": "CategoryId", "labelKey": "facts.category" } ] }
```

Le composant d'édition de filtres devient un composant à part, piloté par `kind` : c'est lui qui
choisit picker, select ou champ texte, et la liste d'opérateurs. Les éditeurs de tâches et de succès
le réutilisent tels quels le jour où ils gagnent des filtres.

Le drapeau « cette action a-t-elle un producteur ? » **reste**, et devient exact au lieu d'être une
liste tenue à la main : le vocabulaire sait quelles actions ont un traducteur. Il ne peut pas
disparaître : `SignalActions` reste une **liste déclarée**, pas la projection des traducteurs. Sur
les 26 actions déclarées, 24 ont un producteur et **deux n'en ont pas** (`teleport`, `wired`), et du
contenu peut déjà les nommer. Les dériver des traducteurs les ferait disparaître de l'éditeur sans
prévenir, et une tâche écrite dessus deviendrait invisible au lieu d'être signalée inerte.

---

## 10. Tests

### 10.1 Le test qui manquait — en deux moitiés, parce qu'une seule ne suffit pas

Une fixture générique **ne peut pas** prouver toutes les formes, et la spec en portait elle-même les
contre-exemples : le chat exige `Whisper == false`, le geste exige `Gesture == "dance"` ou
`"wave"` exactement, le déplacement exige `RotatedInPlace` dans ses **deux** états, l'achat exige
`CreditCost > 0` pour émettre sa seconde action. Un générateur qui sait produire « une chaîne non
vide » ne devinera jamais `"dance"`. Prétendre le contraire, c'était écrire un test vert qui ne
prouve rien — le défaut même qu'il est censé empêcher.

**Moitié générique — invariants structurels, sur tous les traducteurs.** La fixture fabrique
l'événement par réflexion sur son constructeur positionnel, avec des valeurs non nulles et non
vides, et le test exige :

1. aucun signal ne porte une clé **non déclarée** dans les `Shapes` ;
2. chaque `Action` produite est déclarée ;
3. aucune valeur de fait n'est nulle ou vide ;
4. `PlayerId > 0` sur chaque signal produit.

Ces quatre-là valent même quand `Translate` rend un tableau vide, ce qui est le cas normal pour un
traducteur branché mal deviné.

**Moitié explicite — une matrice de cas, pour les traducteurs qui branchent.** Un cas nommé par
chemin, avec l'événement écrit à la main, qui exige que **chaque clé déclarée de la forme visée soit
présente et non vide** :

| Traducteur | Cas |
| --- | --- |
| Chat | ligne normale → signal ; chuchotement → rien |
| Geste | `"dance"` → `dance` ; `"wave"` → `wave` ; `"cough"` → rien |
| Meuble déplacé | déplacement → `move_item` seul ; rotation → `move_item` **et** `rotate_item` |
| Achat catalogue | `CreditCost > 0` → deux signaux ; `CreditCost == 0` → un seul ; `DeliveryIdOf` rend l'`OperationId` |
| Badges | trois codes → trois signaux ; liste vide → rien |
| Échange | deux signaux, chacun avec **l'autre** joueur en fait `player` |
| Habbicon | en appart → fait `room` ; en conversation privée (`RoomId == 0`) → **pas** de fait `room` |
| Amitié acceptée | deux signaux, un par joueur |
| Niveau de familier | `Amount` == le niveau, pas 1 ; le crédit va au propriétaire |

**Et le filet qui rend la matrice fiable** : un test de couverture exige que **chaque `Shape`
déclarée soit couverte par au moins un cas** — explicite ou générique. Un traducteur branché ajouté
sans son cas fait échouer la suite au lieu de rester silencieusement non testé. Sans ce filet, la
matrice serait exactement le genre de liste tenue à la main qui a produit les neuf dérives du §1.3.

Aucun générateur de fixtures n'est dans le dépôt (ni AutoFixture, ni Bogus) : le constructeur est
maison, ~60 lignes, et couvre exactement les types que les 24 événements concernés utilisent —
`int`, `long`, `bool`, `string`, `string?`, `PlayerId`, `PlayerId?`, `RoomId`,
`ImmutableArray<string>`, `IReadOnlyList<int>`. Un type inconnu fait échouer le test avec son nom,
pas passer silencieusement.

### 10.2 L'évaluateur, les tables, la boucle

Les cas de `TaskProgressRulesTests` restent valides ; s'ajoutent `Contains` (casse, sous-chaîne,
chaîne vide), l'échec en fermé sur fait absent pour les quatre opérateurs, et le tableau des
opérateurs autorisés par `FactKind`.

Les deux tables de correspondance (§7.2) ont un test chacune : chaque action nommée existe dans
`SignalActions`, chaque code existe dans `QuestTypes` / `AchievementNames`, et **aucune action de
la table n'est produite par un événement du même système** (règle anti-boucle).

### 10.3 La gouvernance

Un test compare les `FactKey` déclarées à une liste de référence versionnée dans le dépôt et échoue
si une clé disparaît ou change de `Kind`. Ajouter une clé met la liste à jour ; en retirer une
demande de le vouloir explicitement.

### 10.4 Les chemins chauds

`Vortex.Benchmark` et `Vortex.LoadGen` mesurent déjà les arrivées. Une passe avant/après sur
l'entrée d'appart, dans les deux cas :

- **aucun contenu n'écoute** : la porte (§4.4) doit rendre ce cas au moins aussi rapide
  qu'aujourd'hui — et il devrait l'être davantage, puisque deux appels de grain inconditionnels
  disparaissent (§1.2) ;
- **du contenu écoute** : trois handlers activés hier, un hôte singleton plus une enveloppe et trois
  consommateurs activés demain.

On vérifie la mesure, on ne la suppose pas.

### 10.5 Observabilité

Le hall a déjà un endpoint `/metrics` Prometheus et un sink de regroupement d'erreurs. Le signal y
ajoute trois compteurs, parce qu'un système de progression qui n'avance pas est un bug qu'on
découvre par une plainte de joueur, jamais par une exception :

| Métrique | Ce qu'elle répond |
| --- | --- |
| `signals_raised_total{action}` | quelles actions arrivent réellement, et lesquelles n'arrivent jamais |
| `signals_gated_total{action}` | combien la porte d'intérêt a arrêté — la preuve qu'elle sert |
| `signal_consumer_failures_total{consumer}` | quel consommateur tombe, sans lire les journaux |

Le premier est aussi l'outil de couverture du §13 : une action déclarée dont le compteur reste à
zéro sur une semaine est soit du contenu que personne ne déclenche, soit un traducteur qui ne
traduit pas.

### 10.6 L'hébergement

Un test d'hébergement démarre le pipeline avec `Vortex.Signals` chargé et vérifie que le processeur
a enregistré au moins un traducteur et une forme — parce qu'un projet oublié dans `Vortex.Main`
est un système de progression entièrement muet, sans une seule exception.

---

## 11. Étapes de livraison

Chaque étape est livrable seule et laisse le système fonctionnel.

**① Le contrat, les traducteurs, un consommateur.**
`Vortex.Primitives/Signals`, `Vortex.Signals` avec les 21 traducteurs portés depuis
`RewardTrackEventHandlers` (dont un pour `ItemMovedEvent` à la place de deux handlers), le
processeur, le consommateur reward tracks, l'évaluateur et les règles déplacés, `ISignalVocabulary`
branché dans le validateur et le dashboard, les tests §10.1, §10.3, §10.6.
`RewardTrackEventHandlers.cs` et `RewardTrackActionFacts` sont supprimés. Aucun changement de
comportement observable ; les tests de contenu existants le prouvent.

**② Tâches quotidiennes et succès.**
Les trois traducteurs manquants (§7.2), les deux consommateurs, les deux tables et leurs tests, un
index d'intérêt pour les tâches quotidiennes, et la suppression de leurs 15 handlers. Les cinq
événements traduits trois fois ne le sont plus qu'une.

**③ La grammaire pour le wired et le vocabulaire élargi.**
Le composant de filtres partagé côté dashboard, le wired branché sur le registre de faits avec ses
propres sources, et l'élargissement de la couverture (§13).

### 11.1 Ce que ② n'apporte PAS

L'étape ② ne donne **aucune capacité nouvelle** aux tâches quotidiennes ni aux succès : ni filtres,
ni faits, ni séquences. C'est de la déduplication pure. Leur contenu, leurs tables et leurs écrans
sont inchangés, et un joueur ne voit rien — sauf, pour les tâches quotidiennes, une porte d'intérêt
qu'elles n'avaient pas.

C'est délibéré : leur donner des filtres en même temps mélangerait une migration sans risque
observable et une fonctionnalité neuve dans la même livraison. Les filtres pour les tâches, s'ils
sont voulus, sont une étape ④ — bon marché, puisque la grammaire et l'éditeur seront là.

### 11.2 Bascule et retour arrière

**Il ne faut pas faire tourner l'ancien handler et le nouveau consommateur en même temps.** Les deux
appelleraient `ProgressAsync` pour le même acte, et toute progression compterait double — sur des
compteurs cumulés persistés, donc sans rattrapage possible autrement qu'à la main en base. Un
« double run pour comparer » est le piège évident de cette migration ; il est explicitement exclu.

Le retour arrière est donc **un revert de commit**, pas un drapeau de configuration. Ce qui rend le
revert sûr :

- l'étape ① ne touche **aucun schéma de base** — mêmes tables, mêmes colonnes, mêmes chaînes
  d'actions et de faits. Ce sont les mêmes appels de grain, émis depuis un autre endroit ;
- elle est livrée **en une seule fois** (contrat, traducteurs, consommateur, suppression des anciens
  handlers) précisément pour qu'il n'existe jamais d'état intermédiaire où les deux tournent ;
- le contrôle après bascule est `signals_raised_total` (§10.5) comparé aux actions attendues : une
  action qui tombe à zéro dit immédiatement quel traducteur manque.

Un drapeau de configuration serait pire que le revert : il maintiendrait les deux chemins vivants,
donc le risque de double comptage, pour économiser un `git revert`.

### 11.3 Ordre de grandeur

Indicatif, pour arbitrer — pas un engagement.

| Étape | Fichiers | Le gros du travail |
| --- | --- | --- |
| ① | ~40 (dont 21 traducteurs mécaniques) | le processeur §4.5, le portage des 21 traductions, le test générique §10.1 |
| ② | ~15 | trois traducteurs, deux tables, un index d'intérêt, la suppression de 15 handlers |
| ③ | ~15 côté dashboard + wired | le composant de filtres partagé, puis la couverture §13 au fil de l'eau |

Les 21 traducteurs de ① sont du portage ligne à ligne d'un fichier lu en entier (§1.4) : c'est le
volume, pas la difficulté. La difficulté de ① est le processeur, la porte d'intérêt et le test
générique — trois choses qui n'existent pas encore et dont dépend tout le reste.

---

## 12. Ce qui est déjà commité

Fait avant cette conception, et qui **reste valable** — ce sont les faits et les correctifs, pas
l'architecture :

- `26f35920f` — l'éditeur de séquence en blocs, le tag premium, les cartes d'étapes, les pickers.
- `0c36265d5` — `RoomCreatedEvent` porte description, modèle et catégorie ; les faits `name`,
  `desc`, `category`, `model` ; les neuf dérives du §1.3 corrigées ; `Contains`.

À l'étape ①, ces traductions deviennent des traducteurs ; le travail n'est pas jeté, il est déplacé.

---

## 13. Couverture : les 115 événements restants

L'ordre est celui de l'utilité pour du contenu, pas de la facilité :

| Famille | Événements | Ce que ça débloque |
| --- | --- | --- |
| Pets | `PetLeveledUp` (déclarer `RoomId`, déjà porté), naissance, soins, dressage | « élever un familier », « faire éclore un plant » |
| Groupes | création, adhésion, forum | « fonder un groupe », « poster sur un forum » |
| Marketplace / échanges | mise en vente, vente conclue | « vendre trois meubles » |
| Collectibles / mystery box | frappe, ouverture, prix | « ouvrir une boîte », « frapper un objet » |
| Habbicons | déjà traduit | filtres par collection |
| Pêche, jeux, wired | prise, score, partie gagnée | tâches de mini-jeu |

Pour chacun : d'abord vérifier ce que l'événement **porte déjà**, puis enrichir au point de
publication si la donnée y est libre, et **écarter explicitement** ce qui coûterait une lecture
(§4.3).

---

## 14. Risques

| Risque | Parade |
| --- | --- |
| **La porte d'intérêt est oubliée** ou évaluée après `Translate` : une enveloppe par action, une fois par case marchée | §4.4 : première instruction de l'hôte, avant toute allocation. Mesure du cas « aucun contenu » en §10.4, `signals_gated_total` en production. |
| +1 enveloppe par **événement traduit** quand du contenu écoute (§3.1 : un lot, pas un par action) | En face : traduction et enrichissement passent de trois fois à une, l'hôte est un singleton là où trois handlers étaient instanciés, et deux appels de grain inconditionnels disparaissent. Mesuré avant de livrer ①. |
| Un consommateur dépense son reçu de rejeu sur un lot qui ne le concerne pas | Filtrer avant de garder, garder avant de traiter — l'ordre est écrit dans §7.1 et couvert par un test de la matrice §10.1 (achat catalogue). |
| L'ancien handler et le nouveau consommateur tournent ensemble : **toute progression compte double**, sur des compteurs persistés | Livraison en un seul commit, pas de drapeau, retour arrière par revert (§11.2). |
| Le modèle canonique devient un goulot qui force des déploiements en lock-step | Vocabulaire additif (§5.3) ; un consommateur ignore ce qu'il ne connaît pas ; un filtre sur un fait absent échoue en fermé (§6). |
| Un renommage de clé casse du contenu déjà écrit en base | Interdit et verrouillé par un test (§5.3, §10.3). |
| Une redélivrance saute un consommateur | Garde de rejeu par consommateur, `DeliveryId` porté par le signal (§7.3). |
| Un consommateur s'abonne à une action produite par ses propres événements | Règle anti-boucle, testée sur les tables (§7.2, §10.2). |
| Un traducteur qui plante coupe trois systèmes au lieu d'un | `Translate` est pure, sans E/S : elle ne plante que sur un bug de code, le registre isole l'exception, le test §10.1 la couvre. |
| Deux plugins déclarent la même clé | Préfixe obligatoire par clé de plugin, collision refusée au chargement (§8.1). |
| Le nouveau projet n'est pas chargé : plus aucun signal, aucune exception | Test d'hébergement (§10.6) ; `signals_raised_total` en production. |
| Une séquence multi-étapes échoue à s'ordonner sur deux actions quasi simultanées | Comportement actuel, conservé et écrit (§7.5). |
| Un traducteur non public compile et n'est jamais découvert | Déjà couvert par `WarnNonPublic` (§1.6) ; le test §10.1 découvre par le même chemin et échouerait sur un traducteur attendu et absent. |

---

## 15. Ce qui n'a pas été vérifié

Aucune source d'émulateur de référence n'est présente sur cette machine (à côté du dépôt il n'y a
que `vortex-modern-client`). Cette conception ne s'appuie donc sur **aucune** preuve tierce sur la
façon dont un autre émulateur Habbo déclenche ses quêtes — uniquement sur ce dépôt et sur les
patterns publics d'intégration (Message Translator, Canonical Data Model, Content Enricher, et la
gouvernance de taxonomie d'événements).

Non mesuré non plus : le coût réel de la publication imbriquée sur l'entrée d'appart. C'est ce que
§10.4 existe pour établir avant que ① soit livré.

---

## 16. Historique

- **v1** — première conception.
- **v2** — quatre trous comblés après relecture contre le code : `Translate` rend plusieurs signaux ;
  `Target` est un champ ; la porte d'intérêt ; les plugins. Ajoutés : ordre, observabilité, retour
  arrière, ce que ② n'apporte pas, tailles. Compte des traducteurs corrigé (21).
- **v3** — relecture ligne à ligne des 22 handlers de reward tracks et des 15 autres, et lecture de
  `Vortex.Pipeline` / `Vortex.Plugins`. Corrigé : la rotation émet `move_item` **et** `rotate_item`
  (la v2 disait « ou ») ; les badges sont un cas multi-signal de plus ; un fait sans valeur est omis.
  Décidé sur preuve : hôte singleton enregistré par un processeur d'assembly (les handlers sont
  instanciés à chaque invocation ; hors invocation, le provider d'un plugin ne voit pas les
  services de l'hôte) ;
  `ISignalVocabulary` dans Primitives parce que le validateur ne peut pas référencer `Vortex.Signals`.
  Découvert : les tâches et les succès n'ont aucune porte ; ② a besoin de trois traducteurs qui
  n'existaient pas ; `Login` est `ProgressDailyAsync` ; les quêtes ont un quatrième chemin depuis les
  handlers de paquets ; le garde de rejeu ne fait rien sur un id vide. Ajoutés : la règle
  anti-boucle, le test d'hébergement, la liste exacte des types du constructeur de fixtures.
- **v4** — six défauts relevés en revue, tous confirmés dans le code, tous corrigés.
  **Bloquant** : un `DeliveryId` par signal aurait fait rejeter le second signal d'un même
  événement — `TryRecordStepAsync` est unique par `(operation, stepKey)`, donc `spend_credits`
  n'aurait jamais compté. L'enveloppe devient un **lot** : `ProgressSignalsRaised(DeliveryId,
  Signals)`, un garde par événement source comme aujourd'hui, et le surcoût passe de +1 enveloppe
  par action à +1 par événement traduit. **Faux** : §7.3 promettait une reprise après échec ; le
  reçu est écrit avant l'appel métier, `InvokeOneAsync` avale l'exception et `CommerceRelayService`
  marque relayé après un publish qui ne remonte rien — c'est un garde contre la republication, et
  la garantie réelle (une inbox idempotente dans le grain) est hors périmètre. **Régression** :
  l'exemple perdait le fait `target`, que `SendAsync` insère automatiquement aujourd'hui ; il est
  désormais inséré par l'hôte, et `SignalShape` porte un `TargetKind` qui en fait un fait virtuel
  typé par action — ce qui lui donne enfin un picker. **Impossible** : une fixture générique ne peut
  pas deviner `Gesture == "dance"` ; le test se scinde en invariants structurels sur tous les
  traducteurs plus une matrice de cas explicites pour les branchants, avec un test de couverture qui
  refuse une `Shape` non couverte. **Non câblé** : l'intérêt ne peut pas être porté par le
  consommateur, les handlers ne sont pas des services DI — ce sont trois singletons dédiés.
  **Gouvernance** : les actions sont figées comme les faits ; « jamais zéro » ne vaut que pour les
  identifiants, un `Number` peut valoir 0 ; et un plugin écrit ses clés déjà qualifiées, le
  processeur **valide** le préfixe au lieu de réécrire — sinon le vocabulaire aurait porté
  `acme:trophy` pendant que l'hôte publiait `trophy`, et un plugin n'aurait pas pu réutiliser
  `room`.
