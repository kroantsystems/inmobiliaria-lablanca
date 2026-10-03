import { Bath, BedDouble, MapPin, Ruler } from "lucide-react";
import Image from "next/image";
import { getTranslations } from "next-intl/server";
import { Link } from "@/i18n/navigation";
import type { PropertyCard as Card } from "@/lib/api/types";
import { formatNumber } from "@/lib/format/format";
import { Price } from "./CurrencyProvider";

export async function PropertyCard({ property, locale, priority = false }: { property: Card; locale: string; priority?: boolean }) {
  const t = await getTranslations("property");
  const area = property.builtAreaM2 ?? property.lotAreaM2;
  const href = { pathname: "/properties/[slug]" as const, params: { slug: property.slug } };

  return (
    <article className="group relative flex flex-col overflow-hidden rounded-[var(--radius-card)] border border-lb-border bg-white shadow-[var(--shadow-card)] transition hover:-translate-y-1 hover:border-lb-blue hover:shadow-[var(--shadow-raised)]">
      <div className="relative aspect-[4/3] bg-lb-panel">
        {property.cover ? (
          <Image
            src={property.cover.url}
            alt={property.cover.alt}
            fill
            sizes="(min-width: 1024px) 380px, (min-width: 640px) 50vw, 100vw"
            className="object-cover"
            priority={priority}
          />
        ) : null}
        <span className="absolute bottom-3 left-3 rounded-lg bg-lb-blue/90 px-2.5 py-1 text-xs font-bold text-white backdrop-blur">
          {area ? `${t("area", { value: formatNumber(area, locale) })} • ` : ""}
          {t(`operation.${property.operation}`)}
        </span>
        {property.status === "Reserved" ? (
          <span className="absolute top-3 left-3 rounded-full bg-amber-400 px-2.5 py-1 text-xs font-bold text-lb-ink">{t("status.Reserved")}</span>
        ) : null}
      </div>
      <div className="flex flex-1 flex-col p-5">
        <p className="flex items-center gap-1 text-xs font-extrabold uppercase text-lb-blue">
          <MapPin className="size-3.5" aria-hidden />
          {property.zone?.name ?? property.city}
        </p>
        <h3 className="mt-1 mb-3 text-lg font-bold leading-snug">
          <Link href={href} className="after:absolute after:inset-0">
            {property.title}
          </Link>
        </h3>
        <Price amount={property.price} currency={property.currency} perMonth={property.operation === "Rent"} className="text-xl font-extrabold text-lb-blue" />
        <ul className="mt-auto flex flex-wrap gap-4 border-t border-lb-border pt-3 text-sm text-lb-muted">
          {property.bedrooms ? (
            <li className="flex items-center gap-1">
              <BedDouble className="size-4" aria-hidden />
              {t("bedrooms", { count: property.bedrooms })}
            </li>
          ) : null}
          {property.bathrooms ? (
            <li className="flex items-center gap-1">
              <Bath className="size-4" aria-hidden />
              {t("bathrooms", { count: property.bathrooms })}
            </li>
          ) : null}
          {area ? (
            <li className="flex items-center gap-1">
              <Ruler className="size-4" aria-hidden />
              {t("area", { value: formatNumber(area, locale) })}
            </li>
          ) : null}
        </ul>
        <span
          aria-hidden
          className="mt-4 inline-flex w-full items-center justify-center rounded-full bg-lb-blue py-2.5 text-sm font-bold uppercase text-white shadow-md shadow-lb-blue/30 transition group-hover:bg-lb-blue-hover"
        >
          {t("details")}
        </span>
      </div>
    </article>
  );
}

export async function PropertyGrid({ properties, locale, prioritizeFirst = false }: { properties: Card[]; locale: string; prioritizeFirst?: boolean }) {
  return (
    <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
      {properties.map((property, index) => (
        <PropertyCard key={property.id} property={property} locale={locale} priority={prioritizeFirst && index === 0} />
      ))}
    </div>
  );
}
