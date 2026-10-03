export const TITLE_MAX = 60;
export const DESCRIPTION_MAX = 160;

/** Corta o texto no limite, preferindo o fim de uma palavra, e marca o corte com reticências. */
export function clampText(text: string, max: number): string {
  const clean = text.replace(/\s+/g, " ").trim();
  if (clean.length <= max) return clean;
  const slice = clean.slice(0, max - 1);
  const lastSpace = slice.lastIndexOf(" ");
  const cut = lastSpace > max * 0.5 ? slice.slice(0, lastSpace) : slice;
  return `${cut.replace(/[\s,.;:–-]+$/, "")}…`;
}

/** "Título | Marca" quando cabe em 60 caracteres; senão só o título, cortado. */
export function pageTitle(title: string, siteName: string): string {
  const full = `${title.trim()} | ${siteName}`;
  return full.length <= TITLE_MAX ? full : clampText(title, TITLE_MAX);
}
