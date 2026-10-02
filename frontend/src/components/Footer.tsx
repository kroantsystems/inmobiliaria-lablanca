export function Footer() {
  const year = new Date().getFullYear();

  return (
    <footer className="mt-auto border-t border-stone-200 bg-stone-50">
      <div className="mx-auto grid max-w-6xl gap-6 px-4 py-10 text-sm text-stone-600 sm:grid-cols-3 sm:px-6">
        <div>
          <p className="font-serif text-lg text-stone-900">La Blanca Imóveis</p>
          <p className="mt-2">Compra, venda e locação de imóveis.</p>
        </div>
        <div>
          <p className="font-medium text-stone-900">Contato</p>
          <p className="mt-2">(00) 0000-0000</p>
          <p>contato@lablanca.com.br</p>
        </div>
        <div>
          <p className="font-medium text-stone-900">Endereço</p>
          <p className="mt-2">Rua Exemplo, 123 — Centro</p>
          <p>Cidade/UF</p>
        </div>
      </div>
      <p className="border-t border-stone-200 py-4 text-center text-xs text-stone-500">
        © {year} La Blanca Imóveis. Todos os direitos reservados.
      </p>
    </footer>
  );
}
