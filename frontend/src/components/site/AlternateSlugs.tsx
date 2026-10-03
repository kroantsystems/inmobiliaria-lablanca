"use client";

import { createContext, useContext, useEffect, useState } from "react";

type Slugs = Record<string, string>;

const AlternateSlugsContext = createContext<{ slugs: Slugs | null; setSlugs: (slugs: Slugs | null) => void } | null>(null);

/** Guarda os slugs traduzidos da página atual para o seletor de idioma levar ao endereço certo. */
export function AlternateSlugsProvider({ children }: { children: React.ReactNode }) {
  const [slugs, setSlugs] = useState<Slugs | null>(null);
  return <AlternateSlugsContext.Provider value={{ slugs, setSlugs }}>{children}</AlternateSlugsContext.Provider>;
}

export function useAlternateSlugs() {
  return useContext(AlternateSlugsContext)?.slugs ?? null;
}

/** Usado pela página de anúncio: publica os slugs enquanto ela estiver aberta. */
export function SetAlternateSlugs({ slugs }: { slugs: Slugs }) {
  const setSlugs = useContext(AlternateSlugsContext)?.setSlugs;
  const key = JSON.stringify(slugs);
  useEffect(() => {
    setSlugs?.(JSON.parse(key));
    return () => setSlugs?.(null);
  }, [key, setSlugs]);
  return null;
}
