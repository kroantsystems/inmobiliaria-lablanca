import { ImageResponse } from "next/og";
import { hasLocale } from "next-intl";
import { getTranslations } from "next-intl/server";
import { routing } from "@/i18n/routing";
import { getProperty } from "@/lib/api/server";
import { formatNumber } from "@/lib/format/format";
import { coverDataUrl, logoDataUrl, OG_HEADERS, OG_SIZE } from "@/lib/og";
import { placeOf } from "@/lib/property";

/** Imagem 1200×630 do anúncio (capa, título, preço e logo) para WhatsApp, Facebook etc. */
export async function GET(_request: Request, { params }: { params: Promise<{ locale: string; slug: string }> }) {
  const { locale, slug } = await params;
  if (!hasLocale(routing.locales, locale)) return new Response("Not found", { status: 404 });
  const property = await getProperty(locale, slug);
  if (!property) return new Response("Not found", { status: 404 });

  const [logo, cover, t, tc] = await Promise.all([
    logoDataUrl(),
    coverDataUrl(property.gallery.find((item) => item.kind === "Image")),
    getTranslations({ locale, namespace: "property" }),
    getTranslations({ locale, namespace: "currency" }),
  ]);
  // Código da moeda em vez do símbolo: a fonte padrão do gerador não tem "₲".
  const price = `${property.currency} ${formatNumber(property.price, locale)}${property.operation === "Rent" ? ` ${tc("perMonth")}` : ""}`;

  return new ImageResponse(
    (
      <div style={{ display: "flex", position: "relative", width: "100%", height: "100%", background: "#0F172A", color: "#FFFFFF" }}>
        {cover ? (
          // eslint-disable-next-line @next/next/no-img-element -- ImageResponse só aceita <img>
          <img src={cover} width={1200} height={630} alt="" style={{ position: "absolute", inset: 0, objectFit: "cover" }} />
        ) : null}
        <div
          style={{
            display: "flex",
            flexDirection: "column",
            justifyContent: "space-between",
            width: "100%",
            height: "100%",
            padding: 48,
            background: "linear-gradient(180deg, rgba(15,23,42,0.15) 0%, rgba(15,23,42,0.35) 45%, rgba(15,23,42,0.92) 100%)",
          }}
        >
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "flex-start" }}>
            <div style={{ display: "flex", background: "rgba(15,23,42,0.85)", borderRadius: 20, padding: "14px 18px" }}>
              {/* eslint-disable-next-line @next/next/no-img-element -- ImageResponse só aceita <img> */}
              <img src={logo} width={244} height={139} alt="" />
            </div>
            <div style={{ display: "flex", background: "#005DAA", borderRadius: 999, padding: "10px 24px", fontSize: 28, fontWeight: 700 }}>
              {t(`operation.${property.operation}`)}
            </div>
          </div>
          <div style={{ display: "flex", flexDirection: "column" }}>
            <div style={{ display: "flex", fontSize: 56, fontWeight: 700, lineHeight: 1.1, maxWidth: 1050 }}>{property.title}</div>
            <div style={{ display: "flex", alignItems: "center", marginTop: 20, gap: 24 }}>
              <div style={{ display: "flex", background: "#E31B23", borderRadius: 16, padding: "8px 20px", fontSize: 40, fontWeight: 700 }}>{price}</div>
              <div style={{ display: "flex", fontSize: 30, color: "#E2E8F0" }}>{placeOf(property)}</div>
            </div>
          </div>
        </div>
      </div>
    ),
    { ...OG_SIZE, headers: OG_HEADERS },
  );
}
