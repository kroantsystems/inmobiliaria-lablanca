"use client";

import { useTranslations } from "next-intl";

/** Barras de progresso por arquivo e erros de validação/envio. */
export function UploadStatus({ progress, errors }: { progress: Record<string, number>; errors: string[] }) {
  const t = useTranslations("admin.properties.media");
  const entries = Object.entries(progress);
  if (entries.length === 0 && errors.length === 0) return null;
  return (
    <div className="mt-3 space-y-2">
      {entries.map(([name, percent]) => (
        <div key={name}>
          <p className="text-xs font-semibold">{t("uploading", { name, percent })}</p>
          <div className="mt-1 h-2 overflow-hidden rounded-full bg-lb-panel" role="progressbar" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100} aria-label={name}>
            <div className="h-full rounded-full bg-lb-blue transition-[width]" style={{ width: `${percent}%` }} />
          </div>
        </div>
      ))}
      {errors.length > 0 ? (
        <ul role="alert" className="space-y-1 rounded-lg bg-red-50 px-3 py-2 text-xs font-semibold text-lb-red">
          {errors.map((error) => (
            <li key={error}>{error}</li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
