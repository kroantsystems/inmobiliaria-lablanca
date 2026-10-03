import { ChevronLeft, ChevronRight } from "lucide-react";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { CurrencyNotice } from "@/components/site/CurrencyProvider";
import { Breadcrumbs } from "@/components/site/Breadcrumbs";
import { EmptyState } from "@/components/site/EmptyState";
import { PropertyGrid } from "@/components/site/PropertyCard";
import { SearchBox } from "@/components/site/SearchBox";
import { WhatsAppLink } from "@/components/site/WhatsApp";
import { buttons } from "@/components/ui/buttons";
import { WhatsAppIcon } from "@/components/ui/Icons";
import { Link } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { getSettings, safely, searchProperties } from "@/lib/api/server";
import type { PublicSettings } from "@/lib/api/types";
import { canonicalQuery, catalogHref, catalogQuery, parseCatalogParams, toApiParams, type CatalogFilters, type CatalogSort } from "@/lib/catalog";
import { buildMetadata } from "@/lib/seo/metadata";

type Props = PageProps<"/[locale]/properties">;

async function catalogTitle(filters: CatalogFilters, settings: PublicSettings | null) {
  const t = await getTranslations("catalog");
  const heading = t("heading", { type: filters.type ?? "all", operation: filters.operation ?? "all" });
  const zone = settings?.zones.find((item) => item.slug === filters.zone);
  return zone ? t("inZone", { title: heading, zone: zone.name }) : heading;
}

export async function generateMetadata({ params, searchParams }: Props) {
  const { locale } = (await params) as { locale: Locale };
  const filters = parseCatalogParams(await searchParams);
  const [t, settings] = await Promise.all([getTranslations({ locale, namespace: "meta" }), safely(getSettings(locale), null)]);
  return buildMetadata({
    locale,
    title: await catalogTitle(filters, settings),
    description: t("catalogDescription"),
    href: catalogHref(canonicalQuery(filters)),
  });
}

function pageWindow(current: number, pages: number): number[] {
  const start = Math.max(1, Math.min(current - 2, pages - 4));
  return Array.from({ length: Math.min(5, pages) }, (_, index) => start + index);
}

export default async function CatalogPage({ params, searchParams }: Props) {
  const { locale } = (await params) as { locale: Locale };
  setRequestLocale(locale);
  const filters = parseCatalogParams(await searchParams);
  const t = await getTranslations("catalog");
  const tn = await getTranslations("nav");
  const [settings, result] = await Promise.all([safely(getSettings(locale), null), safely(searchProperties(locale, toApiParams(filters)), null)]);
  const title = await catalogTitle(filters, settings);
  const items = result?.items ?? [];
  const total = result?.total ?? 0;
  const pages = result ? Math.max(1, Math.ceil(result.total / result.pageSize)) : 1;
  const hasFilters = Object.keys(canonicalQuery(filters)).length > 0 || !!(filters.q || filters.minPrice || filters.maxPrice || filters.minBedrooms);

  const sorts: { value: CatalogSort | undefined; label: string }[] = [
    { value: undefined, label: t("sortRecent") },
    { value: "price_asc", label: t("sortPriceAsc") },
    { value: "price_desc", label: t("sortPriceDesc") },
  ];
  const pageLink = (page: number) => catalogHref(catalogQuery(filters, { page }));

  return (
    <>
      <section className="bg-gradient-to-b from-lb-slate to-lb-ink pb-16 text-white">
        <div className="mx-auto max-w-6xl px-4 pt-6">
          <Breadcrumbs locale={locale} tone="dark" items={[{ name: tn("properties"), href: "/properties" }]} />
          <h1 className="mt-4 text-3xl font-extrabold sm:text-4xl">{title}</h1>
          <p className="mt-2 text-sm text-white/80">
            {filters.q ? `${t("searchFor", { q: filters.q })} · ` : ""}
            {t("results", { count: total })}
          </p>
        </div>
      </section>

      <div className="relative z-20 mx-auto -mt-10 max-w-6xl px-4">
        <SearchBox
          key={JSON.stringify(filters)}
          zones={settings?.zones ?? []}
          showKeyword
          defaults={{
            operation: filters.operation,
            type: filters.type,
            zone: filters.zone,
            minPrice: filters.minPrice?.toString(),
            maxPrice: filters.maxPrice?.toString(),
            minBedrooms: filters.minBedrooms?.toString(),
            q: filters.q,
          }}
        />
      </div>

      <section aria-label={title} className="mx-auto mt-10 max-w-6xl px-4">
        {items.length > 0 ? (
          <>
            <div className="mb-6 flex flex-wrap items-center justify-between gap-3">
              <nav aria-label={t("sort")} className="flex flex-wrap items-center gap-2 text-sm">
                <span className="font-bold text-lb-muted">{t("sort")}:</span>
                {sorts.map((sort) => {
                  const active = filters.sort === sort.value;
                  return (
                    <Link
                      key={sort.label}
                      href={catalogHref(catalogQuery(filters, { sort: sort.value, page: 1 }))}
                      aria-current={active ? "true" : undefined}
                      className={`rounded-full px-3 py-1 font-semibold transition ${
                        active ? "bg-lb-blue text-white" : "border border-lb-border bg-white text-lb-ink hover:border-lb-blue"
                      }`}
                    >
                      {sort.label}
                    </Link>
                  );
                })}
              </nav>
              <CurrencyNotice className="text-xs text-lb-muted" />
            </div>

            <h2 className="sr-only">{t("results", { count: total })}</h2>
            <PropertyGrid properties={items} locale={locale} prioritizeFirst />

            {pages > 1 ? (
              <nav aria-label={t("page", { page: filters.page, pages })} className="mt-10 flex flex-wrap items-center justify-center gap-2 text-sm font-bold">
                {filters.page > 1 ? (
                  <Link href={pageLink(filters.page - 1)} rel="prev" className="inline-flex items-center gap-1 rounded-full border border-lb-border bg-white px-4 py-2 hover:border-lb-blue">
                    <ChevronLeft className="size-4" aria-hidden />
                    {t("previous")}
                  </Link>
                ) : null}
                {pageWindow(filters.page, pages).map((page) => (
                  <Link
                    key={page}
                    href={pageLink(page)}
                    aria-current={page === filters.page ? "page" : undefined}
                    className={`flex size-10 items-center justify-center rounded-full ${
                      page === filters.page ? "bg-lb-blue text-white" : "border border-lb-border bg-white hover:border-lb-blue"
                    }`}
                  >
                    {page}
                  </Link>
                ))}
                {filters.page < pages ? (
                  <Link href={pageLink(filters.page + 1)} rel="next" className="inline-flex items-center gap-1 rounded-full border border-lb-border bg-white px-4 py-2 hover:border-lb-blue">
                    {t("next")}
                    <ChevronRight className="size-4" aria-hidden />
                  </Link>
                ) : null}
              </nav>
            ) : null}
          </>
        ) : (
          <EmptyState title={t("emptyTitle")} text={t("emptyText")}>
            {hasFilters ? (
              <Link href="/properties" className={buttons.blue}>
                {t("clearFilters")}
              </Link>
            ) : null}
            <WhatsAppLink number={settings?.whatsappNumber ?? null} className={buttons.whatsapp}>
              <WhatsAppIcon className="size-5" />
              WhatsApp
            </WhatsAppLink>
          </EmptyState>
        )}
      </section>
    </>
  );
}
