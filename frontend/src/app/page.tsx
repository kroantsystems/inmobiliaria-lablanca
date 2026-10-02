import { Suspense } from "react";
import { ApiStatus, ApiStatusFallback } from "@/components/ApiStatus";

const highlights = [
  { title: "Comprar", description: "Casas, apartamentos e terrenos selecionados para você." },
  { title: "Alugar", description: "Opções de locação residencial e comercial." },
  { title: "Anunciar", description: "Avaliação e divulgação do seu imóvel com segurança." },
];

export default function Home() {
  return (
    <>
      <section className="bg-stone-900 text-stone-50">
        <div className="mx-auto max-w-6xl px-4 py-24 sm:px-6">
          <p className="text-xs uppercase tracking-[0.3em] text-amber-400">La Blanca Imóveis</p>
          <h1 className="mt-4 max-w-2xl font-serif text-4xl leading-tight sm:text-5xl">
            Encontre o imóvel certo para a sua próxima fase.
          </h1>
          <p className="mt-6 max-w-xl text-stone-300">
            Em breve: catálogo completo de imóveis com busca por bairro, faixa de preço e características.
          </p>
        </div>
      </section>

      <section className="mx-auto grid max-w-6xl gap-6 px-4 py-16 sm:grid-cols-3 sm:px-6">
        {highlights.map((item) => (
          <article key={item.title} className="rounded-lg border border-stone-200 p-6">
            <h2 className="font-serif text-xl text-stone-900">{item.title}</h2>
            <p className="mt-2 text-sm text-stone-600">{item.description}</p>
          </article>
        ))}
      </section>

      <div className="mx-auto w-full max-w-6xl px-4 pb-8 sm:px-6">
        <Suspense fallback={<ApiStatusFallback />}>
          <ApiStatus />
        </Suspense>
      </div>
    </>
  );
}
