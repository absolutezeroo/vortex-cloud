# Vortex Cloud — Prompts (couche d'exécution de la ROADMAP)

Ce document est le **pont** entre la ROADMAP (le quoi/pourquoi) et le code. Chaque prompt
est une *story* exécutable, façon BMAD : tu le donnes à Claude Code tel quel. Le principe de
liaison :

```
ROADMAP.md        →  quoi faire, dans quel ordre, et la Definition of Done
DATA-MODEL.md     →  les tables/entités exactes (source autoritaire)
docs/walkthroughs →  comment câbler une feature de bout en bout dans ce repo
PROMPTS.md (ici)  →  l'instruction exécutable qui pointe vers les trois ci-dessus
```

> **Pourquoi les prompts sont courts.** Le DATA-MODEL et les walkthroughs portent déjà le
> détail. Le job d'un prompt est de **pointer l'agent vers le doc autoritaire** et d'imposer
> la **Definition of Done** de la roadmap — pas de tout réexpliquer. Un prompt qui recopie le
> schéma est un prompt qui divergera du schéma.

---

## Carte de liaison (Roadmap ↔ Prompt ↔ Docs ↔ Statut)

| Epic / Story (ROADMAP) | Prompt | Lit en premier | Statut |
|---|---|---|---|
| 0 · stratégie d'erreur + 2.x wired | **P-WIRED** | DATA-MODEL §6 | ⬛ priorité n°1 |
| 0.1 / 2.4 · harnais de test | **P-TEST** | DATA-MODEL, vertical-slice.md | ⬛ à faire |
| 2 · permissions/modération | *(prompt déjà exécuté)* | — | ✅ sur `main` |
| 3 · migration WebApi ASP.NET | *(prompt déjà fourni)* | — | 🟧 branche `feat/webapi-aspnetcore-migration` |
| 5 · groupes | **P-GROUPS** | DATA-MODEL §2 | ⬛ à faire |
| 4 · rentable space | **P-RENTABLE** | DATA-MODEL §3 | ⬛ à faire |
| (gap) · pets | **P-PETS** | DATA-MODEL §4 | ⬛ à faire |
| (gap) · bots | **P-BOTS** | DATA-MODEL §5 | ⬛ à faire |
| 1 / 4 / 5 · remplissage handlers | **P-FILL** (template) | request-lifecycle.md, add-a-feature.md | ⬛ répétable |
| 8 · metrics tiering | **P-METRICS** | DATA-MODEL §8 | ⬛ à faire |

**Ordre d'exécution conseillé :** P-WIRED → P-TEST → P-GROUPS → P-RENTABLE → P-PETS/P-BOTS →
P-FILL (par domaine) → P-METRICS. (P-WIRED d'abord : c'est le seul qui corrige une perte de
données actuelle.)

**Contrat « lire d'abord » commun à TOUS les prompts :** `CONTEXT.md` (frontières),
`AGENTS.md` (definition of done du repo), et le walkthrough `docs/walkthroughs/request-lifecycle.md`
(le flux handler→grain→stream). Les prompts ci-dessous l'ajoutent en une ligne, ne le répète pas.

---

## P-WIRED — Corriger la persistance wired (PRIORITÉ N°1)

```
Implémente : ROADMAP Epic 0 (stratégie d'erreur) + le constat wired.
Lis d'abord : DATA-MODEL.md §6, puis CONTEXT.md et docs/walkthroughs/request-lifecycle.md.

Problème (déjà diagnostiqué, voir §6) : FurnitureWiredLogic utilise
StuffPersistanceType.RoomActive, donc la config wired n'est PAS écrite en DB et se perd au
déchargement de room / reboot. La colonne extra_data existe déjà et WiredData est déjà
sérialisable JSON. Ce n'est pas un schéma à créer, c'est un mode de persistance à corriger.

À faire :
1. Faire persister la config wired : la furni wired sérialise son WiredData dans extra_data
   et le recharge à l'activation de la room.
2. Distinguer config (à persister : IntParams, StuffIds, StuffIds2, StringParam, VariableIds,
   sources) de l'état runtime éphémère (compteurs « a déjà déclenché ») qui reste en mémoire.
3. Choisir le bon mode : la donnée joueur durable doit être Persistent, pas RoomActive.

Definition of Done : un test prouve le round-trip — poser un wired, le configurer, décharger
la room, la recharger → la config est identique. Aucune autre furni n'est impactée.
```

---

## P-TEST — Harnais de test sur les policies pures

```
Implémente : ROADMAP Epic 0.1 / Story 2.4.
Lis d'abord : docs/patterns/vertical-slice.md, le projet existant Turbo.WebApi.Tests (à
calquer), puis CONTEXT.md.

À faire :
1. Nouveau projet Turbo.Permissions.Tests (xunit + FluentAssertions, comme Turbo.WebApi.Tests).
2. Couvrir RoomSecurityPolicy.ResolveControllerLevel : System, superuser, ModerateAny,
   BuildAny, owner explicite, rights, none.
3. Couvrir ModerationPolicy.IsAllowed : chaque action × capability spécifique × ModerateAny ×
   wildcard × refus.
4. Ces tests tournent dans le quality gate.

Definition of Done : les branches de décision des deux policies sont couvertes, le gate
exécute les tests. Échec d'un cas = build rouge.
```

---

## P-GROUPS — Entités groupes + migration

```
Implémente : ROADMAP Epic 5 / Story 5.2.
Lis d'abord : DATA-MODEL.md §2 (source autoritaire des tables), puis CONTEXT.md.

À faire — générer EXACTEMENT les entités définies en §2, dans la convention du repo
(TurboEntity, [Table]/[Column] snake_case, FK + required, enums dans
Turbo.Primitives.Groups.Enums) :
- GroupEntity, GroupMemberEntity, GroupMembershipRequestEntity, GroupForumSettingsEntity,
  GroupForumThreadEntity, GroupForumPostEntity.
- Ajouter group_id (nullable) à RoomEntity.
- Les enums du §2.8.
- La migration EF correspondante.
- Fluent API : OnDelete non-cascade sur la relation circulaire groups.room_id ↔ rooms.group_id
  (voir l'avertissement §2.7).
- À la création d'un groupe, créer la ligne group_forum_settings avec ses defaults
  (invariant : 1 ligne par groupe).

Ne PAS recréer les tables messenger (amis existent déjà, §1). Ne PAS mettre les permissions
de forum sur GroupEntity (elles vont dans GroupForumSettingsEntity, décision §2.4).

Definition of Done : les entités compilent, la migration applique proprement, l'OnDelete
non-cascade est en place, un test crée un groupe et vérifie sa ligne de settings.
```

---

## P-RENTABLE — Rentable space (entités + logique)

```
Implémente : ROADMAP Epic 4 (économie).
Lis d'abord : DATA-MODEL.md §3 (entités + mapping packet + règles source WIN63), puis
CONTEXT.md et docs/walkthroughs/request-lifecycle.md.

À faire :
- Entités §3 : RentableSpaceTermsEntity (termes sur le TYPE de furni), RoomRentableSpaceEntity
  (état, 1 ligne/instance, MAJ en place), tag rentable_space_furniture_id sur FurnitureEntity.
  Migration + FK.
- Handlers rentSpace / cancelRent / getRentableSpaceStatus (furni id), câblés selon le flux
  handler→grain. Le statut renvoie les champs du mapping §3.2.
- Règles (source §3.4) : 1 locataire/espace ; 1 espace loué/joueur (vérifié via l'index
  renter_player_id) ; annulation par owner de la furni OU staff niveau ≥ requis.
- À la location : débiter via le ledger (economy_ledger) — c'est aussi l'historique, pas de
  table dédiée (décision §3).
- À l'expiration : rendre tous les meubles où rentable_space_furniture_id = X à l'inventaire
  du locataire, puis null renter/rented_until. Bornes de placement vérifiées dans le grain.

Definition of Done : louer/annuler/expirer fonctionne, chaque location est une entrée de
ledger, un joueur ne peut louer qu'un espace, les meubles reviennent à l'expiration. Tests sur
ces invariants.
```

---

## P-PETS — Entités pets

```
Implémente : ROADMAP (gap pets).
Lis d'abord : DATA-MODEL.md §4 (pet + food + sous-systèmes), puis CONTEXT.md.

À faire :
- PetEntity (§4) dans la convention du repo + migration. Stats avec defaults (level 1, xp 0,
  respect 0). room_id nullable. Gender via AvatarGenderType.
- PetFoodEntity (§4.1) : config food/produit (furniture_definition_id, pet_type, nutrition).
- Logique de nourrissage (grain) : pet va vers la food → nutrition += pet_food.nutrition →
  l'item-food est consommé.
- Breeding optionnel : parent_one_id/parent_two_id si visé.

NE PAS modéliser commandes/niveaux/palettes maintenant (§4.2) — ce sont des chantiers
séparés, food d'abord.

Definition of Done : un pet peut être créé, placé, remis en inventaire, et nourri (nutrition
monte, food consommée). Test sur le nourrissage.
```

---

## P-BOTS — Entités bots

```
Implémente : ROADMAP (gap bots).
Lis d'abord : DATA-MODEL.md §5, puis CONTEXT.md.

À faire : générer BotEntity + BotMessageEntity (§5) + l'enum BotMessageMode, dans la convention
du repo + migration. Commencer par identité + position + messages (random/keyword) ; ne PAS
faire le serving d'items sauf si explicitement visé (table bot_serve_items optionnelle, §5.2).

Definition of Done : entités compilent, migration applique, un bot peut être posé avec des
messages.
```

---

## P-FILL — Remplir les handlers d'un domaine (TEMPLATE répétable)

```
Implémente : ROADMAP Epic 1 (cœur jouable) / 4 / 5, domaine = <DOMAINE>  (ex: Navigator, Room,
Inventory, RoomSettings…).
Lis d'abord : docs/walkthroughs/request-lifecycle.md ET add-a-feature.md (le flux et le
placement), docs/glossary.md au besoin, puis CONTEXT.md.

À faire pour le domaine <DOMAINE> :
1. Lister tous les handlers stub de Turbo.PacketHandlers/<DOMAINE> (ceux en
   `await ValueTask.CompletedTask`).
2. Les implémenter un par un selon le flux du walkthrough : handler = orchestration only,
   résout le grain, délègue ; logique dans le grain/module ; broadcast via
   SendComposerToRoomAsync ; jamais de DB ni de socket dans le handler.
3. Gater chaque action sensible sur une capability (IPermissionService / RoomSecurityModule),
   jamais sur un flag client.
4. Auditer les actions à enjeu (event observability).

Definition of Done (ROADMAP §5) : aucun nouveau stub ; le compteur de stubs du domaine a
baissé ; les actions sensibles sont gatées et auditées ; le chemin jouable du domaine marche
de bout en bout.

⚠ Lancer ce prompt UN domaine à la fois. Commencer par Navigator (8% — la porte d'entrée),
puis Room, puis RoomSettings, puis Inventory.
```

---

## P-METRICS — Tiering du stockage observability

```
Implémente : ROADMAP Epic 8.
Lis d'abord : DATA-MODEL.md §8, puis l'archi observability existante (Turbo.Observability).

À faire (décision §8 : NE PAS fusionner les tables, tiérer) :
1. Garder en MySQL : economy_ledger, item_events, audit_events (vérité transactionnelle).
2. Router la télémétrie haut volume (perf, métriques serveur) vers le meter
   System.Diagnostics.Metrics déjà en place → exporter OTel/Prometheus.
3. Évaluer la suppression de performance_logs (logging client legacy, flash_version) au profit
   de la vraie observability.
4. Appliquer la rétention/purge RGPD sur audit_events (Epic 7 / Story 7.1).

Definition of Done : la télémétrie haut volume ne tape plus la DB transactionnelle, la
rétention d'audit est en place, performance_logs est tranchée (gardée justifiée ou supprimée).
```

---

## Note de maintenance

Quand une table change dans DATA-MODEL.md ou une story dans ROADMAP.md, **le prompt n'a pas à
changer** — il pointe vers le doc. C'est l'intérêt de la liaison : une seule source de vérité
par sujet. Mettre à jour la carte de liaison ci-dessus (colonne Statut) au fil de l'avancement.
```

