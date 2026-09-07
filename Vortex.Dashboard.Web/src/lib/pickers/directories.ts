/**
 * Every directory the shared picker can browse, one entry each.
 *
 * PickerModal used to hold this itself, and it grew into a component that knew seventeen URLs, the
 * sort vocabulary of each, and four different row layouts in one `{#if kind === ...}` chain. That
 * is how a kind shipped listed in the chain and missing from the URL map: the fallback quietly
 * served the player directory, so a guild picker looked like it worked.
 *
 * Now the shell knows none of it. Adding a directory is one entry here and, only if it needs a
 * layout that does not exist yet, one row component.
 *
 * @typedef {Object} Directory
 * @property {string} endpoint       Where the rows come from.
 * @property {string} row            Which row component renders one: see components/pickers.
 * @property {string[]} [sorts]      The sort vocabulary the server accepts. Omitted means it orders
 *                                   itself, and the sort control is hidden rather than left dead.
 * @property {string} [filter]       An extra filter control this directory supports.
 */

/** @type {Record<string, Directory>} */
/** One picker directory: where its rows come from and how they are drawn. */
export type Directory = {
  endpoint: string;
  row: string;
  /** Absent when the directory offers no ordering of its own. */
  sorts?: string[];
  filter?: string;
};

export const DIRECTORIES: Record<string, Directory> = {
  // The three that predate the reward-track filters, each with its own layout and controls.
  furniture: {
    endpoint: '/api/v1/directory/furniture',
    row: 'furniture',
    sorts: ['relevance', 'name', 'id', 'idDesc', 'sprite', 'logic'],
    filter: 'logic',
  },
  room: {
    endpoint: '/api/v1/directory/rooms',
    row: 'room',
    sorts: ['relevance', 'name', 'id', 'idDesc'],
  },
  user: {
    endpoint: '/api/v1/directory/players',
    row: 'player',
    sorts: ['relevance', 'name', 'id', 'idDesc'],
    filter: 'online',
  },

  // Everything a filter value could otherwise be typed from memory. Each replaces a code or an id
  // that saves cleanly when misspelled and then never matches anything.
  group: { endpoint: '/api/v1/directory/groups', row: 'plain' },
  habbicon: { endpoint: '/api/v1/directory/habbicons', row: 'plain' },
  collection: { endpoint: '/api/v1/directory/habbicon-collections', row: 'plain' },
  offer: { endpoint: '/api/v1/directory/catalog-offers', row: 'plain' },
  category: { endpoint: '/api/v1/directory/navigator-categories', row: 'plain' },
  badge: { endpoint: '/api/v1/directory/badges', row: 'plain' },
  petSpecies: { endpoint: '/api/v1/directory/pet-species', row: 'plain' },
  poll: { endpoint: '/api/v1/directory/polls', row: 'plain' },
  quiz: { endpoint: '/api/v1/directory/quizzes', row: 'plain' },
  campaign: { endpoint: '/api/v1/directory/quest-campaigns', row: 'plain' },
  voucher: { endpoint: '/api/v1/directory/vouchers', row: 'plain' },
  clubGift: { endpoint: '/api/v1/directory/club-gifts', row: 'plain' },
  nftProduct: { endpoint: '/api/v1/directory/nft-store', row: 'plain' },
  targetedOffer: { endpoint: '/api/v1/directory/targeted-offers', row: 'plain' },
  thread: { endpoint: '/api/v1/directory/forum-threads', row: 'plain' },
  effect: { endpoint: '/api/v1/directory/avatar-effects', row: 'plain' },
  placedItem: { endpoint: '/api/v1/directory/placed-furniture', row: 'plain' },
};

/**
 * One row a directory returns.
 *
 * Every directory answers with an id and a name -- that is what makes them one picker rather than
 * seventeen. Everything below it is a layout extra: the directory whose row component draws it
 * sends it, and the other nineteen do not, which is why each is optional here rather than split
 * into a type per directory that the shared picker would then have to choose between.
 */
export type PickerRow = {
  id: number;
  name: string;
  /** plain */
  description?: string | null;
  /** player */
  avatarUrl?: string | null;
  online?: boolean;
  /** furniture */
  iconUrl?: string | null;
  spriteId?: number | null;
  logic?: string | null;
  type?: string | null;
  canTrade?: boolean;
  /** room */
  ownerName?: string | null;
  usersNow?: number | null;
  playersMax?: number | null;
  /** Whatever else the directory thought worth saying about it. */
  [field: string]: unknown;
};

/** The directory for a kind, or null. Null is a bug the picker reports rather than papers over. */
export function directoryFor(kind: string): Directory | null {
  return DIRECTORIES[kind] ?? null;
}
