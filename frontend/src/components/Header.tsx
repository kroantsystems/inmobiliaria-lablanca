import Link from "next/link";

const navItems = [
  { href: "/", label: "Início" },
  { href: "/imoveis", label: "Imóveis" },
  { href: "/sobre", label: "Sobre" },
  { href: "/contato", label: "Contato" },
];

export function Header() {
  return (
    <header className="border-b border-stone-200 bg-white/90 backdrop-blur">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-4 py-4 sm:px-6">
        <Link href="/" className="font-serif text-2xl tracking-wide text-stone-900">
          La Blanca
          <span className="ml-2 align-middle text-xs font-sans uppercase tracking-[0.2em] text-amber-700">
            Imóveis
          </span>
        </Link>
        <nav aria-label="Navegação principal">
          <ul className="flex flex-wrap gap-x-6 gap-y-2 text-sm text-stone-600">
            {navItems.map((item) => (
              <li key={item.href}>
                <Link href={item.href} className="transition-colors hover:text-stone-900">
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      </div>
    </header>
  );
}
