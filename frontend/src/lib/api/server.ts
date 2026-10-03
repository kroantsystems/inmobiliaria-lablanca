import type { PagedResult, PropertyCard, PropertyDetail, PublicSettings, PublicZone, SitemapProperty } from "./types";

export const API_URL = process.env.API_INTERNAL_URL ?? "http://localhost:5080";

/** Tags iguais às de CacheTags na API, que dispara /revalidate após cada alteração. */
export const CacheTags = {
  properties: "properties",
  featured: "featured",
  sitemap: "sitemap",
  llms: "llms",
  settings: "settings",
  zones: "zones",
} as const;

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

type GetOptions = { tags: string[]; revalidate?: number; locale?: string };

/** GET de servidor com cache persistente por tags (invalidado pela API via revalidateTag). */
export async function apiGet<T>(path: string, { tags, revalidate = 3600, locale }: GetOptions): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    cache: "force-cache",
    next: { tags, revalidate },
    headers: { Accept: "application/json", ...(locale ? { "Accept-Language": locale } : {}) },
  });

  if (!response.ok) {
    throw new ApiError(response.status, `GET ${path} failed with ${response.status}`);
  }

  return (await response.json()) as T;
}

async function orNull<T>(request: Promise<T>): Promise<T | null> {
  try {
    return await request;
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null;
    throw error;
  }
}

const query = (params: Record<string, string | number | undefined | null>) => {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") search.set(key, String(value));
  }
  const text = search.toString();
  return text ? `?${text}` : "";
};

export const getSettings = (locale: string) =>
  apiGet<PublicSettings>(`/api/public/settings${query({ locale })}`, { tags: [CacheTags.settings, CacheTags.zones], locale });

export const getZones = (locale: string) =>
  apiGet<PublicZone[]>(`/api/public/zones${query({ locale })}`, { tags: [CacheTags.zones], locale });

export const getZone = (slug: string, locale: string) =>
  orNull(apiGet<PublicZone>(`/api/public/zones/${encodeURIComponent(slug)}${query({ locale })}`, { tags: [CacheTags.zones], locale }));

export const getFeatured = (locale: string, take = 6) =>
  apiGet<PropertyCard[]>(`/api/public/properties/featured${query({ locale, take })}`, { tags: [CacheTags.featured, CacheTags.properties], locale });

export type SearchParams = {
  operation?: string;
  type?: string;
  zone?: string;
  minPrice?: string;
  maxPrice?: string;
  minBedrooms?: string;
  q?: string;
  sort?: string;
  page?: string;
};

export const searchProperties = (locale: string, params: SearchParams) =>
  apiGet<PagedResult<PropertyCard>>(`/api/public/properties${query({ locale, ...params })}`, { tags: [CacheTags.properties], locale });

export const getProperty = (locale: string, slug: string) =>
  orNull(
    apiGet<PropertyDetail>(`/api/public/properties/${locale}/${encodeURIComponent(slug)}`, { tags: [CacheTags.properties], locale }),
  );

export const getSimilar = (id: string, locale: string) =>
  apiGet<PropertyCard[]>(`/api/public/properties/${id}/similar${query({ locale })}`, { tags: [CacheTags.properties], locale });

export const getMapPins = (locale: string) =>
  apiGet<PropertyCard[]>(`/api/public/properties/map${query({ locale })}`, { tags: [CacheTags.properties], locale });

export const getSitemapProperties = () =>
  apiGet<SitemapProperty[]>("/api/public/properties/sitemap", { tags: [CacheTags.sitemap, CacheTags.properties] });

/** Para páginas que devem continuar renderizando quando a API está fora (ex.: build sem API). */
export async function safely<T>(request: Promise<T>, fallback: T): Promise<T> {
  try {
    return await request;
  } catch (error) {
    console.error("[api]", error instanceof Error ? error.message : error);
    return fallback;
  }
}
