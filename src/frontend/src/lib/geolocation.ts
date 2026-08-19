const STORAGE_KEY = "gas_last_position";

/**
 * How long a remembered fix stays usable. Short enough that it can't survive a
 * drive to a different station, long enough to cover filling the form out,
 * saving, and coming back to edit it.
 */
export const REMEMBERED_MAX_AGE_MS = 5 * 60 * 1000;

export interface Coordinates {
  latitude: number;
  longitude: number;
}

export interface PositionResult extends Coordinates {
  /** True when the fix came from storage, so no permission prompt was raised. */
  remembered: boolean;
  /** Epoch ms the fix was taken. */
  timestamp: number;
}

interface StoredPosition extends Coordinates {
  timestamp: number;
}

/**
 * Reuse the last fix when it is recent. Safari re-asks for permission on most
 * page loads, so skipping the browser call entirely is the only reliable way to
 * avoid a second prompt; a genuine grant, when the browser keeps one, still
 * short-circuits through `maximumAge` below.
 */
export function readRememberedPosition(
  maxAgeMs: number = REMEMBERED_MAX_AGE_MS
): StoredPosition | null {
  let raw: string | null = null;
  try {
    raw = localStorage.getItem(STORAGE_KEY);
  } catch {
    return null; // Storage blocked (private browsing) — just ask again.
  }
  if (!raw) return null;

  try {
    const parsed = JSON.parse(raw) as Partial<StoredPosition>;
    if (
      typeof parsed?.latitude !== "number" ||
      typeof parsed?.longitude !== "number" ||
      typeof parsed?.timestamp !== "number"
    ) {
      return null;
    }
    const age = Date.now() - parsed.timestamp;
    if (age < 0 || age > maxAgeMs) return null;
    return {
      latitude: parsed.latitude,
      longitude: parsed.longitude,
      timestamp: parsed.timestamp,
    };
  } catch {
    return null;
  }
}

export function rememberPosition(coords: Coordinates, timestamp: number = Date.now()): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...coords, timestamp }));
  } catch {
    // Storage blocked — the fix just isn't remembered.
  }
}

export function forgetRememberedPosition(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nothing to clean up if storage is unavailable.
  }
}

export interface GetPositionOptions {
  /** Skip the remembered fix and go back to the browser. */
  forceFresh?: boolean;
  maxAgeMs?: number;
  timeoutMs?: number;
}

export function getCurrentPosition({
  forceFresh = false,
  maxAgeMs = REMEMBERED_MAX_AGE_MS,
  timeoutMs = 10000,
}: GetPositionOptions = {}): Promise<PositionResult> {
  if (!forceFresh) {
    const remembered = readRememberedPosition(maxAgeMs);
    if (remembered) return Promise.resolve({ ...remembered, remembered: true });
  }

  if (!navigator.geolocation) {
    return Promise.reject(new Error("Geolocation not supported"));
  }

  return new Promise((resolve, reject) => {
    navigator.geolocation.getCurrentPosition(
      (pos) => {
        const coords = {
          latitude: pos.coords.latitude,
          longitude: pos.coords.longitude,
        };
        const timestamp = Date.now();
        rememberPosition(coords, timestamp);
        resolve({ ...coords, remembered: false, timestamp });
      },
      (err) => reject(new Error(err.message || "Unable to get location")),
      {
        enableHighAccuracy: true,
        timeout: timeoutMs,
        // Lets the browser hand back its own recent fix instead of powering up GPS.
        maximumAge: forceFresh ? 0 : maxAgeMs,
      }
    );
  });
}
