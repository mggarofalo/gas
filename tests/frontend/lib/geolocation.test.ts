import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import {
  REMEMBERED_MAX_AGE_MS,
  forgetRememberedPosition,
  getCurrentPosition,
  readRememberedPosition,
  rememberPosition,
} from "@/lib/geolocation";

const STORAGE_KEY = "gas_last_position";

function mockGeolocation(impl: (success: PositionCallback, error: PositionErrorCallback, opts?: PositionOptions) => void) {
  const getCurrentPositionMock = vi.fn(impl);
  Object.defineProperty(navigator, "geolocation", {
    value: { getCurrentPosition: getCurrentPositionMock },
    configurable: true,
    writable: true,
  });
  return getCurrentPositionMock;
}

beforeEach(() => {
  localStorage.clear();
  vi.useFakeTimers();
  vi.setSystemTime(new Date("2026-08-19T12:00:00Z"));
});

afterEach(() => {
  vi.useRealTimers();
  vi.restoreAllMocks();
});

describe("remembered position storage", () => {
  it("returns null when nothing is stored", () => {
    expect(readRememberedPosition()).toBeNull();
  });

  it("round-trips a stored fix", () => {
    rememberPosition({ latitude: 42.1, longitude: -71.2 });
    expect(readRememberedPosition()).toEqual({
      latitude: 42.1,
      longitude: -71.2,
      timestamp: Date.now(),
    });
  });

  it("ignores a fix older than the max age", () => {
    rememberPosition({ latitude: 42.1, longitude: -71.2 }, Date.now() - REMEMBERED_MAX_AGE_MS - 1);
    expect(readRememberedPosition()).toBeNull();
  });

  it("ignores a fix from the future (clock changed)", () => {
    rememberPosition({ latitude: 42.1, longitude: -71.2 }, Date.now() + 60_000);
    expect(readRememberedPosition()).toBeNull();
  });

  it("ignores malformed storage", () => {
    localStorage.setItem(STORAGE_KEY, "not json");
    expect(readRememberedPosition()).toBeNull();

    localStorage.setItem(STORAGE_KEY, JSON.stringify({ latitude: "x", longitude: 1, timestamp: Date.now() }));
    expect(readRememberedPosition()).toBeNull();
  });

  it("forgets on request", () => {
    rememberPosition({ latitude: 1, longitude: 2 });
    forgetRememberedPosition();
    expect(readRememberedPosition()).toBeNull();
  });
});

describe("getCurrentPosition", () => {
  it("asks the browser and remembers the result when nothing is cached", async () => {
    const spy = mockGeolocation((success) =>
      success({ coords: { latitude: 42.5, longitude: -71.5 } } as GeolocationPosition)
    );

    const pos = await getCurrentPosition();

    expect(spy).toHaveBeenCalledTimes(1);
    expect(pos).toMatchObject({ latitude: 42.5, longitude: -71.5, remembered: false });
    expect(readRememberedPosition()).toMatchObject({ latitude: 42.5, longitude: -71.5 });
  });

  it("reuses a recent fix without prompting the browser again", async () => {
    const spy = mockGeolocation((success) =>
      success({ coords: { latitude: 42.5, longitude: -71.5 } } as GeolocationPosition)
    );

    await getCurrentPosition();
    spy.mockClear();

    const pos = await getCurrentPosition();
    expect(spy).not.toHaveBeenCalled();
    expect(pos).toMatchObject({ latitude: 42.5, longitude: -71.5, remembered: true });
  });

  it("goes back to the browser once the remembered fix expires", async () => {
    const spy = mockGeolocation((success) =>
      success({ coords: { latitude: 42.5, longitude: -71.5 } } as GeolocationPosition)
    );

    await getCurrentPosition();
    vi.setSystemTime(Date.now() + REMEMBERED_MAX_AGE_MS + 1);
    spy.mockClear();

    const pos = await getCurrentPosition();
    expect(spy).toHaveBeenCalledTimes(1);
    expect(pos.remembered).toBe(false);
  });

  it("forceFresh skips the cache and disables the browser's own cache", async () => {
    const spy = mockGeolocation((success) =>
      success({ coords: { latitude: 1, longitude: 2 } } as GeolocationPosition)
    );

    await getCurrentPosition();
    spy.mockClear();

    await getCurrentPosition({ forceFresh: true });
    expect(spy).toHaveBeenCalledTimes(1);
    expect(spy.mock.calls[0][2]).toMatchObject({ maximumAge: 0 });
  });

  it("rejects with the browser's message", async () => {
    mockGeolocation((_success, error) =>
      error({ code: 1, message: "User denied Geolocation" } as GeolocationPositionError)
    );

    await expect(getCurrentPosition()).rejects.toThrow("User denied Geolocation");
  });

  it("rejects when geolocation is unavailable", async () => {
    Object.defineProperty(navigator, "geolocation", {
      value: undefined,
      configurable: true,
      writable: true,
    });

    await expect(getCurrentPosition()).rejects.toThrow("Geolocation not supported");
  });
});
