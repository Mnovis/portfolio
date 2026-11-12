"use client";

import {
  useEffect,
  useRef,
  useState,
  type KeyboardEvent,
  type MouseEvent,
  type PointerEvent,
  type ReactNode,
} from "react";
import { useLens } from "@/lib/lens";
import { API_URL, useApi, type ApiState } from "@/lib/useApi";

/** O que fica por baixo de um bloco: o endpoint, a resposta e o código que a gera. */
export type Underlayer = {
  route: string;
  /** GET (padrão): o site chama a rota e mostra a resposta. POST: mostra um corpo de exemplo. */
  method?: "GET" | "POST";
  /**
   * GET: resposta montada no build a partir de content/, usada enquanto a API não responde.
   * POST: exemplo do corpo da requisição.
   */
  snapshot: unknown;
  code: { file: string; source: string };
};

type BlockProps = {
  id: string;
  /** Título da seção (h2). Blocos sem título trazem o próprio h1. */
  title?: string;
  under?: Underlayer;
  /** Na abertura a lente passa sozinha uma vez, para mostrar que existe. */
  demo?: boolean;
  children: ReactNode;
};

const MAX_JSON_LINES = 34;
const REPO_URL = process.env.NEXT_PUBLIC_REPO_URL;
const padding = "px-gutter pt-[clamp(1.5rem,4vw,3.25rem)] pb-[clamp(2.5rem,6vw,5rem)]";

function moveLens(element: HTMLElement, x: number, y: number) {
  element.style.setProperty("--x", `${x}px`);
  element.style.setProperty("--y", `${y}px`);
  element.dataset.lens = "";
}

/**
 * Uma seção da página. Tem duas camadas na mesma área: a superfície (o conteúdo) e, por
 * baixo, o endpoint que serve aquele dado. O botão do bloco alterna a camada inteira. Com
 * a lente ligada no cabeçalho, ela segue o cursor do mouse e o clique fixa a camada.
 */
export function Block({ id, title, under, demo = false, children }: BlockProps) {
  const section = useRef<HTMLElement>(null);
  const [open, setOpen] = useState(false);
  // A API só é chamada quando alguém olha por baixo do bloco pela primeira vez.
  const [used, setUsed] = useState(demo);
  // Só rotas de leitura são chamadas aqui; o POST do contato quem faz é o formulário.
  const { state, retry } = useApi(under?.method === "POST" ? undefined : under?.route, used);

  const lensOn = useLens();

  const titleId = `${id}-titulo`;
  const underId = `${id}-por-baixo`;

  // Desligou a lente com o cursor em cima de um bloco: ela some na hora.
  useEffect(() => {
    if (!lensOn) delete section.current?.dataset.lens;
  }, [lensOn]);

  function toggle() {
    setUsed(true);
    setOpen((current) => !current);
  }

  function handlePointerMove(event: PointerEvent<HTMLElement>) {
    if (!lensOn || event.pointerType !== "mouse") return;

    // Sobre link, botão ou formulário a lente some: a pessoa precisa ver onde clica.
    if ((event.target as Element).closest("a, button, form")) {
      delete event.currentTarget.dataset.lens;
      return;
    }

    const box = event.currentTarget.getBoundingClientRect();
    moveLens(event.currentTarget, event.clientX - box.left, event.clientY - box.top);
    setUsed(true);
  }

  function handlePointerLeave(event: PointerEvent<HTMLElement>) {
    delete event.currentTarget.dataset.lens;
  }

  function handleClick(event: MouseEvent<HTMLElement>) {
    // Clicar no bloco só fixa a camada com a lente ligada; sem ela, o clique é só um clique.
    if (!lensOn || !window.matchMedia("(pointer: fine)").matches) return;
    if ((event.target as Element).closest("a, button, form")) return;
    // Quem está selecionando texto para copiar não quer fechar a camada.
    if (window.getSelection()?.toString()) return;
    toggle();
  }

  function handleKeyDown(event: KeyboardEvent<HTMLElement>) {
    if (event.key === "Escape" && open) setOpen(false);
  }

  useEffect(() => {
    const element = section.current;
    if (!demo || !element) return;
    if (!window.matchMedia("(pointer: fine) and (prefers-reduced-motion: no-preference)").matches) return;

    // Uma vez por visita: voltar para a página inicial não repete a demonstração.
    try {
      if (sessionStorage.getItem("lente-demonstrada")) return;
    } catch {
      return;
    }

    let cancelled = false;
    let frame = 0;
    const cancel = () => {
      cancelled = true;
    };
    element.addEventListener("pointermove", cancel, { once: true });

    const timer = window.setTimeout(() => {
      sessionStorage.setItem("lente-demonstrada", "1");
      const start = performance.now();

      const step = (now: number) => {
        if (cancelled) return;
        const progress = Math.min((now - start) / 2200, 1);
        const eased = progress < 0.5 ? 2 * progress ** 2 : 1 - (-2 * progress + 2) ** 2 / 2;
        const box = element.getBoundingClientRect();
        moveLens(element, box.width * (0.12 + eased * 0.6), box.height * 0.42);

        if (progress < 1) frame = requestAnimationFrame(step);
        else delete element.dataset.lens;
      };

      frame = requestAnimationFrame(step);
    }, 700);

    return () => {
      cancelled = true;
      window.clearTimeout(timer);
      cancelAnimationFrame(frame);
      element.removeEventListener("pointermove", cancel);
    };
  }, [demo]);

  return (
    <section
      ref={section}
      id={id}
      aria-labelledby={title ? titleId : undefined}
      data-open={open ? "" : undefined}
      className="xray scroll-mt-4 border-t border-rule"
      onPointerMove={under ? handlePointerMove : undefined}
      onPointerLeave={under ? handlePointerLeave : undefined}
      onClick={under ? handleClick : undefined}
      onKeyDown={under ? handleKeyDown : undefined}
    >
      <div className={`surface ${padding}`} inert={open}>
        {under && (
          <p aria-hidden="true" className="route">
            <span className="font-medium text-signal">{under.method ?? "GET"}</span> {under.route}
          </p>
        )}
        {title && (
          <h2 id={titleId} className="display-2 mt-[clamp(1.5rem,4vw,3rem)]">
            {title}
          </h2>
        )}
        {children}
      </div>

      {under && (
        <>
          <div id={underId} className={`under ${padding}`} inert={!open}>
            <UnderContent under={under} state={state} retry={retry} />
          </div>

          <button type="button" className="peek" aria-pressed={open} aria-controls={underId} onClick={toggle}>
            {open ? "Voltar à superfície" : "Ver por baixo"}
          </button>
        </>
      )}
    </section>
  );
}

function statusText(state: ApiState) {
  switch (state.kind) {
    case "live":
      return `${state.status}${state.status === 200 ? " OK" : ""} · ${state.ms} ms · ao vivo`;
    case "loading":
      return "consultando a API… no plano gratuito ela dorme e pode levar até um minuto";
    case "down":
      return "a API não respondeu";
    case "unconfigured":
      return "API ainda não publicada";
    default:
      return "snapshot do build";
  }
}

function UnderContent({ under, state, retry }: { under: Underlayer; state: ApiState; retry: () => void }) {
  const post = under.method === "POST";
  const live = state.kind === "live";
  const lines = JSON.stringify(live ? state.body : under.snapshot, null, 2).split("\n");

  let responseLabel = "Resposta · snapshot do build";
  if (post) responseLabel = "Corpo da requisição · exemplo";
  else if (live) responseLabel = "Resposta ao vivo";
  const hiddenLines = lines.length - MAX_JSON_LINES;
  // O link para a resposta completa só aparece quando a API está respondendo.
  const responseUrl = live && API_URL ? API_URL + under.route : undefined;
  const codeUrl = REPO_URL ? `${REPO_URL}/blob/main/${under.code.file}` : undefined;

  return (
    <>
      <p className="mb-5 flex flex-wrap gap-x-5 gap-y-1 border-b border-paper/25 pr-40 pb-4">
        <span>
          <span className="font-medium text-under-signal">{under.method ?? "GET"}</span> {under.route}
        </span>
        <span role="status" className={live ? "text-under-signal" : "text-under-muted"}>
          {post ? "responde 202 Accepted quando a mensagem é gravada" : statusText(state)}
        </span>
        {state.kind === "down" && (
          <button type="button" className="link" onClick={retry}>
            Tentar de novo
          </button>
        )}
      </p>

      <div className="grid gap-x-12 gap-y-8 lg:grid-cols-2">
        <div className="min-w-0">
          <p className="label mb-2 text-under-muted">{responseLabel}</p>
          <pre>{lines.slice(0, MAX_JSON_LINES).join("\n")}</pre>
          {hiddenLines > 0 && (
            <p className="mt-2 text-under-muted">
              … mais {hiddenLines} linhas
              {responseUrl && (
                <>
                  {" · "}
                  <a href={responseUrl} className="link" target="_blank" rel="noreferrer">
                    resposta completa <span aria-hidden="true">↗</span>
                  </a>
                </>
              )}
            </p>
          )}
        </div>

        <div className="min-w-0">
          <p className="label mb-2 text-under-muted">
            {codeUrl ? (
              <a href={codeUrl} className="link" target="_blank" rel="noreferrer">
                {under.code.file} <span aria-hidden="true">↗</span>
              </a>
            ) : (
              under.code.file
            )}
          </p>
          <pre>{under.code.source}</pre>
        </div>
      </div>
    </>
  );
}
