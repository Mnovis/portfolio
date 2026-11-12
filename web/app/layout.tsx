import type { Metadata } from "next";
import { IBM_Plex_Mono, IBM_Plex_Sans, Instrument_Serif } from "next/font/google";
import Link from "next/link";
import { profile } from "@/lib/content";
import "./globals.css";

// next/font baixa as fontes no build e serve do próprio site: sem requisição externa.
const display = Instrument_Serif({
  weight: "400",
  style: ["normal", "italic"],
  subsets: ["latin"],
  variable: "--font-instrument-serif",
});

const sans = IBM_Plex_Sans({
  weight: ["400", "500", "600"],
  subsets: ["latin"],
  variable: "--font-plex-sans",
});

const mono = IBM_Plex_Mono({
  weight: ["400", "500"],
  subsets: ["latin"],
  variable: "--font-plex-mono",
});

export const metadata: Metadata = {
  title: {
    default: `${profile.shortName} — ${profile.headline}`,
    template: `%s — ${profile.shortName}`,
  },
  description: profile.summary,
};

const sections = [
  { href: "/#projetos", label: "Projetos" },
  { href: "/#stack", label: "Stack" },
  { href: "/#formacao", label: "Formação" },
  { href: "/#contato", label: "Contato" },
];

export default function RootLayout({ children }: LayoutProps<"/">) {
  return (
    <html lang="pt-BR" className={`${display.variable} ${sans.variable} ${mono.variable}`}>
      <body>
        <a href="#conteudo" className="skip-link">
          Pular para o conteúdo
        </a>

        <header className="flex flex-wrap items-baseline justify-between gap-x-8 px-gutter py-2 font-mono text-[0.8125rem]">
          <Link href="/" className="py-2 hover:underline">
            {profile.shortName}
          </Link>
          <nav aria-label="Seções">
            <ul className="flex flex-wrap gap-x-6">
              {sections.map((section) => (
                <li key={section.href}>
                  <Link href={section.href} className="block py-2 hover:underline">
                    {section.label}
                  </Link>
                </li>
              ))}
            </ul>
          </nav>
        </header>

        <main id="conteudo">{children}</main>

        <footer className="border-t border-rule px-gutter py-6 font-mono text-[0.8125rem] text-muted">
          <p>API em ASP.NET Core e PostgreSQL · site em Next.js</p>
        </footer>
      </body>
    </html>
  );
}
