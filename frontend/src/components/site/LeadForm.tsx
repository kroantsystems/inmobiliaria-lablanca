"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useId, useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { Link } from "@/i18n/navigation";
import { api, problemOf } from "@/lib/api/client";
import type { LeadInterest, LeadSource } from "@/lib/api/types";

type LeadFormProps = {
  source: Exclude<LeadSource, "Manual">;
  propertyId?: string;
  interest?: LeadInterest;
  successMessage: string;
  submitLabel?: string;
  withEmail?: boolean;
  withMessage?: boolean;
  tone?: "light" | "dark";
  /** Só dentro de modais: fora deles o foco automático faria a página rolar até o formulário. */
  autoFocus?: boolean;
};

type Values = { name: string; phone: string; email: string; message?: string; consent: boolean; website?: string };

export function LeadForm({
  source,
  propertyId,
  interest,
  successMessage,
  submitLabel,
  withEmail = source === "Newsletter",
  withMessage = false,
  tone = "light",
  autoFocus = false,
}: LeadFormProps) {
  const t = useTranslations("forms");
  const tc = useTranslations("common");
  const locale = useLocale();
  const id = useId();
  const [sent, setSent] = useState(false);

  const schema = z.object({
    name: z.string().trim().min(1, t("required")).max(120),
    phone: z.string().trim().refine((value) => (value.match(/\d/g)?.length ?? 0) >= 8, t("invalidPhone")),
    // Obrigatório só na newsletter; nos outros formulários pode ficar vazio, mas se preenchido precisa ser válido.
    email: z
      .string()
      .trim()
      .refine((value) => source !== "Newsletter" || value.length > 0, t("required"))
      .refine((value) => value === "" || z.email().safeParse(value).success, t("invalidEmail")),
    message: z.string().max(2000).optional(),
    consent: z.boolean().refine((value) => value, t("consentRequired")),
    website: z.string().optional(),
  });

  const {
    register,
    handleSubmit,
    reset,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<Values>({ resolver: zodResolver(schema), defaultValues: { email: "", consent: false } });

  const onSubmit = handleSubmit(async (values) => {
    try {
      await api.post("/api/public/leads", {
        source,
        name: values.name,
        phone: values.phone,
        email: values.email || null,
        interest: interest ?? null,
        propertyId: propertyId ?? null,
        message: values.message || null,
        locale,
        consent: values.consent,
        website: values.website || null,
      });
      reset();
      setSent(true);
    } catch (error) {
      const problem = problemOf(error);
      for (const [field, messages] of Object.entries(problem?.errors ?? {})) {
        if (field in schema.shape) setError(field as keyof Values, { message: messages[0] });
      }
      setError("root", { message: problem?.detail ?? tc("genericError") });
    }
  });

  const dark = tone === "dark";
  const label = `block text-xs font-bold uppercase tracking-wide ${dark ? "text-slate-300" : "text-lb-blue"}`;
  const input = `mt-1 w-full rounded-lg border px-3 py-2.5 text-sm outline-none transition focus:border-lb-blue ${
    dark ? "border-slate-600 bg-lb-slate text-white placeholder:text-slate-400" : "border-lb-border bg-lb-bg text-lb-ink"
  }`;
  const error = "mt-1 text-xs font-medium text-lb-red";

  if (sent) {
    return (
      <div role="status" className={`flex items-start gap-3 rounded-lg p-4 text-sm ${dark ? "bg-white/5 text-white" : "bg-emerald-50 text-emerald-900"}`}>
        <CheckCircle2 className="mt-0.5 size-5 shrink-0 text-emerald-500" aria-hidden />
        <p>{successMessage}</p>
      </div>
    );
  }

  return (
    <form onSubmit={onSubmit} noValidate className="space-y-3">
      <div>
        <label htmlFor={`${id}-name`} className={label}>
          {t("name")}
        </label>
        <input id={`${id}-name`} autoComplete="name" autoFocus={autoFocus} className={input} aria-invalid={!!errors.name} {...register("name")} />
        {errors.name ? <p className={error}>{errors.name.message}</p> : null}
      </div>
      {withEmail ? (
        <div>
          <label htmlFor={`${id}-email`} className={label}>
            {t("email")}
          </label>
          <input id={`${id}-email`} type="email" autoComplete="email" className={input} aria-invalid={!!errors.email} {...register("email")} />
          {errors.email ? <p className={error}>{errors.email.message}</p> : null}
        </div>
      ) : null}
      <div>
        <label htmlFor={`${id}-phone`} className={label}>
          {t("phone")}
        </label>
        <input id={`${id}-phone`} type="tel" autoComplete="tel" placeholder="+595 981 000 000" className={input} aria-invalid={!!errors.phone} {...register("phone")} />
        {errors.phone ? <p className={error}>{errors.phone.message}</p> : null}
      </div>
      {withMessage ? (
        <div>
          <label htmlFor={`${id}-message`} className={label}>
            {t("message")}
          </label>
          <textarea id={`${id}-message`} rows={3} className={input} {...register("message")} />
        </div>
      ) : null}
      <div className="hidden" aria-hidden="true">
        <label htmlFor={`${id}-website`}>{t("honeypot")}</label>
        <input id={`${id}-website`} tabIndex={-1} autoComplete="off" {...register("website")} />
      </div>
      <div>
        <label className={`flex items-start gap-2 text-xs ${dark ? "text-slate-300" : "text-lb-muted"}`}>
          <input type="checkbox" className="mt-0.5 size-4 accent-lb-blue" {...register("consent")} />
          <span>
            {t.rich("consent", {
              link: (chunks) => (
                <Link href="/privacy" className="font-semibold underline" target="_blank">
                  {chunks}
                </Link>
              ),
            })}
          </span>
        </label>
        {errors.consent ? <p className={error}>{errors.consent.message}</p> : null}
      </div>
      {errors.root ? (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-lb-red">
          {errors.root.message}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={isSubmitting}
        className="inline-flex w-full items-center justify-center rounded-full bg-lb-red px-6 py-3 text-sm font-bold text-white shadow-md transition hover:bg-lb-red-hover disabled:opacity-60"
      >
        {isSubmitting ? t("sending") : (submitLabel ?? t("submit"))}
      </button>
    </form>
  );
}
