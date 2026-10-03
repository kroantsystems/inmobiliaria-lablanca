"use client";

import { useLocale, useTranslations } from "next-intl";
import { createContext, useCallback, useContext, useMemo, useSyncExternalStore } from "react";
import { convertPrice, CURRENCIES, formatDate, formatPrice, type CurrencyCode, type Rates } from "@/lib/format/format";

const STORAGE_KEY = "lb-currency";

type CurrencyContextValue = {
  currency: CurrencyCode;
  setCurrency: (currency: CurrencyCode) => void;
  rates: Rates;
  ratesUpdatedAt: string;
  format: (amount: number, from: CurrencyCode) => string;
};

const CurrencyContext = createContext<CurrencyContextValue | null>(null);

// Moeda escolhida guardada no navegador; se o armazenamento estiver bloqueado, vale só em memória nesta visita.
let memoryCurrency: CurrencyCode | null = null;
const listeners = new Set<() => void>();

function readCurrency(): CurrencyCode {
  try {
    const saved = localStorage.getItem(STORAGE_KEY);
    if (saved && (CURRENCIES as readonly string[]).includes(saved)) return saved as CurrencyCode;
  } catch {
    // Armazenamento bloqueado.
  }
  return memoryCurrency ?? "USD";
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  window.addEventListener("storage", listener);
  return () => {
    listeners.delete(listener);
    window.removeEventListener("storage", listener);
  };
}

export function CurrencyProvider({ rates, ratesUpdatedAt, children }: { rates: Rates; ratesUpdatedAt: string; children: React.ReactNode }) {
  const locale = useLocale();
  // No servidor e na hidratação o preço sai em USD; depois aplica a moeda salva.
  const currency = useSyncExternalStore(subscribe, readCurrency, () => "USD" as CurrencyCode);

  const setCurrency = useCallback((next: CurrencyCode) => {
    memoryCurrency = next;
    try {
      localStorage.setItem(STORAGE_KEY, next);
    } catch {
      // Sem persistência; a escolha vale só para esta visita.
    }
    listeners.forEach((listener) => listener());
  }, []);

  const value = useMemo<CurrencyContextValue>(
    () => ({
      currency,
      setCurrency,
      rates,
      ratesUpdatedAt,
      format: (amount, from) => formatPrice(convertPrice(amount, from, currency, rates), currency, locale),
    }),
    [currency, setCurrency, rates, ratesUpdatedAt, locale],
  );

  return <CurrencyContext.Provider value={value}>{children}</CurrencyContext.Provider>;
}

export function useCurrency() {
  const context = useContext(CurrencyContext);
  if (!context) throw new Error("useCurrency must be used inside CurrencyProvider");
  return context;
}

/** Preço na moeda escolhida; o servidor renderiza o valor original para não deslocar o layout. */
export function Price({ amount, currency, perMonth = false, className }: { amount: number; currency: CurrencyCode; perMonth?: boolean; className?: string }) {
  const { format } = useCurrency();
  const t = useTranslations("currency");
  return (
    <span className={className}>
      {format(amount, currency)}
      {perMonth ? <span className="text-[0.7em] font-semibold opacity-80"> {t("perMonth")}</span> : null}
    </span>
  );
}

export function CurrencyNotice({ className }: { className?: string }) {
  const { currency, ratesUpdatedAt } = useCurrency();
  const locale = useLocale();
  const t = useTranslations("currency");
  if (currency === "USD") return null;
  return <p className={className}>{t("notice", { date: formatDate(ratesUpdatedAt, locale, { dateStyle: "medium" }) })}</p>;
}
