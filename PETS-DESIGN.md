# Vortex Cloud — Design des Pets

Conception d'implémentation des pets, **correcte** là où les retros échouent. Complète le
schéma (`DATA-MODEL.md` §4) côté comportement, persistance et architecture. À lire avec
`docs/walkthroughs/request-lifecycle.md` (le modèle grain/système) et `ROADMAP.md`.

---

## Le diagnostic : pourquoi les retros ratent les pets

Les retros traitent un pet comme **un meuble qui bouge** ou **un faux avatar** : un objet qui
se déplace au hasard et reste planté. Un pet correct est un **agent autonome** :

1. il a des **besoins** qui décroissent dans le **temps réel** ;
2. il **agit seul** pour les satisfaire (chercher à manger, dormir) ;
3. il **apprend** (niveaux, commandes).

C'est cette boucle autonome — besoins → décision → action → apprentissage — que personne
n'implémente vraiment. Le reste (anim, figure) est cosmétique.

---

## Architecture dans le modèle Orleans

### Où vit le pet

- **Identité + stats persistantes** → `PetEntity` en DB (`DATA-MODEL.md` §4).
- **Comportement** → un `RoomPetSystem` **dans le `RoomGrain`**, PAS un `PetGrain` séparé.

> **Pourquoi pas un PetGrain.** Le pet a besoin en permanence de l'état de la room pour
> naviguer (carte, meubles, autres avatars). Un grain séparé = un appel cross-grain à chaque
> pas → latence et complexité inutiles. Le pet est intrinsèquement **room-scoped** tant qu'il
> est posé. Il est donc un `RoomObject` du room grain, comme un avatar, piloté par un système
> — exactement le pattern de `RoomChatSystem` / `RoomPathingSystem` / `RoomAvatarTickSystem`.

### Intégration aux systèmes existants

- **Déplacement** → réutilise ton **A\*** (`RoomPathingSystem`). Le pet demande un chemin
  comme un avatar ; il contourne les meubles, il ne téléporte pas.
- **Tick** → le `RoomPetSystem` est tické avec les avatars (`RoomAvatarTickSystem` ou un tick
  dédié). Chaque tick fait avancer la machine à états et la décroissance des besoins.
- **Diffusion** → les updates de pet (position, geste, statut, level-up) passent par
  `SendComposerToRoomAsync` → stream → presence (le flux de `request-lifecycle.md`).

---

## Le cœur : machine à états pilotée par les besoins

```
            besoins OK
   ┌──────────────────────────┐
   ▼                          │
[Idle] ──timer──> [Wander] ───┘
   │  ▲
   │  └───────────────────────────────┐
   │ nutrition < seuil                 │
   ├──────────> [Hungry] ──food trouvée──> [Eat] ──> (nutrition+, XP+) ──┐
   │               │ pas de food                                          │
   │               └──> whine/idle                                        │
   │ énergie < seuil                                                      │
   ├──────────> [Sleep] ──(nid/lit ou sur place)──> (énergie régénère) ──┤
   │ owner ordonne                                                        │
   └──────────> [Command] ──skill apprise ?──> geste/anim + XP ──────────┘
                              non → ignore
```

États :

- **Idle** : par défaut ; bascule de temps en temps vers Wander.
- **Wander** : tuile atteignable au hasard, path A\* dessus.
- **Hungry** (nutrition sous seuil) : cherche la `pet_food` la plus proche matching son
  `pet_type`, y va, mange. Pas de food → geint/idle.
- **Eat** : consomme la food (item décrémenté/supprimé), `nutrition += pet_food.nutrition`,
  petit gain d'XP.
- **Sleep** (énergie basse) : nid/lit pet ou sur place ; régénère l'énergie.
- **Command** (owner) : interrompt ; exécute **si la skill est apprise** (selon le niveau) ;
  joue le geste ; gagne de l'XP ; revient à Idle.

**Les besoins conduisent les transitions.** La faim monte, l'énergie baisse → ça déclenche
Hungry/Sleep tout seul. C'est l'autonomie que les retros n'ont pas.

---

## Les deux points d'ingénierie qui font le « correct »

### 1. Décroissance basée sur le TEMPS ÉCOULÉ, pas sur le tick

Si tu décrémentes la faim « à chaque tick tant que la room est chargée », un pet dans une room
déchargée n'a jamais faim → incohérent. Bon modèle :

```
au chargement du pet (ou à chaque maj) :
    elapsed = now - pet.updated_at
    pet.nutrition = clamp(pet.nutrition - hungerRate * elapsed)
    pet.energy    = clamp(pet.energy    - energyRate * elapsed)
```

Le pet a faim en fonction du **temps réel**, observé ou non. Quasi personne ne le fait.

### 2. Persistance des stats : en mémoire, flush THROTTLÉ — jamais par tick

Les stats changent à chaque tick (décroissance). Tu ne les écris **pas** à chaque tick.

- Stats en mémoire dans l'état de la room.
- Flush en DB **périodique** (toutes les N s, ou sur changement significatif : level-up, repas)
  **et** au déchargement de room / ramassage du pet.

> C'est **la leçon de ton bug wired** (`RoomActive` vs `Persistent`) : ne perds pas la donnée,
> mais ne martèle pas la DB. Les retros tombent dans un extrême — soit ils perdent les stats au
> reboot, soit ils écrivent à chaque frame et la DB fume.

---

## Leveling & commandes (par-dessus la boucle)

- **Sources d'XP** : manger, exécuter une commande, recevoir des scratches (respect).
- **Seuils par niveau** : config `pet_levels` (`DATA-MODEL.md` §4.2). Monter de niveau augmente
  les caps de stats et débloque des commandes.
- **Commandes** : config `pet_commands` (`pet_type`, `command`, `level_required`). Le pet
  n'exécute une commande que s'il l'a apprise (niveau atteint).

---

## Détails que les retros oublient (et qui font le correct)

- **Pathfinding réel** autour des meubles via l'A\* existant — pas un déplacement bidon.
- **Limite quotidienne de scratches/respect** (comme le respect joueur : N/jour, pas infini).
- **Breeding qui hérite des traits** (type/couleur des parents) avec une part d'aléa — pas un
  pet random. Traçabilité via `parent_one_id`/`parent_two_id` (`DATA-MODEL.md` §4).
- **Décroissance hors-ligne** : appliquée au rechargement (cf. point 1), pas ignorée.

---

## Contrat du `RoomPetSystem` (ce qu'il expose au RoomGrain)

- `LoadPetAsync(petId)` — charge la `PetEntity`, applique la décroissance temps-écoulé,
  l'ajoute comme RoomObject.
- `TickAsync()` — avance la machine à états + besoins de chaque pet ; marque dirty.
- `IssueCommandAsync(petId, command, actorPlayerId)` — vérifie skill apprise + ownership,
  exécute, accorde l'XP.
- `FeedAsync(petId, foodFurniId)` — consomme la food, monte la nutrition, accorde l'XP.
- `FlushDirtyAsync()` — persiste les stats des pets dirty (throttlé + au unload).

Même forme que tes autres systèmes de room : un système plat tenant une référence au grain,
appelé en in-process, qui délègue la persistance et la diffusion aux mécanismes existants.

---

## Ordre d'implémentation conseillé

1. `PetEntity` + placement/ramassage (le pet existe et apparaît dans la room).
2. `RoomPetSystem` + machine à états Idle/Wander (le pet bouge intelligemment).
3. Besoins + décroissance temps-écoulé + Hungry/Eat avec `pet_food` (l'autonomie).
4. Persistance throttlée (la leçon wired).
5. Sleep, puis niveaux (`pet_levels`), puis commandes (`pet_commands`).
6. Breeding et scratches en dernier.

> Chaque étape est jouable et testable seule. Ne saute pas l'étape 3-4 : c'est ce qui sépare
> un vrai pet d'un meuble qui bouge.
