"use client";

import { Menu, Send, X } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { Logo } from "@/components/brand/Logo";
import { Link } from "@/i18n/navigation";
import { useSiteDialogs } from "./SiteDialogs";

export function SiteHeader() {
  const t = useTranslations();
  const locale = useLocale();
  const { open } = useSiteDialogs();
  const [menuOpen, setMenuOpen] = useState(false);

  const links = [
    { key: "home", node: <Link href="/">{t("nav.home")}</Link> },
    { key: "properties", node: <Link href="/properties">{t("nav.properties")}</Link> },
    { key: "map", node: <a href={`/${locale}#mapa`}>{t("nav.map")}</a> },
    { key: "about", node: <Link href="/about">{t("nav.about")}</Link> },
    { key: "simulator", node: <a href={`/${locale}#simulador`}>{t("nav.simulator")}</a> },
    { key: "contact", node: <Link href="/contact">{t("nav.contact")}</Link> },
  ];

  return (
    <header className="bg-lb-slate text-white">
      <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-3">
        <Link href="/" aria-label={t("meta.siteName")} className="shrink-0">
          <Logo variant="dark" alt={t("meta.logoAlt")} width={120} priority />
        </Link>

        <nav aria-label={t("nav.mainLabel")} className="hidden lg:block">
          <ul className="flex items-center gap-6 text-sm font-medium text-white/80 [&_a:hover]:text-white">
            {links.map((link) => (
              <li key={link.key}>{link.node}</li>
            ))}
          </ul>
        </nav>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => open("contact")}
            className="hidden items-center gap-2 rounded-full bg-lb-red px-5 py-2.5 text-sm font-bold shadow-lg shadow-lb-red/30 transition hover:bg-lb-red-hover sm:inline-flex"
          >
            <Send className="size-4" aria-hidden />
            {t("nav.vipContact")}
          </button>
          <button
            type="button"
            className="rounded-lg p-2 lg:hidden"
            aria-expanded={menuOpen}
            aria-controls="mobile-menu"
            aria-label={menuOpen ? t("nav.closeMenu") : t("nav.openMenu")}
            onClick={() => setMenuOpen((value) => !value)}
          >
            {menuOpen ? <X className="size-6" aria-hidden /> : <Menu className="size-6" aria-hidden />}
          </button>
        </div>
      </div>

      {menuOpen ? (
        <nav id="mobile-menu" aria-label={t("nav.mainLabel")} className="border-t border-white/10 lg:hidden">
          <ul className="mx-auto max-w-6xl space-y-1 px-4 py-3 text-base [&_a]:block [&_a]:rounded-lg [&_a]:px-3 [&_a]:py-2 [&_a:hover]:bg-white/10">
            {links.map((link) => (
              <li key={link.key} onClick={() => setMenuOpen(false)}>
                {link.node}
              </li>
            ))}
            <li>
              <button
                type="button"
                onClick={() => {
                  setMenuOpen(false);
                  open("contact");
                }}
                className="mt-2 w-full rounded-full bg-lb-red px-5 py-2.5 text-sm font-bold"
              >
                {t("nav.vipContact")}
              </button>
            </li>
          </ul>
        </nav>
      ) : null}
    </header>
  );
}
