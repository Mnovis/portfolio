import type { Metadata } from "next";
import { AdminPanel } from "@/components/AdminPanel";
import { Block } from "@/components/Block";

// Fora dos buscadores: a página é só para o dono do site.
export const metadata: Metadata = { title: "Painel", robots: { index: false, follow: false } };

// A página em si é pública e estática; o que protege os dados é a API, que só responde
// às rotas api/admin com um token válido.
export default function AdminPage() {
  return (
    <Block id="painel">
      <p className="route">
        <span className="font-medium text-signal">POST</span> /api/admin/login
      </p>
      <AdminPanel />
    </Block>
  );
}
