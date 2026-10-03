"use client";

import { MapPin } from "lucide-react";
import Image from "next/image";
import { useLocale, useTranslations } from "next-intl";
import { useEffect, useState } from "react";
import { Price } from "@/components/site/CurrencyProvider";
import { buttons } from "@/components/ui/buttons";
import { Link } from "@/i18n/navigation";
import type { PropertyCard } from "@/lib/api/types";
import { placeOf, propertyHref } from "@/lib/property-links";

/**
 * Anúncio vendido, despublicado ou inexistente: 404 com outras opções.
 * Componente de cliente porque o Next renderiza este arquivo junto com a página estática (ISR), onde ler o idioma
 * pelo servidor exigiria `headers()`; as sugestões vêm da API pelo navegador.
 */
export default function PropertyNotFound() {
  const t = useTranslations("property");
  const locale = useLocale();
  const [suggestions, setSuggestions] = useState<PropertyCard[]>([]);

  useEffect(() => {
    fetch(`/api/public/properties/featured?locale=${locale}&take=3`)
      .then((response) => (response.ok ? (response.json() as Promise<PropertyCard[]>) : []))
      .then(setSuggestions)
      .catch(() => setSuggestions([]));
  }, [locale]);

  return (
    <section className="mx-auto max-w-6xl px-4 py-14">
      <div className="text-center">
        <h1 className="text-3xl font-extrabold">{t("notFoundTitle")}</h1>
        <p className="mt-2 text-lb-muted">{t("notFoundText")}</p>
        <Link href="/properties" className={`${buttons.blue} mt-6`}>
          {t("backToCatalog")}
        </Link>
      </div>
      {suggestions.length > 0 ? (
        <div className="mt-12">
          <h2 className="mb-6 text-2xl font-extrabold">{t("otherOptions")}</h2>
          <ul className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {suggestions.map((property) => (
              <li key={property.id} className="relative overflow-hidden rounded-[var(--radius-card)] border border-lb-border bg-white shadow-[var(--shadow-card)]">
                <div className="relative aspect-[4/3] bg-lb-panel">
                  {property.cover ? <Image src={property.cover.url} alt={property.cover.alt} fill sizes="(min-width: 1024px) 380px, 100vw" className="object-cover" /> : null}
                </div>
                <div className="p-5">
                  <p className="flex items-center gap-1 text-xs font-extrabold text-lb-blue uppercase">
                    <MapPin className="size-3.5" aria-hidden />
                    {placeOf(property)}
                  </p>
                  <h3 className="mt-1 mb-2 text-lg font-bold">
                    <Link href={propertyHref(property.slug)} className="after:absolute after:inset-0">
                      {property.title}
                    </Link>
                  </h3>
                  <Price amount={property.price} currency={property.currency} perMonth={property.operation === "Rent"} className="text-xl font-extrabold text-lb-blue" />
                </div>
              </li>
            ))}
          </ul>
        </div>
      ) : null}
    </section>
  );
}
