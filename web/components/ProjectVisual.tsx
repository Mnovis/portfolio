import Image from "next/image";
import { visuals, type DataModel } from "@/lib/visuals";

/** A parte visual da página de um projeto: tela do app, modelo de dados e rotas da API. */
export function ProjectVisual({ slug }: { slug: string }) {
  const visual = visuals[slug];
  if (!visual) return null;

  return (
    <div className="grid gap-8">
      {visual.screenshot && (
        <Image
          src={visual.screenshot.src}
          alt={visual.screenshot.alt}
          placeholder="blur"
          sizes="(min-width: 1024px) 55vw, 100vw"
          className="h-auto w-full border border-ink"
        />
      )}

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

function ModelDiagram({ model }: { model: DataModel }) {
  return (
    <figure>
      <div className="model-grid">
        {model.tables.map((table) => (
          <table key={table.name} className="model-table">
            <caption>{table.name}</caption>
            <tbody>
              {table.columns.map((column) => (
                <tr key={column.name}>
                  <th scope="row">{column.name}</th>
                  <td>{column.type}</td>
                  <td className="model-key">{column.key}</td>
                </tr>
              ))}
            </tbody>
          </table>
        ))}
      </div>

      <figcaption className="mt-3 font-mono text-[0.75rem] text-muted">Modelo de dados · {model.source}</figcaption>

      {model.relations.length > 0 && (
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
