"use client";

import { useLocale } from "next-intl";
import { usePathname } from "next/navigation";
import { useEffect } from "react";
import { track } from "@/lib/analytics";

export function PageViewTracker() {
  const pathname = usePathname();
  const locale = useLocale();

  useEffect(() => {
    track("PageView", { locale, path: pathname });
  }, [pathname, locale]);

  return null;
}

export function PropertyViewTracker({ propertyId }: { propertyId: string }) {
  const locale = useLocale();

  useEffect(() => {
    track("PropertyView", { locale, propertyId });
  }, [propertyId, locale]);

  return null;
}
