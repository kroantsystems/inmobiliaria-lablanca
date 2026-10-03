"use client";

import { useQuery } from "@tanstack/react-query";
import { CalendarPlus, ChevronLeft, ChevronRight } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useState } from "react";
import { Badge, btn, Field, Loading, LoadError, Panel, ui } from "@/components/admin/ui";
import { Modal } from "@/components/ui/Modal";
import { currentMonth, dayKey, gridRange, monthGrid, shiftMonth, timeKey, zonedToUtcIso, type MonthRef } from "@/lib/admin/calendar";
import { apiGet, keys, useAdminMutation, useLeads, useProperties, useUpcomingVisits } from "@/lib/admin/queries";
import { VISIT_STATUS_FILL, VISIT_STATUS_TONE } from "@/lib/admin/tones";
import { api } from "@/lib/api/client";
import { VISIT_STATUSES, type Visit, type VisitStatus } from "@/lib/api/types";
import { formatDate, intlLocale, TIME_ZONE } from "@/lib/format/format";

type VisitValues = { propertyId: string; leadId: string; clientName: string; date: string; time: string; duration: string; notes: string };

const MAX_PER_DAY = 3;
const today = () => dayKey(new Date().toISOString(), TIME_ZONE);

function toRequest(values: VisitValues, clientNameFromLead?: string) {
  return {
    propertyId: values.propertyId,
    leadId: values.leadId || null,
    clientName: values.leadId ? (clientNameFromLead ?? values.clientName) : values.clientName.trim(),
    startsAt: zonedToUtcIso(values.date, values.time, TIME_ZONE),
    durationMinutes: Number(values.duration) || 60,
    notes: values.notes.trim() || null,
  };
}

function VisitFields({ values, onChange }: { values: VisitValues; onChange: (values: VisitValues) => void }) {
  const t = useTranslations("admin.calendar");
  const properties = useProperties();
  const leads = useLeads();
  const set = (key: keyof VisitValues, value: string) => onChange({ ...values, [key]: value });
  const options = (properties.data?.items ?? []).filter((property) => property.status !== "Archived");

  return (
    <div className="grid gap-4">
      <Field label={`${t("property")} *`}>
        {(id) => (
          <select id={id} required value={values.propertyId} onChange={(e) => set("propertyId", e.target.value)} className={ui.input}>
            <option value="">{t("chooseProperty")}</option>
            {options.map((property) => (
              <option key={property.id} value={property.id}>
                {property.title}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label={t("lead")}>
        {(id) => (
          <select id={id} value={values.leadId} onChange={(e) => set("leadId", e.target.value)} className={ui.input}>
            <option value="">{t("noLead")}</option>
            {(leads.data?.items ?? []).map((lead) => (
              <option key={lead.id} value={lead.id}>
                {lead.name} · {lead.phone}
              </option>
            ))}
          </select>
        )}
      </Field>
      {values.leadId ? null : (
        <Field label={`${t("client")} *`}>
          {(id) => <input id={id} required placeholder="Carlos Mendoza" value={values.clientName} onChange={(e) => set("clientName", e.target.value)} className={ui.input} />}
        </Field>
      )}
      <div className="grid grid-cols-3 gap-3">
        <Field label={`${t("date")} *`}>{(id) => <input id={id} type="date" required value={values.date} onChange={(e) => set("date", e.target.value)} className={ui.input} />}</Field>
        <Field label={`${t("time")} *`}>{(id) => <input id={id} type="time" required value={values.time} onChange={(e) => set("time", e.target.value)} className={ui.input} />}</Field>
        <Field label={t("duration")}>
          {(id) => <input id={id} type="number" min={15} max={480} step={15} value={values.duration} onChange={(e) => set("duration", e.target.value)} className={ui.input} />}
        </Field>
      </div>
      <Field label={t("notes")}>{(id) => <textarea id={id} rows={2} value={values.notes} onChange={(e) => set("notes", e.target.value)} className={ui.input} />}</Field>
    </div>
  );
}

function VisitDetail({ visit, onClose }: { visit: Visit; onClose: () => void }) {
  const t = useTranslations("admin.calendar");
  const ta = useTranslations("admin");
  const leads = useLeads();
  const [values, setValues] = useState<VisitValues>({
    propertyId: visit.propertyId,
    leadId: visit.leadId ?? "",
    clientName: visit.clientName,
    date: dayKey(visit.startsAt, TIME_ZONE),
    time: timeKey(visit.startsAt, TIME_ZONE),
    duration: String(visit.durationMinutes),
    notes: visit.notes ?? "",
  });
  const leadName = leads.data?.items.find((lead) => lead.id === values.leadId)?.name;
  const save = useAdminMutation(() => api.put(`/api/admin/visits/${visit.id}`, toRequest(values, leadName)), { invalidate: [keys.visits, keys.dashboard], success: t("updated"), onSuccess: onClose });
  const status = useAdminMutation((next: VisitStatus) => api.put(`/api/admin/visits/${visit.id}/status`, { status: next }), { invalidate: [keys.visits, keys.dashboard], success: ta("common.saved") });

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center gap-2">
        <Badge tone={VISIT_STATUS_TONE[visit.status]}>{ta(`visitStatus.${visit.status}`)}</Badge>
        <label className="sr-only" htmlFor="visit-status">
          {t("status")}
        </label>
        <select id="visit-status" value={visit.status} onChange={(e) => status.mutate(e.target.value as VisitStatus)} className={`${ui.select} py-1.5`}>
          {VISIT_STATUSES.map((value) => (
            <option key={value} value={value}>
              {ta(`visitStatus.${value}`)}
            </option>
          ))}
        </select>
      </div>
      <form
        onSubmit={(event) => {
          event.preventDefault();
          save.mutate(undefined);
        }}
        className="space-y-4"
      >
        <VisitFields values={values} onChange={setValues} />
        <button type="submit" disabled={save.isPending} className={btn.primary}>
          {save.isPending ? ta("common.saving") : t("reschedule")}
        </button>
      </form>
    </div>
  );
}

export default function CalendarPage() {
  const t = useTranslations("admin.calendar");
  const ta = useTranslations("admin");
  const locale = useLocale();
  const leads = useLeads();
  const [month, setMonth] = useState<MonthRef>(() => currentMonth(TIME_ZONE));
  const [selected, setSelected] = useState<Visit | null>(null);
  const blank = (): VisitValues => ({ propertyId: "", leadId: "", clientName: "", date: today(), time: "10:00", duration: "60", notes: "" });
  const [form, setForm] = useState<VisitValues>(blank);

  const days = useMemo(() => monthGrid(month.year, month.month), [month]);
  const range = useMemo(() => gridRange(days, TIME_ZONE), [days]);
  const visits = useQuery({
    queryKey: [...keys.visits, "range", range.from, range.to],
    queryFn: () => apiGet<Visit[]>(`/api/admin/visits?from=${encodeURIComponent(range.from)}&to=${encodeURIComponent(range.to)}`),
  });
  const upcoming = useUpcomingVisits(8);

  const byDay = useMemo(() => {
    const groups = new Map<string, Visit[]>();
    for (const visit of [...(visits.data ?? [])].sort((a, b) => a.startsAt.localeCompare(b.startsAt))) {
      const key = dayKey(visit.startsAt, TIME_ZONE);
      groups.set(key, [...(groups.get(key) ?? []), visit]);
    }
    return groups;
  }, [visits.data]);

  const create = useAdminMutation(
    () => api.post("/api/admin/visits", toRequest(form, leads.data?.items.find((lead) => lead.id === form.leadId)?.name)),
    { invalidate: [keys.visits, keys.dashboard, keys.leads], success: t("saved"), onSuccess: () => setForm(blank()) },
  );

  const locale_ = intlLocale(locale);
  const monthTitle = new Intl.DateTimeFormat(locale_, { month: "long", year: "numeric", timeZone: "UTC" }).format(new Date(Date.UTC(month.year, month.month - 1, 1)));
  // 4 de outubro de 2026 é domingo: base para os nomes dos dias da semana.
  const weekdays = Array.from({ length: 7 }, (_, index) =>
    new Intl.DateTimeFormat(locale_, { weekday: "short", timeZone: "UTC" }).format(new Date(Date.UTC(2026, 9, 4 + index))).replace(".", ""),
  );
  const todayKey = today();

  return (
    <div className="grid items-start gap-6 xl:grid-cols-[340px_minmax(0,1fr)]">
      <div className="space-y-6">
        <Panel title={t("form")}>
          <form
            onSubmit={(event) => {
              event.preventDefault();
              create.mutate(undefined);
            }}
            className="space-y-4"
          >
            <VisitFields values={form} onChange={setForm} />
            <button type="submit" disabled={create.isPending} className={`${btn.danger} w-full`}>
              <CalendarPlus className="size-4" aria-hidden />
              {create.isPending ? ta("common.saving") : t("save")}
            </button>
          </form>
        </Panel>

        <Panel title={t("upcoming")}>
          {upcoming.isPending ? (
            <Loading />
          ) : upcoming.isError ? (
            <LoadError onRetry={() => upcoming.refetch()} />
          ) : upcoming.data.length === 0 ? (
            <p className="text-sm text-lb-muted">{t("noUpcoming")}</p>
          ) : (
            <ul className="space-y-2.5">
              {upcoming.data.map((visit) => (
                <li key={visit.id}>
                  <button type="button" onClick={() => setSelected(visit)} className="w-full rounded-r-lg border-l-4 border-lb-blue bg-lb-bg p-3 text-left hover:bg-sky-50">
                    <span className="flex items-center justify-between gap-2">
                      <strong className="line-clamp-1 text-sm">{visit.propertyTitle}</strong>
                      <span className="shrink-0 text-xs font-extrabold text-lb-blue">{timeKey(visit.startsAt, TIME_ZONE)}</span>
                    </span>
                    <span className="mt-1 block text-xs text-lb-muted">
                      {visit.clientName} ({formatDate(visit.startsAt, locale, { day: "numeric", month: "short" })})
                    </span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Panel>
      </div>

      <section aria-labelledby="calendar-title" className="rounded-xl border border-lb-border bg-white p-4 shadow-[0_2px_8px_rgb(0_0_0/0.04)]">
        <div className="mb-4 flex items-center justify-between gap-3">
          <h2 id="calendar-title" className="text-lg font-extrabold first-letter:uppercase" aria-live="polite">
            {monthTitle}
          </h2>
          <div className="flex items-center gap-1.5">
            <button type="button" onClick={() => setMonth(currentMonth(TIME_ZONE))} className="rounded-full bg-lb-panel px-3 py-1.5 text-xs font-bold hover:bg-slate-200">
              {t("today")}
            </button>
            <button type="button" onClick={() => setMonth((current) => shiftMonth(current, -1))} className="flex size-8 items-center justify-center rounded-full bg-lb-panel hover:bg-slate-200" aria-label={t("previous")}>
              <ChevronLeft className="size-4" aria-hidden />
            </button>
            <button type="button" onClick={() => setMonth((current) => shiftMonth(current, 1))} className="flex size-8 items-center justify-center rounded-full bg-lb-panel hover:bg-slate-200" aria-label={t("next")}>
              <ChevronRight className="size-4" aria-hidden />
            </button>
          </div>
        </div>

        {visits.isError ? <LoadError onRetry={() => visits.refetch()} /> : null}
        <div className="overflow-x-auto">
          <div className="min-w-[640px]">
            <div className="grid grid-cols-7 border-b border-lb-border pb-2 text-center text-xs font-extrabold text-lb-muted uppercase" aria-hidden>
              {weekdays.map((weekday) => (
                <div key={weekday}>{weekday}</div>
              ))}
            </div>
            <ol className="mt-2 grid grid-cols-7 gap-1">
              {days.map((day) => {
                const events = byDay.get(day.key) ?? [];
                const isToday = day.key === todayKey;
                return (
                  <li
                    key={day.key}
                    aria-label={formatDate(`${day.key}T12:00:00Z`, locale, { dateStyle: "full" })}
                    className={`flex min-h-[96px] flex-col gap-1 rounded-md border p-1.5 ${day.inMonth ? "bg-[#fafbfd]" : "bg-white opacity-40"} ${
                      isToday ? "border-lb-blue bg-sky-50" : "border-slate-100"
                    }`}
                  >
                    <span className={`text-xs font-bold ${isToday ? "flex size-6 items-center justify-center rounded-full bg-lb-blue text-white" : "text-lb-muted"}`}>{day.day}</span>
                    {events.slice(0, MAX_PER_DAY).map((visit) => (
                      <button
                        key={visit.id}
                        type="button"
                        onClick={() => setSelected(visit)}
                        title={`${timeKey(visit.startsAt, TIME_ZONE)} ${visit.propertyTitle} – ${visit.clientName}`}
                        className={`truncate rounded px-1.5 py-0.5 text-left text-[0.68rem] font-semibold text-white ${VISIT_STATUS_FILL[visit.status]}`}
                      >
                        {timeKey(visit.startsAt, TIME_ZONE)} {visit.propertyTitle}
                      </button>
                    ))}
                    {events.length > MAX_PER_DAY ? <span className="text-[0.68rem] font-bold text-lb-muted">{t("more", { count: events.length - MAX_PER_DAY })}</span> : null}
                  </li>
                );
              })}
            </ol>
          </div>
        </div>
        {visits.isPending ? <Loading /> : null}
      </section>

      <Modal open={selected !== null} onClose={() => setSelected(null)} labelledBy="visit-detail-title">
        {selected ? (
          <>
            <h2 id="visit-detail-title" className="mb-1 text-xl font-extrabold">
              {t("detail")}
            </h2>
            <p className="mb-4 text-sm text-lb-muted">
              {selected.propertyTitle} · {selected.clientName} · {formatDate(selected.startsAt, locale, { dateStyle: "full", timeStyle: "short" })}
            </p>
            <VisitDetail key={selected.id} visit={selected} onClose={() => setSelected(null)} />
          </>
        ) : null}
      </Modal>
    </div>
  );
}
