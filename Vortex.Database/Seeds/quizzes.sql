-- The two quizzes the client can open, with their answer keys.
--
-- Question numbers and answer indexes are not ours to choose: they are the ones already in the
-- hotel's external_flash_texts.json, under quiz.<code>.question.<n> and
-- quiz.<code>.answer.<n>.<i>. Renumbering them to start at 1, or reordering them, would leave the
-- client asking its localization for keys that do not exist and drawing blank questions.
--
-- The answer keys below are Habbo's own: the Habbo Way quiz rewards calling a moderator over
-- retaliating, and the safety quiz rewards refusing to share anything that identifies you.

INSERT IGNORE INTO `quizzes` (`code`, `reward_badge_code`, `enabled`, `created_at`, `updated_at`)
VALUES ('HabboWay1', 'ACH_HabboWay1', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP()),
       ('SafetyQuiz1', '', 1, UTC_TIMESTAMP(), UTC_TIMESTAMP());

INSERT IGNORE INTO `quiz_questions`
    (`quiz_id`, `question_number`, `correct_answer_index`, `sort_order`, `created_at`, `updated_at`)
SELECT `q`.`id`, `k`.`question_number`, `k`.`correct_answer_index`, `k`.`question_number`,
       UTC_TIMESTAMP(), UTC_TIMESTAMP()
  FROM `quizzes` AS `q`
  JOIN (
        SELECT 'HabboWay1' AS `code`, 0 AS `question_number`, 2 AS `correct_answer_index`
  UNION SELECT 'HabboWay1', 1, 1
  UNION SELECT 'HabboWay1', 2, 3
  UNION SELECT 'HabboWay1', 3, 0
  UNION SELECT 'HabboWay1', 4, 1
  UNION SELECT 'HabboWay1', 5, 3
  UNION SELECT 'HabboWay1', 6, 1
  UNION SELECT 'HabboWay1', 7, 3
  UNION SELECT 'HabboWay1', 8, 0
  UNION SELECT 'HabboWay1', 9, 0
  UNION SELECT 'SafetyQuiz1', 0, 1
  UNION SELECT 'SafetyQuiz1', 1, 1
  UNION SELECT 'SafetyQuiz1', 2, 1
  UNION SELECT 'SafetyQuiz1', 3, 1
  UNION SELECT 'SafetyQuiz1', 4, 1
  UNION SELECT 'SafetyQuiz1', 5, 0
  UNION SELECT 'SafetyQuiz1', 6, 1
  UNION SELECT 'SafetyQuiz1', 7, 1
  UNION SELECT 'SafetyQuiz1', 8, 2
  UNION SELECT 'SafetyQuiz1', 9, 2
  UNION SELECT 'SafetyQuiz1', 10, 0
  UNION SELECT 'SafetyQuiz1', 11, 0
  UNION SELECT 'SafetyQuiz1', 12, 0
       ) AS `k` ON `k`.`code` = `q`.`code`;
