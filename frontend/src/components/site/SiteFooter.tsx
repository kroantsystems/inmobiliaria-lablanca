import { Mail, MapPin, Phone } from "lucide-react";
import { getLocale, getTranslations } from "next-intl/server";
import { Logo } from "@/components/brand/Logo";
import { Link } from "@/i18n/navigation";
import type { PublicSettings } from "@/lib/api/types";
import { LeadForm } from "./LeadForm";

export async function SiteFooter({ settings }: { settings: PublicSettings | null }) {
  const t = await getTranslations();
  const locale = await getLocale();
  const year = new Date().getFullYear();
  const linkClass = "text-slate-400 transition hover:text-white";

  return (
    <footer className="mt-20 border-t-4 border-lb-blue bg-lb-ink px-4 pt-12 pb-8 text-white">
      <div className="mx-auto grid max-w-6xl gap-10 md:grid-cols-2 lg:grid-cols-[1.4fr_0.8fr_0.9fr_1.6fr]">
        <div>
          <Logo variant="dark" alt={t("meta.logoAlt")} width={140} />
          <p className="mt-4 max-w-xs text-sm text-slate-400">{t("footer.description")}</p>
          {settings ? (
            <ul className="mt-4 space-y-2 text-sm text-slate-300">
              {settings.phone ? (
                <li className="flex items-center gap-2">
                  <Phone className="size-4 text-lb-red" aria-hidden />
                  <a href={`tel:${settings.phone.replace(/\s/g, "")}`} className={linkClass}>
                    {settings.phone}
                  </a>
                </li>
              ) : null}
              {settings.email ? (
                <li className="flex items-center gap-2">
                  <Mail className="size-4 text-lb-red" aria-hidden />
                  <a href={`mailto:${settings.email}`} className={linkClass}>
                    {settings.email}
                  </a>
                </li>
              ) : null}
              {settings.address ? (
                <li className="flex items-start gap-2">
                  <MapPin className="mt-0.5 size-4 shrink-0 text-lb-red" aria-hidden />
                  <span>{settings.address}</span>
                </li>
              ) : null}
            </ul>
          ) : null}
        </div>

        <nav aria-label={t("footer.navigation")}>
          <h2 className="mb-3 text-sm font-extrabold">{t("footer.navigation")}</h2>
          <ul className="space-y-2 text-sm">
            <li>
              <Link href={{ pathname: "/properties", query: { operation: "Sale" } }} className={linkClass}>
                {t("catalog.titleSale")}
              </Link>
            </li>
            <li>
              <Link href={{ pathname: "/properties", query: { operation: "Rent" } }} className={linkClass}>
                {t("catalog.titleRent")}
              </Link>
            </li>
            <li>
              <Link href="/about" className={linkClass}>
                {t("nav.about")}
              </Link>
            </li>
            <li>
              <Link href="/faq" className={linkClass}>
                {t("nav.faq")}
              </Link>
            </li>
            <li>
              <Link href="/contact" className={linkClass}>
                {t("nav.contact")}
              </Link>
            </li>
            <li>
              <Link href="/privacy" className={linkClass}>
                {t("nav.privacy")}
              </Link>
            </li>
          </ul>
        </nav>

        <nav aria-label={t("footer.zones")}>
          <h2 className="mb-3 text-sm font-extrabold">{t("footer.zones")}</h2>
          <ul className="space-y-2 text-sm">
            {(settings?.zones ?? []).map((zone) => (
              <li key={zone.id}>
                <Link href={{ pathname: "/zones/[zone]", params: { zone: zone.slug } }} className={linkClass}>
                  {zone.name}
                </Link>
              </li>
            ))}
          </ul>
        </nav>

        <div className="rounded-[var(--radius-card)] border border-white/10 bg-white/5 p-5">
          <h2 className="text-sm font-extrabold">{t("forms.newsletterTitle")}</h2>
          <p className="mt-1 mb-3 text-xs text-slate-400">{t("forms.newsletterText")}</p>
          <LeadForm source="Newsletter" tone="dark" successMessage={t("forms.newsletterSuccess")} submitLabel={t("forms.newsletterSubmit")} />
        </div>
      </div>

      <p className="mx-auto mt-10 max-w-6xl border-t border-lb-slate pt-6 text-center text-xs text-slate-500" lang={locale}>
        © {year} {settings?.companyName ?? t("meta.siteName")} – Ciudad del Este. {t("footer.rights")}
      </p>
    </footer>
  );
}
