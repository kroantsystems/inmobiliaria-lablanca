"use client";

import { ExternalLink, ImageOff, Pencil, Plus, Star } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { AdminImage } from "@/components/admin/AdminImage";
import { Badge, btn, FilterSelect, Loading, LoadError, Panel, SearchInput, SortHeader, tdClass, thClass, ui } from "@/components/admin/ui";
import { useClientTable } from "@/hooks/useClientTable";
import { useDebounce } from "@/hooks/useDebounce";
import { Link, useRouter } from "@/i18n/navigation";
import { keys, useAdminMutation, useProperties, useSettings } from "@/lib/admin/queries";
import { PROPERTY_STATUS_TONE } from "@/lib/admin/tones";
import { api } from "@/lib/api/client";
import { PROPERTY_STATUSES, type AdminProperty, type AdminPropertyListItem } from "@/lib/api/types";
import { convertPrice, formatDate, formatPrice } from "@/lib/format/format";

type SortKey = "title" | "operation" | "price" | "status" | "updated" | "media";
const EMPTY: AdminPropertyListItem[] = [];
const searchText = (item: AdminPropertyListItem) => [item.title, item.zoneName, item.city, item.ownerName];

export default function PropertiesPage() {
  const t = useTranslations("admin.properties");
  const ta = useTranslations("admin");
  const tp = useTranslations("property");
  const locale = useLocale();
  const router = useRouter();
  const properties = useProperties();
  const settings = useSettings();
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [operation, setOperation] = useState("");
  const [published, setPublished] = useState("");
  const term = useDebounce(search);

  const filters = useMemo(
    () => [
      (item: AdminPropertyListItem) => !status || item.status === status,
      (item: AdminPropertyListItem) => !operation || item.operation === operation,
      (item: AdminPropertyListItem) => !published || String(item.isPublished) === published,
    ],
    [status, operation, published],
  );

  // Preço comparado em dólares para misturar USD, PYG e BRL.
  const sorters = useMemo(() => {
    const rates = { pygPerUsd: settings.data?.pygPerUsd ?? 7500, brlPerUsd: settings.data?.brlPerUsd ?? 5 };
    const usd = (item: AdminPropertyListItem) => convertPrice(item.price, item.currency, "USD", rates);
    return {
      title: (a: AdminPropertyListItem, b: AdminPropertyListItem) => a.title.localeCompare(b.title, locale),
      operation: (a: AdminPropertyListItem, b: AdminPropertyListItem) => a.operation.localeCompare(b.operation),
      price: (a: AdminPropertyListItem, b: AdminPropertyListItem) => usd(a) - usd(b),
      status: (a: AdminPropertyListItem, b: AdminPropertyListItem) => PROPERTY_STATUSES.indexOf(a.status) - PROPERTY_STATUSES.indexOf(b.status),
      updated: (a: AdminPropertyListItem, b: AdminPropertyListItem) => (a.updatedAt ?? a.createdAt).localeCompare(b.updatedAt ?? b.createdAt),
      media: (a: AdminPropertyListItem, b: AdminPropertyListItem) => a.mediaCount - b.mediaCount,
    };
  }, [settings.data, locale]);

  const table = useClientTable<AdminPropertyListItem, SortKey>(properties.data?.items ?? EMPTY, {
    search: term,
    searchText,
    filters,
    sorters,
    initialSort: { key: "updated", direction: "desc" },
  });

  const toggle = useAdminMutation(({ id, action }: { id: string; action: "publish" | "unpublish" | "feature" | "unfeature" }) =>
    action === "feature" || action === "unfeature"
      ? api.put<AdminProperty>(`/api/admin/properties/${id}/featured`, { featured: action === "feature" })
      : api.post<AdminProperty>(`/api/admin/properties/${id}/${action}`), { invalidate: [keys.properties, keys.dashboard] });

  const columns: { key: SortKey; label: string }[] = [
    { key: "title", label: t("columns.title") },
    { key: "operation", label: t("columns.operation") },
    { key: "price", label: t("columns.price") },
    { key: "status", label: t("columns.status") },
    { key: "media", label: t("columns.media") },
    { key: "updated", label: t("columns.updated") },
  ];

  return (
    <Panel
      title={t("list")}
      actions={
        <Link href={{ pathname: "/admin/properties/[id]", params: { id: "new" } }} className={btn.danger}>
          <Plus className="size-4" aria-hidden />
          {t("new")}
        </Link>
      }
    >
      <div className="mb-4 flex flex-wrap items-center gap-2">
        <SearchInput value={search} onChange={setSearch} placeholder={t("searchPlaceholder")} />
        <FilterSelect label={t("filterStatus")} value={status} onChange={setStatus} options={PROPERTY_STATUSES.map((value) => ({ value, label: ta(`propertyStatus.${value}`) }))} />
        <FilterSelect
          label={t("filterOperation")}
          value={operation}
          onChange={setOperation}
          options={(["Sale", "Rent"] as const).map((value) => ({ value, label: tp(`operation.${value}`) }))}
        />
        <FilterSelect
          label={t("filterPublished")}
          value={published}
          onChange={setPublished}
          options={[
            { value: "true", label: t("published") },
            { value: "false", label: t("unpublished") },
          ]}
        />
        <span className="ml-auto text-xs text-lb-muted">{ta("common.showing", { count: table.items.length, total: table.total })}</span>
      </div>

      {properties.isPending ? (
        <Loading />
      ) : properties.isError ? (
        <LoadError onRetry={() => properties.refetch()} />
      ) : table.total === 0 ? (
        <p className="py-8 text-center text-sm text-lb-muted">{t("empty")}</p>
      ) : (
        <div className="-mx-5 overflow-x-auto sm:-mx-6">
          <table className="w-full min-w-[860px] border-collapse text-sm">
            <thead>
              <tr>
                {columns.map((column) => (
                  <SortHeader key={column.key} label={column.label} column={column.key} sort={table.sort} onSort={table.toggleSort} />
                ))}
                <th scope="col" className={thClass}>
                  {ta("common.actions")}
                </th>
              </tr>
            </thead>
            <tbody>
              {table.items.map((item) => (
                <tr key={item.id} className="cursor-pointer hover:bg-lb-bg" onClick={() => router.push({ pathname: "/admin/properties/[id]", params: { id: item.id } })}>
                  <td className={tdClass}>
                    <div className="flex items-center gap-3">
                      <div className="relative h-12 w-16 shrink-0 overflow-hidden rounded-md bg-lb-panel">
                        {item.coverUrl ? (
                          <AdminImage publicUrl={item.coverUrl} published={item.isPublished} alt="" sizes="64px" />
                        ) : (
                          <ImageOff className="m-auto mt-3.5 size-5 text-lb-muted" aria-hidden />
                        )}
                      </div>
                      <div className="min-w-0">
                        <Link
                          href={{ pathname: "/admin/properties/[id]", params: { id: item.id } }}
                          className="line-clamp-1 font-bold hover:text-lb-blue"
                          onClick={(event) => event.stopPropagation()}
                        >
                          {item.title}
                        </Link>
                        <p className="text-xs text-lb-muted">{[item.zoneName, item.city].filter(Boolean).join(" · ")}</p>
                      </div>
                    </div>
                  </td>
                  <td className={tdClass}>
                    {tp(`operation.${item.operation}`)}
                    <p className="text-xs text-lb-muted">{tp(`type.${item.type}`)}</p>
                  </td>
                  <td className={`${tdClass} font-bold whitespace-nowrap`}>{formatPrice(item.price, item.currency, locale)}</td>
                  <td className={tdClass}>
                    <div className="flex flex-wrap gap-1">
                      <Badge tone={PROPERTY_STATUS_TONE[item.status]}>{ta(`propertyStatus.${item.status}`)}</Badge>
                      <Badge tone={item.isPublished ? "green" : "gray"}>{item.isPublished ? t("published") : t("unpublished")}</Badge>
                    </div>
                  </td>
                  <td className={tdClass}>{item.mediaCount}</td>
                  <td className={`${tdClass} whitespace-nowrap text-lb-muted`}>{formatDate(item.updatedAt ?? item.createdAt, locale, { dateStyle: "short" })}</td>
                  <td className={tdClass} onClick={(event) => event.stopPropagation()}>
                    <div className="flex items-center gap-1">
                      <button
                        type="button"
                        className={`${ui.iconButton} ${item.isFeatured ? "text-amber-500" : ""}`}
                        aria-pressed={item.isFeatured}
                        title={item.isFeatured ? t("unfeature") : t("feature")}
                        aria-label={item.isFeatured ? t("unfeature") : t("feature")}
                        onClick={() => toggle.mutate({ id: item.id, action: item.isFeatured ? "unfeature" : "feature" })}
                      >
                        <Star className="size-4" fill={item.isFeatured ? "currentColor" : "none"} aria-hidden />
                      </button>
                      <button
                        type="button"
                        className="rounded-lg border border-lb-border px-2.5 py-1.5 text-xs font-bold hover:border-lb-blue hover:text-lb-blue"
                        onClick={() => toggle.mutate({ id: item.id, action: item.isPublished ? "unpublish" : "publish" })}
                      >
                        {item.isPublished ? t("unpublish") : t("publish")}
                      </button>
                      <Link href={{ pathname: "/admin/properties/[id]", params: { id: item.id } }} className={ui.iconButton} aria-label={ta("common.edit")} title={ta("common.edit")}>
                        <Pencil className="size-4" aria-hidden />
                      </Link>
                      {item.isPublished ? (
                        <Link
                          href={{ pathname: "/properties/[slug]", params: { slug: item.slug } }}
                          locale="es"
                          target="_blank"
                          className={ui.iconButton}
                          aria-label={t("viewOnSite")}
                          title={t("viewOnSite")}
                        >
                          <ExternalLink className="size-4" aria-hidden />
                        </Link>
                      ) : null}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {table.items.length === 0 ? <p className="py-8 text-center text-sm text-lb-muted">{ta("common.noResults")}</p> : null}
        </div>
      )}
    </Panel>
  );
}

