"use client";

import { Archive, ExternalLink, Eye, EyeOff, Star } from "lucide-react";
import { useTranslations } from "next-intl";
import { getPathname, Link } from "@/i18n/navigation";
import { useRouter } from "@/i18n/navigation";
import { keys, useAdminMutation } from "@/lib/admin/queries";
import { PROPERTY_STATUS_TONE } from "@/lib/admin/tones";
import { api } from "@/lib/api/client";
import { LISTED_STATUSES, PROPERTY_STATUSES, type AdminProperty, type PropertyStatus } from "@/lib/api/types";
import { Badge, btn, Field, Panel, ui } from "./ui";

/** Status, publicação, destaque e arquivamento do anúncio. */
export function PropertyActions({ property }: { property: AdminProperty }) {
  const t = useTranslations("admin.properties");
  const ta = useTranslations("admin");
  const router = useRouter();
  const invalidate = [keys.property(property.id), keys.properties, keys.dashboard];
  const spanishSlug = property.translations.find((item) => item.locale === "es")?.slug ?? property.translations[0]?.slug;
  const canPublish = LISTED_STATUSES.includes(property.status);

  const status = useAdminMutation((next: PropertyStatus) => api.put(`/api/admin/properties/${property.id}/status`, { status: next }), { invalidate, success: ta("common.saved") });
  const publish = useAdminMutation(() => api.post(`/api/admin/properties/${property.id}/${property.isPublished ? "unpublish" : "publish"}`), {
    invalidate,
    success: ta("common.saved"),
  });
  const feature = useAdminMutation(() => api.put(`/api/admin/properties/${property.id}/featured`, { featured: !property.isFeatured }), { invalidate, success: ta("common.saved") });
  const archive = useAdminMutation(() => api.post(`/api/admin/properties/${property.id}/archive`), {
    invalidate,
    success: ta("common.saved"),
    onSuccess: () => router.push("/admin/properties"),
  });

  return (
    <Panel title={t("form.sectionActions")}>
      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Badge tone={PROPERTY_STATUS_TONE[property.status]}>{ta(`propertyStatus.${property.status}`)}</Badge>
        <Badge tone={property.isPublished ? "green" : "gray"}>{property.isPublished ? t("form.isPublished") : t("form.notPublished")}</Badge>
        {property.isFeatured ? <Badge tone="amber">{t("featured")}</Badge> : null}
      </div>

      {property.status !== "Archived" ? (
        <Field label={t("changeStatus")}>
          {(id) => (
            <select id={id} value={property.status} disabled={status.isPending} onChange={(e) => status.mutate(e.target.value as PropertyStatus)} className={ui.input}>
              {PROPERTY_STATUSES.filter((value) => value !== "Archived").map((value) => (
                <option key={value} value={value}>
                  {ta(`propertyStatus.${value}`)}
                </option>
              ))}
            </select>
          )}
        </Field>
      ) : null}

      <div className="mt-4 grid gap-2">
        <button type="button" disabled={publish.isPending || (!property.isPublished && !canPublish)} onClick={() => publish.mutate(undefined)} className={property.isPublished ? btn.ghost : btn.danger}>
          {property.isPublished ? <EyeOff className="size-4" aria-hidden /> : <Eye className="size-4" aria-hidden />}
          {property.isPublished ? t("unpublish") : t("publish")}
        </button>
        {!property.isPublished && !canPublish ? <p className="text-xs text-lb-muted">{t("form.publishHint")}</p> : null}
        <button type="button" disabled={feature.isPending} onClick={() => feature.mutate(undefined)} className={btn.ghost} aria-pressed={property.isFeatured}>
          <Star className={`size-4 ${property.isFeatured ? "text-amber-500" : ""}`} fill={property.isFeatured ? "currentColor" : "none"} aria-hidden />
          {property.isFeatured ? t("unfeature") : t("feature")}
        </button>
        {property.isPublished && spanishSlug ? (
          <Link href={{ pathname: "/properties/[slug]", params: { slug: spanishSlug } }} locale="es" target="_blank" className={btn.ghost}>
            <ExternalLink className="size-4" aria-hidden />
            {t("viewOnSite")}
          </Link>
        ) : null}
        {property.status !== "Archived" ? (
          <button
            type="button"
            disabled={archive.isPending}
            onClick={() => {
              if (window.confirm(t("confirmArchive"))) archive.mutate(undefined);
            }}
            className={`${btn.ghost} text-lb-red`}
          >
            <Archive className="size-4" aria-hidden />
            {t("archive")}
          </button>
        ) : null}
      </div>

      {spanishSlug ? (
        <p className="mt-4 text-xs break-all text-lb-muted">
          {t("form.sitePath", { path: getPathname({ locale: "es", href: { pathname: "/properties/[slug]", params: { slug: spanishSlug } } }) })}
        </p>
      ) : null}
    </Panel>
  );
}
