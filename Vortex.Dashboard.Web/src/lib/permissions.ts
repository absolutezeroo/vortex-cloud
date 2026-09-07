/** A dashboard identity, as /api/me answers it. */
export type Identity = {
  superuser?: boolean;
  capabilities?: string[];
  [key: string]: unknown;
};

export function hasErrorCode(err: unknown, code: string): boolean {
  return Boolean(err) && (err as { code?: string }).code === code;
}

export function isPermissionDeniedError(err: unknown): boolean {
  return (
    Number((err as { status?: number })?.status) === 403 || hasErrorCode(err, 'forbidden')
  );
}

export function hasDashboardCapability(
  identity: Identity | null | undefined,
  required: string | string[],
): boolean {
  if (!identity) {
    return false;
  }

  if (identity.superuser) {
    return true;
  }

  const capabilities = new Set(
    (identity.capabilities || [])
      .map((cap) => String(cap).trim().toLowerCase())
      .filter((cap) => cap.length > 0),
  );

  if (capabilities.has('*')) {
    return true;
  }

  if (Array.isArray(required)) {
    return required.some((capability) => capabilities.has(String(capability).toLowerCase().trim()));
  }

  return capabilities.has(String(required || '').toLowerCase().trim());
}
