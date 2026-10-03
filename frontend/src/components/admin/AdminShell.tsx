"use client";

import { Building2, CalendarDays, ChevronDown, ExternalLink, FolderOpen, LayoutDashboard, LogOut, Settings, UserCog, UserRound, Users } from "lucide-react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import { BrandMark } from "@/components/brand/Logo";
import { LanguageSwitcher } from "@/components/site/TopBar";
import { Link, usePathname, useRouter } from "@/i18n/navigation";
import { session } from "@/lib/api/client";
import { useAuth } from "@/lib/auth/AuthProvider";
import { Loading, ToastProvider } from "./ui";

const SECTIONS = [
  { key: "dashboard", href: "/admin", icon: LayoutDashboard },
  { key: "calendar", href: "/admin/calendar", icon: CalendarDays },
  { key: "files", href: "/admin/files", icon: FolderOpen },
  { key: "properties", href: "/admin/properties", icon: Building2 },
  { key: "leads", href: "/admin/leads", icon: Users },
  { key: "owners", href: "/admin/owners", icon: UserRound },
  { key: "settings", href: "/admin/settings", icon: Settings },
] as const;

function titleKey(pathname: string, id: string | undefined): string {
  if (pathname === "/admin/properties/[id]") return id === "new" ? "propertyNew" : "propertyEdit";
  if (pathname === "/admin/account") return "account";
  return SECTIONS.find((section) => section.href === pathname)?.key ?? "dashboard";
}

function initials(name: string) {
  return name
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0]!.toUpperCase())
    .join("");
}

function UserMenu({ name, onLogout }: { name: string; onLogout: () => void }) {
  const t = useTranslations("admin");
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const close = (event: MouseEvent | KeyboardEvent) => {
      if (event instanceof KeyboardEvent ? event.key === "Escape" : !ref.current?.contains(event.target as Node)) setOpen(false);
    };
    document.addEventListener("mousedown", close);
    document.addEventListener("keydown", close);
    return () => {
      document.removeEventListener("mousedown", close);
      document.removeEventListener("keydown", close);
    };
  }, [open]);

  const item = "flex w-full items-center gap-2 rounded-lg px-3 py-2 text-left text-sm font-semibold hover:bg-lb-panel";

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        aria-haspopup="menu"
        aria-expanded={open}
        aria-label={t("userMenu")}
        onClick={() => setOpen((value) => !value)}
        className="flex items-center gap-2.5 rounded-full border border-lb-border bg-white py-1.5 pr-3 pl-1.5"
      >
        <span className="flex size-8 items-center justify-center rounded-full bg-lb-blue text-xs font-extrabold text-white">{initials(name)}</span>
        <span className="hidden text-sm font-bold sm:inline">{name}</span>
        <ChevronDown className="size-4 text-lb-muted" aria-hidden />
      </button>
      {open ? (
        <div role="menu" className="absolute right-0 z-30 mt-2 w-52 rounded-xl border border-lb-border bg-white p-1.5 shadow-[var(--shadow-modal)]">
          <Link role="menuitem" href="/admin/account" className={item} onClick={() => setOpen(false)}>
            <UserCog className="size-4" aria-hidden />
            {t("menu.account")}
          </Link>
          <Link role="menuitem" href="/" className={item} target="_blank">
            <ExternalLink className="size-4" aria-hidden />
            {t("viewSite")}
          </Link>
          <button role="menuitem" type="button" onClick={onLogout} className={`${item} text-lb-red`}>
            <LogOut className="size-4" aria-hidden />
            {t("logout")}
          </button>
        </div>
      ) : null}
    </div>
  );
}

/** Estrutura do painel (barra lateral + topo) e guarda de sessão: sem sessão válida volta para a home com o login aberto. */
export function AdminShell({ children }: { children: React.ReactNode }) {
  const t = useTranslations("admin");
  const { status, user, logout } = useAuth();
  const router = useRouter();
  const pathname = usePathname();
  const params = useParams<{ id?: string }>();
  const exit = useRef<"expired" | "logout" | null>(null);

  useEffect(() => {
    session.onExpired(() => {
      exit.current = "expired";
    });
    return () => session.onExpired(() => {});
  }, []);

  useEffect(() => {
    if (status !== "anonymous") return;
    if (exit.current === "logout") router.replace("/");
    else router.replace({ pathname: "/", query: exit.current === "expired" ? { login: "1", expired: "1" } : { login: "1" } });
  }, [status, router]);

  if (status !== "authenticated" || !user) {
    return (
      <div className="flex min-h-dvh items-center justify-center bg-lb-panel">
        <Loading />
      </div>
    );
  }

  const active = (href: string) => (href === "/admin" ? pathname === "/admin" : pathname === href || pathname.startsWith(`${href}/`));

  return (
    <ToastProvider>
      <div className="min-h-dvh bg-lb-panel">
        <aside className="fixed inset-y-0 left-0 z-20 flex w-20 flex-col gap-7 bg-lb-ink px-2 py-5 text-white lg:w-[260px] lg:px-4 lg:py-6">
          <Link href="/admin" className="flex items-center justify-center gap-3 px-1 lg:justify-start">
            <BrandMark className="h-11 w-auto shrink-0" title="Inmobiliaria La Blanca" />
            <span className="hidden font-display text-xl leading-none lg:block">
              LA BLANCA
              <span className="mt-1 block font-sans text-[0.65rem] font-extrabold tracking-widest text-lb-red uppercase">{t("brandSubtitle")}</span>
            </span>
          </Link>
          <nav aria-label={t("menuLabel")}>
            <ul className="flex flex-col gap-1.5">
              {SECTIONS.map(({ key, href, icon: Icon }) => {
                const isActive = active(href);
                return (
                  <li key={key}>
                    <Link
                      href={href}
                      aria-current={isActive ? "page" : undefined}
                      title={t(`menu.${key}`)}
                      className={`flex items-center justify-center gap-3 rounded-lg px-3 py-3 text-sm font-semibold transition lg:justify-start lg:px-4 ${
                        isActive ? "bg-lb-blue font-bold text-white" : "text-slate-400 hover:bg-white/10 hover:text-white"
                      }`}
                    >
                      <Icon className="size-5 shrink-0" aria-hidden />
                      <span className="sr-only lg:not-sr-only">{t(`menu.${key}`)}</span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </nav>
        </aside>

        <div className="ml-20 min-w-0 p-4 sm:p-6 lg:ml-[260px] lg:p-8">
          <header className="mb-7 flex flex-wrap items-center justify-between gap-4">
            <div>
              <h1 className="text-2xl font-extrabold">{t(`titles.${titleKey(pathname, params.id)}`)}</h1>
              <p className="text-sm text-lb-muted">{t("welcome", { name: user.name.split(" ")[0] })}</p>
            </div>
            <div className="flex items-center gap-3">
              <LanguageSwitcher className="cursor-pointer rounded-full border border-lb-border bg-white px-3 py-2 text-sm font-semibold outline-none" />
              <UserMenu
                name={user.name}
                onLogout={() => {
                  exit.current = "logout";
                  void logout();
                }}
              />
            </div>
          </header>
          <main>{children}</main>
        </div>
      </div>
    </ToastProvider>
  );
}
