-- Vague 2b : rend utilisables les 40 meubles wired dont la colonne logic vaut 'none'.
-- Sans cela ils retombent sur default_floor : ce ne sont pas des box wired, la fenêtre
-- de configuration ne s'ouvre pas. Concerne les 20 sélecteurs et les 7 box de variables.
-- Retour arrière : db-backup/revert-wired-logic.sql

START TRANSACTION;
UPDATE furniture_definitions SET logic='wf_act_give_var' WHERE id=32013615 AND logic='none';
UPDATE furniture_definitions SET logic='wf_act_remove_var' WHERE id=32013622 AND logic='none';
UPDATE furniture_definitions SET logic='wf_act_set_altitude' WHERE id=4490563 AND logic='none';
UPDATE furniture_definitions SET logic='wf_cnd_not_user_performs_action' WHERE id=32013631 AND logic='none';
UPDATE furniture_definitions SET logic='wf_cnd_slc_quantity' WHERE id=32013632 AND logic='none';
UPDATE furniture_definitions SET logic='wf_cnd_team_has_rank' WHERE id=32013633 AND logic='none';
UPDATE furniture_definitions SET logic='wf_cnd_team_has_score' WHERE id=32013634 AND logic='none';
UPDATE furniture_definitions SET logic='wf_cnd_user_performs_action' WHERE id=32013636 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_altitude' WHERE id=252293444 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_area' WHERE id=966610621 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_bytype' WHERE id=446313468 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_neighborhood' WHERE id=857043807 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_onfurni' WHERE id=333117657 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_picks' WHERE id=717724789 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_signal' WHERE id=422001511 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_furni_with_var' WHERE id=674941925 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_remote' WHERE id=2000028788 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_area' WHERE id=735519086 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_byaction' WHERE id=987510308 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_byname' WHERE id=807283715 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_bytype' WHERE id=246616967 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_group' WHERE id=880755949 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_handitem' WHERE id=238800259 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_neighborhood' WHERE id=756632562 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_onfurni' WHERE id=352976345 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_signal' WHERE id=451817134 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_team' WHERE id=751055284 AND logic='none';
UPDATE furniture_definitions SET logic='wf_slc_users_with_var' WHERE id=373654393 AND logic='none';
UPDATE furniture_definitions SET logic='wf_trg_recv_signal' WHERE id=2000029125 AND logic='none';
UPDATE furniture_definitions SET logic='wf_trg_stuff_state' WHERE id=4490565 AND logic='none';
UPDATE furniture_definitions SET logic='wf_trg_user_performs_action' WHERE id=4490566 AND logic='none';
UPDATE furniture_definitions SET logic='wf_trg_var_changed' WHERE id=32013641 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_context' WHERE id=2000028785 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_furni' WHERE id=2000028787 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_quest' WHERE id=28408418 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_quest_chain' WHERE id=28408419 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_reference' WHERE id=2000029123 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_room' WHERE id=2000028790 AND logic='none';
UPDATE furniture_definitions SET logic='wf_var_user' WHERE id=2000028784 AND logic='none';
UPDATE furniture_definitions SET logic='wf_xtra_filter_furni' WHERE id=2000028767 AND logic='none';
COMMIT;

-- Contrôle : doit renvoyer 0
SELECT COUNT(*) AS restant_none FROM furniture_definitions WHERE name LIKE 'wf!_%' ESCAPE '!' AND logic='none';
