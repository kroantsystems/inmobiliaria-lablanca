"use client";

import { useQuery } from "@tanstack/react-query";
import { ArrowLeft } from "lucide-react";
import { useParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { PropertyActions } from "@/components/admin/PropertyActions";
import { PropertyForm } from "@/components/admin/PropertyForm";
import { PropertyMedia } from "@/components/admin/PropertyMedia";
import { Loading, LoadError, Panel } from "@/components/admin/ui";
import { Link } from "@/i18n/navigation";
import { apiGet, keys } from "@/lib/admin/queries";
import type { AdminProperty } from "@/lib/api/types";

export default function PropertyEditorPage() {
  const t = useTranslations("admin.properties");
  const { id } = useParams<{ id: string }>();
  const isNew = id === "new";
  const property = useQuery({ queryKey: keys.property(id), queryFn: () => apiGet<AdminProperty>(`/api/admin/properties/${id}`), enabled: !isNew });

  const back = (
    <Link href="/admin/properties" className="inline-flex items-center gap-1.5 text-sm font-bold text-lb-blue hover:underline">
      <ArrowLeft className="size-4" aria-hidden />
      {t("back")}
    </Link>
  );

  if (isNew) {
    return (
      <div className="space-y-5">
        {back}
        <PropertyForm initial={null} />
        <Panel title={t("form.sectionMedia")}>
          <p className="text-sm text-lb-muted">{t("form.saveFirst")}</p>
        </Panel>
      </div>
    );
  }

  if (property.isPending) return <Loading />;
  if (property.isError) return <LoadError onRetry={() => property.refetch()} />;

  return (
    <div className="space-y-5">
      {back}
      <div className="grid items-start gap-6 xl:grid-cols-[minmax(0,1fr)_320px]">
        <div className="space-y-6">
          <PropertyForm key={property.data.id} initial={property.data} />
          <PropertyMedia property={property.data} />
        </div>
        <div className="xl:sticky xl:top-6">
          <PropertyActions property={property.data} />
        </div>
      </div>
    </div>
  );
}
