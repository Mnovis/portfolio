import type { ReactNode } from "react";

type BlockProps = {
  id: string;
  /** Rota da API que serve os dados deste bloco. */
  route?: string;
  /** Título da seção (h2). Blocos sem título trazem o próprio h1. */
  title?: string;
  children: ReactNode;
};

/** Uma seção da página. Cada bloco corresponde a um endpoint da API. */
export function Block({ id, route, title, children }: BlockProps) {
  const titleId = `${id}-titulo`;

  return (
    <section
      id={id}
      aria-labelledby={title ? titleId : undefined}
      className="scroll-mt-4 border-t border-rule px-gutter pt-[clamp(1.5rem,4vw,3.25rem)] pb-[clamp(2.5rem,6vw,5rem)]"
    >
      {route && (
        <p aria-hidden="true" className="route">
          <span className="font-medium text-signal">GET</span> {route}
        </p>
      )}
      {title && (
        <h2 id={titleId} className="display-2 mt-[clamp(1.5rem,4vw,3rem)]">
          {title}
        </h2>
      )}
      {children}
    </section>
  );
}
