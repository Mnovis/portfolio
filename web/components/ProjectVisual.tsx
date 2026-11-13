import Image from "next/image";
import { visuals, type DataModel } from "@/lib/visuals";

type ProjectVisualProps = {
  slug: string;
  /** Versão resumida, para a lista da página inicial: uma imagem só, poucas tabelas. */
  compact?: boolean;
};

const COMPACT_TABLES = 3;
const COMPACT_COLUMNS = 5;

/** A parte visual de um projeto: tela do app, modelo de dados e rotas da API. */
export function ProjectVisual({ slug, compact = false }: ProjectVisualProps) {
  const visual = visuals[slug];
  if (!visual) return null;

  const screenshot = visual.screenshot && (
    <Image
      src={visual.screenshot.src}
      alt={visual.screenshot.alt}
      placeholder="blur"
      sizes="(min-width: 1024px) 55vw, 100vw"
      className="h-auto w-full border border-ink"
    />
  );

  // Na página inicial, quem tem tela mostra a tela; quem não tem, mostra o modelo de dados.
  if (compact) return screenshot ?? (visual.model ? <ModelDiagram model={visual.model} compact /> : null);

  return (
    <div className="grid gap-8">
      {screenshot}
      {visual.model && <ModelDiagram model={visual.model} />}
      {visual.endpoints && (
        <div>
          <h2 className="label mb-2 text-muted">Rotas</h2>
          <ul className="font-mono text-[0.8125rem]">
            {visual.endpoints.map((endpoint) => {
              const [verb, path] = endpoint.split(" ");
              return (
                <li key={endpoint} className="border-t border-rule py-1.5">
                  <span className="inline-block w-16 font-medium text-signal">{verb}</span>
                  {path}
                </li>
              );
            })}
          </ul>
        </div>
      )}
    </div>
  );
}

function ModelDiagram({ model, compact = false }: { model: DataModel; compact?: boolean }) {
  const tables = compact ? model.tables.slice(0, COMPACT_TABLES) : model.tables;
  const hiddenTables = model.tables.length - tables.length;

  return (
    <figure>
      <div className="model-grid">
        {tables.map((table) => {
          const columns = compact ? table.columns.slice(0, COMPACT_COLUMNS) : table.columns;
          const hiddenColumns = table.columns.length - columns.length;

          return (
            <table key={table.name} className="model-table">
              <caption>{table.name}</caption>
              <tbody>
                {columns.map((column) => (
                  <tr key={column.name}>
                    <th scope="row">{column.name}</th>
                    <td>{column.type}</td>
                    <td className="model-key">{column.key}</td>
                  </tr>
                ))}
                {hiddenColumns > 0 && (
                  <tr>
                    <td colSpan={3}>
                      + {hiddenColumns} {hiddenColumns === 1 ? "coluna" : "colunas"}
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          );
        })}
      </div>

      <figcaption className="mt-3 font-mono text-[0.75rem] text-muted">
        Modelo de dados · {model.source}
        {hiddenTables > 0 && ` · + ${hiddenTables} ${hiddenTables === 1 ? "tabela" : "tabelas"}`}
      </figcaption>

      {!compact && model.relations.length > 0 && (
        <ul className="mt-3 font-mono text-[0.8125rem]">
          {model.relations.map((relation) => (
            <li key={relation} className="border-t border-rule py-1.5">
              {relation}
            </li>
          ))}
        </ul>
      )}
    </figure>
  );
}
