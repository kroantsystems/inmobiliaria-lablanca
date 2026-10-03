"use client";

import { Pencil, Trash2, UserRoundCheck, UserRoundPlus } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { Badge, btn, Drawer, Field, Loading, LoadError, Panel, SearchInput, SortHeader, tdClass, thClass, ui } from "@/components/admin/ui";
import { whatsappUrl } from "@/components/site/WhatsApp";
import { WhatsAppIcon } from "@/components/ui/Icons";
import { useClientTable } from "@/hooks/useClientTable";
import { useDebounce } from "@/hooks/useDebounce";
import { Link } from "@/i18n/navigation";
import { keys, useAdminMutation, useLeads, useOwners } from "@/lib/admin/queries";
import { PROPERTY_STATUS_TONE } from "@/lib/admin/tones";
import { api } from "@/lib/api/client";
import type { Owner } from "@/lib/api/types";
import { formatDate } from "@/lib/format/format";

type OwnerValues = { name: string; phone: string; email: string; document: string; notes: string };
type SortKey = "name" | "properties" | "created";

const EMPTY: Owner[] = [];
const blank: OwnerValues = { name: "", phone: "", email: "", document: "", notes: "" };
const searchText = (owner: Owner) => [owner.name, owner.phone, owner.email, owner.document, ...owner.properties.map((p) => p.title)];
const toRequest = (values: OwnerValues) => ({
  name: values.name.trim(),
  phone: values.phone.trim(),
  email: values.email.trim() || null,
  document: values.document.trim() || null,
  notes: values.notes.trim() || null,
});

function OwnerFields({ values, onChange }: { values: OwnerValues; onChange: (values: OwnerValues) => void }) {
  const t = useTranslations("admin.owners");
  const set = (key: keyof OwnerValues, value: string) => onChange({ ...values, [key]: value });
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label={`${t("name")} *`}>{(id) => <input id={id} required value={values.name} onChange={(e) => set("name", e.target.value)} className={ui.input} />}</Field>
      <Field label={`${t("phone")} *`}>
        {(id) => <input id={id} required type="tel" placeholder="+595 983 000 000" value={values.phone} onChange={(e) => set("phone", e.target.value)} className={ui.input} />}
      </Field>
      <Field label={t("email")}>{(id) => <input id={id} type="email" value={values.email} onChange={(e) => set("email", e.target.value)} className={ui.input} />}</Field>
      <Field label={t("document")}>{(id) => <input id={id} value={values.document} onChange={(e) => set("document", e.target.value)} className={ui.input} />}</Field>
      <Field label={t("notes")} full>
        {(id) => <textarea id={id} rows={3} value={values.notes} onChange={(e) => set("notes", e.target.value)} className={ui.input} />}
      </Field>
    </div>
  );
}

function EditOwner({ owner, onClose }: { owner: Owner; onClose: () => void }) {
  const t = useTranslations("admin.owners");
  const tc = useTranslations("admin.common");
  const [values, setValues] = useState<OwnerValues>({
    name: owner.name,
    phone: owner.phone,
    email: owner.email ?? "",
    document: owner.document ?? "",
    notes: owner.notes ?? "",
  });
  const save = useAdminMutation(() => api.put(`/api/admin/owners/${owner.id}`, toRequest(values)), { invalidate: [keys.owners], success: t("saved"), onSuccess: onClose });
  const remove = useAdminMutation(() => api.delete(`/api/admin/owners/${owner.id}`), { invalidate: [keys.owners, keys.properties], success: tc("saved"), onSuccess: onClose });

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate(undefined);
      }}
      className="space-y-5"
    >
      <OwnerFields values={values} onChange={setValues} />
      <div className="flex flex-wrap gap-2">
        <button type="submit" disabled={save.isPending} className={btn.primary}>
          {save.isPending ? tc("saving") : tc("save")}
        </button>
        <button
          type="button"
          disabled={remove.isPending}
          onClick={() => {
            if (window.confirm(tc("confirmDelete"))) remove.mutate(undefined);
          }}
          className={`${btn.ghost} ml-auto text-lb-red`}
        >
          <Trash2 className="size-4" aria-hidden />
          {tc("delete")}
        </button>
      </div>
    </form>
  );
}

export default function OwnersPage() {
  const t = useTranslations("admin.owners");
  const ta = useTranslations("admin");
  const locale = useLocale();
  const owners = useOwners();
  const leads = useLeads();
  const [form, setForm] = useState<OwnerValues>(blank);
  const [search, setSearch] = useState("");
  const [editing, setEditing] = useState<Owner | null>(null);
  const term = useDebounce(search);

  const create = useAdminMutation(() => api.post("/api/admin/owners", toRequest(form)), { invalidate: [keys.owners], success: t("saved"), onSuccess: () => setForm(blank) });
  const convert = useAdminMutation((leadId: string) => api.post(`/api/admin/leads/${leadId}/convert-to-owner`), {
    invalidate: [keys.owners, keys.leads],
    success: ta("leads.converted"),
  });

  const proposals = (leads.data?.items ?? []).filter((lead) => lead.source === "OwnerProposal" && lead.status !== "Won" && lead.status !== "Lost");
  const sorters = useMemo(
    () => ({
      name: (a: Owner, b: Owner) => a.name.localeCompare(b.name, locale),
      properties: (a: Owner, b: Owner) => a.properties.length - b.properties.length,
      created: (a: Owner, b: Owner) => a.createdAt.localeCompare(b.createdAt),
    }),
    [locale],
  );
  const table = useClientTable<Owner, SortKey>(owners.data?.items ?? EMPTY, { search: term, searchText, sorters, initialSort: { key: "name", direction: "asc" } });

  return (
    <div className="space-y-6">
      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_380px]">
        <Panel title={t("form")}>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              create.mutate(undefined);
            }}
          >
            <OwnerFields values={form} onChange={setForm} />
            <button type="submit" disabled={create.isPending} className={`${btn.primary} mt-4`}>
              <UserRoundPlus className="size-4" aria-hidden />
              {create.isPending ? ta("common.saving") : t("save")}
            </button>
          </form>
        </Panel>

        <Panel title={t("proposals")}>
          <p className="mb-3 text-xs text-lb-muted">{t("proposalsHint")}</p>
          {leads.isPending ? (
            <Loading />
          ) : proposals.length === 0 ? (
            <p className="text-sm text-lb-muted">{t("noProposals")}</p>
          ) : (
            <ul className="space-y-3">
              {proposals.map((lead) => (
                <li key={lead.id} className="rounded-r-lg border-l-4 border-violet-500 bg-lb-bg p-3">
                  <div className="flex items-center justify-between gap-2">
                    <strong className="text-sm">{lead.name}</strong>
                    <a href={whatsappUrl(lead.phone, "")} target="_blank" rel="noopener noreferrer" className="text-[#0f7a41]" aria-label={ta("leads.whatsapp", { name: lead.name })}>
                      <WhatsAppIcon className="size-5" />
                    </a>
                  </div>
                  <p className="text-xs text-lb-muted">
                    {lead.phone} · {formatDate(lead.createdAt, locale, { dateStyle: "short" })}
                  </p>
                  {lead.message ? <p className="mt-1 line-clamp-3 text-xs">{lead.message}</p> : null}
                  <button type="button" disabled={convert.isPending} onClick={() => convert.mutate(lead.id)} className={`${btn.ghost} mt-2 w-full py-1.5 text-xs`}>
                    <UserRoundCheck className="size-4" aria-hidden />
                    {ta("leads.convert")}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>

      <Panel title={t("list")}>
        <div className="mb-4 flex flex-wrap items-center gap-2">
          <SearchInput value={search} onChange={setSearch} placeholder={t("searchPlaceholder")} />
          <span className="ml-auto text-xs text-lb-muted">{ta("common.showing", { count: table.items.length, total: table.total })}</span>
        </div>
        {owners.isPending ? (
          <Loading />
        ) : owners.isError ? (
          <LoadError onRetry={() => owners.refetch()} />
        ) : table.total === 0 ? (
          <p className="py-8 text-center text-sm text-lb-muted">{t("empty")}</p>
        ) : (
          <div className="-mx-5 overflow-x-auto sm:-mx-6">
            <table className="w-full min-w-[720px] border-collapse text-sm">
              <thead>
                <tr>
                  <SortHeader label={t("name")} column="name" sort={table.sort} onSort={table.toggleSort} />
                  <th scope="col" className={thClass}>
                    {t("phone")}
                  </th>
                  <SortHeader label={t("properties")} column="properties" sort={table.sort} onSort={table.toggleSort} />
                  <th scope="col" className={thClass}>
                    {ta("common.actions")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {table.items.map((owner) => (
                  <tr key={owner.id} className="hover:bg-lb-bg">
                    <td className={tdClass}>
                      <strong>{owner.name}</strong>
                      {owner.document ? <p className="text-xs text-lb-muted">{owner.document}</p> : null}
                    </td>
                    <td className={tdClass}>
                      <div className="flex items-center gap-2">
                        <a href={whatsappUrl(owner.phone, "")} target="_blank" rel="noopener noreferrer" className="text-[#0f7a41]" aria-label={ta("leads.whatsapp", { name: owner.name })}>
                          <WhatsAppIcon className="size-5" />
                        </a>
                        <span className="whitespace-nowrap">{owner.phone}</span>
                      </div>
                      {owner.email ? <p className="text-xs text-lb-muted">{owner.email}</p> : null}
                    </td>
                    <td className={tdClass}>
                      {owner.properties.length === 0 ? (
                        <span className="text-xs text-lb-muted">{t("noProperties")}</span>
                      ) : (
                        <ul className="space-y-1">
                          {owner.properties.map((property) => (
                            <li key={property.id} className="flex flex-wrap items-center gap-2">
                              <Link href={{ pathname: "/admin/properties/[id]", params: { id: property.id } }} className="hover:text-lb-blue">
                                {property.title}
                              </Link>
                              <Badge tone={PROPERTY_STATUS_TONE[property.status]}>{ta(`propertyStatus.${property.status}`)}</Badge>
                            </li>
                          ))}
                        </ul>
                      )}
                    </td>
                    <td className={tdClass}>
                      <button type="button" onClick={() => setEditing(owner)} className={ui.iconButton} aria-label={t("edit")} title={t("edit")}>
                        <Pencil className="size-4" aria-hidden />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            {table.items.length === 0 ? <p className="py-8 text-center text-sm text-lb-muted">{ta("common.noResults")}</p> : null}
          </div>
        )}
      </Panel>

      <Drawer open={editing !== null} onClose={() => setEditing(null)} title={t("edit")}>
        {editing ? <EditOwner key={editing.id} owner={editing} onClose={() => setEditing(null)} /> : null}
      </Drawer>
    </div>
  );
}
