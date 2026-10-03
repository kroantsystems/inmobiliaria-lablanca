"use client";

import { useLocale, useTranslations } from "next-intl";
import { useId, useState } from "react";
import { useRouter } from "@/i18n/navigation";
import { apiClient, problemOf, session } from "@/lib/api/client";

export function LoginForm({ notice }: { notice?: string }) {
  const t = useTranslations("login");
  const tc = useTranslations("common");
  const router = useRouter();
  const locale = useLocale();
  const id = useId();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    try {
      // Mensagens de erro da API no idioma da página (o sincronizador de idioma só existe no painel).
      apiClient.setLocale(locale);
      await session.login(email, password);
      router.push("/admin");
    } catch (failure) {
      const problem = problemOf(failure);
      setError(problem?.detail ?? tc("genericError"));
      setPassword("");
      setSubmitting(false);
    }
  }

  const input = "mt-1 w-full rounded-lg border border-lb-border bg-lb-bg px-3 py-2.5 text-sm outline-none focus:border-lb-blue";
  const label = "block text-xs font-bold uppercase tracking-wide text-lb-blue";

  return (
    <form onSubmit={submit} className="space-y-4">
      {notice ? (
        <p role="status" className="rounded-lg bg-amber-50 px-3 py-2 text-sm text-amber-900">
          {notice}
        </p>
      ) : null}
      <div>
        <label htmlFor={`${id}-email`} className={label}>
          {t("email")}
        </label>
        <input
          id={`${id}-email`}
          type="email"
          autoComplete="username"
          autoFocus
          required
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          className={input}
        />
      </div>
      <div>
        <label htmlFor={`${id}-password`} className={label}>
          {t("password")}
        </label>
        <input
          id={`${id}-password`}
          type="password"
          autoComplete="current-password"
          required
          value={password}
          onChange={(e) => setPassword(e.target.value)}
          className={input}
        />
      </div>
      {error ? (
        <p role="alert" className="rounded-lg bg-red-50 px-3 py-2 text-sm text-lb-red">
          {error}
        </p>
      ) : null}
      <button
        type="submit"
        disabled={submitting}
        className="w-full rounded-full bg-lb-blue px-6 py-3 text-sm font-bold text-white transition hover:bg-lb-blue-hover disabled:opacity-60"
      >
        {submitting ? t("submitting") : t("submit")}
      </button>
    </form>
  );
}
