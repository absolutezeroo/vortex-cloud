-- ============================================================================================
-- Catalogue Wired - reconstruction, portable
--
-- A lancer sur N'IMPORTE QUELLE base de l'hotel : aucun identifiant n'est ecrit en dur.
-- Les pages, les definitions et les offres sont resolues au moment de l'execution, depuis
-- la base ou tu le lances. Rejouable : une seconde execution refait proprement par-dessus
-- la premiere.
--
-- CE QU'IL FAIT
--   1. repare deux boites wired dont la colonne `logic` pointe sur un comportement generique
--   2. retire (deleted_at, jamais DELETE) toutes les offres wired existantes
--   3. retire les pages qui ne contenaient QUE du wired ; celles qui contiennent autre chose
--      restent debout et ne perdent que leurs offres wired
--   4. reconstruit l'arbre sous la page `wired_furniture`, dans l'ordre ou une pile s'execute
--   5. recree une offre par definition wired, rangee par famille puis par cible
--
-- RIEN N'EST SUPPRIME. Tout passe par deleted_at, donc tout se defait par un UPDATE.
--
-- POUR VALIDER : le script se termine par un SELECT de controle et un COMMIT commente.
-- Lis le controle, puis decommente COMMIT.
-- ============================================================================================

-- Le jeu de caracteres de la CONNEXION, pas celui des tables. Sans lui, les chaines ecrites
-- dans ce fichier arrivent dans l'encodage du client -- cp850 depuis une console Windows -- et
-- le COLLATE pose plus bas sur ces memes chaines devient invalide (erreur 1253). Une ligne, et
-- le script se comporte pareil depuis phpMyAdmin, depuis mysql en console ou depuis un tunnel.
SET NAMES utf8mb4;

START TRANSACTION;

-- --------------------------------------------------------------------------------------------
-- 0. Verification : la page racine doit exister. Si ce SELECT ne renvoie rien, ARRETE-TOI,
--    le reste ne greffera nulle part.
-- --------------------------------------------------------------------------------------------
SELECT id AS racine_wired_trouvee, name
  FROM catalog_pages
 WHERE localization = 'wired_furniture' AND deleted_at IS NULL;

-- --------------------------------------------------------------------------------------------
-- 1. Deux definitions wired branchees sur une logique generique.
--    Reperees par leur classe, jamais par un id.
--      wf_var_echo         : la variable echo EST implementee cote serveur ; branchee sur
--                            furniture_multistate elle ne fonctionne pas du tout.
--      wf_act_endgame_team : effet de fin de partie ; pas encore implemente, il rejoint
--                            simplement les inertes plutot que de se faire passer pour un
--                            furni ordinaire.
--    Les ~128 autres furnis nommes wf_* avec une logique non-wired (portails, interrupteurs,
--    plaques, cables, jetons) sont de vrais mecanismes et ne sont PAS touches.
-- --------------------------------------------------------------------------------------------
UPDATE furniture_definitions
   SET logic = 'wf_var_echo'
 WHERE name = 'wf_var_echo' AND logic NOT LIKE 'wf\_%' AND deleted_at IS NULL;

UPDATE furniture_definitions
   SET logic = 'wf_act_endgame_team'
 WHERE name = 'wf_act_endgame_team' AND logic NOT LIKE 'wf\_%' AND deleted_at IS NULL;

-- --------------------------------------------------------------------------------------------
-- 2. Photo de l'existant, prise AVANT d'y toucher.
-- --------------------------------------------------------------------------------------------

-- Quelles pages sont 100 % wired ? Celles-la seront retirees. Celles qui melangent gardent
-- leur place et ne perdent que leurs offres wired.
DROP TEMPORARY TABLE IF EXISTS tmp_pages;
CREATE TEMPORARY TABLE tmp_pages AS
SELECT pg.id,
       SUM(d.logic LIKE 'wf\_%')     AS wired,
       SUM(d.logic NOT LIKE 'wf\_%') AS autre
  FROM catalog_pages pg
  JOIN catalog_offers   o  ON o.page_id  = pg.id AND o.deleted_at  IS NULL
  JOIN catalog_products pr ON pr.offer_id = o.id AND pr.deleted_at IS NULL
  JOIN furniture_definitions d ON d.id = pr.definition_id
 WHERE pg.deleted_at IS NULL
 GROUP BY pg.id;

-- Quelles boites sont reservees au staff ? On ne le devine pas : on lit les pages que cet
-- hotel declare lui-meme comme telles.
--
-- `wfkit_staff` est dans la liste pour que le script reste rejouable : sa propre etape 4 retire
-- les pages `staff_wired` (elles sont 100 % wired), donc une deuxieme execution ne les
-- trouverait plus et reverserait le staff dans les familles ordinaires. Elle relit alors la
-- page que la premiere execution a creee.
DROP TEMPORARY TABLE IF EXISTS tmp_staff;
CREATE TEMPORARY TABLE tmp_staff AS
SELECT DISTINCT pr.definition_id AS id
  FROM catalog_pages pg
  JOIN catalog_offers   o  ON o.page_id  = pg.id AND o.deleted_at  IS NULL
  JOIN catalog_products pr ON pr.offer_id = o.id AND pr.deleted_at IS NULL
 WHERE pg.localization IN ('staff_wired', 'wfkit_staff') AND pg.deleted_at IS NULL;

-- Les offres wired a retirer. Aucune offre ne melange wired et non-wired : le compte est
-- verifiable avec la requete en commentaire ci-dessous, il vaut 0.
--   SELECT COUNT(*) FROM (SELECT o.id FROM catalog_offers o
--     JOIN catalog_products pr ON pr.offer_id=o.id AND pr.deleted_at IS NULL
--     JOIN furniture_definitions d ON d.id=pr.definition_id WHERE o.deleted_at IS NULL
--     GROUP BY o.id HAVING SUM(d.logic LIKE 'wf\_%')>0 AND SUM(d.logic NOT LIKE 'wf\_%')>0) x;
DROP TEMPORARY TABLE IF EXISTS tmp_offers;
CREATE TEMPORARY TABLE tmp_offers AS
SELECT DISTINCT o.id
  FROM catalog_offers o
  JOIN catalog_products pr ON pr.offer_id = o.id AND pr.deleted_at IS NULL
  JOIN furniture_definitions d ON d.id = pr.definition_id AND d.logic LIKE 'wf\_%'
 WHERE o.deleted_at IS NULL;

-- --------------------------------------------------------------------------------------------
-- 3. Classement. Famille d'apres le prefixe de la logique, cible d'apres ce que la logique
--    et la classe nomment. Les quatre grosses familles sont eclatees par cible ; les petites
--    tiennent sur une seule page.
--
--    Exclus : wf_blob et wf_pyramid, qui ne sont pas des boites wired.
-- --------------------------------------------------------------------------------------------
-- L'adresse de chaque boite est calculee dans la meme requete : surtout pas par un
-- ALTER TABLE apres coup. MySQL valide implicitement la transaction sur un ALTER, meme pour
-- une table temporaire, ce qui rendrait le ROLLBACK de fin inoperant et transformerait le
-- "lis le controle avant de valider" en mensonge.
DROP TEMPORARY TABLE IF EXISTS tmp_defs;
CREATE TEMPORARY TABLE tmp_defs AS
SELECT z.id, z.classe, z.famille, z.cible,
       CASE WHEN z.cible = 'staff' THEN 'wfkit_staff'
            WHEN z.cible = ''      THEN CONCAT('wfkit_', z.famille)
            ELSE CONCAT('wfkit_', z.famille, '_', z.cible) END AS page_slug
  FROM (
SELECT d.id,
       d.name AS classe,
       fam.f  AS famille,
       CASE WHEN s.id IS NOT NULL THEN 'staff'
            WHEN fam.f IN ('triggers','conditions','conditions_negative','effects') THEN
                 CASE
                   WHEN h.hay REGEXP 'bot'                                                  THEN 'bot'
                   WHEN h.hay REGEXP 'chest|transaction|contract|currency|credit|ducket|diamond' THEN 'chest'
                   WHEN h.hay REGEXP 'team'                                                 THEN 'team'
                   WHEN h.hay REGEXP 'group'                                                THEN 'group'
                   WHEN h.hay REGEXP 'var|placeholder|lvlup|levelling'                      THEN 'variable'
                   WHEN h.hay REGEXP 'badge|achievement|tag'                                THEN 'badge'
                   WHEN h.hay REGEXP 'game|score|banzai|freeze|football'                    THEN 'game'
                   WHEN h.hay REGEXP 'time|clock|period|date|timer|idle|afk'                THEN 'time'
                   WHEN h.hay REGEXP 'furni|stuff|item|state|altitude|snapshot|roller|toggle|move|rotate|raise|lower' THEN 'furni'
                   WHEN h.hay REGEXP 'user|habbo|avtr|avatar|triggerer|actor|effect|handitem|dance|walk|say|chat|kick|mute|alert|message' THEN 'user'
                   WHEN h.hay REGEXP 'room|tile|signal|stacks|log|wired'                    THEN 'room'
                   ELSE 'general'
                 END
            ELSE '' END AS cible
  FROM furniture_definitions d
  LEFT JOIN tmp_staff s ON s.id = d.id
  JOIN (SELECT 1) dummy
  JOIN LATERAL (SELECT CONCAT(d.logic, ' ', d.name) AS hay) h
  JOIN LATERAL (SELECT CASE
          WHEN d.logic LIKE 'wf\_trg\_%'                                     THEN 'triggers'
          WHEN d.logic LIKE 'wf\_slc\_%'                                     THEN 'selectors'
          WHEN d.logic LIKE 'wf\_cnd\_%' AND d.logic LIKE '%\_not\_%'        THEN 'conditions_negative'
          WHEN d.logic LIKE 'wf\_cnd\_%'                                     THEN 'conditions'
          WHEN d.logic LIKE 'wf\_act\_%' AND d.logic LIKE '%\_neg\_%'        THEN 'effects_negative'
          WHEN d.logic LIKE 'wf\_act\_%'                                     THEN 'effects'
          WHEN d.logic LIKE 'wf\_xtra\_%'                                    THEN 'addons'
          WHEN d.logic LIKE 'wf\_var\_%'                                     THEN 'variables'
          ELSE 'misc' END AS f) fam
 WHERE d.deleted_at IS NULL
   AND d.logic LIKE 'wf\_%'
   AND d.logic NOT IN ('wf_blob', 'wf_pyramid')
  ) z;

-- --------------------------------------------------------------------------------------------
-- 4. Retrait de l'ancien. Les produits d'abord, puis les offres, puis les pages 100 % wired
--    et tout ce qu'une execution precedente de ce script aurait cree.
-- --------------------------------------------------------------------------------------------
UPDATE catalog_products pr JOIN tmp_offers t ON t.id = pr.offer_id
   SET pr.deleted_at = NOW() WHERE pr.deleted_at IS NULL;

UPDATE catalog_offers o JOIN tmp_offers t ON t.id = o.id
   SET o.deleted_at = NOW() WHERE o.deleted_at IS NULL;

UPDATE catalog_pages pg JOIN tmp_pages t ON t.id = pg.id
   SET pg.deleted_at = NOW()
 WHERE t.autre = 0 AND t.wired > 0 AND pg.deleted_at IS NULL;

UPDATE catalog_pages SET deleted_at = NOW()
 WHERE localization LIKE 'wfkit\_%' AND deleted_at IS NULL;

-- --------------------------------------------------------------------------------------------
-- 5. Le nouvel arbre. Les libelles des cibles et l'ordre d'affichage.
--
-- `image_data` et `text_data` sont mappees en JSON cote EF (List<string>?). Les seules valeurs
-- valides sont un tableau JSON ou NULL : une chaine vide fait echouer la lecture de la page
-- entiere ("The empty string is not valid JSON") et casse le dashboard. On reprend donc ce que
-- le reste du catalogue utilise, l'entete standard.
-- --------------------------------------------------------------------------------------------
DROP TEMPORARY TABLE IF EXISTS tmp_targets;
CREATE TEMPORARY TABLE tmp_targets (cible VARCHAR(16), libelle VARCHAR(32), rang INT);
INSERT INTO tmp_targets VALUES
  ('user','Users',10), ('furni','Furni',20), ('room','Room',30), ('bot','Bots',40),
  ('team','Teams',50), ('game','Games & Score',60), ('variable','Variables',70),
  ('time','Time & Clocks',80), ('chest','Chests & Currency',90),
  ('badge','Badges & Achievements',100), ('group','Groups',110), ('general','General',120);

DROP TEMPORARY TABLE IF EXISTS tmp_fams;
CREATE TEMPORARY TABLE tmp_fams (famille VARCHAR(24), libelle VARCHAR(32), icone INT, rang INT, visible TINYINT);
INSERT INTO tmp_fams VALUES
  ('triggers','Triggers',81,10,1),
  ('selectors','Selectors',10623,20,1),
  ('conditions','Conditions',83,30,1),
  ('conditions_negative','Negative Conditions',83,40,1),
  ('effects','Effects',82,50,1),
  ('effects_negative','Negative Effects',82,60,1),
  ('addons','Add-ons',318,70,1),
  ('variables','Variables',336,80,1),
  ('misc','Chests & Contracts',4157,90,1),
  ('staff','Staff',82,100,0);

-- Les pages de famille, greffees sur la racine wired.
--
-- Chaque comparaison de chaines porte un COLLATE explicite DES DEUX COTES. Les tables
-- temporaires heritent de la collation par defaut du serveur, les tables du catalogue de
-- celle de la base : sur un hotel en utf8mb4_unicode_ci face a un serveur en
-- utf8mb4_0900_ai_ci, MySQL refuse la comparaison (erreur 1267). Fixer les deux cotes sur
-- une collation qui existe partout rend le script independant de ce reglage.
INSERT INTO catalog_pages
       (parent_id, localization, name, icon, layout, image_data, text_data, sort_order, visible, catalog_type)
SELECT r.id, CONCAT('wfkit_', f.famille), f.libelle, f.icone, 'default_3x3', '["hubbly_h"]', NULL,f.rang, f.visible, 0
  FROM tmp_fams f
  JOIN (SELECT id FROM catalog_pages WHERE localization = 'wired_furniture' AND deleted_at IS NULL
        ORDER BY id LIMIT 1) r
 WHERE EXISTS (SELECT 1 FROM tmp_defs x
                WHERE x.page_slug COLLATE utf8mb4_general_ci
                      = CONCAT('wfkit_', f.famille) COLLATE utf8mb4_general_ci
                   OR x.page_slug COLLATE utf8mb4_general_ci
                      LIKE CONCAT('wfkit_', f.famille, '\_%') COLLATE utf8mb4_general_ci);

-- Les sous-pages par cible, uniquement pour les combinaisons qui ont reellement du contenu.
INSERT INTO catalog_pages
       (parent_id, localization, name, icon, layout, image_data, text_data, sort_order, visible, catalog_type)
SELECT p.id, x.page_slug, CONCAT(f.libelle, ' - ', t.libelle), f.icone, 'default_3x3', '["hubbly_h"]', NULL,t.rang, 1, 0
  FROM (SELECT DISTINCT famille, cible, page_slug FROM tmp_defs WHERE cible NOT IN ('', 'staff')) x
  JOIN tmp_fams    f ON f.famille COLLATE utf8mb4_general_ci = x.famille COLLATE utf8mb4_general_ci
  JOIN tmp_targets t ON t.cible   COLLATE utf8mb4_general_ci = x.cible   COLLATE utf8mb4_general_ci
  JOIN (SELECT id, localization FROM catalog_pages
         WHERE localization LIKE 'wfkit\_%' AND deleted_at IS NULL) p
    ON p.localization COLLATE utf8mb4_general_ci
       = CONCAT('wfkit_', x.famille) COLLATE utf8mb4_general_ci;

-- --------------------------------------------------------------------------------------------
-- 6. Une offre par boite, puis son produit.
--    L'offre porte d'abord un marqueur unique `wfgen:<id de definition>` : c'est lui qui permet
--    de rattacher chaque produit a la bonne offre sans connaitre aucun identifiant a l'avance.
--    Il est remplace par la classe du furni juste apres, qui est ce que le client attend.
-- --------------------------------------------------------------------------------------------
INSERT INTO catalog_offers
       (page_id, localization_id, cost_credits, cost_currency, currency_type_id,
        can_gift, can_bundle, club_level, visible, discount_percent)
SELECT pg.id, CONCAT('wfgen:', x.id), 3, 0, NULL, 1, 1, 0, 1, 0
  FROM tmp_defs x
  JOIN (SELECT id, localization FROM catalog_pages
         WHERE localization LIKE 'wfkit\_%' AND deleted_at IS NULL) pg
    ON pg.localization COLLATE utf8mb4_general_ci = x.page_slug COLLATE utf8mb4_general_ci;

INSERT INTO catalog_products (offer_id, product_type, definition_id, quantity)
SELECT o.id, 0, CAST(SUBSTRING(o.localization_id, 7) AS UNSIGNED), 1
  FROM catalog_offers o
 WHERE o.localization_id LIKE 'wfgen:%' AND o.deleted_at IS NULL;

UPDATE catalog_offers o
  JOIN furniture_definitions d ON d.id = CAST(SUBSTRING(o.localization_id, 7) AS UNSIGNED)
   SET o.localization_id = d.name
 WHERE o.localization_id LIKE 'wfgen:%' AND o.deleted_at IS NULL;

-- --------------------------------------------------------------------------------------------
-- 7. Controle. Lis ces trois resultats avant de valider.
-- --------------------------------------------------------------------------------------------
-- Une table temporaire ne peut etre citee qu'une fois par requete (MySQL 1137), d'ou les
-- deux comptes en une passe plutot qu'en deux lignes d'UNION.
SELECT COUNT(*) AS boites_classees, SUM(cible = 'staff') AS dont_staff FROM tmp_defs;

SELECT 'pages creees' AS controle, COUNT(*) AS n FROM catalog_pages
          WHERE localization LIKE 'wfkit\_%' AND deleted_at IS NULL
UNION ALL SELECT 'offres creees', COUNT(*) FROM catalog_offers o
          JOIN catalog_pages pg ON pg.id = o.page_id
          WHERE pg.localization LIKE 'wfkit\_%' AND o.deleted_at IS NULL
UNION ALL SELECT 'offres sans produit (doit valoir 0)', COUNT(*) FROM catalog_offers o
          JOIN catalog_pages pg ON pg.id = o.page_id
          LEFT JOIN catalog_products pr ON pr.offer_id = o.id AND pr.deleted_at IS NULL
          WHERE pg.localization LIKE 'wfkit\_%' AND o.deleted_at IS NULL AND pr.id IS NULL
UNION ALL SELECT 'marqueurs wfgen restants (doit valoir 0)', COUNT(*) FROM catalog_offers
          WHERE localization_id LIKE 'wfgen:%' AND deleted_at IS NULL;

SELECT pg.sort_order AS ordre, pg.name AS page, pg.visible, COUNT(o.id) AS offres
  FROM catalog_pages pg
  LEFT JOIN catalog_offers o ON o.page_id = pg.id AND o.deleted_at IS NULL
 WHERE pg.localization LIKE 'wfkit\_%' AND pg.deleted_at IS NULL
 GROUP BY pg.id
 ORDER BY pg.sort_order, pg.name;

DROP TEMPORARY TABLE IF EXISTS tmp_pages;
DROP TEMPORARY TABLE IF EXISTS tmp_staff;
DROP TEMPORARY TABLE IF EXISTS tmp_offers;
DROP TEMPORARY TABLE IF EXISTS tmp_defs;
DROP TEMPORARY TABLE IF EXISTS tmp_targets;
DROP TEMPORARY TABLE IF EXISTS tmp_fams;

-- COMMIT;
-- ROLLBACK;
