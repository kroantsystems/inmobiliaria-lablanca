"use client";

import { AlertTriangle, ArrowDown, ArrowUp, ArrowUpDown, Loader2, Search, X } from "lucide-react";
import { useTranslations } from "next-intl";
import { createContext, useCallback, useContext, useEffect, useId, useRef, useState } from "react";
import type { SortState } from "@/hooks/useClientTable";

// Estilos do dashboard.html: cartões brancos com borda clara, rótulos em azul-escuro maiúsculo, botões retangulares.
const control =
  "rounded-lg border border-lb-border bg-white px-3.5 py-2.5 text-sm text-lb-ink outline-none transition focus:border-lb-blue disabled:bg-lb-bg disabled:text-lb-muted";

export const ui = {
  input: `w-full ${control}`,
  select: `w-auto ${control}`,
  label: "text-xs font-extrabold text-lb-blue-dark uppercase",
  button:
    "inline-flex items-center justify-center gap-2 rounded-lg px-4 py-2.5 text-sm font-bold transition disabled:pointer-events-none disabled:opacity-60",
  primary: "bg-lb-blue text-white hover:bg-lb-blue-hover",
  danger: "bg-lb-red text-white hover:bg-lb-red-hover",
  ghost: "border border-lb-border bg-white text-lb-ink hover:border-lb-blue hover:text-lb-blue",
  iconButton: "inline-flex size-9 items-center justify-center rounded-lg text-lb-muted transition hover:bg-lb-panel hover:text-lb-ink",
};

export const btn = {
  primary: `${ui.button} ${ui.primary}`,
  danger: `${ui.button} ${ui.danger}`,
  ghost: `${ui.button} ${ui.ghost}`,
};

export function Panel({ title, actions, children, className = "" }: { title?: React.ReactNode; actions?: React.ReactNode; children: React.ReactNode; className?: string }) {
  return (
    <section className={`rounded-xl border border-lb-border bg-white p-5 shadow-[0_2px_8px_rgb(0_0_0/0.04)] sm:p-6 ${className}`}>
      {title || actions ? (
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          {title ? <h2 className="text-base font-extrabold">{title}</h2> : <span />}
          {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
        </div>
      ) : null}
      {children}
    </section>
  );
}

type FieldProps = { label: string; hint?: string; error?: string; full?: boolean; children: (id: string) => React.ReactNode };

/** Rótulo + controle + dica/erro, com ids ligados para leitores de tela. */
export function Field({ label, hint, error, full = false, children }: FieldProps) {
  const id = useId();
  return (
    <div className={`flex flex-col gap-1.5 ${full ? "sm:col-span-2" : ""}`}>
      <label htmlFor={id} className={ui.label}>
        {label}
      </label>
      {children(id)}
      {hint ? <p className="text-xs text-lb-muted">{hint}</p> : null}
      {error ? (
        <p role="alert" className="text-xs font-semibold text-lb-red">
          {error}
        </p>
      ) : null}
    </div>
  );
}

export const TONES = {
  green: "bg-green-100 text-green-800",
  amber: "bg-amber-100 text-amber-900",
  blue: "bg-sky-100 text-lb-blue-dark",
  gray: "bg-slate-200 text-slate-700",
  red: "bg-red-100 text-red-800",
  purple: "bg-violet-100 text-violet-800",
} as const;

export type Tone = keyof typeof TONES;

export function Badge({ tone = "gray", children }: { tone?: Tone; children: React.ReactNode }) {
  return <span className={`inline-flex items-center rounded-full px-2.5 py-1 text-[0.7rem] font-extrabold whitespace-nowrap uppercase ${TONES[tone]}`}>{children}</span>;
}

export function SortHeader<K extends string>({ label, column, sort, onSort }: { label: string; column: K; sort: SortState<K>; onSort: (key: K) => void }) {
  const t = useTranslations("admin.common");
  const active = sort?.key === column;
  const Icon = !active ? ArrowUpDown : sort.direction === "asc" ? ArrowUp : ArrowDown;
  return (
    <th scope="col" aria-sort={active ? (sort.direction === "asc" ? "ascending" : "descending") : "none"} className={thClass}>
      <button type="button" onClick={() => onSort(column)} className="inline-flex items-center gap-1 uppercase hover:text-lb-ink" title={t("sortBy", { column: label })}>
        {label}
        <Icon className={`size-3.5 ${active ? "text-lb-blue" : "opacity-50"}`} aria-hidden />
      </button>
    </th>
  );
}

export const thClass = "bg-lb-bg px-3 py-3 text-left text-[0.7rem] font-extrabold text-lb-muted uppercase border-b border-lb-border";
export const tdClass = "px-3 py-3 align-middle border-b border-lb-border";

export function SearchInput({ value, onChange, placeholder }: { value: string; onChange: (value: string) => void; placeholder: string }) {
  const t = useTranslations("admin.common");
  return (
    <div className="relative w-full sm:w-72">
      <Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-lb-muted" aria-hidden />
      <input type="search" value={value} onChange={(event) => onChange(event.target.value)} placeholder={placeholder} aria-label={t("search")} className={`${ui.input} pl-9`} />
    </div>
  );
}

export function FilterSelect({ label, value, onChange, options }: { label: string; value: string; onChange: (value: string) => void; options: { value: string; label: string }[] }) {
  const t = useTranslations("admin.common");
  return (
    <select aria-label={label} value={value} onChange={(event) => onChange(event.target.value)} className={`${ui.select} py-2`}>
      <option value="">
        {label}: {t("all")}
      </option>
      {options.map((option) => (
        <option key={option.value} value={option.value}>
          {option.label}
        </option>
      ))}
    </select>
  );
}

export function Loading() {
  const t = useTranslations("admin");
  return (
    <p className="flex items-center justify-center gap-2 py-10 text-sm text-lb-muted" role="status">
      <Loader2 className="size-4 animate-spin" aria-hidden />
      {t("loading")}
    </p>
  );
}

export function LoadError({ onRetry }: { onRetry: () => void }) {
  const t = useTranslations("admin.common");
  return (
    <div role="alert" className="flex flex-col items-center gap-3 py-10 text-sm text-lb-muted">
      <AlertTriangle className="size-6 text-lb-red" aria-hidden />
      {t("loadError")}
      <button type="button" onClick={onRetry} className={btn.ghost}>
        {t("retry")}
      </button>
    </div>
  );
}

/** Painel lateral (diálogo nativo): foco preso, `Esc` fecha. */
export function Drawer({ open, onClose, title, children }: { open: boolean; onClose: () => void; title: string; children: React.ReactNode }) {
  const ref = useRef<HTMLDialogElement>(null);
  const titleId = useId();
  const t = useTranslations("admin.common");

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={titleId}
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClick={(event) => {
        if (event.target === ref.current) onClose();
      }}
      className="m-0 ml-auto h-dvh max-h-dvh w-full max-w-lg bg-white p-0 text-lb-ink shadow-[var(--shadow-modal)] backdrop:bg-lb-ink/60"
    >
      {open ? (
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b border-lb-border px-6 py-4">
            <h2 id={titleId} className="text-lg font-extrabold">
              {title}
            </h2>
            <button type="button" onClick={onClose} className={ui.iconButton} aria-label={t("close")}>
              <X className="size-5" aria-hidden />
            </button>
          </div>
          <div className="flex-1 overflow-y-auto px-6 py-5">{children}</div>
        </div>
      ) : null}
    </dialog>
  );
}

type Toast = { id: number; tone: "success" | "error"; text: string };
const ToastContext = createContext<(text: string, tone?: Toast["tone"]) => void>(() => {});

export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([]);
  const notify = useCallback((text: string, tone: Toast["tone"] = "success") => {
    const id = Date.now() + Math.random();
    setToasts((current) => [...current, { id, tone, text }]);
    setTimeout(() => setToasts((current) => current.filter((toast) => toast.id !== id)), 4500);
  }, []);

  return (
    <ToastContext.Provider value={notify}>
      {children}
      <div aria-live="polite" className="pointer-events-none fixed right-4 bottom-4 z-[60] flex w-80 max-w-[calc(100vw-2rem)] flex-col gap-2">
        {toasts.map((toast) => (
          <p
            key={toast.id}
            role={toast.tone === "error" ? "alert" : "status"}
            className={`rounded-lg px-4 py-3 text-sm font-semibold text-white shadow-[var(--shadow-modal)] ${toast.tone === "error" ? "bg-lb-red" : "bg-lb-ink"}`}
          >
            {toast.text}
          </p>
        ))}
      </div>
    </ToastContext.Provider>
  );
}

export const useToast = () => useContext(ToastContext);
