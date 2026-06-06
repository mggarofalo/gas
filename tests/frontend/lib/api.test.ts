import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  getAccessToken,
  getRefreshToken,
  setTokens,
  clearTokens,
  isAuthenticated,
  mustResetPassword,
  apiFetch,
  login,
  refreshAccessToken,
  hasValidAccessToken,
  getTokenExpiry,
} from "@/lib/api";

// Helper: build a minimal JWT with given payload
function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: "HS256", typ: "JWT" }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake-sig`;
}

beforeEach(() => {
  localStorage.clear();
});

// --- Token storage ---

describe("token storage", () => {
  it("getAccessToken returns null when empty", () => {
    expect(getAccessToken()).toBeNull();
  });

  it("getRefreshToken returns null when empty", () => {
    expect(getRefreshToken()).toBeNull();
  });

  it("setTokens stores and retrieves tokens", () => {
    setTokens({ accessToken: "acc", refreshToken: "ref" });
    expect(getAccessToken()).toBe("acc");
    expect(getRefreshToken()).toBe("ref");
  });

  it("clearTokens removes both tokens", () => {
    setTokens({ accessToken: "acc", refreshToken: "ref" });
    clearTokens();
    expect(getAccessToken()).toBeNull();
    expect(getRefreshToken()).toBeNull();
  });
});

// --- isAuthenticated ---

describe("isAuthenticated", () => {
  it("returns false when no token", () => {
    expect(isAuthenticated()).toBe(false);
  });

  it("returns true when token is set", () => {
    setTokens({ accessToken: "tok", refreshToken: "ref" });
    expect(isAuthenticated()).toBe(true);
  });
});

// --- hasValidAccessToken / getTokenExpiry ---

describe("hasValidAccessToken", () => {
  it("returns false when no token", () => {
    expect(hasValidAccessToken()).toBe(false);
  });

  it("returns false when access token is past its exp", () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) - 60 });
    setTokens({ accessToken: token, refreshToken: "ref" });
    expect(hasValidAccessToken()).toBe(false);
  });

  it("returns true when access token is unexpired", () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });
    expect(hasValidAccessToken()).toBe(true);
  });

  it("returns true when token has no exp claim (treat as non-expiring)", () => {
    const token = makeJwt({ sub: "x" });
    setTokens({ accessToken: token, refreshToken: "ref" });
    expect(hasValidAccessToken()).toBe(true);
  });
});

describe("getTokenExpiry", () => {
  it("returns ms-since-epoch for a JWT with exp", () => {
    const token = makeJwt({ exp: 1700000000 });
    expect(getTokenExpiry(token)).toBe(1700000000 * 1000);
  });

  it("returns null for a malformed token", () => {
    expect(getTokenExpiry("not.a.jwt")).toBeNull();
  });
});

// --- refreshAccessToken ---

describe("refreshAccessToken", () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("returns false when there is no refresh token", async () => {
    expect(await refreshAccessToken()).toBe(false);
  });

  it("sends BOTH accessToken and refreshToken per backend contract", async () => {
    setTokens({ accessToken: "old-access", refreshToken: "old-refresh" });
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () =>
        Promise.resolve({ accessToken: "new-access", refreshToken: "new-refresh" }),
    });

    const ok = await refreshAccessToken();

    expect(ok).toBe(true);
    const call = vi.mocked(globalThis.fetch).mock.calls[0];
    expect(call[0]).toBe("/api/auth/refresh");
    expect(JSON.parse(call[1]?.body as string)).toEqual({
      accessToken: "old-access",
      refreshToken: "old-refresh",
    });
    expect(getAccessToken()).toBe("new-access");
    expect(getRefreshToken()).toBe("new-refresh");
  });

  it("returns false and does not overwrite tokens on non-ok response", async () => {
    setTokens({ accessToken: "old-access", refreshToken: "old-refresh" });
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
    });

    expect(await refreshAccessToken()).toBe(false);
    expect(getAccessToken()).toBe("old-access");
  });

  it("deduplicates concurrent refreshes", async () => {
    setTokens({ accessToken: "old", refreshToken: "ref" });
    let resolveFetch: ((v: unknown) => void) | null = null;
    globalThis.fetch = vi.fn().mockImplementation(
      () =>
        new Promise((resolve) => {
          resolveFetch = resolve;
        })
    );

    const a = refreshAccessToken();
    const b = refreshAccessToken();
    resolveFetch!({
      ok: true,
      json: () => Promise.resolve({ accessToken: "new", refreshToken: "new-ref" }),
    });

    await Promise.all([a, b]);
    expect(vi.mocked(globalThis.fetch).mock.calls.length).toBe(1);
  });
});

// --- mustResetPassword ---

describe("mustResetPassword", () => {
  it("returns false when no token", () => {
    expect(mustResetPassword()).toBe(false);
  });

  it("returns true when JWT has must_reset_password claim", () => {
    const token = makeJwt({ must_reset_password: true, exp: 9999999999 });
    setTokens({ accessToken: token, refreshToken: "ref" });
    expect(mustResetPassword()).toBe(true);
  });

  it("returns false when JWT has must_reset_password = false", () => {
    const token = makeJwt({ must_reset_password: false, exp: 9999999999 });
    setTokens({ accessToken: token, refreshToken: "ref" });
    expect(mustResetPassword()).toBe(false);
  });

  it("returns false for malformed token", () => {
    setTokens({ accessToken: "not.valid.jwt", refreshToken: "ref" });
    expect(mustResetPassword()).toBe(false);
  });
});

// --- apiFetch ---

describe("apiFetch", () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("sets Authorization header from stored token", async () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({ data: 1 }),
    });

    await apiFetch("/api/test");

    const call = vi.mocked(globalThis.fetch).mock.calls[0];
    const headers = call[1]?.headers as Headers;
    expect(headers.get("Authorization")).toBe(`Bearer ${token}`);
  });

  it("sets Content-Type to json by default", async () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.resolve({}),
    });

    await apiFetch("/api/test");

    const call = vi.mocked(globalThis.fetch).mock.calls[0];
    const headers = call[1]?.headers as Headers;
    expect(headers.get("Content-Type")).toBe("application/json");
  });

  it("returns undefined for 204 No Content", async () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      status: 204,
    });

    const result = await apiFetch("/api/test");
    expect(result).toBeUndefined();
  });

  it("throws on non-ok response with parsed error message", async () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 400,
      text: () => Promise.resolve(JSON.stringify({ message: "Bad request" })),
    });

    await expect(apiFetch("/api/test")).rejects.toThrow("Bad request");
  });

  it("throws with raw text when error response is not JSON", async () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 500,
      text: () => Promise.resolve("Internal Server Error"),
    });

    await expect(apiFetch("/api/test")).rejects.toThrow("Internal Server Error");
  });
});

// --- login ---

describe("login", () => {
  const originalFetch = globalThis.fetch;

  afterEach(() => {
    globalThis.fetch = originalFetch;
  });

  it("posts credentials and returns tokens", async () => {
    const tokens = { accessToken: "a", refreshToken: "r" };
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () => Promise.resolve(tokens),
    });

    const result = await login("user@test.com", "pass");

    expect(result).toEqual(tokens);
    const call = vi.mocked(globalThis.fetch).mock.calls[0];
    expect(call[0]).toBe("/api/auth/login");
    expect(JSON.parse(call[1]?.body as string)).toEqual({
      email: "user@test.com",
      password: "pass",
    });
  });

  it("throws on failed login with parsed message", async () => {
    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      text: () => Promise.resolve(JSON.stringify({ message: "Invalid credentials" })),
    });

    await expect(login("user@test.com", "wrong")).rejects.toThrow("Invalid credentials");
  });
});
