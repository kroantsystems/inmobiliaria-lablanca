"use client";

import { useLocale, useTranslations } from "next-intl";
import { useId, useState } from "react";
import { formatNumber, formatPrice } from "@/lib/format/format";
import { monthlyPayment, SIMULATOR_TERMS } from "@/lib/simulator";
import { useCurrency } from "./CurrencyProvider";

export function Simulator({ annualRate }: { annualRate: number }) {
  const t = useTranslations("simulator");
  const locale = useLocale();
  const { currency } = useCurrency();
  const id = useId();
  const [value, setValue] = useState("100000");
  const [years, setYears] = useState<number>(10);

  const payment = monthlyPayment(Number(value), annualRate, years);

  return (
    <section id="simulador" className="mx-auto mt-16 max-w-6xl scroll-mt-6 px-4">
      <div className="grid items-center gap-10 rounded-[var(--radius-panel)] bg-gradient-to-br from-lb-blue to-lb-blue-dark p-8 text-white shadow-[var(--shadow-raised)] md:grid-cols-2 md:p-10">
        <div>
          <span className="rounded-full bg-white/20 px-3 py-1 text-xs font-extrabold uppercase">{t("tag")}</span>
          <h2 className="mt-3 text-3xl font-extrabold">{t("title")}</h2>
          <p className="mt-2 text-sm text-white/90">{t("text")}</p>
        </div>
        <div className="rounded-[var(--radius-card)] bg-white p-6 text-lb-ink shadow-[var(--shadow-modal)]">
          <label htmlFor={`${id}-value`} className="block text-xs font-bold uppercase text-lb-blue">
            {t("value", { currency })}
          </label>
          <input
            id={`${id}-value`}
            type="number"
            min={0}
            inputMode="numeric"
            value={value}
            onChange={(e) => setValue(e.target.value)}
            className="mt-1 mb-4 w-full rounded-lg border border-lb-border bg-lb-bg px-3 py-2.5 outline-none focus:border-lb-blue"
          />
          <label htmlFor={`${id}-years`} className="block text-xs font-bold uppercase text-lb-blue">
            {t("years")}
          </label>
          <select
            id={`${id}-years`}
            value={years}
            onChange={(e) => setYears(Number(e.target.value))}
            className="mt-1 w-full rounded-lg border border-lb-border bg-lb-bg px-3 py-2.5 outline-none focus:border-lb-blue"
          >
            {SIMULATOR_TERMS.map((term) => (
              <option key={term} value={term}>
                {t("yearsOption", { count: term })}
              </option>
            ))}
          </select>
          <div className="mt-5 text-center" aria-live="polite">
            <p className="text-xs font-extrabold uppercase text-lb-muted">{t("result")}</p>
            <p className="text-3xl font-extrabold text-lb-blue">{payment === null ? "—" : formatPrice(payment, currency, locale)}</p>
          </div>
          <p className="mt-3 text-center text-xs text-lb-muted">{t("disclaimer", { rate: formatNumber(annualRate, locale, 2) })}</p>
        </div>
      </div>
    </section>
  );
}
