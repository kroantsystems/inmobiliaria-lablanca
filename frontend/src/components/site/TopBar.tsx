"use client";

import { HousePlus, Lock } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useParams, useSearchParams } from "next/navigation";
import { Suspense, useTransition } from "react";
import { usePathname, useRouter } from "@/i18n/navigation";
import { locales, type Locale } from "@/i18n/routing";
import { CURRENCIES, type CurrencyCode } from "@/lib/format/format";
import { useAlternateSlugs } from "./AlternateSlugs";
import { useCurrency } from "./CurrencyProvider";
import { useSiteDialogs } from "./SiteDialogs";

const selectClass =
  "cursor-pointer rounded-full border border-white/20 bg-white/10 px-3 py-1 text-xs text-white outline-none [&>option]:bg-white [&>option]:text-lb-ink";

function LanguageSwitcher() {
  const t = useTranslations();
  const locale = useLocale();
  const router = useRouter();
  const pathname = usePathname();
  const params = useParams();
  const searchParams = useSearchParams();
  const slugs = useAlternateSlugs();
  const [pending, startTransition] = useTransition();

  return (
    <select
      aria-label={t("topbar.language")}
      className={selectClass}
      value={locale}
      disabled={pending}
      onChange={(event) => {
        const next = event.target.value as Locale;
        // Página de anúncio: o slug muda por idioma (a página ainda redireciona se receber o slug de outro idioma).
        const nextParams = slugs && pathname === "/properties/[slug]" ? { ...params, slug: slugs[next] ?? slugs.es ?? params.slug } : params;
        startTransition(() => {
          router.replace(
            // @ts-expect-error -- params do pathname atual são compatíveis com a rota.
            { pathname, params: nextParams, query: Object.fromEntries(searchParams.entries()) },
            { locale: next },
          );
        });
      }}
    >
      {locales.map((code) => (
        <option key={code} value={code}>
          {t(`languages.${code}`)}
        </option>
      ))}
    </select>
  );
}

export function TopBar() {
  const t = useTranslations("topbar");
  const { open } = useSiteDialogs();
  const { currency, setCurrency } = useCurrency();

  return (
    <div className="border-b border-slate-700 bg-lb-ink text-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-2 px-4 py-2 text-xs">
        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => open("login")}
            className="inline-flex items-center gap-1.5 rounded-full border border-slate-400 px-3 py-1 font-bold text-slate-200 transition hover:bg-white hover:text-lb-ink"
          >
            <Lock className="size-3.5" aria-hidden />
            {t("login")}
          </button>
          <button
            type="button"
            onClick={() => open("owner")}
            className="inline-flex items-center gap-1.5 rounded-full border-2 border-lb-red px-3 py-1 font-bold transition hover:bg-lb-red"
          >
            <HousePlus className="size-3.5" aria-hidden />
            {t("publishProperty")}
          </button>
        </div>
        <div className="flex items-center gap-2">
          <Suspense fallback={null}>
            <LanguageSwitcher />
          </Suspense>
          <select
            aria-label={t("currency")}
            className={selectClass}
            value={currency}
            onChange={(event) => setCurrency(event.target.value as CurrencyCode)}
          >
            {CURRENCIES.map((code) => (
              <option key={code} value={code}>
                {code === "USD" ? "USD ($)" : code === "PYG" ? "PYG (₲)" : "BRL (R$)"}
              </option>
            ))}
          </select>
        </div>
      </div>
    </div>
  );
}
