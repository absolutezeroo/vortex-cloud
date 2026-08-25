# VORTEX CLOUD — Dashboard / API / Security — FINAL FROZEN

**Statut : VALIDÉ / FROZEN — document canonique du chantier Dashboard / API / Security.**  
**Baseline :** `afc485be58ffd983b8d96430efe8aed620ad0ade` (`main`, 25 août 2026).  
**Relation avec la V4 runtime :** ce document est indépendant de `Vortex_Runtime_Architecture_Workflow_V4_FINAL.md`. La V4 runtime exclut explicitement Dashboard/API/Security de son périmètre. Aucun chantier Runtime/Commerce/Rooms/Wired ne doit modifier les décisions ci-dessous par effet de bord.

> Ce fichier consolide les décisions Dashboard déjà validées. Il ne constitue pas une nouvelle réouverture d’audit. Toute modification structurante de ce document exige un ADR explicite et un audit ciblé.

---

## 1. Objectif

Le Dashboard est un **plan de contrôle administratif privilégié** pour Vortex. Il ne doit pas devenir une seconde couche métier concurrente de l’émulateur, ni contourner les ownership boundaries existantes.

Le Dashboard doit fournir :

- authentification opérateur robuste ;
- sessions opérateur révocables ;
- RBAC par **capabilities** ;
- step-up MFA pour les opérations sensibles ;
- récupération MFA opérateur auditée ;
- opérations administratives centralisées et auditables ;
- ledger/audit durable des mutations privilégiées ;
- API stable avec erreurs structurées ;
- modération/CFH complète ;
- séparation claire entre Dashboard Web, API et domaines Vortex ;
- tests de matrice d’autorisation et quality gates.

---

## 2. Périmètre

### IN SCOPE

- `Vortex.Dashboard.API`
- `Vortex.Dashboard.Web`
- `Vortex.Dashboard.Tests`
- contrats de permissions et administration utilisés par le Dashboard ;
- authentification opérateur ;
- MFA et récupération MFA ;
- sessions Dashboard ;
- opérations staff/RBAC ;
- modération et CFH ;
- audit/ledger administratif ;
- contrats HTTP, IDs et erreurs ;
- observabilité et tests du plan de contrôle.

### OUT OF SCOPE

- refactor Rooms/Furniture/Wired ;
- Commerce Consistency de la V4 ;
- changement des ownership boundaries Orleans ;
- microservices généralisés ;
- event sourcing global ;
- CQRS/MediatR généralisé ;
- remplacement du moteur de permissions du gameplay ;
- mélange des PR Dashboard avec les PR V4 Runtime.

---

## 3. Principes non négociables

1. **Le Dashboard est un control plane, pas un second moteur métier.** Les writes appellent des contrats/services/grains propriétaires ; ils ne réimplémentent pas la logique domaine dans les endpoints.
2. **Authorization par capabilities, jamais par nom de rôle ou rang codé en dur.**
3. Les opérations qui accordent des capacités sont elles-mêmes protégées par une capability dédiée — notamment `OpsStaffManage`.
4. **Toute mutation privilégiée exige un acteur identifiable, une raison opérateur et une trace d’audit.**
5. Les endpoints HTTP restent minces : parsing/authz/validation → service d’opération → contrat domaine.
6. Les secrets MFA ne sont jamais renvoyés ou journalisés en clair hors flux d’enrôlement strictement nécessaire.
7. Une récupération MFA ne contourne pas l’audit : elle est une opération staff explicite.
8. Les sessions opérateur sont **server-side et révocables** ; l’identité d’une session n’est pas équivalente à une simple présence d’un cookie côté client.
9. Les erreurs API utilisent un format structuré et stable de type **Problem Details** / code d’erreur stable ; aucun détail d’exception interne n’est exposé au client.
10. Les IDs transportés par l’API ont une sémantique explicite ; ne pas utiliser des chaînes ambiguës ou des indexes UI comme identités métier.
11. La modération CFH reste reliée aux modèles/tickets réels ; pas de seconde queue uniquement Dashboard.
12. Les opérations critiques ne sont jamais “fire-and-forget” du point de vue de l’audit.
13. **Dashboard/API/Security reste FROZEN** tant qu’un ADR ne le rouvre pas.

---

## 4. Architecture cible figée

```text
Vortex.Dashboard.Web
        |
        | HTTPS / API contracts
        v
Vortex.Dashboard.API
  ├── Authentication / Sessions / MFA
  ├── Authorization / Capabilities
  ├── HTTP Endpoints
  ├── DashboardOperationsService
  ├── Audit / Ledger
  ├── Read APIs / Projections
  └── CFH / Moderation API
        |
        | domain contracts / Orleans calls / application services
        v
Vortex domain owners
  ├── Players / Authentication / Permissions
  ├── Rooms / Moderation / CFH
  ├── Catalog / Economy / Reference Data
  └── other bounded owners
```

### Boundary

`DashboardEndpoint -> DashboardOperationsService -> owner contract`

et non :

`DashboardEndpoint -> DbContext -> mutation métier arbitraire`.

Une lecture administrative peut utiliser des projections/read services dédiés ; une mutation doit respecter l’owner réel.

---

## 5. Authentication & sessions opérateur

Le dépôt contient déjà le sous-système dédié :

- `Vortex.Dashboard.API/Security/DashboardAuthService.cs`
- `DashboardAuthenticationHandler.cs`
- `DashboardPrincipal.cs`
- `DashboardSessionStore.cs`
- `LoginRequest.cs`

Décision figée :

- authentification Dashboard distincte de la simple session joueur ;
- principal Dashboard explicite ;
- session server-side ;
- invalidation/révocation possible côté serveur ;
- aucune authorization sensible basée uniquement sur ce que le Web déclare ;
- l’API reconstitue toujours le principal effectif côté serveur.

### Target security context

Le modèle cible conserve le concept validé d’un **ActorSecurityContext** au boundary administratif : identité opérateur, session, capabilities effectives, état MFA/step-up et metadata de corrélation nécessaires à l’audit.

Le nom exact de type peut évoluer sans ADR si la sémantique reste identique ; ce qui est figé est la frontière : les opérations privilégiées ne doivent pas dépendre d’un simple `string actor` comme unique contexte de sécurité à long terme.

---

## 6. RBAC / capabilities

Le Dashboard administre les rôles et capabilities via les contrats de permissions existants.

Le code baseline possède notamment des opérations :

- création / modification / suppression de rôle ;
- remplacement de l’ensemble des capabilities d’un rôle ;
- assignation / désassignation de rôle ;
- administration des sanction presets ;
- récupération MFA opérateur.

Ces writes passent par `DashboardOperationsService.Staff.cs`.

### Invariant

Une modification de permissions doit enregistrer **l’état résultant complet** lorsqu’il est utile à l’audit, pas uniquement un delta impossible à interpréter isolément.

Exemple déjà documenté dans le code : lors de `SetRoleCapabilitiesAsync`, l’ensemble complet des capabilities est enregistré dans le détail de l’opération afin que l’entrée d’audit puisse répondre seule à « que pouvait faire ce rôle après le changement ? ».

---

## 7. MFA, step-up et récupération

### MFA normal

Le Web possède un flux MFA dédié (`MfaModal.svelte`) et l’API expose les opérations account/MFA associées.

### Step-up MFA

Décision figée : une authentification initiale valide ne suffit pas nécessairement pour une opération hautement sensible. Les opérations sélectionnées comme critiques doivent pouvoir exiger un **step-up MFA récent**.

Le step-up appartient au contexte de sécurité opérateur, pas au payload métier envoyé par le navigateur.

### Recovery MFA

`DashboardOperationsService.ResetAccountMfaAsync` est la voie administrative dédiée.

Invariant validé :

- capability dédiée `OpsStaffManage` ;
- raison opérateur obligatoire ;
- audit de l’action `ops.staff.mfa.reset` ;
- target account explicite ;
- aucun “code MFA de secours” inventé par l’API ;
- la récupération efface le second facteur via le service MFA propriétaire.

Cette voie est une **recovery path** et doit rester plus fortement auditée qu’un changement ordinaire.

---

## 8. Ledger / audit administratif

Le Dashboard ne doit pas avoir des mutations privilégiées non traçables.

Chaque opération sensible doit produire une entrée possédant au minimum :

```text
OperationId / CorrelationId
Timestamp
Actor / Operator
Session identity
Effective capabilities / security context pertinent
Action key
Reason
Target account/player/room/entity
Structured detail
Outcome
Failure code (si applicable)
```

### Règle de cohérence

Quand mutation métier et audit vivent dans la même frontière transactionnelle locale, **mutation + entrée ledger doivent être committées ensemble**.

Quand l’opération traverse un owner distant/grain, le Dashboard journalise l’intention et le résultat avec un identifiant de corrélation stable ; il ne prétend pas créer une transaction ACID cross-grain.

### Interdictions

- audit uniquement sous forme de log texte ;
- raison facultative sur un write sensible ;
- modification destructive sans actor ;
- journal contenant secrets, mot de passe ou seed MFA ;
- succès retourné avant que l’état d’audit requis soit durable.

---

## 9. Operations boundary

`DashboardOperationsService` est la frontière canonique des writes Dashboard.

Les endpoints ne doivent pas grossir en services métier.

Pattern cible :

```text
Endpoint
  -> authenticate
  -> authorize capability
  -> validate request/reason
  -> build ActorSecurityContext
  -> DashboardOperationsService
  -> owner service/grain
  -> ledger result
  -> stable HTTP result
```

Les writes qui donnent eux-mêmes des permissions restent séparés des opérations de contenu génériques.

---

## 10. API contract & Problem Details

Décision figée : les erreurs API sont **structurées et stables**.

Cible :

```json
{
  "type": "urn:vortex:dashboard:<error-code>",
  "title": "Human readable category",
  "status": 403,
  "code": "mfa_step_up_required",
  "traceId": "...",
  "detail": "..."
}
```

Le shape exact peut suivre `ProblemDetails`, mais les invariants sont :

- status HTTP correct ;
- `code` stable pour le Web ;
- trace/correlation id ;
- pas de stack trace ;
- pas de message SQL/EF/Orleans brut ;
- validation différenciée de authorization et domain rejection ;
- les erreurs domaine connues ne sont pas loggées comme faults système.

---

## 11. IDs et contrats

Les DTOs de Dashboard doivent distinguer :

- `AccountId`
- `PlayerId`
- `RoomId`
- `RoleId`
- `PresetId`
- IDs de ticket CFH
- autres IDs domaine

Éviter :

- “id” sans contexte dans les APIs sensibles ;
- conversion implicite d’un ID UI vers un ID métier différent ;
- utiliser username comme clé d’autorité quand l’ID stable existe.

Les strongly typed IDs peuvent rester au boundary interne ; le JSON peut rester numérique/string selon compatibilité existante, mais la sémantique doit être non ambiguë.

---

## 12. Moderation / CFH

CFH = **Call For Help**.

Le baseline contient :

- `Vortex.Dashboard.API/Api/DashboardApiService.Cfh.cs`
- `Vortex.Dashboard.Web/src/pages/CfhQueuePage.svelte`
- `Vortex.Dashboard.Web/src/pages/CfhStatsPage.svelte`
- `Vortex.Rooms/CfhTicketService.cs`
- entités DB `CfhTicketEntity`, `CfhTopicEntity`, `CfhCategoryEntity`
- queue de modération côté runtime.

### Décision

Le Dashboard **projette et opère sur la vérité CFH existante**. Il ne crée pas une seconde source de vérité administrative.

Les writes de modération utilisent les capabilities correspondantes et le ledger opérateur.

Les stats sont des read models ; elles ne deviennent jamais owner du workflow CFH.

---

## 13. Hôte administratif

Décision d’architecture validée : le plan de contrôle Dashboard doit pouvoir être **hébergé hors du process gameplay principal**.

But :

- réduire le blast radius d’une erreur Web/admin ;
- permettre déploiement/redémarrage du Dashboard sans redémarrer le runtime jeu ;
- éviter qu’un serveur HTTP administratif augmente directement la surface d’attaque du silo principal ;
- conserver les appels vers les vrais owners via des frontières explicites.

Cela ne signifie pas microservices généralisés : c’est une séparation du **control plane**, pas une fragmentation du domaine.

`DashboardWebHost.cs` reste le composition point HTTP du Dashboard.

---

## 14. Web client

Le Web :

- ne décide jamais des permissions effectives ;
- masque/désactive l’UI selon capabilities pour UX, mais l’API reste l’autorité ;
- gère MFA/step-up comme interaction utilisateur, sans stocker le secret ;
- consomme les error codes stables de l’API ;
- ne reproduit pas les règles métier des grains/services.

Les permissions de navigation (`dashboardPermissions.js`, routes, etc.) doivent rester alignées avec la matrice API.

---

## 15. Authorization matrix

Le dépôt dispose d’une matrice de tests dédiée (`Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt`).

Critère final :

- chaque endpoint privilégié apparaît dans la matrice ;
- capability minimale attendue documentée ;
- absence de capability -> refus ;
- capability correcte -> accès ;
- les endpoints staff/manage ne partagent pas une capability trop large avec des writes de contenu ;
- les opérations à step-up testent le cas MFA récent / absent / expiré.

Une nouvelle route Dashboard sans entrée de matrice est une régression.

---

## 16. Tests obligatoires

### Auth / session

- login succès/échec ;
- session inconnue/révoquée/expirée ;
- principal reconstitué côté serveur ;
- logout/revocation ;
- aucune confiance dans les permissions déclarées par le client.

### RBAC

- matrice endpoint × capability ;
- role assign/unassign ;
- replacement complet des capabilities ;
- séparation `OpsStaffManage` des autres permissions.

### MFA

- enrollment/verification ;
- step-up requis ;
- step-up valide ;
- récupération `ResetAccountMfaAsync` auditée ;
- secret absent des logs/audit.

### Ledger

- actor/reason/action/target/detail présents ;
- failure outcome enregistré selon contrat ;
- aucune mutation critique sans entrée durable ;
- secret redaction.

### API

- codes d’erreur stables ;
- Problem Details / shape standard ;
- pas de stack trace ;
- correlation id.

### CFH

- queue/stats lisent la même vérité métier ;
- actions moderator respectent capabilities ;
- audit de mutation.

---

## 17. Observabilité

Métriques minimales :

```text
dashboard.auth.success / failure
dashboard.session.active / revoked
dashboard.mfa.challenge / failure / recovery
dashboard.authorization.denied{capability}
dashboard.operation.total{action,outcome}
dashboard.operation.duration{action}
dashboard.audit.write.failure
dashboard.cfh.queue.size
dashboard.http.error{code}
```

Logs structurés avec correlation id, jamais de secret.

Toute alerte sur `dashboard.audit.write.failure` est prioritaire : une mutation privilégiée non auditée est un défaut de control plane.

---

## 18. Anti-patterns explicitement interdits

- authorization par `if (rank >= X)` ;
- authorization uniquement dans Svelte ;
- endpoint qui instancie directement un `DbContext` pour modifier un domaine dont il n’est pas owner ;
- route “god endpoint” qui mélange plusieurs capabilities ;
- MFA recovery non auditée ;
- raison opérateur optionnelle pour sanctions/permissions/destructive writes ;
- audit best-effort après avoir retourné succès ;
- stack traces en réponse HTTP ;
- secrets MFA/passwords dans logs ou ledger ;
- Dashboard et Runtime V4 modifiés dans la même grosse PR sans nécessité technique prouvée ;
- rouvrir l’architecture Dashboard pendant les PR Commerce/Wired.

---

## 19. Workflow IA pour ce chantier

Ce domaine est **FROZEN**.

Avant tout changement :

```text
1. Lire ce document.
2. Lire AGENTS.md / CONTEXT.md / CLAUDE.md.
3. Comparer le SHA audité au HEAD sur :
   - Vortex.Dashboard.API/**
   - Vortex.Dashboard.Web/**
   - Vortex.Dashboard.Tests/**
   - Vortex.Primitives/Permissions/**
   - Vortex.Primitives/Authentication/**
4. Si les watched_paths n’ont pas changé :
   -> ne pas réauditer l’architecture.
5. Si un changement contredit une décision FROZEN :
   -> STOP
   -> annoncer le conflit
   -> proposer un ADR
   -> aucune implémentation avant acceptation.
```

Les PR Dashboard ne doivent pas être mélangées aux PR `C*`, `W*`, `G*`, `S*` de la V4 Runtime sauf dépendance concrète documentée.

---

## 20. Critères d’acceptation finaux

Le chantier Dashboard/API/Security est conforme lorsque :

1. chaque route sensible est protégée par capability côté API ;
2. les permissions Web ne sont qu’un miroir UX ;
3. les opérations de permissions utilisent une capability staff dédiée ;
4. chaque mutation sensible exige actor + reason ;
5. le ledger fournit une trace durable et corrélable ;
6. la récupération MFA est protégée et auditée ;
7. les opérations désignées sensibles supportent le step-up MFA ;
8. les sessions opérateur sont server-side et révocables ;
9. les erreurs sont structurées, stables et sans fuite interne ;
10. les IDs métier sont non ambigus ;
11. CFH utilise la source de vérité existante ;
12. les endpoints n’embarquent pas la logique domaine ;
13. l’hôte administratif peut être séparé du process gameplay sans changer les domaines ;
14. la matrice d’autorisation couvre toutes les routes privilégiées ;
15. les quality gates Dashboard passent ;
16. aucune décision de ce document n’est modifiée implicitement par la V4 Runtime.

---

# FINAL STATUS

```text
Dashboard / API / Security
STATUS: ACCEPTED / FROZEN

Runtime / Gameplay / Commerce
STATUS: V4 CANONICAL

RULE:
The two documents are complementary.
Neither silently overrides the other.
A conflict requires an explicit ADR.
```

---

# Annexe A — Sources baseline

Repository : `https://github.com/absolutezeroo/vortex-cloud`  
Commit : `afc485be58ffd983b8d96430efe8aed620ad0ade`

## Dashboard API / Security

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardAuthService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardAuthenticationHandler.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardPrincipal.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Security/DashboardSessionStore.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Operations/DashboardOperationsService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Operations/DashboardOperationsService.Staff.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Operations/StaffOperationContracts.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Hosting/DashboardEndpoints.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Hosting/DashboardEndpoints.Account.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Hosting/DashboardWebHost.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Http/DashboardAuditEmitter.cs`

## Permissions / auth

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Permissions/Capabilities.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Authentication/Permissions/DefaultRoles.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Authentication/IAccountMfaService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Authentication/IAccountAuthenticator.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Primitives/Authentication/AccountSessionStore.cs`

## CFH

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.API/Api/DashboardApiService.Cfh.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Rooms/CfhTicketService.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Rooms/Grains/ModerationQueueGrain.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Database/Entities/Moderation/CfhTicketEntity.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Database/Entities/Moderation/CfhTopicEntity.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Database/Entities/Moderation/CfhCategoryEntity.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/pages/CfhQueuePage.svelte`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/pages/CfhStatsPage.svelte`

## Tests / Web

- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Tests/Hosting/authorization-matrix.txt`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Tests/Hosting/DashboardOperationReasonTests.cs`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/components/MfaModal.svelte`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/lib/dashboardPermissions.js`
- `https://github.com/absolutezeroo/vortex-cloud/blob/afc485be58ffd983b8d96430efe8aed620ad0ade/Vortex.Dashboard.Web/src/lib/routes.js`
