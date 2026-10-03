import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { useClientTable } from "./useClientTable";
import { useDebounce } from "./useDebounce";

describe("useDebounce", () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it("updates only 300 ms after the last change", () => {
    const { result, rerender } = renderHook(({ value }) => useDebounce(value, 300), { initialProps: { value: "" } });

    for (const value of ["a", "au", "aur", "aura"]) {
      rerender({ value });
      act(() => vi.advanceTimersByTime(100));
    }

    expect(result.current).toBe("");
    act(() => vi.advanceTimersByTime(200));
    expect(result.current).toBe("aura");
  });
});

type Row = { id: number; title: string; price: number; status: string };

const rows: Row[] = [
  { id: 1, title: "Residencia Aura Light", price: 350_000, status: "Available" },
  { id: 2, title: "Sky Horizon Duplex", price: 2_200, status: "Draft" },
  { id: 3, title: "Botanical Sanctuary", price: 510_000, status: "Available" },
];

describe("useClientTable", () => {
  const options = {
    searchText: (row: Row) => [row.title],
    sorters: { price: (a: Row, b: Row) => a.price - b.price, title: (a: Row, b: Row) => a.title.localeCompare(b.title) },
  };

  it("filters by search term without accents and case", () => {
    const { result } = renderHook(() => useClientTable(rows, { ...options, search: "AURA" }));
    expect(result.current.items.map((r) => r.id)).toEqual([1]);
  });

  it("applies predicate filters", () => {
    const { result } = renderHook(() => useClientTable(rows, { ...options, filters: [(r) => r.status === "Available"] }));
    expect(result.current.items.map((r) => r.id)).toEqual([1, 3]);
  });

  it("toggles sort direction on the same column", () => {
    const { result } = renderHook(() => useClientTable(rows, options));

    act(() => result.current.toggleSort("price"));
    expect(result.current.items.map((r) => r.id)).toEqual([2, 1, 3]);

    act(() => result.current.toggleSort("price"));
    expect(result.current.items.map((r) => r.id)).toEqual([3, 1, 2]);
    expect(result.current.sort).toEqual({ key: "price", direction: "desc" });
  });

  it("does not recompute when inputs are unchanged", () => {
    const { result, rerender } = renderHook(() => useClientTable(rows, options));
    const first = result.current.items;
    rerender();
    expect(result.current.items).toBe(first);
  });
});
