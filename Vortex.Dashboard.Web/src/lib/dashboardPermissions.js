export const CAPABILITIES = {
  overviewRead: 'dashboard.overview.read',
  auditRead: 'dashboard.audit.read',
  economyRead: 'dashboard.economy.read',
  playersRead: 'dashboard.players.read',
  furnitureRead: 'dashboard.furniture.read',
  opsGrantCurrency: 'dashboard.ops.currency.grant',
  opsGrantItem: 'dashboard.ops.item.grant',
  opsKickPlayer: 'dashboard.ops.player.kick',
  opsBanAccount: 'dashboard.ops.player.ban',
  opsMutePlayer: 'dashboard.ops.player.mute',
  opsTradingLock: 'dashboard.ops.player.trading_lock',
  opsCfhManage: 'dashboard.ops.cfh.manage',
  opsRoomsManage: 'dashboard.ops.rooms.manage',
  opsManageVouchers: 'dashboard.ops.vouchers.manage',
  catalogRead: 'dashboard.catalog.read',
  opsCatalogManage: 'dashboard.ops.catalog.manage',
  opsFurnitureManage: 'dashboard.ops.furniture.manage',
  groupsRead: 'dashboard.groups.read',
  petsRead: 'dashboard.pets.read',
  cfhRead: 'dashboard.cfh.read',
  catalogPurchasesRead: 'dashboard.catalog.purchases.read',
  wiredRead: 'dashboard.wired.read',
  targetedOffersRead: 'dashboard.targeted_offers.read',
  opsTargetedOffersManage: 'dashboard.ops.targeted_offers.manage',
};

export const ROUTE_PERMISSIONS = {
  overview: [CAPABILITIES.overviewRead],
  infrastructure: [CAPABILITIES.overviewRead],
  incidents: [CAPABILITIES.overviewRead],
  packets: [CAPABILITIES.overviewRead],
  economy: [CAPABILITIES.economyRead],
  investigation: [CAPABILITIES.auditRead],
  rooms: [CAPABILITIES.auditRead],
  audit: [CAPABILITIES.auditRead],
  moderation: [CAPABILITIES.auditRead],
  moderationActions: [
    CAPABILITIES.opsBanAccount,
    CAPABILITIES.opsMutePlayer,
    CAPABILITIES.opsTradingLock,
  ],
  cfh: [CAPABILITIES.opsCfhManage],
  roomControl: [CAPABILITIES.opsRoomsManage],
  vouchers: [CAPABILITIES.opsManageVouchers],
  catalog: [CAPABILITIES.catalogRead],
  furnitureDefinitions: [CAPABILITIES.furnitureRead],
  groupsStats: [CAPABILITIES.groupsRead],
  petsStats: [CAPABILITIES.petsRead],
  cfhStats: [CAPABILITIES.cfhRead],
  catalogPurchases: [CAPABILITIES.catalogPurchasesRead],
  wiredStats: [CAPABILITIES.wiredRead],
  targetedOffers: [CAPABILITIES.targetedOffersRead],
  targetedOffersStats: [CAPABILITIES.targetedOffersRead],
  apiExplorer: [CAPABILITIES.overviewRead],
  operations: [
    CAPABILITIES.opsGrantCurrency,
    CAPABILITIES.opsGrantItem,
    CAPABILITIES.opsKickPlayer,
  ],
};

export const OPERATION_CAPABILITIES = {
  credits: CAPABILITIES.opsGrantCurrency,
  activity: CAPABILITIES.opsGrantCurrency,
  item: CAPABILITIES.opsGrantItem,
  kick: CAPABILITIES.opsKickPlayer,
};

// Per-action capability map for the moderation action forms on ModerationPage — mirrors
// OPERATION_CAPABILITIES' role for OperationsPage.
export const MODERATION_OPERATION_CAPABILITIES = {
  ban: CAPABILITIES.opsBanAccount,
  unban: CAPABILITIES.opsBanAccount,
  mute: CAPABILITIES.opsMutePlayer,
  tradingLock: CAPABILITIES.opsTradingLock,
  tradingUnlock: CAPABILITIES.opsTradingLock,
};
