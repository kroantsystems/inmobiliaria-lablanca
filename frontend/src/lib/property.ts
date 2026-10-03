import { getTranslations } from "next-intl/server";
import type { LeadInterest, PropertyCard, PropertyDetail } from "@/lib/api/types";
import { formatNumber, formatPrice } from "@/lib/format/format";

export const propertyHref = (slug: string) => ({ pathname: "/properties/[slug]" as const, params: { slug } });

/** "Zona, Cidade" sem repetir quando a zona tem o nome da cidade. */
export function placeOf(property: Pick<PropertyCard, "zone" | "city">): string {
  return property.zone && property.zone.name !== property.city ? `${property.zone.name}, ${property.city}` : property.city;
}

/** Frase factual (tipo, operação, local, preço, área, quartos) usada no início da página e na descrição. */
export async function propertySummary(property: PropertyDetail, locale: string): Promise<string> {
  const t = await getTranslations({ locale, namespace: "property" });
  return t("summary", {
    type: t(`type.${property.type}`),
    operation: property.operation,
    place: placeOf(property),
    price: formatPrice(property.price, property.currency, locale),
    area: property.builtAreaM2 ? formatNumber(property.builtAreaM2, locale) : "none",
    bedrooms: property.bedrooms ? String(property.bedrooms) : "none",
  });
}

export function leadInterest(property: Pick<PropertyCard, "operation" | "type">): LeadInterest {
  if (property.operation === "Sale" && property.type === "House") return "BuyHouse";
  if (property.operation === "Rent" && property.type === "Apartment") return "RentApartment";
  if (property.operation === "Sale" && property.type === "Land") return "BuyLand";
  return "Other";
}
