import { describe, expect, it } from "vitest";
import type { PropertyDetail, PublicSettings } from "@/lib/api/types";
import { breadcrumbList, faqPage, realEstateAgent, realEstateListing, serializeJsonLd, webSite } from "./jsonld";
import { clampText, pageTitle } from "./text";

const settings: PublicSettings = {
  companyName: "Inmobiliaria La Blanca",
  phone: "+595 61 500 000",
  whatsappNumber: "+595 981 000 000",
  email: "contacto@lablanca.com.py",
  address: "Av. San Blas 123",
  officeLatitude: -25.51,
  officeLongitude: -54.61,
  openingHours: "Mo-Fr 08:00-18:00",
  facebookUrl: "https://facebook.com/lablanca",
  instagramUrl: null,
  tiktokUrl: null,
  youtubeUrl: null,
  pygPerUsd: 7500,
  brlPerUsd: 5,
  ratesUpdatedAt: "2026-10-01T00:00:00Z",
  simulatorAnnualRate: 8,
  zones: [{ id: "z1", slug: "hernandarias", city: "Hernandarias", name: "Hernandarias", description: null, locale: "es" }],
};

const property: PropertyDetail = {
  id: "p1",
  slug: "casa-en-hernandarias",
  title: "Casa en Hernandarias",
  locale: "es",
  operation: "Sale",
  type: "House",
  status: "Available",
  price: 250000,
  currency: "USD",
  zone: { slug: "hernandarias", name: "Hernandarias" },
  city: "Hernandarias",
  bedrooms: 3,
  bathrooms: 2,
  builtAreaM2: 180,
  lotAreaM2: 450,
  latitude: -25.4,
  longitude: -54.63,
  publishedAt: "2026-09-01T12:00:00Z",
  description: "Casa amplia con jardín.",
  seoTitle: null,
  seoDescription: null,
  parkingSpaces: 2,
  features: ["Piscina"],
  videoUrl: null,
  gallery: [{ url: "/api/public/media/a.webp", alt: "Fachada", kind: "Image", contentType: "image/webp" }],
  slugs: { es: "casa-en-hernandarias" },
  updatedAt: "2026-09-10T12:00:00Z",
};

describe("clampText", () => {
  it("keeps short texts", () => {
    expect(clampText("Casa en venta", 60)).toBe("Casa en venta");
  });

  it("cuts on a word boundary and adds an ellipsis within the limit", () => {
    const result = clampText("Departamento moderno con vista al lago en el centro de Ciudad del Este", 40);
    expect(result.length).toBeLessThanOrEqual(40);
    expect(result.endsWith("…")).toBe(true);
    expect(result).toBe("Departamento moderno con vista al lago…");
  });

  it("collapses whitespace", () => {
    expect(clampText("  Casa \n en   venta ", 60)).toBe("Casa en venta");
  });
});

describe("pageTitle", () => {
  it("adds the site name when it fits in 60 characters", () => {
    expect(pageTitle("Contacto", "Inmobiliaria La Blanca")).toBe("Contacto | Inmobiliaria La Blanca");
  });

  it("drops the site name and clamps long titles", () => {
    const title = pageTitle("Residencia moderna con piscina y quincho en Paraná Country Club", "Inmobiliaria La Blanca");
    expect(title.length).toBeLessThanOrEqual(60);
    expect(title).not.toContain("La Blanca");
  });
});

describe("JSON-LD", () => {
  it("describes the agency with absolute logo, address, geo and social links", () => {
    const agent = realEstateAgent(settings, "es");
    expect(agent["@context"]).toBe("https://schema.org");
    expect(agent["@type"]).toBe("RealEstateAgent");
    expect(agent.logo).toMatch(/^https?:\/\/.+\/brand\/logo-light\.png$/);
    expect(agent.address).toMatchObject({ "@type": "PostalAddress", addressCountry: "PY", streetAddress: "Av. San Blas 123" });
    expect(agent.geo).toEqual({ "@type": "GeoCoordinates", latitude: -25.51, longitude: -54.61 });
    expect(agent.sameAs).toEqual(["https://facebook.com/lablanca"]);
    expect(agent.areaServed).toContain("Hernandarias");
  });

  it("works without settings", () => {
    const agent = realEstateAgent(null, "pt");
    expect(agent.name).toBe("Inmobiliaria La Blanca");
    expect(agent.geo).toBeUndefined();
  });

  it("declares the site search action", () => {
    const site = webSite("pt", "https://lablanca.com.py/pt/imoveis");
    expect(site["@type"]).toBe("WebSite");
    expect(site.inLanguage).toBe("pt-BR");
    expect(site.potentialAction).toMatchObject({
      "@type": "SearchAction",
      target: { urlTemplate: "https://lablanca.com.py/pt/imoveis?q={search_term_string}" },
      "query-input": "required name=search_term_string",
    });
  });

  it("describes a listing with offer, residence type, area and geo", () => {
    const listing = realEstateListing(property, "https://lablanca.com.py/es/propiedades/casa-en-hernandarias");
    expect(listing["@type"]).toBe("RealEstateListing");
    expect(listing.offers).toMatchObject({
      "@type": "Offer",
      price: 250000,
      priceCurrency: "USD",
      availability: "https://schema.org/InStock",
    });
    expect(listing.about).toMatchObject({
      "@type": "SingleFamilyResidence",
      numberOfBedrooms: 3,
      numberOfBathroomsTotal: 2,
      floorSize: { "@type": "QuantitativeValue", value: 180, unitCode: "MTK" },
      geo: { "@type": "GeoCoordinates", latitude: -25.4, longitude: -54.63 },
    });
    expect(listing.image).toEqual([expect.stringMatching(/^https?:\/\/.+\/api\/public\/media\/a\.webp$/)]);
  });

  it("maps apartments and land to schema types", () => {
    expect(realEstateListing({ ...property, type: "Apartment" }, "u").about["@type"]).toBe("Apartment");
    expect(realEstateListing({ ...property, type: "Land" }, "u").about["@type"]).toBe("Place");
  });

  it("lists breadcrumbs in order", () => {
    const crumbs = breadcrumbList([
      { name: "Inicio", url: "https://x/es" },
      { name: "Propiedades", url: "https://x/es/propiedades" },
    ]);
    expect(crumbs.itemListElement).toEqual([
      { "@type": "ListItem", position: 1, name: "Inicio", item: "https://x/es" },
      { "@type": "ListItem", position: 2, name: "Propiedades", item: "https://x/es/propiedades" },
    ]);
  });

  it("builds FAQ questions and answers", () => {
    const faq = faqPage([{ question: "¿Dónde?", answer: "En CDE." }]);
    expect(faq.mainEntity).toEqual([{ "@type": "Question", name: "¿Dónde?", acceptedAnswer: { "@type": "Answer", text: "En CDE." } }]);
  });

  it("escapes markup when serializing", () => {
    expect(serializeJsonLd({ name: "</script><script>alert(1)</script>" })).not.toContain("</script>");
  });
});
