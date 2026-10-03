"use client";

import { useLocale, useTranslations } from "next-intl";
import { useSearchParams } from "next/navigation";
import { createContext, Suspense, useCallback, useContext, useEffect, useMemo, useState } from "react";
import { Modal } from "@/components/ui/Modal";
import { track } from "@/lib/analytics";
import { LeadForm } from "./LeadForm";
import { LoginForm } from "./LoginForm";

type DialogKind = "login" | "contact" | "owner";

const DialogContext = createContext<{ open: (kind: DialogKind) => void } | null>(null);

export function useSiteDialogs() {
  const context = useContext(DialogContext);
  if (!context) throw new Error("useSiteDialogs must be used inside SiteDialogs");
  return context;
}

/** Abre o login quando a URL tem ?login=1 (ex.: sessão do painel expirada). */
function LoginFromUrl({ onOpen }: { onOpen: (expired: boolean) => void }) {
  const params = useSearchParams();
  useEffect(() => {
    if (params.get("login") === "1") onOpen(params.get("expired") === "1");
  }, [params, onOpen]);
  return null;
}

export function SiteDialogs({ children }: { children: React.ReactNode }) {
  const [current, setCurrent] = useState<DialogKind | null>(null);
  const [expired, setExpired] = useState(false);
  const locale = useLocale();
  const t = useTranslations();

  const open = useCallback(
    (kind: DialogKind) => {
      if (kind === "contact") track("ContactClick", { locale });
      setCurrent(kind);
    },
    [locale],
  );

  const openLoginFromUrl = useCallback((wasExpired: boolean) => {
    setExpired(wasExpired);
    setCurrent("login");
  }, []);

  const close = useCallback(() => {
    setCurrent(null);
    setExpired(false);
    const url = new URL(window.location.href);
    if (url.searchParams.has("login")) {
      url.searchParams.delete("login");
      url.searchParams.delete("expired");
      window.history.replaceState(null, "", url);
    }
  }, []);

  const value = useMemo(() => ({ open }), [open]);

  return (
    <DialogContext.Provider value={value}>
      {children}
      <Suspense fallback={null}>
        <LoginFromUrl onOpen={openLoginFromUrl} />
      </Suspense>

      <Modal open={current === "login"} onClose={close} labelledBy="login-title" size="sm">
        <p className="text-xs font-extrabold uppercase tracking-wide text-lb-blue">{t("meta.siteName")}</p>
        <h2 id="login-title" className="mt-1 font-display text-2xl font-bold">
          {t("login.title")}
        </h2>
        <p className="mb-5 text-sm text-lb-muted">{t("login.subtitle")}</p>
        <LoginForm notice={expired ? t("login.sessionExpired") : undefined} />
      </Modal>

      <Modal open={current === "contact"} onClose={close} labelledBy="contact-title">
        <p className="text-xs font-extrabold uppercase tracking-wide text-lb-blue">{t("forms.contactTitle")}</p>
        <h2 id="contact-title" className="mt-1 mb-5 font-display text-2xl font-bold">
          {t("forms.contactHeading")}
        </h2>
        <LeadForm source="Contact" autoFocus withEmail withMessage successMessage={t("forms.contactSuccess")} />
      </Modal>

      <Modal open={current === "owner"} onClose={close} labelledBy="owner-title">
        <p className="text-xs font-extrabold uppercase tracking-wide text-lb-blue">{t("forms.ownerTag")}</p>
        <h2 id="owner-title" className="mt-1 mb-5 font-display text-2xl font-bold">
          {t("forms.ownerTitle")}
        </h2>
        <LeadForm source="OwnerProposal" autoFocus withEmail withMessage successMessage={t("forms.ownerSuccess")} />
      </Modal>
    </DialogContext.Provider>
  );
}
