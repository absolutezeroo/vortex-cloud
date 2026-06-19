# Vortex Cloud — Modèle de données (final)

Spécification consolidée et définitive des tables manquantes + des règles de conception,
calée sur les conventions **réelles** du repo et sur la source Habbo (WIN63) comme ground
truth. Toutes les décisions sont verrouillées. Objectif : qu'aucun dev ni agent ne parte de
travers, et viser le meilleur — pas juste le fonctionnel.

Sommaire : 0) conventions & nommage · 1) amis (existant) · 2) groupes · 3) rentable space ·
4) pets · 5) bots · 6) wired (bug de persistance) · 7) trading · 8) metrics (tiering) ·
9) checklist.

---

## 0. Conventions & règle de nommage

### 0.1 Base d'entité (hérité de `TurboEntity`, NE JAMAIS redéclarer)

| Colonne | Type | Rôle |
|---|---|---|
| `id` | int, PK identity | clé primaire |
| `created_at` | datetime | création (auto) |
| `updated_at` | datetime | modification (auto, computed) |
| `deleted_at` | datetime? | **soft delete** |

### 0.2 Style (à reproduire)

- `[Table("snake_case_pluriel")]`, `[Column("snake_case")]`.
- FK : `[Column("x_id")] public required int XEntityId;` + `[ForeignKey(nameof(XEntityId))]
  public required XEntity XEntity;` (nav nullable si FK nullable).
- Unicité composite : `[Index(nameof(A), nameof(B), IsUnique = true)]`.
- Obligatoire `required` ; optionnel nullable. Enums : type direct, `[DefaultValue]` +
  `DatabaseGeneratedOption.None`, dans `Turbo.Primitives.<Domaine>.Enums`.
- Soft delete partout ; jamais de hard delete.

### 0.3 Règle de préfixe (résout l'incohérence `ltd_`)

**Le préfixe = le bounded context propriétaire. Les tables de référence partagées restent
neutres (sans préfixe).**

État vérifié : `catalog_offers/pages/products/club_offers/club_gifts` sont cohérents, mais
`ltd_series` / `ltd_raffle_entries` ne le sont pas — le LTD est un concept **catalogue**.

- À corriger : `ltd_series` → **`catalog_ltd_series`**, `ltd_raffle_entries` →
  **`catalog_ltd_raffle_entries`**.
- À **garder neutre** : `currency_types`. La monnaie n'appartient pas au catalogue — elle
  est référencée par `economy_ledger`, `marketplace_offers` et le catalogue. C'est
  transversal → pas de préfixe. (Ne pas forcer `catalog_currency_types`.)

> Pragmatique : verrouiller la règle maintenant, l'appliquer à **toutes les nouvelles**
> tables, et renommer `ltd_*` dans une **migration dédiée** quand c'est commode — pas en
> big-bang au milieu du dev.

---

## 1. Amis & messagerie — EXISTANT (ne pas recréer)

| Table | Entité |
|---|---|
| `messenger_friends` | `MessengerFriendEntity` (`player_id`, `requested_id`, `category_id?`, `relation`) |
| `messenger_requests` | `MessengerRequestEntity` |
| `messenger_blocked` | `MessengerBlockedEntity` |
| `messenger_ignored` | `MessengerIgnoredEntity` |
| `messenger_categories` | `MessengerCategoryEntity` |
| `messenger_messages` | `MessengerMessageEntity` |

L'Epic 5 côté amis = câblage de handlers, 0 schéma.

---

## 2. Groupes / Guildes — À CRÉER

Modèle guildes Habbo. **Décision** : la config du forum est séparée de l'identité (table
1:1), pas en colonnes sur `groups`.

### 2.1 `groups` — `GroupEntity` (identité, table chaude)

| Colonne | Type | Null | Index |
|---|---|---|---|
| `name` | string(50) | non | — |
| `description` | string(255) | oui | — |
| `badge` | string(100) | non | — |
| `room_id` | int FK rooms | non | unique |
| `player_id` | int FK players | non | index |
| `type` | `GroupType` | non | — |
| `color_one` / `color_two` | string(12) | non | — |
| `admin_only_decoration` | bool | non | — |

```csharp
[Table("groups")]
[Index(nameof(RoomEntityId), IsUnique = true)]
[Index(nameof(OwnerPlayerEntityId))]
public class GroupEntity : TurboEntity
{
    [Column("name")] [MaxLength(50)] public required string Name { get; set; }
    [Column("description")] [MaxLength(255)] public string? Description { get; set; }
    [Column("badge")] [MaxLength(100)] public required string Badge { get; set; }
    [Column("room_id")] public required int RoomEntityId { get; set; }
    [Column("player_id")] public required int OwnerPlayerEntityId { get; set; }
    [Column("type")] [DefaultValue(GroupType.Open)] [DatabaseGenerated(DatabaseGeneratedOption.None)] public required GroupType Type { get; set; }
    [Column("color_one")] [MaxLength(12)] public required string ColorOne { get; set; }
    [Column("color_two")] [MaxLength(12)] public required string ColorTwo { get; set; }
    [Column("admin_only_decoration")] [DefaultValue(false)] public required bool AdminOnlyDecoration { get; set; }

    [ForeignKey(nameof(RoomEntityId))] public required RoomEntity RoomEntity { get; set; }
    [ForeignKey(nameof(OwnerPlayerEntityId))] public required PlayerEntity OwnerPlayerEntity { get; set; }
    [InverseProperty(nameof(GroupMemberEntity.GroupEntity))] public List<GroupMemberEntity>? Members { get; set; }
    [InverseProperty(nameof(GroupForumSettingsEntity.GroupEntity))] public GroupForumSettingsEntity? ForumSettings { get; set; }
}
```

### 2.2 `group_members` — `GroupMemberEntity`

| Colonne | Type | Null | Index |
|---|---|---|---|
| `group_id` | int FK groups | non | unique(group_id, player_id) |
| `player_id` | int FK players | non | ↑ + index |
| `rank` | `GroupMemberRank` | non | — |

### 2.3 `group_membership_requests` — `GroupMembershipRequestEntity`

`group_id`, `player_id` — unique(group_id, player_id). Pour les groupes `Exclusive`.

### 2.4 `group_forum_settings` — `GroupForumSettingsEntity` (config 1:1)

Créée avec defaults à la création du groupe (invariant : 1 ligne par groupe).

| Colonne | Type | Null | Index |
|---|---|---|---|
| `group_id` | int FK groups | non | unique |
| `enabled` | bool | non | — |
| `read_permission` / `post_permission` / `thread_permission` / `mod_permission` | `GroupForumPermission` | non | — |

### 2.5 / 2.6 Forum : `group_forum_threads` / `group_forum_posts`

- **threads** : `group_id`(idx), `player_id`, `subject`, `state` (`GroupForumThreadState`),
  `is_pinned`, `post_count`, `last_post_at`(idx), `last_post_player_id?`.
- **posts** : `thread_id`(idx), `group_id`(idx), `player_id`, `message`(text), `state`
  (`GroupForumPostState`). Modération = bascule `state`, jamais hard-delete.

### 2.7 Lien room → groupe (modif `RoomEntity`)

```csharp
[Column("group_id")] public int? GroupEntityId { get; set; }
[ForeignKey(nameof(GroupEntityId))] public GroupEntity? GroupEntity { get; set; }
```

> **Relation circulaire** `groups.room_id` ↔ `rooms.group_id` : `OnDelete` non-cascade
> (Fluent API). Dissoudre = détacher `rooms.group_id` puis soft-delete.

### 2.8 Enums (`Turbo.Primitives.Groups.Enums`)

```csharp
public enum GroupType { Open = 0, Exclusive = 1, Private = 2 }
public enum GroupMemberRank { Member = 0, Admin = 1 }
public enum GroupForumThreadState { Open = 0, Locked = 1, Hidden = 2 }
public enum GroupForumPostState { Visible = 0, Hidden = 1, HiddenByAdmin = 2 }
public enum GroupForumPermission { Members = 0, Admins = 1, Owner = 2, Everyone = 3 }
```

---

## 3. Rentable Space — À CRÉER (feature Habbo officielle, vérifiée WIN63)

Type de furni `furniture_rentable_space`. L'espace **est une furniture**. Packet de statut :
`rented, canRent, canRentErrorCode, renterId, renterName, timeRemaining, price`. Packets :
`rentSpace/cancelRent/getRentableSpaceStatus(furniId)`.

**Décisions verrouillées :** termes (prix/durée/monnaie/HC) sur le **type** (table dédiée,
pas sur `FurnitureDefinitionEntity`) · état = **1 ligne/instance, MAJ en place** (MySQL n'a
pas d'unique filtré) · historique = **gratuit via `economy_ledger`** (louer = entrée ledger).

### 3.1 `rentable_space_terms` — `RentableSpaceTermsEntity` (1:1 avec la définition)

| Colonne | Type | Null | Index |
|---|---|---|---|
| `furniture_definition_id` | int FK definitions | non | unique |
| `price` | int | non | — |
| `currency_type_id` | int FK currency_types | non | — |
| `rent_duration_seconds` | int | non | — |
| `requires_hc` | bool | non | — |

### 3.2 `room_rentable_spaces` — `RoomRentableSpaceEntity` (état, 1/instance)

| Colonne | Type | Null | Index |
|---|---|---|---|
| `furniture_id` | int FK furniture | non | unique |
| `renter_player_id` | int? FK players | oui | index |
| `rented_until` | datetime? | oui | — |

```csharp
[Table("room_rentable_spaces")]
[Index(nameof(FurnitureEntityId), IsUnique = true)]
[Index(nameof(RenterPlayerEntityId))]
public class RoomRentableSpaceEntity : TurboEntity
{
    [Column("furniture_id")] public required int FurnitureEntityId { get; set; }
    [Column("renter_player_id")] public int? RenterPlayerEntityId { get; set; }
    [Column("rented_until")] public DateTime? RentedUntil { get; set; }
    [ForeignKey(nameof(FurnitureEntityId))] public required FurnitureEntity FurnitureEntity { get; set; }
    [ForeignKey(nameof(RenterPlayerEntityId))] public PlayerEntity? RenterPlayerEntity { get; set; }
}
```

### 3.3 Tag des meubles posés (modif `FurnitureEntity`) — nettoyage atomique à l'expiration

```csharp
[Column("rentable_space_furniture_id")] public int? RentableSpaceFurnitureEntityId { get; set; }
[ForeignKey(nameof(RentableSpaceFurnitureEntityId))] public FurnitureEntity? RentableSpaceFurnitureEntity { get; set; }
```

> Expiration : rendre tous les meubles où `rentable_space_furniture_id = X` à l'inventaire,
> puis null `renter`/`rented_until`. Bornes vérifiées dans le grain au placement.

### 3.4 Règles (source) : 1 locataire/espace · **1 espace loué/joueur** (`can_rent_only_one_space`,
via l'index `renter_player_id`) · annulation par owner de la furni **ou** staff (`hasSecurity(5)`).

---

## 4. Pets — À CRÉER (feature confirmée WIN63 : `PetInfoData`, breeding, stats)

Modèle pet Habbo standard. Un pet appartient à un joueur, vit dans une room ou en inventaire,
porte ses stats.

| Colonne | Type | Null | Index | Sens |
|---|---|---|---|---|
| `player_id` | int FK players | non | index | propriétaire |
| `room_id` | int? FK rooms | oui | index | room courante, `null` = inventaire |
| `name` | string(40) | non | — | nom |
| `type` | int | non | — | espèce (chien/chat/…) |
| `race` | int | non | — | variante/breed |
| `color` | string(12) | non | — | couleur |
| `gender` | `AvatarGenderType` | non | — | sexe |
| `level` | int | non | — | niveau |
| `experience` | int | non | — | xp |
| `energy` | int | non | — | énergie |
| `nutrition` | int | non | — | faim |
| `respect` | int | non | — | « scratches »/respect reçu |
| `x` / `y` | int | non | — | position en room |
| `z` | double(10,3) | non | — | hauteur |
| `direction` | int | non | — | orientation |

```csharp
[Table("pets")]
[Index(nameof(OwnerPlayerEntityId))]
[Index(nameof(RoomEntityId))]
public class PetEntity : TurboEntity
{
    [Column("player_id")] public required int OwnerPlayerEntityId { get; set; }
    [Column("room_id")] public int? RoomEntityId { get; set; }
    [Column("name")] [MaxLength(40)] public required string Name { get; set; }
    [Column("type")] public required int Type { get; set; }
    [Column("race")] public required int Race { get; set; }
    [Column("color")] [MaxLength(12)] public required string Color { get; set; }
    [Column("gender")] [DefaultValue(AvatarGenderType.Male)] [DatabaseGenerated(DatabaseGeneratedOption.None)] public required AvatarGenderType Gender { get; set; }
    [Column("level")] [DefaultValue(1)] public required int Level { get; set; }
    [Column("experience")] [DefaultValue(0)] public required int Experience { get; set; }
    [Column("energy")] public required int Energy { get; set; }
    [Column("nutrition")] public required int Nutrition { get; set; }
    [Column("respect")] [DefaultValue(0)] public required int Respect { get; set; }
    [Column("x")] public required int X { get; set; }
    [Column("y")] public required int Y { get; set; }
    [Column("z", TypeName = "double(10,3)")] public required double Z { get; set; }
    [Column("direction")] public required int Direction { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))] public required PlayerEntity OwnerPlayerEntity { get; set; }
    [ForeignKey(nameof(RoomEntityId))] public RoomEntity? RoomEntity { get; set; }
}
```

> Le breeding (croisement) est confirmé dans la source (`ConfirmPetBreedingPetData`). Pas
> de table en plus : un pet né d'un croisement est un nouveau `PetEntity` ; la traçabilité
> parents, si voulue, passe par deux colonnes nullables `parent_one_id`/`parent_two_id`.

### 4.1 `pet_food` — `PetFoodEntity` (config food/produits)

Vérifié WIN63 : les food/boissons/jouets sont des **furnitures** avec `FurniturePetProductLogic`
(`useObject()` → le pet s'en sert). Le mapping item → type de pet → effet est de la config
**domaine pet** (donc préfixe `pet_`, pas `catalog_`, même si l'item est vendu au catalogue).
Les valeurs d'effet sont serveur (le client obfusqué ne les expose pas — comme le prix de la
rentable space).

| Colonne | Type | Null | Index | Sens |
|---|---|---|---|---|
| `furniture_definition_id` | int FK definitions | non | unique | l'item de nourriture |
| `pet_type` | int | non | index | espèce qui peut la manger |
| `nutrition` | int | non | — | nutrition restaurée à la consommation |

```csharp
[Table("pet_food")]
[Index(nameof(FurnitureDefinitionEntityId), IsUnique = true)]
[Index(nameof(PetType))]
public class PetFoodEntity : TurboEntity
{
    [Column("furniture_definition_id")] public required int FurnitureDefinitionEntityId { get; set; }
    [Column("pet_type")] public required int PetType { get; set; }
    [Column("nutrition")] public required int Nutrition { get; set; }

    [ForeignKey(nameof(FurnitureDefinitionEntityId))] public required FurnitureDefinitionEntity FurnitureDefinitionEntity { get; set; }
}
```

> Nourrissage (grain) : le pet va vers la food, mange, `pet.nutrition += pet_food.nutrition`,
> et l'item-food est **consommé** (décrément dans `extra_data` ou suppression de la furni).

### 4.2 Niveaux & commandes (implique par `PETS-DESIGN.md`)

Le design des pets (boucle d'apprentissage) implique deux tables de config. Voir
`PETS-DESIGN.md` pour le comportement runtime qui les consomme.

**`pet_levels` — `PetLevelEntity`** (config des seuils de niveau) :

| Colonne | Type | Null | Index | Sens |
|---|---|---|---|---|
| `pet_type` | int | non | unique(pet_type, level) | espèce |
| `level` | int | non | ↑ | niveau |
| `experience_required` | int | non | — | XP cumulée pour atteindre ce niveau |
| `energy_cap` | int | non | — | cap d'énergie à ce niveau |
| `nutrition_cap` | int | non | — | cap de nutrition à ce niveau |

**`pet_commands` — `PetCommandEntity`** (config des commandes apprenables) :

| Colonne | Type | Null | Index | Sens |
|---|---|---|---|---|
| `pet_type` | int | non | index | espèce |
| `command` | int | non | — | identifiant de commande (geste/trick) |
| `level_required` | int | non | — | niveau minimum pour l'exécuter |

> Le pet n'exécute une commande que si `pet.level >= level_required`. Mêmes conventions que le
> reste (TurboEntity, enums dans `Turbo.Primitives.Pets.Enums`).

**À garder pour plus tard, hors scope immédiat :**
- **Palettes de couleurs** (`SellablePetPalettesEvent`) → `pet_palettes` (`pet_type`, palette,
  couleurs) — quand tu feras la vente de pets au catalogue.

> Ordre : food → niveaux → commandes (cf. `PETS-DESIGN.md`). Chaque sous-système = une config +
> un effet runtime, même esprit que food.

---

## 5. Bots — À CRÉER (bots serveurs / « rentable bot », confirmé WIN63)

Un bot est un PNJ posable : identité d'avatar + position + un set de messages (parole
aléatoire, réponses à mots-clés). Deux tables.

### 5.1 `bots` — `BotEntity`

| Colonne | Type | Null | Index | Sens |
|---|---|---|---|---|
| `player_id` | int FK players | non | index | propriétaire |
| `room_id` | int? FK rooms | oui | index | room, `null` = inventaire |
| `name` | string(40) | non | — | nom |
| `motto` | string(255) | oui | — | mission |
| `figure` | string(100) | non | — | apparence |
| `gender` | `AvatarGenderType` | non | — | sexe |
| `x` / `y` | int | non | — | position |
| `z` | double(10,3) | non | — | hauteur |
| `direction` | int | non | — | orientation |

```csharp
[Table("bots")]
[Index(nameof(OwnerPlayerEntityId))]
[Index(nameof(RoomEntityId))]
public class BotEntity : TurboEntity
{
    [Column("player_id")] public required int OwnerPlayerEntityId { get; set; }
    [Column("room_id")] public int? RoomEntityId { get; set; }
    [Column("name")] [MaxLength(40)] public required string Name { get; set; }
    [Column("motto")] [MaxLength(255)] public string? Motto { get; set; }
    [Column("figure")] [MaxLength(100)] public required string Figure { get; set; }
    [Column("gender")] [DefaultValue(AvatarGenderType.Male)] [DatabaseGenerated(DatabaseGeneratedOption.None)] public required AvatarGenderType Gender { get; set; }
    [Column("x")] public required int X { get; set; }
    [Column("y")] public required int Y { get; set; }
    [Column("z", TypeName = "double(10,3)")] public required double Z { get; set; }
    [Column("direction")] public required int Direction { get; set; }

    [ForeignKey(nameof(OwnerPlayerEntityId))] public required PlayerEntity OwnerPlayerEntity { get; set; }
    [ForeignKey(nameof(RoomEntityId))] public RoomEntity? RoomEntity { get; set; }
    [InverseProperty(nameof(BotMessageEntity.BotEntity))] public List<BotMessageEntity>? Messages { get; set; }
}
```

### 5.2 `bot_messages` — `BotMessageEntity`

| Colonne | Type | Null | Index | Sens |
|---|---|---|---|---|
| `bot_id` | int FK bots | non | index | bot |
| `mode` | `BotMessageMode` | non | — | RandomSpeech / KeywordResponse |
| `keyword` | string(100) | oui | — | déclencheur (mode keyword) |
| `text` | string(255) | non | — | ce que le bot dit |

```csharp
public enum BotMessageMode { RandomSpeech = 0, KeywordResponse = 1 }
```

> Le « servir des boissons/items » est une action de bot configurable : si tu la vises, une
> table `bot_serve_items` (`bot_id`, `item_definition_id`) la modélise — mais commence par
> identité + messages, c'est le cœur.

---

## 6. Wired — PAS un trou de schéma, un BUG de persistance (priorité n°1)

**Constat vérifié dans le code.** `StuffPersistanceType` n'a que deux valeurs : `RoomActive
= 0` et `Persistent = 1`. `FurnitureWiredLogic` déclare `_stuffPersistanceType =>
StuffPersistanceType.RoomActive`. La furni persiste son état via la colonne `extra_data`
(`ExtraData.GetJsonString()`), et `WiredData` **est** sérialisable JSON.

**Conclusion.** En `RoomActive`, la donnée vit tant que la room est active mais n'est **pas**
écrite en DB → la **config wired d'un joueur (items sélectionnés, délais, conditions) ne
survit pas à un déchargement de room / reboot**. C'est une perte de données silencieuse, pas
un manque de table.

**Le fix (pas une migration de schéma) :**
- La furni wired doit utiliser `StuffPersistanceType.Persistent` et sérialiser son
  `WiredData` (déjà JSON-isable) dans `extra_data`, qui existe déjà.
- Distinguer **config** (à persister : `IntParams`, `StuffIds`, `StringParam`, sources) de
  l'**état runtime éphémère** (compteurs « a déjà déclenché », à laisser en mémoire).
- À couvrir par un test : poser un wired, configurer, décharger la room, recharger →
  la config est identique.

> C'est le seul point de cette liste qui peut faire perdre du contenu joueur **aujourd'hui**.
> À traiter avant pets/bots/groupes.

---

## 7. Trading — log via le ledger (pas d'état persistant)

Échange éphémère (grain : offre, lock, confirmation). Pas de table d'état. Trace
anti-duplication via `item_events` / `economy_ledger` existants. Vrai sujet : **atomicité**
du commit + **validation serveur** de possession (Epic 6).

---

## 8. Metrics / observability — ne pas réduire les tables, tiérer le stockage

**Verdict : le nombre de tables est justifié, fusionner serait une régression.** Les formes
sont réellement différentes : `economy_ledger` (financier typé, `delta`/`balance_after`, pas
de blob) ≠ `performance_logs` (télémétrie client : `flash_version`, `frame_rate`, `gc`) ≠
`error_groups`+`error_occurrences` (pattern Sentry, fingerprint/dédup) ≠ `audit_events` /
`item_events` (payload `data` text). Les fusionner = God-table/EAV : colonnes creuses, perte
des colonnes typées et des index, rétention par type impossible.

**Le vrai « mieux » = tiérer par pattern d'accès, pas réduire :**
- **Garder en MySQL (vérité transactionnelle)** : `economy_ledger`, `item_events`,
  `audit_events`.
- **Sortir vers un store time-series/analytique** (ClickHouse, ou Prometheus/Grafana via le
  meter `System.Diagnostics.Metrics` déjà en place) : la télémétrie haut volume — `performance_logs`
  et, à terme, les métriques de perf serveur.
- **À questionner/supprimer** : `performance_logs` porte `flash_version` → c'est du logging
  client legacy ; avec ta vraie observability en 2026, c'est une table candidate à la
  **suppression**, pas à la fusion.

---

## 9. Checklist anti-erreur (relire avant de coder une table)

- [ ] Hérite de `TurboEntity` ; ne pas redéclarer id/created_at/updated_at/deleted_at.
- [ ] « Supprimer » = `deleted_at`, jamais hard delete.
- [ ] FK = paire `[Column("x_id")] int` + `[ForeignKey] nav`, nullable accordé.
- [ ] Paires relationnelles → `[Index(..., IsUnique = true)]` composite.
- [ ] Enums dans `Turbo.Primitives.<Domaine>.Enums`, `[DefaultValue]` + `DatabaseGeneratedOption.None`.
- [ ] **Nommage** : préfixe = bounded context propriétaire ; référence partagée = neutre
      (`catalog_ltd_*`, mais `currency_types` nu).
- [ ] Relations circulaires → `OnDelete` non-cascade (Fluent API).
- [ ] Config froide/optionnelle/évolutive → table 1:1 (forum, termes de location) ; mais 1
      seul champ → on ne sort pas.
- [ ] État (instance) ≠ config (type) : termes de location vs état de location.
- [ ] Pas de table d'historique quand le ledger le donne gratuitement.
- [ ] Donnée joueur à conserver → mode de persistance `Persistent`, pas `RoomActive` (cf.
      wired).
- [ ] Ne pas recréer une table qui existe (amis/messagerie).
```

