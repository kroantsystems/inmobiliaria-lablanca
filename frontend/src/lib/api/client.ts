"use client";

import axios, { isAxiosError } from "axios";
import { createHttpClient } from "./http";
import type { AuthResponse, AuthUser, ProblemDetails } from "./types";

type SessionListener = (user: AuthUser | null) => void;

let currentUser: AuthUser | null = null;
const listeners = new Set<SessionListener>();
let sessionExpiredHandler: () => void = () => {};

function setSession(response: AuthResponse | null) {
  apiClient.setAccessToken(response?.accessToken ?? null);
  currentUser = response?.user ?? null;
  listeners.forEach((listener) => listener(currentUser));
}

/** Renova pelo cookie HttpOnly (mesma origem via rewrite /api). */
async function refreshSession(): Promise<string> {
  const { data } = await axios.post<AuthResponse>("/api/auth/refresh", null, { withCredentials: true });
  setSession(data);
  return data.accessToken;
}

export const apiClient = createHttpClient({
  refresh: refreshSession,
  onSessionExpired: () => {
    setSession(null);
    sessionExpiredHandler();
  },
});

export const api = apiClient.http;

export const session = {
  get user() {
    return currentUser;
  },
  subscribe(listener: SessionListener) {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },
  onExpired(handler: () => void) {
    sessionExpiredHandler = handler;
  },
  async login(email: string, password: string) {
    const { data } = await api.post<AuthResponse>("/api/auth/login", { email, password });
    setSession(data);
    return data.user;
  },
  /** Recupera a sessão após recarregar a página (o token só existe em memória). */
  async restore(): Promise<AuthUser | null> {
    if (apiClient.getAccessToken() && currentUser) return currentUser;
    try {
      await refreshSession();
      return currentUser;
    } catch {
      setSession(null);
      return null;
    }
  },
  async logout() {
    try {
      await api.post("/api/auth/logout");
    } finally {
      setSession(null);
    }
  },
  replace(response: AuthResponse) {
    setSession(response);
  },
};

export function problemOf(error: unknown): ProblemDetails | null {
  return isAxiosError(error) && error.response?.data && typeof error.response.data === "object"
    ? (error.response.data as ProblemDetails)
    : null;
}
