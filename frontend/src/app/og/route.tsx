import { ImageResponse } from "next/og";
import { logoDataUrl, OG_HEADERS, OG_SIZE } from "@/lib/og";

/** Imagem de compartilhamento padrão das páginas sem foto própria. */
export async function GET() {
  const logo = await logoDataUrl();
  return new ImageResponse(
    (
      <div
        style={{
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
          justifyContent: "center",
          width: "100%",
          height: "100%",
          background: "linear-gradient(135deg, #1E293B 0%, #0F172A 100%)",
          borderBottom: "14px solid #005DAA",
          color: "#FFFFFF",
        }}
      >
        {/* eslint-disable-next-line @next/next/no-img-element -- ImageResponse só aceita <img> */}
        <img src={logo} width={610} height={348} alt="" />
        <div style={{ display: "flex", marginTop: 28, fontSize: 38, color: "#CBD5E1" }}>Ciudad del Este · Hernandarias · Alto Paraná</div>
      </div>
    ),
    { ...OG_SIZE, headers: OG_HEADERS },
  );
}
