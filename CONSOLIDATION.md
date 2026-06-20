# Vortex Cloud — Travaux de consolidation (hors feature)

Backlog de durcissement, audité sur `main` (commit `fa1e6e8`). Chiffres réels, pas
d'estimation. Complète la `ROADMAP.md` (qui couvre les features) : ceci couvre la **qualité
de l'existant**. Objectif : fermer l'écart entre l'ambition de la structure et la profondeur
de l'implémentation.

---

## Instantané d'audit

| Métrique | Valeur | Lecture |
|---|---|---|
| Projets | 28 | quelques-uns trop petits (sur-modularisation) |
| Plus petit projet | `Turbo.Contracts` (100 lignes) | candidat fusion |
| Projets de test | 2 (`WebApi.Tests`, `Rooms.Tests`) | en progrès, mais sensible non couvert |
| Cas de test | 21 | WebApi (16) + wired round-trip ; **policies + ledger absents** |
| Handlers | 498 | dont **393 stubs vides (78 %)** |
| TODO/FIXME/HACK | 327 | dette diffuse |
| `TODO handle exceptions` | **0** | ✅ stratégie d'erreur appliquée |
| Blocs catch | 172 | à auditer (swallow ?) |
| `EventContext` | `class { }` vide | seam d'interception mort |
| `= false` hardcodés (Rooms) | 3 | capacités simulées (group room…) |

**Déjà fait (à ton crédit) :** stratégie d'erreur de grain (0 TODO exception), test de
round-trip wired, migration WebApi + ses tests.

---

## Backlog priorisé

### P1 — Tester le sensible (le plus haut levier)
**Évidence :** 21 cas, mais `RoomSecurityPolicy`, `ModerationPolicy` (fonctions pures) et
`economy_ledger` ne sont pas couverts. Ce sont les endroits où une régression est silencieuse
et coûteuse (escalade de privilège, currency dupliquée).
**Travail :** étendre `Turbo.Rooms.Tests` (ou un `Turbo.Permissions.Tests`) pour couvrir les
deux policies (toutes les branches) + des tests d'invariants sur le ledger (débit/crédit,
idempotence, refus solde insuffisant, aucun chemin ne bouge la monnaie sans entrée de ledger).
**Done quand :** policies et ledger couverts ; le gate exécute ces tests.

### P2 — Remplir les coquilles vides
**Évidence :** `EventContext` est `class { }` → ton `IEventBehavior` est inerte (pas
d'interception plugin possible). 3 `= false` hardcodés dans Rooms simulent des capacités
(group room). 2 classes vides au total.
**Travail :** appliquer le modèle d'event deux-phases + `EventContext` enrichi (Cancel,
CorrelationId, Items) — le prompt déjà rédigé. Câbler les `isGroupRoom`/`canGroupDecorate`
réels (vient avec la branche groupes). Auditer s'il existe d'autres interfaces présentes mais
non câblées.
**Done quand :** `EventContext` fonctionnel, plus aucune capacité simulée par un `false`
hardcodé.

### P3 — Dégonfler la sur-modularisation
**Évidence :** 28 projets ; `Turbo.Contracts` (100 l), `Turbo.Events` (186 l),
`Turbo.Logging` (330 l), `Turbo.Messages` (337 l) sont petits.
**Travail :** auditer chaque frontière. Celles qui ne sont PAS un vrai seam de plugin et font
<150 lignes → fusionner dans le parent logique. `Turbo.Contracts` est le cas le plus net.
**Done quand :** le nombre de projets reflète de vraies frontières, pas des dossiers déguisés.

### P4 — Verrouiller le quality gate
**Évidence :** la structure existe (gate deux-phases, csharpier, githooks) mais le format
n'était pas vert partout. Un gate non-vert n'est qu'une suggestion.
**Travail :** passer `csharpier`/`format` au vert sur tout le repo, rendre le `pre-push`
bloquant. (À vérifier : `dotnet csharpier --check .`.)
**Done quand :** gate vert et bloquant en pre-push.

### P5 — Auditer l'uniformité des catch
**Évidence :** 0 `TODO exception` (bien), mais 172 blocs catch — certains avalent peut-être
l'erreur en silence (≈24 repérés vides lors d'un passage précédent).
**Travail :** parcourir les catch, s'assurer qu'aucun n'avale une erreur sans log (via la
stratégie d'erreur existante).
**Done quand :** aucun swallow silencieux.

### P6 — Hygiène metrics
**Évidence :** `performance_logs` porte du legacy client (`flash_version`).
**Travail :** trancher `performance_logs` (supprimer), router la télémétrie haut volume vers le
meter OTel plutôt que la DB transactionnelle (cf. `DATA-MODEL.md` §8).
**Done quand :** la DB transactionnelle ne porte plus de télémétrie haut volume.

### P7 — Dette de stubs (honnêteté de l'état)
**Évidence :** 393/498 handlers vides (78 %) + 327 TODO. Le projet se lit plus fini qu'il
n'est.
**Travail :** par domaine, décider — remplir (devient du travail feature, hors scope ici) ou
tracer explicitement les stubs pour ne pas faire passer du vide pour du fait. Au minimum, ne
plus en ajouter sans le marquer.
**Done quand :** l'état réel d'avancement est lisible, pas masqué par le scaffolding.

---

## Le méta-travail (ce qui empêche le défaut de revenir)

Instaurer une **Definition of Done appliquée** : rien n'entre dans `main` sans être testé sur
le sensible, sans nouveau stub non tracé, gaté et audité. Elle est documentée (`AGENTS.md`)
mais pas tenue — la tenir transforme la discipline de séquencement, le seul vrai reproche de
fond.

---

## Ordre conseillé

**P1 (tests) → P2 (coquilles) → P3 (projets) → P4 (gate)**, puis P5/P6/P7 en continu. C'est
moins gratifiant qu'une feature, mais c'est exactement ce qui sépare « la plus belle charpente
de la scène » d'un hôtel solide.
