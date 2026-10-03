import { getTranslations, setRequestLocale } from "next-intl/server";
import { PageHero } from "@/components/site/PageHero";
import type { Locale } from "@/i18n/routing";
import { formatDate } from "@/lib/format/format";
import { buildMetadata } from "@/lib/seo/metadata";

// Atualizar junto com qualquer mudança no texto da política.
const LAST_UPDATED = "2026-10-02T12:00:00Z";
const SECTIONS = ["data", "use", "analytics", "sharing", "retention", "rights", "contact"] as const;

export async function generateMetadata({ params }: PageProps<"/[locale]/privacy">) {
  const { locale } = (await params) as { locale: Locale };
  const [t, tm] = await Promise.all([getTranslations({ locale, namespace: "privacy" }), getTranslations({ locale, namespace: "meta" })]);
  return buildMetadata({ locale, title: t("title"), description: tm("privacyDescription"), href: "/privacy" });
}

export default async function PrivacyPage({ params }: PageProps<"/[locale]/privacy">) {
  const { locale } = (await params) as { locale: Locale };
  setRequestLocale(locale);
  const t = await getTranslations("privacy");

  return (
    <>
      <PageHero locale={locale} crumbs={[{ name: t("title"), href: "/privacy" }]} title={t("title")} lead={t("intro")} />
      <article className="mx-auto mt-12 max-w-3xl space-y-8 px-4">
        <p className="text-sm text-lb-muted">{t("updated", { date: formatDate(LAST_UPDATED, locale, { dateStyle: "long" }) })}</p>
        {SECTIONS.map((key) => (
          <section key={key}>
            <h2 className="mb-2 text-xl font-extrabold">{t(`sections.${key}.title`)}</h2>
            <p className="leading-relaxed text-lb-ink/85">{t(`sections.${key}.text`)}</p>
          </section>
        ))}
      </article>
    </>
  );
}
