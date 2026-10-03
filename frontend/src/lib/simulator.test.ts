import { describe, expect, it } from "vitest";
import { monthlyPayment } from "./simulator";

describe("monthlyPayment (Price table)", () => {
  it("matches the reference case of USD 100,000 over 10 years at 8%", () => {
    expect(Math.round(monthlyPayment(100_000, 8, 10)!)).toBe(1213);
  });

  it("splits evenly when the rate is zero", () => {
    expect(monthlyPayment(120_000, 0, 10)).toBe(1000);
  });

  it.each([0, -10, Number.NaN])("returns null for invalid value %s", (value) => {
    expect(monthlyPayment(value, 8, 10)).toBeNull();
  });
});
