import { apiFetchRaw, ApiError } from "@/lib/api";
import type { FillUp } from "@/lib/types";

export interface ReceiptUploadCallbacks {
  onSuccess?: (fillUp: FillUp) => void;
  onFailure?: (error: Error) => void;
}

/** Backoff schedule for transient failures; length = max retries after the first attempt. */
const RETRY_DELAYS_MS = [2_000, 5_000, 15_000, 30_000];

let pendingCount = 0;

function beforeUnloadHandler(e: BeforeUnloadEvent) {
  // A receipt upload is still in flight; leaving now would lose the file.
  e.preventDefault();
  e.returnValue = "";
}

function trackPending(delta: 1 | -1) {
  const wasPending = pendingCount > 0;
  pendingCount += delta;
  if (!wasPending && pendingCount > 0) {
    window.addEventListener("beforeunload", beforeUnloadHandler);
  } else if (wasPending && pendingCount === 0) {
    window.removeEventListener("beforeunload", beforeUnloadHandler);
  }
}

export function pendingReceiptUploads(): number {
  return pendingCount;
}

/** 4xx responses are permanent (validation, auth); network errors and 5xx/408/429 are worth retrying. */
function isTransient(err: unknown): boolean {
  if (err instanceof ApiError) {
    return err.status >= 500 || err.status === 408 || err.status === 429;
  }
  // fetch() rejects with TypeError on network failure
  return true;
}

function sleep(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Upload a receipt for an already-saved fill-up without blocking the UI.
 * Retries transient failures with exponential backoff; survives SPA route
 * changes because nothing here is tied to a component lifecycle.
 */
export function uploadReceiptInBackground(
  fillUpId: string,
  file: File,
  callbacks: ReceiptUploadCallbacks = {}
): void {
  trackPending(1);
  void (async () => {
    try {
      for (let attempt = 0; ; attempt++) {
        try {
          const formData = new FormData();
          formData.append("receipt", file);
          const res = await apiFetchRaw(`/api/fill-ups/${fillUpId}/receipt`, {
            method: "PUT",
            body: formData,
          });
          const fillUp = (await res.json()) as FillUp;
          callbacks.onSuccess?.(fillUp);
          return;
        } catch (err) {
          if (attempt >= RETRY_DELAYS_MS.length || !isTransient(err)) {
            callbacks.onFailure?.(
              err instanceof Error ? err : new Error("Receipt upload failed")
            );
            return;
          }
          await sleep(RETRY_DELAYS_MS[attempt]);
        }
      }
    } finally {
      trackPending(-1);
    }
  })();
}
