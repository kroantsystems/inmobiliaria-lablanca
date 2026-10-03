export type UploadKind = "image" | "video" | "document";

export type UploadValidation =
  | { ok: true; kind: UploadKind }
  | { ok: false; error: "typeNotAllowed" }
  | { ok: false; error: "tooLarge"; limitMb: number };

// Mesmas regras da API (Uploads:* no appsettings); a API valida de novo, inclusive o conteúdo.
export const UPLOAD_RULES: Record<UploadKind, { extensions: string[]; limitMb: number }> = {
  image: { extensions: [".png", ".jpg", ".jpeg", ".webp"], limitMb: 50 },
  video: { extensions: [".mp4", ".mov", ".webm"], limitMb: 50 },
  document: { extensions: [".pdf", ".doc", ".docx"], limitMb: 15 },
};

export const ACCEPT_ATTRIBUTE = Object.values(UPLOAD_RULES)
  .flatMap((rule) => rule.extensions)
  .join(",");

export const ACCEPTED_EXTENSIONS_LABEL = Object.values(UPLOAD_RULES)
  .flatMap((rule) => rule.extensions.map((ext) => ext.slice(1).toUpperCase()))
  .join(", ");

export function validateUpload(file: { name: string; size: number }): UploadValidation {
  const dot = file.name.lastIndexOf(".");
  const extension = dot >= 0 ? file.name.slice(dot).toLowerCase() : "";
  const entry = (Object.entries(UPLOAD_RULES) as [UploadKind, (typeof UPLOAD_RULES)[UploadKind]][]).find(([, rule]) =>
    rule.extensions.includes(extension),
  );

  if (!entry) return { ok: false, error: "typeNotAllowed" };

  const [kind, rule] = entry;
  if (file.size > rule.limitMb * 1024 * 1024) return { ok: false, error: "tooLarge", limitMb: rule.limitMb };

  return { ok: true, kind };
}
