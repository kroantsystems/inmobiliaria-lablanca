const YOUTUBE_HOSTS = new Set(["youtube.com", "www.youtube.com", "m.youtube.com", "youtu.be", "www.youtube-nocookie.com"]);
const VIMEO_HOSTS = new Set(["vimeo.com", "www.vimeo.com", "player.vimeo.com"]);

/** URL de incorporação (YouTube sem cookies ou Vimeo, os únicos liberados na CSP) ou null. */
export function videoEmbedUrl(url: string): string | null {
  let parsed: URL;
  try {
    parsed = new URL(url);
  } catch {
    return null;
  }
  if (parsed.protocol !== "https:") return null;

  if (YOUTUBE_HOSTS.has(parsed.hostname)) {
    const parts = parsed.pathname.split("/").filter(Boolean);
    const id =
      parsed.hostname === "youtu.be" ? parts[0] : parsed.pathname === "/watch" ? parsed.searchParams.get("v") : ["shorts", "embed", "live"].includes(parts[0]) ? parts[1] : null;
    return id && /^[\w-]{11}$/.test(id) ? `https://www.youtube-nocookie.com/embed/${id}` : null;
  }

  if (VIMEO_HOSTS.has(parsed.hostname)) {
    const id = parsed.pathname.split("/").filter(Boolean).find((part) => /^\d+$/.test(part));
    return id ? `https://player.vimeo.com/video/${id}` : null;
  }

  return null;
}
