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

