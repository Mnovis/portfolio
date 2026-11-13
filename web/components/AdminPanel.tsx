"use client";

import { useEffect, useState, useSyncExternalStore, type FormEvent } from "react";
import { inputClass } from "@/components/ContactForm";
import { API_URL } from "@/lib/useApi";

// Os mesmos formatos que a API devolve (AdminContracts.cs).
type Message = {
  id: number;
  name: string;
  email: string;
  message: string;
  createdAt: string;
  notifiedAt: string | null;
  readAt: string | null;
};

type Overview = {
  messages: number;
  unread: number;
  notNotified: number;
  projects: number;
  technologies: number;
  educationItems: number;
};

// O token fica no sessionStorage: some ao fechar a aba. Não dá para usar cookie HttpOnly
// porque o site e a API estão em domínios diferentes, e o navegador bloqueia cookie de
// terceiros. O preço é que um script injetado na página conseguiria ler o token; por isso
// ele vale só 2 horas e a página não carrega script de fora.
const TOKEN_KEY = "painel-token";
const listeners = new Set<() => void>();
let token: string | null | undefined;

function readToken() {
  if (token === undefined) {
    try {
      token = sessionStorage.getItem(TOKEN_KEY);
    } catch {
      token = null;
    }
  }
  return token;
}

function setToken(value: string | null) {
  token = value;
  try {
    if (value) sessionStorage.setItem(TOKEN_KEY, value);
    else sessionStorage.removeItem(TOKEN_KEY);
  } catch {
    // Sem sessionStorage o token fica só na memória, até recarregar a página.
  }
  listeners.forEach((listener) => listener());
}

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

function request(path: string, bearer: string | null, init?: RequestInit) {
  return fetch(API_URL + path, {
    ...init,
    headers: { "Content-Type": "application/json", ...(bearer && { Authorization: `Bearer ${bearer}` }) },
    // No plano gratuito a API dorme; acordar pode levar perto de um minuto.
    signal: AbortSignal.timeout(70_000),
  });
}

const actionClass = "link py-2 font-mono text-[0.8125rem] disabled:opacity-50";
const dateFormat = new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" });

export function AdminPanel() {
  const current = useSyncExternalStore(subscribe, readToken, () => null);

  if (!API_URL) return <p className="mt-8">A API ainda não está configurada neste site.</p>;
  return current ? <Dashboard token={current} /> : <Login />;
}

function Login() {
  const [sending, setSending] = useState(false);
  const [error, setError] = useState<string>();

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const body = JSON.stringify(Object.fromEntries(new FormData(event.currentTarget)));

    setSending(true);
    setError(undefined);

    try {
      const response = await request("/api/admin/login", null, { method: "POST", body });

      if (response.ok) {
        setToken(((await response.json()) as { token: string }).token);
        return;
      }

      const messages: Record<number, string> = {
        400: "Preencha e-mail e senha.",
        401: "E-mail ou senha incorretos.",
        429: "Muitas tentativas. Espere alguns minutos.",
        503: "O painel ainda não foi configurado na API.",
      };
      setError(messages[response.status] ?? "Não consegui entrar agora. Tente de novo.");
    } catch {
      setError("A API não respondeu. Tente de novo.");
    }

    setSending(false);
  }

  return (
    <>
      <h1 className="display-2 mt-[clamp(1.5rem,4vw,3rem)]">Painel</h1>

      <form method="post" onSubmit={handleSubmit} className="mt-8 grid max-w-[24em] gap-5">
        <div>
          <label htmlFor="painel-email" className="label">
            E-mail
          </label>
          <input id="painel-email" name="email" type="email" autoComplete="username" required className={inputClass} />
        </div>

        <div>
          <label htmlFor="painel-password" className="label">
            Senha
          </label>
          <input
            id="painel-password"
            name="password"
            type="password"
            autoComplete="current-password"
            required
            className={inputClass}
          />
        </div>

        <div className="flex flex-wrap items-center gap-x-5 gap-y-3">
          <button type="submit" className="button" disabled={sending}>
            {sending ? "Entrando…" : "Entrar"}
          </button>
          {sending && (
            <p role="status" className="text-[0.875rem] text-muted">
              Se a API estiver dormindo, pode levar até um minuto.
            </p>
          )}
          {error && (
            <p role="alert" className="text-signal">
              {error}
            </p>
          )}
        </div>
      </form>
    </>
  );
}

type Panel = { overview: Overview; messages: Message[] };

/** Busca as contagens e as mensagens. Devolve null quando o token venceu ou não vale mais. */
async function fetchPanel(token: string): Promise<Panel | null> {
  const [overview, messages] = await Promise.all([
    request("/api/admin/overview", token),
    request("/api/admin/messages", token),
  ]);

  if (overview.status === 401 || messages.status === 401) return null;
  if (!overview.ok || !messages.ok) throw new Error("resposta inesperada");

  return { overview: await overview.json(), messages: await messages.json() };
}

function Dashboard({ token }: { token: string }) {
  const [data, setData] = useState<Panel>();
  const [error, setError] = useState<string>();
  const [busy, setBusy] = useState(false);

  // Primeira carga, ao entrar. Sem token válido, volta para o login.
  useEffect(() => {
    fetchPanel(token).then(
      (panel) => (panel ? setData(panel) : setToken(null)),
      () => setError("A API não respondeu. Tente atualizar."),
    );
  }, [token]);

  /** Roda uma ação (marcar, excluir ou só atualizar) e recarrega a lista e as contagens. */
  async function run(action?: { path: string; init: RequestInit }) {
    setBusy(true);
    try {
      if (action) {
        const response = await request(action.path, token, action.init);
        if (response.status === 401) return setToken(null);
        // 404 = a mensagem já não existe (excluída em outra aba); recarregar resolve.
        if (!response.ok && response.status !== 404) throw new Error("resposta inesperada");
      }

      const panel = await fetchPanel(token);
      if (!panel) return setToken(null);
      setData(panel);
      setError(undefined);
    } catch {
      setError("Não consegui concluir a ação. Tente de novo.");
    } finally {
      setBusy(false);
    }
  }

  const facts: [string, number | undefined][] = [
    ["Mensagens", data?.overview.messages],
    ["Não lidas", data?.overview.unread],
    ["Sem aviso por e-mail", data?.overview.notNotified],
    ["Projetos", data?.overview.projects],
    ["Tecnologias", data?.overview.technologies],
    ["Formação", data?.overview.educationItems],
  ];

  return (
    <>
      <div className="mt-[clamp(1.5rem,4vw,3rem)] flex flex-wrap items-baseline justify-between gap-x-8 gap-y-2">
        <h1 className="display-2">Painel</h1>
        <p className="flex gap-x-6">
          <button type="button" className={actionClass} disabled={busy} onClick={() => run()}>
            Atualizar
          </button>
          <button type="button" className={actionClass} onClick={() => setToken(null)}>
            Sair
          </button>
        </p>
      </div>

      {error && (
        <p role="alert" className="mt-6 text-signal">
          {error}
        </p>
      )}

      <dl className="mt-8 grid grid-cols-2 gap-x-8 sm:grid-cols-3 lg:grid-cols-6">
        {facts.map(([term, value]) => (
          <div key={term} className="flex flex-col-reverse border-t border-rule py-3">
            <dt className="label text-muted">{term}</dt>
            <dd className="display-3">{value ?? "–"}</dd>
          </div>
        ))}
      </dl>

      <h2 className="display-3 mt-[clamp(2rem,5vw,3.5rem)]">Mensagens</h2>

      {!data && !error && (
        <p role="status" className="mt-4 text-muted">
          Carregando… se a API estiver dormindo, pode levar até um minuto.
        </p>
      )}
      {data?.messages.length === 0 && <p className="mt-4 text-muted">Nenhuma mensagem ainda.</p>}

      <ul className="mt-4">
        {data?.messages.map((message) => (
          <li key={message.id} className="grid gap-x-16 gap-y-2 border-t border-rule py-5 lg:grid-cols-[1fr_2.2fr]">
            <div>
              <p className={message.readAt ? "" : "font-semibold"}>
                {message.name}
                {!message.readAt && <span className="label ml-3 text-signal">Nova</span>}
              </p>
              <p>
                <a href={`mailto:${message.email}`} className="link inline-block py-0.5 break-all">
                  {message.email}
                </a>
              </p>
              <p className="mt-1 font-mono text-[0.8125rem] text-muted">
                <time dateTime={message.createdAt}>{dateFormat.format(new Date(message.createdAt))}</time>
                {!message.notifiedAt && " · aviso por e-mail não saiu"}
              </p>
            </div>

            <div>
              <p className="max-w-[44em] break-words whitespace-pre-wrap">{message.message}</p>
              <p className="mt-2 flex flex-wrap gap-x-6">
                <button
                  type="button"
                  className={actionClass}
                  disabled={busy}
                  onClick={() =>
                    run({
                      path: `/api/admin/messages/${message.id}`,
                      init: { method: "PATCH", body: JSON.stringify({ read: !message.readAt }) },
                    })
                  }
                >
                  {message.readAt ? "Marcar como não lida" : "Marcar como lida"}
                  <span className="sr-only"> a mensagem de {message.name}</span>
                </button>
                <button
                  type="button"
                  className={actionClass}
                  disabled={busy}
                  onClick={() => {
                    // Excluir apaga do banco e não tem volta: pede confirmação.
                    if (window.confirm(`Excluir a mensagem de ${message.name}? Não dá para desfazer.`))
                      void run({ path: `/api/admin/messages/${message.id}`, init: { method: "DELETE" } });
                  }}
                >
                  Excluir<span className="sr-only"> a mensagem de {message.name}</span>
                </button>
              </p>
            </div>
          </li>
        ))}
      </ul>
    </>
  );
}
