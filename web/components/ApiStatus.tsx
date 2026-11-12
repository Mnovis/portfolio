"use client";

import { useApi } from "@/lib/useApi";

/** Estado real da API, medido no navegador de quem visita. */
export function ApiStatus({ route }: { route: string }) {
  const { state } = useApi(route, true);
  const live = state.kind === "live";

  let text: string;
  if (state.kind === "live") text = `${state.status}${state.status === 200 ? " OK" : ""} · ${state.ms} ms`;
  else if (state.kind === "down") text = "sem resposta agora";
  else if (state.kind === "unconfigured") text = "ainda não publicada";
  else text = "consultando…";

  return (
    <span role="status" className="inline-flex items-center gap-2 font-mono text-[0.8125rem]">
      <span
        aria-hidden="true"
        className={`size-2 shrink-0 rounded-full border border-signal ${live ? "bg-signal" : ""}`}
      />
      {text}
    </span>
  );
}
