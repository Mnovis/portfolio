// O que cada bloco mostra na camada de baixo. Roda só no build (usa o sistema de arquivos).
import fs from "node:fs";
import path from "node:path";
import technologiesJson from "../../content/technologies.json";
import type { Underlayer } from "@/components/Block";
import { education, profile, projects, type Project } from "./content";

const API_SOURCE = path.join(process.cwd(), "..", "api", "src", "Portfolio.Api");

/**
 * Lê um trecho de C# direto do código da API. Com `region`, pega o que está entre
 * `#region nome` e `#endregion`; sem ela, o arquivo inteiro depois do namespace.
 * O código exibido no site nunca é copiado à mão, então não fica desatualizado.
 */
function csharp(file: string, region?: string) {
  const lines = fs.readFileSync(path.join(API_SOURCE, file), "utf8").split(/\r?\n/);

  let start: number;
  let end: number;
  if (region) {
    start = lines.findIndex((line) => line.trim() === `#region ${region}`) + 1;
    end = lines.findIndex((line, index) => index >= start && line.trim() === "#endregion");
    if (start === 0 || end === -1) throw new Error(`Região "${region}" não encontrada em ${file}.`);
  } else {
    start = lines.findIndex((line) => line.startsWith("namespace ")) + 1;
    end = lines.length;
    if (start === 0) throw new Error(`Namespace não encontrado em ${file}.`);
  }

  const body = lines.slice(start, end);
  const indent = Math.min(...body.filter((line) => line.trim()).map((line) => line.search(/\S/)));
  const source = body
    .map((line) => line.slice(indent))
    .join("\n")
    .trim();

  return { file: `api/src/Portfolio.Api/${file}`, source };
}

// Os snapshots abaixo repetem, em TypeScript, o formato das respostas da API (os records
// de Contracts/Responses.cs). Servem enquanto a API não responde; quando ela responde, a
// resposta ao vivo toma o lugar. Simplificação consciente: se um DTO mudar, este arquivo
// precisa mudar junto. Se isso virar problema, a API passa a exportar os snapshots.

const position = new Map(technologiesJson.map((technology, index) => [technology.name, index]));

/** A API devolve as tecnologias de um projeto na ordem de technologies.json. */
function inCatalogOrder(names: string[]) {
  return [...names].sort((a, b) => (position.get(a) ?? 0) - (position.get(b) ?? 0));
}

function projectSummary(project: Project) {
  return {
    slug: project.slug,
    name: project.name,
    summary: project.summary,
    status: project.status,
    featured: project.featured,
    technologies: inCatalogOrder(project.technologies),
  };
}

function projectDetail(project: Project) {
  return {
    slug: project.slug,
    name: project.name,
    summary: project.summary,
    context: project.context,
    highlights: project.highlights,
    status: project.status,
    featured: project.featured,
    repoUrl: project.repoUrl,
    liveUrl: project.liveUrl,
    technologies: inCatalogOrder(project.technologies),
  };
}

export const underlayers = {
  profile: {
    route: "/api/profile",
    snapshot: profile,
    code: csharp("Controllers/ProfileController.cs"),
  },
  projects: {
    route: "/api/projects",
    snapshot: projects.map(projectSummary),
    code: csharp("Controllers/ProjectsController.cs", "GetAll"),
  },
  technologies: {
    route: "/api/technologies",
    snapshot: technologiesJson.map((technology) => {
      const usedIn = projects
        .filter((project) => project.technologies.includes(technology.name))
        .map((project) => ({ slug: project.slug, name: project.name }));

      return {
        name: technology.name,
        category: technology.category,
        studying: technology.studying,
        projects: usedIn,
        proven: usedIn.length > 0,
      };
    }),
    code: csharp("Controllers/TechnologiesController.cs"),
  },
  education: {
    route: "/api/education",
    snapshot: education,
    code: csharp("Controllers/EducationController.cs"),
  },
} satisfies Record<string, Underlayer>;

export function projectUnderlayer(project: Project): Underlayer {
  return {
    route: `/api/projects/${project.slug}`,
    snapshot: projectDetail(project),
    code: csharp("Controllers/ProjectsController.cs", "GetBySlug"),
  };
}
