"use client";

import { Pencil, Trash2, UserPlus, UserRoundCheck } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { Badge, btn, Drawer, Field, FilterSelect, Loading, LoadError, Panel, SearchInput, SortHeader, tdClass, thClass, TONES, ui } from "@/components/admin/ui";
import { whatsappUrl } from "@/components/site/WhatsApp";
import { WhatsAppIcon } from "@/components/ui/Icons";
import { useClientTable } from "@/hooks/useClientTable";
import { useDebounce } from "@/hooks/useDebounce";
import { keys, useAdminMutation, useLeads, useProperties } from "@/lib/admin/queries";
import { LEAD_STATUS_TONE } from "@/lib/admin/tones";
import { api } from "@/lib/api/client";
import { LEAD_INTERESTS, LEAD_SOURCES, LEAD_STATUSES, type Lead, type LeadInterest, type LeadStatus } from "@/lib/api/types";
import { formatDate } from "@/lib/format/format";

type LeadValues = { name: string; phone: string; email: string; interest: LeadInterest; propertyId: string; notes: string };
type SortKey = "name" | "interest" | "source" | "status" | "created";

const EMPTY: Lead[] = [];
const blank: LeadValues = { name: "", phone: "", email: "", interest: "BuyHouse", propertyId: "", notes: "" };
const searchText = (lead: Lead) => [lead.name, lead.phone, lead.email, lead.propertyTitle];
const toRequest = (values: LeadValues) => ({
  name: values.name.trim(),
  phone: values.phone.trim(),
  email: values.email.trim() || null,
  interest: values.interest,
  propertyId: values.propertyId || null,
  notes: values.notes.trim() || null,
});

function LeadFields({ values, onChange }: { values: LeadValues; onChange: (values: LeadValues) => void }) {
  const t = useTranslations("admin.leads");
  const ti = useTranslations("admin.interest");
  const properties = useProperties();
  const set = (key: keyof LeadValues, value: string) => onChange({ ...values, [key]: value });
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <Field label={`${t("name")} *`}>{(id) => <input id={id} required value={values.name} onChange={(e) => set("name", e.target.value)} className={ui.input} />}</Field>
      <Field label={`${t("phone")} *`}>
        {(id) => <input id={id} required type="tel" placeholder="+595 981 000 000" value={values.phone} onChange={(e) => set("phone", e.target.value)} className={ui.input} />}
      </Field>
      <Field label={t("email")}>{(id) => <input id={id} type="email" value={values.email} onChange={(e) => set("email", e.target.value)} className={ui.input} />}</Field>
      <Field label={t("interest")}>
        {(id) => (
          <select id={id} value={values.interest} onChange={(e) => set("interest", e.target.value)} className={ui.input}>
            {LEAD_INTERESTS.map((value) => (
              <option key={value} value={value}>
                {ti(value)}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label={t("property")} full>
        {(id) => (
          <select id={id} value={values.propertyId} onChange={(e) => set("propertyId", e.target.value)} className={ui.input}>
            <option value="">{t("noProperty")}</option>
            {(properties.data?.items ?? []).map((property) => (
              <option key={property.id} value={property.id}>
                {property.title}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label={t("notes")} full>
        {(id) => <textarea id={id} rows={3} value={values.notes} onChange={(e) => set("notes", e.target.value)} className={ui.input} />}
      </Field>
    </div>
  );
}

function EditLead({ lead, onClose }: { lead: Lead; onClose: () => void }) {
  const t = useTranslations("admin.leads");
  const tc = useTranslations("admin.common");
  const ts = useTranslations("admin.leadSource");
  const locale = useLocale();
  const [values, setValues] = useState<LeadValues>({
    name: lead.name,
    phone: lead.phone,
    email: lead.email ?? "",
    interest: lead.interest,
    propertyId: lead.propertyId ?? "",
    notes: lead.notes ?? "",
  });
  const save = useAdminMutation(() => api.put(`/api/admin/leads/${lead.id}`, toRequest(values)), { invalidate: [keys.leads], success: t("saved"), onSuccess: onClose });
  const remove = useAdminMutation(() => api.delete(`/api/admin/leads/${lead.id}`), { invalidate: [keys.leads, keys.dashboard], success: tc("saved"), onSuccess: onClose });
  const convert = useAdminMutation(() => api.post(`/api/admin/leads/${lead.id}/convert-to-owner`), { invalidate: [keys.leads, keys.owners], success: t("converted"), onSuccess: onClose });

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate(undefined);
      }}
      className="space-y-5"
    >
      <p className="text-xs text-lb-muted">
        {ts(lead.source)} · {formatDate(lead.createdAt, locale, { dateStyle: "medium", timeStyle: "short" })}
      </p>
      {lead.message ? (
        <div className="rounded-lg bg-lb-bg p-3">
          <p className={ui.label}>{t("message")}</p>
          <p className="mt-1 text-sm whitespace-pre-line">{lead.message}</p>
        </div>
      ) : null}
      <LeadFields values={values} onChange={setValues} />
      <div className="flex flex-wrap gap-2">
        <button type="submit" disabled={save.isPending} className={btn.primary}>
          {save.isPending ? tc("saving") : tc("save")}
        </button>
        {lead.source === "OwnerProposal" ? (
          <button type="button" disabled={convert.isPending} onClick={() => convert.mutate(undefined)} className={btn.ghost}>
            <UserRoundCheck className="size-4" aria-hidden />
            {t("convert")}
          </button>
        ) : null}
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

export default function LeadsPage() {
  const t = useTranslations("admin.leads");
  const ta = useTranslations("admin");
  const locale = useLocale();
  const leads = useLeads();
  const [form, setForm] = useState<LeadValues>(blank);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState("");
  const [source, setSource] = useState("");
  const [editing, setEditing] = useState<Lead | null>(null);
  const term = useDebounce(search);

  const create = useAdminMutation(() => api.post("/api/admin/leads", toRequest(form)), {
    invalidate: [keys.leads, keys.dashboard],
    success: t("saved"),
    onSuccess: () => setForm(blank),
  });
  const changeStatus = useAdminMutation(({ id, next }: { id: string; next: LeadStatus }) => api.put(`/api/admin/leads/${id}/status`, { status: next }), {
    invalidate: [keys.leads, keys.dashboard],
  });

  const filters = useMemo(() => [(lead: Lead) => !status || lead.status === status, (lead: Lead) => !source || lead.source === source], [status, source]);
  const sorters = useMemo(
    () => ({
      name: (a: Lead, b: Lead) => a.name.localeCompare(b.name, locale),
      interest: (a: Lead, b: Lead) => a.interest.localeCompare(b.interest),
      source: (a: Lead, b: Lead) => a.source.localeCompare(b.source),
      status: (a: Lead, b: Lead) => LEAD_STATUSES.indexOf(a.status) - LEAD_STATUSES.indexOf(b.status),
      created: (a: Lead, b: Lead) => a.createdAt.localeCompare(b.createdAt),
    }),
    [locale],
  );
  const table = useClientTable<Lead, SortKey>(leads.data?.items ?? EMPTY, { search: term, searchText, filters, sorters, initialSort: { key: "created", direction: "desc" } });

  return (
    <div className="space-y-6">
      <Panel title={t("form")}>
        <form
          onSubmit={(event) => {
            event.preventDefault();
            create.mutate(undefined);
          }}
        >
          <LeadFields values={form} onChange={setForm} />
          <button type="submit" disabled={create.isPending} className={`${btn.primary} mt-4`}>
            <UserPlus className="size-4" aria-hidden />
            {create.isPending ? ta("common.saving") : t("save")}
          </button>
        </form>
      </Panel>

      <Panel title={t("list")}>
        <div className="mb-4 flex flex-wrap items-center gap-2">
          <SearchInput value={search} onChange={setSearch} placeholder={t("searchPlaceholder")} />
          <FilterSelect label={t("status")} value={status} onChange={setStatus} options={LEAD_STATUSES.map((value) => ({ value, label: ta(`leadStatus.${value}`) }))} />
          <FilterSelect label={t("source")} value={source} onChange={setSource} options={LEAD_SOURCES.map((value) => ({ value, label: ta(`leadSource.${value}`) }))} />
          <span className="ml-auto text-xs text-lb-muted">{ta("common.showing", { count: table.items.length, total: table.total })}</span>
        </div>

        {leads.isPending ? (
          <Loading />
        ) : leads.isError ? (
          <LoadError onRetry={() => leads.refetch()} />
        ) : table.total === 0 ? (
          <p className="py-8 text-center text-sm text-lb-muted">{t("empty")}</p>
        ) : (
          <div className="-mx-5 overflow-x-auto sm:-mx-6">
            <table className="w-full min-w-[820px] border-collapse text-sm">
              <thead>
                <tr>
                  <SortHeader label={t("name")} column="name" sort={table.sort} onSort={table.toggleSort} />
                  <th scope="col" className={thClass}>
                    {t("contact")}
                  </th>
                  <SortHeader label={t("interest")} column="interest" sort={table.sort} onSort={table.toggleSort} />
                  <SortHeader label={t("source")} column="source" sort={table.sort} onSort={table.toggleSort} />
                  <SortHeader label={t("status")} column="status" sort={table.sort} onSort={table.toggleSort} />
                  <th scope="col" className={thClass}>
                    {ta("common.actions")}
                  </th>
                </tr>
              </thead>
              <tbody>
                {table.items.map((lead) => (
                  <tr key={lead.id} className="hover:bg-lb-bg">
                    <td className={tdClass}>
                      <strong>{lead.name}</strong>
                      <p className="text-xs text-lb-muted">{formatDate(lead.createdAt, locale, { dateStyle: "short", timeStyle: "short" })}</p>
                    </td>
                    <td className={tdClass}>
                      <div className="flex items-center gap-2">
                        <a
                          href={whatsappUrl(lead.phone, "")}
                          target="_blank"
                          rel="noopener noreferrer"
                          aria-label={t("whatsapp", { name: lead.name })}
                          title={t("whatsapp", { name: lead.name })}
                          className="text-[#0f7a41] hover:scale-110"
                        >
                          <WhatsAppIcon className="size-5" />
                        </a>
                        <span className="whitespace-nowrap">{lead.phone}</span>
                      </div>
                      {lead.email ? <p className="text-xs text-lb-muted">{lead.email}</p> : null}
                    </td>
                    <td className={tdClass}>
                      {ta(`interest.${lead.interest}`)}
                      {lead.propertyTitle ? <p className="line-clamp-1 text-xs text-lb-muted">{lead.propertyTitle}</p> : null}
                    </td>
                    <td className={tdClass}>
                      <Badge tone={lead.source === "OwnerProposal" ? "purple" : "gray"}>{ta(`leadSource.${lead.source}`)}</Badge>
                    </td>
                    <td className={tdClass}>
                      <label className="sr-only" htmlFor={`status-${lead.id}`}>
                        {t("status")}
                      </label>
                      <select
                        id={`status-${lead.id}`}
                        value={lead.status}
                        onChange={(e) => changeStatus.mutate({ id: lead.id, next: e.target.value as LeadStatus })}
                        className={`cursor-pointer rounded-full border-0 px-2 py-1 text-xs font-extrabold uppercase ${TONES[LEAD_STATUS_TONE[lead.status]]}`}
                      >
                        {LEAD_STATUSES.map((value) => (
                          <option key={value} value={value}>
                            {ta(`leadStatus.${value}`)}
                          </option>
                        ))}
                      </select>
                    </td>
                    <td className={tdClass}>
                      <button type="button" onClick={() => setEditing(lead)} className={ui.iconButton} aria-label={t("edit")} title={t("edit")}>
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
        {editing ? <EditLead key={editing.id} lead={editing} onClose={() => setEditing(null)} /> : null}
      </Drawer>
    </div>
  );
}
