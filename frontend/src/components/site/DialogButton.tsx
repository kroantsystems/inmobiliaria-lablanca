"use client";

import { useSiteDialogs } from "./SiteDialogs";

/** Botão que abre um dos modais do site a partir de componentes de servidor. */
export function DialogButton({ dialog, className, children }: { dialog: "contact" | "owner" | "login"; className?: string; children: React.ReactNode }) {
  const { open } = useSiteDialogs();
  return (
    <button type="button" onClick={() => open(dialog)} className={className}>
      {children}
    </button>
  );
}
