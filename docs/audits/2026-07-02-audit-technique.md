# Audit technique — branche `copilot/technical-audit-emulateur-habbo` (2026-07-02)

Rapport consolidé des audits produits sur cette branche. Les constats détaillés et leur suivi
vivent dans `CONSOLIDATION.md` (backlog P1–P7) et `ROADMAP.md` (Epic 0/3) ; ce document fige un
instantané daté des audits réalisés, des bugs confirmés et des correctifs livrés.

---

## 1. Audit du flux d'achat / portefeuille (économie)

**Constat (bug confirmé, reproductible) :** `CatalogPurchaseGrain`, `MarketplacePurchaseGrain` et
`LtdRaffleGrain` débitaient le portefeuille puis effectuaient le travail DB/attribution **sans
compensation**. Tout échec après un débit réussi (ex. `FurnitureDefinitionNotFound` sur une offre
catalogue avec un `FurniDefinitionId` obsolète) faisait perdre définitivement les crédits du joueur.
Aucune entrée de ledger ne couvrait ce chemin. Lacune annexe : `Silver` n'était pas remboursable
(seuls `Credits`/`ActivityPoints` l'étaient).

**Correctif livré :**
- Extraction d'un helper partagé `IPlayerWalletGrain.ExecutePurchaseAsync`
  (`Turbo.Primitives/Players/Wallet/WalletPurchaseExtensions.cs`) : débite une fois, exécute
  l'étape d'attribution, et **re-crédite automatiquement** (avec log d'erreur) si l'attribution
  lève une exception. Les trois grains l'utilisent désormais.
- `IPlayerWalletGrain` gagne `CreditBackAsync` (remboursement générique, couvre aussi `Silver`).

**Couverture de régression :** `WalletPurchaseExtensionsTests.cs`
(`Turbo.Rooms.Tests/Observability/`) — solde insuffisant n'invoque jamais l'attribution ;
attribution réussie ne rembourse jamais ; attribution levant une exception déclenche exactement un
`CreditBackAsync` avec les débits d'origine avant re-throw ; liste de débits vide non remboursée.

---

## 2. Audit des blocs `catch` (uniformité de la gestion d'erreurs, repo-wide)

**Périmètre :** 167 blocs `catch` sur tout le dépôt (pas seulement `Turbo.Rooms`), 0
`TODO handle exceptions` restant.

**Constats et correctifs de cette passe :**
- `RoomAvatarTickSystem.cs` — récupération d'échec de pas de marche sans aucune ligne de log →
  log ajouté.
- `RoomItemsProvider.cs` et `InventoryFurnitureLoader.cs` — `catch (Exception) { continue; }`
  silencieux à la charge d'un item : risque réel de disparition de mobilier/inventaire sans trace
  diagnostique → les deux loggent désormais un warning avec l'id de l'item et la room/le joueur
  propriétaire avant de sauter l'item. `InventoryFurnitureLoader` reçoit un
  `ILogger<InventoryFurnitureLoader>` injecté par constructeur (aucun logger auparavant).
- Fichiers `Turbo.Rooms` précédemment signalés (`RoomService.Floor.cs`, `RoomService.Wall.cs`,
  `RoomWiredSystem.cs`, `RoomRollerSystem.cs`, `RoomPathingSystem.cs`, `RoomMapModule.Avatar.cs`,
  `RoomAvatarModule.cs`) — vérifiés déjà corrigés par une passe antérieure (log via
  `_roomGrain._logger.LogWarning`).

**Écart restant tracké (hors périmètre de cette passe) :** `FurnitureWiredLogic.cs`
(`Turbo.Rooms/Object/Logic/Furniture/Floor/Wired/`) — deux `catch { }` totalement silencieux
(~l.314, ~l.340, avalant les échecs d'`Activator.CreateInstance` lors de la réhydratation des
wired) et un `catch (Exception ex) { Console.WriteLine(ex); return false; }` (~l.362, contournant
le logging structuré). Le correctif propre exige de faire passer un `ILogger` par le constructeur
de base, cascade sur 6 classes abstraites intermédiaires + 83 classes feuilles concrètes
construites via `ActivatorUtilities.CreateInstance` dans `RoomObjectLogicFeatureProcessor.cs` —
scopé comme follow-up dédié (voir `CONSOLIDATION.md` P5).

---

## 3. Audit de la couverture de tests des chemins sensibles (P1)

**Constat initial :** 21 cas de test au total ; `RoomSecurityPolicy`, `ModerationPolicy` et
`economy_ledger` non couverts — précisément là où une régression silencieuse coûte cher
(escalade de privilèges, duplication de monnaie).

**État après cette passe :**
- `RoomSecurityPolicyTests.cs` et `ModerationPolicyTests.cs` (`Turbo.Rooms.Tests/Permissions/`)
  couvrent toutes les branches des deux policies.
- Mapping du ledger couvert par `EconomyLedgerTests.cs`.
- Invariant achat-remboursement couvert (voir §1).
- Preuve de concept grain réel : `RoomDirectoryGrainClusterTests.cs`
  (`Turbo.Rooms.Tests/Grains/`) démarre un `TestCluster` in-process via
  `Microsoft.Orleans.TestingHost` (aligné Orleans 9.2.1) et exerce `RoomDirectoryGrain` de bout en
  bout (activation + câblage DI + appels par référence de grain).
- `Turbo.Rooms.Tests` : 42/42 verts ; exécutés dans le gate via `dotnet test Turbo.Cloud.sln`
  (`TurboCloudFastCheck`).

---

## 4. Audit du quality gate (P4 — nouvel écart identifié)

**Constat :** `dotnet csharpier check .` est vert sur tout le dépôt, mais `TurboCloudQualityGate`
(`Directory.Build.targets`) exécute `dotnet format ... --verify-no-changes` **uniquement sur
`Turbo.Main`** (racine de composition, quasiment sans logique métier). Les violations
analyzers/style dans `Turbo.Rooms`, `Turbo.PacketHandlers`, `Turbo.Players`, etc. ne sont jamais
gatées. Élargir le scope aujourd'hui casserait immédiatement le gate bloquant sur ~1200 warnings
analyzers préexistants — laissé en follow-up délibéré plutôt que forcé.

**Reco :** pointer les étapes `format style`/`format analyzers` sur `Turbo.Cloud.sln`, puis rendre
`pre-push` bloquant sur le gate complet.

---

## 5. Synthèse des métriques d'audit

| Métrique | Valeur | Lecture |
|---|---|---|
| Projets | 29 | sur-modularisation partielle ; `Turbo.Contracts` déjà fusionné |
| Projets de test | 4 | `Rooms.Tests` couvre policies, ledger, invariant achat-remboursement |
| Cas de test | 40+ dans `Rooms.Tests` seul (21 au total avant) | chemins sensibles couverts |
| Handlers | 501 | ~300 stubs vides (~60 %), en baisse depuis 78 % |
| TODO/FIXME/HACK | 276 | dette éparpillée, en baisse depuis 327 |
| `TODO handle exceptions` | 0 | stratégie d'erreur appliquée |
| Blocs catch | 167 | 0 avalement silencieux hors gap `FurnitureWiredLogic.cs` tracké |

---

## 6. Suivi

- Backlog priorisé et statut à jour : `CONSOLIDATION.md` (P1 ✅, P5 ✅ hors gap tracké, P2/P3/P4/P6/P7 ouverts).
- Statut features : `ROADMAP.md` (Epic 0 ✅, Epic 3 ✅).
- Follow-ups explicitement scopés : élargissement du scope `dotnet format` du gate (P4) ;
  `ILogger` dans la chaîne DI wired-logic (`FurnitureWiredLogic.cs`, P5).
