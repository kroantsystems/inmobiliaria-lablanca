"use client";

import { MapPinned, Pencil, Plus, Save, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { zoneName } from "@/components/admin/PropertyForm";
import { btn, Drawer, Field, Loading, LoadError, Panel, tdClass, thClass, ui } from "@/components/admin/ui";
import { locales, type Locale } from "@/i18n/routing";
import { keys, useAdminMutation, useSettings, useZones } from "@/lib/admin/queries";
import { api } from "@/lib/api/client";
import type { AdminSettings, AdminZone } from "@/lib/api/types";
import { formatDate } from "@/lib/format/format";

type SettingsValues = Record<
  | "companyName"
  | "phone"
  | "whatsappNumber"
  | "email"
  | "address"
  | "officeLatitude"
  | "officeLongitude"
  | "openingHours"
  | "facebookUrl"
  | "instagramUrl"
  | "tiktokUrl"
  | "youtubeUrl"
  | "pygPerUsd"
  | "brlPerUsd"
  | "simulatorAnnualRate"
  | "monthlySalesGoal",
  string
>;

const text = (value: string | number | null | undefined) => (value === null || value === undefined ? "" : String(value));
const optional = (value: string) => value.trim() || null;
const decimal = (value: string) => (value.trim() === "" ? null : Number(value.replace(",", ".")));

function fromSettings(settings: AdminSettings): SettingsValues {
  return {
    companyName: settings.companyName,
    phone: text(settings.phone),
    whatsappNumber: text(settings.whatsappNumber),
    email: text(settings.email),
    address: text(settings.address),
    officeLatitude: text(settings.officeLatitude),
    officeLongitude: text(settings.officeLongitude),
    openingHours: text(settings.openingHours),
    facebookUrl: text(settings.facebookUrl),
    instagramUrl: text(settings.instagramUrl),
    tiktokUrl: text(settings.tiktokUrl),
    youtubeUrl: text(settings.youtubeUrl),
    pygPerUsd: text(settings.pygPerUsd),
    brlPerUsd: text(settings.brlPerUsd),
    simulatorAnnualRate: text(settings.simulatorAnnualRate),
    monthlySalesGoal: text(settings.monthlySalesGoal),
  };
}

function SettingsForm({ settings }: { settings: AdminSettings }) {
  const t = useTranslations("admin.settings");
  const tc = useTranslations("admin.common");
  const locale = useLocale();
  const [values, setValues] = useState(() => fromSettings(settings));
  const set = (key: keyof SettingsValues, value: string) => setValues((current) => ({ ...current, [key]: value }));

  const save = useAdminMutation(
    () =>
      api.put<AdminSettings>("/api/admin/settings", {
        companyName: values.companyName.trim(),
        phone: optional(values.phone),
        whatsappNumber: optional(values.whatsappNumber),
        email: optional(values.email),
        address: optional(values.address),
        officeLatitude: decimal(values.officeLatitude),
        officeLongitude: decimal(values.officeLongitude),
        openingHours: optional(values.openingHours),
        facebookUrl: optional(values.facebookUrl),
        instagramUrl: optional(values.instagramUrl),
        tiktokUrl: optional(values.tiktokUrl),
        youtubeUrl: optional(values.youtubeUrl),
        pygPerUsd: decimal(values.pygPerUsd),
        brlPerUsd: decimal(values.brlPerUsd),
        simulatorAnnualRate: decimal(values.simulatorAnnualRate),
        monthlySalesGoal: Number(values.monthlySalesGoal) || 0,
      }),
    { invalidate: [keys.settings, keys.dashboard], success: t("saved") },
  );

  const input = (key: keyof SettingsValues, label: string, props: React.InputHTMLAttributes<HTMLInputElement> = {}) => (
    <Field label={label}>{(id) => <input id={id} value={values[key]} onChange={(e) => set(key, e.target.value)} className={ui.input} {...props} />}</Field>
  );

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate(undefined);
      }}
      className="space-y-6"
    >
      <Panel title={t("company")}>
        <div className="grid gap-4 sm:grid-cols-2">
          {input("companyName", `${t("companyName")} *`, { required: true })}
          {input("email", t("email"), { type: "email" })}
          {input("phone", t("phone"), { type: "tel" })}
          {input("whatsappNumber", t("whatsapp"), { type: "tel", placeholder: "+595981000000" })}
          {input("address", t("address"))}
          {input("openingHours", t("hours"))}
          {input("officeLatitude", t("latitude"), { inputMode: "decimal", placeholder: "-25.5097" })}
          {input("officeLongitude", t("longitude"), { inputMode: "decimal", placeholder: "-54.6111" })}
        </div>
        <h3 className="mt-6 mb-3 text-sm font-extrabold">{t("social")}</h3>
        <div className="grid gap-4 sm:grid-cols-2">
          {input("facebookUrl", "Facebook", { type: "url", placeholder: "https://facebook.com/…" })}
          {input("instagramUrl", "Instagram", { type: "url", placeholder: "https://instagram.com/…" })}
          {input("tiktokUrl", "TikTok", { type: "url", placeholder: "https://tiktok.com/@…" })}
          {input("youtubeUrl", "YouTube", { type: "url", placeholder: "https://youtube.com/@…" })}
        </div>
      </Panel>

      <Panel title={t("rates")}>
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
          {input("pygPerUsd", `${t("pygPerUsd")} *`, { required: true, inputMode: "decimal" })}
          {input("brlPerUsd", `${t("brlPerUsd")} *`, { required: true, inputMode: "decimal" })}
          {input("simulatorAnnualRate", `${t("simulatorRate")} *`, { required: true, inputMode: "decimal" })}
          {input("monthlySalesGoal", t("goal"), { type: "number", min: 0 })}
        </div>
        <p className="mt-3 text-xs text-lb-muted" aria-live="polite">
          {t("ratesUpdated", { date: formatDate(settings.ratesUpdatedAt, locale, { dateStyle: "long", timeStyle: "short" }) })}
        </p>
      </Panel>

      <div className="flex justify-end">
        <button type="submit" disabled={save.isPending} className={btn.primary}>
          <Save className="size-4" aria-hidden />
          {save.isPending ? tc("saving") : tc("save")}
        </button>
      </div>
    </form>
  );
}

type ZoneValues = { slug: string; city: string; sortOrder: string; texts: Record<Locale, { name: string; description: string }> };

function ZoneForm({ zone, onClose }: { zone: AdminZone | null; onClose: () => void }) {
  const t = useTranslations("admin.settings");
  const tc = useTranslations("admin.common");
  const tl = useTranslations("languages");
  const [values, setValues] = useState<ZoneValues>(() => ({
    slug: zone?.slug ?? "",
    city: zone?.city ?? "Ciudad del Este",
    sortOrder: String(zone?.sortOrder ?? 0),
    texts: Object.fromEntries(
      locales.map((locale) => {
        const translation = zone?.translations.find((item) => item.locale === locale);
        return [locale, { name: translation?.name ?? "", description: translation?.description ?? "" }];
      }),
    ) as ZoneValues["texts"],
  }));
  const setText = (locale: Locale, key: "name" | "description", value: string) =>
    setValues((current) => ({ ...current, texts: { ...current.texts, [locale]: { ...current.texts[locale], [key]: value } } }));

  const request = () => ({
    slug: values.slug.trim() || null,
    city: values.city.trim(),
    sortOrder: Number(values.sortOrder) || 0,
    translations: locales
      .filter((locale) => values.texts[locale].name.trim())
      .map((locale) => ({ locale, name: values.texts[locale].name.trim(), description: values.texts[locale].description.trim() || null })),
  });
  const save = useAdminMutation(() => (zone ? api.put(`/api/admin/zones/${zone.id}`, request()) : api.post("/api/admin/zones", request())), {
    invalidate: [keys.zones, keys.settings],
    success: t("zoneSaved"),
    onSuccess: onClose,
  });

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate(undefined);
      }}
      className="space-y-4"
    >
      <div className="grid gap-4 sm:grid-cols-2">
        <Field label={`${t("zoneCity")} *`}>{(id) => <input id={id} required value={values.city} onChange={(e) => setValues({ ...values, city: e.target.value })} className={ui.input} />}</Field>
        <Field label={t("zoneOrder")}>
          {(id) => <input id={id} type="number" value={values.sortOrder} onChange={(e) => setValues({ ...values, sortOrder: e.target.value })} className={ui.input} />}
        </Field>
        <Field label={t("zoneSlug")} hint={t("zoneSlugHint")} full>
          {(id) => <input id={id} value={values.slug} pattern="[a-z0-9-]*" onChange={(e) => setValues({ ...values, slug: e.target.value })} className={ui.input} />}
        </Field>
      </div>
      {locales.map((locale) => (
        <fieldset key={locale} className="space-y-3 rounded-lg border border-lb-border p-3">
          <legend className="px-1 text-xs font-extrabold text-lb-blue uppercase">{tl(locale)}</legend>
          <Field label={`${t("zoneName")}${locale === "es" ? " *" : ""}`}>
            {(id) => <input id={id} required={locale === "es"} lang={locale} value={values.texts[locale].name} onChange={(e) => setText(locale, "name", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("zoneDescription")}>
            {(id) => <textarea id={id} rows={2} lang={locale} value={values.texts[locale].description} onChange={(e) => setText(locale, "description", e.target.value)} className={ui.input} />}
          </Field>
        </fieldset>
      ))}
      <button type="submit" disabled={save.isPending} className={btn.primary}>
        {save.isPending ? tc("saving") : tc("save")}
      </button>
    </form>
  );
}

function Zones() {
  const t = useTranslations("admin.settings");
  const tc = useTranslations("admin.common");
  const locale = useLocale();
  const zones = useZones();
  const [editing, setEditing] = useState<AdminZone | "new" | null>(null);
  const remove = useAdminMutation((id: string) => api.delete(`/api/admin/zones/${id}`), { invalidate: [keys.zones, keys.settings], success: tc("saved") });

  return (
    <Panel
      title={t("zones")}
      actions={
        <button type="button" onClick={() => setEditing("new")} className={btn.primary}>
          <Plus className="size-4" aria-hidden />
          {t("newZone")}
        </button>
      }
    >
      <p className="mb-3 text-xs text-lb-muted">{t("zonesHint")}</p>
      {zones.isPending ? (
        <Loading />
      ) : zones.isError ? (
        <LoadError onRetry={() => zones.refetch()} />
      ) : (
        <div className="-mx-5 overflow-x-auto sm:-mx-6">
          <table className="w-full min-w-[560px] border-collapse text-sm">
            <thead>
              <tr>
                <th scope="col" className={thClass}>
                  {t("zoneName")}
                </th>
                <th scope="col" className={thClass}>
                  {t("zoneCity")}
                </th>
                <th scope="col" className={thClass}>
                  {t("zoneSlug")}
                </th>
                <th scope="col" className={thClass}>
                  {tc("actions")}
                </th>
              </tr>
            </thead>
            <tbody>
              {[...zones.data].sort((a, b) => a.sortOrder - b.sortOrder).map((zone) => (
                <tr key={zone.id}>
                  <td className={tdClass}>
                    <span className="flex items-center gap-2 font-bold">
                      <MapPinned className="size-4 text-lb-red" aria-hidden />
                      {zoneName(zone, locale)}
                    </span>
                    <span className="text-xs text-lb-muted">{t("zoneCount", { count: zone.propertyCount })}</span>
                  </td>
                  <td className={tdClass}>{zone.city}</td>
                  <td className={`${tdClass} font-mono text-xs`}>{zone.slug}</td>
                  <td className={tdClass}>
                    <div className="flex gap-1">
                      <button type="button" onClick={() => setEditing(zone)} className={ui.iconButton} aria-label={`${tc("edit")} ${zoneName(zone, locale)}`}>
                        <Pencil className="size-4" aria-hidden />
                      </button>
                      <button
                        type="button"
                        disabled={zone.propertyCount > 0}
                        title={zone.propertyCount > 0 ? t("zoneInUse") : tc("delete")}
                        onClick={() => {
                          if (window.confirm(tc("confirmDelete"))) remove.mutate(zone.id);
                        }}
                        className={`${ui.iconButton} hover:text-lb-red disabled:opacity-40`}
                        aria-label={`${tc("delete")} ${zoneName(zone, locale)}`}
                      >
                        <Trash2 className="size-4" aria-hidden />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
      <Drawer open={editing !== null} onClose={() => setEditing(null)} title={editing === "new" ? t("newZone") : t("editZone")}>
        {editing ? <ZoneForm key={editing === "new" ? "new" : editing.id} zone={editing === "new" ? null : editing} onClose={() => setEditing(null)} /> : null}
      </Drawer>
    </Panel>
  );
}

export default function SettingsPage() {
  const settings = useSettings();
  return (
    <div className="space-y-6">
      {settings.isPending ? <Loading /> : settings.isError ? <LoadError onRetry={() => settings.refetch()} /> : <SettingsForm key={settings.data.updatedAt ?? ""} settings={settings.data} />}
      <Zones />
    </div>
  );
}
