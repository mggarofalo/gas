import { describe, it, expect } from "vitest";
import {
  digitsFromInput,
  digitsFromValue,
  formatMasked,
  valueFromDigits,
} from "@/lib/numberMask";

describe("formatMasked", () => {
  it("pads to the required decimals", () => {
    expect(formatMasked("3.4", 3)).toBe("3.400");
    expect(formatMasked("12.5", 2)).toBe("12.50");
    expect(formatMasked("", 2)).toBe("");
  });

  it("groups thousands", () => {
    expect(formatMasked("123456", 0)).toBe("123,456");
    expect(formatMasked("1234567.89", 2)).toBe("1,234,567.89");
    expect(formatMasked("999", 0)).toBe("999");
  });

  it("truncates decimals past the mask's precision", () => {
    expect(formatMasked("1.2345", 3)).toBe("1.234");
  });
});

describe("valueFromDigits", () => {
  it("fills digits in from the right", () => {
    expect(valueFromDigits("3", 3)).toBe("0.003");
    expect(valueFromDigits("34", 3)).toBe("0.034");
    expect(valueFromDigits("345", 3)).toBe("0.345");
    expect(valueFromDigits("3459", 3)).toBe("3.459");
    expect(valueFromDigits("12345", 2)).toBe("123.45");
    expect(valueFromDigits("123456", 0)).toBe("123456");
  });

  it("treats all-zero digits as empty so backspace clears the field", () => {
    expect(valueFromDigits("", 2)).toBe("");
    expect(valueFromDigits("0", 2)).toBe("");
    expect(valueFromDigits("000", 2)).toBe("");
  });

  it("caps runaway input", () => {
    expect(valueFromDigits("9".repeat(20), 2)).toBe("9999999999.99");
  });
});

describe("digitsFromValue", () => {
  it("round-trips a stored value back to digits", () => {
    expect(digitsFromValue("3.459", 3)).toBe("3459");
    expect(digitsFromValue("3", 3)).toBe("3000");
    expect(digitsFromValue("123.45", 2)).toBe("12345");
    expect(digitsFromValue("", 2)).toBe("");
  });
});

describe("digitsFromInput", () => {
  it("keeps only digits", () => {
    expect(digitsFromInput("1,234.56")).toBe("123456");
    expect(digitsFromInput("$12.30 usd")).toBe("1230");
    expect(digitsFromInput("abc")).toBe("");
  });
});
