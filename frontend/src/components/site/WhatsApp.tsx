"use client";

import { useLocale, useTranslations } from "next-intl";
import { WhatsAppIcon } from "@/components/ui/Icons";
import { track } from "@/lib/analytics";

export function whatsappUrl(number: string, message: string) {
  return `https://wa.me/${number.replace(/\D/g, "")}?text=${encodeURIComponent(message)}`;
}

type WhatsAppLinkProps = {
  number: string | null;
  message?: string;
  propertyId?: string;
  className?: string;
  children: React.ReactNode;
};

export function WhatsAppLink({ number, message, propertyId, className, children }: WhatsAppLinkProps) {
  const t = useTranslations("whatsapp");
  const locale = useLocale();
  if (!number) return null;

  return (
    <a
      href={whatsappUrl(number, message ?? t("defaultMessage"))}
      target="_blank"
      rel="noopener noreferrer"
      className={className}
      onClick={() => track("WhatsAppClick", { locale, propertyId })}
    >
      {children}
    </a>
  );
}

export function WhatsAppFloat({ number }: { number: string | null }) {
  const t = useTranslations("whatsapp");
  return (
    <WhatsAppLink
      number={number}
      className="fixed right-5 bottom-5 z-40 flex size-14 items-center justify-center rounded-full bg-whatsapp text-white shadow-lg shadow-whatsapp/40 transition hover:scale-105"
    >
      <WhatsAppIcon className="size-8" />
      <span className="sr-only">{t("label")}</span>
    </WhatsAppLink>
  );
}
