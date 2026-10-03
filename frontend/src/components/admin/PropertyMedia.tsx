"use client";

import { ArrowLeft, ArrowRight, CloudUpload, Film, GripVertical, Star, Trash2 } from "lucide-react";
import { useTranslations } from "next-intl";
import { useRef, useState } from "react";
import { keys, useAdminMutation } from "@/lib/admin/queries";
import { api } from "@/lib/api/client";
import type { AdminMedia, AdminProperty } from "@/lib/api/types";
import { UPLOAD_RULES } from "@/lib/uploads/validation";
import { useQueryClient } from "@tanstack/react-query";
import { AdminImage } from "./AdminImage";
import { Panel, ui } from "./ui";
import { UploadStatus } from "./UploadStatus";
import { useUploader } from "./useUploader";

const GALLERY_EXTENSIONS = [...UPLOAD_RULES.image.extensions, ...UPLOAD_RULES.video.extensions];
const GALLERY_ACCEPT = GALLERY_EXTENSIONS.join(",");
const GALLERY_TYPES = GALLERY_EXTENSIONS.map((ext) => ext.slice(1).toUpperCase()).join(", ");

function MediaCard({
  media,
  published,
  index,
  total,
  onMove,
  onCover,
  onDelete,
  onAlt,
  dragHandlers,
}: {
  media: AdminMedia;
  published: boolean;
  index: number;
  total: number;
  onMove: (from: number, to: number) => void;
  onCover: () => void;
  onDelete: () => void;
  onAlt: (alt: string) => void;
  dragHandlers: React.HTMLAttributes<HTMLLIElement>;
}) {
  const t = useTranslations("admin.properties.media");
  const [alt, setAlt] = useState(media.altText ?? "");

  return (
    <li draggable {...dragHandlers} className="flex flex-col overflow-hidden rounded-lg border border-lb-border bg-lb-bg">
      <div className="relative aspect-[4/3] bg-lb-panel">
        {media.kind === "Image" ? (
          <AdminImage publicUrl={media.publicUrl} adminUrl={media.url} published={published} alt={media.altText ?? ""} sizes="240px" />
        ) : (
          <Film className="absolute inset-0 m-auto size-10 text-lb-muted" aria-hidden />
        )}
        <span className="absolute top-2 left-2 flex size-7 cursor-grab items-center justify-center rounded-md bg-white/90 text-lb-muted" aria-hidden>
          <GripVertical className="size-4" />
        </span>
        {media.isCover ? <span className="absolute top-2 right-2 rounded-full bg-lb-red px-2 py-0.5 text-[0.65rem] font-extrabold text-white uppercase">{t("cover")}</span> : null}
      </div>
      <div className="flex flex-1 flex-col gap-2 p-2.5">
        <p className="truncate text-xs font-bold" title={media.originalName}>
          {media.originalName}
        </p>
        {media.kind === "Image" ? (
          <input
            value={alt}
            onChange={(e) => setAlt(e.target.value)}
            onBlur={() => {
              if (alt !== (media.altText ?? "")) onAlt(alt);
            }}
            placeholder={t("alt")}
            aria-label={`${t("alt")} – ${media.originalName}`}
            title={t("altHint")}
            className={`${ui.input} py-1.5 text-xs`}
          />
        ) : null}
        <div className="mt-auto flex items-center justify-between gap-1">
          <div className="flex">
            <button type="button" disabled={index === 0} onClick={() => onMove(index, index - 1)} className={ui.iconButton} aria-label={t("moveLeft")} title={t("moveLeft")}>
              <ArrowLeft className="size-4" aria-hidden />
            </button>
            <button type="button" disabled={index === total - 1} onClick={() => onMove(index, index + 1)} className={ui.iconButton} aria-label={t("moveRight")} title={t("moveRight")}>
              <ArrowRight className="size-4" aria-hidden />
            </button>
          </div>
          <div className="flex">
            {media.kind === "Image" && !media.isCover ? (
              <button type="button" onClick={onCover} className={ui.iconButton} aria-label={t("setCover")} title={t("setCover")}>
                <Star className="size-4" aria-hidden />
              </button>
            ) : null}
            <button type="button" onClick={onDelete} className={`${ui.iconButton} hover:text-lb-red`} aria-label={t("remove")} title={t("remove")}>
              <Trash2 className="size-4" aria-hidden />
            </button>
          </div>
        </div>
      </div>
    </li>
  );
}

function Gallery({ property }: { property: AdminProperty }) {
  const t = useTranslations("admin.properties.media");
  const tc = useTranslations("admin.common");
  const gallery = property.media.filter((item) => item.kind !== "Document").sort((a, b) => a.sortOrder - b.sortOrder);
  const [order, setOrder] = useState(gallery);
  const dragged = useRef<number | null>(null);
  const invalidate = [keys.property(property.id), keys.properties, keys.files];

  const reorder = useAdminMutation((ids: string[]) => api.put(`/api/admin/properties/${property.id}/media/order`, { mediaIds: ids }), { invalidate });
  const cover = useAdminMutation((id: string) => api.put(`/api/admin/properties/${property.id}/media/${id}/cover`), { invalidate });
  const remove = useAdminMutation((id: string) => api.delete(`/api/admin/files/${id}`), { invalidate });
  const alt = useAdminMutation(
    ({ media, altText }: { media: AdminMedia; altText: string }) =>
      api.put(`/api/admin/files/${media.id}`, { propertyId: property.id, description: media.description, altText: altText || null, isPublic: media.isPublic }),
    { invalidate: [keys.files], success: tc("saved") },
  );

  function move(from: number, to: number) {
    if (from === to || to < 0 || to >= order.length) return;
    const next = [...order];
    const [item] = next.splice(from, 1);
    next.splice(to, 0, item);
    setOrder(next);
    reorder.mutate(next.map((media) => media.id));
  }

  if (order.length === 0) return <p className="text-sm text-lb-muted">{t("empty")}</p>;

  return (
    <>
      <p className="mb-3 text-xs text-lb-muted">{t("reorderHint")}</p>
      <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 xl:grid-cols-4">
        {order.map((media, index) => (
          <MediaCard
            key={media.id}
            media={media}
            published={property.isPublished}
            index={index}
            total={order.length}
            onMove={move}
            onCover={() => cover.mutate(media.id)}
            onDelete={() => {
              if (window.confirm(tc("confirmDelete"))) remove.mutate(media.id);
            }}
            onAlt={(altText) => alt.mutate({ media, altText })}
            dragHandlers={{
              onDragStart: () => {
                dragged.current = index;
              },
              onDragOver: (event) => event.preventDefault(),
              onDrop: (event) => {
                event.preventDefault();
                if (dragged.current !== null) move(dragged.current, index);
                dragged.current = null;
              },
            }}
          />
        ))}
      </ul>
    </>
  );
}

/** Galeria do anúncio: envio múltiplo com progresso, ordem por arrastar, capa, texto alternativo e exclusão. */
export function PropertyMedia({ property }: { property: AdminProperty }) {
  const t = useTranslations("admin.properties.media");
  const tf = useTranslations("admin.properties.form");
  const client = useQueryClient();
  const { upload, progress, errors, busy } = useUploader();
  const input = useRef<HTMLInputElement>(null);
  const [over, setOver] = useState(false);

  async function send(files: FileList | null) {
    if (!files || files.length === 0) return;
    const sent = await upload(Array.from(files), { propertyId: property.id, isPublic: true });
    if (input.current) input.current.value = "";
    if (sent > 0) await Promise.all([keys.property(property.id), keys.properties, keys.files].map((queryKey) => client.invalidateQueries({ queryKey })));
  }

  return (
    <Panel title={tf("sectionMedia")}>
      <label
        onDragOver={(event) => {
          event.preventDefault();
          setOver(true);
        }}
        onDragLeave={() => setOver(false)}
        onDrop={(event) => {
          event.preventDefault();
          setOver(false);
          void send(event.dataTransfer.files);
        }}
        className={`mb-5 flex cursor-pointer flex-col items-center rounded-xl border-2 border-dashed px-4 py-6 text-center transition ${over ? "border-lb-blue bg-sky-50" : "border-lb-border bg-lb-bg"}`}
      >
        <CloudUpload className="mb-2 size-8 text-lb-blue" aria-hidden />
        <span className="text-sm font-bold">{t("upload")}</span>
        <span className="text-xs text-lb-muted">{t("dropHint")}</span>
        <span className="mt-1 text-xs text-lb-muted">{t("accepted", { types: GALLERY_TYPES })}</span>
        <input ref={input} type="file" multiple accept={GALLERY_ACCEPT} disabled={busy} onChange={(e) => void send(e.target.files)} className="sr-only" />
      </label>
      <UploadStatus progress={progress} errors={errors} />
      <div className="mt-4">
        <Gallery key={property.media.map((media) => `${media.id}:${media.sortOrder}:${media.isCover}`).join()} property={property} />
      </div>
    </Panel>
  );
}
