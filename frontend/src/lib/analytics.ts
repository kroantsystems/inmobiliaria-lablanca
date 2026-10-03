"use client";

import type { AnalyticsEventType } from "@/lib/api/types";

const SESSION_KEY = "lb-sid";

/** Identificador aleatório por aba (sessionStorage), sem cookie e sem dado pessoal. */
function sessionId(): string {
  try {
    let id = sessionStorage.getItem(SESSION_KEY);
    if (!id) {
      id = crypto.randomUUID();
      sessionStorage.setItem(SESSION_KEY, id);
    }
    return id;
  } catch {
    return crypto.randomUUID();
  }
}

export function track(type: AnalyticsEventType, options: { propertyId?: string; locale: string; path?: string }) {
  const body = JSON.stringify({
    type,
    propertyId: options.propertyId ?? null,
    path: options.path ?? window.location.pathname,
    locale: options.locale,
    sessionId: sessionId(),
    referrer: document.referrer || null,
  });

  try {
    fetch("/api/public/analytics/events", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body,
      keepalive: true,
    }).catch(() => {});
  } catch {
    // Métrica nunca pode quebrar a página.
  }
}
