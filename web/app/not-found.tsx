import Link from "next/link";
import { Block } from "@/components/Block";

export default function NotFound() {
  return (
    <Block id="nao-encontrada">
      <p className="route">
        <span className="font-medium text-signal">404</span> Not Found
      </p>
      <h1 className="display-2 mt-[clamp(1.5rem,4vw,3rem)]">Página não encontrada</h1>
      <p className="mt-6">
        <Link href="/" className="link">
          Voltar ao início
        </Link>
      </p>
    </Block>
  );
}
