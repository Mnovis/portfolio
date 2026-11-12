import type { Metadata } from "next";
import Link from "next/link";
import { notFound } from "next/navigation";
import { Block } from "@/components/Block";
import { projects, projectStatusLabel } from "@/lib/content";

// Só existem as páginas dos projetos de content/projects.json; o resto é 404.
export const dynamicParams = false;

export function generateStaticParams() {
  return projects.map((project) => ({ slug: project.slug }));
}

export async function generateMetadata({ params }: PageProps<"/projetos/[slug]">): Promise<Metadata> {
  const { slug } = await params;
  const project = projects.find((candidate) => candidate.slug === slug);
  return project ? { title: project.name, description: project.summary } : {};
}

export default async function ProjectPage({ params }: PageProps<"/projetos/[slug]">) {
  const { slug } = await params;
  const project = projects.find((candidate) => candidate.slug === slug);
  if (!project) notFound();

  const rowClass = "border-t border-rule py-3";

  return (
    <Block id="projeto" route={`/api/projects/${project.slug}`}>
      <p className="mt-8">
        <Link href="/#projetos" className="link inline-block py-1 font-mono text-[0.8125rem]">
          <span aria-hidden="true">←</span> Projetos
        </Link>
      </p>

      <h1 className="display-2 mt-6">{project.name}</h1>

      <div className="mt-[clamp(1.5rem,4vw,3rem)] grid gap-x-16 gap-y-10 lg:grid-cols-[1fr_2.2fr]">
        <div className="lg:col-start-2">
          <p className="max-w-[38em] text-[clamp(1.0625rem,1.4vw,1.25rem)] leading-normal">{project.summary}</p>

          <h2 className="label mt-10 mb-2 text-muted">Destaques</h2>
          <ul className="max-w-[44em]">
            {project.highlights.map((highlight) => (
              <li key={highlight} className="relative border-t border-rule py-2.5 pl-7">
                <span aria-hidden="true" className="absolute left-0 font-mono text-signal">
                  →
                </span>
                {highlight}
              </li>
            ))}
          </ul>
        </div>

        <dl className="grid content-start lg:col-start-1 lg:row-start-1">
          <div className={rowClass}>
            <dt className="label text-muted">Estado</dt>
            <dd className="mt-1">{projectStatusLabel[project.status]}</dd>
          </div>

          {project.context && (
            <div className={rowClass}>
              <dt className="label text-muted">Contexto</dt>
              <dd className="mt-1">{project.context}</dd>
            </div>
          )}

          <div className={rowClass}>
            <dt className="label text-muted">Tecnologias</dt>
            <dd className="mt-1 font-mono text-[0.8125rem] leading-[1.8]">{project.technologies.join(" · ")}</dd>
          </div>

          <div className={rowClass}>
            <dt className="label text-muted">Links</dt>
            <dd className="mt-1 grid gap-1">
              {project.repoUrl && (
                <a href={project.repoUrl} className="link inline-block py-1" target="_blank" rel="noreferrer">
                  Repositório <span aria-hidden="true">↗</span>
                  <span className="sr-only"> (abre em nova aba)</span>
                </a>
              )}
              {project.liveUrl && (
                <a href={project.liveUrl} className="link inline-block py-1" target="_blank" rel="noreferrer">
                  Ver publicado <span aria-hidden="true">↗</span>
                  <span className="sr-only"> (abre em nova aba)</span>
                </a>
              )}
              {!project.repoUrl && !project.liveUrl && (
                <span className="text-muted">Repositório ainda não está público.</span>
              )}
            </dd>
          </div>
        </dl>
      </div>
    </Block>
  );
}
