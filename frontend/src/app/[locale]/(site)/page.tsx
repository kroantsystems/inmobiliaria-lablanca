import { Calculator, HouseIcon, MapPin, Search } from "lucide-react";
import Image from "next/image";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { BrandMark } from "@/components/brand/Logo";
import { JsonLd } from "@/components/seo/JsonLd";
import { Price } from "@/components/site/CurrencyProvider";
import { DialogButton } from "@/components/site/DialogButton";
import { EmptyState } from "@/components/site/EmptyState";
import { FaqList } from "@/components/site/FaqList";
import { LazyMap } from "@/components/site/LazyMap";
import { PropertyGrid } from "@/components/site/PropertyCard";
import { SearchBox } from "@/components/site/SearchBox";
import { Simulator } from "@/components/site/Simulator";
import { WhatsAppLink } from "@/components/site/WhatsApp";
import { buttons } from "@/components/ui/buttons";
import { WhatsAppIcon } from "@/components/ui/Icons";
import { Link } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { getFeatured, getMapPins, getSettings, safely, searchProperties } from "@/lib/api/server";
import type { PropertyCard } from "@/lib/api/types";
import { getFaqItems } from "@/lib/faq";
import { webSite } from "@/lib/seo/jsonld";
import { buildMetadata, localizedUrl } from "@/lib/seo/metadata";

export const revalidate = 3600;

export async function generateMetadata({ params }: PageProps<"/[locale]">) {
  const { locale } = (await params) as { locale: Locale };
  const t = await getTranslations({ locale, namespace: "meta" });
  return buildMetadata({ locale, title: t("homeTitle"), description: t("defaultDescription"), href: "/" });
}

function HighlightCard({ property, badge, fallback }: { property: PropertyCard | undefined; badge: string; fallback: string }) {
  const card = "relative block rounded-[var(--radius-panel)] border border-white/15 bg-white/8 p-4 text-white backdrop-blur-md";
  const badgeNode = (
    <span className="absolute top-7 right-7 z-[2] rounded-full bg-lb-red px-3.5 py-1.5 text-[0.7rem] font-extrabold uppercase">{badge}</span>
  );

  if (!property) {
    return (
      <div className={card}>
        <div className="flex h-52 items-center justify-center rounded-[var(--radius-card)] bg-gradient-to-br from-lb-blue to-lb-blue-dark">
          <BrandMark className="h-28 w-auto" />
        </div>
        <p className="mt-3 text-lg font-bold">{fallback}</p>
      </div>
    );
  }

  return (
    <Link href={{ pathname: "/properties/[slug]", params: { slug: property.slug } }} className={`${card} transition hover:border-white/40`}>
      {badgeNode}
      <div className="relative mb-3 h-52 overflow-hidden rounded-[var(--radius-card)] bg-lb-slate">
        {property.cover ? (
          <Image src={property.cover.url} alt={property.cover.alt} fill priority sizes="(min-width: 1024px) 440px, 100vw" className="object-cover" />
        ) : null}
      </div>
      <Price amount={property.price} currency={property.currency} perMonth={property.operation === "Rent"} className="block text-2xl font-extrabold" />
      <p className="font-bold">{property.title}</p>
      <p className="mt-0.5 flex items-center gap-1 text-xs text-white/80">
        <MapPin className="size-3.5" aria-hidden />
        {property.zone ? `${property.zone.name} / ${property.city}` : property.city}
      </p>
    </Link>
  );
}

export default async function HomePage({ params }: PageProps<"/[locale]">) {
  const { locale } = (await params) as { locale: Locale };
  setRequestLocale(locale);
  const t = await getTranslations("home");
  const tc = await getTranslations("common");

  const [settings, featured, pins, faq] = await Promise.all([
    safely(getSettings(locale), null),
    safely(getFeatured(locale), []),
    safely(getMapPins(locale), []),
    getFaqItems(locale),
  ]);
  // Sem destaques marcados, a vitrine mostra os anúncios mais recentes.
  const cards = featured.length > 0 ? featured : ((await safely(searchProperties(locale, {}), null))?.items.slice(0, 6) ?? []);
  const hero = cards[0];
  const aboutImage = (cards[1] ?? cards[0])?.cover;

  return (
    <>
      <JsonLd data={webSite(locale, localizedUrl(locale, "/properties"))} />

      <section className="bg-gradient-to-b from-lb-slate to-lb-ink pb-20 text-white">
        <div className="mx-auto grid max-w-6xl items-center gap-10 px-4 pt-10 md:pt-14 lg:grid-cols-[1.2fr_0.8fr]">
          <div>
            <h1 className="mb-4 text-3xl leading-[1.1] font-extrabold uppercase sm:text-4xl lg:text-[2.8rem]">{t("heroTitle")}</h1>
            <p className="mb-7 text-white/80">{t("heroSubtitle")}</p>
            <div className="flex flex-wrap gap-3">
              <Link href="/properties" className={buttons.red}>
                <Search className="size-4" aria-hidden />
                {t("seeProperties")}
              </Link>
              <a href="#simulador" className={buttons.blue}>
                <Calculator className="size-4" aria-hidden />
                {t("simulate")}
              </a>
              <WhatsAppLink number={settings?.whatsappNumber ?? null} className={buttons.whatsapp}>
                <WhatsAppIcon className="size-5" />
                {t("whatsappDoubts")}
              </WhatsAppLink>
            </div>
          </div>
          <HighlightCard property={hero} badge={t("featuredBadge")} fallback={t("featuredFallback")} />
        </div>
      </section>

      <div className="relative z-20 mx-auto -mt-10 max-w-6xl px-4">
        <SearchBox zones={settings?.zones ?? []} />
      </div>

      <section id="imoveis" aria-labelledby="featured-title" className="mx-auto mt-16 max-w-6xl scroll-mt-6 px-4">
        <div className="mb-8 flex flex-wrap items-end justify-between gap-4">
          <div>
            <p className="text-xs font-extrabold text-lb-blue uppercase">{t("catalogTag")}</p>
            <h2 id="featured-title" className="text-3xl font-extrabold">
              {t("featuredTitle")}
            </h2>
          </div>
          {cards.length > 0 ? (
            <Link href="/properties" className={buttons.small}>
              {tc("seeAll")}
            </Link>
          ) : null}
        </div>
        {cards.length > 0 ? (
          <PropertyGrid properties={cards} locale={locale} />
        ) : (
          <EmptyState title={t("emptyTitle")} text={t("emptyText")}>
            <DialogButton dialog="contact" className={buttons.red}>
              {t("emptyCta")}
            </DialogButton>
          </EmptyState>
        )}
      </section>

      {pins.length > 0 ? (
        <section id="mapa" aria-labelledby="map-title" className="mx-auto mt-16 max-w-6xl scroll-mt-6 px-4">
          <div className="mb-6 text-center">
            <p className="text-xs font-extrabold text-lb-blue uppercase">{t("mapTag")}</p>
            <h2 id="map-title" className="text-3xl font-extrabold">
              {t("mapTitle")}
            </h2>
          </div>
          <div className="relative">
            <p className="pointer-events-none absolute top-3 left-14 z-10 flex items-center gap-2 rounded-[var(--radius-card)] border border-white/15 bg-lb-ink/90 px-4 py-2.5 text-xs font-bold text-white shadow-[var(--shadow-raised)] backdrop-blur">
              <HouseIcon className="size-4 text-lb-red" aria-hidden />
              {t("mapLegend")}
            </p>
            <LazyMap properties={pins} className="h-[420px] md:h-[480px]" />
          </div>
        </section>
      ) : null}

      <section id="nosotros" aria-labelledby="about-title" className="mx-auto mt-16 max-w-6xl scroll-mt-6 px-4">
        <div className="grid items-center gap-10 rounded-[var(--radius-panel)] border border-white/20 bg-gradient-to-br from-lb-blue to-lb-blue-dark p-8 text-white shadow-[var(--shadow-raised)] md:grid-cols-2 md:p-10">
          <div>
            <p className="text-xs font-extrabold text-sky-100 uppercase">{t("aboutTag")}</p>
            <h2 id="about-title" className="mt-2 mb-4 text-3xl font-extrabold">
              {t("aboutTitle")}
            </h2>
            <p className="text-sm leading-relaxed text-white/85">{t("aboutText")}</p>
            <ul className="mt-6 flex flex-wrap gap-8">
              {([1, 2, 3] as const).map((n) => (
                <li key={n}>
                  <p className="font-display text-4xl font-bold [text-shadow:0_2px_4px_rgb(0_0_0/0.2)]">{t(`stat${n}Value`)}</p>
                  <p className="text-xs text-white/80">{t(`stat${n}Label`)}</p>
                </li>
              ))}
            </ul>
            <Link href="/about" className={`${buttons.ghostLight} mt-8`}>
              {t("aboutCta")}
            </Link>
          </div>
          <div className="relative h-64 overflow-hidden rounded-[var(--radius-card)] border border-white/20 md:h-80">
            {aboutImage ? (
              <Image src={aboutImage.url} alt={aboutImage.alt} fill sizes="(min-width: 768px) 520px, 100vw" className="object-cover" />
            ) : (
              <div className="flex h-full items-center justify-center bg-white/10">
                <BrandMark className="h-32 w-auto" />
              </div>
            )}
          </div>
        </div>
      </section>

      <Simulator annualRate={settings?.simulatorAnnualRate ?? 8} />

      <section id="preguntas" aria-labelledby="faq-title" className="mx-auto mt-16 max-w-3xl scroll-mt-6 px-4">
        <h2 id="faq-title" className="mb-6 text-center text-3xl font-extrabold">
          {t("faqTitle")}
        </h2>
        <FaqList items={faq.slice(0, 4)} />
        <p className="mt-5 text-center">
          <Link href="/faq" className="font-bold text-lb-blue hover:underline">
            {t("faqMore")}
          </Link>
        </p>
      </section>
    </>
  );
}
