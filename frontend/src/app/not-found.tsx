import Link from "next/link";

// 404 fora de qualquer idioma (ex.: arquivo inexistente). O layout raiz só repassa, então este define <html>.
export default function RootNotFound() {
  return (
    <html lang="es">
      <body style={{ fontFamily: "system-ui, sans-serif", textAlign: "center", padding: "4rem 1rem", color: "#0f172a" }}>
        <h1>404</h1>
        <p>Página no encontrada · Página não encontrada · Page not found</p>
        <p>
          <Link href="/es" style={{ color: "#005daa", fontWeight: 700 }}>
            Inmobiliaria La Blanca
          </Link>
        </p>
      </body>
    </html>
  );
}
