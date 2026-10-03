// O layout raiz real fica em [locale]/layout.tsx (define <html lang>); este só repassa.
export default function RootLayout({ children }: { children: React.ReactNode }) {
  return children;
}
