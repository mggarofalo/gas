/**
 * The mask behind CurrencyInput: the user types digits only and they fill in
 * from the right, so with three decimals 3, 4, 5, 9 reads 0.003, 0.034, 0.345,
 * 3.459. Values are carried around as canonical strings ("3.459", "" when
 * blank) and only formatted with separators for display.
 */

/**
 * Cap on how many digits the mask will hold. Keeps a stuck key from producing
 * a number that overflows the display (and the backend's decimal columns).
 */
export const MAX_DIGITS = 12;

/** Drop leading zeros that carry no meaning, and clamp to MAX_DIGITS. */
function normalizeDigits(digits: string): string {
  return digits.replace(/^0+/, "").slice(0, MAX_DIGITS);
}

/** Keep only the digits of whatever was typed or pasted. */
export function digitsFromInput(raw: string): string {
  return raw.replace(/\D/g, "").slice(0, MAX_DIGITS);
}

/** Strip a canonical/partial numeric string down to the digits the mask holds. */
export function digitsFromValue(value: string, decimals: number): string {
  const [whole = "", fraction = ""] = value.replace(/[^\d.]/g, "").split(".");
  return normalizeDigits(whole + fraction.slice(0, decimals).padEnd(decimals, "0"));
}

/** Turn the mask's digits into the canonical value the form stores. */
export function valueFromDigits(digits: string, decimals: number): string {
  const trimmed = normalizeDigits(digits);
  // All-zero input reads as "nothing typed yet", so backspacing empties the field.
  if (trimmed === "") return "";
  const padded = trimmed.padStart(decimals + 1, "0");
  if (decimals === 0) return padded;
  const split = padded.length - decimals;
  return `${padded.slice(0, split)}.${padded.slice(split)}`;
}

/** Format a canonical value for display: grouped thousands, fixed decimals. */
export function formatMasked(value: string, decimals: number): string {
  const canonical = valueFromDigits(digitsFromValue(value, decimals), decimals);
  if (canonical === "") return "";
  const [whole, fraction] = canonical.split(".");
  const grouped = whole.replace(/\B(?=(\d{3})+(?!\d))/g, ",");
  return fraction ? `${grouped}.${fraction}` : grouped;
}
