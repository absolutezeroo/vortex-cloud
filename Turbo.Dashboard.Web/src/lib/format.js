export function formatDate(value, fallback = '-') {
  if (!value) {
    return fallback;
  }

  const parsed = Date.parse(value);
  if (!Number.isFinite(parsed)) {
    return value;
  }

  return new Date(parsed).toLocaleString();
}

export function formatNumber(value, decimals = 0) {
  const numeric = Number(value || 0);
  return new Intl.NumberFormat('en-US', {
    maximumFractionDigits: decimals,
    minimumFractionDigits: decimals,
  }).format(numeric);
}

export function formatDuration(seconds) {
  const total = Math.max(0, Number(seconds || 0));
  const days = Math.floor(total / 86400);
  const hours = Math.floor((total % 86400) / 3600);
  const minutes = Math.floor((total % 3600) / 60);

  if (days > 0) {
    return `${days}d ${hours}h`;
  }

  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }

  return `${minutes}m`;
}

export function compactCorrelation(value) {
  return value ? String(value).substring(0, 8) : '-';
}

export function summarizeData(value) {
  if (!value) {
    return '-';
  }

  const text = String(value).trim();
  if (!text) {
    return '-';
  }

  try {
    const parsed = JSON.parse(text);
    if (!parsed || typeof parsed !== 'object') {
      return text;
    }

    return Object.entries(parsed)
      .filter(([, entryValue]) => entryValue !== null && entryValue !== undefined && entryValue !== '')
      .slice(0, 4)
      .map(([key, entryValue]) => `${key}=${entryValue}`)
      .join(' - ');
  } catch {
    return text.length > 160 ? `${text.substring(0, 160)}...` : text;
  }
}
