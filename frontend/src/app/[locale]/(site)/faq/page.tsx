import { getTranslations, setRequestLocale } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { DialogButton } from "@/components/site/DialogButton";
import { FaqList } from "@/components/site/FaqList";
import { PageHero } from "@/components/site/PageHero";
import { buttons } from "@/components/ui/buttons";
import type { Locale } from "@/i18n/routing";
import { getFaqItems } from "@/lib/faq";
import { faqPage } from "@/lib/seo/jsonld";
import { buildMetadata } from "@/lib/seo/metadata";

export async function generateMetadata({ params }: PageProps<"/[locale]/faq">) {
  const { locale } = (await params) as { locale: Locale };
  const [t, tm] = await Promise.all([getTranslations({ locale, namespace: "faq" }), getTranslations({ locale, namespace: "meta" })]);
  return buildMetadata({ locale, title: t("title"), description: tm("faqDescription"), href: "/faq" });
}

export default async function FaqPage({ params }: PageProps<"/[locale]/faq">) {
  const { locale } = (await params) as { locale: Locale };
  setRequestLocale(locale);
  const [t, items] = await Promise.all([getTranslations("faq"), getFaqItems(locale)]);

  return (
    <>
      <JsonLd data={faqPage(items)} />
      <PageHero locale={locale} crumbs={[{ name: t("title"), href: "/faq" }]} title={t("title")} lead={t("intro")} />

      <section className="mx-auto mt-12 max-w-3xl px-4">
        <FaqList items={items} />
        <div className="mt-10 rounded-[var(--radius-panel)] border border-lb-border bg-white p-8 text-center shadow-[var(--shadow-card)]">
          <h2 className="text-xl font-extrabold">{t("contactTitle")}</h2>
          <p className="mt-1 text-sm text-lb-muted">{t("contactText")}</p>
          <DialogButton dialog="contact" className={`${buttons.red} mt-5`}>
            {(await getTranslations("nav"))("vipContact")}
          </DialogButton>
        </div>
      </section>
    </>
  );
}
