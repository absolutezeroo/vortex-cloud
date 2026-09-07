// Generated from the C# response contracts by ApiTypeScriptContractTests.
// Do not edit: change the records, run the test, copy what it writes.

export interface AchievementCategoryStats {
  category: string;
  achievements: number;
  levels: number;
  badgesAwarded: number;
}

export interface AchievementDetail {
  id: number;
  name: string;
  category: string;
  displayMethod: number;
  triggered: boolean;
  levelCount: number;
  completedPlayers: number;
  ladder: AchievementLadderRung[];
  levelDistribution: AchievementLevelCount[];
  topPlayers: AchievementTopPlayer[];
}

export interface AchievementLadderRung {
  id: number;
  level: number;
  badgeCode: string;
  badgeUrl: string | null;
  progressRequirement: number;
  rewardAmount: number;
  rewardType: number;
  rewardKind: string;
  scorePoints: number;
}

export interface AchievementLevel {
  level: number;
  badgeCode: string;
  badgeUrl: string | null;
  progressRequirement: number;
  rewardAmount: number;
  rewardType: number;
  rewardKind: string;
  scorePoints: number;
}

export interface AchievementLevelCount {
  level: number;
  players: number;
}

export interface AchievementListItem {
  id: number;
  name: string;
  category: string;
  displayMethod: number;
  triggered: boolean;
  levelCount: number;
  totalScore: number;
  creditsPayout: number;
  pointsPayout: number;
  finalRequirement: number;
  playersTracked: number;
  playersStarted: number;
  playersCompleted: number;
  badgesAwarded: number;
  highestLevelReached: number;
  badgeUrl: string | null;
  levels: AchievementLevel[];
}

export interface AchievementListResponse {
  count: number;
  categories: string[];
  items: AchievementListItem[];
}

export interface AchievementResolutions {
  offers: ResolutionOffer[];
  challenges: ResolutionChallenge[];
  totals: ResolutionTotals;
  truncated: boolean;
}

export interface AchievementScorePlayer {
  playerId: number;
  playerName: string | null;
  score: number;
  badges: number;
}

export interface AchievementStats {
  totals: AchievementStatsTotals;
  byCategory: AchievementCategoryStats[];
  untouched: UntouchedAchievement[];
  topPlayers: AchievementScorePlayer[];
  badgeImageTemplate: string | null;
}

export interface AchievementStatsTotals {
  totalAchievements: number;
  totalLevels: number;
  triggeredCount: number;
  untriggeredCount: number;
  badgesAwarded: number;
  playersWithProgress: number;
  maxScoreAvailable: number;
}

export interface AchievementTopPlayer {
  playerId: number;
  playerName: string | null;
  level: number;
  progress: number;
  updatedAt: string;
}

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

export interface AvatarBatch {
  items: AvatarBatchRow[];
}

export interface AvatarBatchRow {
  id: number;
  avatarUrl: string | null;
}

export interface BadgeCollector {
  playerId: number;
  playerName: string | null;
  badges: number;
}

export interface BadgeHolderCount {
  badgeCode: string;
  badgeUrl: string | null;
  holders: number;
  equipped: number;
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

export interface BuildersClubTierRow {
  id: number;
  level: number;
  furniLimit: number;
}

export interface CapabilityGroup {
  area: string;
  capabilities: string[];
}

export interface CatalogOfferSales {
  offerId: number;
  offerName: string;
  furniIconUrl: string | null;
  catalogType: string;
  purchaseCount: number;
  quantity: number;
  creditsSpent: number;
}

export interface CatalogPurchasePoint {
  bucket: string;
  label: string;
  purchaseCount: number;
  creditsSpent: number;
}

export interface CatalogPurchaseStats {
  window: ReportWindow;
  totals: CatalogPurchaseTotals;
  timeline: CatalogPurchasePoint[];
  topOffers: CatalogOfferSales[];
}

export interface CatalogPurchaseTotals {
  purchaseCount: number;
  totalCreditsSpent: number;
  totalQuantity: number;
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

export interface ChatStyleRow {
  id: number;
  clientStyleId: number;
  owners: number;
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

export interface ClubExpiringSubscription {
  playerId: number;
  playerName: string | null;
  type: string;
  level: number;
  totalMonths: number;
  expiresAt: string;
  remainingDays: number;
}

export interface ClubLifecyclePoint {
  bucket: string;
  label: string;
  purchases: number;
  renewals: number;
  expired: number;
}

export interface ClubLifecycleTotals {
  purchases: number;
  renewals: number;
  expired: number;
  renewalShare: number;
}

export interface ClubMonthsBreakdown {
  months: number;
  total: number;
  purchases: number;
  renewals: number;
  expired: number;
}

export interface ClubSubscriptionEventRow {
  occurredAt: string;
  action: string;
  actorPlayerId: number | null;
  actorPlayerName: string | null;
  months: number | null;
  totalMonths: number | null;
  creditCost: number | null;
  isRenewal: boolean | null;
  isVip: boolean | null;
}

export interface ClubSubscriptionLifecycle {
  totals: ClubLifecycleTotals;
  byMonths: ClubMonthsBreakdown[];
  recentEvents: ClubSubscriptionEventRow[];
  timeline: ClubLifecyclePoint[];
}

export interface ClubSubscriptionTotals {
  totalSubscriptions: number;
  activeSubscriptions: number;
  inactiveSubscriptions: number;
  expiringIn7Days: number;
  expiringIn30Days: number;
  activeRate: number;
}

export interface ClubSubscriptionTypeBreakdown {
  type: string;
  total: number;
  active: number;
  inactive: number;
  averageRemainingDays: number;
  averageTotalMonths: number;
}

export interface ClubSubscriptions {
  window: ModerationWindow;
  totals: ClubSubscriptionTotals;
  byType: ClubSubscriptionTypeBreakdown[];
  topExpiring: ClubExpiringSubscription[];
  lifecycle: ClubSubscriptionLifecycle;
}

export interface CodeDirectoryPage {
  count: number;
  total: number;
  offset: number;
  hasMore: boolean;
  items: CodeDirectoryRow[];
}

export interface CodeDirectoryRow {
  id: string;
  value: string;
  name: string;
  description: string | null;
}

export interface CollectiblesOverview {
  totals: CollectiblesTotals;
  collections: CollectionRow[];
  nftAvatars: NftAvatarRow[];
  storeOffers: NftStoreOfferRow[];
  mintableTypes: MintableTypeRow[];
  tokenOffers: MintTokenOfferRow[];
  assets: NftAssetRow[];
  claims: NftClaimRow[];
  topCollectors: CollectorScore[];
}

export interface CollectiblesTotals {
  collections: number;
  items: number;
  unresolvedItems: number;
  completableCollections: number;
  trackedPlayers: number;
  storeOffers: number;
  storeOffersOnSale: number;
  mintableTypes: number;
  mintableTypesOpen: number;
  mintedRelics: number;
  stampsHeld: number;
  nftAvatars: number;
  nftAvatarsGranted: number;
}

export interface CollectionItemRow {
  id: number;
  productCode: string;
  itemTypeId: string;
  productTypeId: number;
  score: number;
  rarity: string;
  sortOrder: number;
  resolved: boolean;
  iconUrl: string | null;
}

export interface CollectionRow {
  id: number;
  collectionCode: string;
  name: string;
  boostScore: number;
  releasedAt: string | null;
  snapshotAt: string | null;
  status: number;
  rewardProductCode: string | null;
  bonusProductCode: string | null;
  itemCount: number;
  totalScore: number;
  unresolvedItems: number;
  completable: boolean;
  items: CollectionItemRow[];
}

export interface CollectorScore {
  playerId: number;
  playerName: string | null;
  score: number;
}

export interface CurrencyTypeRow {
  id: number;
  name: string | null;
  currencyType: string;
  activityPointType: number | null;
  enabled: boolean;
  startingAmount: number;
  walletRows: number;
  totalHeld: number;
}

export interface DirectoryPage {
  count: number;
  total: number;
  offset: number;
  hasMore: boolean;
  items: DirectoryRow[];
}

export interface DirectoryRow {
  id: number;
  value: string;
  name: string;
  description: string | null;
}

export interface EconomyCurrencyTotals {
  spend: number;
  earned: number;
  net: number;
  transactionCount: number;
}

export interface EconomyExtras {
  totals: EconomyExtrasTotals;
  ltdSeries: LtdSeriesRow[];
  rentableSpaces: RentableSpaceRow[];
  rentableTerms: RentableSpaceTermRow[];
  currencies: CurrencyTypeRow[];
  buildersClub: BuildersClubTierRow[];
}

export interface EconomyExtrasTotals {
  ltdSeries: number;
  runningSeries: number;
  rentableSpaces: number;
  rentedNow: number;
  rentableTerms: number;
  currencies: number;
  buildersClubTiers: number;
}

export interface EconomyLedgerEntry {
  id: number;
  occurredAt: string;
  playerId: number;
  playerName: string | null;
  currency: string;
  activityPointType: number | null;
  delta: number;
  balanceAfter: number;
  reason: string;
  refId: number | null;
  correlationId: string | null;
}

export interface EconomyLedgerPage {
  count: number;
  page: number;
  limit: number;
  total: number;
  offset: number;
  items: EconomyLedgerEntry[];
}

export interface EconomySpendCategory {
  currency: string;
  action: string;
  spend: number;
  transactionCount: number;
}

export interface EconomyTrendPoint {
  bucket: string;
  label: string;
  spend: number;
  earned: number;
  net: number;
  transactionCount: number;
}

export interface EconomyTrendSeries {
  currency: string;
  points: EconomyTrendPoint[];
}

export interface EconomyTrends {
  window: ReportWindow;
  currencies: string[];
  series: EconomyTrendSeries[];
  totals: Record<string, EconomyCurrencyTotals>;
  categories: EconomySpendCategory[];
}

export interface EffectOwnerCount {
  effectId: number;
  imageUrl: string | null;
  owners: number;
  activated: number;
  selected: number;
}

export interface FactOption {
  key: string;
  kind: string;
  labelKey: string;
  fallbackLabel: string;
  operators: number[];
  values: FactOptionValue[];
}

export interface FactOptionValue {
  value: string;
  labelKey: string;
  fallbackLabel: string;
}

export interface FishingActivity {
  records: FishingRecordRow[];
  derbies: FishingDerbyRow[];
  anglers: number;
}

export interface FishingContent {
  zones: FishingZoneRow[];
  species: FishingSpeciesRow[];
  rodTiers: FishingRodTierRow[];
  levels: FishingLevelRow[];
}

export interface FishingDerbyRow {
  id: number;
  nameKey: string;
  startsAt: string;
  endsAt: string;
  entries: number;
}

export interface FishingLevelRow {
  id: number;
  level: number;
  xpThreshold: number;
}

export interface FishingRecordRow {
  id: number;
  playerId: number;
  playerName: string | null;
  speciesId: number;
  speciesNameKey: string | null;
  bestWeight: number;
  caughtCount: number;
  bestAt: string;
}

export interface FishingRodTierRow {
  id: number;
  quality: number;
  xpThreshold: number;
  nameKey: string;
  handItemId: number;
  catchMultiplier: number;
  goldenMultiplier: number;
  hookHavocChance: number;
}

export interface FishingSpeciesRow {
  id: number;
  zoneId: number;
  nameKey: string;
  requiredLevel: number;
  rarityStars: number;
  catchRate: number;
  catchRatePercent: number;
  rarityWeight: number;
  drawSharePercent: number;
  minWeight: number;
  maxWeight: number;
  xpReward: number;
  goldenXpBonus: number;
  currencyReward: number;
  activeHours: number;
  activeWeekdays: number;
  activeSeasons: number;
  allHours: boolean;
  allWeekdays: boolean;
}

export interface FishingZoneRow {
  id: number;
  nameKey: string;
  furniClass: string;
  furniIconUrl: string | null;
  requiredLevel: number;
  minCatches: number;
  maxCatches: number;
  speciesCount: number;
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

export interface FurnitureDirectoryPage {
  count: number;
  total: number;
  offset: number;
  hasMore: boolean;
  items: FurnitureDirectoryRow[];
}

export interface FurnitureDirectoryRow {
  id: number;
  spriteId: number;
  name: string;
  logic: string;
  type: string;
  category: string;
  width: number;
  length: number;
  canTrade: boolean;
  canSell: boolean;
  iconUrl: string | null;
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

export interface HabbiconCollectionList {
  count: number;
  items: HabbiconCollectionRow[];
  artwork: HabbiconSheets | null;
}

export interface HabbiconCollectionRow {
  id: number;
  code: string;
  localizationKey: string;
  sortOrder: number;
  enabled: boolean;
  hidden: boolean;
  availableFrom: string | null;
  availableUntil: string | null;
  priceCredits: number;
  priceActivityPoints: number;
  activityPointType: number;
  campaignCode: string;
  entryCount: number;
  rewardHabbiconId: number;
  rewardCode: string;
  completedBy: number;
  sprite: HabbiconSprite | null;
  habbicons: HabbiconRow[];
}

export interface HabbiconRow {
  sprite: HabbiconSprite | null;
  id: number;
  code: string;
  localizationKey: string;
  collectionId: number;
  sortOrder: number;
  isCollectionReward: boolean;
  priceCredits: number;
  priceActivityPoints: number;
  activityPointType: number;
  enabled: boolean;
  availableFrom: string | null;
  availableUntil: string | null;
  owners: number;
}

export interface HabbiconSheets {
  spritesheetUrl: string;
  collectionSpritesheetUrl: string | null;
  frameSize: number;
  collectionIconSize: number;
}

export interface HabbiconSourceOption {
  name: string;
  value: number;
}

export interface HabbiconSourceOptions {
  count: number;
  items: HabbiconSourceOption[];
}

export interface HabbiconSprite {
  x: number;
  y: number;
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

export interface LtdRaffleResultCount {
  result: string;
  count: number;
}

export interface LtdSeriesRow {
  id: number;
  productId: number;
  productName: string | null;
  iconUrl: string | null;
  totalQuantity: number;
  remainingQuantity: number;
  sold: number;
  costCredits: number;
  raffleWindowSeconds: number;
  isActive: boolean;
  hasRaffleFinished: boolean;
  startsAt: string | null;
  endsAt: string | null;
  running: boolean;
  pendingEntries: number;
  entriesByResult: LtdRaffleResultCount[];
}

export interface MarketplaceSalePoint {
  bucket: string;
  label: string;
  sales: number;
  volume: number;
}

export interface MarketplaceSeller {
  sellerId: number;
  sellerName: string | null;
  sales: number;
  volume: number;
}

export interface MarketplaceSummary {
  window: ReportWindow;
  totals: MarketplaceTotals;
  timeline: MarketplaceSalePoint[];
  topSellers: MarketplaceSeller[];
}

export interface MarketplaceTotals {
  activeListings: number;
  soldCount: number;
  totalVolume: number;
  averagePrice: number;
}

export interface MintTokenOfferRow {
  id: number;
  productCode: string;
  silverPrice: number;
  amountTokens: number;
  enabled: boolean;
  sortOrder: number;
}

export interface MintableTypeRow {
  id: number;
  productCode: string;
  stampPrice: number;
  startsAt: string;
  endsAt: string;
  regionLocked: boolean;
  limitedEdition: boolean;
  editionSize: number;
  enabled: boolean;
  sortOrder: number;
  mintedCount: number;
  exhausted: boolean;
  resolved: boolean;
  open: boolean;
  expired: boolean;
  isNft: boolean;
  iconUrl: string | null;
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

export interface NftAssetRow {
  id: number;
  playerId: number;
  playerName: string | null;
  productCode: string;
  stampCost: number;
  serialNumber: number;
  editionSize: number;
  mintedAt: string;
  iconUrl: string | null;
  history: NftAssetTransfer[];
}

export interface NftAssetTransfer {
  id: number;
  fromPlayer: string | null;
  toPlayer: string | null;
  reason: string;
  at: string;
}

export interface NftAvatarHolder {
  id: number;
  playerId: number;
  playerName: string | null;
  serialNumber: number;
  grantNote: string | null;
  grantedAt: string;
  worn: boolean;
}

export interface NftAvatarRow {
  id: number;
  avatarCode: string;
  name: string;
  figure: string;
  gender: string;
  contractKey: string;
  editionSize: number;
  enabled: boolean;
  sortOrder: number;
  grantedCount: number;
  exhausted: boolean;
  knownCollection: boolean;
  avatarImageUrl: string | null;
  holders: NftAvatarHolder[];
}

export interface NftClaimRow {
  id: number;
  playerId: number;
  playerName: string | null;
  productCode: string;
  setId: string;
  collection: string;
  claimLimit: number;
  claimedAmount: number;
  remaining: number;
  validFrom: string | null;
  validTo: string | null;
  isNft: boolean;
  iconUrl: string | null;
}

export interface NftStoreOfferRow {
  id: number;
  productCode: string;
  emeraldPrice: number;
  isFeatured: boolean;
  isLimited: boolean;
  mintLimit: number;
  soldCount: number;
  itemTypeId: string;
  productTypeId: number;
  score: number;
  rarity: string;
  enabled: boolean;
  sortOrder: number;
  resolved: boolean;
  soldOut: boolean;
  isNft: boolean;
  iconUrl: string | null;
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

export interface PlayerBadgeRow {
  id: number;
  badgeCode: string;
  badgeUrl: string | null;
  slotId: number | null;
  createdAt: string;
}

export interface PlayerChatStyleRow {
  id: number;
  chatStyleId: number;
}

export interface PlayerDirectoryPage {
  count: number;
  total: number;
  offset: number;
  hasMore: boolean;
  online: number;
  items: PlayerDirectoryRow[];
}

export interface PlayerDirectoryRow {
  id: number;
  name: string;
  avatarUrl: string | null;
  online: boolean;
}

export interface PlayerEffectRow {
  id: number;
  effectId: number;
  imageUrl: string | null;
  subType: number;
  totalDuration: number;
  activatedAt: string | null;
  isSelected: boolean;
}

export interface PlayerHabbiconRow {
  habbiconId: number;
  code: string;
  collectionId: number;
  state: string;
  source: string;
  acquiredAt: string;
  lastUsedAt: string | null;
}

export interface PlayerHabbicons {
  playerId: number;
  count: number;
  items: PlayerHabbiconRow[];
}

export interface PlayerOutfitRow {
  id: number;
  slotId: number;
  figure: string;
  gender: string;
}

export interface PlayerRewardDetail {
  playerId: number;
  playerName: string;
  avatarUrl: string | null;
  badges: PlayerBadgeRow[];
  effects: PlayerEffectRow[];
  chatStyles: PlayerChatStyleRow[];
  outfits: PlayerOutfitRow[];
}

export interface PlayerRewardStats {
  totals: PlayerRewardTotals;
  effectImageTemplate: string | null;
  badgeImageTemplate: string | null;
  topBadges: BadgeHolderCount[];
  topEffects: EffectOwnerCount[];
  chatStyles: ChatStyleRow[];
  topCollectors: BadgeCollector[];
}

export interface PlayerRewardTotals {
  totalBadges: number;
  equippedBadges: number;
  playersWithBadges: number;
  distinctBadgeCodes: number;
  totalEffects: number;
  activatedEffects: number;
  selectedEffects: number;
  chatStyleCount: number;
  wardrobeOutfits: number;
  wardrobeUsers: number;
}

export interface PlayerRewardTrackClaimRow {
  prizeId: string;
  claimedAt: string;
  pointsAtClaim: number;
  granted: string;
}

export interface PlayerRewardTrackRow {
  trackId: string;
  points: number;
  premiumUnlocked: boolean;
  premiumUnlockedAt: string | null;
  completedAt: string | null;
  contentVersion: number;
  tasks: PlayerRewardTrackTaskRow[];
  claims: PlayerRewardTrackClaimRow[];
}

export interface PlayerRewardTrackTaskRow {
  taskId: string;
  progressCount: number;
  highestPaidLevelIndex: number;
}

export interface PlayerRewardTracks {
  playerId: number;
  count: number;
  items: PlayerRewardTrackRow[];
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

export interface PrizeEntryDraws {
  entryId: number;
  draws: number;
}

export interface PrizePoolBindingList {
  count: number;
  items: PrizePoolBindingRow[];
}

export interface PrizePoolBindingRow {
  id: number;
  furnitureDefinitionId: number;
  pool: string;
  hitsRequired: number;
  enabled: boolean;
  furnitureName: string | null;
  furnitureLogic: string | null;
  furnitureIconUrl: string | null;
}

export interface PrizePoolContent {
  pools: PrizePoolList;
  entries: PrizePoolEntryList;
  totals: PrizePoolWeightTotal[];
  bindings: PrizePoolBindingList;
  productTypes: string[];
}

export interface PrizePoolDraws {
  pool: string;
  draws: number;
  entries: PrizeEntryDraws[];
  sources: PrizeSourceDraws[];
}

export interface PrizePoolEntryList {
  count: number;
  items: PrizePoolEntryRow[];
}

export interface PrizePoolEntryRow {
  id: number;
  poolId: number;
  pool: string;
  variant: string | null;
  productType: string;
  furnitureDefinitionId: number | null;
  extraParam: string | null;
  weight: number;
  enabled: boolean;
  furnitureName: string | null;
  furnitureIconUrl: string | null;
}

export interface PrizePoolList {
  count: number;
  items: PrizePoolRow[];
}

export interface PrizePoolRow {
  id: number;
  code: string;
  name: string;
  variants: string | null;
  notes: string | null;
  enabled: boolean;
  isBuiltIn: boolean;
}

export interface PrizePoolStats {
  days: number;
  totalDraws: number;
  pools: PrizePoolDraws[];
}

export interface PrizePoolWeightTotal {
  poolId: number;
  variant: string | null;
  totalWeight: number;
  entries: number;
}

export interface PrizeSourceDraws {
  source: string;
  draws: number;
}

export interface RentableSpaceAuditEntry {
  id: number;
  occurredAt: string;
  action: string;
  actorPlayerId: number | null;
  actorName: string | null;
  targetPlayerId: number | null;
  targetName: string | null;
  roomId: number | null;
  itemId: number | null;
  data: string | null;
  correlationId: string | null;
}

export interface RentableSpaceAuditPage {
  activeRentals: number;
  count: number;
  page: number;
  limit: number;
  total: number;
  offset: number;
  items: RentableSpaceAuditEntry[];
}

export interface RentableSpaceRow {
  id: number;
  furnitureId: number;
  furnitureName: string | null;
  iconUrl: string | null;
  renterId: number | null;
  renterName: string | null;
  rentedUntil: string | null;
  rented: boolean;
  hasTerms: boolean;
}

export interface RentableSpaceTermRow {
  id: number;
  furnitureEntityId: number;
  price: number;
  currencyTypeEntityId: number;
  rentDurationSeconds: number;
  requiresHc: boolean;
}

export interface ReportWindow {
  since: string;
  until: string;
  granularity: string;
}

export interface ResolutionChallenge {
  id: number;
  playerId: number;
  playerName: string | null;
  itemId: number;
  achievementId: number;
  achievementName: string | null;
  targetLevel: number;
  reachedLevel: number;
  startedAt: string;
  endsAt: string;
  completedAt: string | null;
  badgeCode: string | null;
  badgeUrl: string | null;
  state: string;
}

export interface ResolutionOffer {
  id: number;
  achievementId: number;
  achievementName: string | null;
  category: string | null;
  orphaned: boolean;
  levelCount: number;
  targetLevelOffset: number;
  sortOrder: number;
  enabled: boolean;
  taken: number;
  completed: number;
  live: number;
  expired: number;
  completionRate: number;
}

export interface ResolutionTotals {
  offers: number;
  enabledOffers: number;
  orphanedOffers: number;
  taken: number;
  completed: number;
  live: number;
  expired: number;
  completionRate: number;
  players: number;
}

export interface RewardKindOption {
  name: string;
  value: number;
  target: string;
}

export interface RewardKindOptions {
  count: number;
  items: RewardKindOption[];
}

export interface RewardTrackActionOption {
  name: string;
  wired: boolean;
  facts: FactOption[];
}

export interface RewardTrackActionOptions {
  count: number;
  items: RewardTrackActionOption[];
}

export interface RewardTrackFilterRow {
  factKey: string;
  op: number;
  value: string;
}

export interface RewardTrackLevelRow {
  levelIndex: number;
  requiredCount: number;
  pointsReward: number;
  premium: boolean;
}

export interface RewardTrackList {
  count: number;
  items: RewardTrackRow[];
}

export interface RewardTrackPrizeRow {
  id: number;
  prizeId: string;
  requiredPoints: number;
  premium: boolean;
  sortOrder: number;
  reachable: boolean;
  rewards: RewardTrackRewardRow[];
}

export interface RewardTrackRewardRow {
  id: number;
  kind: string;
  kindValue: number;
  rewardTypeId: string;
  amount: number;
  extraParams: string;
  sortOrder: number;
}

export interface RewardTrackRow {
  id: number;
  trackId: string;
  localizationKey: string;
  theme: string;
  status: string;
  sortOrder: number;
  startsAt: string | null;
  progressEndsAt: string | null;
  claimEndsAt: string | null;
  unlockKind: string;
  unlockValue: string;
  completionPolicy: string;
  premiumEnabled: boolean;
  premiumBoostPerMille: number;
  premiumInstantPoints: number;
  premiumCostCredits: number;
  premiumCostDiamonds: number;
  contentVersion: number;
  hidden: boolean;
  campaignCode: string;
  freePointCeiling: number;
  premiumPointCeiling: number;
  participants: number;
  completions: number;
  premiumHolders: number;
  prizesClaimed: number;
  tasks: RewardTrackTaskRow[];
  prizes: RewardTrackPrizeRow[];
}

export interface RewardTrackStepRow {
  stepIndex: number;
  actionCode: string;
  filters: RewardTrackFilterRow[];
}

export interface RewardTrackTaskRow {
  id: number;
  taskId: string;
  localizationKey: string;
  actionCode: string;
  wired: boolean;
  parameter: string;
  mode: string;
  premium: boolean;
  sortOrder: number;
  steps: RewardTrackStepRow[];
  levels: RewardTrackLevelRow[];
}

export interface RoomDirectoryPage {
  count: number;
  total: number;
  offset: number;
  hasMore: boolean;
  items: RoomDirectoryRow[];
}

export interface RoomDirectoryRow {
  id: number;
  name: string;
  ownerName: string | null;
  usersNow: number;
  playersMax: number;
  lastActive: string;
}

export interface SanctionPresetKindOption {
  value: number;
  label: string;
}

export interface SanctionPresetRow {
  id: number;
  kind: string;
  presetIndex: number;
  name: string;
  durationSeconds: number | null;
  message: string | null;
  permanent: boolean;
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

export interface StaffAccountMatch {
  id: number;
  email: string;
  playerNames: string[];
  roleIds: number[];
}

export interface StaffAccountSearch {
  count: number;
  items: StaffAccountMatch[];
}

export interface StaffMember {
  id: number;
  email: string;
  createdAt: string;
  playerNames: string[];
  players: StaffPlayer[];
  roles: string[];
  roleIds: number[];
}

export interface StaffOverview {
  totals: StaffTotals;
  roles: StaffRole[];
  staff: StaffMember[];
  presets: SanctionPresetRow[];
  ungrantedCapabilities: string[];
  wildcardExists: boolean;
  allCapabilities: CapabilityGroup[];
  wildcard: string;
  presetKinds: SanctionPresetKindOption[];
}

export interface StaffPlayer {
  id: number;
  name: string;
  avatarUrl: string | null;
}

export interface StaffRole {
  id: number;
  key: string;
  name: string;
  capabilityCount: number;
  capabilities: string[];
  unknownCapabilities: string[];
  wildcard: boolean;
  holders: number;
}

export interface StaffTotals {
  roleCount: number;
  staffAccounts: number;
  declaredCapabilities: number;
  grantedCapabilities: number;
  ungrantedCapabilities: number;
  presetCount: number;
  activeBans: number;
}

export interface UntouchedAchievement {
  id: number;
  name: string;
  category: string;
  triggered: boolean;
  levels: number;
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

