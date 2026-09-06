# Signaux de progression — conception

**Date** : 2026-09-06
**État** : proposée, non implémentée
**Périmètre** : reward tracks, quêtes/tâches quotidiennes, succès. Le wired reçoit la grammaire,
pas le bus (§7.4).

Un seul vocabulaire de faits, une seule traduction des événements du domaine, et trois systèmes de
progression qui s'y branchent au lieu d'en écrire chacun le sien.

---

## 1. Ce qui existe aujourd'hui

### 1.1 Trois traductions du même événement

`Vortex.Primitives/Events` déclare **137 événements de domaine**. Trois familles de handlers les
traduisent, chacune avec son vocabulaire :

| Famille | Fichier | Handlers | Vocabulaire |
| --- | --- | --- | --- |
| Reward tracks | `Vortex.RewardTracks/Events/RewardTrackEventHandlers.cs` | 22 | action + montant + `target` + **faits** |
| Quêtes / tâches quotidiennes | `Vortex.Progression/Quests/Events/DailyTaskProgressEventHandlers.cs` | 7 | `QuestTypes.*` (20 codes) + montant |
| Succès | `Vortex.Progression/Achievements/Events/AchievementProgressEventHandlers.cs` | 8 | `AchievementNames.*` (8 codes) + montant |

**Cinq événements sont traduits trois fois** — `PlayerEnteredRoomEvent`, `PlayerFigureChangedEvent`,
`PlayerMottoChangedEvent`, `ItemPlacedEvent`, `RespectGivenEvent` — et deux le sont deux fois
(`CatalogPurchasedEvent` : tracks + quêtes ; `FriendRequestAcceptedEvent` : quêtes + succès).
Dix-neuf handlers pour sept événements.

> **Conséquence mesurable** : enrichir un événement ne profite aujourd'hui qu'au système dont on a
> édité le handler. Les deux autres continuent d'ignorer la donnée qui vient d'arriver.

**115 des 137 événements n'alimentent aucun système de progression.** Le plafond n'a jamais été le
moteur de filtres, c'est la surface de traduction.

### 1.2 La carte des faits est recopiée à la main

`RewardTrackActionFacts` décrit, action par action, les faits que le handler correspondant émet.
L'éditeur du dashboard lit cette carte pour ne proposer que des filtres qui peuvent matcher, et le
validateur de contenu la lit pour refuser les autres (`filter_fact_not_emitted_by_action`).

Son propre commentaire affirme que `RewardTrackActionFactsTests` empêche les deux listes de diverger.
**Ce test n'existe pas** — aucun fichier du dépôt ne référence `RewardTrackActionFacts` hors du code
de production. Les deux listes avaient donc divergé sur **neuf actions** : la carte annonçait des
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

*(Corrigé dans l'arbre de travail, voir §12.)*

La raison de fond n'est pas la négligence : **la traduction est soudée à un handler Orleans**, qui
demande un `IGrainFactory` et un `IRewardTrackCatalog` pour être instancié. On ne peut pas
l'exécuter dans un test unitaire sans monter la moitié du silo — c'est pour ça que le test annoncé
n'a jamais été écrit.

### 1.3 Le garde de rejeu est par consommateur

`RewardTrackPurchaseHandler` appelle
`CommerceReplayGuard.FirstDeliveryAsync(journal, e.OperationId, "reward-track", ct)`. La clé porte
le **nom du consommateur**. C'est correct et il faut le préserver : le pipeline livre au moins une
fois, et un consommateur qui a échoué doit revoir la livraison même si les deux autres l'ont traitée.

### 1.4 Ce que le pipeline sait déjà faire

`EventFeatureProcessor` découvre les `IEventHandler<>` par balayage d'assembly. `EventRegistry`
dispatche **en parallèle**, isole les exceptions de chaque handler et les regroupe dans le sink
d'erreurs. Un handler qui tombe ne fait tomber ni l'action qui l'a causé, ni les autres handlers.

> **Il n'y a donc rien à construire côté infrastructure.** Le signal est un événement de plus.

---

## 2. Ce qu'on construit — et ce qu'on ne construit pas

**On construit :**

1. Un contrat de signal canonique, publié comme un `IEvent` ordinaire.
2. Des **traducteurs** : une classe par événement de domaine, fonction pure, auto-découverte, qui
   **se décrit elle-même**.
3. Un registre de faits **typés**, d'où l'éditeur déduit ses contrôles et ses opérateurs.
4. Un évaluateur et un validateur de filtres partagés.
5. Les consommateurs : reward tracks d'abord, puis quêtes et succès.
6. **La porte d'intérêt**, reprise et élargie — listée ici parce que l'oublier transformerait cette
   conception en régression de performance sur les trois chemins les plus chauds du hall (§4.4).

**On ne construit pas :**

- **Pas de nouveau bus.** Le pipeline d'événements existant suffit (§1.4).
- **Pas de réécriture du contenu existant.** Les codes `QuestTypes.*` et `AchievementNames.*`
  restent ; chaque consommateur garde sa table de correspondance code → action.
- **Pas de versionnage de schéma à l'exécution.** Le vocabulaire est additif (§5.3) ; un registre
  de schémas versionnés serait de la machinerie pour un problème qu'on s'interdit d'avoir.
- **Le wired n'est pas un consommateur du bus** (§7.4).
- **Pas d'enrichissement qui coûte une lecture.** Règle en §4.3.

---

## 3. Le contrat

```csharp
namespace Vortex.Primitives.Signals;

/// <summary>Ce qu'un joueur vient de faire, dit une seule fois pour tout le monde.</summary>
public sealed record ProgressSignal(
    long PlayerId,
    string Action,          // SignalActions.*
    int Amount,             // 1 pour un acte, N pour "a dépensé N crédits"
    string? Target,         // de quoi le signal parle principalement
    ImmutableArray<SignalFact> Facts,
    string DeliveryId       // vide si l'événement source n'est pas rejouable
);

public readonly record struct SignalFact(string Key, string Value);

/// <summary>Le signal sur le pipeline. Un événement comme les autres.</summary>
public sealed record ProgressSignalRaised(ProgressSignal Signal) : IEvent;
```

**Les valeurs restent des chaînes.** C'est déjà le cas (`RewardTrackFactSnapshot`), c'est ce que la
capture inter-étapes sérialise sur la ligne du joueur, et un `object` typé forcerait chaque
consommateur à connaître le type de chaque fait. Le **type** vit dans le registre (§5), pas dans la
valeur : il sert à l'éditeur, pas au moteur.

`DeliveryId` porte l'`OperationId` de l'événement source quand il en a un. Chaque consommateur
continue de faire tourner **son propre** garde de rejeu avec sa propre clé (§1.3, §7.3).

### 3.1 Pourquoi `Target` est un champ et pas seulement un fait

`Target` est déjà un paramètre de premier rang de `IPlayerRewardTrackGrain.ProgressAsync`, et il y
fait **deux choses qu'aucun fait ne fait** :

1. c'est ce que le `Parameter` d'une tâche compare — le mécanisme des tâches d'avant les séquences,
   qui continue de fonctionner sans être réécrit ;
2. c'est **la clé de déduplication du mode distinct** : « visiter 20 apparts différents » compte 20
   `Target` distincts.

Le noyer dans les faits obligerait chaque consommateur à connaître la clé conventionnelle `"target"`
et casserait le mode distinct au premier oubli. Il reste dupliqué dans les faits — c'est déjà le cas
aujourd'hui, et c'est ce qui permet de filtrer dessus comme sur les autres.

---

## 4. Le traducteur

### 4.1 Forme

```csharp
public sealed class RoomCreatedTranslator : ISignalTranslator<RoomCreatedEvent>
{
    // Une forme par action produite -- l'achat catalogue en déclare deux.
    public static ImmutableArray<SignalShape> Shapes { get; } =
    [
        new(SignalActions.CreateRoom,
            [Facts.Room, Facts.RoomName, Facts.RoomDescription, Facts.Category, Facts.Model]),
    ];

    public ImmutableArray<ProgressSignal> Translate(RoomCreatedEvent e) =>
        [new(e.OwnerId.Value, SignalActions.CreateRoom, 1,
            Target: e.RoomId.ToString(CultureInfo.InvariantCulture),
            [
                new(Facts.Room.Key, e.RoomId.ToString(CultureInfo.InvariantCulture)),
                new(Facts.RoomName.Key, e.Name),
                new(Facts.RoomDescription.Key, e.Description),
                new(Facts.Category.Key, e.CategoryId.ToString(CultureInfo.InvariantCulture)),
                new(Facts.Model.Key, e.ModelName),
            ],
            DeliveryId: "")];
}
```

**`Translate` renvoie zéro, un ou plusieurs signaux** — pas un seul. Ce n'est pas de la
généralisation gratuite, trois handlers actuels le font déjà :

| Handler | Ce qu'il émet |
| --- | --- |
| Achat catalogue | **deux actions** : `buy_from_catalogue` et `spend_credits`, avec des montants différents |
| Échange conclu | **deux joueurs** : un signal par participant, chacun avec l'autre comme fait `player` |
| Geste | `dance` **ou** `wave` selon le geste, et rien pour un geste inconnu |
| Meuble déplacé | `move_item` **ou** `rotate_item` — deux handlers sur le même `ItemMovedEvent` aujourd'hui, un seul traducteur demain |

Une signature qui rend un seul signal aurait forcé ces trois-là à rester des handlers écrits à la
main, c'est-à-dire à rester hors du test générique — exactement la population où les dérives se sont
produites.

Le tableau vide remplace les `if (...) return;` en tête des handlers actuels : un chuchotement, un
geste inconnu, un joueur système. Chaque `Shape` déclare **l'union** des faits que les signaux
produits peuvent porter, et le test §10.1 exige que chaque clé annoncée soit couverte par au moins
un signal.

Un `SignalTranslatorHost<TEvent>` générique — **un seul pour tout le système** — implémente
`IEventHandler<TEvent>`, appelle le traducteur et publie `ProgressSignalRaised`. C'est lui que le
balayage d'assembly enregistre ; les traducteurs eux-mêmes n'ont aucune dépendance.

### 4.2 Deux propriétés qui justifient tout le reste

1. **`Shapes` est la seule source de vérité.** Le dashboard et le validateur les lisent, plus une
   carte parallèle. Un fait proposé dans l'éditeur mais jamais émis devient impossible à écrire.
2. **`Translate` est une fonction pure.** Un test générique parcourt tous les traducteurs, leur
   donne un événement fabriqué et vérifie que chaque clé annoncée sort réellement —
   **un seul test pour les 137 événements** (§10.1). C'est précisément ce que la forme actuelle
   rendait impossible.

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

### 4.4 La porte d'intérêt — à ne surtout pas perdre en route

`RewardTrackSignal.SendAsync` commence aujourd'hui par `if (!catalog.IsActionInteresting(action))
return;`, et le commentaire de `RewardTrackCatalog` dit exactement pourquoi :

> Les entrées d'appart, les lignes de chat et les poses de meubles arrivent constamment ; sans
> l'index, chacune atteindrait un grain pour y découvrir qu'elle n'intéresse personne.
> `IsActionInteresting` répond depuis un `HashSet` sur le thread appelant, donc une action qu'aucun
> contenu ne mentionne coûte une recherche et s'arrête là.

Publier le signal inconditionnellement supprimerait cette porte et paierait une enveloppe par action
pour toujours. **Le `SignalTranslatorHost` la reprend, élargie** : il interroge un
`ISignalInterest` agrégé — l'union des actions qui intéressent au moins un consommateur — et
n'appelle le traducteur que si la réponse est oui.

```csharp
// L'union, reconstruite quand un consommateur recharge son contenu.
public interface ISignalInterest { bool AnyConsumerCares(string action); }
```

Chaque consommateur publie son propre ensemble d'actions (les reward tracks l'ont déjà : c'est
`_index.Actions`) et l'union est recalculée aux mêmes moments qu'aujourd'hui — au démarrage, et
après une écriture de contenu par le service d'admin. **Sans cette porte, la conception est une
régression de performance sur les trois chemins les plus chauds du hall.**

Conséquence sur `Shapes` : la porte se ferme sur l'**action**, mais un traducteur peut en produire
plusieurs (§4.1). Elle est donc évaluée sur l'union des actions déclarées par le traducteur, et le
tri fin — « cette action-ci n'intéresse personne » — reste au consommateur, où il est déjà.

---

## 5. Le registre des faits

### 5.1 Un fait est typé

```csharp
public sealed record FactKey(string Key, FactKind Kind, string LabelKey);

public enum FactKind
{
    Text,           // nom, description, mission
    Number,         // un compte, un niveau
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
le select sol/mur). Ça remonte dans le contrat, donc **quêtes et succès héritent des pickers sans
écrire une ligne**.

### 5.3 Gouvernance : additif seulement

Les clés de faits sont stockées **en chaînes dans le contenu**
(`RewardTrackStepFilterEntity.FactKey`, et les tables équivalentes des futurs consommateurs).

> **On ajoute des faits, on n'en renomme jamais, on n'en supprime jamais.** Un fait qui n'a plus de
> sens est marqué `[Obsolete]`, disparaît de l'éditeur, et continue d'être évalué pour le contenu
> qui l'utilise déjà.

Un test verrouille la règle : il compare les clés déclarées à une liste de référence versionnée, et
échoue si une clé disparaît ou change de type (§10.3). C'est la discipline habituelle des registres
de schémas ; sans elle, un renommage casse silencieusement des filtres déjà écrits en base.

---

## 6. Les opérateurs

`Equals`, `NotEquals`, `OneOf` existent. On ajoute :

```
Contains = 3   // sous-chaîne, insensible à la casse, comparaison ordinale
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

---

## 7. Les consommateurs

### 7.1 Forme

```csharp
public sealed class RewardTrackSignalConsumer(...) : IEventHandler<ProgressSignalRaised>
{
    public ValueTask HandleAsync(ProgressSignalRaised e, EventContext ctx, CancellationToken ct)
        => /* ce que fait aujourd'hui RewardTrackSignal.SendAsync */;
}
```

Un consommateur par système. Ils sont découverts, isolés et dispatchés en parallèle par le registre
existant : la garantie actuelle — un handler qui tombe n'empêche pas les autres — est conservée
telle quelle, sans code.

### 7.2 Correspondance des vocabulaires

Quêtes et succès **gardent leurs codes**. Chacun tient sa table `action → son code` :

```csharp
// Vortex.Progression/Quests/Signals/DailyTaskActionMap.cs
[SignalActions.EnterOtherUsersRoom] = QuestTypes.RoomEntry,
[SignalActions.ChangeFigure]        = QuestTypes.AvatarLooks,
```

Aucune migration de données, aucun contenu à réécrire. Une table de 20 lignes remplace 7 handlers
pour les quêtes, une de 8 lignes remplace 8 handlers pour les succès.

### 7.3 Livraison au moins une fois

Le garde de rejeu reste **par consommateur** : `FirstDeliveryAsync(journal, signal.DeliveryId,
"reward-track" | "daily-task" | "achievement", ct)`. Dédupliquer une fois pour tous dans le
traducteur serait un bug : les consommateurs sont isolés, l'un peut échouer là où les autres
réussissent, et il doit revoir la livraison.

### 7.4 Le wired n'est pas un consommateur

Le wired est évalué **en direct dans le grain de l'appart**, sur l'état vivant de la pièce, pas sur
un flux d'événements — et il est déjà pointé comme P0 dans l'audit protocole. Il prend **la
grammaire** (le registre de faits, les opérateurs, l'évaluateur, les contrôles de l'éditeur) et
fournit ses faits lui-même depuis l'état du grain. Le faire consommer le bus signifierait faire
transiter chaque pas d'un avatar par le pipeline d'événements : c'est non.

Le sens inverse existe déjà et ne change pas : l'action wired `PROGRESS_REWARD_TRACK` appelle
`IPlayerRewardTrackGrain.ProgressTaskAsync`, qui nomme une piste et une tâche et **contourne
délibérément l'index d'actions**. Le wired écrit dans les reward tracks sans passer par un signal,
parce qu'il ne décrit pas quelque chose que le joueur a fait — il ordonne une progression.

### 7.5 Ordre et concurrence

Le registre dispatche les handlers **en parallèle**, donc deux signaux d'un même joueur peuvent être
traités dans le désordre. Ce n'est pas nouveau : c'est déjà vrai des trois familles de handlers
d'aujourd'hui. **Le point de sérialisation est le grain du joueur** — Orleans exécute ses appels un
à la fois, et c'est là que la capture inter-étapes (`StepCaptures`) est lue puis réécrite.

Une séquence multi-étapes est donc sûre au sens où deux signaux ne peuvent pas s'écraser, mais
**l'ordre de deux actions quasi simultanées n'est pas garanti**. Une séquence « pose un canapé puis
marche dessus » exécutée en deux dixièmes de seconde peut échouer à s'ordonner. C'est le
comportement actuel, il est conservé tel quel, et il est écrit ici pour que personne ne découvre
plus tard que la conception l'avait promis autrement.

---

## 8. Où vit le code

| Projet | Contenu | Dépend de |
| --- | --- | --- |
| `Vortex.Primitives/Signals/` | `ProgressSignal`, `ProgressSignalRaised`, `SignalActions`, `Facts`, `FactKind`, `SignalFilter`, l'évaluateur, le validateur | rien |
| **`Vortex.Signals`** *(nouveau)* | les traducteurs, `SignalTranslatorHost<T>`, le registre des `Shape` | Primitives, Events |
| `Vortex.RewardTracks` | son consommateur | Primitives |
| `Vortex.Progression` | ses deux consommateurs + les deux tables de correspondance | Primitives |
| `Vortex.Dashboard.API` | sert les `Shape` à l'éditeur | Vortex.Signals |

**Aucun cycle** : les consommateurs ne connaissent que le contrat dans Primitives, jamais
`Vortex.Signals`. Le nouveau projet doit être référencé par `Vortex.Main` pour que le balayage
d'assembly le voie, et rangé dans le dossier de solution qui va bien (cf. commit `14226133b`).

L'évaluateur part de `Vortex.RewardTracks/Progression/TaskProgressRules.StepMatches` vers Primitives
tel quel — c'est de la logique pure, sans dépendance.

### 8.1 Un plugin peut ajouter un traducteur

`PluginBootstrapper` et `PluginManager` font tourner **le même `AssemblyProcessor`** que l'hôte sur
les assemblys de plugins. Un traducteur étant découvert par ce balayage, **un plugin peut livrer les
siens sans que le cœur soit modifié** — et avec eux ses propres faits et ses propres actions. C'est
l'argument le plus fort en faveur de cette forme : la modularité n'est pas à construire, elle est
héritée du chargeur de plugins existant.

Deux règles en découlent, et elles sont la contrepartie :

1. **Espace de noms.** Une clé de fait ou d'action venant d'un plugin est préfixée par la clé du
   plugin (`acme:trophy`). Une collision avec le vocabulaire du cœur est refusée au chargement, pas
   découverte le jour où deux plugins se marchent dessus.
2. **La règle « on ne renomme jamais » (§5.3) vaut aussi pour eux**, mais le dépôt ne peut pas la
   verrouiller par un test. Elle est donc écrite dans le contrat du plugin : un plugin qui renomme
   une de ses clés casse le contenu écrit dessus, et c'est à lui d'assumer sa migration.

Le vocabulaire du cœur reste fermé et testé (§10.3) ; celui des plugins est ouvert et à leur charge.

---

## 9. Le dashboard devient générique

`RewardTrackActionOptions` renvoie aujourd'hui `{ name, wired, facts: string[] }`, où `facts` sort
de la carte manuelle. Il renverra les `Shape` du registre, faits **typés** :

```json
{ "name": "create_room",
  "facts": [
    { "key": "room",     "kind": "RoomId",     "labelKey": "facts.room" },
    { "key": "name",     "kind": "Text",       "labelKey": "facts.roomName" },
    { "key": "category", "kind": "CategoryId", "labelKey": "facts.category" } ] }
```

Le composant d'édition de filtres devient un composant à part, piloté par `kind` : c'est lui qui
choisit picker, select ou champ texte, et la liste d'opérateurs. Les éditeurs de quêtes et de succès
le réutilisent tels quels le jour où ils gagnent des filtres.

Le drapeau `wired` (« cette action a-t-elle un producteur ? ») **reste**, et devient exact au lieu
d'être une liste tenue à la main : le registre sait quelles actions ont un traducteur.

Il ne peut pas disparaître. `SignalActions` reste une **liste déclarée**, pas la projection des
traducteurs existants : sur les 26 actions déclarées aujourd'hui, 24 ont un producteur et **deux
n'en ont pas** (`teleport`, `wired`), et du contenu peut déjà les nommer. Les dériver des traducteurs les ferait disparaître de l'éditeur
sans prévenir, et une tâche écrite dessus deviendrait invisible au lieu d'être signalée comme
inerte. Le drapeau est ce qui le dit à l'opérateur ; c'est une protection, pas un pansement.

---

## 10. Tests

### 10.1 Le test qui manquait

Un test paramétré sur tous les traducteurs découverts :

1. fabrique l'événement source par réflexion (valeurs non nulles, non vides, non nulles
   numériquement — un fait à `0` ou `""` passerait à côté d'un traducteur qui oublie un champ) ;
2. appelle `Translate` ;
3. exige que **chaque clé déclarée soit présente et non vide** dans au moins un signal produit,
   et qu'aucun signal ne porte une clé non déclarée.

Il échoue le jour où quelqu'un ajoute un fait à une `Shape` sans l'émettre — c'est-à-dire exactement les
neuf dérives du §1.2, mais avant le commit.

### 10.2 L'évaluateur

Les cas de `TaskProgressRulesTests` restent valides ; s'ajoutent `Contains` (casse, sous-chaîne,
chaîne vide), l'échec en fermé sur fait absent pour les quatre opérateurs, et le tableau des
opérateurs autorisés par `FactKind`.

### 10.3 La gouvernance

Un test compare les `FactKey` déclarées à une liste de référence versionnée dans le dépôt et échoue
si une clé disparaît ou change de `Kind`. Ajouter une clé met la liste à jour ; en retirer une
demande de le vouloir explicitement.

### 10.4 Les chemins chauds

`Vortex.Benchmark` mesure déjà les arrivées. Une passe avant/après sur l'entrée d'appart :
aujourd'hui trois handlers traduisent l'événement, demain un traducteur plus une enveloppe
supplémentaire pour trois consommateurs. On vérifie la mesure, on ne la suppose pas.

Le cas à mesurer en priorité est **celui où aucun contenu n'écoute** : la porte d'intérêt (§4.4)
doit rendre ce cas au moins aussi rapide qu'aujourd'hui, sinon la conception coûte plus qu'elle ne
rapporte sur un hôtel sans campagne active.

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

---

## 11. Étapes de livraison

Chaque étape est livrable seule et laisse le système fonctionnel.

**① Le contrat, les traducteurs, un consommateur.**
`Vortex.Primitives/Signals`, `Vortex.Signals` avec les 21 traducteurs portés depuis
`RewardTrackEventHandlers`, le consommateur reward tracks, l'évaluateur déplacé, le test §10.1.
`RewardTrackEventHandlers.cs` est supprimé. `RewardTrackActionFacts` est supprimé — c'est le
registre qui répond. Aucun changement de comportement observable ; les tests de contenu existants le
prouvent.

**② Quêtes et succès.**
Leurs deux consommateurs, leurs deux tables de correspondance, et la suppression de leurs 15
handlers. Les cinq événements traduits trois fois ne le sont plus qu'une.

**③ La grammaire pour le wired et le vocabulaire élargi.**
Le composant de filtres partagé côté dashboard, le wired branché sur le registre de faits avec ses
propres sources, et l'élargissement de la couverture (§13).

### 11.1 Ce que ② n'apporte PAS

L'étape ② ne donne **aucune capacité nouvelle** aux quêtes ni aux succès : ni filtres, ni faits, ni
séquences. C'est de la déduplication pure — cinq événements traduits une fois au lieu de trois. Leur
contenu, leurs tables et leurs écrans sont inchangés, et un joueur ne voit rien.

C'est délibéré : leur donner des filtres en même temps mélangerait une migration sans risque
observable et une fonctionnalité neuve dans la même livraison. Les filtres pour les quêtes, s'ils
sont voulus, sont une étape ④ qui n'existe pas encore — et qui sera bon marché puisque la grammaire
et l'éditeur seront là.

### 11.2 Bascule et retour arrière

**Il ne faut pas faire tourner l'ancien handler et le nouveau consommateur en même temps.** Les deux
appelleraient `ProgressAsync` pour le même acte, et toute progression compterait double — sur des
compteurs cumulés persistés, donc sans rattrapage possible autrement qu'à la main en base. Un
« double run pour comparer » est le piège évident de cette migration ; il est explicitement exclu.

Le retour arrière est donc **un revert de commit**, pas un drapeau de configuration. Ce qui rend le
revert sûr :

- l'étape ① ne touche **aucun schéma de base** — mêmes tables, mêmes colonnes, mêmes valeurs. Ce
  sont les mêmes appels de grain, émis depuis un autre endroit ;
- elle est livrée **en une seule fois** (contrat, traducteurs, consommateur, suppression des anciens
  handlers) précisément pour qu'il n'existe jamais d'état intermédiaire où les deux tournent ;
- le contrôle après bascule est le compteur `signals_raised_total` (§10.5) comparé aux actions
  attendues : une action qui tombe à zéro dit immédiatement quel traducteur manque.

Un drapeau de configuration serait pire que le revert : il maintiendrait les deux chemins vivants,
donc le risque de double comptage, pour économiser un `git revert`.

### 11.3 Ordre de grandeur

Indicatif, pour arbitrer — pas un engagement.

| Étape | Fichiers | Le gros du travail |
| --- | --- | --- |
| ① | ~35 (dont 21 traducteurs mécaniques) | le portage des 21 traductions et le test générique §10.1 |
| ② | ~12 | les deux tables de correspondance, la suppression de 15 handlers |
| ③ | ~15 côté dashboard + wired | le composant de filtres partagé, puis la couverture §13 au fil de l'eau |

Les 21 traducteurs de ① sont du portage ligne à ligne depuis un fichier que je viens de lire
entièrement : c'est le volume, ce n'est pas la difficulté. La difficulté de ① est la porte
d'intérêt (§4.4) et le test générique.

---

## 12. Ce qui est déjà dans l'arbre de travail

Fait avant cette conception, non commité, et qui **reste valable** — ce sont les faits et les
correctifs, pas l'architecture :

- `RoomCreatedEvent` porte désormais description, modèle et catégorie ; `RoomService.Create` les
  publie.
- Les faits `name`, `desc`, `category`, `model` ; `create_room` les émet et cible l'id du nouvel
  appart.
- Les neuf dérives du §1.2 sont corrigées : chaque fait annoncé est émis.
- `StepFilterOperator.Contains` et son évaluation.
- Côté dashboard : pickers mobilier / appart / joueur sur les filtres, select sol-mur, et la
  refonte visuelle de l'éditeur de séquence.

À l'étape ①, ces traductions deviennent des traducteurs ; le travail n'est pas jeté, il est déplacé.

---

## 13. Couverture : les 115 événements restants

L'ordre est celui de l'utilité pour du contenu, pas de la facilité :

| Famille | Événements | Ce que ça débloque |
| --- | --- | --- |
| Pets | `PetLeveledUp` (enrichir : race, niveau, appart), naissance, soins, dressage | « élever un familier », « faire éclore un plant » |
| Groupes | création, adhésion, forum | « fonder un groupe », « poster sur un forum » |
| Marketplace / échanges | mise en vente, vente conclue, échange | « vendre trois meubles » |
| Collectibles / mystery box | frappe, ouverture, prix | « ouvrir une boîte », « frapper un objet » |
| Habbicons | déjà partiellement traduit | filtres par collection |
| Pêche, jeux, wired | prise, score, partie gagnée | tâches de mini-jeu |

Pour chacun : d'abord vérifier ce que l'événement **porte déjà**, puis enrichir au point de
publication si la donnée y est libre, et **écarter explicitement** ce qui coûterait une lecture
(§4.3).

---

## 14. Risques

| Risque | Parade |
| --- | --- |
| +1 enveloppe par action sur les chemins chauds | En face, la traduction et l'enrichissement passent de trois fois à une. Mesuré en §10.4 avant de livrer ①. |
| Le modèle canonique devient un goulot qui force des déploiements en lock-step | Vocabulaire additif (§5.3) ; un consommateur ignore ce qu'il ne connaît pas ; un filtre sur un fait absent échoue en fermé (§6). |
| Un renommage de clé casse du contenu déjà écrit en base | Interdit et verrouillé par un test (§5.3, §10.3). |
| Une redélivrance saute un consommateur | Garde de rejeu par consommateur, `DeliveryId` porté par le signal (§7.3). |
| Un traducteur qui plante coupe trois systèmes au lieu d'un | `Translate` est une fonction pure sans E/S : elle ne plante que sur un bug de code, et le registre isole déjà l'exception. Le test §10.1 la couvre. |
| Le nouveau projet n'est pas chargé, donc plus aucun signal | Un test d'hébergement vérifie qu'au moins un traducteur est découvert au démarrage, et `signals_raised_total` le dit en production. |
| **La porte d'intérêt est oubliée** et chaque action publie une enveloppe même quand aucun contenu n'écoute | C'est la régression de performance la plus probable de cette conception. §4.4, plus la mesure du cas « aucun contenu » en §10.4. |
| L'ancien handler et le nouveau consommateur tournent ensemble : **toute progression compte double**, sur des compteurs persistés | Livraison en un seul commit, pas de drapeau, retour arrière par revert (§11.2). |
| Deux plugins déclarent la même clé de fait | Préfixe obligatoire par clé de plugin, collision refusée au chargement (§8.1). |
| Une séquence multi-étapes échoue à s'ordonner sur deux actions quasi simultanées | Comportement actuel, conservé et écrit (§7.5). Le grain du joueur reste le point de sérialisation. |

---

## 15. Ce qui n'a pas été vérifié

Aucune source d'émulateur de référence n'est présente sur cette machine (à côté du dépôt il n'y a
que `vortex-modern-client`). Cette conception ne s'appuie donc sur **aucune** preuve tierce sur la
façon dont un autre émulateur Habbo déclenche ses quêtes — uniquement sur ce dépôt et sur les
patterns publics d'intégration (Message Translator, Canonical Data Model, Content Enricher, et la
gouvernance de taxonomie d'événements).
