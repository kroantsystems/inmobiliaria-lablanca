"use client";

import { KeyRound } from "lucide-react";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { btn, Field, Panel, ui } from "@/components/admin/ui";
import { useAdminMutation } from "@/lib/admin/queries";
import { api, session } from "@/lib/api/client";
import type { AuthResponse } from "@/lib/api/types";
import { useAuth } from "@/lib/auth/AuthProvider";

// Mesmas regras da API (StrongPassword): 10+ caracteres com letras e números.
const PASSWORD_MIN = 10;
const isStrong = (value: string) => value.length >= PASSWORD_MIN && /\p{L}/u.test(value) && /\d/.test(value);

export default function AccountPage() {
  const t = useTranslations("admin.account");
  const tl = useTranslations("login");
  const tn = useTranslations("admin.leads");
  const { user } = useAuth();
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [error, setError] = useState<string | null>(null);

  const change = useAdminMutation(
    async () => (await api.post<AuthResponse>("/api/auth/change-password", { currentPassword: current, newPassword: next })).data,
    {
      invalidate: [],
      success: t("changed"),
      onSuccess: (response) => {
        // A API encerra as outras sessões e devolve uma nova para este navegador.
        session.replace(response);
        setCurrent("");
        setNext("");
        setConfirm("");
      },
    },
  );

  function submit(event: React.FormEvent) {
    event.preventDefault();
    if (next !== confirm) return setError(t("mismatch"));
    if (!isStrong(next) || next === current) return setError(t("weak"));
    setError(null);
    change.mutate(undefined);
  }

  return (
    <div className="grid items-start gap-6 lg:grid-cols-2">
      <Panel title={t("profile")}>
        <dl className="space-y-3 text-sm">
          <div>
            <dt className={ui.label}>{tl("email")}</dt>
            <dd className="mt-0.5 font-semibold">{user?.email}</dd>
          </div>
          <div>
            <dt className={ui.label}>{tn("name")}</dt>
            <dd className="mt-0.5 font-semibold">{user?.name}</dd>
          </div>
          <div>
            <dt className={ui.label}>{t("role")}</dt>
            <dd className="mt-0.5 font-semibold">{user?.role}</dd>
          </div>
        </dl>
      </Panel>

      <Panel title={t("password")}>
        <form onSubmit={submit} className="space-y-4" noValidate>
          <Field label={t("current")}>
            {(id) => <input id={id} type="password" autoComplete="current-password" required value={current} onChange={(e) => setCurrent(e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("new")} hint={t("rules")}>
            {(id) => <input id={id} type="password" autoComplete="new-password" required minLength={PASSWORD_MIN} value={next} onChange={(e) => setNext(e.target.value)} className={ui.input} />}
          </Field>
          <Field label={t("confirm")} error={error ?? undefined}>
            {(id) => <input id={id} type="password" autoComplete="new-password" required value={confirm} onChange={(e) => setConfirm(e.target.value)} className={ui.input} />}
          </Field>
          <button type="submit" disabled={change.isPending || !current || !next || !confirm} className={btn.primary}>
            <KeyRound className="size-4" aria-hidden />
            {t("submit")}
          </button>
        </form>
      </Panel>
    </div>
  );
}
