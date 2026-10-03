import { getLocale, getTranslations } from "next-intl/server";
import { PropertyGrid } from "@/components/site/PropertyCard";
import { buttons } from "@/components/ui/buttons";
import { Link } from "@/i18n/navigation";
import { getFeatured, safely } from "@/lib/api/server";

export default async function SiteNotFound() {
  const locale = await getLocale();
  const [t, suggestions] = await Promise.all([getTranslations("errors"), safely(getFeatured(locale, 3), [])]);

  return (
    <section className="mx-auto max-w-6xl px-4 py-14">
      <div className="text-center">
        <p className="font-display text-6xl font-bold text-lb-blue">404</p>
        <h1 className="mt-2 text-3xl font-extrabold">{t("notFoundTitle")}</h1>
        <p className="mt-2 text-lb-muted">{t("notFoundText")}</p>
        <Link href="/" className={`${buttons.red} mt-6`}>
          {t("goHome")}
        </Link>
      </div>
      {suggestions.length > 0 ? (
        <div className="mt-12">
          <h2 className="mb-6 text-2xl font-extrabold">{t("suggestions")}</h2>
          <PropertyGrid properties={suggestions} locale={locale} />
        </div>
      ) : null}
    </section>
  );
}
