import { describe, expect, it } from "vitest";
import { convertPrice, formatDate, formatPrice, intlLocale, type Rates } from "./format";

const rates: Rates = { pygPerUsd: 7500, brlPerUsd: 5 };

describe("intlLocale", () => {
  it.each([
    ["es", "es-PY"],
    ["pt", "pt-BR"],
    ["en", "en-US"],
    ["gn", "es-PY"],
  ])("%s -> %s", (locale, expected) => {
    expect(intlLocale(locale)).toBe(expected);
  });
});

describe("formatPrice", () => {
  it("formats guaranies with the ₲ symbol, dot thousands and no decimals", () => {
    expect(formatPrice(2_625_000_000, "PYG", "es")).toBe("₲ 2.625.000.000");
  });

  it("formats dollars per locale", () => {
    expect(formatPrice(350_000, "USD", "es")).toBe("USD 350.000");
    expect(formatPrice(350_000, "USD", "en")).toBe("USD 350,000");
  });

  it("formats reais in Portuguese", () => {
    expect(formatPrice(1_750_000, "BRL", "pt")).toBe("R$ 1.750.000");
  });

  it("uses Paraguayan formatting for Guarani", () => {
    expect(formatPrice(1234567, "PYG", "gn")).toBe("₲ 1.234.567");
  });
});

describe("convertPrice", () => {
  it("converts through the dollar", () => {
    expect(convertPrice(100_000, "USD", "PYG", rates)).toBe(750_000_000);
    expect(convertPrice(750_000_000, "PYG", "BRL", rates)).toBe(500_000);
    expect(convertPrice(500_000, "BRL", "USD", rates)).toBe(100_000);
    expect(convertPrice(42, "USD", "USD", rates)).toBe(42);
  });
});

describe("formatDate", () => {
  it("formats in the Asuncion time zone without failing for Guarani", () => {
    const date = new Date("2026-10-15T13:00:00Z");
    expect(() => formatDate(date, "gn", { dateStyle: "long" })).not.toThrow();
    expect(formatDate(date, "es", { hour: "2-digit", minute: "2-digit", hour12: false })).toBe("10:00");
  });
});
