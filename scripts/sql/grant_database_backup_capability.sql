-- Donne la nouvelle capacité `dashboard.ops.database.backup` aux rôles qui doivent voir et
-- déclencher les sauvegardes.
--
-- Pourquoi un script et pas une migration : les lignes de `role_permissions` sont des données
-- d'exploitation, pas du schéma. Chaque hôtel décide qui a le droit ; une migration imposerait ce
-- choix à tout le monde.
--
-- Pourquoi c'est nécessaire : seul le rôle `owner` porte le wildcard `*`. `admin` a ses capacités
-- listées une par une (18 au 2026-08-16), donc sans cette ligne un admin ne verra pas le panneau
-- « Sauvegardes de la base » sur la page Infrastructure — et le panneau ne dira pas pourquoi, il
-- sera simplement absent.
--
-- Idempotent : relançable sans créer de doublon.

-- 1) Vérification à blanc — qui l'a déjà, qui ne l'a pas.
SELECT r.`key`                                            AS role_key,
       MAX(p.capability_key = '*')                        AS has_wildcard,
       MAX(p.capability_key = 'dashboard.ops.database.backup') AS has_backup
FROM roles r
LEFT JOIN role_permissions p
       ON p.role_id = r.id
      AND p.deleted_at IS NULL
GROUP BY r.id, r.`key`
ORDER BY r.id;

-- 2) L'octroi. Ajoute ou retire des `key` de la liste selon ce que tu veux.
--    `owner` est volontairement inclus : il a le wildcard aujourd'hui, mais une ligne explicite
--    survit à un resserrement futur de ce rôle.
INSERT INTO role_permissions (role_id, capability_key)
SELECT r.id, 'dashboard.ops.database.backup'
FROM roles r
WHERE r.`key` IN ('owner', 'admin')
  AND r.deleted_at IS NULL
  AND NOT EXISTS (
        SELECT 1
        FROM role_permissions p
        WHERE p.role_id = r.id
          AND p.capability_key = 'dashboard.ops.database.backup'
          AND p.deleted_at IS NULL
      );

-- 3) Contrôle après coup.
SELECT r.`key` AS role_key, p.capability_key, p.created_at
FROM role_permissions p
JOIN roles r ON r.id = p.role_id
WHERE p.capability_key = 'dashboard.ops.database.backup'
  AND p.deleted_at IS NULL
ORDER BY r.id;
