"use client";

import dynamic from "next/dynamic";
import { useTranslations } from "next-intl";
import { useEffect, useRef, useState } from "react";
import type { PropertyCard } from "@/lib/api/types";

const PropertiesMap = dynamic(() => import("./PropertiesMap"), { ssr: false });

/** O código do mapa (Leaflet) só é baixado quando a seção se aproxima da área visível. */
export function LazyMap({ properties, single = false, className = "h-[420px]" }: { properties: PropertyCard[]; single?: boolean; className?: string }) {
  const t = useTranslations("home");
  const ref = useRef<HTMLDivElement>(null);
  const [visible, setVisible] = useState(false);

  useEffect(() => {
    const element = ref.current;
    if (!element) return;
    const observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          setVisible(true);
          observer.disconnect();
        }
      },
      { rootMargin: "300px" },
    );
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  return (
    <div ref={ref} className={`relative isolate overflow-hidden rounded-[var(--radius-panel)] border border-lb-border bg-lb-panel shadow-[var(--shadow-raised)] ${className}`}>
      {visible ? (
        <PropertiesMap properties={properties} single={single} />
      ) : (
        <p className="flex h-full items-center justify-center text-sm text-lb-muted">{t("mapLoading")}</p>
      )}
    </div>
  );
}
