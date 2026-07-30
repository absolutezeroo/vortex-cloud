-- Pet offers were imported as furniture, so buying a pet handed out a placeholder item.
--
-- Every product on a `pets` page points at a furniture definition named `a0 pet<N>`, where N is the
-- pet type. They arrived with product_type 0 (floor furniture) and no extra_param, so the catalog
-- grant took its furniture branch and inserted `a0 pet<N>` into the buyer's inventory. The pet
-- branch it should have taken reads the type out of extra_param.
--
-- Scoped to pages whose layout is `pets`, which is the layout Habbo sells pets from. Seven further
-- `a0 pet<N>` products sit on furniture and badge pages (Trash Bin, Badge Dump, Wedding, ...);
-- those are left alone deliberately -- turning a product on a badge page into a pet is a guess,
-- and nothing sells pets from there.
--
-- Re-running is harmless: the WHERE only matches rows still typed as furniture.

UPDATE catalog_products p
    JOIN catalog_offers o ON o.id = p.offer_id
    JOIN catalog_pages pg ON pg.id = o.page_id
    JOIN furniture_definitions f ON f.id = p.definition_id
SET p.product_type = 6,
    p.extra_param = SUBSTRING(f.name, 7)
WHERE pg.layout = 'pets'
  AND f.name REGEXP '^a0 pet[0-9]+$'
  AND p.product_type = 0;
