import type { Locale } from "@/i18n/routing";
import { apiGet, CacheTags, safely } from "@/lib/api/server";
import type { PublicSettings, SitemapProperty } from "@/lib/api/types";
import { formatPrice } from "@/lib/format/format";
import { localizedUrl, type Href } from "@/lib/seo/metadata";
import { SITE_URL } from "@/lib/site";

export const revalidate = 3600;

const OPERATION = { Sale: "Venta", Rent: "Alquiler" } as const;
const TYPE = { House: "Casa", Apartment: "Departamento", Land: "Terreno", Commercial: "Comercial", Other: "Otro" } as const;

const link = (title: string, locale: Locale, href: Href, note?: string) => `- [${title}](${localizedUrl(locale, href)})${note ? `: ${note}` : ""}`;

/** Resumo do site para assistentes de IA (formato llms.txt), atualizado junto com anúncios e configurações. */
export async function GET() {
  const [settings, properties] = await Promise.all([
    safely(apiGet<PublicSettings>("/api/public/settings?locale=es", { tags: [CacheTags.settings, CacheTags.zones, CacheTags.llms], locale: "es" }), null),
    safely(apiGet<SitemapProperty[]>("/api/public/properties/sitemap", { tags: [CacheTags.llms, CacheTags.sitemap, CacheTags.properties] }), []),
  ]);
  const name = settings?.companyName ?? "Inmobiliaria La Blanca";

  const contacts = [
    settings?.whatsappNumber && `- WhatsApp: ${settings.whatsappNumber}`,
    settings?.phone && `- Teléfono: ${settings.phone}`,
    settings?.email && `- Correo: ${settings.email}`,
    settings?.address && `- Dirección: ${settings.address}${/ciudad del este/i.test(settings.address) ? "" : ", Ciudad del Este"}, Alto Paraná, Paraguay`,
    settings?.openingHours && `- Horario: ${settings.openingHours.replace(/\s+/g, " ")}`,
  ].filter(Boolean);

  const lines = [
    `# ${name}`,
    "",
    "> Inmobiliaria de Ciudad del Este (Alto Paraná, Paraguay) que vende y alquila casas, departamentos y terrenos en Ciudad del Este, Hernandarias y alrededores. Sitio en español, portugués, inglés y guaraní; los precios se publican en su moneda original (generalmente USD) con conversión aproximada a guaraníes (PYG) y reales (BRL).",
    "",
    ...(contacts.length > 0 ? ["## Contacto", "", ...contacts, ""] : []),
    "## Páginas principales",
    "",
    link("Inicio", "es", "/"),
    link("Inmuebles en venta y alquiler", "es", "/properties", "catálogo con filtros por operación, tipo, zona, precio y habitaciones"),
    link("Imóveis à venda e para alugar (português)", "pt", "/properties"),
    link("Properties for sale and rent (English)", "en", "/properties"),
    link("Nosotros", "es", "/about"),
    link("Contacto", "es", "/contact"),
    link("Preguntas frecuentes", "es", "/faq"),
    "",
  ];

  if (settings?.zones.length) {
    lines.push("## Zonas", "", ...settings.zones.map((zone) => link(zone.name, "es", { pathname: "/zones/[zone]", params: { zone: zone.slug } }, zone.city)), "");
  }

  lines.push("## Inmuebles publicados", "");
  if (properties.length === 0) lines.push("- No hay inmuebles publicados en este momento.");
  for (const property of properties) {
    const [locale, slug] = property.slugs.es ? (["es", property.slugs.es] as const) : (Object.entries(property.slugs)[0] as [Locale, string]);
    const title = property.titles[locale] ?? Object.values(property.titles)[0];
    const price = `${formatPrice(property.price, property.currency, "es")}${property.operation === "Rent" ? " / mes" : ""}`;
    lines.push(link(title, locale, { pathname: "/properties/[slug]", params: { slug } }, `${OPERATION[property.operation]} · ${TYPE[property.type]} · ${price} · ${property.city}`));
  }

  lines.push("", "## Optional", "", `- [Sitemap](${SITE_URL}/sitemap.xml)`, "");

  return new Response(lines.join("\n"), { headers: { "Content-Type": "text/markdown; charset=utf-8" } });
}
