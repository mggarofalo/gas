import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import AuthGate from "@/components/AuthGate";
import { setTokens, getAccessToken } from "@/lib/api";

function makeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: "HS256", typ: "JWT" }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake-sig`;
}

beforeEach(() => {
  localStorage.clear();
});

const originalFetch = globalThis.fetch;
afterEach(() => {
  globalThis.fetch = originalFetch;
});

describe("AuthGate", () => {
  it("renders children immediately when no token is present (synchronous)", () => {
    render(
      <AuthGate>
        <div>app</div>
      </AuthGate>
    );
    expect(screen.getByText("app")).toBeInTheDocument();
  });

  it("renders children immediately when access token is unexpired", () => {
    const token = makeJwt({ exp: Math.floor(Date.now() / 1000) + 3600 });
    setTokens({ accessToken: token, refreshToken: "ref" });

    render(
      <AuthGate>
        <div>app</div>
      </AuthGate>
    );
    expect(screen.getByText("app")).toBeInTheDocument();
  });

  it("shows an accessible spinner while refreshing, then renders app", async () => {
    const expired = makeJwt({ exp: Math.floor(Date.now() / 1000) - 60 });
    setTokens({ accessToken: expired, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: true,
      json: () =>
        Promise.resolve({ accessToken: "new-access", refreshToken: "new-ref" }),
    });

    render(
      <AuthGate>
        <div>app</div>
      </AuthGate>
    );

    // App not rendered yet; an accessible live region announces the wait.
    expect(screen.queryByText("app")).not.toBeInTheDocument();
    const status = screen.getByRole("status");
    expect(status).toHaveAttribute("aria-live", "polite");
    expect(status).toHaveAttribute("aria-busy", "true");
    expect(status).toHaveTextContent(/restoring your session/i);

    await waitFor(() => expect(screen.getByText("app")).toBeInTheDocument());
    expect(getAccessToken()).toBe("new-access");
  });

  it("clears tokens and renders app when refresh fails (router will redirect)", async () => {
    const expired = makeJwt({ exp: Math.floor(Date.now() / 1000) - 60 });
    setTokens({ accessToken: expired, refreshToken: "ref" });

    globalThis.fetch = vi.fn().mockResolvedValue({
      ok: false,
      status: 401,
    });

    render(
      <AuthGate>
        <div>app</div>
      </AuthGate>
    );

    await waitFor(() => expect(screen.getByText("app")).toBeInTheDocument());
    expect(getAccessToken()).toBeNull();
  });
});

