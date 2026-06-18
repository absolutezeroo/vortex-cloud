export const CAPABILITIES = {
  overviewRead: 'dashboard.overview.read',
  auditRead: 'dashboard.audit.read',
  economyRead: 'dashboard.economy.read',
  playersRead: 'dashboard.players.read',
  furnitureRead: 'dashboard.furniture.read',
  opsGrantCurrency: 'dashboard.ops.currency.grant',
  opsGrantItem: 'dashboard.ops.item.grant',
  opsKickPlayer: 'dashboard.ops.player.kick',
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
