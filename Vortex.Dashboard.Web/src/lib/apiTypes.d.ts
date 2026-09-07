// Generated from the C# response contracts by ApiTypeScriptContractTests.
// Do not edit: change the records, run the test, copy what it writes.

export interface ArticleCategoryOption {
  id: number;
  code: string;
  labels: string;
  sortOrder: number;
  enabled: boolean;
}

export interface ArticleDetail {
  id: number;
  slug: string;
  category: string;
  status: string;
  publishAt: string | null;
  pinned: boolean;
  author: string;
  translations: ArticleTranslation[];
}

export interface ArticleFormMeta {
  categories: ArticleCategoryOption[];
  languages: ArticleLanguageOption[];
  imageBase: string | null;
  imageDirectories: string[];
  blockTypes: string[];
}

export interface ArticleImage {
  path: string;
  thumb: string;
}

export interface ArticleImageBrowse {
  total: number;
  page: number;
  pageSize: number;
  count: number;
  items: ArticleImage[];
  error: string | null;
}

export interface ArticleLanguageOption {
  id: number;
  code: string;
  label: string;
  isDefault: boolean;
  enabled: boolean;
  sortOrder: number;
}

export interface ArticleListItem {
  id: number;
  slug: string;
  category: string;
  status: string;
  scheduled: boolean;
  publishAt: string | null;
  pinned: boolean;
  author: string;
  title: string;
  languages: string[];
}

export interface ArticleListResponse {
  total: number;
  page: number;
  pageSize: number;
  count: number;
  items: ArticleListItem[];
}

export interface ArticleTranslation {
  lang: string;
  title: string;
  summary: string;
  body: string;
  headerImage: string | null;
  thumbnail: string | null;
}

export interface AuditEntry {
  id: number;
  occurredAt: string;
  category: string;
  action: string;
  severity: string;
  result: string;
  actorPlayerId: number | null;
  actorName: string | null;
  targetPlayerId: number | null;
  targetName: string | null;
  roomId: number | null;
  itemId: number | null;
  ipHash: string | null;
  correlationId: string | null;
  data: string | null;
}

export interface AuditPage {
  count: number;
  page: number;
  limit: number;
  total: number;
  offset: number;
  items: AuditEntry[];
}

export interface BotDetail {
  id: number;
  name: string;
  motto: string;
  figure: string;
  avatarUrl: string | null;
  gender: string;
  ownerId: number;
  ownerName: string | null;
  roomId: number | null;
  roomName: string | null;
  placed: boolean;
  x: number;
  y: number;
  z: number;
  rotation: number;
  skills: number[];
  skillNames: string[];
  phrases: string[];
  autoChat: boolean;
  chatDelaySeconds: number;
  mixSentences: boolean;
  wanders: boolean;
  dances: boolean;
  rawSkillsJson: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface BotGenderCount {
  gender: string;
  count: number;
}

export interface BotGrowthPoint {
  bucket: string;
  label: string;
  botsCreated: number;
}

export interface BotListItem {
  id: number;
  name: string;
  motto: string;
  figure: string;
  avatarUrl: string | null;
  gender: string;
  ownerId: number;
  ownerName: string | null;
  roomId: number | null;
  roomName: string | null;
  placed: boolean;
  x: number;
  y: number;
  z: number;
  rotation: number;
  skills: number[];
  skillNames: string[];
  phraseCount: number;
  autoChat: boolean;
  chatDelaySeconds: number;
  wanders: boolean;
  dances: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface BotListResponse {
  page: number;
  limit: number;
  offset: number;
  total: number;
  count: number;
  items: BotListItem[];
}

export interface BotOwnerCount {
  ownerId: number;
  ownerName: string | null;
  botCount: number;
}

export interface BotRoomCount {
  roomId: number;
  roomName: string | null;
  botCount: number;
}

export interface BotStats {
  window: ReportWindow;
  totals: BotTotals;
  byGender: BotGenderCount[];
  growth: BotGrowthPoint[];
  topOwners: BotOwnerCount[];
  topRooms: BotRoomCount[];
}

export interface BotTotals {
  totalBots: number;
  placedBots: number;
  inventoryBots: number;
  configuredBots: number;
  chattyBots: number;
  autoChatBots: number;
  wanderingBots: number;
  dancingBots: number;
  distinctOwners: number;
  roomsWithBots: number;
}

export interface CfhCloseReasonCount {
  reason: string;
  count: number;
}

export interface CfhReportedPlayer {
  playerId: number;
  playerName: string | null;
  reportCount: number;
}

export interface CfhStats {
  window: ReportWindow;
  totals: CfhTotals;
  timeline: CfhTimelinePoint[];
  byCloseReason: CfhCloseReasonCount[];
  topTopics: CfhTopicCount[];
  topReportedPlayers: CfhReportedPlayer[];
}

export interface CfhTimelinePoint {
  bucket: string;
  label: string;
  ticketsCreated: number;
}

export interface CfhTopicCount {
  topicId: number;
  topicName: string;
  count: number;
}

export interface CfhTotals {
  totalTickets: number;
  openCount: number;
  pickedCount: number;
  closedCount: number;
  sanctionedCount: number;
  sanctionRate: number;
  avgResolutionMinutes: number;
}

export interface ChatlogEntry {
  id: number;
  createdAt: string;
  roomId: number;
  roomName: string | null;
  playerId: number;
  playerName: string | null;
  targetPlayerId: number | null;
  targetPlayerName: string | null;
  message: string;
}

export interface ChatlogFilters {
  q: string | null;
  player: number | null;
  room: number | null;
}

export interface ChatlogPage {
  count: number;
  page: number;
  limit: number;
  total: number;
  offset: number;
  window: ChatlogWindow;
  filters: ChatlogFilters;
  items: ChatlogEntry[];
}

export interface ChatlogWindow {
  since: string;
  until: string;
}

export interface ForumGroupRanking {
  groupId: number;
  groupName: string | null;
  badgeUrl: string | null;
  threads: number;
  postCount: number;
  lastPostAt: string | null;
}

export interface ForumStateCount {
  state: string;
  count: number;
}

export interface ForumThreadSummary {
  id: number;
  groupId: number;
  groupName: string | null;
  badgeUrl: string | null;
  subject: string;
  state: string;
  isPinned: boolean;
  postCount: number;
  lastPostAt: string | null;
  createdAt: string;
  authorId: number;
  authorName: string | null;
}

export interface GroupActivityEvent {
  occurredAt: string;
  action: string;
  actorPlayerId: number | null;
  actorPlayerName: string | null;
  result: string;
  data: string | null;
}

export interface GroupForumRanking {
  groupId: number;
  name: string;
  threadCount: number;
  postCount: number;
}

export interface GroupGrowthPoint {
  bucket: string;
  label: string;
  groupsCreated: number;
}

export interface GroupMemberRanking {
  groupId: number;
  name: string;
  badge: string;
  badgeUrl: string | null;
  ownerId: number;
  ownerName: string;
  memberCount: number;
  roomId: number;
}

export interface GroupStats {
  window: ReportWindow;
  totals: GroupTotals;
  growth: GroupGrowthPoint[];
  topGroupsByMembers: GroupMemberRanking[];
  topGroupsByForumActivity: GroupForumRanking[];
  recentActivity: GroupActivityEvent[];
}

export interface GroupTotals {
  totalGroups: number;
  totalMembers: number;
  totalThreads: number;
  totalPosts: number;
  avgMembersPerGroup: number;
}

export interface HandItemList {
  count: number;
  consumableCount: number;
  imageTemplate: string | null;
  items: HandItemRow[];
}

export interface HandItemRow {
  id: number;
  handItemId: number;
  name: string;
  nutrition: number;
  thirst: number;
  consumable: boolean;
  imageUrl: string | null;
}

export interface ModerationActionCount {
  action: string;
  count: number;
}

export interface ModerationActorCount {
  actorPlayerId: number;
  actorName: string | null;
  count: number;
}

export interface ModerationDistribution {
  byAction: ModerationActionCount[];
  byResult: ModerationResultCount[];
}

export interface ModerationResultCount {
  result: string;
  count: number;
}

export interface ModerationRoomCount {
  roomId: number;
  roomName: string | null;
  count: number;
}

export interface ModerationRow {
  id: number;
  occurredAt: string;
  action: string;
  result: string;
  actorPlayerId: number | null;
  actorName: string | null;
  targetPlayerId: number | null;
  targetName: string | null;
  roomId: number | null;
  roomName: string | null;
  durationSeconds: number | null;
  duration: string | null;
  reason: string | null;
  isRenewal: boolean;
  correlationId: string | null;
}

export interface ModerationStats {
  window: ModerationWindow;
  totals: ModerationTotals;
  distribution: ModerationDistribution;
  timeline: ModerationTimelinePoint[];
  topActors: ModerationActorCount[];
  topTargets: ModerationTargetCount[];
  topRooms: ModerationRoomCount[];
  rows: ModerationRow[];
}

export interface ModerationTargetCount {
  targetPlayerId: number;
  targetName: string | null;
  count: number;
}

export interface ModerationTimelinePoint {
  bucket: string;
  label: string;
  count: number;
}

export interface ModerationTotals {
  total: number;
  limit: number;
  page: number;
  offset: number;
  success: number;
  denied: number;
  failed: number;
  retentionRate: number;
  activeBans: number;
  inactiveBans: number;
  totalBans: number;
  renewalCount: number;
  averageDurationSeconds: number;
}

export interface ModerationWindow {
  since: string;
  until: string;
}

export interface PetGrowthPoint {
  bucket: string;
  label: string;
  petsCreated: number;
}

export interface PetOwnerCount {
  ownerId: number;
  ownerName: string | null;
  petCount: number;
}

export interface PetRaceCount {
  type: number;
  race: number;
  count: number;
}

export interface PetRarityCount {
  rarityLevel: number;
  count: number;
}

export interface PetStats {
  window: ReportWindow;
  totals: PetTotals;
  byType: PetTypeCount[];
  byRace: PetRaceCount[];
  byRarity: PetRarityCount[];
  growth: PetGrowthPoint[];
  topOwners: PetOwnerCount[];
}

export interface PetTotals {
  totalPets: number;
  avgLevel: number;
  avgEnergy: number;
  avgNutrition: number;
  breedablePets: number;
  bredPets: number;
}

export interface PetTypeCount {
  type: number;
  count: number;
}

export interface PollChoiceDetail {
  id: number;
  value: string;
  choiceText: string;
  choiceType: number;
  sortOrder: number;
}

export interface PollDetail {
  id: number;
  code: string;
  pollType: string;
  headline: string;
  summary: string;
  startMessage: string;
  endMessage: string;
  npsPoll: boolean;
  enabled: boolean;
  offerOnRoomEntry: boolean;
  roomId: number | null;
  roomName: string | null;
  sortOrder: number;
  questions: PollQuestionNode[];
}

export interface PollFreeTextAnswer {
  playerId: number;
  playerName: string | null;
  avatarUrl: string | null;
  answer: string;
  answeredAt: string;
}

export interface PollFunnel {
  offered: number;
  pending: number;
  started: number;
  completed: number;
  rejected: number;
  completionRate: number;
  rejectionRate: number;
}

export interface PollListItem {
  id: number;
  code: string;
  pollType: string;
  headline: string;
  summary: string;
  startMessage: string;
  endMessage: string;
  npsPoll: boolean;
  enabled: boolean;
  offerOnRoomEntry: boolean;
  roomId: number | null;
  roomName: string | null;
  roomMissing: boolean;
  sortOrder: number;
  rootQuestionCount: number;
  followUpCount: number;
  offeredCount: number;
  startedCount: number;
  completedCount: number;
  rejectedCount: number;
  completionRate: number;
  offerable: boolean;
}

export interface PollListResponse {
  count: number;
  items: PollListItem[];
}

export interface PollQuestionNode {
  id: number;
  sortOrder: number;
  questionType: number;
  questionTypeName: string;
  questionText: string;
  questionCategory: number;
  questionAnswerType: number;
  answerCount: number;
  choices: PollChoiceDetail[];
  children: PollQuestionNode[];
}

export interface PollQuestionResult {
  id: number;
  questionText: string;
  questionType: number;
  questionTypeName: string;
  isFollowUp: boolean;
  parentQuestionId: number | null;
  questionCategory: number;
  respondents: number;
  answerCount: number;
  tally: PollTallyEntry[];
  freeText: PollFreeTextAnswer[];
  freeTextTruncated: boolean;
}

export interface PollQuestionTypeOption {
  id: number;
  name: string;
  supported: boolean;
  takesChoices: boolean;
}

export interface PollQuestionTypeOptions {
  count: number;
  items: PollQuestionTypeOption[];
}

export interface PollResults {
  id: number;
  code: string;
  headline: string;
  npsPoll: boolean;
  funnel: PollFunnel;
  questions: PollQuestionResult[];
}

export interface PollTallyEntry {
  value: string;
  text: string;
  choiceType: number;
  count: number;
  share: number;
  retired: boolean;
}

export interface ReportWindow {
  since: string;
  until: string;
  granularity: string;
}

export interface SocialForums {
  threadsByState: ForumStateCount[];
  postsByState: ForumStateCount[];
  topGroups: ForumGroupRanking[];
  recentThreads: ForumThreadSummary[];
}

export interface SocialFriendedCount {
  playerId: number;
  playerName: string | null;
  friends: number;
}

export interface SocialSenderCount {
  playerId: number;
  playerName: string | null;
  messages: number;
}

export interface SocialStats {
  window: ReportWindow;
  totals: SocialTotals;
  timeline: SocialTimelinePoint[];
  topSenders: SocialSenderCount[];
  topFriended: SocialFriendedCount[];
  forums: SocialForums;
}

export interface SocialTimelinePoint {
  bucket: string;
  label: string;
  messages: number;
}

export interface SocialTotals {
  friendships: number;
  friendRows: number;
  playersWithFriends: number;
  pendingRequests: number;
  blockedPairs: number;
  ignoredPairs: number;
  totalMessages: number;
  undelivered: number;
  windowMessages: number;
  threads: number;
  posts: number;
}

export interface SongListItem {
  id: number;
  name: string;
  creator: string;
  lengthMs: number;
  lengthSeconds: number;
  officialSongId: string;
  data: string;
  diskCount: number;
  loadedInJukeboxes: number;
}

export interface SongListResponse {
  total: number;
  page: number;
  pageSize: number;
  items: SongListItem[];
}

export interface WiredCategoryCount {
  category: string;
  count: number;
}

export interface WiredLogicCount {
  logic: string;
  count: number;
  furniIconUrl: string | null;
}

export interface WiredRoomCount {
  roomId: number;
  roomName: string;
  wiredCount: number;
}

export interface WiredStats {
  totals: WiredTotals;
  byCategory: WiredCategoryCount[];
  byLogic: WiredLogicCount[];
  topRooms: WiredRoomCount[];
}

export interface WiredTotals {
  totalWiredPlaced: number;
  roomsWithWired: number;
}

