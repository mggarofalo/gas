import { useEffect, useState } from "react";
import {
  clearTokens,
  getAccessToken,
  getTokenExpiry,
  refreshAccessToken,
} from "@/lib/api";
import Spinner from "@/components/Spinner";

/**
 * Resolves the session before mounting the router, so route guards see a
 * consistent token state. If the access token is missing or already valid,
 * we render immediately. If it's expired but a refresh token is present,
 * we show a centered spinner while refreshing, then mount the router with
 * either the new token or no token (depending on refresh outcome).
 *
 * Without this, the router admits stale sessions and the authenticated
 * layout briefly paints before a 401-triggered redirect to /login.
 */
export default function AuthGate({ children }: { children: React.ReactNode }) {
  const [ready, setReady] = useState(() => {
    const token = getAccessToken();
    if (!token) return true;
    const expiry = getTokenExpiry(token);
    return expiry == null || expiry > Date.now();
  });

  useEffect(() => {
    if (ready) return;
    let cancelled = false;
    (async () => {
      const ok = await refreshAccessToken();
      if (cancelled) return;
      if (!ok) clearTokens();
      setReady(true);
    })();
    return () => {
      cancelled = true;
    };
  }, [ready]);

  if (!ready) {
    return (
      <div
        role="status"
        aria-live="polite"
        aria-busy="true"
        className="flex h-screen items-center justify-center bg-gray-50 dark:bg-gray-900"
      >
        <Spinner />
        <span className="sr-only">Restoring your session</span>
      </div>
    );
  }

  return <>{children}</>;
}
