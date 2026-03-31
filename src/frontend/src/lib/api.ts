import {
  getAccessToken,
  getRefreshToken,
  setTokens,
  clearTokens,
  isTokenExpired,
} from "./auth";

const BASE = "/api";

let refreshPromise: Promise<boolean> | null = null;

async function tryRefresh(): Promise<boolean> {
  const accessToken = getAccessToken();
  const refreshToken = getRefreshToken();
  if (!accessToken || !refreshToken) return false;

  try {
    const res = await fetch(`${BASE}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accessToken, refreshToken }),
    });
    if (!res.ok) return false;
    const data = await res.json();
    setTokens(data.accessToken, data.refreshToken);
    return true;
  } catch {
    return false;
  }
}

export async function apiFetch<T>(
  path: string,
  init?: RequestInit,
): Promise<T> {
  let token = getAccessToken();

  // Proactively refresh if token is expired
  if (token && isTokenExpired(token)) {
    if (!refreshPromise) refreshPromise = tryRefresh();
    const ok = await refreshPromise;
    refreshPromise = null;
    if (!ok) {
      clearTokens();
      window.location.href = "/login";
      throw new Error("Session expired");
    }
    token = getAccessToken();
  }

  const headers: Record<string, string> = {
    ...(init?.body instanceof FormData
      ? {}
      : { "Content-Type": "application/json" }),
    ...(init?.headers as Record<string, string>),
  };
  if (token) {
    headers["Authorization"] = `Bearer ${token}`;
  }

  const res = await fetch(`${BASE}${path}`, { ...init, headers });

  // Handle 401 with refresh retry
  if (res.status === 401 && token) {
    if (!refreshPromise) refreshPromise = tryRefresh();
    const ok = await refreshPromise;
    refreshPromise = null;
    if (ok) {
      headers["Authorization"] = `Bearer ${getAccessToken()}`;
      const retry = await fetch(`${BASE}${path}`, { ...init, headers });
      if (!retry.ok) throw new Error(`${retry.status} ${retry.statusText}`);
      if (retry.status === 204) return undefined as T;
      return retry.json();
    }
    clearTokens();
    window.location.href = "/login";
    throw new Error("Session expired");
  }

  if (!res.ok) throw new Error(`${res.status} ${res.statusText}`);
  if (res.status === 204) return undefined as T;
  return res.json();
}
