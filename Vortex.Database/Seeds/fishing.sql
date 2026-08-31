-- Fishing reference data.
--
-- Reconstructed from Habbo Hotel: Origins, which has no client dump available. The three zones,
-- their level bands and the Fishing Frenzy schedule are what the community guides report. EVERY
-- NUMBER BELOW IS A GUESS -- catch rates, weights, XP, token rewards, the level curve and the rod
-- tiers were all chosen here, not recovered, which is exactly why they live in tables an operator
-- can edit while the hotel is running rather than in code. See the client's
-- docs/vortex-original/fishing.md, which states the evidence level for each part of the system.
--
-- INSERT IGNORE throughout, so re-running this against a hotel that has already been tuned changes
-- nothing: an operator's edits win over these defaults.
--
-- The tunables are NOT here. The daily cap, the sighting delays, the frenzy schedule and the Hook
-- Havoc parameters are admin-editable gameplay config and live in `IServerConfigGrain` under the
-- `fishing.*` keys (see Vortex.Fishing/FishingConfig.cs), which is write-through and therefore live
-- without a reload. Only content -- species, zones, rod tiers, the level curve -- is seeded here.
--
-- The furni classes (vtx_fishing_spot_*) and the trophy (vtx_fishing_trophy) do not exist in any
-- Habbo furnidata. A hotel that has not added them will find no spot to click, which is the honest
-- outcome -- the alternative is pointing the zones at real furniture and turning somebody's pond
-- decoration into a fishing spot.

-- Zones. The level bands are Origins': Infobus Park 1-29, Port Hana 30-69, Snouthill Pier 70+.
-- min/max catches are the "one fish or several" the guides describe, made concrete.
INSERT IGNORE INTO fishing_zones
    (id, name_key, furni_class, required_level, min_catches, max_catches, created_at)
VALUES
    (1, 'vortex.fishing.zone.infobus_park',   'vtx_fishing_spot_park',      0,  1, 6,  UTC_TIMESTAMP()),
    (2, 'vortex.fishing.zone.port_hana',      'vtx_fishing_spot_hana',      30, 2, 8,  UTC_TIMESTAMP()),
    (3, 'vortex.fishing.zone.snouthill_pier', 'vtx_fishing_spot_snouthill', 70, 3, 10, UTC_TIMESTAMP());

-- Species. Eighteen, drawn from the artwork the hotel ships (`fish_<name>.png`): the sprite is
-- resolved from the last segment of `name_key`, so a species named here without a matching image
-- simply shows none in the Fishopedia. The first cut invented names — perch, night_eel, moon_squid,
-- ice_char, golden_koi — for which no artwork exists; these are real ones from that set.
--
-- Enough to exercise every axis the roll turns on -- level, hour, weekday and season -- without
-- inventing a Fishopedia wholesale.
--
-- active_hours is a 24-bit mask over UTC hours, active_weekdays a 7-bit mask with bit 0 = Sunday,
-- and active_seasons a 4-bit mask (spring, summer, autumn, winter). 16777215 / 127 / 15 mean "always".
INSERT IGNORE INTO fishing_species
    (id, zone_id, name_key, required_level, rarity_stars, catch_rate, rarity_weight,
     min_weight, max_weight, xp_reward, golden_xp_bonus, currency_reward,
     active_hours, active_weekdays, active_seasons, created_at)
VALUES
    -- Infobus Park. The starting zone: common, forgiving, and worth little.
    (1, 1, 'vortex.fishing.species.minnow',    0,  1, 900, 400, 10,  60,   5,  20,  1, 16777215, 127, 15, UTC_TIMESTAMP()),
    (2, 1, 'vortex.fishing.species.bluegill',  0,  1, 850, 350, 30,  140,  7,  28,  2, 16777215, 127, 15, UTC_TIMESTAMP()),
    (3, 1, 'vortex.fishing.species.roach',     0,  1, 800, 300, 40,  180,  8,  30,  2, 16777215, 127, 15, UTC_TIMESTAMP()),
    (4, 1, 'vortex.fishing.species.tadpole',   3,  2, 600, 150, 5,   25,   12, 45,  3, 16777215, 127, 7,  UTC_TIMESTAMP()),
    (5, 1, 'vortex.fishing.species.frog',      5,  3, 400, 60,  20,  90,   25, 90,  6, 16777215, 127, 7,  UTC_TIMESTAMP()),
    -- Nocturnal: 20:00-04:59 UTC. Bits 20-23 and 0-4 -- 15728671. The first version wrote
    -- 15728655, which is bits 20-23 and 0-3: one hour short, ending the window at 04:00
    -- rather than 05:00. The Fishopedia's hour-range formatter is what surfaced it.
    (6, 1, 'vortex.fishing.species.eel',       10, 4, 300, 40,  80,  400,  40, 140, 9, 15728671, 127, 15, UTC_TIMESTAMP()),

    -- Port Hana.
    (7,  2, 'vortex.fishing.species.silver_bream',    30, 2, 750, 300, 120, 600,  20, 70,  5,  16777215, 127, 15, UTC_TIMESTAMP()),
    (8,  2, 'vortex.fishing.species.flounder',     30, 2, 720, 280, 140, 650,  22, 75,  5,  16777215, 127, 15, UTC_TIMESTAMP()),
    (9,  2, 'vortex.fishing.species.king_mackerel',30, 2, 700, 250, 150, 700,  24, 80,  6,  16777215, 127, 15, UTC_TIMESTAMP()),
    (10, 2, 'vortex.fishing.species.barracuda',    38, 3, 500, 120, 300, 1200, 35, 120, 9,  16777215, 127, 15, UTC_TIMESTAMP()),
    (11, 2, 'vortex.fishing.species.snapper',      40, 4, 280, 50,  200, 900,  60, 200, 14, 15728671, 127, 15, UTC_TIMESTAMP()),
    -- Summer only.
    (12, 2, 'vortex.fishing.species.mahi_mahi',    45, 4, 260, 45,  400, 1600, 70, 240, 16, 16777215, 127, 2,  UTC_TIMESTAMP()),

    -- Snouthill Pier. The last zone: rare, heavy, and where the tokens are.
    (13, 3, 'vortex.fishing.species.halibut',   70, 3, 600, 200, 500,  2400, 45,  160, 12, 16777215, 127, 15, UTC_TIMESTAMP()),
    (14, 3, 'vortex.fishing.species.sturgeon',  70, 3, 550, 180, 600,  2800, 50,  180, 14, 16777215, 127, 15, UTC_TIMESTAMP()),
    (15, 3, 'vortex.fishing.species.tuna',      74, 4, 420, 110, 900,  3400, 60,  210, 18, 16777215, 127, 15, UTC_TIMESTAMP()),
    (16, 3, 'vortex.fishing.species.swordfish', 78, 4, 350, 80,  1000, 3800, 75,  260, 22, 15728671, 127, 15, UTC_TIMESTAMP()),
    -- Winter only, and only at the weekend: bit 0 (Sunday) and bit 6 (Saturday) -- 65.
    (17, 3, 'vortex.fishing.species.sockeye_salmon', 80, 5, 180, 20, 800,  3600, 120, 420, 30, 16777215, 65,  8,  UTC_TIMESTAMP()),
    (18, 3, 'vortex.fishing.species.marlin',        90, 5, 120, 10, 1000, 5000, 200, 700, 50, 16777215, 127, 15, UTC_TIMESTAMP());

-- The fishing level curve. Unlocks zones and nothing else observed in Origins, so the thresholds
-- only have to put Port Hana (30) and Snouthill Pier (70) at a sensible distance from each other.
-- Quadratic: 60 * level^2 / 2, rounded to something readable.
INSERT IGNORE INTO fishing_levels (id, level, xp_threshold, created_at) VALUES
    (1, 1, 0, UTC_TIMESTAMP()),        (2, 5, 900, UTC_TIMESTAMP()),
    (3, 10, 3600, UTC_TIMESTAMP()),    (4, 15, 8600, UTC_TIMESTAMP()),
    (5, 20, 15800, UTC_TIMESTAMP()),   (6, 25, 25200, UTC_TIMESTAMP()),
    (7, 30, 36900, UTC_TIMESTAMP()),   (8, 40, 67200, UTC_TIMESTAMP()),
    (9, 50, 108000, UTC_TIMESTAMP()),  (10, 60, 159000, UTC_TIMESTAMP()),
    (11, 70, 220500, UTC_TIMESTAMP()), (12, 80, 292000, UTC_TIMESTAMP()),
    (13, 90, 374000, UTC_TIMESTAMP()), (14, 100, 466000, UTC_TIMESTAMP());

-- Rod tiers. A SEPARATE progression from the level, on its own XP counter and its own curve --
-- conflating the two was the second-biggest error in this system's first design. The rod raises the
-- multipliers and the chance of triggering Hook Havoc; it unlocks nothing.
--
-- Hand item ids are >= 1000 on purpose: the client's CARRY_ITEM_LAST_CONSUMABLE is 999, and below it
-- the avatar plays the drinking animation instead of holding the rod.
-- catch_multiplier and golden_multiplier are thousandths (1000 = x1.00); hook_havoc_chance is
-- tenths of a percent.
INSERT IGNORE INTO fishing_rod_tiers
    (id, quality, xp_threshold, name_key, hand_item_id, catch_multiplier, golden_multiplier,
     hook_havoc_chance, created_at)
VALUES
    (1, 1, 0,      'vortex.fishing.rod.branch',   1000, 1000, 1000, 20,  UTC_TIMESTAMP()),
    (2, 2, 5000,   'vortex.fishing.rod.bamboo',   1001, 1100, 1150, 35,  UTC_TIMESTAMP()),
    (3, 3, 25000,  'vortex.fishing.rod.carbon',   1002, 1250, 1400, 55,  UTC_TIMESTAMP()),
    (4, 4, 90000,  'vortex.fishing.rod.titanium', 1003, 1400, 1700, 80,  UTC_TIMESTAMP()),
    (5, 5, 250000, 'vortex.fishing.rod.legend',   1004, 1600, 2100, 120, UTC_TIMESTAMP());
