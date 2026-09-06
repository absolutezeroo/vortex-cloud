-- Une piste de récompenses « vitrine », qui exerce tout ce que le système de séquences sait faire.
--
-- Pourquoi elle existe : jusqu'ici AUCUNE tâche de l'hôtel n'a de séquence. Les 16 tâches de la
-- piste `introduction` ont zéro ligne dans `reward_track_task_steps` — le catalogue leur synthétise
-- une étape unique à partir de leur `action_code`, ce qui les fait fonctionner mais ne prouve rien.
-- Rien en base ne montrait donc qu'une séquence multi-étapes, un filtre, ou une référence `$N`
-- fonctionnent bout en bout. Cette piste est cette preuve, et elle sert de modèle à recopier.
--
-- Ce qu'elle couvre, une capacité par tâche :
--   1. `furnish_a_visit`    — 3 étapes + DEUX références `$N` (le câble : « la même pièce », « le
--                             même meuble ») + un filtre sur une énumération fermée (sol/mur).
--   2. `say_the_word`       — `Contains` sur du texte libre : le fait `message`, ce que le joueur a
--                             réellement tapé. Le seul opérateur qui ait un sens dessus.
--   3. `open_the_casino`    — `Contains` sur le NOM d'une pièce, puis `$0` pour dire « dedans ».
--   4. `lucky_colours`      — `OneOf` sur une énumération fermée, et une tâche premium.
--   5. `tour_the_hotel`     — mode Distinct : « 15 pièces DIFFÉRENTES », dédupliqué sur le target.
--
-- Les opérateurs (`operator`) : 0 = Equals, 1 = NotEquals, 2 = OneOf, 3 = Contains.
-- Les modes (`mode`)          : 0 = Counter, 1 = Distinct, 2 = Absolute, 3 = Highest.
-- Le statut (`status`)        : 0 = Draft, 1 = Scheduled, 2 = Active.
--
-- Les faits utilisés ici sont ceux que les translators déclarent vraiment (`Shapes`) : `room`,
-- `item`, `kind`, `name`, `message`, `colour`. Un fait qu'aucun translator n'émet ne peut jamais
-- correspondre — c'est précisément ce que `VocabularyGovernanceTests` empêche côté code.
--
-- Pourquoi un script et pas une migration : c'est du CONTENU, pas du schéma. Un hôtel peut ne pas en
-- vouloir, et un admin doit pouvoir le supprimer depuis la dashboard sans qu'une migration le
-- réinstalle au prochain démarrage.
--
-- Idempotent : relançable sans créer de doublon. Le bloc 0 est une vérification à blanc.

-- ---------------------------------------------------------------------------------------------
-- 0) Vérification à blanc — la piste existe-t-elle déjà ?
-- ---------------------------------------------------------------------------------------------
SELECT t.id,
       t.track_id,
       t.status,
       (SELECT COUNT(*) FROM reward_track_tasks k WHERE k.reward_track_id = t.id) AS tasks
FROM reward_tracks t
WHERE t.track_id = 'sequence_showcase';

-- ---------------------------------------------------------------------------------------------
-- 1) La piste.
--    `hidden` = 1 : elle n'apparaît pas dans la liste du client tant qu'un admin ne l'a pas
--    décidé. Elle reste ouvrable dans la dashboard, qui est là où on veut la regarder.
-- ---------------------------------------------------------------------------------------------
INSERT INTO reward_tracks (track_id, theme, status, sort_order, completion_policy,
                           premium_enabled, premium_boost_permille, premium_cost_credits,
                           content_version, hidden, campaign_code, created_at, updated_at)
SELECT 'sequence_showcase', 'blue', 2, 900, 0,
       1, 1500, 50,
       1, 1, '', UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM reward_tracks WHERE track_id = 'sequence_showcase');

SET @track := (SELECT id FROM reward_tracks WHERE track_id = 'sequence_showcase' LIMIT 1);

-- ---------------------------------------------------------------------------------------------
-- 2) Les tâches.
--    `action_code` d'une tâche à séquence = celui de son étape 0 : le client ne dessine qu'une
--    icône par tâche et la tire de là.
-- ---------------------------------------------------------------------------------------------
INSERT INTO reward_track_tasks (reward_track_id, task_id, action_code, parameter, mode, premium,
                                sort_order, created_at, updated_at)
SELECT @track, v.task_id, v.action_code, '', v.mode, v.premium, v.sort_order,
       UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    SELECT 'furnish_a_visit' AS task_id, 'enter_other_users_room' AS action_code,
           0 AS mode, 0 AS premium, 10 AS sort_order
    UNION ALL SELECT 'say_the_word',    'chat_with_someone',      0, 0, 20
    UNION ALL SELECT 'open_the_casino', 'create_room',            0, 0, 30
    UNION ALL SELECT 'lucky_colours',   'open_mystery_box',       0, 1, 40
    UNION ALL SELECT 'tour_the_hotel',  'enter_other_users_room', 1, 0, 50
) v
WHERE NOT EXISTS (
    SELECT 1 FROM reward_track_tasks k
    WHERE k.reward_track_id = @track AND k.task_id = v.task_id
);

-- ---------------------------------------------------------------------------------------------
-- 3) Les paliers. Ce que le joueur doit atteindre, et ce que ça rapporte en points de piste.
-- ---------------------------------------------------------------------------------------------
INSERT INTO reward_track_task_levels (task_id, level_index, required_count, points_reward, premium,
                                      created_at, updated_at)
SELECT k.id, v.level_index, v.required_count, v.points_reward, v.premium,
       UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    -- La séquence complète, une fois. Un `required_count` plus haut voudrait dire la refaire
    -- entièrement : le curseur du joueur repart de l'étape 0 à chaque complétion.
    SELECT 'furnish_a_visit' AS task_id, 0 AS level_index, 1 AS required_count,
           30 AS points_reward, 0 AS premium
    UNION ALL SELECT 'say_the_word',     0,  3,  5, 0
    UNION ALL SELECT 'say_the_word',     1, 10, 10, 0
    UNION ALL SELECT 'open_the_casino',  0,  1, 25, 0
    UNION ALL SELECT 'lucky_colours',    0,  3, 20, 1
    UNION ALL SELECT 'tour_the_hotel',   0,  5, 10, 0
    UNION ALL SELECT 'tour_the_hotel',   1, 15, 25, 0
) v
JOIN reward_track_tasks k ON k.reward_track_id = @track AND k.task_id = v.task_id
WHERE NOT EXISTS (
    SELECT 1 FROM reward_track_task_levels l
    WHERE l.task_id = k.id AND l.level_index = v.level_index
);

-- ---------------------------------------------------------------------------------------------
-- 4) Les étapes. C'est ici que le système cesse d'être une liste de compteurs.
-- ---------------------------------------------------------------------------------------------
INSERT INTO reward_track_task_steps (task_id, step_index, action_code, parameter,
                                     created_at, updated_at)
SELECT k.id, v.step_index, v.action_code, '', UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    -- « Entre chez quelqu'un, pose un meuble au sol DANS CETTE pièce, puis monte DESSUS. »
    SELECT 'furnish_a_visit' AS task_id, 0 AS step_index, 'enter_other_users_room' AS action_code
    UNION ALL SELECT 'furnish_a_visit', 1, 'place_item'
    UNION ALL SELECT 'furnish_a_visit', 2, 'walk_on_furni'

    -- Une seule étape : une tâche « simple » est une séquence de un, il n'y a pas d'autre chemin.
    UNION ALL SELECT 'say_the_word',    0, 'chat_with_someone'

    -- « Ouvre un casino, puis meuble-le. »
    UNION ALL SELECT 'open_the_casino', 0, 'create_room'
    UNION ALL SELECT 'open_the_casino', 1, 'place_item'

    UNION ALL SELECT 'lucky_colours',   0, 'open_mystery_box'
    UNION ALL SELECT 'tour_the_hotel',  0, 'enter_other_users_room'
) v
JOIN reward_track_tasks k ON k.reward_track_id = @track AND k.task_id = v.task_id
WHERE NOT EXISTS (
    SELECT 1 FROM reward_track_task_steps s
    WHERE s.task_id = k.id AND s.step_index = v.step_index
);

-- ---------------------------------------------------------------------------------------------
-- 5) Les filtres. Un `value` en `$N` désigne une étape ANTÉRIEURE et lit ce qu'elle a enregistré
--    POUR LE MÊME FAIT — c'est tout ce que « le même meuble » veut dire, et c'est le câble que
--    l'éditeur de séquence dessine.
-- ---------------------------------------------------------------------------------------------
INSERT INTO reward_track_step_filters (step_id, sort_order, fact_key, operator, value,
                                       created_at, updated_at)
SELECT s.id, v.sort_order, v.fact_key, v.operator, v.value, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    -- furnish_a_visit / étape 1 : la MÊME pièce que celle où il vient d'entrer, et au sol.
    SELECT 'furnish_a_visit' AS task_id, 1 AS step_index, 0 AS sort_order,
           'room' AS fact_key, 0 AS operator, '$0' AS value
    UNION ALL SELECT 'furnish_a_visit', 1, 1, 'kind',    0, 'floor'
    -- furnish_a_visit / étape 2 : le MÊME meuble que celui qu'il vient de poser.
    UNION ALL SELECT 'furnish_a_visit', 2, 0, 'item',    0, '$1'

    -- say_the_word : ce qui a été dit contient le mot. Le seul opérateur utile sur du texte libre.
    UNION ALL SELECT 'say_the_word',    0, 0, 'message', 3, 'vortex'

    -- open_the_casino : le nom de la pièce créée, puis « dedans ».
    UNION ALL SELECT 'open_the_casino', 0, 0, 'name',    3, 'casino'
    UNION ALL SELECT 'open_the_casino', 1, 0, 'room',    0, '$0'

    -- lucky_colours : trois couleurs parmi les huit que le système déclare.
    UNION ALL SELECT 'lucky_colours',   0, 0, 'colour',  2, 'red,purple,turquoise'
) v
JOIN reward_track_tasks k ON k.reward_track_id = @track AND k.task_id = v.task_id
JOIN reward_track_task_steps s ON s.task_id = k.id AND s.step_index = v.step_index
WHERE NOT EXISTS (
    SELECT 1 FROM reward_track_step_filters f
    WHERE f.step_id = s.id AND f.fact_key = v.fact_key
);

-- ---------------------------------------------------------------------------------------------
-- 6) Les lots. Total atteignable : 30 + 15 + 25 + 20 + 35 = 125 points (dont 20 premium).
--    Kind 8 = monnaie (`reward_type_id` : -1 crédits, 0 duckets, 5 diamants), kind 12 = Habbicon.
-- ---------------------------------------------------------------------------------------------
INSERT INTO reward_track_prizes (reward_track_id, prize_id, required_points, premium, sort_order,
                                 created_at, updated_at)
SELECT @track, v.prize_id, v.required_points, v.premium, v.sort_order,
       UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    SELECT 'showcase_free_1' AS prize_id, 25 AS required_points, 0 AS premium, 10 AS sort_order
    UNION ALL SELECT 'showcase_prem_1', 45, 1, 15
    UNION ALL SELECT 'showcase_free_2', 70, 0, 20
    UNION ALL SELECT 'showcase_free_3', 110, 0, 30
) v
WHERE NOT EXISTS (
    SELECT 1 FROM reward_track_prizes p
    WHERE p.reward_track_id = @track AND p.prize_id = v.prize_id
);

INSERT INTO reward_track_prize_rewards (prize_id, kind, reward_type_id, amount, extra_params,
                                        sort_order, created_at, updated_at)
SELECT p.id, v.kind, v.reward_type_id, v.amount, '', 0, UTC_TIMESTAMP(), UTC_TIMESTAMP()
FROM (
    SELECT 'showcase_free_1' AS prize_id, 8 AS kind, '0' AS reward_type_id, 150 AS amount
    UNION ALL SELECT 'showcase_prem_1', 8,  '-1', 100
    UNION ALL SELECT 'showcase_free_2', 12, '28',   1  -- Habbicon `duck_duck`
    UNION ALL SELECT 'showcase_free_3', 8,  '-1', 250
) v
JOIN reward_track_prizes p ON p.reward_track_id = @track AND p.prize_id = v.prize_id
WHERE NOT EXISTS (
    SELECT 1 FROM reward_track_prize_rewards r WHERE r.prize_id = p.id
);

-- ---------------------------------------------------------------------------------------------
-- 7) Contrôle — ce que la piste contient maintenant, séquence par séquence.
--    `$N` doit toujours désigner une étape d'index INFÉRIEUR ; sinon la référence ne résout rien.
-- ---------------------------------------------------------------------------------------------
SELECT k.task_id,
       k.mode,
       k.premium,
       s.step_index,
       s.action_code,
       COALESCE(GROUP_CONCAT(
           CONCAT(f.fact_key,
                  ELT(f.operator + 1, ' = ', ' != ', ' one of ', ' contains '),
                  f.value)
           ORDER BY f.sort_order SEPARATOR ' AND '
       ), '(aucun filtre)') AS filters
FROM reward_track_tasks k
JOIN reward_track_task_steps s ON s.task_id = k.id AND s.deleted_at IS NULL
LEFT JOIN reward_track_step_filters f ON f.step_id = s.id AND f.deleted_at IS NULL
WHERE k.reward_track_id = @track AND k.deleted_at IS NULL
-- `sort_order` est dans le GROUP BY parce qu'il est dans le ORDER BY : sous `only_full_group_by`,
-- qui est le mode par défaut ici, trier sur une colonne non agrégée et non groupée est refusé.
GROUP BY k.sort_order, k.task_id, k.mode, k.premium, s.step_index, s.action_code
ORDER BY k.sort_order, s.step_index;

-- Filet : une référence qui pointe en avant, ou sur elle-même, ne résoudra jamais.
SELECT k.task_id, s.step_index, f.fact_key, f.value AS reference_invalide
FROM reward_track_tasks k
JOIN reward_track_task_steps s ON s.task_id = k.id
JOIN reward_track_step_filters f ON f.step_id = s.id
WHERE k.reward_track_id = @track
  AND f.value LIKE '$%'
  AND CAST(SUBSTRING(f.value, 2) AS UNSIGNED) >= s.step_index;
