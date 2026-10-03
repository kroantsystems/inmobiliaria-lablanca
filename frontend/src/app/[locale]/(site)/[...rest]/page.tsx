import { notFound } from "next/navigation";

// Qualquer caminho desconhecido dentro de um idioma mostra o 404 com o layout do site.
export default function CatchAllPage() {
  notFound();
}
