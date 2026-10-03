import Image from "next/image";

const LOGO_WIDTH = 610;
const LOGO_HEIGHT = 348;

type LogoProps = {
  /** "light" para fundos claros; "dark" deixa "CIUDAD DEL ESTE" em branco para fundos escuros. */
  variant?: "light" | "dark";
  alt: string;
  width?: number;
  priority?: boolean;
  className?: string;
};

export function Logo({ variant = "light", alt, width = 150, priority = false, className }: LogoProps) {
  return (
    <Image
      src={`/brand/logo-${variant}.webp`}
      alt={alt}
      width={width}
      height={Math.round((width * LOGO_HEIGHT) / LOGO_WIDTH)}
      priority={priority}
      sizes={`${width}px`}
      className={className}
    />
  );
}

/** Ícone oficial (prédio + casa) sem fundo, para superfícies escuras. */
export function BrandMark({ className, title }: { className?: string; title?: string }) {
  return (
    <svg viewBox="0 0 54 58" className={className} role={title ? "img" : undefined} aria-hidden={title ? undefined : true}>
      {title ? <title>{title}</title> : null}
      <defs>
        <mask id="lb-brand-mark-cut">
          <rect x="1" y="1" width="35" height="48" rx="4" fill="#fff" />
          <g fill="#000">
            <rect x="8" y="12" width="5" height="5" />
            <rect x="16" y="12" width="5" height="5" />
            <rect x="24" y="12" width="5" height="5" />
            <rect x="8" y="23" width="5" height="5" />
            <rect x="16" y="23" width="5" height="5" />
            <rect x="24" y="23" width="5" height="5" />
            <path d="M15.5 49V38.5a3 3 0 0 1 6 0V49z" />
          </g>
        </mask>
      </defs>
      <rect x="1" y="1" width="35" height="48" rx="4" fill="#FFFFFF" mask="url(#lb-brand-mark-cut)" />
      <g fill="#E31B23">
        <path d="M35 29.5 52.5 45H48v12h-9v-6.5h-7.5V57h-9V45h-5z" />
        <rect x="42" y="31" width="5" height="9" />
      </g>
    </svg>
  );
}
