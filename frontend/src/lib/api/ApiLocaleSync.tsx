"use client";

import { useLocale } from "next-intl";
import { useEffect } from "react";
import { apiClient } from "./client";

/** Mantém o Accept-Language das chamadas do navegador igual ao idioma da página. */
export function ApiLocaleSync() {
  const locale = useLocale();
  useEffect(() => apiClient.setLocale(locale), [locale]);
  return null;
}
