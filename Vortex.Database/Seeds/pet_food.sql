-- Pet food seed
-- Columns: furniture_definition_id, pet_type, nutrition, energy, max_uses
-- pet_type: 0=dog 1=cat 2=croco 3=terrier 4=bear 5=pig 6=bunny 7=chick 8=devil 9=monkey
--           10=horse 11=gorilla 12=bunyip 13=frog 14=dragon 15=horse(alt) 16=meow 17=pig2 18=generic
-- Food items restore nutrition (energy=0).
-- Drink items restore energy (nutrition=0).

-- ─── FOOD (petfood1–24, logic = pet_food) ────────────────────────────────────
-- total_states=4 → 4 uses ; total_states=5 → 5 uses
INSERT IGNORE INTO `pet_food` (`furniture_definition_id`, `pet_type`, `nutrition`, `energy`, `max_uses`, `created_at`, `updated_at`)
VALUES
  -- petfood1  (id 1532, 4 states)
  (1532,  0, 20, 0, 4, NOW(), NOW()),
  -- petfood2  (id 1533, 0 states – fallback 4)
  (1533,  1, 20, 0, 4, NOW(), NOW()),
  -- petfood3  (id 1534, 4 states)
  (1534,  2, 20, 0, 4, NOW(), NOW()),
  -- petfood4  (id 2131, 4 states)
  (2131,  3, 20, 0, 4, NOW(), NOW()),
  -- petfood5  (id 3324, 4 states)
  (3324,  4, 20, 0, 4, NOW(), NOW()),
  -- petfood6  (id 3323, 4 states)
  (3323,  5, 20, 0, 4, NOW(), NOW()),
  -- petfood7  (id 3321, 4 states)
  (3321,  6, 20, 0, 4, NOW(), NOW()),
  -- petfood8  (id 3325, 4 states)
  (3325,  7, 20, 0, 4, NOW(), NOW()),
  -- petfood9  (id 3322, 0 states – fallback 4)
  (3322,  8, 20, 0, 4, NOW(), NOW()),
  -- petfood10 (id 3326, 4 states)
  (3326,  9, 20, 0, 4, NOW(), NOW()),
  -- petfood11 (id 3358, 5 states)
  (3358, 10, 20, 0, 5, NOW(), NOW()),
  -- petfood12 (id 3370, logic=none in source – skip or keep as decoration)
  -- petfood13 (id 3359, 5 states)
  (3359, 11, 20, 0, 5, NOW(), NOW()),
  -- petfood14 (id 3599, 4 states)
  (3599, 12, 20, 0, 4, NOW(), NOW()),
  -- petfood15 (id 3583, 5 states)
  (3583, 13, 20, 0, 5, NOW(), NOW()),
  -- petfood16 (id 3744, 5 states)
  (3744, 14, 20, 0, 5, NOW(), NOW()),
  -- petfood17 (id 3817, 4 states)
  (3817, 15, 20, 0, 4, NOW(), NOW()),
  -- petfood18 (id 3816, 4 states)
  (3816, 16, 20, 0, 4, NOW(), NOW()),
  -- petfood19 (id 3821, 4 states)
  (3821, 17, 20, 0, 4, NOW(), NOW()),
  -- petfood21 (id 3907, 0 states – fallback 4)
  (3907, 18, 20, 0, 4, NOW(), NOW()),
  -- petfood22 (id 4000, 4 states) – extra variety, also maps to type 0
  (4000,  0, 20, 0, 4, NOW(), NOW()),
  -- petfood23 (id 3995, 4 states) – extra variety, also maps to type 1
  (3995,  1, 20, 0, 4, NOW(), NOW()),
  -- petfood24 (id 4037, 4 states) – extra variety, also maps to type 2
  (4037,  2, 20, 0, 4, NOW(), NOW());

-- ─── DRINK (waterbowl*1–5, logic = pet_drink) ────────────────────────────────
-- Restores energy (nutrition=0), 6 states → 6 uses.
-- Waterbowls are shared across all pet types – one row per type × bowl variant.
-- Each bowl variant maps to all pet types (0–18).

INSERT IGNORE INTO `pet_food` (`furniture_definition_id`, `pet_type`, `nutrition`, `energy`, `max_uses`, `created_at`, `updated_at`)
VALUES
  -- waterbowl*1 (id 1538)
  (1538,  0, 0, 20, 5, NOW(), NOW()),
  (1538,  1, 0, 20, 5, NOW(), NOW()),
  (1538,  2, 0, 20, 5, NOW(), NOW()),
  (1538,  3, 0, 20, 5, NOW(), NOW()),
  (1538,  4, 0, 20, 5, NOW(), NOW()),
  (1538,  5, 0, 20, 5, NOW(), NOW()),
  (1538,  6, 0, 20, 5, NOW(), NOW()),
  (1538,  7, 0, 20, 5, NOW(), NOW()),
  (1538,  8, 0, 20, 5, NOW(), NOW()),
  (1538,  9, 0, 20, 5, NOW(), NOW()),
  (1538, 10, 0, 20, 5, NOW(), NOW()),
  (1538, 11, 0, 20, 5, NOW(), NOW()),
  (1538, 12, 0, 20, 5, NOW(), NOW()),
  (1538, 13, 0, 20, 5, NOW(), NOW()),
  (1538, 14, 0, 20, 5, NOW(), NOW()),
  (1538, 15, 0, 20, 5, NOW(), NOW()),
  (1538, 16, 0, 20, 5, NOW(), NOW()),
  (1538, 17, 0, 20, 5, NOW(), NOW()),
  (1538, 18, 0, 20, 5, NOW(), NOW()),
  -- waterbowl*2 (id 1537)
  (1537,  0, 0, 20, 5, NOW(), NOW()),
  (1537,  1, 0, 20, 5, NOW(), NOW()),
  (1537,  2, 0, 20, 5, NOW(), NOW()),
  (1537,  3, 0, 20, 5, NOW(), NOW()),
  (1537,  4, 0, 20, 5, NOW(), NOW()),
  (1537,  5, 0, 20, 5, NOW(), NOW()),
  (1537,  6, 0, 20, 5, NOW(), NOW()),
  (1537,  7, 0, 20, 5, NOW(), NOW()),
  (1537,  8, 0, 20, 5, NOW(), NOW()),
  (1537,  9, 0, 20, 5, NOW(), NOW()),
  (1537, 10, 0, 20, 5, NOW(), NOW()),
  (1537, 11, 0, 20, 5, NOW(), NOW()),
  (1537, 12, 0, 20, 5, NOW(), NOW()),
  (1537, 13, 0, 20, 5, NOW(), NOW()),
  (1537, 14, 0, 20, 5, NOW(), NOW()),
  (1537, 15, 0, 20, 5, NOW(), NOW()),
  (1537, 16, 0, 20, 5, NOW(), NOW()),
  (1537, 17, 0, 20, 5, NOW(), NOW()),
  (1537, 18, 0, 20, 5, NOW(), NOW()),
  -- waterbowl*3 (id 1539)
  (1539,  0, 0, 20, 5, NOW(), NOW()),
  (1539,  1, 0, 20, 5, NOW(), NOW()),
  (1539,  2, 0, 20, 5, NOW(), NOW()),
  (1539,  3, 0, 20, 5, NOW(), NOW()),
  (1539,  4, 0, 20, 5, NOW(), NOW()),
  (1539,  5, 0, 20, 5, NOW(), NOW()),
  (1539,  6, 0, 20, 5, NOW(), NOW()),
  (1539,  7, 0, 20, 5, NOW(), NOW()),
  (1539,  8, 0, 20, 5, NOW(), NOW()),
  (1539,  9, 0, 20, 5, NOW(), NOW()),
  (1539, 10, 0, 20, 5, NOW(), NOW()),
  (1539, 11, 0, 20, 5, NOW(), NOW()),
  (1539, 12, 0, 20, 5, NOW(), NOW()),
  (1539, 13, 0, 20, 5, NOW(), NOW()),
  (1539, 14, 0, 20, 5, NOW(), NOW()),
  (1539, 15, 0, 20, 5, NOW(), NOW()),
  (1539, 16, 0, 20, 5, NOW(), NOW()),
  (1539, 17, 0, 20, 5, NOW(), NOW()),
  (1539, 18, 0, 20, 5, NOW(), NOW()),
  -- waterbowl*4 (id 1535)
  (1535,  0, 0, 20, 5, NOW(), NOW()),
  (1535,  1, 0, 20, 5, NOW(), NOW()),
  (1535,  2, 0, 20, 5, NOW(), NOW()),
  (1535,  3, 0, 20, 5, NOW(), NOW()),
  (1535,  4, 0, 20, 5, NOW(), NOW()),
  (1535,  5, 0, 20, 5, NOW(), NOW()),
  (1535,  6, 0, 20, 5, NOW(), NOW()),
  (1535,  7, 0, 20, 5, NOW(), NOW()),
  (1535,  8, 0, 20, 5, NOW(), NOW()),
  (1535,  9, 0, 20, 5, NOW(), NOW()),
  (1535, 10, 0, 20, 5, NOW(), NOW()),
  (1535, 11, 0, 20, 5, NOW(), NOW()),
  (1535, 12, 0, 20, 5, NOW(), NOW()),
  (1535, 13, 0, 20, 5, NOW(), NOW()),
  (1535, 14, 0, 20, 5, NOW(), NOW()),
  (1535, 15, 0, 20, 5, NOW(), NOW()),
  (1535, 16, 0, 20, 5, NOW(), NOW()),
  (1535, 17, 0, 20, 5, NOW(), NOW()),
  (1535, 18, 0, 20, 5, NOW(), NOW()),
  -- waterbowl*5 (id 1536)
  (1536,  0, 0, 20, 5, NOW(), NOW()),
  (1536,  1, 0, 20, 5, NOW(), NOW()),
  (1536,  2, 0, 20, 5, NOW(), NOW()),
  (1536,  3, 0, 20, 5, NOW(), NOW()),
  (1536,  4, 0, 20, 5, NOW(), NOW()),
  (1536,  5, 0, 20, 5, NOW(), NOW()),
  (1536,  6, 0, 20, 5, NOW(), NOW()),
  (1536,  7, 0, 20, 5, NOW(), NOW()),
  (1536,  8, 0, 20, 5, NOW(), NOW()),
  (1536,  9, 0, 20, 5, NOW(), NOW()),
  (1536, 10, 0, 20, 5, NOW(), NOW()),
  (1536, 11, 0, 20, 5, NOW(), NOW()),
  (1536, 12, 0, 20, 5, NOW(), NOW()),
  (1536, 13, 0, 20, 5, NOW(), NOW()),
  (1536, 14, 0, 20, 5, NOW(), NOW()),
  (1536, 15, 0, 20, 5, NOW(), NOW()),
  (1536, 16, 0, 20, 5, NOW(), NOW()),
  (1536, 17, 0, 20, 5, NOW(), NOW()),
  (1536, 18, 0, 20, 6, NOW(), NOW());

-- ─── NEST: update basket furni logic so the emulator recognises them ──────────
UPDATE `furniture_definitions`
SET `logic` = 'furniture_pet_nest'
WHERE `id` IN (4827, 4828);
