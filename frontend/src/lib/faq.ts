import { getTranslations } from "next-intl/server";

export const FAQ_KEYS = ["zones", "currency", "visit", "publish", "simulator", "foreign"] as const;

/** Perguntas frequentes no idioma atual, na ordem de exibição. */
export async function getFaqItems(locale: string) {
  const t = await getTranslations({ locale, namespace: "faq.items" });
  return FAQ_KEYS.map((key) => ({ question: t(`${key}.q`), answer: t(`${key}.a`) }));
}
