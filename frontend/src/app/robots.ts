import type { MetadataRoute } from "next";
import { SITE_URL } from "@/lib/site";

// Robôs de busca e de assistentes de IA podem ler o site inteiro, menos o painel e a API (exceto as fotos públicas).
const AI_BOTS = [
  "GPTBot",
  "OAI-SearchBot",
  "ChatGPT-User",
  "ClaudeBot",
  "Claude-SearchBot",
  "Claude-User",
  "PerplexityBot",
  "Perplexity-User",
  "Google-Extended",
  "Applebot-Extended",
];

export default function robots(): MetadataRoute.Robots {
  const allow = ["/", "/api/public/media/"];
  const disallow = ["/api/", "/*/admin"];
  return {
    rules: [
      { userAgent: "*", allow, disallow },
      { userAgent: AI_BOTS, allow, disallow },
    ],
    sitemap: `${SITE_URL}/sitemap.xml`,
    host: SITE_URL,
  };
}
