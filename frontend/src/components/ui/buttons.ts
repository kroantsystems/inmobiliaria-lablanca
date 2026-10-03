// Botões do protótipo (pílula com sombra colorida e leve elevação no hover).
const base =
  "inline-flex items-center justify-center gap-2 rounded-full px-6 py-3 text-sm font-bold text-white transition hover:-translate-y-0.5 disabled:pointer-events-none disabled:opacity-60";

export const buttons = {
  red: `${base} bg-lb-red shadow-lg shadow-lb-red/30 hover:bg-lb-red-hover`,
  blue: `${base} bg-lb-blue shadow-lg shadow-lb-blue/30 hover:bg-lb-blue-hover`,
  // Verde mais escuro que o da marca WhatsApp para manter contraste AA com texto branco.
  whatsapp: `${base} bg-[#0f7a41] shadow-lg shadow-whatsapp/30 hover:bg-[#0b6234]`,
  ghostLight: `${base} border border-white/40 hover:bg-white hover:text-lb-ink`,
  small: "inline-flex items-center gap-1.5 rounded-full bg-lb-blue px-4 py-2 text-xs font-bold text-white shadow-md shadow-lb-blue/30 transition hover:bg-lb-blue-hover",
};
