# Audit des écarts silencieux — wired et coutures de déclaration

- **Révision auditée** : `04550328f8d696358d477cc835c7d86dffb2b8bc` (`main`) + `d88948d` (rapport de bêta), branche `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date** : 2026-09-15.
- **Question posée** : « il y a pas mal de soucis au niveau wired et tout qu'on retrouve encore, ce n'est pas normal, j'ai l'impression que plus on ajoute plus c'est la perte, on oublie des trucs tout cons. »
- **Nature** : audit en lecture seule du code, plus un contrôle mécanique livré avec le rapport (`scripts/hooks/check-inert-declarations.mjs`). Aucun code de production modifié.

---

## 1. La réponse en une page

L'impression est juste, et elle a une cause précise et unique.

Il ne s'agit pas de négligence ni d'un système wired « fragile ». Il s'agit d'**une seule classe de bug**, qui revient parce que rien ne la surveille :

> **Une déclaration d'un côté d'une couture, une implémentation de l'autre, aucun compilateur au milieu, et le silence en cas de désaccord.**

La boîte se pose, se configure, se sauvegarde, le build est vert, les tests passent — et rien ne se produit. Aucune exception, aucun log d'erreur, aucun paquet de refus. Le seul moyen de s'en apercevoir est qu'un joueur, ou vous, essayiez la fonctionnalité à la main.

Ce n'est pas une intuition. Sur les 51 commits de l'historique visible, **19 corrigent un cas de cette classe** ; sur les 22 commits `wired:`, **12**. Les titres le disent eux-mêmes :

| Commit | Ce qui était déclaré | Ce qui manquait en face |
|---|---|---|
| `482acb1` | `CanWriteValue` sur `@type` et `@position.x` | l'écriture : le client proposait les lignes, les sélectionner ne faisait rien |
| `0455032` | huit lectures de niveau | le drapeau : visibles, jamais sélectionnables |
| `c38f813` | les mêmes lectures | `CanReadCreationTime` / `CanReadLastUpdateTime` |
| `2ccb45b` | 21 règles de paramètres | le client en envoie 22 : **toute** la sauvegarde était refusée |
| `7c2ad33` | des règles fixes | le client envoie un nombre variable |
| `0aed855` | une boîte à variables | `GetMaxVariableIds()` valait 0 : une seule sur trois arrivait |
| `5ae3929` | le dialogue de variable de référence | la section 4 du contexte n'était jamais émise : dialogue grisé |
| `4163006` | la variable echo | lisait bien, avalait toute écriture |
| `f17e75e` | l'onglet des variables de contexte | les lectures : il ne pouvait répondre que zéro |

Neuf bugs, une seule forme. Et **la même forme existe partout ailleurs dans le dépôt**, pas seulement dans le wired : un handler sans parseur, un composer sans sérialiseur, une clé de réglage absente du catalogue, une logique de furni non liée en base.

Et le savoir n'était pas manquant. Le walkthrough `add-a-wired-box.md` prévient déjà, en gras, que le nombre de règles de paramètres doit correspondre à ce que le client envoie « *or the config is silently refused* ». Ce texte existait le 12 septembre ; les deux bugs de ce type sont partis les 13 et 14. **Le problème n'est donc pas ce que vous savez, c'est que rien ne le vérifie à votre place** (§7).

Le dépôt a déjà compris le problème **au cas par cas** : sept contrôles mécaniques existent (`scripts/hooks/check-*.mjs`), et chacun a été écrit après un incident. Ce qui manque, c'est le même réflexe appliqué aux coutures qui n'en ont pas encore — et un filet qui fonctionne (voir §7 : trois des contrôles existants sont cassés, donc la barrière est contournée depuis huit commits).

**Ce que je livre avec ce rapport** : `scripts/hooks/check-inert-declarations.mjs`, qui couvre six coutures d'un coup, tourne en 0,9 s, et **retrouve exactement les bugs historiques quand on le lance sur les commits d'avant leur correction** (preuve en §6).

---

## 2. Les six coutures, et qui les surveille

Une « couture » est un endroit où deux moitiés d'une même fonctionnalité sont écrites séparément et doivent se correspondre exactement.

| # | Couture | Autorité de référence | Effet d'un désaccord | Surveillé avant cet audit |
|---|---|---|---|---|
| 1 | Nombre de règles `GetIntParamRules()` ↔ index lus à l'exécution | le code lui-même | le paramètre vaut toujours son défaut | non |
| 2 | Drapeaux `WiredVariableFlags` ↔ méthodes implémentées | le code lui-même | la ligne est proposée au joueur et ne fait rien | test sur la **seule** bande User |
| 3 | Contrat entrant ↔ parseur mappé ↔ handler | le code lui-même | le handler ne peut jamais s'exécuter | non |
| 4 | Composer construit ↔ sérialiseur mappé | le code lui-même | le paquet disparaît à l'encodeur | non |
| 5 | Clé lue depuis `IServerConfigGrain` ↔ `ConfigKeyCatalog` | le code lui-même | le réglage existe, aucun opérateur ne le voit | non |
| 6 | Boîte wired déclarée ↔ comportement qui lit sa configuration | le code lui-même | la boîte se configure et ne fait rien | non |
| **7** | **Règles de paramètres ↔ ce que le client envoie réellement** | **le client officiel, hors dépôt** | **toute la sauvegarde est refusée en silence** | **impossible en l'état** |
| 8 | Clé `[RoomObjectLogic]` ↔ menu déroulant admin | le code | la logique existe, personne ne peut la choisir | `check-logic-groups.mjs` |
| 9 | Id d'en-tête mappé ↔ registre du client | le client | l'id s'enregistre et ne peut jamais arriver | `check-header-registry.mjs` (dégradé) |
| 10 | Capacité dashboard ↔ 4 fichiers | le code | la page est invisible pour tout opérateur | `check-dashboard-capabilities.mjs` |
| 11 | `furniture_definitions.logic` ↔ classe de logique | **la base de données** | le mobi n'a pas le comportement attendu | métrique seulement, et aveugle au cas wired |

Les six premières sont désormais couvertes par le contrôle livré. La 7 et la 11 demandent une décision (§5 et §4.4).

---

## 3. État du wired aujourd'hui : ce qui est propre

Il faut le dire clairement, parce que l'impression de « tout se dégrade » ne correspond pas à ce que le code montre : **le wired est en bon état sur trois des quatre coutures internes.**

| Vérification | Résultat à HEAD |
|---|---|
| 173 boîtes concrètes : un index de paramètre lu hors des règles déclarées | **0** |
| 45 lectures de variables concrètes : un drapeau sans implémentation (ou l'inverse) | **0** |
| 23 déclencheurs : un événement de room que personne ne publie | **0** |
| 394 composers construits : un sans sérialiseur mappé | **0** |
| Boîtes avec un bloc d'implémentation commenté | **1** |

Les corrections récentes ont réellement fermé ce qu'elles visaient. Le problème n'est pas que le wired se dégrade : c'est qu'il n'existait **aucun moyen de savoir** qu'une régression de cette forme était réapparue, sauf à retomber dessus.

---

## 4. Ce qui reste ouvert

### 4.1 Deux boîtes wired inertes (confirmé par le code)

**`wf_slc_furni_with_var` — « sélectionner les mobis qui portent cette variable »**
`Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/Selectors/WiredSelectorItemsWithVariable.cs`

La boîte est complète côté configuration : cinq règles de paramètres, une source de mobis, une source de joueurs, et le contexte `AllVariablesInRoom` qui alimente le sélecteur de variables du client. Elle se pose, se configure, se sauvegarde.

`SelectAsync` (ligne 38 et suivantes) est un **bloc commenté de 85 lignes** et retourne une sélection vide. Le code commenté ne compile pas — il contient `if(_wiredData.Vari)` et `_ctx.Furni.GetVariableById()` sans argument : quelqu'un a commencé l'implémentation, l'a commentée pour que le build passe, et la boîte est partie ainsi.

Second défaut sur la même boîte : elle n'override pas `GetMaxVariableIds()`, dont le défaut est `0` (`FurnitureWiredLogic.cs:557`). `GetValidVariableIds` plafonne donc à zéro : même avec le corps décommenté, **aucun identifiant de variable ne survivrait à la sauvegarde**. C'est exactement le bug corrigé par `0aed855` sur une autre boîte, resté ici.

*Effet joueur* : toute chaîne qui utilise ce sélecteur sélectionne zéro mobi, sans message.
*Correction* : implémenter `SelectAsync` et déclarer `GetMaxVariableIds() => 1`, ou retirer la boîte du registre tant qu'elle n'est pas écrite (comme le fait `WiredTriggerHabboPerformsAction`, qui documente son inertie au lieu de la cacher).

**`wf_xtra_mov_carry_users` — l'add-on « emporter les joueurs »**
`Vortex.Rooms/Object/Logic/Furniture/Floor/Wired/Addons/WiredAddonCarryUsers.cs`

`FillInternalDataAsync` lit soigneusement le paramètre dans `_carryUserType`. `MutatePolicyAsync` retourne `true` sans jamais le lire, et **`_carryUserType` n'est lu nulle part ailleurs dans le dépôt** (vérifié par recherche sur tout l'arbre). L'add-on est configurable et sans effet : les avatars posés sur un mobi qui bouge ne sont pas emportés.

### 4.2 Six handlers qui ne peuvent jamais s'exécuter

La chaîne entrante a trois maillons : un contrat de message, un parseur mappé sur un id d'en-tête, un handler. Six handlers ont leur contrat et leur parseur **écrits**, et aucune ligne `MapParser` :

| Message | Verdict |
|---|---|
| `SetRelationshipStatusMessage` | **vrai manque.** L'id existe (`Headers.cs:636`, valeur 1773), le parseur existe, le handler existe ; seule la ligne dans `UsersMap.cs` manque — seul le *Get* y est mappé (`UsersMap.cs:84`). Un joueur peut lire les statuts de relation de ses amis et **jamais en définir un**. |
| `ChargeFireworkMessage` | **vrai manque.** Handler et parseur écrits, aucun id d'en-tête, aucun mappage. Charger un feu d'artifice n'atteint pas le serveur. |
| `GetTargetedOfferMessage` | poids mort. `GetNextTargetedOffer` (id 848) est mappé et fait le même travail ; le triplet est un doublon. |
| `GiveStarGemToUserMessage` | poids mort assumé : `Headers.cs:282` porte `= -1 // REMOVED in 2026`. |
| `class_165Message`, `class_200Message` | classes client non identifiées, jamais mappées. À nommer depuis le client ou à supprimer. |

Deux fonctionnalités réellement perdues, quatre morceaux de code mort. Le détecteur les trouve en 0,9 s ; rien ne les avait trouvés jusqu'ici.

### 4.3 Quarante-huit réglages que personne ne peut régler

`ConfigKeyCatalog` est ce que l'éditeur de configuration du dashboard affiche. Quarante-huit clés sont lues depuis `IServerConfigGrain` et **absentes du catalogue** :

| Famille | Clés | Fichier |
|---|---|---|
| Animaux | 22 | `Vortex.Rooms/Configuration/PetTuning.cs` |
| Pêche | 16 | `Vortex.Fishing/FishingConfig.cs` |
| Modération | 4 | `Vortex.PacketHandlers/Configuration/ModerationConfig.cs` |
| Quêtes | 3 | `Vortex.Progression/Grains/PlayerQuestGrain.cs` |
| Collectibles, vêtements, achievements | 3 | divers |

L'ironie est dans les commentaires du code lui-même. `PetTuning.cs:8` : « *so they can be retuned from the dashboard without a restart* ». `FishingConfig.cs:11-19` : « *exactly what `IServerConfigGrain` exists for* ». L'intention est écrite, la moitié qui la réalise manque. Les jeux de room (Freeze, Banzai, football) ont, eux, leurs clés au catalogue — parce qu'un walkthrough l'impose (`docs/walkthroughs/add-a-room-game.md`, étape 3). La différence entre les deux familles n'est pas le soin apporté : c'est l'existence d'une checklist.

*Correction* : un descripteur par clé dans `ConfigKeyCatalog.All`. Une heure de travail, et le contrôle livré empêche la prochaine de s'échapper.

### 4.4 La liaison mobi → logique, qui vit en base et que rien ne vérifie

`scripts/sql/wired_logic_binding_fix.sql` le dit dans ses propres mots :

> « Sixty do not — they say `furniture_multistate`, which is the generic "it has states" logic — so those boxes attach no wired behaviour and do nothing when a trigger reaches them. **Nothing reports it: an unresolved logic falls back silently.** »

Et le seed `furni_logic_bindings.sql` raconte la même histoire un cran plus tôt : « *53 782 of 55 279 definitions resolved to nothing and fell through to the family default. Silently.* »

La métrique `Vortex.furniture.logic.fallback` (`VortexMetrics.cs:126`, émise par `RoomObjectLogicProvider.cs:143`) couvre le cas « logique introuvable ». Elle est **aveugle au cas wired** : une boîte `wf_act_*` dont la colonne `logic` dit `furniture_multistate` résout parfaitement — vers une logique qui n'est pas une logique wired. Aucun repli, aucun compteur, aucun log.

*Correction proposée, cinq lignes* : à l'hydratation d'un objet de room, si le nom de la définition correspond à `^wf_(act|cnd|trg|slc|xtra|var)_` et que la logique résolue n'est pas un `IWiredBox`, émettre un avertissement et un compteur. Le silence devient visible, sans base de données à interroger.

*Vérification à faire de votre côté* (aucun MySQL dans l'environnement d'audit) :

```sql
SELECT d.name, d.logic, COUNT(*) AS boites
FROM furniture_definitions d
WHERE d.name REGEXP '^wf_(act|cnd|trg|slc|xtra|var)_'
  AND d.logic <> d.name
GROUP BY d.name, d.logic
ORDER BY d.name;
```

---

## 5. La couture la plus coûteuse, et pourquoi aucun outil ne peut la voir

C'est celle qui a produit `2ccb45b` et `7c2ad33`, et c'est la seule dont l'autorité n'est pas dans le dépôt.

`TryNormalizeIntParams` (`FurnitureWiredLogic.cs:636-708`) est brutal, et il a raison de l'être :

```csharp
if (tailRule is null)
{
    if (proposed.Count != fixedRules.Count)
    {
        return false;   // -> ApplyWiredUpdateAsync renvoie false -> rien n'est sauvegardé
    }
```

Un écart d'**une** unité entre le nombre de règles déclarées et le nombre de paramètres que le client envoie, et la boîte entière refuse de se sauvegarder. Pas ce paramètre-là : la boîte entière. Et le refus est muet — `docs/habbo-specs/unknowns/medium/uk_91fc7e0d6b.yaml` enregistre que ce que le serveur officiel répond à une sauvegarde wired refusée est **inconnu**, donc Vortex ne répond rien.

Le nombre attendu est décidé par les classes de configuration du client officiel, sous `com/sulake/habbo/roomevents/wired_setup/`. Ce répertoire **est déjà lu** par le dépôt : `Vortex.Specs/Completeness/WiredSurface.cs:104-109` y ouvre `triggerconfs/TriggerConfCodes.as`, `actiontypes/ActionTypeCodes.as` et leurs quatre équivalents — pour en extraire la liste des codes, jamais le nombre de paramètres.

Deux façons de fermer la couture, par ordre de coût :

**A. Depuis la base (faisable aujourd'hui, sans le client).** La configuration wired est persistée en JSON dans `furniture.extra_data`, section `wired` (`ExtraDataSectionType.WIRED = "wired"`). Les boîtes déjà configurées par de vrais clients disent donc combien de paramètres le client envoie :

```sql
SELECT d.name AS boite,
       JSON_LENGTH(f.extra_data, '$.wired.IntParams') AS params_envoyes,
       COUNT(*) AS occurrences
FROM furniture f
JOIN furniture_definitions d ON d.id = f.definition_id
WHERE d.name REGEXP '^wf_'
  AND JSON_EXTRACT(f.extra_data, '$.wired') IS NOT NULL
GROUP BY boite, params_envoyes
ORDER BY boite, params_envoyes;
```

Toute ligne dont `params_envoyes` diffère du nombre de règles déclarées par la classe correspondante est une boîte qui ne se sauvegarde pas — ou qui ne se sauvegardait pas au moment où la donnée a été écrite. *(Requête non exécutée : pas de MySQL dans l'environnement d'audit ; les noms de table et de colonne viennent des attributs `[Table]`/`[Column]` des entités, à confirmer sur votre schéma.)*

**B. Depuis le client (le contrôle durable).** Étendre `WiredSurfaceAnalyzer` pour lire, à côté des fichiers `*Codes.as` qu'il ouvre déjà, la classe de configuration de chaque boîte et compter les paramètres entiers qu'elle collecte ; écrire le résultat dans la surface générée ; ajouter au contrôle livré une septième couture qui compare ce nombre à `GetIntParamRules().Count`. Le travail se fait contre les sources réelles du client, qui sont sur votre machine et pas ici — je ne peux pas l'écrire à l'aveugle, mais l'emplacement d'accroche est précis : `Vortex.Specs/Completeness/WiredSurface.cs`, la même boucle qui construit aujourd'hui `WiredBoxFamily`.

Tant que l'une des deux n'existe pas, **chaque nouvelle boîte wired est un pari sur un nombre**, et le pari se révèle quand un joueur signale que sa boîte ne garde pas ses réglages.

---

## 6. Ce qui est livré, et la preuve que ça marche

`scripts/hooks/check-inert-declarations.mjs` (+ `inert-declarations-baseline.json`).

Six coutures, une passe, **0,9 seconde**, aucune dépendance (node nu, comme les sept autres contrôles du dépôt). Sortie actuelle :

```
check-inert-declarations: OK (185 wired boxes, 86 variable readings, 561 handlers,
394 composers, 62 config keys; 56 known gap(s) baselined).
```

Même idiome que les contrôles existants : cliquet avec baseline, `--update` pour accepter un écart, `--all` pour tout voir, sortie 2 sur un écart **nouveau**. Les 56 écarts d'aujourd'hui sont dans la baseline **avec une note chacun**, dont deux marquées explicitement `OPEN BUG, not accepted debt` (les deux boîtes inertes du §4.1) : la baseline est là pour permettre l'adoption immédiate, pas pour enterrer le travail.

**La preuve de régression.** Lancé sur l'arbre du commit qui précède la correction de `482acb1` :

```
[wired-variable-flags]
  @position.x   declares CanWriteValue with no write behind it: the picker offers it,
                selecting it does nothing
  @type         declares CanWriteValue with no write behind it: …
```

Exactement les deux lectures que le message de `482acb1` décrit, trouvées en une seconde, alors qu'il avait fallu lire le client pour s'en apercevoir.

**Adoption** : une ligne dans `Directory.Build.targets`, dans la cible `VortexCloudFastCheck`, à côté des cinq contrôles qui y sont déjà.

```xml
<Exec Command="node scripts/hooks/check-inert-declarations.mjs"
      WorkingDirectory="$(MSBuildThisFileDirectory)" />
```

Je ne l'ai pas ajoutée : la cible est aujourd'hui rouge pour d'autres raisons (§7) et y greffer un contrôle de plus n'aiderait pas tant que la barrière est contournée.

---

## 7. Pourquoi le motif était inévitable, et le reste tant que la barrière est cassée

Deux causes, l'une structurelle, l'autre conjoncturelle.

**La surface d'enregistrement.** Ajouter une boîte wired demande de toucher, au minimum : la classe de logique et son attribut, le `WiredCode`, les règles de paramètres (dont le nombre vient du client), éventuellement `GetMaxVariableIds`, `GetAllowedFurniSources`, `GetAllowedPlayerSources`, `GetWiredContextSnapshots`, le menu déroulant admin, la ligne de catalogue, et la colonne `logic` en base. Dix endroits, dont **un seul** était vérifié mécaniquement (le menu).

Et voici le fait qui tranche la question, parce qu'il interdit la réponse facile (« il faut faire plus attention ») :

> `docs/walkthroughs/add-a-wired-box.md` existe. Son étape 3 s'intitule « *Declare the parameter rules, even if they look redundant* » et prévient, en gras : « *This is the one that bites, and it fails **silently on the operator's screen*** ». Le point 4 de sa checklist dit : « *One `IWiredParamRule` per int the form sends, or a tail rule — **or the config is silently refused***. »
>
> Ce texte existait avant le 12 septembre. Le 13 septembre, `2ccb45b` corrige deux boîtes qui ne pouvaient pas être sauvegardées pour cette raison exacte. Le 14 septembre, `7c2ad33` en corrige une troisième.

Le savoir était écrit, au bon endroit, en gras, avec le mode de défaillance nommé. Les bugs sont partis quand même, deux fois, dans les quarante-huit heures. **Une checklist est un rappel pour quelqu'un qui pense à la relire ; ce n'est pas un contrôle.** C'est la seule conclusion que cet historique autorise, et c'est pour cela que le livrable de cet audit est un script et pas un paragraphe de plus.

Le contraste avec les jeux de room va dans le même sens, mais par l'autre bout : leur walkthrough impose l'étape « les clés de balance dans `ConfigKeyCatalog` » et leurs clés y sont toutes ; les animaux et la pêche, sans walkthrough, n'y sont pas (§4.3). Ce qui décide n'est pas le soin : c'est qu'une étape soit vérifiée par une machine ou non.

**La barrière est débranchée.** C'est le constat QA-01 du rapport de bêta, toujours vrai au moment de cet audit, et il est la moitié de la réponse à « pourquoi on ne s'en aperçoit pas » :

| Contrôle | État aujourd'hui |
|---|---|
| `check-dashboard-capabilities.mjs` | sortie 0 |
| `check-architecture-walls.mjs` | sortie 0 |
| `check-logic-groups.mjs` | sortie 0 |
| `check-header-registry.mjs` | **sortie 2** (mode plafond, ignore sa propre baseline) |
| `check-wire-conflicts.mjs` | **sortie 2** (« the CLI output format changed, this check is blind ») |
| `scripts/hooks/__test/run.mjs` | **sortie 1** (3 auto-tests en échec) |

Donc `VortexCloudFastCheck` échoue quoi qu'on fasse, donc les commits partent en `--no-verify` (les messages de `4ca7c8a`, `482acb1` et `0455032` le disent), donc **aucun** des contrôles — y compris ceux qui marchent — ne s'exécute avant `main`. Écrire de nouveaux contrôles pendant que les anciens sont contournés ne sert à rien : la première chose à faire est de remettre les trois au vert.

Compilation et tests sont sains par ailleurs : `dotnet build Vortex.Cloud.sln` → 0 erreur ; suite complète → 0 échec (~3 900 tests, même chiffre qu'au 14 septembre).

---

## 8. Plan proposé

**Avant tout — remettre la barrière (1 jour).** Réparer les trois contrôles cassés, puis brancher `check-inert-declarations.mjs` dans `VortexCloudFastCheck`. Sans cette étape, tout le reste est décoratif. Critère : `dotnet build Vortex.Main/Vortex.Main.csproj -t:VortexCloudFastCheck` vert, et la CI verte sur les trois OS.

**Ensuite, par coût croissant :**

1. **Les 48 clés de configuration** (1 h). Un descripteur par clé dans `ConfigKeyCatalog.All`, puis `--update` de la baseline. Critère : le contrôle passe avec 8 écarts restants au lieu de 56.
2. **`SetRelationshipStatus` et `ChargeFirework`** (1 h). Pour le premier, une ligne `MapParser` dans `UsersMap.cs` — l'id et le parseur existent déjà. Pour le second, identifier l'id dans le client ou supprimer le triplet. Critère : définir un statut de relation depuis le client change la valeur affichée.
3. **Les deux boîtes inertes** (0,5 à 2 jours). `wf_xtra_mov_carry_users` : consommer `_carryUserType` dans le déplacement de mobi. `wf_slc_furni_with_var` : écrire `SelectAsync` et déclarer `GetMaxVariableIds() => 1`, ou retirer la boîte du registre en documentant pourquoi. Critère : un test qui pose la boîte, la configure et vérifie la sélection.
4. **La couture 7** (1 jour, option A ou B du §5). C'est celle qui rapporte le plus : elle transforme le bug le plus coûteux du système en erreur de build.
5. **Le repli wired silencieux** (2 h). Les cinq lignes du §4.4, plus une alerte sur le compteur.
6. **Le code mort** (1 h). Supprimer les quatre triplets injoignables, ou les documenter comme `WiredTriggerHabboPerformsAction` le fait — c'est le bon modèle : une boîte inerte qui **dit** qu'elle est inerte n'est plus un piège.
7. **Le walkthrough** (1 h). Il est bon et couvre déjà le piège principal ; il lui manque `GetMaxVariableIds` (le défaut à 0, qui a coûté `0aed855` et coûte encore `wf_slc_furni_with_var`) et l'entrée du menu admin. Y ajouter surtout, en tête de checklist, la ligne « le contrôle qui vérifie ceci est `check-inert-declarations` » : une checklist adossée à une machine est tenue, une checklist seule ne l'est pas.

---

## 9. Couverture et limites de cet audit

**Analysé mécaniquement, sur tout l'arbre** : 185 boîtes wired (paramètres, comportement), 86 lectures de variables (drapeaux), 23 déclencheurs (événements), 561 handlers et 555 parseurs (chaîne entrante), 394 composers (chaîne sortante), 62 clés de configuration, 260 clés `[RoomObjectLogic]`.

**Analysé à la lecture** : `FurnitureWiredLogic` (chemin de sauvegarde, normalisation, hydratation), les bases des six familles, les quatre bandes de variables et leurs bases, les deux boîtes inertes, les six handlers injoignables, `WiredSurface.cs`, `RoomObjectLogicProvider`, les scripts SQL de liaison.

**Non vérifié** :
- Le **moteur d'exécution** wired lui-même (`Vortex.Rooms/Wired/Engine/**` : cycles, profondeur, ordonnanceur, fenêtres d'exécution) — il a sa propre suite de tests et sa matrice de parité (`docs/architecture-v4/acceptance-matrix.md`), que je n'ai pas contre-vérifiée.
- Le **comportement réel** : aucun MySQL, aucun client, aucune exécution de l'émulateur dans cet environnement. Les deux boîtes inertes sont établies par lecture du code, pas par une partie jouée.
- Les **sources du client officiel**, absentes ici : la couture 7 et le contrôle des ids d'en-tête restent aveugles, et les 22 boîtes que le client sait configurer sans implémentation Vortex (`docs/completeness/generated/WIRED-BOXES.md`) n'ont pas été recomptées.
- Les familles wired **au-delà de leur enveloppe** : les 53 actions, 45 conditions et 21 sélecteurs ont été passés aux détecteurs, pas lus un par un. Un défaut de logique métier à l'intérieur d'une boîte correctement déclarée ne serait pas vu par cet audit.
