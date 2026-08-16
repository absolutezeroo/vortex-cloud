-- Met en vente toutes les boîtes wired que la section Wired du catalogue ne vend pas.
-- Additif : ne modifie, ne déplace et ne réaffiche aucune offre existante.

-- 1. Les deux pages de famille qui manquent sous la racine wired.
INSERT INTO catalog_pages (parent_id, localization, name, icon, layout, image_data, sort_order, visible, catalog_type)
SELECT r.id, v.localization, v.name, r.icon, 'default_3x3', r.image_data, v.sort_order, 1, r.catalog_type
FROM catalog_pages r
JOIN (SELECT 'selectors' AS localization, 'Selectors' AS name, 304 AS sort_order
      UNION ALL SELECT 'variables', 'Variables', 305) v
WHERE r.localization = 'wired_furniture' AND r.deleted_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM catalog_pages c
                  WHERE c.parent_id = r.id AND c.localization = v.localization AND c.deleted_at IS NULL);

-- 2. Une offre à 3 crédits par classname wired non vendu dans la section.
CREATE TEMPORARY TABLE wired_todo AS
WITH RECURSIVE section AS (
  SELECT id FROM catalog_pages WHERE localization = 'wired_furniture' AND deleted_at IS NULL
  UNION ALL
  SELECT c.id FROM catalog_pages c JOIN section s ON c.parent_id = s.id WHERE c.deleted_at IS NULL
)
SELECT MIN(f.id) AS definition_id, f.name, f.type AS product_type,
       (SELECT c.id FROM catalog_pages c JOIN catalog_pages r ON r.id = c.parent_id
         WHERE r.localization = 'wired_furniture' AND c.deleted_at IS NULL
           AND c.localization = CASE
                 WHEN f.name LIKE 'wf\_trg\_%'  THEN 'triggers'
                 WHEN f.name LIKE 'wf\_act\_%'  THEN 'effects'
                 WHEN f.name LIKE 'wf\_cnd\_%'  THEN 'conditions'
                 WHEN f.name LIKE 'wf\_slc\_%'  THEN 'selectors'
                 WHEN f.name LIKE 'wf\_var\_%'  THEN 'variables'
                 WHEN f.name LIKE 'wf\_xtra\_%' THEN 'wired_addons' END
         LIMIT 1) AS page_id
FROM furniture_definitions f
WHERE f.deleted_at IS NULL
  AND f.name REGEXP '^wf_(trg|act|cnd|slc|var|xtra)_'
  AND NOT EXISTS (
    SELECT 1 FROM catalog_products p
    JOIN catalog_offers o ON o.id = p.offer_id
    JOIN furniture_definitions f2 ON f2.id = p.definition_id
    WHERE f2.name = f.name AND p.deleted_at IS NULL AND o.deleted_at IS NULL
      AND o.visible = 1 AND o.page_id IN (SELECT id FROM section))
GROUP BY f.name, f.type;

INSERT INTO catalog_offers (page_id, localization_id, cost_credits, cost_currency, can_gift, can_bundle, club_level, visible)
SELECT page_id, name, 3, 0, 1, 1, 0, 1 FROM wired_todo WHERE page_id IS NOT NULL;

INSERT INTO catalog_products (offer_id, product_type, definition_id, quantity, unique_size, unique_remaining, builders_club_eligible)
SELECT o.id, t.product_type, t.definition_id, 1, 0, 0, 0
FROM wired_todo t
JOIN catalog_offers o ON o.localization_id = t.name AND o.page_id = t.page_id
WHERE t.page_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM catalog_products p WHERE p.offer_id = o.id);

DROP TEMPORARY TABLE wired_todo;
