// Shared field validators for admin-action forms (PlayerOperationsPanel, ModerationActionsPage, ...).
// Every mutating form uses the same "reason >= 3 chars" convention so operators learn one rule.

export function reasonOk(reason: unknown): boolean {
  return typeof reason === 'string' && reason.trim().length >= 3;
}

export function positive(value: unknown): boolean {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric > 0;
}

export function nonNegative(value: unknown): boolean {
  const numeric = Number(value);
  return Number.isFinite(numeric) && numeric >= 0;
}
