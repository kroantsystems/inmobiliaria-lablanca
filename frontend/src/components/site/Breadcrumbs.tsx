import { ChevronRight } from "lucide-react";
import { getTranslations } from "next-intl/server";
import { JsonLd } from "@/components/seo/JsonLd";
import { Link } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { breadcrumbList } from "@/lib/seo/jsonld";
import { localizedUrl, type Href } from "@/lib/seo/metadata";

export type Crumb = { name: string; href: Href };

/** Trilha visível + JSON-LD `BreadcrumbList`. O início é incluído automaticamente. */
export async function Breadcrumbs({ locale, items, tone = "light" }: { locale: Locale; items: Crumb[]; tone?: "light" | "dark" }) {
  const t = await getTranslations("nav");
  const all: Crumb[] = [{ name: t("home"), href: "/" }, ...items];
  const muted = tone === "dark" ? "text-white/70 hover:text-white" : "text-lb-muted hover:text-lb-blue";
  const current = tone === "dark" ? "text-white" : "text-lb-ink";

  return (
    <>
      <nav aria-label={t("breadcrumb")} className="text-xs font-semibold">
        <ol className="flex flex-wrap items-center gap-1">
          {all.map((crumb, index) => {
            const last = index === all.length - 1;
            return (
              <li key={index} className="flex items-center gap-1">
                {last ? (
                  <span aria-current="page" className={`line-clamp-1 ${current}`}>
                    {crumb.name}
                  </span>
                ) : (
                  <>
                    <Link href={crumb.href} className={`transition ${muted}`}>
                      {crumb.name}
                    </Link>
                    <ChevronRight className={`size-3.5 ${tone === "dark" ? "text-white/50" : "text-lb-muted"}`} aria-hidden />
                  </>
                )}
              </li>
            );
          })}
        </ol>
      </nav>
      <JsonLd data={breadcrumbList(all.map((crumb) => ({ name: crumb.name, url: localizedUrl(locale, crumb.href) })))} />
    </>
  );
}
