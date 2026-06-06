/**
 * Parse a `YYYY-MM-DD` string as a local-time Date (midnight in the user's TZ).
 *
 * `new Date("YYYY-MM-DD")` per ECMA-262 parses as UTC midnight, which then
 * shifts backward by the local offset when rendered — west of UTC this shows
 * the prior day. Accepts an optional time suffix (`YYYY-MM-DDTHH:mm:ss…`)
 * and ignores it.
 */
export function parseDateOnlyLocal(value: string): Date {
  const [y, m, d] = value.split("T")[0].split("-").map(Number);
  return new Date(y, m - 1, d);
}

/**
 * Format a server-supplied `YYYY-MM-DD` string with `toLocaleDateString`,
 * interpreting it as a local date (never UTC).
 */
export function formatDateOnly(
  value: string,
  locales?: Intl.LocalesArgument,
  options?: Intl.DateTimeFormatOptions
): string {
  return parseDateOnlyLocal(value).toLocaleDateString(locales, options);
}

/** Today's date in the user's local timezone, formatted `YYYY-MM-DD`. */
export function todayLocalIso(): string {
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}
