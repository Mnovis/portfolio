"use client";

import { toggleLens, useLens } from "@/lib/lens";

/**
 * Liga e desliga a lente que segue o cursor. Só aparece para quem usa mouse; com toque
 * ou teclado, cada bloco tem o próprio botão "Ver por baixo".
 */
export function LensToggle() {
  const enabled = useLens();

  return (
    <button type="button" className="lens-toggle" aria-pressed={enabled} onClick={toggleLens}>
      <span aria-hidden="true" className="lens-toggle-dot" />
      {enabled ? "Lente ligada" : "Ligar lente"}
    </button>
  );
}
