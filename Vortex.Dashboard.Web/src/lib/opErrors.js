// Turns the server's domain rejection codes into something an operator can act on.
//
// Every dashboard write goes through DashboardOperationsService.ExecuteAsync, which catches
// InvalidOperationException and returns the domain's own code as OperationResult.Message --
// `offer_has_products`, `page_has_children`, `role_still_assigned`. That code is the whole reason
// the write was refused, and it already travels all the way to the browser; it just had nowhere
// readable to land.
//
// Unknown codes are humanised rather than swallowed: a new rejection shipped on the server without a
// translation here shows as "Offer has products" instead of a generic failure, so the operator can
// still read it, search it and quote it. Never fall back to "Failed" -- that was the bug.
import { translate } from './i18n.js';

const NAMESPACE = 'opError.';

/**
 * @param {string|null|undefined} code The server's OperationResult.message.
 * @returns {string} A sentence for the operator.
 */
export function describeOpError(code) {
  if (typeof code !== 'string' || code.trim() === '') {
    return translate('common.resultFailed');
  }

  const key = NAMESPACE + code;
  const translated = translate(key);

  // `translate` returns the key itself when it resolves nothing, in either dictionary.
  return translated === key ? humanise(code) : translated;
}

/** `offer_has_products` -> `Offer has products`. */
function humanise(code) {
  const words = code.replace(/_/g, ' ').trim();

  return words.charAt(0).toUpperCase() + words.slice(1);
}
