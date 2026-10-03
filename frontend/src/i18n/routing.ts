import { defineRouting } from "next-intl/routing";

export const locales = ["es", "pt", "en", "gn"] as const;
export type Locale = (typeof locales)[number];

export const routing = defineRouting({
  locales,
  defaultLocale: "es",
  localePrefix: "always",
  // Caminhos traduzidos para SEO. Guarani usa os caminhos em espanhol.
  pathnames: {
    "/": "/",
    "/properties": { es: "/propiedades", pt: "/imoveis", en: "/properties", gn: "/propiedades" },
    "/properties/[slug]": {
      es: "/propiedades/[slug]",
      pt: "/imoveis/[slug]",
      en: "/properties/[slug]",
      gn: "/propiedades/[slug]",
    },
    "/zones/[zone]": { es: "/zonas/[zone]", pt: "/zonas/[zone]", en: "/zones/[zone]", gn: "/zonas/[zone]" },
    "/about": { es: "/nosotros", pt: "/sobre-nos", en: "/about", gn: "/nosotros" },
    "/contact": { es: "/contacto", pt: "/contato", en: "/contact", gn: "/contacto" },
    "/faq": { es: "/preguntas-frecuentes", pt: "/perguntas-frequentes", en: "/faq", gn: "/preguntas-frecuentes" },
    "/privacy": { es: "/privacidad", pt: "/privacidade", en: "/privacy", gn: "/privacidad" },
    "/admin": "/admin",
    "/admin/calendar": "/admin/calendar",
    "/admin/files": "/admin/files",
    "/admin/properties": "/admin/properties",
    "/admin/properties/[id]": "/admin/properties/[id]",
    "/admin/leads": "/admin/leads",
    "/admin/owners": "/admin/owners",
    "/admin/settings": "/admin/settings",
    "/admin/account": "/admin/account",
  },
});

export type AppPathname = keyof typeof routing.pathnames;
