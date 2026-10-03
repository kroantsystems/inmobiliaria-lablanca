export type CurrencyCode = "USD" | "PYG" | "BRL";

export const CURRENCIES: readonly CurrencyCode[] = ["USD", "PYG", "BRL"];

export type Rates = { pygPerUsd: number; brlPerUsd: number };

export const TIME_ZONE = "America/Asuncion";

// `gn` usa as regras do espanhol do Paraguai: o suporte a guarani no Intl varia entre navegadores.
const INTL_LOCALES: Record<string, string> = { es: "es-PY", pt: "pt-BR", en: "en-US", gn: "es-PY" };

const SYMBOLS: Record<CurrencyCode, string> = { USD: "USD", PYG: "₲", BRL: "R$" };

export function intlLocale(locale: string): string {
  return INTL_LOCALES[locale] ?? "es-PY";
}

export function formatNumber(value: number, locale: string, maximumFractionDigits = 0): string {
  return new Intl.NumberFormat(intlLocale(locale), { maximumFractionDigits }).format(value);
}

export function formatPrice(amount: number, currency: CurrencyCode, locale: string): string {
  return `${SYMBOLS[currency]} ${formatNumber(Math.round(amount), locale)}`;
}

export function convertPrice(amount: number, from: CurrencyCode, to: CurrencyCode, rates: Rates): number {
  if (from === to) return amount;
  const usd = from === "USD" ? amount : from === "PYG" ? amount / rates.pygPerUsd : amount / rates.brlPerUsd;
  return to === "USD" ? usd : to === "PYG" ? usd * rates.pygPerUsd : usd * rates.brlPerUsd;
}

export function formatDate(date: Date | string, locale: string, options: Intl.DateTimeFormatOptions = { dateStyle: "medium" }): string {
  return new Intl.DateTimeFormat(intlLocale(locale), { timeZone: TIME_ZONE, ...options }).format(
    typeof date === "string" ? new Date(date) : date,
  );
}
