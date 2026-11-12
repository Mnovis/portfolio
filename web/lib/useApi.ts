"use client";

import { useEffect, useSyncExternalStore } from "react";

// Em desenvolvimento a API roda em localhost:5036 (dotnet run). Em produção o endereço
// vem de NEXT_PUBLIC_API_URL; sem ela, o site mostra só o snapshot do build.
export const API_URL =
  process.env.NEXT_PUBLIC_API_URL ?? (process.env.NODE_ENV === "development" ? "http://localhost:5036" : undefined);

export type ApiState =
  | { kind: "idle" }
  | { kind: "unconfigured" }
  | { kind: "loading" }
  | { kind: "live"; status: number; ms: number; body: unknown }
  | { kind: "down" };

type Result = Extract<ApiState, { kind: "live" | "down" }>;

// Um estado por rota, fora dos componentes: quem pergunta a mesma coisa (o estado da API
// na abertura e a camada de baixo do mesmo bloco) vê a mesma resposta e a mesma tentativa.
const results = new Map<string, Result>();
const pending = new Set<string>();
const listeners = new Set<() => void>();

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

function notify() {
  listeners.forEach((listener) => listener());
}

function load(route: string) {
  if (pending.has(route)) return;

  pending.add(route);
  results.delete(route);
  notify();

  const started = performance.now();

  // O plano gratuito da hospedagem dorme sem tráfego; a primeira chamada espera a API acordar.
  fetch(API_URL + route, { signal: AbortSignal.timeout(70_000) })
    .then(async (response): Promise<Result> => {
      const body: unknown = await response.json();
      return { kind: "live", status: response.status, ms: Math.round(performance.now() - started), body };
    })
    .catch((): Result => ({ kind: "down" }))
    .then((result) => {
      pending.delete(route);
      results.set(route, result);
      notify();
    });
}

/** Chama a API de verdade, do navegador de quem visita, e mede o tempo da resposta. */
export function useApi(route: string | undefined, enabled: boolean) {
  const result = useSyncExternalStore(
    subscribe,
    () => (route ? results.get(route) : undefined),
    () => undefined,
  );

  const active = Boolean(route && enabled && API_URL);

  useEffect(() => {
    if (active && route && !results.has(route)) load(route);
  }, [active, route]);

  let state: ApiState;
  if (!route || !enabled) state = { kind: "idle" };
  else if (!API_URL) state = { kind: "unconfigured" };
  else state = result ?? { kind: "loading" };

  return { state, retry: () => route && load(route) };
}
