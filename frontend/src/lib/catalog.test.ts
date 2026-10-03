import { describe, expect, it } from "vitest";
import { canonicalQuery, catalogHref, catalogQuery, parseCatalogParams, toApiParams } from "./catalog";

describe("parseCatalogParams", () => {
  it("keeps valid filters", () => {
    expect(
      parseCatalogParams({
        operation: "Rent",
        type: "Apartment",
        zone: "parana-country-club",
        minPrice: "500",
        maxPrice: "1500",
        minBedrooms: "2",
        q: "  piscina ",
        sort: "price_asc",
        page: "3",
      }),
    ).toEqual({
      operation: "Rent",
      type: "Apartment",
      zone: "parana-country-club",
      minPrice: 500,
      maxPrice: 1500,
      minBedrooms: 2,
      q: "piscina",
      sort: "price_asc",
      page: 3,
    });
  });

  it("drops invalid values instead of sending them to the API", () => {
    expect(
      parseCatalogParams({
        operation: "Swap",
        type: "Castle",
        zone: "../etc",
        minPrice: "-5",
        maxPrice: "abc",
        minBedrooms: "99",
        sort: "random",
        page: "0",
      }),
    ).toEqual({ page: 1 });
  });

  it("uses the first value of repeated params and limits the text", () => {
    const result = parseCatalogParams({ operation: ["Sale", "Rent"], q: "x".repeat(300) });
    expect(result.operation).toBe("Sale");
    expect(result.q).toHaveLength(100);
  });
});

describe("queries", () => {
  const filters = parseCatalogParams({ operation: "Sale", zone: "hernandarias", q: "pileta", sort: "price_desc", page: "2" });

  it("sends API names", () => {
    expect(toApiParams(filters)).toEqual({ operation: "Sale", zone: "hernandarias", q: "pileta", sort: "price_desc", page: "2" });
  });

  it("builds links that keep filters and replace sort/page", () => {
    expect(catalogQuery(filters, { page: 3 })).toEqual({ operation: "Sale", zone: "hernandarias", q: "pileta", sort: "price_desc", page: "3" });
    expect(catalogQuery(filters, { sort: undefined, page: 1 })).toEqual({ operation: "Sale", zone: "hernandarias", q: "pileta" });
  });

  it("canonical keeps only operation, type and zone", () => {
    expect(canonicalQuery(filters)).toEqual({ operation: "Sale", zone: "hernandarias" });
  });
});

describe("catalogHref", () => {
  it("uses the bare path when there are no filters (no trailing ?)", () => {
    expect(catalogHref({})).toBe("/properties");
    expect(catalogHref({ operation: "Rent" })).toEqual({ pathname: "/properties", query: { operation: "Rent" } });
  });
});
