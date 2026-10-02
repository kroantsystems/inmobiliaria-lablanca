const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5080";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...init?.headers,
    },
  });

  if (!response.ok) {
    throw new ApiError(response.status, `Erro ${response.status} ao chamar ${path}`);
  }

  return (await response.json()) as T;
}

export type HealthStatus = {
  status: "Healthy" | "Degraded" | "Unhealthy";
  checks: Record<string, string>;
};

export async function getHealth(): Promise<HealthStatus> {
  return apiFetch<HealthStatus>("/health", { cache: "no-store" });
}
