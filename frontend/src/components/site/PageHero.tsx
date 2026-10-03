import type { Locale } from "@/i18n/routing";
import { Breadcrumbs, type Crumb } from "./Breadcrumbs";

/** Faixa escura do topo das páginas internas (continua o cabeçalho), com trilha, título e introdução. */
export function PageHero({ locale, crumbs, title, lead }: { locale: Locale; crumbs: Crumb[]; title: string; lead?: string }) {
  return (
    <section className="bg-gradient-to-b from-lb-slate to-lb-ink pb-12 text-white">
      <div className="mx-auto max-w-6xl px-4 pt-6">
        <Breadcrumbs locale={locale} tone="dark" items={crumbs} />
        <h1 className="mt-6 text-3xl font-extrabold sm:text-4xl">{title}</h1>
        {lead ? <p className="mt-3 max-w-3xl text-white/85">{lead}</p> : null}
      </div>
    </section>
  );
}
