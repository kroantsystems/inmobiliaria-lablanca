import type { Metadata } from "next";
import { setRequestLocale } from "next-intl/server";
import { AdminProviders } from "@/components/admin/AdminProviders";
import { AdminShell } from "@/components/admin/AdminShell";

// Painel fora dos buscadores (também bloqueado no robots.txt).
export const metadata: Metadata = {
  title: { absolute: "Panel · Inmobiliaria La Blanca" },
  robots: { index: false, follow: false, nocache: true },
};

export default async function AdminLayout({ children, params }: LayoutProps<"/[locale]/admin">) {
  const { locale } = await params;
  setRequestLocale(locale);
  return (
    <AdminProviders>
      <AdminShell>{children}</AdminShell>
    </AdminProviders>
  );
}
