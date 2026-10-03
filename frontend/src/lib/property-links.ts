import type { PropertyCard } from "@/lib/api/types";

export const propertyHref = (slug: string) => ({ pathname: "/properties/[slug]" as const, params: { slug } });

/** "Zona, Cidade" sem repetir quando a zona tem o nome da cidade. */
export function placeOf(property: Pick<PropertyCard, "zone" | "city">): string {
  return property.zone && property.zone.name !== property.city ? `${property.zone.name}, ${property.city}` : property.city;
}
