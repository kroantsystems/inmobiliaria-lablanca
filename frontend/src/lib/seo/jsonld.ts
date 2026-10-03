import type { Locale } from "@/i18n/routing";
import type { PropertyDetail, PropertyType, PublicSettings } from "@/lib/api/types";
import { absoluteUrl, HREFLANG, SITE_URL } from "@/lib/site";

const CONTEXT = "https://schema.org";
const SITE_NAME = "Inmobiliaria La Blanca";
export const ORGANIZATION_ID = `${SITE_URL}/#organization`;

const geo = (latitude: number | null | undefined, longitude: number | null | undefined) =>
  latitude != null && longitude != null ? { "@type": "GeoCoordinates", latitude, longitude } : undefined;

export function realEstateAgent(settings: PublicSettings | null, locale: Locale) {
  const sameAs = [settings?.facebookUrl, settings?.instagramUrl, settings?.tiktokUrl, settings?.youtubeUrl].filter((url): url is string => !!url);
  const logo = absoluteUrl("/brand/logo-light.png");
  return {
    "@context": CONTEXT,
    "@type": "RealEstateAgent",
    "@id": ORGANIZATION_ID,
    name: settings?.companyName ?? SITE_NAME,
    url: `${SITE_URL}/${locale}`,
    logo,
    image: logo,
    telephone: settings?.phone ?? settings?.whatsappNumber ?? undefined,
    email: settings?.email ?? undefined,
    address: {
      "@type": "PostalAddress",
      streetAddress: settings?.address ?? undefined,
      addressLocality: "Ciudad del Este",
      addressRegion: "Alto Paraná",
      addressCountry: "PY",
    },
    geo: geo(settings?.officeLatitude, settings?.officeLongitude),
    openingHours: settings?.openingHours ?? undefined,
    areaServed: [...new Set(["Ciudad del Este", "Hernandarias", "Alto Paraná", ...(settings?.zones ?? []).map((zone) => zone.name)])],
    sameAs: sameAs.length > 0 ? sameAs : undefined,
  };
}

export function webSite(locale: Locale, catalogUrl: string) {
  return {
    "@context": CONTEXT,
    "@type": "WebSite",
    "@id": `${SITE_URL}/#website`,
    url: `${SITE_URL}/${locale}`,
    name: SITE_NAME,
    inLanguage: HREFLANG[locale],
    publisher: { "@id": ORGANIZATION_ID },
    potentialAction: {
      "@type": "SearchAction",
      target: { "@type": "EntryPoint", urlTemplate: `${catalogUrl}?q={search_term_string}` },
      "query-input": "required name=search_term_string",
    },
  };
}

const RESIDENCE_TYPE: Record<PropertyType, string> = {
  House: "SingleFamilyResidence",
  Apartment: "Apartment",
  Land: "Place",
  Commercial: "Place",
  Other: "Place",
};

export function realEstateListing(property: PropertyDetail, url: string) {
  const area = property.builtAreaM2 ?? property.lotAreaM2;
  const images = property.gallery.filter((item) => item.kind === "Image").map((item) => absoluteUrl(item.url));
  return {
    "@context": CONTEXT,
    "@type": "RealEstateListing",
    "@id": `${url}#listing`,
    url,
    name: property.title,
    description: property.seoDescription ?? property.description ?? undefined,
    inLanguage: HREFLANG[property.locale as Locale] ?? property.locale,
    datePosted: property.publishedAt ?? undefined,
    dateModified: property.updatedAt ?? undefined,
    image: images.length > 0 ? images : undefined,
    offers: {
      "@type": "Offer",
      price: property.price,
      priceCurrency: property.currency,
      availability: property.status === "Reserved" ? "https://schema.org/LimitedAvailability" : "https://schema.org/InStock",
      businessFunction: property.operation === "Rent" ? "http://purl.org/goodrelations/v1#LeaseOut" : "http://purl.org/goodrelations/v1#Sell",
      offeredBy: { "@id": ORGANIZATION_ID },
    },
    about: {
      "@type": RESIDENCE_TYPE[property.type],
      name: property.title,
      numberOfBedrooms: property.bedrooms ?? undefined,
      numberOfBathroomsTotal: property.bathrooms ?? undefined,
      floorSize: area ? { "@type": "QuantitativeValue", value: area, unitCode: "MTK" } : undefined,
      address: {
        "@type": "PostalAddress",
        addressLocality: property.city,
        addressRegion: "Alto Paraná",
        addressCountry: "PY",
      },
      geo: geo(property.latitude, property.longitude),
      amenityFeature:
        property.features.length > 0
          ? property.features.map((name) => ({ "@type": "LocationFeatureSpecification", name, value: true }))
          : undefined,
    },
  };
}

export function breadcrumbList(items: { name: string; url: string }[]) {
  return {
    "@context": CONTEXT,
    "@type": "BreadcrumbList",
    itemListElement: items.map((item, index) => ({ "@type": "ListItem", position: index + 1, name: item.name, item: item.url })),
  };
}

export function faqPage(items: { question: string; answer: string }[]) {
  return {
    "@context": CONTEXT,
    "@type": "FAQPage",
    mainEntity: items.map((item) => ({ "@type": "Question", name: item.question, acceptedAnswer: { "@type": "Answer", text: item.answer } })),
  };
}

/** JSON para `<script type="application/ld+json">` sem permitir fechar a tag. */
export function serializeJsonLd(data: unknown): string {
  return JSON.stringify(data).replace(/</g, "\\u003c");
}
