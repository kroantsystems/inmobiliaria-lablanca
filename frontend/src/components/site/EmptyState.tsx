import { Building2 } from "lucide-react";

/** Estado vazio com ações (contato, limpar filtros…), sem quebrar o layout da página. */
export function EmptyState({ title, text, children }: { title: string; text: string; children?: React.ReactNode }) {
  return (
    <div className="rounded-[var(--radius-panel)] border border-dashed border-lb-border bg-white px-6 py-12 text-center shadow-[var(--shadow-card)]">
      <Building2 className="mx-auto size-10 text-lb-blue" aria-hidden />
      <h3 className="mt-4 text-xl font-extrabold">{title}</h3>
      <p className="mx-auto mt-2 max-w-md text-sm text-lb-muted">{text}</p>
      {children ? <div className="mt-6 flex flex-wrap justify-center gap-3">{children}</div> : null}
    </div>
  );
}
