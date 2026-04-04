import type { AuthTokens } from "./types";

const ACCESS_TOKEN_KEY = "gas_access_token";
const REFRESH_TOKEN_KEY = "gas_refresh_token";

export function getAccessToken(): string | null {
  return localStorage.getItem(ACCESS_TOKEN_KEY);
}

export function getRefreshToken(): string | null {
  return localStorage.getItem(REFRESH_TOKEN_KEY);
}

export function setTokens(tokens: AuthTokens): void {
  localStorage.setItem(ACCESS_TOKEN_KEY, tokens.accessToken);
  localStorage.setItem(REFRESH_TOKEN_KEY, tokens.refreshToken);
}

export function clearTokens(): void {
  localStorage.removeItem(ACCESS_TOKEN_KEY);
  localStorage.removeItem(REFRESH_TOKEN_KEY);
}

export function isAuthenticated(): boolean {
  return !!getAccessToken();
}

/** Decode JWT payload to check expiration */
function getTokenExpiry(token: string): number | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.exp ? payload.exp * 1000 : null;
  } catch {
    return null;
  }
}

function getTokenClaim<T>(token: string, claim: string): T | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload[claim] ?? null;
  } catch {
    return null;
  }
}

export function mustResetPassword(): boolean {
  const token = getAccessToken();
  if (!token) return false;
  return getTokenClaim<boolean>(token, "must_reset_password") === true;
}

/** Module-level promise for deduplicating concurrent refresh calls */
let refreshPromise: Promise<boolean> | null = null;

async function refreshAccessToken(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    const refreshToken = getRefreshToken();
    if (!refreshToken) return false;

    try {
      const res = await fetch("/api/auth/refresh", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken }),
      });

      if (!res.ok) return false;

      const data: AuthTokens = await res.json();
      setTokens(data);
      return true;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

/** Proactively refresh if token expires within 60 seconds */
async function ensureFreshToken(): Promise<void> {
  const token = getAccessToken();
  if (!token) return;

  const expiry = getTokenExpiry(token);
  if (expiry && expiry - Date.now() < 60_000) {
    await refreshAccessToken();
  }
}

export async function apiFetch<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  await ensureFreshToken();

  const token = getAccessToken();
  const headers = new Headers(options.headers);
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }
  if (!headers.has("Content-Type") && !(options.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  let res = await fetch(url, { ...options, headers });

  // On 401, try refreshing and retrying once
  if (res.status === 401) {
    const refreshed = await refreshAccessToken();
    if (refreshed) {
      const newToken = getAccessToken();
      if (newToken) {
        headers.set("Authorization", `Bearer ${newToken}`);
      }
      res = await fetch(url, { ...options, headers });
    }

    if (res.status === 401) {
      clearTokens();
      window.location.href = "/login";
      throw new Error("Unauthorized");
    }
  }

  if (!res.ok) {
    const body = await res.text();
    let message: string;
    try {
      const json = JSON.parse(body);
      message = json.message || json.error || body;
    } catch {
      message = body;
    }
    throw new Error(message);
  }

  // Handle 204 No Content
  if (res.status === 204) {
    return undefined as T;
  }

  return res.json();
}

/** Login - does not use apiFetch since we don't have a token yet */
export async function login(
  email: string,
  password: string
): Promise<AuthTokens> {
  const res = await fetch("/api/auth/login", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ email, password }),
  });

  if (!res.ok) {
    const body = await res.text();
    let message: string;
    try {
      const json = JSON.parse(body);
      message = json.message || json.error || body;
    } catch {
      message = body;
    }
    throw new Error(message);
  }

  return res.json();
}
