import { getTranslations, setRequestLocale } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { AlternateSlugsProvider } from "@/components/site/AlternateSlugs";
import { PageViewTracker } from "@/components/site/Analytics";
import { CurrencyProvider } from "@/components/site/CurrencyProvider";
import { SiteDialogs } from "@/components/site/SiteDialogs";
import { SiteFooter } from "@/components/site/SiteFooter";
import { SiteHeader } from "@/components/site/SiteHeader";
import { TopBar } from "@/components/site/TopBar";
import { WhatsAppFloat } from "@/components/site/WhatsApp";
import type { Locale } from "@/i18n/routing";
import { getSettings, safely } from "@/lib/api/server";
import { realEstateAgent } from "@/lib/seo/jsonld";

export default async function SiteLayout({ children, params }: LayoutProps<"/[locale]">) {
  const { locale } = await params;
  setRequestLocale(locale);
  const t = await getTranslations("nav");
  const settings = await safely(getSettings(locale), null);

  return (
    <CurrencyProvider
      rates={{ pygPerUsd: settings?.pygPerUsd ?? 7500, brlPerUsd: settings?.brlPerUsd ?? 5 }}
      ratesUpdatedAt={settings?.ratesUpdatedAt ?? new Date(0).toISOString()}
    >
      <AlternateSlugsProvider>
        <SiteDialogs>
          <a href="#conteudo" className="sr-only z-50 bg-white p-3 focus:not-sr-only focus:fixed focus:top-2 focus:left-2">
            {t("skipToContent")}
          </a>
          <TopBar />
          <SiteHeader />
          <main id="conteudo">{children}</main>
          <SiteFooter settings={settings} />
          <WhatsAppFloat number={settings?.whatsappNumber ?? null} />
          <PageViewTracker />
          <JsonLd data={realEstateAgent(settings, locale as Locale)} />
        </SiteDialogs>
      </AlternateSlugsProvider>
    </CurrencyProvider>
  );
}
