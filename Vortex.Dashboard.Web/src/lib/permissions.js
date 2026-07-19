export function hasErrorCode(err, code) {
  return err && err.code === code;
}

export function isPermissionDeniedError(err) {
  return Number(err?.status) === 403 || hasErrorCode(err, 'forbidden');
}

export function hasDashboardCapability(identity, required) {
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
