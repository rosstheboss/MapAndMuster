function hasControlCharacter(value: string): boolean {
  for (let index = 0; index < value.length; index += 1) {
    const code = value.charCodeAt(index);
    if (code <= 31 || code === 127) {
      return true;
    }
  }

  return false;
}

const STORAGE_KEY = 'mapandmuster.auth-return';
const MAX_AGE_MS = 30 * 60 * 1000;

/** Sign-in and account-creation routes must not become post-login destinations. */
const BLOCKED_PREFIXES = [
  '/login',
  '/register',
  '/confirm-email',
  '/forgot-password',
  '/reset-password',
  '/complete-external',
] as const;

interface StoredReturn {
  url: string;
  at: number;
}

/**
 * Accepts a same-origin relative path. External, protocol-relative, and account-flow
 * addresses are rejected so a return link cannot leave the app or loop on login.
 */
export function safeReturnUrl(value: string | null | undefined): string | null {
  if (value === null || value === undefined) {
    return null;
  }

  const trimmed = value.trim();
  if (trimmed.length === 0 || trimmed.length > 2048) {
    return null;
  }

  let decoded: string;
  try {
    decoded = decodeURIComponent(trimmed);
  } catch {
    return null;
  }

  if (!decoded.startsWith('/') || decoded.startsWith('//') || decoded.includes('\\') || decoded.includes('://')) {
    return null;
  }

  if (hasControlCharacter(decoded)) {
    return null;
  }

  const hashIndex = decoded.indexOf('#');
  const withoutHash = hashIndex >= 0 ? decoded.slice(0, hashIndex) : decoded;
  const path = withoutHash.split('?')[0] ?? withoutHash;
  if (path.includes('//')) {
    return null;
  }

  const normalized = path.length > 1 && path.endsWith('/') ? path.slice(0, -1) : path;
  if (normalized === '' || normalized === '/') {
    return null;
  }

  if (BLOCKED_PREFIXES.some((prefix) => normalized === prefix || normalized.startsWith(`${prefix}/`))) {
    return null;
  }

  return decoded;
}

export function returnUrlOrHome(value: string | null | undefined): string {
  return safeReturnUrl(value) ?? '/';
}

export function rememberReturnUrl(value: string | null | undefined): void {
  const safe = safeReturnUrl(value);
  if (!safe) {
    clearReturnUrl();
    return;
  }

  try {
    const stored: StoredReturn = { url: safe, at: Date.now() };
    sessionStorage.setItem(STORAGE_KEY, JSON.stringify(stored));
  } catch {
    // Password and guest sign-in still read the login query when storage is unavailable.
  }
}

export function peekReturnUrl(): string | null {
  try {
    const raw = sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }

    const parsed = JSON.parse(raw) as Partial<StoredReturn>;
    if (typeof parsed.url !== 'string' || typeof parsed.at !== 'number' || Date.now() - parsed.at > MAX_AGE_MS) {
      clearReturnUrl();
      return null;
    }

    const safe = safeReturnUrl(parsed.url);
    if (!safe) {
      clearReturnUrl();
    }

    return safe;
  } catch {
    return null;
  }
}

export function takeReturnUrl(): string | null {
  const url = peekReturnUrl();
  clearReturnUrl();
  return url;
}

export function clearReturnUrl(): void {
  try {
    sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // Storage can be disabled; there is then nothing to clear.
  }
}
