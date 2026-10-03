import { readFile } from "node:fs/promises";
import { join } from "node:path";
import { API_URL } from "@/lib/api/server";
import type { PublicImage } from "@/lib/api/types";

export const OG_SIZE = { width: 1200, height: 630 };

// Preço e título mudam: cache curto no navegador e um pouco maior em CDN.
export const OG_HEADERS = { "Cache-Control": "public, max-age=3600, s-maxage=86400" };

/** Logo com "CIUDAD DEL ESTE" em branco, para fundo escuro. */
export async function logoDataUrl(): Promise<string> {
  return `data:image/png;base64,${await readFile(join(process.cwd(), "public/brand/logo-dark.png"), "base64")}`;
}

/** Capa recortada em 1200×630 JPEG (o gerador não lê WebP/AVIF). Sem capa ou em erro, devolve null. */
export async function coverDataUrl(image: PublicImage | undefined): Promise<string | null> {
  if (!image) return null;
  try {
    // Fotos podem ter dezenas de MB: sem cache de dados (limite de 2 MB por item).
    const response = await fetch(`${API_URL}${image.url}`, { cache: "no-store" });
    if (!response.ok) return null;
    const original = Buffer.from(await response.arrayBuffer());
    try {
      const sharp = (await import("sharp")).default;
      const jpeg = await sharp(original).resize(OG_SIZE.width, OG_SIZE.height, { fit: "cover" }).jpeg({ quality: 80 }).toBuffer();
      return `data:image/jpeg;base64,${jpeg.toString("base64")}`;
    } catch {
      const type = response.headers.get("content-type") ?? image.contentType;
      return /^image\/(png|jpeg)$/.test(type) ? `data:${type};base64,${original.toString("base64")}` : null;
    }
  } catch {
    return null;
  }
}
