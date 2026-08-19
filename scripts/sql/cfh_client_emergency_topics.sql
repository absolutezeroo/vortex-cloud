-- Ajoute les quatre topics CFH que le formulaire « Make a help request » du client code en dur.
--
-- Pourquoi c'est nécessaire : ce formulaire ne lit PAS ses topics sur le réseau. Son sélecteur vient
-- du layout `emergency_help_request`, où quatre boutons radio portent les ids 121, 122, 123 et 124
-- (captions `help.emergency.main.step.one.topic.<id>`). Le client enverra donc toujours l'un de ces
-- quatre ids. Le catalogue par défaut, lui, était seedé en auto-increment (1 à 9) : `GetTopicAsync`
-- ne trouvait rien et CfhReportHelper jetait le signalement — sans ticket et sans réponse.
--
-- Pourquoi un script et pas une migration : le catalogue CFH est une donnée d'exploitation qu'un
-- admin peut restructurer. `CfhCatalogSeederService` ne réagit que sur une base vierge, donc les
-- hôtels déjà seedés ont besoin de ce rattrapage une fois. Les nouvelles installations reçoivent
-- désormais ces lignes directement du seeder.
--
-- Idempotent : relançable sans créer de doublon.

-- 1) Vérification à blanc — ces ids existent-ils déjà ?
SELECT t.id,
       t.name,
       c.name AS category
FROM cfh_topics t
LEFT JOIN cfh_categories c ON c.id = t.category_id
WHERE t.id IN (121, 122, 123, 124)
ORDER BY t.id;

-- 2) La catégorie qui les porte.
INSERT INTO cfh_categories (name, display_order, created_at, updated_at)
SELECT 'Urgent', 99, UTC_TIMESTAMP(), UTC_TIMESTAMP()
WHERE NOT EXISTS (SELECT 1 FROM cfh_categories WHERE name = 'Urgent');

-- 3) Les quatre topics, à leurs ids imposés par le client.
--    Le libellé reprend la caption du client pour qu'un modérateur lise ce que le joueur a lu.
INSERT INTO cfh_topics (id, category_id, name, consequence, display_order, created_at, updated_at)
SELECT v.id,
       (SELECT id FROM cfh_categories WHERE name = 'Urgent' ORDER BY id LIMIT 1),
       v.name,
       v.consequence,
       v.display_order,
       UTC_TIMESTAMP(),
       UTC_TIMESTAMP()
FROM (
    SELECT 121 AS id, 'Someone is being sexually explicit'        AS name, 'Ban 1 week' AS consequence, 0 AS display_order
    UNION ALL SELECT 122, 'Someone is sharing personal details',        'Ban 3 days', 1
    UNION ALL SELECT 123, 'Someone is bullying another Habbo',          'Ban 3 days', 2
    UNION ALL SELECT 124, 'Someone is being threatening or dangerous',  'Ban 1 week', 3
) AS v
WHERE NOT EXISTS (SELECT 1 FROM cfh_topics t WHERE t.id = v.id);

-- 4) Contrôle final — les quatre doivent être là.
SELECT t.id,
       t.name,
       t.consequence,
       c.name AS category
FROM cfh_topics t
LEFT JOIN cfh_categories c ON c.id = t.category_id
WHERE t.id IN (121, 122, 123, 124)
ORDER BY t.id;
