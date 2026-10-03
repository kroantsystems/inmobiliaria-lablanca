import { Bath, BedDouble, Car, CheckCircle2, MapPin, Ruler, Trees } from "lucide-react";
import { notFound, permanentRedirect } from "next/navigation";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { SetAlternateSlugs } from "@/components/site/AlternateSlugs";
import { PropertyViewTracker } from "@/components/site/Analytics";
import { Breadcrumbs } from "@/components/site/Breadcrumbs";
import { CurrencyNotice, Price } from "@/components/site/CurrencyProvider";
import { LazyMap } from "@/components/site/LazyMap";
import { LeadForm } from "@/components/site/LeadForm";
import { PropertyGallery } from "@/components/site/PropertyGallery";
import { PropertyGrid } from "@/components/site/PropertyCard";
import { VideoEmbed } from "@/components/site/VideoEmbed";
import { WhatsAppLink } from "@/components/site/WhatsApp";
import { buttons } from "@/components/ui/buttons";
import { WhatsAppIcon } from "@/components/ui/Icons";
import { getPathname, Link } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { getProperty, getSettings, getSimilar, safely } from "@/lib/api/server";
import { formatNumber } from "@/lib/format/format";
import { leadInterest, placeOf, propertyHref, propertySummary } from "@/lib/property";
import { realEstateListing } from "@/lib/seo/jsonld";
import { buildMetadata, localizedUrl } from "@/lib/seo/metadata";

type Props = PageProps<"/[locale]/properties/[slug]">;

export const revalidate = 3600;

// Nenhum anúncio é gerado no build: cada um é renderizado no primeiro acesso e fica em cache até a API revalidar.
export async function generateStaticParams() {
  return [];
}

export async function generateMetadata({ params }: Props) {
  const { locale, slug } = (await params) as { locale: Locale; slug: string };
  const property = await getProperty(locale, slug);
  if (!property) return {};
  const title = property.seoTitle || property.title;
  return buildMetadata({
    locale,
    title,
    description: property.seoDescription || (await propertySummary(property, locale)),
    href: (target) => (property.slugs[target] ? propertyHref(property.slugs[target]) : null),
    image: { url: `/og/property/${locale}/${property.slug}`, width: 1200, height: 630, alt: title },
    type: "article",
  });
}

export default async function PropertyPage({ params }: Props) {
  const { locale, slug } = (await params) as { locale: Locale; slug: string };
  setRequestLocale(locale);
  const property = await getProperty(locale, slug);
  if (!property) notFound();
  if (property.slug !== slug) permanentRedirect(getPathname({ locale, href: propertyHref(property.slug) }));

  const [t, tn, tf, settings, similar, summary] = await Promise.all([
    getTranslations("property"),
    getTranslations("nav"),
    getTranslations("forms"),
    safely(getSettings(locale), null),
    safely(getSimilar(property.id, locale), []),
    propertySummary(property, locale),
  ]);
  const url = localizedUrl(locale, propertyHref(property.slug));
  const images = property.gallery.filter((item) => item.kind === "Image");
  const videos = property.gallery.filter((item) => item.kind === "Video");
  const located = property.latitude !== null && property.longitude !== null;

  const facts = [
    { icon: BedDouble, label: t("factBedrooms"), value: property.bedrooms },
    { icon: Bath, label: t("factBathrooms"), value: property.bathrooms },
    { icon: Ruler, label: t("factBuilt"), value: property.builtAreaM2 ? t("area", { value: formatNumber(property.builtAreaM2, locale) }) : null },
    { icon: Trees, label: t("factLot"), value: property.lotAreaM2 ? t("area", { value: formatNumber(property.lotAreaM2, locale) }) : null },
    { icon: Car, label: t("factParking"), value: property.parkingSpaces },
    { icon: MapPin, label: t("factZone"), value: placeOf(property) },
  ].filter((fact) => fact.value !== null && fact.value !== undefined && fact.value !== 0);

  const card = "rounded-[var(--radius-panel)] border border-lb-border bg-white p-6 shadow-[var(--shadow-card)]";
  const sectionTitle = "mb-4 text-xl font-extrabold";

  return (
    <>
      <JsonLd data={realEstateListing(property, url)} />
      <SetAlternateSlugs slugs={property.slugs} />
      <PropertyViewTracker propertyId={property.id} />

      <div className="bg-lb-slate text-white">
        <div className="mx-auto max-w-6xl px-4 pt-2 pb-5">
          <Breadcrumbs
            locale={locale}
            tone="dark"
            items={[
              { name: tn("properties"), href: "/properties" },
              ...(property.zone ? [{ name: property.zone.name, href: { pathname: "/zones/[zone]" as const, params: { zone: property.zone.slug } } }] : []),
              { name: property.title, href: propertyHref(property.slug) },
            ]}
          />
        </div>
      </div>

      <article className="mx-auto max-w-6xl px-4 pt-8">
        <header className="mb-6">
          <p className="flex flex-wrap items-center gap-2 text-xs font-extrabold text-lb-blue uppercase">
            <span className="rounded-lg bg-lb-blue px-2.5 py-1 text-white">{t(`operation.${property.operation}`)}</span>
            {property.status === "Reserved" ? <span className="rounded-lg bg-amber-400 px-2.5 py-1 text-lb-ink">{t("status.Reserved")}</span> : null}
            <span className="flex items-center gap-1">
              <MapPin className="size-3.5" aria-hidden />
              {placeOf(property)}
            </span>
          </p>
          <h1 className="mt-2 text-3xl font-extrabold sm:text-4xl">{property.title}</h1>
          <Price
            amount={property.price}
            currency={property.currency}
            perMonth={property.operation === "Rent"}
            className="mt-2 block text-3xl font-extrabold text-lb-blue"
          />
          <p className="mt-4 max-w-3xl text-lb-muted">{summary}</p>
        </header>

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_360px]">
          <div className="space-y-10">
            <PropertyGallery images={images} title={property.title} />

            {facts.length > 0 ? (
              <section aria-labelledby="facts-title">
                <h2 id="facts-title" className={sectionTitle}>
                  {t("facts")}
                </h2>
                <dl className="grid grid-cols-2 gap-3 sm:grid-cols-3">
                  {facts.map((fact) => (
                    <div key={fact.label} className="flex items-center gap-3 rounded-[var(--radius-card)] border border-lb-border bg-white p-4">
                      <fact.icon className="size-5 shrink-0 text-lb-blue" aria-hidden />
                      <div>
                        <dt className="text-xs font-bold text-lb-muted uppercase">{fact.label}</dt>
                        <dd className="font-bold">{fact.value}</dd>
                      </div>
                    </div>
                  ))}
                </dl>
              </section>
            ) : null}

            {property.description ? (
              <section aria-labelledby="description-title">
                <h2 id="description-title" className={sectionTitle}>
                  {t("description")}
                </h2>
                <p className="leading-relaxed whitespace-pre-line text-lb-ink/90">{property.description}</p>
              </section>
            ) : null}

            {property.features.length > 0 ? (
              <section aria-labelledby="features-title">
                <h2 id="features-title" className={sectionTitle}>
                  {t("features")}
                </h2>
                <ul className="grid gap-2 sm:grid-cols-2">
                  {property.features.map((feature) => (
                    <li key={feature} className="flex items-center gap-2 text-sm">
                      <CheckCircle2 className="size-4 shrink-0 text-lb-blue" aria-hidden />
                      {feature}
                    </li>
                  ))}
                </ul>
              </section>
            ) : null}

            {property.videoUrl || videos.length > 0 ? (
              <section aria-labelledby="video-title" className="space-y-4">
                <h2 id="video-title" className={sectionTitle}>
                  {t("video")}
                </h2>
                {property.videoUrl ? <VideoEmbed url={property.videoUrl} title={property.title} poster={images[0]?.url} /> : null}
                {videos.map((video) => (
                  <video key={video.url} controls preload="none" poster={images[0]?.url} className="w-full rounded-[var(--radius-card)] bg-lb-ink">
                    <source src={video.url} type={video.contentType} />
                    {t("uploadedVideo")}
                  </video>
                ))}
              </section>
            ) : null}

            {located ? (
              <section aria-labelledby="location-title">
                <h2 id="location-title" className={sectionTitle}>
                  {t("location")}
                </h2>
                <LazyMap properties={[{ ...property, cover: null, isFeatured: false }]} single className="h-[320px]" />
                <p className="mt-2 text-xs text-lb-muted">{t("locationNote")}</p>
              </section>
            ) : null}
          </div>

          <aside className="space-y-6 lg:sticky lg:top-6 lg:self-start">
            <div className={card}>
              <Price
                amount={property.price}
                currency={property.currency}
                perMonth={property.operation === "Rent"}
                className="block text-2xl font-extrabold text-lb-blue"
              />
              <CurrencyNotice className="mt-1 text-xs text-lb-muted" />
              <WhatsAppLink
                number={settings?.whatsappNumber ?? null}
                propertyId={property.id}
                message={(await getTranslations("whatsapp"))("propertyMessage", { title: property.title, url })}
                className={`${buttons.whatsapp} mt-4 w-full`}
              >
                <WhatsAppIcon className="size-5" />
                {t("whatsapp")}
              </WhatsAppLink>
            </div>

            <div id="visita" className={`${card} scroll-mt-6`}>
              <h2 className="text-xl font-extrabold">{tf("visitTitle")}</h2>
              <p className="mt-1 mb-4 text-sm text-lb-muted">{t("visitIntro")}</p>
              <LeadForm
                source="VisitRequest"
                propertyId={property.id}
                interest={leadInterest(property)}
                withMessage
                successMessage={tf("visitSuccess")}
                submitLabel={t("requestVisit")}
              />
            </div>
          </aside>
        </div>

        {similar.length > 0 ? (
          <section aria-labelledby="similar-title" className="mt-16">
            <h2 id="similar-title" className="mb-6 text-2xl font-extrabold">
              {t("similar")}
            </h2>
            <PropertyGrid properties={similar} locale={locale} />
          </section>
        ) : null}

        <p className="mt-10">
          <Link href="/properties" className="font-bold text-lb-blue hover:underline">
            ← {t("backToCatalog")}
          </Link>
        </p>
      </article>
    </>
  );
}
