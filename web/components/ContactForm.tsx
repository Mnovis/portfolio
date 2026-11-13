"use client";

import { useState, type FormEvent } from "react";
import { API_URL } from "@/lib/useApi";

type Field = "name" | "email" | "message";

type Status =
  | { kind: "idle" }
  | { kind: "sending" }
  | { kind: "sent" }
  | { kind: "error"; message: string };

const inputClass =
  "mt-1.5 block w-full border border-ink bg-transparent px-3 py-2.5 aria-[invalid=true]:border-signal";

/**
 * Formulário de contato. Envia para POST /api/contact, que valida de novo no servidor,
 * grava a mensagem e avisa por e-mail. Os limites daqui são os mesmos de ContactRequest.
 */
export function ContactForm() {
  const [status, setStatus] = useState<Status>({ kind: "idle" });
  const [errors, setErrors] = useState<Partial<Record<Field, string>>>({});

  // Sem endereço da API (site publicado antes dela), sobram os links diretos.
  if (!API_URL) return null;

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const form = event.currentTarget;

    setStatus({ kind: "sending" });
    setErrors({});

    try {
      const response = await fetch(`${API_URL}/api/contact`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(Object.fromEntries(new FormData(form))),
        signal: AbortSignal.timeout(70_000),
      });

      if (response.ok) {
        form.reset();
        setStatus({ kind: "sent" });
        return;
      }

      if (response.status === 400) {
        // A API devolve um erro por campo, com o mesmo nome do campo do formulário.
        const problem = (await response.json()) as { errors?: Record<string, string[]> };
        setErrors(
          Object.fromEntries(Object.entries(problem.errors ?? {}).map(([field, messages]) => [field, messages[0]])),
        );
        setStatus({ kind: "error", message: "Confira os campos marcados." });
        return;
      }

      setStatus({
        kind: "error",
        message:
          response.status === 429
            ? "Muitas mensagens em pouco tempo. Tente de novo em alguns minutos."
            : "Não consegui enviar agora. Tente de novo ou fale comigo pelo LinkedIn.",
      });
    } catch {
      setStatus({ kind: "error", message: "A API não respondeu. Tente de novo ou fale comigo pelo LinkedIn." });
    }
  }

  const errorId = (field: Field) => `contato-${field}-erro`;
  const fieldProps = (field: Field) => ({
    id: `contato-${field}`,
    name: field,
    required: true,
    className: inputClass,
    "aria-invalid": errors[field] ? true : undefined,
    "aria-describedby": errors[field] ? errorId(field) : undefined,
  });

  const fieldError = (field: Field) =>
    errors[field] && (
      <p id={errorId(field)} className="mt-1 text-[0.875rem] text-signal">
        {errors[field]}
      </p>
    );

  return (
    // method="post": se alguém enviar antes de o JavaScript carregar, o navegador não
    // coloca nome, e-mail e mensagem no endereço da página (o padrão de formulário é GET).
    <form method="post" onSubmit={handleSubmit} className="relative grid max-w-[36em] gap-5">
      <div>
        <label htmlFor="contato-name" className="label">
          Nome
        </label>
        <input type="text" autoComplete="name" minLength={2} maxLength={100} {...fieldProps("name")} />
        {fieldError("name")}
      </div>

      <div>
        <label htmlFor="contato-email" className="label">
          E-mail
        </label>
        <input type="email" autoComplete="email" maxLength={254} {...fieldProps("email")} />
        {fieldError("email")}
      </div>

      <div>
        <label htmlFor="contato-message" className="label">
          Mensagem
        </label>
        <textarea rows={5} minLength={10} maxLength={4000} {...fieldProps("message")} />
        {fieldError("message")}
      </div>

      {/* Campo-isca: fora da tela e fora da navegação. Gente não vê; robô preenche, e a API descarta. */}
      <div aria-hidden="true" className="absolute -left-[9999px]">
        <label>
          Não preencha este campo
          <input type="text" name="website" tabIndex={-1} autoComplete="off" />
        </label>
      </div>

      <div className="flex flex-wrap items-center gap-x-5 gap-y-3">
        <button type="submit" className="button" disabled={status.kind === "sending"}>
          {status.kind === "sending" ? "Enviando…" : "Enviar mensagem"}
        </button>

        {status.kind === "sending" && (
          <p role="status" className="text-[0.875rem] text-muted">
            Se a API estiver dormindo, pode levar até um minuto.
          </p>
        )}
        {status.kind === "sent" && <p role="status">Mensagem enviada. Obrigado!</p>}
        {status.kind === "error" && (
          <p role="alert" className="text-signal">
            {status.message}
          </p>
        )}
      </div>
    </form>
  );
}
