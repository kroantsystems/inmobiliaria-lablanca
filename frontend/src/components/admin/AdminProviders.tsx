"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState } from "react";
import { ApiLocaleSync } from "@/lib/api/ApiLocaleSync";
import { AuthProvider } from "@/lib/auth/AuthProvider";

export function AdminProviders({ children }: { children: React.ReactNode }) {
  const [queryClient] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: { staleTime: 30_000, refetchOnWindowFocus: false, retry: 1 },
        },
      }),
  );

  return (
    <QueryClientProvider client={queryClient}>
      {/* Só o painel usa o cliente Axios; as páginas públicas não carregam essa dependência. */}
      <ApiLocaleSync />
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  );
}
