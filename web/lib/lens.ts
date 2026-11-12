"use client";

import { useSyncExternalStore } from "react";

// A lente começa desligada: presa ao cursor o tempo todo, ela esconde o que a pessoa quer
// ler ou clicar. O botão do cabeçalho liga e desliga, e todos os blocos leem o mesmo valor.
let enabled = false;
const listeners = new Set<() => void>();

function subscribe(listener: () => void) {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
}

export function toggleLens() {
  enabled = !enabled;
  // O CSS usa este atributo para trocar o cursor enquanto a lente está ligada.
  document.documentElement.toggleAttribute("data-lente", enabled);
  listeners.forEach((listener) => listener());
}

export function useLens() {
  return useSyncExternalStore(
    subscribe,
    () => enabled,
    () => false,
  );
}
