"use client";

import { useMutation, useQuery, useQueryClient, type QueryKey } from "@tanstack/react-query";
import { useTranslations } from "next-intl";
import { useToast } from "@/components/admin/ui";
import { api, problemOf } from "@/lib/api/client";
import type { AdminPropertyListItem, AdminSettings, AdminZone, FileItem, Lead, Owner, PagedResult, Visit } from "@/lib/api/types";

/** Páginas grandes (até 500): busca, filtros e ordenação acontecem no cliente. */
const BIG_PAGE = "?page=1&pageSize=500";

export const keys = {
  properties: ["properties"] as const,
  property: (id: string) => ["property", id] as const,
  leads: ["leads"] as const,
  owners: ["owners"] as const,
  files: ["files"] as const,
  zones: ["zones"] as const,
  settings: ["settings"] as const,
  visits: ["visits"] as const,
  dashboard: ["dashboard"] as const,
};

const get = async <T,>(url: string) => (await api.get<T>(url)).data;

export const useProperties = () => useQuery({ queryKey: keys.properties, queryFn: () => get<PagedResult<AdminPropertyListItem>>(`/api/admin/properties${BIG_PAGE}`) });
export const useLeads = () => useQuery({ queryKey: keys.leads, queryFn: () => get<PagedResult<Lead>>(`/api/admin/leads${BIG_PAGE}`) });
export const useOwners = () => useQuery({ queryKey: keys.owners, queryFn: () => get<PagedResult<Owner>>(`/api/admin/owners${BIG_PAGE}`) });
export const useFiles = () => useQuery({ queryKey: keys.files, queryFn: () => get<PagedResult<FileItem>>(`/api/admin/files${BIG_PAGE}`) });
export const useZones = () => useQuery({ queryKey: keys.zones, queryFn: () => get<AdminZone[]>("/api/admin/zones") });
export const useSettings = () => useQuery({ queryKey: keys.settings, queryFn: () => get<AdminSettings>("/api/admin/settings") });
export const useUpcomingVisits = (take = 10) =>
  useQuery({ queryKey: [...keys.visits, "upcoming", take], queryFn: () => get<Visit[]>(`/api/admin/visits/upcoming?take=${take}`) });

export { get as apiGet };

/** Mensagem de erro da API (Problem Details no idioma atual) ou genérica. */
export function useErrorMessage() {
  const t = useTranslations("admin.common");
  return (error: unknown) => {
    const problem = problemOf(error);
    const fieldErrors = Object.values(problem?.errors ?? {}).flat();
    return fieldErrors[0] ?? problem?.detail ?? t("error");
  };
}

/** Mutação que mostra o resultado e recarrega as listas afetadas. */
export function useAdminMutation<TVariables, TResult = unknown>(
  mutationFn: (variables: TVariables) => Promise<TResult>,
  { invalidate, success, onSuccess }: { invalidate: QueryKey[]; success?: string; onSuccess?: (result: TResult, variables: TVariables) => void },
) {
  const client = useQueryClient();
  const notify = useToast();
  const errorMessage = useErrorMessage();
  return useMutation({
    mutationFn,
    onSuccess: async (result, variables) => {
      await Promise.all(invalidate.map((queryKey) => client.invalidateQueries({ queryKey })));
      if (success) notify(success);
      onSuccess?.(result, variables);
    },
    onError: (error) => notify(errorMessage(error), "error"),
  });
}
