import axios, { type AxiosAdapter, type AxiosError, type InternalAxiosRequestConfig } from "axios";

type HttpClientOptions = {
  /** Renova a sessão pelo cookie HttpOnly e devolve o novo access token. */
  refresh: () => Promise<string>;
  onSessionExpired: () => void;
  adapter?: AxiosAdapter;
};

type RetriableConfig = InternalAxiosRequestConfig & { _retried?: boolean };

const AUTH_ENDPOINTS = ["/api/auth/login", "/api/auth/refresh", "/api/auth/logout"];

/**
 * Cliente do navegador: o access token fica só em memória (nunca em localStorage/sessionStorage).
 * Em 401, várias requisições simultâneas compartilham uma única renovação e depois são repetidas.
 */
export function createHttpClient({ refresh, onSessionExpired, adapter }: HttpClientOptions) {
  let accessToken: string | null = null;
  let locale = "es";
  let refreshing: Promise<string> | null = null;
  let expiredRefresh: Promise<string> | null = null;

  const http = axios.create({ withCredentials: true, adapter });

  http.interceptors.request.use((config) => {
    if (accessToken) config.headers.Authorization = `Bearer ${accessToken}`;
    config.headers["Accept-Language"] = locale;
    return config;
  });

  http.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const config = error.config as RetriableConfig | undefined;
      const isAuthCall = AUTH_ENDPOINTS.some((path) => config?.url?.startsWith(path));
      if (error.response?.status !== 401 || !config || config._retried || isAuthCall) throw error;

      config._retried = true;
      const attempt = (refreshing ??= refresh().finally(() => {
        refreshing = null;
      }));

      try {
        accessToken = await attempt;
      } catch {
        accessToken = null;
        if (expiredRefresh !== attempt) {
          expiredRefresh = attempt;
          onSessionExpired();
        }
        throw error;
      }

      config.headers.Authorization = `Bearer ${accessToken}`;
      return http(config);
    },
  );

  return {
    http,
    setAccessToken: (token: string | null) => {
      accessToken = token;
    },
    getAccessToken: () => accessToken,
    setLocale: (value: string) => {
      locale = value;
    },
  };
}
