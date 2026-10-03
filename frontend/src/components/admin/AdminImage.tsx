"use client";

import { useQuery } from "@tanstack/react-query";
import Image from "next/image";
import { adminMediaUrl } from "@/lib/admin/media";
import { api } from "@/lib/api/client";

/** Busca a imagem com o token do painel e mostra como blob (rascunhos não são servidos pela rota pública). */
function AuthImage({ src, alt, className }: { src: string; alt: string; className?: string }) {
  const blob = useQuery({
    queryKey: ["media-blob", src],
    queryFn: async () => URL.createObjectURL((await api.get<Blob>(src, { responseType: "blob" })).data),
    staleTime: Infinity,
    gcTime: 10 * 60_000,
  });
  if (!blob.data) return <span className="absolute inset-0 animate-pulse bg-slate-200" aria-hidden />;
  return <Image src={blob.data} alt={alt} fill unoptimized className={className} />;
}

type AdminImageProps = {
  /** URL pública (só funciona para anúncios publicados). */
  publicUrl: string | null;
  /** URL autenticada do admin; se omitida, é derivada da pública. */
  adminUrl?: string;
  published: boolean;
  alt: string;
  sizes: string;
  className?: string;
};

/** Miniatura no painel: otimizada pela rota pública quando o anúncio está no ar; senão, pela rota autenticada. */
export function AdminImage({ publicUrl, adminUrl, published, alt, sizes, className = "object-cover" }: AdminImageProps) {
  if (publicUrl && published) return <Image src={publicUrl} alt={alt} fill sizes={sizes} className={className} />;
  const src = adminUrl ?? (publicUrl ? adminMediaUrl(publicUrl) : null);
  return src ? <AuthImage src={src} alt={alt} className={className} /> : null;
}
