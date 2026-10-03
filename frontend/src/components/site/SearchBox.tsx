"use client";

import { Search } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useId, useState } from "react";
import { useRouter } from "@/i18n/navigation";
import { PROPERTY_TYPES, type PropertyOperation, type PublicZone } from "@/lib/api/types";
import { formatPrice } from "@/lib/format/format";

const PRICE_RANGES: Record<PropertyOperation, [number, number | null][]> = {
  Sale: [
    [0, 100_000],
    [100_000, 300_000],
    [300_000, null],
  ],
  Rent: [
    [0, 500],
    [500, 1_500],
    [1_500, null],
  ],
};

export type SearchDefaults = { operation?: string; type?: string; zone?: string; minPrice?: string; maxPrice?: string; minBedrooms?: string; q?: string };

export function SearchBox({ zones, defaults = {}, showKeyword = false }: { zones: PublicZone[]; defaults?: SearchDefaults; showKeyword?: boolean }) {
  const t = useTranslations();
  const locale = useLocale();
  const router = useRouter();
  const id = useId();
  const [operation, setOperation] = useState<PropertyOperation>(defaults.operation === "Rent" ? "Rent" : "Sale");
  const [type, setType] = useState(defaults.type ?? "");
  const [zone, setZone] = useState(defaults.zone ?? "");
  const [price, setPrice] = useState(defaults.minPrice || defaults.maxPrice ? `${defaults.minPrice ?? 0}-${defaults.maxPrice ?? ""}` : "");
  const [bedrooms, setBedrooms] = useState(defaults.minBedrooms ?? "");
  const [q, setQ] = useState(defaults.q ?? "");

  const usd = (value: number) => formatPrice(value, "USD", locale);
  const rangeLabel = ([min, max]: [number, number | null]) =>
    min === 0 ? t("search.upTo", { value: usd(max!) }) : max === null ? t("search.over", { value: usd(min) }) : t("search.between", { min: usd(min), max: usd(max) });

  function submit(event: React.FormEvent) {
    event.preventDefault();
    const [minPrice, maxPrice] = price ? price.split("-") : [];
    const query: Record<string, string> = { operation };
    if (type) query.type = type;
    if (zone) query.zone = zone;
    if (minPrice && minPrice !== "0") query.minPrice = minPrice;
    if (maxPrice) query.maxPrice = maxPrice;
    if (bedrooms) query.minBedrooms = bedrooms;
    if (q.trim()) query.q = q.trim();
    router.push({ pathname: "/properties", query });
  }

  const field = "flex flex-col gap-1 border-lb-border md:border-r md:pr-3 [&:last-of-type]:border-0";
  const fieldLabel = "text-[0.7rem] font-extrabold uppercase text-lb-blue";
  const control = "w-full bg-transparent py-1 text-sm font-semibold text-lb-ink outline-none";

  return (
    <form onSubmit={submit} role="search" className="rounded-[var(--radius-panel)] border border-lb-border bg-white p-5 text-lb-ink shadow-[var(--shadow-raised)]">
      <div role="tablist" className="mb-4 flex gap-2 border-b border-lb-border pb-3">
        {(["Sale", "Rent"] as const).map((value) => (
          <button
            key={value}
            type="button"
            role="tab"
            aria-selected={operation === value}
            onClick={() => {
              setOperation(value);
              setPrice("");
            }}
            className={`rounded-full px-5 py-2 text-sm font-bold uppercase transition ${
              operation === value ? "bg-lb-blue text-white" : "bg-lb-bg text-lb-muted hover:text-lb-ink"
            }`}
          >
            {value === "Sale" ? t("search.buy") : t("search.rent")}
          </button>
        ))}
      </div>
      <div className={`grid gap-3 md:items-center ${showKeyword ? "md:grid-cols-[repeat(5,1fr)_auto]" : "md:grid-cols-[repeat(4,1fr)_auto]"}`}>
        <div className={field}>
          <label htmlFor={`${id}-type`} className={fieldLabel}>
            {t("search.type")}
          </label>
          <select id={`${id}-type`} value={type} onChange={(e) => setType(e.target.value)} className={control}>
            <option value="">{t("search.anyType")}</option>
            {PROPERTY_TYPES.map((value) => (
              <option key={value} value={value}>
                {t(`property.type.${value}`)}
              </option>
            ))}
          </select>
        </div>
        <div className={field}>
          <label htmlFor={`${id}-zone`} className={fieldLabel}>
            {t("search.zone")}
          </label>
          <select id={`${id}-zone`} value={zone} onChange={(e) => setZone(e.target.value)} className={control}>
            <option value="">{t("search.anyZone")}</option>
            {zones.map((item) => (
              <option key={item.slug} value={item.slug}>
                {item.name}
              </option>
            ))}
          </select>
        </div>
        <div className={field}>
          <label htmlFor={`${id}-price`} className={fieldLabel}>
            {t("search.price")}
          </label>
          <select id={`${id}-price`} value={price} onChange={(e) => setPrice(e.target.value)} className={control}>
            <option value="">{t("search.anyPrice")}</option>
            {PRICE_RANGES[operation].map((range) => (
              <option key={range.join("-")} value={`${range[0]}-${range[1] ?? ""}`}>
                {rangeLabel(range)}
              </option>
            ))}
          </select>
        </div>
        <div className={field}>
          <label htmlFor={`${id}-bedrooms`} className={fieldLabel}>
            {t("search.bedrooms")}
          </label>
          <select id={`${id}-bedrooms`} value={bedrooms} onChange={(e) => setBedrooms(e.target.value)} className={control}>
            <option value="">{t("search.anyBedrooms")}</option>
            {[1, 2, 3, 4].map((count) => (
              <option key={count} value={count}>
                {t("search.bedroomsMin", { count })}
              </option>
            ))}
          </select>
        </div>
        {showKeyword ? (
          <div className={field}>
            <label htmlFor={`${id}-q`} className={fieldLabel}>
              {t("search.text")}
            </label>
            <input id={`${id}-q`} value={q} onChange={(e) => setQ(e.target.value)} className={control} />
          </div>
        ) : null}
        <button type="submit" className="inline-flex h-full min-h-12 items-center justify-center gap-2 rounded-[var(--radius-card)] bg-lb-blue px-6 font-bold text-white transition hover:bg-lb-blue-hover">
          <Search className="size-4" aria-hidden />
          {t("search.submit")}
        </button>
      </div>
    </form>
  );
}
