"use client";

import { useCallback, useMemo, useState } from "react";

export type SortDirection = "asc" | "desc";

export type SortState<K extends string> = { key: K; direction: SortDirection } | null;

type Options<T, K extends string> = {
  /** Termo já com debounce. */
  search?: string;
  searchText: (item: T) => (string | null | undefined)[];
  filters?: ((item: T) => boolean)[];
  sorters: Record<K, (a: T, b: T) => number>;
  initialSort?: SortState<K>;
};

const NO_FILTERS: never[] = [];

export function normalizeText(value: string): string {
  return value.normalize("NFD").replace(/\p{Diacritic}/gu, "").toLowerCase().trim();
}

/** Busca, filtros e ordenação em memória sobre páginas grandes vindas da API (sem nova requisição). */
export function useClientTable<T, K extends string>(items: readonly T[], options: Options<T, K>) {
  const { search = "", searchText, filters = NO_FILTERS, sorters, initialSort = null } = options;
  const [sort, setSort] = useState<SortState<K>>(initialSort);

  const result = useMemo(() => {
    const term = normalizeText(search);
    let rows = items.filter((item) => filters.every((filter) => filter(item)));
    if (term) {
      rows = rows.filter((item) => searchText(item).some((text) => text && normalizeText(text).includes(term)));
    }

    if (sort) {
      const compare = sorters[sort.key];
      rows = [...rows].sort((a, b) => (sort.direction === "asc" ? compare(a, b) : compare(b, a)));
    }

    return rows;
  }, [items, search, searchText, filters, sorters, sort]);

  const toggleSort = useCallback((key: K) => {
    setSort((current) => (current?.key === key ? { key, direction: current.direction === "asc" ? "desc" : "asc" } : { key, direction: "asc" }));
  }, []);

  return { items: result, total: items.length, sort, toggleSort };
}
