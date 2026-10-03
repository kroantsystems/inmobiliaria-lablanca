import type { Tone } from "@/components/admin/ui";
import type { LeadStatus, PropertyStatus, VisitStatus } from "@/lib/api/types";

export const PROPERTY_STATUS_TONE: Record<PropertyStatus, Tone> = {
  Draft: "gray",
  Available: "green",
  Reserved: "amber",
  Sold: "blue",
  Rented: "purple",
  Archived: "gray",
};

// Como no protótipo: "En negociación" verde, "Pendiente visita" âmbar.
export const LEAD_STATUS_TONE: Record<LeadStatus, Tone> = {
  New: "blue",
  Contacted: "gray",
  VisitScheduled: "amber",
  Negotiating: "green",
  Won: "purple",
  Lost: "red",
};

export const VISIT_STATUS_TONE: Record<VisitStatus, Tone> = {
  Scheduled: "blue",
  Done: "green",
  Cancelled: "red",
  NoShow: "amber",
};

/** Cor das etiquetas do calendário (fundo forte, texto branco). */
export const VISIT_STATUS_FILL: Record<VisitStatus, string> = {
  Scheduled: "bg-lb-blue",
  Done: "bg-green-700",
  Cancelled: "bg-lb-red line-through",
  NoShow: "bg-amber-700",
};
