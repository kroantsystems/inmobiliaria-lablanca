import type { Metadata } from "next";
import { getPathname } from "@/i18n/navigation";
import { locales, routing, type Locale } from "@/i18n/routing";
import { absoluteUrl, HREFLANG, OG_LOCALE } from "@/lib/site";
import { clampText, DESCRIPTION_MAX, pageTitle } from "./text";

export type Href = Parameters<typeof getPathname>[0]["href"];

type PageSeo = {
  locale: Locale;
  title: string;
  description: string;
  /** Rota da página; função quando o caminho muda por idioma (slug traduzido). Devolver null omite o idioma. */
  href: Href | ((locale: Locale) => Href | null);
  image?: { url: string; width: number; height: number; alt: string };
  type?: "website" | "article";
};

const SITE_NAME = "Inmobiliaria La Blanca";
const DEFAULT_IMAGE = { url: "/og", width: 1200, height: 630, alt: "Inmobiliaria La Blanca – Ciudad del Este" };

export function localizedUrl(locale: Locale, href: Href): string {
  return absoluteUrl(getPathname({ locale, href }));
}

/** URLs absolutas da mesma página em cada idioma disponível. */
export function alternateUrls(href: PageSeo["href"]): Partial<Record<Locale, string>> {
  const result: Partial<Record<Locale, string>> = {};
  for (const locale of locales) {
    const target = typeof href === "function" ? href(locale) : href;
    if (target) result[locale] = localizedUrl(locale, target);
  }
  return result;
}

/** Metadata completa de uma página pública: título ≤ 60, descrição ≤ 160, canônica, hreflang, Open Graph e Twitter. */
export function buildMetadata({ locale, title, description, href, image = DEFAULT_IMAGE, type = "website" }: PageSeo): Metadata {
  const urls = alternateUrls(href);
  const canonical = urls[locale] ?? Object.values(urls)[0];
  const finalTitle = pageTitle(title, SITE_NAME);
  const finalDescription = clampText(description, DESCRIPTION_MAX);
  const languages: Record<string, string> = {};
  for (const [code, url] of Object.entries(urls)) languages[HREFLANG[code as Locale]] = url;
  const xDefault = urls[routing.defaultLocale];
  if (xDefault) languages["x-default"] = xDefault;

  return {
    title: { absolute: finalTitle },
    description: finalDescription,
    alternates: { canonical, languages },
    openGraph: {
      type,
      url: canonical,
      siteName: SITE_NAME,
      title: finalTitle,
      description: finalDescription,
      locale: OG_LOCALE[locale],
      alternateLocale: Object.keys(urls)
        .filter((code) => code !== locale)
        .map((code) => OG_LOCALE[code as Locale]),
      images: [image],
    },
    twitter: { card: "summary_large_image", title: finalTitle, description: finalDescription, images: [image.url] },
  };
}
