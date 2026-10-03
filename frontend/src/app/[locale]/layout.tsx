import type { Metadata, Viewport } from "next";
import { Fredoka, Plus_Jakarta_Sans } from "next/font/google";
import { notFound } from "next/navigation";
import { hasLocale, NextIntlClientProvider } from "next-intl";
import { getMessages, getTranslations, setRequestLocale } from "next-intl/server";
import { routing } from "@/i18n/routing";
import { SITE_URL } from "@/lib/site";
import "../globals.css";

// Fontes variáveis: um arquivo por família. Fredoka só aparece abaixo da dobra, então não é pré-carregada
// (assim não disputa banda com a imagem principal).
const fredoka = Fredoka({ subsets: ["latin"], variable: "--font-fredoka", display: "swap", preload: false });
const jakarta = Plus_Jakarta_Sans({ subsets: ["latin"], variable: "--font-jakarta", display: "swap" });

export const viewport: Viewport = {
  themeColor: "#005DAA",
  width: "device-width",
  initialScale: 1,
};

// Namespaces usados por componentes de cliente do site. O painel injeta o restante no próprio layout;
// enviar tudo deixaria o HTML de cada página pública bem maior (textos do painel inclusive).
const CLIENT_NAMESPACES = ["meta", "nav", "topbar", "languages", "currency", "common", "login", "forms", "whatsapp", "search", "property", "simulator", "home"];

export function generateStaticParams() {
  return routing.locales.map((locale) => ({ locale }));
}

export async function generateMetadata({ params }: LayoutProps<"/[locale]">): Promise<Metadata> {
  const { locale } = await params;
  const t = await getTranslations({ locale, namespace: "meta" });
  return {
    metadataBase: new URL(SITE_URL),
    title: { default: `${t("siteName")} | ${t("defaultTitle")}`, template: `%s | ${t("siteName")}` },
    description: t("defaultDescription"),
    applicationName: t("siteName"),
    formatDetection: { telephone: false, email: false, address: false },
  };
}

export default async function LocaleLayout({ children, params }: LayoutProps<"/[locale]">) {
  const { locale } = await params;
  if (!hasLocale(routing.locales, locale)) notFound();
  setRequestLocale(locale);
  const messages = await getMessages();
  const clientMessages = Object.fromEntries(CLIENT_NAMESPACES.map((namespace) => [namespace, messages[namespace]]));

  return (
    <html lang={locale} className={`${fredoka.variable} ${jakarta.variable}`}>
      <body className="min-h-dvh bg-lb-bg font-sans text-lb-ink antialiased">
        <NextIntlClientProvider messages={clientMessages}>
          {children}
        </NextIntlClientProvider>
      </body>
    </html>
  );
}
