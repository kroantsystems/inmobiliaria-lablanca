import { ChevronDown } from "lucide-react";

export type FaqItem = { question: string; answer: string };

/** Perguntas em `<details>`: respostas presentes no HTML (lidas por buscadores e assistentes) e funcionais sem JavaScript. */
export function FaqList({ items }: { items: FaqItem[] }) {
  return (
    <div className="divide-y divide-lb-border overflow-hidden rounded-[var(--radius-card)] border border-lb-border bg-white shadow-[var(--shadow-card)]">
      {items.map((item) => (
        <details key={item.question} className="group">
          <summary className="flex cursor-pointer list-none items-center justify-between gap-4 px-5 py-4 font-bold text-lb-ink transition hover:bg-lb-bg [&::-webkit-details-marker]:hidden">
            <h3 className="text-base">{item.question}</h3>
            <ChevronDown className="size-5 shrink-0 text-lb-blue transition group-open:rotate-180" aria-hidden />
          </summary>
          <p className="px-5 pb-5 text-sm leading-relaxed text-lb-muted">{item.answer}</p>
        </details>
      ))}
    </div>
  );
}
