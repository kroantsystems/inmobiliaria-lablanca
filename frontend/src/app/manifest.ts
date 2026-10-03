import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "Inmobiliaria La Blanca",
    short_name: "La Blanca",
    description: "Casas, departamentos y terrenos en Ciudad del Este y Alto Paraná.",
    start_url: "/es",
    display: "standalone",
    background_color: "#F8FAFC",
    theme_color: "#005DAA",
    icons: [
      { src: "/icons/icon-192.png", sizes: "192x192", type: "image/png" },
      { src: "/icons/icon-512.png", sizes: "512x512", type: "image/png" },
      { src: "/icons/icon-maskable-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
    ],
  };
}
