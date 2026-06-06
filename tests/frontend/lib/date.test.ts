import { describe, it, expect, vi, afterEach } from "vitest";
import { parseDateOnlyLocal, formatDateOnly, todayLocalIso } from "@/lib/date";

describe("parseDateOnlyLocal", () => {
  it("returns a Date anchored at local midnight for YYYY-MM-DD", () => {
    const d = parseDateOnlyLocal("2026-06-06");
    expect(d.getFullYear()).toBe(2026);
    expect(d.getMonth()).toBe(5); // June (0-indexed)
    expect(d.getDate()).toBe(6);
    expect(d.getHours()).toBe(0);
    expect(d.getMinutes()).toBe(0);
  });

  it("ignores a time suffix", () => {
    const d = parseDateOnlyLocal("2026-06-06T15:30:00Z");
    expect(d.getFullYear()).toBe(2026);
    expect(d.getMonth()).toBe(5);
    expect(d.getDate()).toBe(6);
  });

  it("does NOT shift backward the way `new Date(YYYY-MM-DD)` does", () => {
    // Regression: `new Date("2026-06-06").toLocaleDateString()` parses as UTC
    // midnight and shifts to the prior day for any TZ west of UTC. Our helper
    // must not do that — the calendar day must round-trip.
    const d = parseDateOnlyLocal("2026-06-06");
    expect(d.getDate()).toBe(6);
  });
});

describe("formatDateOnly", () => {
  it("formats with default locale", () => {
    // We can't pin a locale here, but we can assert the date components in
    // the formatted output by also formatting a known local Date the same way.
    const expected = new Date(2026, 5, 6).toLocaleDateString();
    expect(formatDateOnly("2026-06-06")).toBe(expected);
  });

  it("passes locale and options through", () => {
    const result = formatDateOnly("2026-06-06", "en-US", {
      month: "short",
      day: "numeric",
    });
    expect(result).toBe("Jun 6");
  });
});

describe("todayLocalIso", () => {
  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns local YYYY-MM-DD even late in the evening west of UTC", () => {
    // 2026-06-06 23:30 local — would be 2026-06-07 in UTC for UTC-1 and further west.
    // Verify the helper uses local components, not UTC.
    const lateLocal = new Date(2026, 5, 6, 23, 30, 0);
    vi.useFakeTimers();
    vi.setSystemTime(lateLocal);
    expect(todayLocalIso()).toBe("2026-06-06");
  });

  it("zero-pads single-digit month and day", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date(2026, 0, 3, 12, 0, 0));
    expect(todayLocalIso()).toBe("2026-01-03");
  });
});
