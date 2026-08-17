export function safeInternalPath(value: string | null | undefined): string | null {
  if (!value) {
    return null;
  }

  const trimmed = value.trim();
  if (!trimmed.startsWith('/') || trimmed.startsWith('//') || trimmed.startsWith('/\\')) {
    return null;
  }

  if (trimmed.includes('://') || trimmed.includes('\\')) {
    return null;
  }

  return trimmed;
}

export function internalReturnLink(
  value: string | null | undefined,
): { path: string; queryParams: Record<string, string> } | null {
  const path = safeInternalPath(value);
  if (!path) {
    return null;
  }

  const [pathname, search] = path.split('?');
  const queryParams: Record<string, string> = {};
  if (search) {
    for (const [key, item] of new URLSearchParams(search)) {
      queryParams[key] = item;
    }
  }

  return { path: pathname, queryParams };
}
