"use client";

import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { useTranslations } from "next-intl";
import { MapContainer, Marker, Popup, TileLayer } from "react-leaflet";
import { Link } from "@/i18n/navigation";
import type { PropertyCard } from "@/lib/api/types";
import { Price } from "./CurrencyProvider";

const TILES = process.env.NEXT_PUBLIC_MAP_TILES_URL ?? "https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png";
const ATTRIBUTION = process.env.NEXT_PUBLIC_MAP_ATTRIBUTION ?? "&copy; OpenStreetMap contributors";
const CIUDAD_DEL_ESTE: [number, number] = [-25.51, -54.64];

// Ícone próprio (casa vermelha) em vez das imagens padrão do Leaflet, que quebram com bundlers.
const pin = L.divIcon({
  className: "",
  html: '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 32 40" width="32" height="40"><path d="M16 0C7.2 0 0 7 0 15.6 0 27.3 16 40 16 40s16-12.7 16-24.4C32 7 24.8 0 16 0z" fill="#E31B23"/><path d="M16 8 8 15h2.5v8h11v-8H24z" fill="#fff"/></svg>',
  iconSize: [32, 40],
  iconAnchor: [16, 40],
  popupAnchor: [0, -36],
});

export default function PropertiesMap({ properties, zoom = 12, single = false }: { properties: PropertyCard[]; zoom?: number; single?: boolean }) {
  const t = useTranslations("property");
  const located = properties.filter((p) => p.latitude !== null && p.longitude !== null);
  const center: [number, number] = located[0] ? [located[0].latitude!, located[0].longitude!] : CIUDAD_DEL_ESTE;

  return (
    <MapContainer center={center} zoom={single ? 15 : zoom} scrollWheelZoom={false} className="h-full w-full">
      <TileLayer url={TILES} attribution={ATTRIBUTION} />
      {located.map((property) => (
        <Marker key={property.id} position={[property.latitude!, property.longitude!]} icon={pin}>
          {single ? null : (
            <Popup>
              <div className="w-48 font-sans">
                {property.cover ? (
                  // eslint-disable-next-line @next/next/no-img-element -- popup do Leaflet, fora do fluxo de layout
                  <img src={property.cover.url} alt={property.cover.alt} className="mb-2 h-24 w-full rounded object-cover" loading="lazy" />
                ) : null}
                <p className="font-bold text-lb-ink">{property.title}</p>
                <Price amount={property.price} currency={property.currency} perMonth={property.operation === "Rent"} className="block font-extrabold text-lb-blue" />
                <Link href={{ pathname: "/properties/[slug]", params: { slug: property.slug } }} className="mt-1 inline-block text-sm font-semibold text-lb-red">
                  {t("details")}
                </Link>
              </div>
            </Popup>
          )}
        </Marker>
      ))}
    </MapContainer>
  );
}
