import createMiddleware from "next-intl/middleware";
import type { NextRequest } from "next/server";
import { routing } from "./i18n/routing";

const handleI18nRouting = createMiddleware(routing);

export function proxy(request: NextRequest) {
  return handleI18nRouting(request);
}

// Fora do proxy: API (o proxy bufferiza o corpo em até 10 MB e quebraria uploads), imagens OG, revalidação,
// internos do Next e arquivos com extensão (sitemap.xml, robots.txt, llms.txt, ícones).
export const config = {
  matcher: ["/((?!api|og|revalidate|_next|_vercel|.*\\..*).*)"],
};
