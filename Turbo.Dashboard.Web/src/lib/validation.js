// Shared field validators for admin-action forms (OperationsPage, ModerationActionsPage, ...).
// Every mutating form uses the same "reason >= 3 chars" convention so operators learn one rule.

export function reasonOk(reason) {
  return typeof reason === 'string' && reason.trim().length >= 3;
}

export function positive(value) {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric > 0;
}

export function nonNegative(value) {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric >= 0;
}
