"use client";

import { X } from "lucide-react";
import { useTranslations } from "next-intl";
import { useEffect, useRef } from "react";

type ModalProps = {
  open: boolean;
  onClose: () => void;
  labelledBy: string;
  children: React.ReactNode;
  size?: "sm" | "md";
};

/**
 * Diálogo modal nativo: o navegador deixa o resto da página inerte (foco não sai do modal) e `Esc` fecha.
 * O primeiro campo com autoFocus recebe o foco ao abrir.
 */
export function Modal({ open, onClose, labelledBy, children, size = "md" }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null);
  const t = useTranslations("common");

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      aria-labelledby={labelledBy}
      onClose={onClose}
      onCancel={(event) => {
        event.preventDefault();
        onClose();
      }}
      onClick={(event) => {
        if (event.target === ref.current) onClose();
      }}
      className={`${size === "sm" ? "max-w-md" : "max-w-xl"} m-auto w-[calc(100%-2rem)] rounded-[var(--radius-panel)] border border-lb-border bg-white p-0 text-lb-ink shadow-[var(--shadow-modal)] backdrop:bg-lb-ink/75 backdrop:backdrop-blur-sm`}
    >
      {open ? (
        <div className="relative max-h-[90dvh] overflow-y-auto p-6 sm:p-8">
          <button
            type="button"
            onClick={onClose}
            className="absolute top-4 right-4 rounded-full p-2 text-lb-muted transition hover:bg-lb-panel hover:text-lb-ink"
            aria-label={t("close")}
          >
            <X className="size-5" aria-hidden />
          </button>
          {children}
        </div>
      ) : null}
    </dialog>
  );
}
