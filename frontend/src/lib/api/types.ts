import type { CurrencyCode } from "@/lib/format/format";

export type PropertyOperation = "Sale" | "Rent";
export type PropertyType = "House" | "Apartment" | "Land" | "Commercial" | "Other";
export type PropertyStatus = "Draft" | "Available" | "Reserved" | "Sold" | "Rented" | "Archived";
export type MediaKind = "Image" | "Video" | "Document";
export type LeadSource = "Contact" | "VisitRequest" | "OwnerProposal" | "Newsletter" | "Manual";
export type LeadStatus = "New" | "Contacted" | "VisitScheduled" | "Negotiating" | "Won" | "Lost";
export type LeadInterest = "BuyHouse" | "RentApartment" | "BuyLand" | "Other";
export type VisitStatus = "Scheduled" | "Done" | "Cancelled" | "NoShow";
export type AnalyticsEventType = "PageView" | "PropertyView" | "WhatsAppClick" | "ContactClick";

export const PROPERTY_TYPES: PropertyType[] = ["House", "Apartment", "Land", "Commercial", "Other"];
export const PROPERTY_STATUSES: PropertyStatus[] = ["Draft", "Available", "Reserved", "Sold", "Rented", "Archived"];
export const LEAD_STATUSES: LeadStatus[] = ["New", "Contacted", "VisitScheduled", "Negotiating", "Won", "Lost"];
export const LEAD_INTERESTS: LeadInterest[] = ["BuyHouse", "RentApartment", "BuyLand", "Other"];

export type PagedResult<T> = { items: T[]; total: number; page: number; pageSize: number };

export type PublicZone = { id: string; slug: string; city: string; name: string; description: string | null; locale: string };

export type PublicSettings = {
  companyName: string;
  phone: string | null;
  whatsappNumber: string | null;
  email: string | null;
  address: string | null;
  officeLatitude: number | null;
  officeLongitude: number | null;
  openingHours: string | null;
  facebookUrl: string | null;
  instagramUrl: string | null;
  tiktokUrl: string | null;
  youtubeUrl: string | null;
  pygPerUsd: number;
  brlPerUsd: number;
  ratesUpdatedAt: string;
  simulatorAnnualRate: number;
  zones: PublicZone[];
};

export type PublicImage = { url: string; alt: string; kind: MediaKind; contentType: string };

export type PropertyCard = {
  id: string;
  slug: string;
  title: string;
  locale: string;
  operation: PropertyOperation;
  type: PropertyType;
  status: PropertyStatus;
  price: number;
  currency: CurrencyCode;
  zone: { slug: string; name: string } | null;
  city: string;
  bedrooms: number | null;
  bathrooms: number | null;
  builtAreaM2: number | null;
  lotAreaM2: number | null;
  latitude: number | null;
  longitude: number | null;
  cover: PublicImage | null;
  isFeatured: boolean;
  publishedAt: string | null;
};

export type PropertyDetail = Omit<PropertyCard, "cover" | "isFeatured"> & {
  description: string | null;
  seoTitle: string | null;
  seoDescription: string | null;
  parkingSpaces: number | null;
  features: string[];
  videoUrl: string | null;
  gallery: PublicImage[];
  slugs: Record<string, string>;
  updatedAt: string | null;
};

export type SitemapProperty = {
  id: string;
  slugs: Record<string, string>;
  titles: Record<string, string>;
  zoneSlug: string | null;
  operation: PropertyOperation;
  type: PropertyType;
  price: number;
  currency: CurrencyCode;
  city: string;
  lastModified: string;
};

export type AuthUser = { id: string; name: string; email: string; role: string; permissions: string[] };

export type AuthResponse = { accessToken: string; expiresIn: number; user: AuthUser };

export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
};
