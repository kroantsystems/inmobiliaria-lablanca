import { getHealth } from "@/lib/api";

export async function ApiStatus() {
  const online = await getHealth()
    .then((health) => health.status === "Healthy")
    .catch(() => false);

  return (
    <p className="inline-flex items-center gap-2 text-xs text-stone-500" role="status">
      <span className={`h-2 w-2 rounded-full ${online ? "bg-emerald-500" : "bg-red-500"}`} aria-hidden />
      API {online ? "conectada" : "indisponível"}
    </p>
  );
}

export function ApiStatusFallback() {
  return (
    <p className="inline-flex items-center gap-2 text-xs text-stone-400" role="status">
      <span className="h-2 w-2 animate-pulse rounded-full bg-stone-300" aria-hidden />
      Verificando API…
    </p>
  );
}
