# Audit des écarts silencieux — wired et coutures de déclaration

- **Révision auditée** : `04550328f8d696358d477cc835c7d86dffb2b8bc` (`main`) + `d88948d` (rapport de bêta), branche `claude/vortex-cloud-beta-audit-apl5gn`.
- **Date** : 2026-09-15.
- **Question posée** : « il y a pas mal de soucis au niveau wired et tout qu'on retrouve encore, ce n'est pas normal, j'ai l'impression que plus on ajoute plus c'est la perte, on oublie des trucs tout cons. »
- **Client de référence** : `absolutezeroo/vortex-modern-client` à `cfeac29`, lu comme source (port TypeScript ; le dump AS3 `sources/` n'est pas commité).
- **Nature** : audit en lecture seule du code, plus deux contrôles mécaniques livrés avec le rapport (`scripts/hooks/check-inert-declarations.mjs`, `scripts/hooks/check-wired-param-counts.mjs`). Aucun code de production modifié.

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

**Ce que je livre avec ce rapport** : deux contrôles mécaniques, tous deux vérifiés par la seule preuve qui vaille — les relancer sur l'arbre d'avant la correction d'un bug réel et vérifier qu'ils le retrouvent.

- `check-inert-declarations.mjs` couvre six coutures d'un coup et tourne en 0,9 s. Sur le commit qui précède `482acb1`, il ressort exactement les deux lectures que ce commit corrige (§6).
- `check-wired-param-counts.mjs` ferme la septième, la plus coûteuse, en confrontant chaque boîte au client que vous avez fourni. Sur le commit qui précède `2ccb45b`, il ressort exactement les deux boîtes de ce commit, avec le bon écart, et rien d'autre (§5.4). **Il trouve aussi cinq boîtes cassées aujourd'hui, à HEAD** : `wf_trg_user_performs_action`, `wf_act_freeze_habbo`, `wf_slc_users_with_var`, `wf_slc_remote` et `wf_xtra_mov_physics` refusent chacune la totalité de leur configuration, en silence, à chaque sauvegarde (§5.3).

---

## 2. Les sept coutures, et qui les surveille

Une « couture » est un endroit où deux moitiés d'une même fonctionnalité sont écrites séparément et doivent se correspondre exactement.

| # | Couture | Autorité de référence | Effet d'un désaccord | Surveillé avant cet audit |
|---|---|---|---|---|
| 1 | Nombre de règles `GetIntParamRules()` ↔ index lus à l'exécution | le code lui-même | le paramètre vaut toujours son défaut | non |
| 2 | Drapeaux `WiredVariableFlags` ↔ méthodes implémentées | le code lui-même | la ligne est proposée au joueur et ne fait rien | test sur la **seule** bande User |
| 3 | Contrat entrant ↔ parseur mappé ↔ handler | le code lui-même | le handler ne peut jamais s'exécuter | non |
| 4 | Composer construit ↔ sérialiseur mappé | le code lui-même | le paquet disparaît à l'encodeur | non |
| 5 | Clé lue depuis `IServerConfigGrain` ↔ `ConfigKeyCatalog` | le code lui-même | le réglage existe, aucun opérateur ne le voit | non |
| 6 | Boîte wired déclarée ↔ comportement qui lit sa configuration | le code lui-même | la boîte se configure et ne fait rien | non |
| **7** | **Règles de paramètres ↔ ce que le client envoie réellement** | **le port TS du client, commité** | **toute la sauvegarde est refusée en silence** | **non — désormais `check-wired-param-counts.mjs`** |
| 8 | Clé `[RoomObjectLogic]` ↔ menu déroulant admin | le code | la logique existe, personne ne peut la choisir | `check-logic-groups.mjs` |
| 9 | Id d'en-tête mappé ↔ registre du client | le client | l'id s'enregistre et ne peut jamais arriver | `check-header-registry.mjs` (dégradé) |
| 10 | Capacité dashboard ↔ 4 fichiers | le code | la page est invisible pour tout opérateur | `check-dashboard-capabilities.mjs` |
| 11 | `furniture_definitions.logic` ↔ classe de logique | **la base de données** | le mobi n'a pas le comportement attendu | métrique seulement, et aveugle au cas wired |

Les six premières sont couvertes par `check-inert-declarations.mjs`, la septième par `check-wired-param-counts.mjs`. La 11 demande encore une décision (§4.4), et la 9 reste dégradée alors qu'elle n'a plus de raison de l'être (§9).

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
*Correction* : implémenter `SelectAsync` et déclarer `GetMaxVariableIds() => 1`, ou retirer la boîte du registre tant qu'elle n'est pas écrite en documentant l'inertie au lieu de la cacher — comme le fait `WiredTriggerHabboPerformsAction`. La *pratique* est la bonne ; dans ce cas précis, la justification écrite est fausse et le §5.3 la corrige.

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

## 5. La couture la plus coûteuse : fermée

C'est celle qui a produit `2ccb45b` et `7c2ad33`, et c'était la seule dont l'autorité n'était pas dans le dépôt. Elle l'est depuis que vous avez fourni le client.

`TryNormalizeIntParams` (`FurnitureWiredLogic.cs:636-708`) est brutal, et il a raison de l'être :

```csharp
if (tailRule is null)
{
    if (proposed.Count != fixedRules.Count)
    {
        return false;   // -> ApplyWiredUpdateAsync renvoie false -> rien n'est sauvegardé
    }
```

Un écart d'**une** unité entre le nombre de règles déclarées et le nombre de paramètres que le client envoie, et la boîte entière refuse de se sauvegarder. Pas ce paramètre-là : la boîte entière. Et le refus est muet, ce que j'ai maintenant suivi jusqu'au bout : `UpdateAddonMessageHandler.cs:28-40` (et ses cinq jumeaux) fait `return;` sur `false` — pas de composer d'erreur, et surtout **pas** le `WiredSaveSuccessEventMessageComposer` qu'il enverrait sinon. Le client ne reçoit rien du tout. `docs/habbo-specs/unknowns/medium/uk_91fc7e0d6b.yaml` enregistre que ce que le serveur officiel répond dans ce cas est **inconnu**, donc Vortex ne répond rien.

### 5.1 Où est vraiment l'autorité (et pourquoi ma première proposition était fausse)

La version précédente de ce rapport proposait de brancher le compte sur `WiredSurfaceAnalyzer`, qui lit déjà `com/sulake/habbo/roomevents/wired_setup/*Codes.as`. **C'était une erreur, et de la même famille que le défaut qu'elle prétendait corriger** : `vortex-modern-client/.gitignore`, ligne 14, ignore `sources/`. Le dump AS3 n'est jamais commité. Un contrôle accroché aux `.as` est donc aveugle partout sauf sur une machine où quelqu'un a décompressé le client à la main — c'est très exactement le mode de défaillance de `check-header-registry.mjs` et `check-wire-conflicts.mjs` (§7).

L'autorité commitée, c'est le port TypeScript : `packages/vortex-engine/src/habbo/roomevents/wired_setup/<famille>/**/<Boîte>.ts`, où chaque boîte porte

```ts
override readIntParamsFromForm(): number[]
{
    return [this._effectId.value, this._priority.value, this._type.selected];
}
```

— littéralement le tableau qui part sur le fil. Ce répertoire est versionné, donc le contrôle fonctionne partout où le client est cloné, CI comprise.

### 5.2 Le contrôle livré

`scripts/hooks/check-wired-param-counts.mjs` (+ `wired-param-counts-baseline.json`). Node nu, même idiome de cliquet que les autres. Il apparie les boîtes par `(famille, code)` — l'énumération `Wired*Type` côté Vortex, les constantes `*Codes` côté client — puis compare `GetIntParamRules().Count` au nombre d'entiers que le formulaire envoie, en tenant compte d'une règle de queue quand il y en a une.

```
check-wired-param-counts: OK (95 boxes compared against the client, 51 whose client form
builds its array from the operator's selection, 15 with no client configuration class;
5 known disagreement(s) baselined; --skipped lists what was not compared).
```

Deux subtilités ont dû être traitées pour que le compte soit juste, et elles valent d'être dites parce qu'elles expliquent pourquoi ce contrôle n'était pas trivial :

- **Le tableau n'est pas toujours un littéral.** Les six afficheurs *Variable FX* — précisément ceux de `2ccb45b` — construisent le leur par `params.push(...)` dans une méthode `writeIntParams` héritée, que la boîte dérivée surcharge pour ajouter un entier de plus. Le contrôle suit la chaîne d'héritage en dispatch virtuel (`super.` remonte, `this.helper()` repart de la classe la plus dérivée) et compte `Util.pushIntAsLong` pour deux.
- **Un push conditionnel n'est pas comptable.** Quand un `push` est dans un `if`, une boucle, un spread ou un `concat`, le nombre dépend de ce que l'opérateur a coché : le contrôle répond « inconnu » et ne compare pas, plutôt que de deviner. C'est le sens des 51 boîtes non comparées ; `--skipped` les liste.

### 5.3 Les cinq désaccords, vérifiés à la main des deux côtés

| Boîte | Client | Vortex | Ce que ça donne |
|---|---:|---:|---|
| `wf_trg_user_performs_action` | 1 | 0 | `UserPerformsAction.ts` envoie toujours le code de l'action ; aucune règle déclarée |
| `wf_act_freeze_habbo` | 2 | 0 | `FreezeUser.ts` envoie `[effet, annuler-au-téléport]` ; aucune règle déclarée |
| `wf_slc_users_with_var` | 5 | 1 | hérite de `VariableSelector.readIntParamsFromForm` (1 + 1 + un long sur deux slots + 1) ; une seule règle déclarée |
| `wf_slc_remote` | 2 | 0 | `RemoteSelector.ts` envoie `[type, compte]` ; aucune règle déclarée |
| `wf_xtra_mov_physics` | 4 | 0 | `MovePhysics.ts` envoie quatre booléens ; aucune règle déclarée |

Chacune de ces cinq boîtes **refuse la totalité de sa configuration, en silence, à chaque sauvegarde**. Le joueur ouvre la boîte, règle, sauvegarde, la fenêtre se ferme, rien n'est écrit.

**`wf_trg_user_performs_action` est le cas le plus instructif des cinq**, et il mérite son propre paragraphe, parce qu'il montre le motif se refermer sur celui qui l'avait vu.

La classe est délibérément vide, et son commentaire explique pourquoi :

> *« Deliberately inert: this client revision ships no configuration class for the code — `wired_setup/triggerconfs/` has one class per trigger and **none of them declares 16** — so the box cannot be given an action to watch for. »*

**Cette affirmation est fausse.** `triggerconfs/UserPerformsAction.ts` existe, retourne bien 16, et envoie un entier : le code de l'action (`wave`/`blow`/…/`sign`/`dance`), plus un `stringParam` pour l'index de pancarte ou de danse.

La cause de l'erreur est visible dans le client lui-même : la constante n'a pas de nom lisible. Elle s'appelle `TriggerConfCodes.TRIGGER_CODE_16`, parce qu'en AS3 elle est obfusquée en `_SafeStr_10352` et qu'aucun arbre ne porte son vrai nom. **Une recherche par nom** — `USER_PERFORMS_ACTION`, `AVATAR_PERFORMS_ACTION` — **ne trouve rien et conclut légitimement « le client ne la déclare pas ».** Une comparaison par valeur la trouve immédiatement. C'est précisément ce que fait le contrôle livré, qui apparie sur `(famille, code)` et jamais sur un nom.

Et les deux jumelles de cette boîte, que le client construit à partir du même formulaire, **sont implémentées et correctes** :

| | Client | Vortex | État |
|---|---:|---:|---|
| `wf_cnd_user_performs_action` (condition 32) | 1 | 1 | `Evaluate` écrit, règle déclarée |
| `wf_slc_users_byaction` (sélecteur 9) | 1 | 1 | `SelectAsync` écrit, règle déclarée |
| `wf_trg_user_performs_action` (déclencheur 16) | 1 | **0** | vide, sur une prémisse fausse |

Vortex sait donc déjà lire ce paramètre, à deux endroits. Le déclencheur est le seul des trois à avoir été abandonné, et il l'a été pour une raison qui n'existe pas. *Correction* : déclarer la règle, câbler l'événement, et supprimer le commentaire — ou, au minimum, corriger le commentaire, car tel quel il décourage activement quiconque voudrait finir le travail.

Trois précisions qui changent la priorité des quatre autres :

- **`wf_act_freeze_habbo` est la plus coûteuse des cinq** : son `ExecuteAsync` est écrit et fonctionne (il verrouille le déplacement via `_ctx.Game.LockMovement`). Le comportement existe, il est simplement inatteignable. Son propre commentaire de classe affirme « *No int params* » — c'est cette phrase que le client contredit. Déclarer les deux règles rend la boîte sauvegardable ; *honorer* l'effet et le drapeau d'annulation est un travail distinct, à ne pas confondre.
- **`wf_slc_users_with_var` est un cas d'asymétrie** : son jumeau `wf_slc_furni_with_var` déclare bien ses 5 règles, et le contrôle le confirme (`client=5 vortex=5`). Les deux boîtes lisent le même formulaire côté client ; une seule a été mise à jour. C'est la signature exacte du motif de cet audit.
- **`wf_xtra_mov_physics` et `wf_slc_remote` n'ont aucun comportement** au-delà de `WiredCode` : corriger le compte seul ne changerait rien de visible. Elles rejoignent le §4.1.

Les cinq sont dans la baseline avec, chacune, une note qui dit ce qu'elle est. L'entrée `_` de ce fichier le dit en toutes lettres : *« Every entry here is an OPEN BUG, not accepted debt »*. L'empreinte inclut les deux nombres (`client=5:vortex=1`), donc si l'un bouge, l'entrée redevient neuve et le contrôle rebloque — baseliner 21-contre-22 ne couvre pas 22-contre-23.

### 5.4 La preuve de régression

C'est le test qui compte, parce qu'un contrôle écrit après coup trouve toujours ce qu'on lui a montré. Avec **la baseline d'aujourd'hui**, lancé sur l'arbre du commit qui précède `2ccb45b` :

```
check-wired-param-counts: 2 box(es) whose rule count disagrees with the client.
  wf_xtra_fx_levelling_progress    addon/1202  client=22  vortex=21
  wf_xtra_fx_number_display        addon/1205  client=22  vortex=21
exit=2
```

Exactement les deux boîtes que ce commit corrige, exactement le bon écart, et rien d'autre. Le bug aurait été arrêté avant d'être écrit.

Une honnêteté nécessaire sur la portée : `7c2ad33` ne serait **pas** attrapé par ce contrôle. Ce bug-là n'était pas un désaccord de compte mais une lecture au-delà des règles fixes, côté exécution — c'est la couture de la règle de queue, pas celle du nombre. Ce contrôle ferme la couture de `2ccb45b`, qui est la plus coûteuse des deux, pas les deux.

### 5.5 Ce que ce contrôle a failli me faire dire

Sa première version a rapporté **33 désaccords**, dont 30 de la forme `vortex = client + 1`. La cause : elle comptait les virgules de premier niveau, et une expression de collection C# accepte une virgule finale — `[a, b, c,]` a trois éléments et trois virgules. Un contrôle écrit pour compter des paramètres était lui-même décalé d'une unité, c'est-à-dire atteint du bug exact qu'il cherche.

Il est resté non publié parce que `wf_act_give_effect` servait de témoin : j'avais lu ses deux côtés plus tôt et je savais qu'ils valaient 3 tous les deux, or le script annonçait 4. Aucun des 33 n'a été rapporté avant que le témoin passe au vert. Les 5 qui restent ont ensuite été relus un par un, fichier contre fichier, avant d'entrer dans ce tableau.

La leçon n'est pas anecdotique : elle vaut pour n'importe quel détecteur que vous ajouterez. **Un contrôle a besoin d'un témoin connu-bon avant que sa sortie vaille quelque chose.** Les deux boîtes *Variable FX* et `wf_slc_furni_with_var` jouent ce rôle ici en permanence, puisqu'elles doivent rester à `22/22`, `21/21` et `5/5`.

### 5.6 Ce que ce contrôle ne voit pas

- **51 boîtes** dont le formulaire client construit son tableau selon ce que l'opérateur a coché. Leur arité varie légitimement ; comparer un nombre fixe n'a pas de sens. Celles-là ont besoin d'une règle de queue côté Vortex, et c'est cette *présence* qu'il faudrait vérifier, pas le compte — extension naturelle du contrôle, non faite.
- **15 boîtes** sans classe de configuration côté client : soit des boîtes propres à Vortex, soit un code qui a bougé. `--skipped` les liste ; elles méritent un coup d'œil, mais ne sont pas des bugs en soi.
- **Le sens des paramètres.** Le contrôle compare des nombres, pas des significations. Une boîte qui déclare le bon nombre de règles dans le mauvais ordre passe au vert et se comporte mal.


## 6. Ce qui est livré, et la preuve que ça marche

Deux contrôles. Le second, `check-wired-param-counts.mjs`, est décrit au §5 ; celui-ci couvre les six autres coutures.

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
<Exec Command="node scripts/hooks/check-wired-param-counts.mjs"
      WorkingDirectory="$(MSBuildThisFileDirectory)" />
```

Je ne les ai pas ajoutées, pour deux raisons. La cible est aujourd'hui rouge pour d'autres motifs (§7) et y greffer un contrôle de plus n'aiderait pas tant que la barrière est contournée. Et cet audit devait livrer un rapport et des éléments de validation **sans toucher au code de production** : un script neuf et sa baseline sont des éléments de validation, une ligne dans la cible de build ne l'est plus tout à fait. La décision vous revient ; le contrôle des comptes, lui, sort 0 sans le client et ne cassera donc pas une machine qui ne l'a pas.

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
4. ~~**La couture 7**~~ — **faite** (§5). Reste à en tirer les cinq corrections : déclarer les règles manquantes de `wf_trg_user_performs_action`, `wf_act_freeze_habbo`, `wf_slc_users_with_var`, `wf_slc_remote`, `wf_xtra_mov_physics`, puis `--update` de la baseline pour qu'elle retombe à zéro. Compter ~2 h pour les trois premières (les règles sont mécaniques, le client dit quoi déclarer) ; les deux dernières n'ont de sens qu'avec le point 3, puisqu'elles n'ont aucun comportement. Critère : la baseline `wired-param-counts-baseline.json` contient une liste `known` vide.
5. **Les boîtes à arité variable** (0,5 jour). Les 51 boîtes du §5.6 ont besoin d'une règle de queue, pas d'un compte fixe ; étendre le contrôle pour vérifier qu'une boîte dont le formulaire client est dynamique en déclare une. C'est la moitié restante de cette couture.
6. **Le repli wired silencieux** (2 h). Les cinq lignes du §4.4, plus une alerte sur le compteur.
7. **Le code mort** (1 h). Supprimer les quatre triplets injoignables, ou les documenter : une boîte inerte qui **dit** qu'elle est inerte n'est plus un piège. Avec une réserve née de cet audit : le commentaire doit être vérifiable, et re-vérifié. Celui de `WiredTriggerHabboPerformsAction` affirme une absence côté client qui n'existe pas (§5.3) — une documentation fausse coûte plus cher que pas de documentation, parce qu'elle clôt la question.
8. **Le walkthrough** (1 h). Il est bon et couvre déjà le piège principal ; il lui manque `GetMaxVariableIds` (le défaut à 0, qui a coûté `0aed855` et coûte encore `wf_slc_furni_with_var`) et l'entrée du menu admin. Y ajouter surtout, en tête de checklist, la ligne « les contrôles qui vérifient ceci sont `check-inert-declarations` et `check-wired-param-counts` » : une checklist adossée à une machine est tenue, une checklist seule ne l'est pas. Et corriger son étape 3, qui dit de déduire le nombre de paramètres « du client » sans dire où : c'est `packages/vortex-engine/src/habbo/roomevents/wired_setup/**/<Boîte>.ts`, méthode `readIntParamsFromForm`.

---

## 9. Couverture et limites de cet audit

**Analysé mécaniquement, sur tout l'arbre** : 185 boîtes wired (paramètres, comportement), 86 lectures de variables (drapeaux), 23 déclencheurs (événements), 561 handlers et 555 parseurs (chaîne entrante), 394 composers (chaîne sortante), 62 clés de configuration, 260 clés `[RoomObjectLogic]`.

**Analysé à la lecture** : `FurnitureWiredLogic` (chemin de sauvegarde, normalisation, hydratation), les bases des six familles, les quatre bandes de variables et leurs bases, les deux boîtes inertes, les six handlers injoignables, `WiredSurface.cs`, `RoomObjectLogicProvider`, les scripts SQL de liaison.

**Non vérifié** :
- Le **moteur d'exécution** wired lui-même (`Vortex.Rooms/Wired/Engine/**` : cycles, profondeur, ordonnanceur, fenêtres d'exécution) — il a sa propre suite de tests et sa matrice de parité (`docs/architecture-v4/acceptance-matrix.md`), que je n'ai pas contre-vérifiée.
- Le **comportement réel** : aucun MySQL, aucune exécution de l'émulateur ni du client dans cet environnement. Les deux boîtes inertes et les cinq désaccords de compte sont établis par lecture du code des deux côtés, pas par une partie jouée. Le client est lu comme source, pas exécuté.
- Le **dump AS3** du client officiel, absent ici et jamais commité (`vortex-modern-client/.gitignore` l.14) : `check-header-registry.mjs` et `check-wire-conflicts.mjs` restent aveugles, et les 22 boîtes que le client sait configurer sans implémentation Vortex (`docs/completeness/generated/WIRED-BOXES.md`) n'ont pas été recomptées. La couture 7, elle, n'a plus besoin de ce dump (§5.1).
- Une piste non exploitée, signalée parce qu'elle vaut une journée : le port TypeScript porte aussi le registre des ids, `packages/vortex-engine/src/habbo/communication/HabboMessages.ts`, sous la forme `this._events.set(<id>, <Classe>)` / `this._composers.set(...)`. C'est une autorité **commitée** pour ce que `check-header-registry.mjs` va chercher aujourd'hui dans un dump absent. Je ne l'ai pas branchée — c'est un contrôle existant, donc du code d'outillage, hors du périmètre « ne pas modifier » que vous aviez fixé — mais c'est probablement le chemin le plus court pour remettre ce contrôle au vert (§7).
- Les familles wired **au-delà de leur enveloppe** : les 53 actions, 45 conditions et 21 sélecteurs ont été passés aux détecteurs, pas lus un par un. Un défaut de logique métier à l'intérieur d'une boîte correctement déclarée ne serait pas vu par cet audit.
