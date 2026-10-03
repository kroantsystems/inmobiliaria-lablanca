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

// ---- Painel administrativo ----

export const LEAD_SOURCES: LeadSource[] = ["Contact", "VisitRequest", "OwnerProposal", "Newsletter", "Manual"];
export const VISIT_STATUSES: VisitStatus[] = ["Scheduled", "Done", "Cancelled", "NoShow"];
export const LISTED_STATUSES: PropertyStatus[] = ["Available", "Reserved"];

export type Kpi = { value: number; previous: number; changePercent: number | null };

export type DashboardSummary = {
  siteVisits: Kpi;
  propertyViews: Kpi;
  sales: Kpi;
  monthlySalesGoal: number;
  activeRentals: Kpi;
  newLeads: Kpi;
  monthStart: string;
};

export type TrafficPoint = { date: string; visits: number; propertyViews: number };
export type TopProperty = { id: string; title: string; views: number };

export type PropertyTranslation = { locale: string; title: string; slug: string; description: string | null; seoTitle: string | null; seoDescription: string | null };

export type AdminMedia = {
  id: string;
  kind: MediaKind;
  originalName: string;
  contentType: string;
  sizeBytes: number;
  url: string;
  publicUrl: string | null;
  isPublic: boolean;
  isCover: boolean;
  sortOrder: number;
  altText: string | null;
  description: string | null;
};

export type AdminProperty = {
  id: string;
  operation: PropertyOperation;
  type: PropertyType;
  status: PropertyStatus;
  isPublished: boolean;
  isFeatured: boolean;
  price: number;
  currency: CurrencyCode;
  zoneId: string;
  city: string;
  address: string | null;
  latitude: number | null;
  longitude: number | null;
  bedrooms: number | null;
  bathrooms: number | null;
  builtAreaM2: number | null;
  lotAreaM2: number | null;
  parkingSpaces: number | null;
  features: string[];
  videoUrl: string | null;
  ownerId: string | null;
  publishedAt: string | null;
  soldAt: string | null;
  rentedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
  translations: PropertyTranslation[];
  media: AdminMedia[];
};

export type AdminPropertyListItem = {
  id: string;
  title: string;
  slug: string;
  operation: PropertyOperation;
  type: PropertyType;
  status: PropertyStatus;
  isPublished: boolean;
  isFeatured: boolean;
  price: number;
  currency: CurrencyCode;
  zoneId: string;
  zoneName: string | null;
  city: string;
  bedrooms: number | null;
  bathrooms: number | null;
  builtAreaM2: number | null;
  coverUrl: string | null;
  mediaCount: number;
  ownerId: string | null;
  ownerName: string | null;
  publishedAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type PropertyInput = Omit<
  AdminProperty,
  "id" | "status" | "isPublished" | "isFeatured" | "publishedAt" | "soldAt" | "rentedAt" | "createdAt" | "updatedAt" | "translations" | "media"
> & { translations: { locale: string; title: string; description: string | null; seoTitle: string | null; seoDescription: string | null }[] };

export type Lead = {
  id: string;
  name: string;
  phone: string;
  email: string | null;
  interest: LeadInterest;
  source: LeadSource;
  status: LeadStatus;
  propertyId: string | null;
  propertyTitle: string | null;
  message: string | null;
  notes: string | null;
  locale: string;
  consentAt: string | null;
  createdAt: string;
  updatedAt: string | null;
};

export type Owner = {
  id: string;
  name: string;
  phone: string;
  email: string | null;
  document: string | null;
  notes: string | null;
  createdAt: string;
  updatedAt: string | null;
  properties: { id: string; title: string; status: PropertyStatus }[];
};

export type Visit = {
  id: string;
  propertyId: string;
  propertyTitle: string | null;
  leadId: string | null;
  clientName: string;
  startsAt: string;
  durationMinutes: number;
  endsAt: string;
  notes: string | null;
  status: VisitStatus;
};

export type FileItem = {
  id: string;
  originalName: string;
  contentType: string;
  kind: MediaKind;
  sizeBytes: number;
  description: string | null;
  altText: string | null;
  isPublic: boolean;
  isCover: boolean;
  propertyId: string | null;
  propertyTitle: string | null;
  uploadedAt: string;
  url: string;
  publicUrl: string | null;
};

export type AdminSettings = Omit<PublicSettings, "zones"> & { monthlySalesGoal: number; updatedAt: string | null };

export type ZoneTranslation = { locale: string; name: string; description: string | null };
export type AdminZone = { id: string; slug: string; city: string; sortOrder: number; translations: ZoneTranslation[]; propertyCount: number };
