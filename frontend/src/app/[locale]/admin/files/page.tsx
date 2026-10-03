"use client";

import { CloudUpload, Download, FileText, Film, Pencil, Trash2 } from "lucide-react";
import { useLocale, useTranslations } from "next-intl";
import { useMemo, useRef, useState } from "react";
import { AdminImage } from "@/components/admin/AdminImage";
import { Badge, btn, Drawer, Field, FilterSelect, Loading, LoadError, Panel, SearchInput, ui, useToast } from "@/components/admin/ui";
import { UploadStatus } from "@/components/admin/UploadStatus";
import { useUploader } from "@/components/admin/useUploader";
import { useClientTable } from "@/hooks/useClientTable";
import { useDebounce } from "@/hooks/useDebounce";
import { keys, useAdminMutation, useErrorMessage, useFiles, useProperties } from "@/lib/admin/queries";
import { api } from "@/lib/api/client";
import type { FileItem, MediaKind } from "@/lib/api/types";
import { formatDate, formatNumber } from "@/lib/format/format";
import { ACCEPT_ATTRIBUTE, ACCEPTED_EXTENSIONS_LABEL } from "@/lib/uploads/validation";
import { useQueryClient } from "@tanstack/react-query";

const EMPTY: FileItem[] = [];
const searchText = (file: FileItem) => [file.originalName, file.description, file.propertyTitle];
const KINDS: MediaKind[] = ["Image", "Video", "Document"];
const NO_SORTERS = { uploaded: (a: FileItem, b: FileItem) => a.uploadedAt.localeCompare(b.uploadedAt) };

function fileSize(bytes: number, locale: string) {
  return bytes >= 1024 * 1024 ? `${formatNumber(bytes / 1024 / 1024, locale, 1)} MB` : `${formatNumber(Math.max(1, Math.round(bytes / 1024)), locale)} KB`;
}

function FileIcon({ file }: { file: FileItem }) {
  if (file.kind === "Video") return <Film className="size-10 text-lb-muted" aria-hidden />;
  const isWord = /\.(docx?)$/i.test(file.originalName);
  return <FileText className={`size-10 ${isWord ? "text-lb-blue" : "text-lb-red"}`} aria-hidden />;
}

function EditFile({ file, onClose }: { file: FileItem; onClose: () => void }) {
  const t = useTranslations("admin.files");
  const tc = useTranslations("admin.common");
  const tm = useTranslations("admin.properties.media");
  const properties = useProperties();
  const [propertyId, setPropertyId] = useState(file.propertyId ?? "");
  const [description, setDescription] = useState(file.description ?? "");
  const [altText, setAltText] = useState(file.altText ?? "");
  const [isPublic, setIsPublic] = useState(file.isPublic);
  const save = useAdminMutation(
    () => api.put(`/api/admin/files/${file.id}`, { propertyId: propertyId || null, description: description.trim() || null, altText: altText.trim() || null, isPublic: isPublic && !!propertyId }),
    { invalidate: [keys.files, keys.properties, ...(propertyId ? [keys.property(propertyId)] : [])], success: tc("saved"), onSuccess: onClose },
  );

  return (
    <form
      onSubmit={(event) => {
        event.preventDefault();
        save.mutate(undefined);
      }}
      className="space-y-4"
    >
      <p className="text-sm font-bold break-all">{file.originalName}</p>
      <Field label={t("link")}>
        {(id) => (
          <select id={id} value={propertyId} onChange={(e) => setPropertyId(e.target.value)} className={ui.input}>
            <option value="">{t("noLink")}</option>
            {(properties.data?.items ?? []).map((property) => (
              <option key={property.id} value={property.id}>
                {property.title}
              </option>
            ))}
          </select>
        )}
      </Field>
      <Field label={t("description")}>{(id) => <textarea id={id} rows={3} value={description} onChange={(e) => setDescription(e.target.value)} className={ui.input} />}</Field>
      {file.kind === "Image" ? (
        <Field label={tm("alt")}>{(id) => <input id={id} value={altText} onChange={(e) => setAltText(e.target.value)} className={ui.input} />}</Field>
      ) : null}
      {file.kind !== "Document" ? (
        <label className="flex items-center gap-2 text-sm">
          <input type="checkbox" checked={isPublic} disabled={!propertyId} onChange={(e) => setIsPublic(e.target.checked)} className="size-4 accent-lb-blue" />
          {t("public")}
        </label>
      ) : null}
      <button type="submit" disabled={save.isPending} className={btn.primary}>
        {save.isPending ? tc("saving") : tc("save")}
      </button>
    </form>
  );
}

export default function FilesPage() {
  const t = useTranslations("admin.files");
  const ta = useTranslations("admin");
  const tm = useTranslations("admin.properties.media");
  const locale = useLocale();
  const notify = useToast();
  const errorMessage = useErrorMessage();
  const client = useQueryClient();
  const files = useFiles();
  const properties = useProperties();
  const { upload, progress, errors, busy } = useUploader();
  const input = useRef<HTMLInputElement>(null);
  const [selection, setSelection] = useState<File[]>([]);
  const [propertyId, setPropertyId] = useState("");
  const [description, setDescription] = useState("");
  const [isPublic, setIsPublic] = useState(false);
  const [search, setSearch] = useState("");
  const [kind, setKind] = useState("");
  const [link, setLink] = useState("");
  const [editing, setEditing] = useState<FileItem | null>(null);
  const term = useDebounce(search);
  const publishedIds = useMemo(() => new Set((properties.data?.items ?? []).filter((item) => item.isPublished).map((item) => item.id)), [properties.data]);

  const remove = useAdminMutation((id: string) => api.delete(`/api/admin/files/${id}`), { invalidate: [keys.files, keys.properties], success: ta("common.saved") });

  const filters = useMemo(
    () => [(file: FileItem) => !kind || file.kind === kind, (file: FileItem) => !link || (link === "linked" ? file.propertyId !== null : file.propertyId === null)],
    [kind, link],
  );
  const table = useClientTable<FileItem, "uploaded">(files.data?.items ?? EMPTY, { search: term, searchText, filters, sorters: NO_SORTERS, initialSort: { key: "uploaded", direction: "desc" } });

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    if (selection.length === 0) return;
    const sent = await upload(selection, { propertyId: propertyId || null, description: description.trim() || undefined, isPublic: isPublic && !!propertyId });
    if (sent > 0) {
      notify(t("saved", { count: sent }));
      setSelection([]);
      setDescription("");
      if (input.current) input.current.value = "";
      await Promise.all([keys.files, keys.properties].map((queryKey) => client.invalidateQueries({ queryKey })));
    }
  }

  async function download(file: FileItem) {
    try {
      const { data } = await api.get<Blob>(file.url, { responseType: "blob" });
      const url = URL.createObjectURL(data);
      const anchor = Object.assign(document.createElement("a"), { href: url, download: file.originalName });
      anchor.click();
      setTimeout(() => URL.revokeObjectURL(url), 1000);
    } catch (error) {
      notify(errorMessage(error), "error");
    }
  }

  return (
    <div className="space-y-6">
      <Panel title={t("form")}>
        <form onSubmit={submit}>
          <div className="grid gap-4 sm:grid-cols-2">
            <Field label={`${t("files")} *`} hint={tm("accepted", { types: ACCEPTED_EXTENSIONS_LABEL })}>
              {(id) => (
                <input
                  id={id}
                  ref={input}
                  type="file"
                  multiple
                  required
                  accept={ACCEPT_ATTRIBUTE}
                  onChange={(e) => setSelection(Array.from(e.target.files ?? []))}
                  className={`${ui.input} file:mr-3 file:rounded-md file:border-0 file:bg-lb-blue file:px-3 file:py-1 file:text-xs file:font-bold file:text-white`}
                />
              )}
            </Field>
            <Field label={t("link")}>
              {(id) => (
                <select id={id} value={propertyId} onChange={(e) => setPropertyId(e.target.value)} className={ui.input}>
                  <option value="">{t("noLink")}</option>
                  {(properties.data?.items ?? []).map((property) => (
                    <option key={property.id} value={property.id}>
                      {property.title}
                    </option>
                  ))}
                </select>
              )}
            </Field>
            <Field label={t("description")} full>
              {(id) => <textarea id={id} rows={2} value={description} onChange={(e) => setDescription(e.target.value)} className={ui.input} />}
            </Field>
          </div>
          <label className="mt-3 flex items-center gap-2 text-sm">
            <input type="checkbox" checked={isPublic && !!propertyId} disabled={!propertyId} onChange={(e) => setIsPublic(e.target.checked)} className="size-4 accent-lb-blue" />
            {t("public")}
          </label>
          <UploadStatus progress={progress} errors={errors} />
          <button type="submit" disabled={busy || selection.length === 0} className={`${btn.primary} mt-4`}>
            <CloudUpload className="size-4" aria-hidden />
            {t("save")}
          </button>
        </form>
      </Panel>

      <Panel title={t("library")}>
        <div className="mb-4 flex flex-wrap items-center gap-2">
          <SearchInput value={search} onChange={setSearch} placeholder={t("searchPlaceholder")} />
          <FilterSelect label={t("filterKind")} value={kind} onChange={setKind} options={KINDS.map((value) => ({ value, label: t(`kind.${value}`) }))} />
          <FilterSelect
            label={t("filterLink")}
            value={link}
            onChange={setLink}
            options={[
              { value: "linked", label: t("linked") },
              { value: "unlinked", label: t("unlinked") },
            ]}
          />
          <span className="ml-auto text-xs text-lb-muted">{ta("common.showing", { count: table.items.length, total: table.total })}</span>
        </div>

        {files.isPending ? (
          <Loading />
        ) : files.isError ? (
          <LoadError onRetry={() => files.refetch()} />
        ) : table.total === 0 ? (
          <p className="py-8 text-center text-sm text-lb-muted">{t("empty")}</p>
        ) : table.items.length === 0 ? (
          <p className="py-8 text-center text-sm text-lb-muted">{ta("common.noResults")}</p>
        ) : (
          <ul className="grid grid-cols-[repeat(auto-fill,minmax(230px,1fr))] gap-4">
            {table.items.map((file) => (
              <li key={file.id} className="flex flex-col gap-2 rounded-lg border border-lb-border bg-lb-bg p-3">
                <div className="relative flex h-28 items-center justify-center overflow-hidden rounded-md bg-slate-200">
                  {file.kind === "Image" ? (
                    <AdminImage publicUrl={file.publicUrl} adminUrl={file.url} published={publishedIds.has(file.propertyId ?? "")} alt={file.altText ?? ""} sizes="260px" />
                  ) : (
                    <FileIcon file={file} />
                  )}
                </div>
                <div>
                  <Badge tone={file.propertyTitle ? "blue" : "gray"}>{file.propertyTitle ?? t("unlinked")}</Badge>
                </div>
                <p className="truncate text-sm font-bold" title={file.originalName}>
                  {file.originalName}
                </p>
                <p className="line-clamp-2 text-xs text-lb-muted">{file.description || t("noDescription")}</p>
                <p className="text-[0.7rem] text-lb-muted">
                  {fileSize(file.sizeBytes, locale)} · {formatDate(file.uploadedAt, locale, { dateStyle: "short" })} · {file.isPublic ? t("publicBadge") : t("private")}
                </p>
                <div className="mt-auto flex justify-end gap-1">
                  <button type="button" onClick={() => void download(file)} className={ui.iconButton} aria-label={`${ta("common.download")} ${file.originalName}`} title={ta("common.download")}>
                    <Download className="size-4" aria-hidden />
                  </button>
                  <button type="button" onClick={() => setEditing(file)} className={ui.iconButton} aria-label={`${t("edit")} ${file.originalName}`} title={t("edit")}>
                    <Pencil className="size-4" aria-hidden />
                  </button>
                  <button
                    type="button"
                    onClick={() => {
                      if (window.confirm(ta("common.confirmDelete"))) remove.mutate(file.id);
                    }}
                    className={`${ui.iconButton} hover:text-lb-red`}
                    aria-label={`${ta("common.delete")} ${file.originalName}`}
                    title={ta("common.delete")}
                  >
                    <Trash2 className="size-4" aria-hidden />
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Panel>

      <Drawer open={editing !== null} onClose={() => setEditing(null)} title={t("edit")}>
        {editing ? <EditFile key={editing.id} file={editing} onClose={() => setEditing(null)} /> : null}
      </Drawer>
    </div>
  );
}
