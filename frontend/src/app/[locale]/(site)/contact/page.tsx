import { Clock, Mail, MapPin, Navigation, Phone } from "lucide-react";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { LazyMap } from "@/components/site/LazyMap";
import { LeadForm } from "@/components/site/LeadForm";
import { PageHero } from "@/components/site/PageHero";
import { WhatsAppLink } from "@/components/site/WhatsApp";
import { WhatsAppIcon } from "@/components/ui/Icons";
import type { Locale } from "@/i18n/routing";
import { getSettings, safely } from "@/lib/api/server";
import type { PropertyCard, PublicSettings } from "@/lib/api/types";
import { buildMetadata } from "@/lib/seo/metadata";

export async function generateMetadata({ params }: PageProps<"/[locale]/contact">) {
  const { locale } = (await params) as { locale: Locale };
  const [t, tm] = await Promise.all([getTranslations({ locale, namespace: "contact" }), getTranslations({ locale, namespace: "meta" })]);
  return buildMetadata({ locale, title: t("title"), description: tm("contactDescription"), href: "/contact" });
}

/** O mapa do site recebe cartões de imóvel; aqui o único marcador é o escritório. */
function officePin(settings: PublicSettings): PropertyCard {
  return {
    id: "office",
    slug: "",
    title: settings.companyName,
    locale: "es",
    operation: "Sale",
    type: "Commercial",
    status: "Available",
    price: 0,
    currency: "USD",
    zone: null,
    city: "Ciudad del Este",
    bedrooms: null,
    bathrooms: null,
    builtAreaM2: null,
    lotAreaM2: null,
    latitude: settings.officeLatitude,
    longitude: settings.officeLongitude,
    cover: null,
    isFeatured: false,
    publishedAt: null,
  };
}

export default async function ContactPage({ params }: PageProps<"/[locale]/contact">) {
  const { locale } = (await params) as { locale: Locale };
  setRequestLocale(locale);
  const [t, tf, settings] = await Promise.all([getTranslations("contact"), getTranslations("forms"), safely(getSettings(locale), null)]);
  const hasOffice = settings?.officeLatitude != null && settings.officeLongitude != null;
  const socials = [
    ["Facebook", settings?.facebookUrl],
    ["Instagram", settings?.instagramUrl],
    ["TikTok", settings?.tiktokUrl],
    ["YouTube", settings?.youtubeUrl],
  ].filter((entry): entry is [string, string] => !!entry[1]);
  const hasChannels = !!(settings?.phone || settings?.whatsappNumber || settings?.email || settings?.address);

  const row = "flex items-start gap-3";
  const icon = "mt-0.5 size-5 shrink-0 text-lb-red";
  const label = "text-xs font-bold text-lb-muted uppercase";
  const link = "font-semibold text-lb-ink hover:text-lb-blue";

  return (
    <>
      <PageHero locale={locale} crumbs={[{ name: t("title"), href: "/contact" }]} title={t("title")} lead={t("lead")} />

      <div className="mx-auto mt-12 grid max-w-6xl gap-8 px-4 lg:grid-cols-[1fr_420px]">
        <section aria-labelledby="channels-title" className="space-y-8">
          <div className="rounded-[var(--radius-panel)] border border-lb-border bg-white p-6 shadow-[var(--shadow-card)]">
            <h2 id="channels-title" className="mb-5 text-xl font-extrabold">
              {t("channels")}
            </h2>
            {hasChannels && settings ? (
              <ul className="space-y-4">
                {settings.whatsappNumber ? (
                  <li className={row}>
                    <WhatsAppIcon className={`${icon} text-[#0f7a41]`} />
                    <div>
                      <p className={label}>{t("whatsapp")}</p>
                      <WhatsAppLink number={settings.whatsappNumber} className={link}>
                        {settings.whatsappNumber}
                      </WhatsAppLink>
                    </div>
                  </li>
                ) : null}
                {settings.phone ? (
                  <li className={row}>
                    <Phone className={icon} aria-hidden />
                    <div>
                      <p className={label}>{t("phone")}</p>
                      <a href={`tel:${settings.phone.replace(/[^\d+]/g, "")}`} className={link}>
                        {settings.phone}
                      </a>
                    </div>
                  </li>
                ) : null}
                {settings.email ? (
                  <li className={row}>
                    <Mail className={icon} aria-hidden />
                    <div>
                      <p className={label}>{t("email")}</p>
                      <a href={`mailto:${settings.email}`} className={link}>
                        {settings.email}
                      </a>
                    </div>
                  </li>
                ) : null}
                {settings.address ? (
                  <li className={row}>
                    <MapPin className={icon} aria-hidden />
                    <div>
                      <p className={label}>{t("address")}</p>
                      <address className="font-semibold not-italic">{settings.address}</address>
                    </div>
                  </li>
                ) : null}
                {settings.openingHours ? (
                  <li className={row}>
                    <Clock className={icon} aria-hidden />
                    <div>
                      <p className={label}>{t("hours")}</p>
                      <p className="font-semibold whitespace-pre-line">{settings.openingHours}</p>
                    </div>
                  </li>
                ) : null}
              </ul>
            ) : (
              <p className="text-sm text-lb-muted">{t("noChannels")}</p>
            )}
            {socials.length > 0 ? (
              <div className="mt-6 border-t border-lb-border pt-4">
                <p className={label}>{t("social")}</p>
                <ul className="mt-2 flex flex-wrap gap-2">
                  {socials.map(([name, url]) => (
                    <li key={name}>
                      <a
                        href={url}
                        target="_blank"
                        rel="noopener noreferrer"
                        className="inline-block rounded-full border border-lb-border px-4 py-1.5 text-sm font-semibold hover:border-lb-blue hover:text-lb-blue"
                      >
                        {name}
                      </a>
                    </li>
                  ))}
                </ul>
              </div>
            ) : null}
          </div>

          {hasOffice && settings ? (
            <div>
              <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
                <h2 className="text-xl font-extrabold">{t("mapTitle")}</h2>
                <a
                  href={`https://www.google.com/maps/dir/?api=1&destination=${settings.officeLatitude},${settings.officeLongitude}`}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="inline-flex items-center gap-1.5 text-sm font-bold text-lb-blue hover:underline"
                >
                  <Navigation className="size-4" aria-hidden />
                  Google Maps
                </a>
              </div>
              <LazyMap properties={[officePin(settings)]} single className="h-[320px]" />
            </div>
          ) : null}
        </section>

        <section aria-labelledby="form-title" className="h-fit rounded-[var(--radius-panel)] border border-lb-border bg-white p-6 shadow-[var(--shadow-card)]">
          <h2 id="form-title" className="mb-4 text-xl font-extrabold">
            {t("formTitle")}
          </h2>
          <LeadForm source="Contact" withEmail withMessage successMessage={tf("contactSuccess")} />
        </section>
      </div>
    </>
  );
}
