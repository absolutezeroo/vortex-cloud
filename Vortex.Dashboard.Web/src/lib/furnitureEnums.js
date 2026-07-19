// Wire-level int values for Vortex.Primitives.Furniture.Enums.ProductType /
// Vortex.Primitives.Rooms.Enums.FurnitureUsageType / Vortex.Primitives.Furniture.StuffData.StuffDataType
// and the FurnitureCategory column on furniture_definitions. Kept as plain int values (not strings)
// because the dashboard API deserializes enums as numbers (System.Text.Json default), matching how
// CreateCatalogProductRequest.ProductType etc. are already sent elsewhere.

export const PRODUCT_TYPES = [
  { value: 0, label: 'Floor' },
  { value: 1, label: 'Wall' },
  { value: 2, label: 'Effect' },
  { value: 3, label: 'Badge' },
  { value: 4, label: 'Robot' },
  { value: 5, label: 'HabboClub' },
  { value: 6, label: 'Pet' },
];

export const FURNITURE_CATEGORIES = [
  { value: 1, label: 'Default' },
  { value: 2, label: 'Wall paper' },
  { value: 3, label: 'Floor' },
  { value: 4, label: 'Landscape' },
  { value: 5, label: 'Post it' },
  { value: 6, label: 'Poster' },
  { value: 7, label: 'Sound set' },
  { value: 8, label: 'Trax song' },
  { value: 9, label: 'Present' },
  { value: 10, label: 'Ecotron box' },
  { value: 11, label: 'Trophy' },
  { value: 12, label: 'Credit furni' },
  { value: 13, label: 'Pet shampoo' },
  { value: 14, label: 'Pet custom part' },
  { value: 15, label: 'Pet custom part shampoo' },
  { value: 16, label: 'Pet saddle' },
  { value: 17, label: 'Guild furni' },
  { value: 18, label: 'Game furni' },
  { value: 19, label: 'Monsterplant seed' },
  { value: 20, label: 'Monsterplant revival' },
  { value: 21, label: 'Monsterplant rebreed' },
  { value: 22, label: 'Monsterplant fertilize' },
  { value: 23, label: 'Figure purchasable set' },
];

export const USAGE_POLICIES = [
  { value: 0, label: 'Nobody' },
  { value: 1, label: 'Controller' },
  { value: 2, label: 'Everybody' },
];

// The real, complete set of registered furniture "logic" keys -- there is no enum for this column
// (it's a free string matched at runtime against every [RoomObjectLogic("...")]-attributed class
// found by assembly scanning, see RoomObjectLogicFeatureProcessor/RoomObjectLogicProvider). Typing
// one from memory means guessing; this list is grepped directly from those attributes so every
// value here is guaranteed to resolve to a real behavior instead of silently falling back to
// default_floor at runtime. Wired trigger/condition/action/selector/extra/variable pieces are
// shown with their raw internal key (not relabeled) since that's what every other catalog tool
// references them by too.
export const LOGIC_GROUPS = [
  {
    label: 'Basic',
    options: [
      { value: 'default_floor', label: 'default_floor - Default (floor item)' },
      { value: 'default_wall', label: 'default_wall - Default (wall item)' },
      { value: 'gate', label: 'gate - Gate (blocks/unblocks a tile)' },
      { value: 'roller', label: 'roller - Roller (conveyor belt)' },
      { value: 'dice', label: 'dice - Dice' },
      { value: 'fireworks', label: 'fireworks - Fireworks' },
      { value: 'room_invisible_click_tile', label: 'room_invisible_click_tile - Invisible click tile' },
      { value: 'wheel_of_fortune', label: 'wheel_of_fortune - Wheel of fortune' },
    ],
  },
  {
    label: 'Pets',
    options: [
      { value: 'monsterplant_seed', label: 'monsterplant_seed - Monsterplant seed' },
      { value: 'pet_drink', label: 'pet_drink - Pet drink bowl' },
      { value: 'pet_nest', label: 'pet_nest - Pet nest' },
      { value: 'pet_food', label: 'pet_food - Pet food bowl' },
    ],
  },
  {
    label: 'Wired: Triggers',
    options: [
      'wf_trg_at_given_time', 'wf_trg_bot_reached_avtr', 'wf_trg_bot_reached_stf', 'wf_trg_click_furni',
      'wf_trg_click_tile', 'wf_trg_clock_counter', 'wf_trg_game_ends', 'wf_trg_game_starts',
      'wf_trg_stuff_state', 'wf_trg_collision', 'wf_trg_enter_room', 'wf_trg_leave_room',
      'wf_trg_user_performs_action', 'wf_trg_says_something', 'wf_trg_state_changed', 'wf_trg_period_long',
      'wf_trg_periodically', 'wf_trg_recv_signal', 'wf_trg_score_achieved', 'wf_trg_period_short',
      'wf_trg_var_changed', 'wf_trg_walks_on_furni', 'wf_trg_walks_off_furni',
    ].map((k) => ({ value: k, label: k })),
  },
  {
    label: 'Wired: Conditions',
    options: [
      'wf_cnd_counter_time_matches', 'wf_cnd_actor_dir', 'wf_cnd_triggerer_match', 'wf_cnd_trggrer_on_frn',
      'wf_cnd_actor_in_group', 'wf_cnd_wearing_effect', 'wf_cnd_has_handitem', 'wf_cnd_habbo_owns_badge',
      'wf_cnd_actor_in_team', 'wf_cnd_user_performs_action', 'wf_cnd_furnis_hv_avtrs', 'wf_cnd_has_furni_on',
      'wf_cnd_has_altitude', 'wf_cnd_match_snapshot', 'wf_cnd_stuff_is', 'wf_cnd_user_count_in',
      'wf_cnd_slc_quantity', 'wf_cnd_team_has_rank', 'wf_cnd_team_has_score', 'wf_cnd_time_less_than',
      'wf_cnd_time_more_than', 'wf_cnd_not_triggerer_match', 'wf_cnd_not_trggrer_on', 'wf_cnd_not_actor_in_group',
      'wf_cnd_not_wearing_fx', 'wf_cnd_not_has_handitem', 'wf_cnd_not_habbo_owns_badge', 'wf_cnd_not_in_team',
      'wf_cnd_not_user_performs_action', 'wf_cnd_not_hv_avtrs', 'wf_cnd_not_furni_on', 'wf_cnd_not_match_snap',
      'wf_cnd_not_stuff_is', 'wf_cnd_not_user_count',
    ].map((k) => ({ value: k, label: k })),
  },
  {
    label: 'Wired: Actions',
    options: ['wf_act_chase', 'wf_act_give_var', 'wf_act_move_rotate', 'wf_act_toggle_state'].map((k) => ({
      value: k,
      label: k,
    })),
  },
  {
    label: 'Wired: Selectors',
    options: [
      'wf_slc_users_byaction', 'wf_slc_users_byname', 'wf_slc_users_bytype', 'wf_slc_users_signal',
      'wf_slc_users_area', 'wf_slc_users_group', 'wf_slc_users_neighborhood', 'wf_slc_users_team',
      'wf_slc_users_onfurni', 'wf_slc_users_handitem', 'wf_slc_users_with_var', 'wf_slc_furni_bytype',
      'wf_slc_furni_signal', 'wf_slc_furni_area', 'wf_slc_furni_neighborhood', 'wf_slc_furni_onfurni',
      'wf_slc_furni_altitude', 'wf_slc_furni_with_var', 'wf_slc_remote', 'wf_slc_furni_picks',
    ].map((k) => ({ value: k, label: k })),
  },
  {
    label: 'Wired: Extras',
    options: [
      'wf_xtra_anim_time', 'wf_xtra_mov_no_animation', 'wf_xtra_mov_carry_users', 'wf_xtra_or_eval',
      'wf_xtra_execution_limit', 'wf_xtra_mov_physics', 'wf_xtra_random', 'wf_xtra_unseen',
      'wf_xtra_filter_users', 'wf_xtra_filter_furni',
    ].map((k) => ({ value: k, label: k })),
  },
  {
    label: 'Wired: Variables',
    options: [
      'wf_var_context', 'wf_var_furni', 'wf_var_quest', 'wf_var_quest_chain',
      'wf_var_reference', 'wf_var_room', 'wf_var_user',
    ].map((k) => ({ value: k, label: k })),
  },
];

export const STUFF_DATA_TYPES = [
  { value: 0, label: 'Legacy key' },
  { value: 1, label: 'Map key' },
  { value: 2, label: 'String key' },
  { value: 3, label: 'Vote key' },
  { value: 4, label: 'Empty key' },
  { value: 5, label: 'Number key' },
  { value: 6, label: 'Highscore key' },
  { value: 7, label: 'Crackable key' },
];
