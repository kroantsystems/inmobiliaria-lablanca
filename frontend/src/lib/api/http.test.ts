import type { AxiosAdapter, AxiosResponse, InternalAxiosRequestConfig } from "axios";
import { describe, expect, it, vi } from "vitest";
import { createHttpClient } from "./http";

function respond(config: InternalAxiosRequestConfig, status: number, data: unknown = {}): AxiosResponse {
  return { config, status, statusText: String(status), headers: {}, data };
}

function rejectWith(config: InternalAxiosRequestConfig, status: number) {
  const error = Object.assign(new Error(`HTTP ${status}`), {
    isAxiosError: true,
    config,
    response: respond(config, status),
  });
  return Promise.reject(error);
}

describe("http client", () => {
  it("sends bearer token and Accept-Language", async () => {
    const adapter = vi.fn<AxiosAdapter>(async (config) => respond(config, 200));
    const client = createHttpClient({ refresh: vi.fn(), onSessionExpired: vi.fn(), adapter });
    client.setAccessToken("token-1");
    client.setLocale("pt");

    await client.http.get("/api/admin/leads");

    const sent = adapter.mock.calls[0][0];
    expect(sent.headers.Authorization).toBe("Bearer token-1");
    expect(sent.headers["Accept-Language"]).toBe("pt");
  });

  it("refreshes once for concurrent 401 responses and retries all of them", async () => {
    let currentValid = "new-token";
    const adapter = vi.fn<AxiosAdapter>(async (config) =>
      config.headers.Authorization === `Bearer ${currentValid}` ? respond(config, 200, { ok: true }) : rejectWith(config, 401),
    );
    const refresh = vi.fn(async () => {
      await new Promise((resolve) => setTimeout(resolve, 10));
      return currentValid;
    });
    const client = createHttpClient({ refresh, onSessionExpired: vi.fn(), adapter });
    client.setAccessToken("expired");

    const results = await Promise.all([client.http.get("/a"), client.http.get("/b"), client.http.get("/c")]);

    expect(refresh).toHaveBeenCalledTimes(1);
    expect(results.map((r) => r.status)).toEqual([200, 200, 200]);
    expect(client.getAccessToken()).toBe("new-token");
    currentValid = "unused";
  });

  it("ends the session when refresh fails", async () => {
    const adapter = vi.fn<AxiosAdapter>(async (config) => rejectWith(config, 401));
    const onSessionExpired = vi.fn();
    const client = createHttpClient({ refresh: vi.fn(async () => Promise.reject(new Error("401"))), onSessionExpired, adapter });
    client.setAccessToken("expired");

    await expect(client.http.get("/a")).rejects.toBeTruthy();

    expect(onSessionExpired).toHaveBeenCalledTimes(1);
    expect(client.getAccessToken()).toBeNull();
  });

  it("does not try to refresh on auth endpoints", async () => {
    const adapter = vi.fn<AxiosAdapter>(async (config) => rejectWith(config, 401));
    const refresh = vi.fn();
    const client = createHttpClient({ refresh, onSessionExpired: vi.fn(), adapter });

    await expect(client.http.post("/api/auth/login", {})).rejects.toBeTruthy();

    expect(refresh).not.toHaveBeenCalled();
  });

  it("never stores tokens in web storage", async () => {
    const adapter = vi.fn<AxiosAdapter>(async (config) => respond(config, 200));
    const client = createHttpClient({ refresh: vi.fn(), onSessionExpired: vi.fn(), adapter });

    client.setAccessToken("secret-token");
    await client.http.get("/a");

    expect(JSON.stringify({ ...localStorage })).not.toContain("secret-token");
    expect(JSON.stringify({ ...sessionStorage })).not.toContain("secret-token");
  });
});
