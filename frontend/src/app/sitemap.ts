import type { MetadataRoute } from "next";
import type { Locale } from "@/i18n/routing";
import { getSettings, getSitemapProperties, safely } from "@/lib/api/server";
import { alternateUrls, type Href } from "@/lib/seo/metadata";
import { HREFLANG } from "@/lib/site";

export const revalidate = 3600;

type Entry = MetadataRoute.Sitemap[number];

/** Uma entrada por idioma, cada uma listando as alternativas (hreflang + x-default). */
function entries(href: Href | ((locale: Locale) => Href | null), options: Omit<Entry, "url" | "alternates"> = {}): Entry[] {
  const urls = alternateUrls(href);
  const languages: Record<string, string> = {};
  for (const [locale, url] of Object.entries(urls)) languages[HREFLANG[locale as Locale]] = url;
  if (urls.es) languages["x-default"] = urls.es;
  return Object.values(urls).map((url) => ({ url, alternates: { languages }, ...options }));
}

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const [settings, properties] = await Promise.all([safely(getSettings("es"), null), safely(getSitemapProperties(), [])]);

  const pages: [Href, number][] = [
    ["/", 1],
    ["/properties", 0.9],
    ["/about", 0.5],
    ["/contact", 0.5],
    ["/faq", 0.5],
    ["/privacy", 0.2],
  ];

  return [
    ...pages.flatMap(([href, priority]) => entries(href, { priority })),
    ...(settings?.zones ?? []).flatMap((zone) => entries({ pathname: "/zones/[zone]", params: { zone: zone.slug } }, { priority: 0.7 })),
    ...properties.flatMap((property) =>
      entries(
        (locale) => (property.slugs[locale] ? { pathname: "/properties/[slug]", params: { slug: property.slugs[locale] } } : null),
        { lastModified: property.lastModified, priority: 0.8 },
      ),
    ),
  ];
}
