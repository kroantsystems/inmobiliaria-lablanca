"use client";

import { ChevronLeft, ChevronRight } from "lucide-react";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { useState } from "react";
import type { PublicImage } from "@/lib/api/types";

/** Foto principal + miniaturas. Todas as imagens já vêm no HTML; só a primeira tem prioridade de carregamento. */
export function PropertyGallery({ images, title }: { images: PublicImage[]; title: string }) {
  const t = useTranslations("property");
  const [index, setIndex] = useState(0);

  if (images.length === 0) {
    return <div className="aspect-[16/10] rounded-[var(--radius-panel)] bg-lb-panel" />;
  }

  const current = images[index];
  const go = (delta: number) => setIndex((value) => (value + delta + images.length) % images.length);
  const altFor = (image: PublicImage, position: number) => image.alt || `${title} – ${t("photo", { index: position + 1 })}`;
  const arrow =
    "absolute top-1/2 flex size-10 -translate-y-1/2 items-center justify-center rounded-full bg-white/90 text-lb-ink shadow-[var(--shadow-raised)] transition hover:bg-white";

  return (
    <section aria-label={t("gallery")}>
      <div
        className="relative aspect-[16/10] overflow-hidden rounded-[var(--radius-panel)] bg-lb-panel"
        onKeyDown={(event) => {
          if (event.key === "ArrowLeft") go(-1);
          if (event.key === "ArrowRight") go(1);
        }}
      >
        <Image
          key={current.url}
          src={current.url}
          alt={altFor(current, index)}
          fill
          priority={index === 0}
          sizes="(min-width: 1024px) 760px, 100vw"
          className="object-cover"
        />
        {images.length > 1 ? (
          <>
            <button type="button" onClick={() => go(-1)} className={`${arrow} left-3`} aria-label={t("previousPhoto")}>
              <ChevronLeft className="size-5" aria-hidden />
            </button>
            <button type="button" onClick={() => go(1)} className={`${arrow} right-3`} aria-label={t("nextPhoto")}>
              <ChevronRight className="size-5" aria-hidden />
            </button>
            <span className="absolute right-3 bottom-3 rounded-lg bg-lb-ink/75 px-2 py-1 text-xs font-bold text-white" aria-live="polite">
              {index + 1} / {images.length}
            </span>
          </>
        ) : null}
      </div>

      {images.length > 1 ? (
        <ul className="mt-3 flex gap-2 overflow-x-auto pb-1">
          {images.map((image, position) => (
            <li key={image.url} className="shrink-0">
              <button
                type="button"
                onClick={() => setIndex(position)}
                aria-label={t("photo", { index: position + 1 })}
                aria-current={position === index ? "true" : undefined}
                className={`relative block h-16 w-24 overflow-hidden rounded-lg border-2 transition ${
                  position === index ? "border-lb-blue" : "border-transparent opacity-75 hover:opacity-100"
                }`}
              >
                <Image src={image.url} alt={altFor(image, position)} fill sizes="96px" className="object-cover" />
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
