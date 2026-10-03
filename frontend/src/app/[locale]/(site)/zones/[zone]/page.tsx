import { MapPin } from "lucide-react";
import { notFound } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { Breadcrumbs } from "@/components/site/Breadcrumbs";
import { DialogButton } from "@/components/site/DialogButton";
import { EmptyState } from "@/components/site/EmptyState";
import { FaqList } from "@/components/site/FaqList";
import { LazyMap } from "@/components/site/LazyMap";
import { PropertyGrid } from "@/components/site/PropertyCard";
import { buttons } from "@/components/ui/buttons";
import { Link } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { getSettings, getZone, safely, searchProperties } from "@/lib/api/server";
import { faqPage } from "@/lib/seo/jsonld";
import { buildMetadata } from "@/lib/seo/metadata";

type Props = PageProps<"/[locale]/zones/[zone]">;

export const revalidate = 3600;

export async function generateStaticParams() {
  return [];
}

const zoneHref = (slug: string) => ({ pathname: "/zones/[zone]" as const, params: { zone: slug } });

export async function generateMetadata({ params }: Props) {
  const { locale, zone: slug } = (await params) as { locale: Locale; zone: string };
  const zone = await getZone(slug, locale);
  if (!zone) return {};
  const [t, tm] = await Promise.all([getTranslations({ locale, namespace: "zone" }), getTranslations({ locale, namespace: "meta" })]);
  return buildMetadata({
    locale,
    title: t("title", { zone: zone.name }),
    description: tm("zoneDescription", { zone: zone.name, city: zone.city }),
    href: zoneHref(zone.slug),
  });
}

export default async function ZonePage({ params }: Props) {
  const { locale, zone: slug } = (await params) as { locale: Locale; zone: string };
  setRequestLocale(locale);
  const zone = await getZone(slug, locale);
  if (!zone) notFound();

  const [t, tn, th, settings, result] = await Promise.all([
    getTranslations("zone"),
    getTranslations("nav"),
    getTranslations("home"),
    safely(getSettings(locale), null),
    safely(searchProperties(locale, { zone: zone.slug }), null),
  ]);
  const items = result?.items ?? [];
  const count = result?.total ?? 0;
  const vars = { zone: zone.name, city: zone.city, count };
  const faq = (["where", "available", "visit"] as const).map((key) => ({ question: t(`faq.${key}.q`, vars), answer: t(`faq.${key}.a`, vars) }));
  const otherZones = (settings?.zones ?? []).filter((item) => item.slug !== zone.slug);

  return (
    <>
      <JsonLd data={faqPage(faq)} />

      <section className="bg-gradient-to-b from-lb-slate to-lb-ink pb-12 text-white">
        <div className="mx-auto max-w-6xl px-4 pt-6">
          <Breadcrumbs
            locale={locale}
            tone="dark"
            items={[
              { name: tn("properties"), href: "/properties" },
              { name: zone.name, href: zoneHref(zone.slug) },
            ]}
          />
          <p className="mt-6 flex items-center gap-1 text-xs font-extrabold text-sky-100 uppercase">
            <MapPin className="size-3.5" aria-hidden />
            {t("tag")} · {zone.city}
          </p>
          <h1 className="mt-1 text-3xl font-extrabold sm:text-4xl">{t("title", { zone: zone.name })}</h1>
          <p className="mt-3 max-w-3xl text-white/85">{t("summary", vars)}</p>
          {zone.description ? <p className="mt-3 max-w-3xl text-sm leading-relaxed whitespace-pre-line text-white/75">{zone.description}</p> : null}
        </div>
      </section>

      <section aria-labelledby="zone-list" className="mx-auto mt-10 max-w-6xl px-4">
        <div className="mb-6 flex flex-wrap items-end justify-between gap-4">
          <h2 id="zone-list" className="text-2xl font-extrabold">
            {t("listTitle")}
          </h2>
          {count > items.length ? (
            <Link href={{ pathname: "/properties", query: { zone: zone.slug } }} className={buttons.small}>
              {th("seeProperties")}
            </Link>
          ) : null}
        </div>
        {items.length > 0 ? (
          <PropertyGrid properties={items} locale={locale} prioritizeFirst />
        ) : (
          <EmptyState title={th("emptyTitle")} text={t("emptyText", { zone: zone.name })}>
            <DialogButton dialog="contact" className={buttons.red}>
              {th("emptyCta")}
            </DialogButton>
          </EmptyState>
        )}
      </section>

      {items.some((item) => item.latitude !== null) ? (
        <section aria-labelledby="zone-map" className="mx-auto mt-14 max-w-6xl px-4">
          <h2 id="zone-map" className="mb-6 text-2xl font-extrabold">
            {t("mapTitle", { zone: zone.name })}
          </h2>
          <LazyMap properties={items} />
        </section>
      ) : null}

      <section aria-labelledby="zone-faq" className="mx-auto mt-14 max-w-3xl px-4">
        <h2 id="zone-faq" className="mb-6 text-2xl font-extrabold">
          {t("faqTitle", { zone: zone.name })}
        </h2>
        <FaqList items={faq} />
      </section>

      {otherZones.length > 0 ? (
        <nav aria-labelledby="other-zones" className="mx-auto mt-14 max-w-6xl px-4">
          <h2 id="other-zones" className="mb-4 text-lg font-extrabold">
            {t("otherZones")}
          </h2>
          <ul className="flex flex-wrap gap-2">
            {otherZones.map((item) => (
              <li key={item.slug}>
                <Link
                  href={zoneHref(item.slug)}
                  className="inline-block rounded-full border border-lb-border bg-white px-4 py-2 text-sm font-semibold transition hover:border-lb-blue hover:text-lb-blue"
                >
                  {item.name}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      ) : null}
    </>
  );
}
