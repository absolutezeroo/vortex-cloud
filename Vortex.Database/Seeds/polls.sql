-- One working survey, so the poll widget has something to show the day the feature ships.
--
-- It is an NPS poll: question 1 asks the recommendation score, and the follow-up a player gets
-- depends on which score they picked. The branch key is the client's own — after a root answer it
-- looks for a child question whose `question_category` equals the picked choice's `choice_type`,
-- and shows nothing when no child matches. That is why the "passive" choices carry choice_type 2
-- with no category-2 child: a lukewarm answer ends the branch instead of nagging.
--
-- Question types are the client's too: 1 = radio, 2 = checkbox, 3 = one-line text, 4 = text area.
-- Types 5 and 6 exist in the client enum but its content dialog skips them, so a survey must not
-- use them.

INSERT IGNORE INTO `polls`
    (`id`, `code`, `poll_type`, `headline`, `summary`, `start_message`, `end_message`,
     `nps_poll`, `enabled`, `offer_on_room_entry`, `room_id`, `sort_order`)
VALUES
    (1, 'hotel_satisfaction', 'nps',
     'Got a minute?',
     'Tell us how we are doing. Three questions, no wrong answers.',
     'Thanks for helping out! Here we go.',
     'That is everything - thanks for your time!',
     1, 1, 1, NULL, 0);

INSERT IGNORE INTO `poll_questions`
    (`id`, `poll_id`, `parent_question_id`, `sort_order`, `question_type`, `question_text`,
     `question_category`, `question_answer_type`)
VALUES
    (1, 1, NULL, 0, 1, 'How likely are you to recommend the hotel to a friend?', 0, 0),
    (2, 1, 1,    0, 4, 'Great to hear! What do you enjoy the most?',             1, 0),
    (3, 1, 1,    1, 4, 'Sorry about that. What let you down?',                   3, 0),
    (4, 1, NULL, 1, 2, 'Which parts of the hotel do you actually use?',          0, 0),
    (5, 1, NULL, 2, 3, 'Anything else you want to tell us?',                     0, 0);

INSERT IGNORE INTO `poll_question_choices`
    (`id`, `question_id`, `value`, `choice_text`, `choice_type`, `sort_order`)
VALUES
    -- Root question 1: the score drives the branch (1 = promoter, 2 = passive, 3 = detractor).
    (1,  1, '10', 'Definitely - I already have',      1, 0),
    (2,  1, '8',  'Probably',                         1, 1),
    (3,  1, '5',  'Not sure',                         2, 2),
    (4,  1, '2',  'Probably not',                     3, 3),
    (5,  1, '0',  'No chance',                        3, 4),
    -- Root question 4: plain checkboxes, no branching.
    (6,  4, 'rooms',     'Building and visiting rooms', 0, 0),
    (7,  4, 'catalogue', 'The catalogue',               0, 1),
    (8,  4, 'groups',    'Groups and forums',           0, 2),
    (9,  4, 'games',     'Games and wired',             0, 3),
    (10, 4, 'trading',   'Trading with other players',  0, 4);
