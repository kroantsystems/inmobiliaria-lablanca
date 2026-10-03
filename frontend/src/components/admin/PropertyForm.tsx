"use client";

import { Plus, Save } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useState } from "react";
import { locales, type Locale } from "@/i18n/routing";
import { useRouter } from "@/i18n/navigation";
import { keys, useAdminMutation, useOwners, useZones } from "@/lib/admin/queries";
import { api } from "@/lib/api/client";
import { PROPERTY_TYPES, type AdminProperty, type AdminZone, type PropertyInput } from "@/lib/api/types";
import { CURRENCIES, type CurrencyCode } from "@/lib/format/format";
import { btn, Field, Panel, useToast, ui } from "./ui";

type Texts = { title: string; description: string; seoTitle: string; seoDescription: string };

type FormState = {
  operation: "Sale" | "Rent";
  type: string;
  price: string;
  currency: CurrencyCode;
  zoneId: string;
  city: string;
  address: string;
  latitude: string;
  longitude: string;
  bedrooms: string;
  bathrooms: string;
  builtAreaM2: string;
  lotAreaM2: string;
  parkingSpaces: string;
  features: string;
  videoUrl: string;
  ownerId: string;
  texts: Record<Locale, Texts>;
};

const text = (value: number | string | null | undefined) => (value === null || value === undefined ? "" : String(value));
const number = (value: string) => (value.trim() === "" ? null : Number(value.replace(",", ".")));
const blankTexts = (): Texts => ({ title: "", description: "", seoTitle: "", seoDescription: "" });

function fromProperty(property: AdminProperty | null): FormState {
  const texts = Object.fromEntries(locales.map((locale) => [locale, blankTexts()])) as Record<Locale, Texts>;
  for (const translation of property?.translations ?? []) {
    if (translation.locale in texts) {
      texts[translation.locale as Locale] = {
        title: translation.title,
        description: text(translation.description),
        seoTitle: text(translation.seoTitle),
        seoDescription: text(translation.seoDescription),
      };
    }
  }
  return {
    operation: property?.operation ?? "Sale",
    type: property?.type ?? "House",
    price: text(property?.price),
    currency: property?.currency ?? "USD",
    zoneId: property?.zoneId ?? "",
    city: property?.city ?? "",
    address: text(property?.address),
    latitude: text(property?.latitude),
    longitude: text(property?.longitude),
    bedrooms: text(property?.bedrooms),
    bathrooms: text(property?.bathrooms),
    builtAreaM2: text(property?.builtAreaM2),
    lotAreaM2: text(property?.lotAreaM2),
    parkingSpaces: text(property?.parkingSpaces),
    features: (property?.features ?? []).join("\n"),
    videoUrl: text(property?.videoUrl),
    ownerId: property?.ownerId ?? "",
    texts,
  };
}

function toInput(form: FormState): PropertyInput {
  return {
    operation: form.operation,
    type: form.type as PropertyInput["type"],
    price: Number(form.price.replace(",", ".")),
    currency: form.currency,
    zoneId: form.zoneId,
    city: form.city.trim(),
    address: form.address.trim() || null,
    latitude: number(form.latitude),
    longitude: number(form.longitude),
    bedrooms: number(form.bedrooms),
    bathrooms: number(form.bathrooms),
    builtAreaM2: number(form.builtAreaM2),
    lotAreaM2: number(form.lotAreaM2),
    parkingSpaces: number(form.parkingSpaces),
    features: form.features
      .split("\n")
      .map((line) => line.trim())
      .filter(Boolean),
    videoUrl: form.videoUrl.trim() || null,
    ownerId: form.ownerId || null,
    // Idiomas sem título ficam de fora: o site usa o texto em espanhol.
    translations: locales
      .filter((locale) => form.texts[locale].title.trim())
      .map((locale) => ({
        locale,
        title: form.texts[locale].title.trim(),
        description: form.texts[locale].description.trim() || null,
        seoTitle: form.texts[locale].seoTitle.trim() || null,
        seoDescription: form.texts[locale].seoDescription.trim() || null,
      })),
  };
}

export const zoneName = (zone: AdminZone, locale: string) =>
  (zone.translations.find((item) => item.locale === locale) ?? zone.translations.find((item) => item.locale === "es") ?? zone.translations[0])?.name ?? zone.slug;

export function PropertyForm({ initial }: { initial: AdminProperty | null }) {
  const t = useTranslations("admin.properties.form");
  const ta = useTranslations("admin");
  const tp = useTranslations("property");
  const tl = useTranslations("languages");
  const locale = useLocale();
  const router = useRouter();
  const notify = useToast();
  const zones = useZones();
  const owners = useOwners();
  const [form, setForm] = useState<FormState>(() => fromProperty(initial));
  const [tab, setTab] = useState<Locale>("es");

  const set = <K extends keyof FormState>(key: K, value: FormState[K]) => setForm((current) => ({ ...current, [key]: value }));
  const setText = (key: keyof Texts, value: string) =>
    setForm((current) => ({ ...current, texts: { ...current.texts, [tab]: { ...current.texts[tab], [key]: value } } }));

  const save = useAdminMutation(
    async (input: PropertyInput) =>
      initial ? (await api.put<AdminProperty>(`/api/admin/properties/${initial.id}`, input)).data : (await api.post<AdminProperty>("/api/admin/properties", input)).data,
    {
      invalidate: [keys.properties, ...(initial ? [keys.property(initial.id)] : [])],
      success: initial ? ta("common.saved") : t("created"),
      onSuccess: (saved) => {
        if (!initial) router.replace({ pathname: "/admin/properties/[id]", params: { id: saved.id } });
      },
    },
  );

  function submit(event: React.FormEvent) {
    event.preventDefault();
    if (!form.texts.es.title.trim()) {
      setTab("es");
      notify(t("spanishRequired"), "error");
      return;
    }
    save.mutate(toInput(form));
  }

  const current = form.texts[tab];
  const grid = "grid gap-4 sm:grid-cols-2";

  return (
    <form onSubmit={submit} className="space-y-6">
      <Panel title={t("sectionTexts")}>
        <div role="tablist" aria-label={t("sectionTexts")} className="mb-4 flex flex-wrap gap-2 border-b border-lb-border pb-3">
          {locales.map((code) => (
            <button
              key={code}
              type="button"
              role="tab"
              aria-selected={tab === code}
              onClick={() => setTab(code)}
              className={`inline-flex items-center gap-1.5 rounded-full px-4 py-1.5 text-sm font-bold transition ${tab === code ? "bg-lb-blue text-white" : "bg-lb-bg text-lb-muted hover:text-lb-ink"}`}
            >
              {tl(code)}
              <span aria-hidden className={`size-2 rounded-full ${form.texts[code].title.trim() ? "bg-emerald-400" : "bg-slate-300"}`} />
            </button>
          ))}
        </div>
        <div role="tabpanel" className={grid}>
          {tab !== "es" && !current.title.trim() ? <p className="text-xs text-lb-muted sm:col-span-2">{t("translationMissing")}</p> : null}
          <Field label={`${t("title")}${tab === "es" ? " *" : ""}`} full>
            {(id) => <input id={id} value={current.title} maxLength={160} onChange={(e) => setText("title", e.target.value)} className={ui.input} lang={tab} />}
          </Field>
          <Field label={t("description")} full>
            {(id) => <textarea id={id} rows={6} value={current.description} onChange={(e) => setText("description", e.target.value)} className={ui.input} lang={tab} />}
          </Field>
          <Field label={t("seoTitle")} hint={`${current.seoTitle.length}/60`}>
            {(id) => <input id={id} value={current.seoTitle} maxLength={70} onChange={(e) => setText("seoTitle", e.target.value)} className={ui.input} lang={tab} />}
          </Field>
          <Field label={t("seoDescription")} hint={`${t("seoHint")} ${current.seoDescription.length}/160`}>
            {(id) => (
              <textarea id={id} rows={2} value={current.seoDescription} maxLength={200} onChange={(e) => setText("seoDescription", e.target.value)} className={ui.input} lang={tab} />
            )}
          </Field>
        </div>
      </Panel>

      <Panel title={t("sectionData")}>
        <div className={grid}>
          <Field label={t("operation")}>
            {(id) => (
              <select id={id} value={form.operation} onChange={(e) => set("operation", e.target.value as FormState["operation"])} className={ui.input}>
                <option value="Sale">{tp("operation.Sale")}</option>
                <option value="Rent">{tp("operation.Rent")}</option>
              </select>
            )}
          </Field>
          <Field label={t("type")}>
            {(id) => (
              <select id={id} value={form.type} onChange={(e) => set("type", e.target.value)} className={ui.input}>
                {PROPERTY_TYPES.map((value) => (
                  <option key={value} value={value}>
                    {tp(`type.${value}`)}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label={`${t("price")} *`}>
            {(id) => <input id={id} required inputMode="decimal" value={form.price} onChange={(e) => set("price", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("currency")}>
            {(id) => (
              <select id={id} value={form.currency} onChange={(e) => set("currency", e.target.value as CurrencyCode)} className={ui.input}>
                {CURRENCIES.map((code) => (
                  <option key={code} value={code}>
                    {code}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label={t("bedrooms")}>
            {(id) => <input id={id} type="number" min={0} value={form.bedrooms} onChange={(e) => set("bedrooms", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("bathrooms")}>
            {(id) => <input id={id} type="number" min={0} value={form.bathrooms} onChange={(e) => set("bathrooms", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("builtArea")}>
            {(id) => <input id={id} inputMode="decimal" value={form.builtAreaM2} onChange={(e) => set("builtAreaM2", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("lotArea")}>
            {(id) => <input id={id} inputMode="decimal" value={form.lotAreaM2} onChange={(e) => set("lotAreaM2", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("parking")}>
            {(id) => <input id={id} type="number" min={0} value={form.parkingSpaces} onChange={(e) => set("parkingSpaces", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("owner")}>
            {(id) => (
              <select id={id} value={form.ownerId} onChange={(e) => set("ownerId", e.target.value)} className={ui.input}>
                <option value="">{t("noOwner")}</option>
                {(owners.data?.items ?? []).map((owner) => (
                  <option key={owner.id} value={owner.id}>
                    {owner.name}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label={t("features")} hint={t("featuresHint")}>
            {(id) => <textarea id={id} rows={4} value={form.features} onChange={(e) => set("features", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("video")}>
            {(id) => <input id={id} type="url" placeholder="https://www.youtube.com/watch?v=…" value={form.videoUrl} onChange={(e) => set("videoUrl", e.target.value)} className={ui.input} />}
          </Field>
        </div>
      </Panel>

      <Panel title={t("sectionLocation")}>
        <div className={grid}>
          <Field label={`${t("zone")} *`}>
            {(id) => (
              <select
                id={id}
                required
                value={form.zoneId}
                onChange={(e) => {
                  const zone = zones.data?.find((item) => item.id === e.target.value);
                  setForm((current) => ({ ...current, zoneId: e.target.value, city: current.city || zone?.city || "" }));
                }}
                className={ui.input}
              >
                <option value="">{t("chooseZone")}</option>
                {(zones.data ?? []).map((zone) => (
                  <option key={zone.id} value={zone.id}>
                    {zoneName(zone, locale)} · {zone.city}
                  </option>
                ))}
              </select>
            )}
          </Field>
          <Field label={`${t("city")} *`}>
            {(id) => <input id={id} required value={form.city} onChange={(e) => set("city", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("address")} full>
            {(id) => <input id={id} value={form.address} onChange={(e) => set("address", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("latitude")} hint={t("coordsHint")}>
            {(id) => <input id={id} inputMode="decimal" placeholder="-25.5097" value={form.latitude} onChange={(e) => set("latitude", e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("longitude")}>
            {(id) => <input id={id} inputMode="decimal" placeholder="-54.6111" value={form.longitude} onChange={(e) => set("longitude", e.target.value)} className={ui.input} />}
          </Field>
        </div>
      </Panel>

      <div className="flex justify-end">
        <button type="submit" disabled={save.isPending} className={initial ? btn.primary : btn.danger}>
          {initial ? <Save className="size-4" aria-hidden /> : <Plus className="size-4" aria-hidden />}
          {save.isPending ? ta("common.saving") : initial ? ta("common.save") : t("create")}
        </button>
      </div>
    </form>
  );
}
