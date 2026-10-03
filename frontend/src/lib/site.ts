import { locales } from "@/i18n/routing";

export const SITE_URL = (process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000").replace(/\/$/, "");

/** Código hreflang por idioma do site. */
export const HREFLANG: Record<(typeof locales)[number], string> = {
  es: "es-PY",
  pt: "pt-BR",
  en: "en",
  gn: "gn-PY",
};

/** Locale do Open Graph (formato ll_RR). */
export const OG_LOCALE: Record<(typeof locales)[number], string> = {
  es: "es_PY",
  pt: "pt_BR",
  en: "en_US",
  gn: "gn_PY",
};

export const absoluteUrl = (path: string) => `${SITE_URL}${path.startsWith("/") ? path : `/${path}`}`;
