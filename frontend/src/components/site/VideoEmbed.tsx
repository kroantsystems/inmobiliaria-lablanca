"use client";

import { Play } from "lucide-react";
import Image from "next/image";
import { useTranslations } from "next-intl";
import { useState } from "react";
import { videoEmbedUrl } from "@/lib/video";

/** O player externo só carrega depois do clique: nada de YouTube/Vimeo no carregamento da página. */
export function VideoEmbed({ url, title, poster }: { url: string; title: string; poster?: string }) {
  const t = useTranslations("property");
  const [playing, setPlaying] = useState(false);
  const src = videoEmbedUrl(url);

  if (!src) {
    return (
      <a href={url} target="_blank" rel="noopener noreferrer" className="font-bold text-lb-blue underline">
        {t("playVideo")}
      </a>
    );
  }

  return (
    <div className="relative aspect-video overflow-hidden rounded-[var(--radius-card)] bg-lb-ink">
      {playing ? (
        <iframe
          src={`${src}?autoplay=1`}
          title={title}
          allow="autoplay; encrypted-media; picture-in-picture; fullscreen"
          allowFullScreen
          referrerPolicy="strict-origin-when-cross-origin"
          className="absolute inset-0 size-full"
        />
      ) : (
        <button type="button" onClick={() => setPlaying(true)} className="group absolute inset-0 flex items-center justify-center">
          {poster ? <Image src={poster} alt="" fill sizes="(min-width: 1024px) 760px, 100vw" className="object-cover opacity-60" /> : null}
          <span className="relative flex size-16 items-center justify-center rounded-full bg-lb-red text-white shadow-lg transition group-hover:scale-110">
            <Play className="size-7 translate-x-0.5" aria-hidden />
          </span>
          <span className="sr-only">{t("playVideo")}</span>
        </button>
      )}
    </div>
  );
}
