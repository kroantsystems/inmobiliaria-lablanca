"use client";

import { useTranslations } from "next-intl";
import { useCallback, useState } from "react";
import { useErrorMessage } from "@/lib/admin/queries";
import { api } from "@/lib/api/client";
import { ACCEPTED_EXTENSIONS_LABEL, validateUpload } from "@/lib/uploads/validation";

// Arquivos grandes vão direto para a origem da API: o rewrite /api do Next não aguenta ~50 MB dentro do tempo
// limite do proxy (teste da tarefa 14.5). Vazio = mesma origem (rewrite), suficiente para arquivos pequenos.
const UPLOAD_BASE = (process.env.NEXT_PUBLIC_API_UPLOAD_URL ?? "").replace(/\/$/, "");

type UploadFields = { propertyId?: string | null; description?: string; altText?: string; isPublic?: boolean };

/** Valida no cliente (mesmas regras da API) e envia um arquivo por vez, com progresso individual. */
export function useUploader() {
  const t = useTranslations("admin.upload");
  const errorMessage = useErrorMessage();
  const [progress, setProgress] = useState<Record<string, number>>({});
  const [errors, setErrors] = useState<string[]>([]);

  const upload = useCallback(
    async (files: File[], fields: UploadFields = {}) => {
      const problems: string[] = [];
      const valid = files.filter((file) => {
        const result = validateUpload(file);
        if (result.ok) return true;
        problems.push(
          result.error === "tooLarge"
            ? t("tooLarge", { name: file.name, limit: result.limitMb })
            : t("typeNotAllowed", { name: file.name, types: ACCEPTED_EXTENSIONS_LABEL }),
        );
        return false;
      });
      setErrors([...problems]);

      let uploaded = 0;
      for (const file of valid) {
        const key = `${file.name} (${Math.round(file.size / 1024)} KB)`;
        setProgress((current) => ({ ...current, [key]: 0 }));
        const form = new FormData();
        form.append("file", file);
        if (fields.propertyId) form.append("propertyId", fields.propertyId);
        if (fields.description) form.append("description", fields.description);
        if (fields.altText) form.append("altText", fields.altText);
        form.append("isPublic", String(fields.isPublic ?? true));
        try {
          await api.post(`${UPLOAD_BASE}/api/admin/files`, form, {
            onUploadProgress: (event) => setProgress((current) => ({ ...current, [key]: Math.round((event.progress ?? 0) * 100) })),
          });
          uploaded++;
        } catch (error) {
          problems.push(t("failed", { name: file.name, error: errorMessage(error) }));
          setErrors([...problems]);
        } finally {
          setProgress((current) => {
            const rest = { ...current };
            delete rest[key];
            return rest;
          });
        }
      }
      return uploaded;
    },
    [t, errorMessage],
  );

  return { upload, progress, errors, busy: Object.keys(progress).length > 0, clearErrors: () => setErrors([]) };
}
