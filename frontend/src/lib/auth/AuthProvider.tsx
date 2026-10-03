"use client";

import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { session } from "@/lib/api/client";
import type { AuthUser } from "@/lib/api/types";

type AuthStatus = "loading" | "authenticated" | "anonymous";

type AuthContextValue = {
  status: AuthStatus;
  user: AuthUser | null;
  logout: () => Promise<void>;
  can: (permission: string) => boolean;
};

const AuthContext = createContext<AuthContextValue | null>(null);

/** Sessão do painel: tenta renovar uma vez pelo cookie ao abrir/recarregar a página. */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(session.user);
  const [status, setStatus] = useState<AuthStatus>(session.user ? "authenticated" : "loading");

  useEffect(() => {
    const unsubscribe = session.subscribe((next) => {
      setUser(next);
      setStatus(next ? "authenticated" : "anonymous");
    });
    let active = true;
    session.restore().then((restored) => {
      if (!active) return;
      setUser(restored);
      setStatus(restored ? "authenticated" : "anonymous");
    });
    return () => {
      active = false;
      unsubscribe();
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      status,
      user,
      logout: () => session.logout(),
      can: (permission) => user?.permissions.includes(permission) ?? false,
    }),
    [status, user],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error("useAuth must be used inside AuthProvider");
  return context;
}
