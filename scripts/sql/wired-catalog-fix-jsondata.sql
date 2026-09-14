-- ============================================================================================
-- Reparation : image_data / text_data vides sur les pages de catalogue
--
-- `catalog_pages.image_data` et `text_data` sont mappees en JSON cote EF (`List<string>?`).
-- Les seules valeurs valides sont un tableau JSON ou NULL. Une chaine vide fait echouer la
-- lecture de la page entiere :
--
--   System.InvalidOperationException: The empty string is not valid JSON.
--   ERR DashboardWebHost : GET /api/v1/catalog/pages/<id>
--
-- Le script de reconstruction wired ecrivait '' sur les pages qu'il creait. A lancer une fois ;
-- il repare aussi toute autre page du catalogue dans le meme etat, d'ou qu'elle vienne.
--
-- Sans danger et rejouable : il ne touche QUE les lignes dont la valeur est la chaine vide.
-- ============================================================================================

SET NAMES utf8mb4;

START TRANSACTION;

-- Ce que la maison utilise partout ailleurs : l'entete standard pour l'image, rien pour le texte.
UPDATE catalog_pages SET image_data = '["hubbly_h"]' WHERE image_data = '';
UPDATE catalog_pages SET text_data  = NULL           WHERE text_data  = '';

-- Controle : les deux comptes doivent valoir 0 apres coup.
SELECT SUM(image_data = '') AS image_data_vides_restants,
       SUM(text_data  = '') AS text_data_vides_restants,
       COUNT(*)             AS pages_examinees
  FROM catalog_pages;

-- COMMIT;
-- ROLLBACK;
