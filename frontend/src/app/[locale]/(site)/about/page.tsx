import { Handshake, MapPinned, ShieldCheck } from "lucide-react";
import { getTranslations, setRequestLocale } from "next-intl/server";
import { DialogButton } from "@/components/site/DialogButton";
import { PageHero } from "@/components/site/PageHero";
import { buttons } from "@/components/ui/buttons";
import { Link } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { buildMetadata } from "@/lib/seo/metadata";

export async function generateMetadata({ params }: PageProps<"/[locale]/about">) {
  const { locale } = (await params) as { locale: Locale };
  const [t, tm] = await Promise.all([getTranslations({ locale, namespace: "about" }), getTranslations({ locale, namespace: "meta" })]);
  return buildMetadata({ locale, title: t("title"), description: tm("aboutDescription"), href: "/about" });
}

const VALUES = [
  { key: "transparency", icon: ShieldCheck },
  { key: "local", icon: MapPinned },
  { key: "service", icon: Handshake },
] as const;

export default async function AboutPage({ params }: PageProps<"/[locale]/about">) {
  const { locale } = (await params) as { locale: Locale };
  setRequestLocale(locale);
  const [t, th, tn] = await Promise.all([getTranslations("about"), getTranslations("home"), getTranslations("nav")]);

  return (
    <>
      <PageHero locale={locale} crumbs={[{ name: tn("about"), href: "/about" }]} title={t("title")} lead={t("lead")} />

      <section className="mx-auto mt-12 grid max-w-6xl items-center gap-10 px-4 md:grid-cols-2">
        <div>
          <p className="text-xs font-extrabold text-lb-blue uppercase">{th("aboutTag")}</p>
          <h2 className="mt-1 mb-4 text-3xl font-extrabold">{t("storyTitle")}</h2>
          <p className="leading-relaxed text-lb-muted">{t("storyText")}</p>
        </div>
        <ul className="grid grid-cols-3 gap-4 rounded-[var(--radius-panel)] bg-gradient-to-br from-lb-blue to-lb-blue-dark p-8 text-center text-white shadow-[var(--shadow-raised)]">
          {([1, 2, 3] as const).map((n) => (
            <li key={n}>
              <p className="font-display text-3xl font-bold sm:text-4xl">{th(`stat${n}Value`)}</p>
              <p className="mt-1 text-xs text-white/80">{th(`stat${n}Label`)}</p>
            </li>
          ))}
        </ul>
      </section>

      <section aria-labelledby="values-title" className="mx-auto mt-16 max-w-6xl px-4">
        <h2 id="values-title" className="mb-6 text-2xl font-extrabold">
          {t("valuesTitle")}
        </h2>
        <ul className="grid gap-6 md:grid-cols-3">
          {VALUES.map(({ key, icon: Icon }) => (
            <li key={key} className="rounded-[var(--radius-card)] border border-lb-border bg-white p-6 shadow-[var(--shadow-card)]">
              <Icon className="size-8 text-lb-red" aria-hidden />
              <h3 className="mt-3 text-lg font-extrabold">{t(`values.${key}.title`)}</h3>
              <p className="mt-1 text-sm text-lb-muted">{t(`values.${key}.text`)}</p>
            </li>
          ))}
        </ul>
      </section>

      <section className="mx-auto mt-16 max-w-6xl px-4">
        <div className="rounded-[var(--radius-panel)] bg-lb-ink px-6 py-10 text-center text-white">
          <h2 className="text-2xl font-extrabold">{t("ctaTitle")}</h2>
          <p className="mt-2 text-white/80">{t("ctaText")}</p>
          <div className="mt-6 flex flex-wrap justify-center gap-3">
            <Link href="/properties" className={buttons.blue}>
              {t("ctaProperties")}
            </Link>
            <DialogButton dialog="contact" className={buttons.red}>
              {t("ctaContact")}
            </DialogButton>
          </div>
        </div>
      </section>
    </>
  );
}
