"use client";

import { useQuery } from "@tanstack/react-query";
import { ArrowDownRight, ArrowUpRight, Eye, Handshake, KeyRound, MousePointerClick, UserPlus } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import { Loading, LoadError, Panel } from "@/components/admin/ui";
import { Link } from "@/i18n/navigation";
import { apiGet, keys, useUpcomingVisits } from "@/lib/admin/queries";
import type { DashboardSummary, Kpi, TopProperty, TrafficPoint } from "@/lib/api/types";
import { formatDate, formatNumber, intlLocale } from "@/lib/format/format";

const ICON_TONES = {
  blue: "bg-lb-blue/10 text-lb-blue",
  red: "bg-lb-red/10 text-lb-red",
  green: "bg-emerald-500/10 text-emerald-700",
  purple: "bg-violet-500/10 text-violet-700",
  amber: "bg-amber-500/10 text-amber-700",
};

function KpiCard({ label, kpi, icon: Icon, tone, footer }: { label: string; kpi: Kpi; icon: typeof Eye; tone: keyof typeof ICON_TONES; footer?: React.ReactNode }) {
  const t = useTranslations("admin.dashboard");
  const locale = useLocale();
  const change = kpi.changePercent;
  return (
    <div className="rounded-xl border border-lb-border bg-white p-5 shadow-[0_2px_8px_rgb(0_0_0/0.04)]">
      <div className="mb-3 flex items-center justify-between gap-2">
        <span className="text-xs font-bold text-lb-muted uppercase">{label}</span>
        <span className={`flex size-10 items-center justify-center rounded-lg ${ICON_TONES[tone]}`}>
          <Icon className="size-5" aria-hidden />
        </span>
      </div>
      <p className="mb-1 text-3xl leading-none font-extrabold">{formatNumber(kpi.value, locale)}</p>
      {footer ??
        (change === null ? (
          <span className="text-xs text-lb-muted">{t("noComparison")}</span>
        ) : (
          <span className={`inline-flex items-center gap-0.5 text-xs font-bold ${change >= 0 ? "text-emerald-700" : "text-lb-red"}`}>
            {change >= 0 ? <ArrowUpRight className="size-3.5" aria-hidden /> : <ArrowDownRight className="size-3.5" aria-hidden />}
            {t("change", { value: `${change > 0 ? "+" : ""}${formatNumber(change, locale, 1)}` })}
          </span>
        ))}
    </div>
  );
}

export default function DashboardPage() {
  const t = useTranslations("admin.dashboard");
  const locale = useLocale();
  const [days, setDays] = useState<7 | 30>(7);
  const summary = useQuery({ queryKey: [...keys.dashboard, "summary"], queryFn: () => apiGet<DashboardSummary>("/api/admin/dashboard/summary") });
  const traffic = useQuery({ queryKey: [...keys.dashboard, "traffic", days], queryFn: () => apiGet<TrafficPoint[]>(`/api/admin/dashboard/traffic?days=${days}`) });
  const top = useQuery({ queryKey: [...keys.dashboard, "top"], queryFn: () => apiGet<TopProperty[]>("/api/admin/dashboard/top-properties") });
  const upcoming = useUpcomingVisits(5);

  const dayLabel = new Intl.DateTimeFormat(intlLocale(locale), days === 7 ? { weekday: "short", timeZone: "UTC" } : { day: "2-digit", month: "2-digit", timeZone: "UTC" });
  const chartData = (traffic.data ?? []).map((point) => ({ ...point, label: dayLabel.format(new Date(`${point.date}T00:00:00Z`)) }));
  const s = summary.data;

  return (
    <div className="space-y-6">
      {summary.isPending ? (
        <Loading />
      ) : summary.isError || !s ? (
        <LoadError onRetry={() => summary.refetch()} />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
          <KpiCard label={t("siteVisits")} kpi={s.siteVisits} icon={Eye} tone="blue" />
          <KpiCard label={t("propertyViews")} kpi={s.propertyViews} icon={MousePointerClick} tone="red" />
          <KpiCard
            label={t("sales")}
            kpi={s.sales}
            icon={Handshake}
            tone="green"
            footer={
              <div>
                <span className="text-xs text-lb-muted">{t("goal", { goal: s.monthlySalesGoal })}</span>
                <div className="mt-1.5 h-1.5 overflow-hidden rounded-full bg-lb-panel" aria-hidden>
                  <div className="h-full rounded-full bg-emerald-500" style={{ width: `${Math.min(100, (s.sales.value / Math.max(1, s.monthlySalesGoal)) * 100)}%` }} />
                </div>
              </div>
            }
          />
          <KpiCard label={t("activeRentals")} kpi={s.activeRentals} icon={KeyRound} tone="purple" />
          <KpiCard label={t("newLeads")} kpi={s.newLeads} icon={UserPlus} tone="amber" />
        </div>
      )}
      {s ? <p className="-mt-3 text-xs text-lb-muted">{t("monthFrom", { date: formatDate(s.monthStart, locale, { dateStyle: "long" }) })}</p> : null}

      <div className="grid gap-5 xl:grid-cols-[2fr_1fr]">
        <Panel
          title={t("traffic")}
          actions={
            <div role="group" aria-label={t("traffic")} className="flex rounded-lg bg-lb-panel p-1">
              {([7, 30] as const).map((value) => (
                <button
                  key={value}
                  type="button"
                  aria-pressed={days === value}
                  onClick={() => setDays(value)}
                  className={`rounded-md px-3 py-1 text-xs font-bold transition ${days === value ? "bg-white text-lb-blue shadow-sm" : "text-lb-muted"}`}
                >
                  {t("days", { count: value })}
                </button>
              ))}
            </div>
          }
        >
          {traffic.isPending ? (
            <Loading />
          ) : traffic.isError ? (
            <LoadError onRetry={() => traffic.refetch()} />
          ) : (
            <div className="h-64">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={chartData} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                  <CartesianGrid vertical={false} stroke="#e2e8f0" />
                  <XAxis dataKey="label" tickLine={false} axisLine={false} fontSize={11} interval={days === 30 ? 2 : 0} />
                  <YAxis allowDecimals={false} tickLine={false} axisLine={false} fontSize={11} />
                  <Tooltip cursor={{ fill: "rgb(0 93 170 / 0.06)" }} />
                  <Legend iconType="circle" wrapperStyle={{ fontSize: 12 }} />
                  <Bar dataKey="visits" name={t("visits")} fill="#005DAA" radius={[6, 6, 0, 0]} />
                  <Bar dataKey="propertyViews" name={t("views")} fill="#E31B23" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          )}
        </Panel>

        <Panel title={t("topProperties")}>
          {top.isPending ? (
            <Loading />
          ) : top.isError ? (
            <LoadError onRetry={() => top.refetch()} />
          ) : top.data.length === 0 ? (
            <p className="text-sm text-lb-muted">{t("noTop")}</p>
          ) : (
            <ol className="space-y-3 text-sm">
              {top.data.map((item, index) => (
                <li key={item.id} className="flex items-center justify-between gap-3 border-b border-lb-border pb-2 last:border-0">
                  <Link href={{ pathname: "/admin/properties/[id]", params: { id: item.id } }} className="line-clamp-1 hover:text-lb-blue">
                    {index + 1}. {item.title}
                  </Link>
                  <strong className="shrink-0">{t("viewsCount", { count: item.views })}</strong>
                </li>
              ))}
            </ol>
          )}
        </Panel>
      </div>

      <Panel
        title={t("upcoming")}
        actions={
          <Link href="/admin/calendar" className="text-sm font-bold text-lb-blue hover:underline">
            {t("seeCalendar")}
          </Link>
        }
      >
        {upcoming.isPending ? (
          <Loading />
        ) : upcoming.isError ? (
          <LoadError onRetry={() => upcoming.refetch()} />
        ) : upcoming.data.length === 0 ? (
          <p className="text-sm text-lb-muted">{t("noUpcoming")}</p>
        ) : (
          <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {upcoming.data.map((visit) => (
              <li key={visit.id} className="rounded-r-lg border-l-4 border-lb-blue bg-lb-bg p-3">
                <div className="flex items-center justify-between gap-2">
                  <strong className="line-clamp-1 text-sm">{visit.propertyTitle}</strong>
                  <span className="shrink-0 text-xs font-extrabold text-lb-blue">{formatDate(visit.startsAt, locale, { hour: "2-digit", minute: "2-digit" })}</span>
                </div>
                <p className="mt-1 text-xs text-lb-muted">
                  {visit.clientName} · {formatDate(visit.startsAt, locale, { weekday: "short", day: "numeric", month: "short" })}
                </p>
              </li>
            ))}
          </ul>
        )}
      </Panel>
    </div>
  );
}
