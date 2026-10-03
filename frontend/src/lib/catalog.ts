import { PROPERTY_TYPES, type PropertyOperation, type PropertyType } from "@/lib/api/types";

export const CATALOG_SORTS = ["price_asc", "price_desc"] as const;
export type CatalogSort = (typeof CATALOG_SORTS)[number];

export type CatalogFilters = {
  operation?: PropertyOperation;
  type?: PropertyType;
  zone?: string;
  minPrice?: number;
  maxPrice?: number;
  minBedrooms?: number;
  q?: string;
  sort?: CatalogSort;
  page: number;
};

type RawParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined) => (Array.isArray(value) ? value[0] : value)?.trim() || undefined;

function positive(value: string | undefined, max: number): number | undefined {
  if (!value || !/^\d+(\.\d+)?$/.test(value)) return undefined;
  const number = Number(value);
  return number > 0 && number <= max ? number : undefined;
}

/** Lê os filtros da URL descartando valores inválidos (a API recusaria a busca inteira). */
export function parseCatalogParams(raw: RawParams): CatalogFilters {
  const operation = first(raw.operation);
  const type = first(raw.type);
  const zone = first(raw.zone);
  const sort = first(raw.sort);
  const page = positive(first(raw.page), 10_000);
  const filters: CatalogFilters = { page: page && Number.isInteger(page) ? page : 1 };

  if (operation === "Sale" || operation === "Rent") filters.operation = operation;
  if (type && (PROPERTY_TYPES as string[]).includes(type)) filters.type = type as PropertyType;
  if (zone && /^[a-z0-9-]{1,80}$/.test(zone)) filters.zone = zone;
  const minPrice = positive(first(raw.minPrice), 1e13);
  const maxPrice = positive(first(raw.maxPrice), 1e13);
  if (minPrice) filters.minPrice = minPrice;
  if (maxPrice) filters.maxPrice = maxPrice;
  const bedrooms = positive(first(raw.minBedrooms), 20);
  if (bedrooms && Number.isInteger(bedrooms)) filters.minBedrooms = bedrooms;
  const q = first(raw.q);
  if (q) filters.q = q.slice(0, 100);
  if (sort && (CATALOG_SORTS as readonly string[]).includes(sort)) filters.sort = sort as CatalogSort;
  return filters;
}

function compact(values: Record<string, string | number | undefined>): Record<string, string> {
  const result: Record<string, string> = {};
  for (const [key, value] of Object.entries(values)) if (value !== undefined && value !== "") result[key] = String(value);
  return result;
}

export function toApiParams(filters: CatalogFilters): Record<string, string> {
  const { page, ...rest } = filters;
  return compact({ ...rest, page: page > 1 ? page : undefined });
}

/** Query da URL do catálogo com os mesmos filtros, trocando ordenação/página. */
export function catalogQuery(filters: CatalogFilters, changes: Partial<Pick<CatalogFilters, "sort" | "page">> = {}): Record<string, string> {
  const merged = { ...filters, ...changes };
  const { page, ...rest } = merged;
  return compact({ ...rest, page: page && page > 1 ? page : undefined });
}

/** Link do catálogo sem "?" sobrando quando não há filtros. */
export function catalogHref(query: Record<string, string>) {
  return Object.keys(query).length > 0 ? { pathname: "/properties" as const, query } : ("/properties" as const);
}

/** A canônica ignora texto livre, preço, quartos, ordenação e página. */
export function canonicalQuery(filters: CatalogFilters): Record<string, string> {
  return compact({ operation: filters.operation, type: filters.type, zone: filters.zone });
}
