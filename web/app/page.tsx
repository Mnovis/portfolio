import Link from "next/link";
import type { ReactNode } from "react";
import { ApiStatus } from "@/components/ApiStatus";
import { Block } from "@/components/Block";
import { ContactForm } from "@/components/ContactForm";
import { underlayers } from "@/lib/underlayer";
import {
  education,
  educationStatusLabel,
  otherTechnologies,
  profile,
  projects,
  projectStatusLabel,
  provenTechnologies,
  studyingTechnologies,
} from "@/lib/content";

// PDF em web/public. Arquivo fixo: mudou o conteúdo do site, gere o PDF de novo.
const resumeUrl = "/curriculo-mateus-marins.pdf";

const sectionGap ="mt-[clamp(1.5rem,4vw,3rem)]";
const twoColumns = "grid gap-x-16 lg:grid-cols-[1fr_2.2fr]";

function Fact({ term, children }: { term: string; children: ReactNode }) {
  return (
    <div className="grid grid-cols-[5.5rem_1fr] items-baseline gap-4 border-t border-rule py-2.5">
      <dt className="label text-muted">{term}</dt>
      <dd>{children}</dd>
    </div>
  );
}

function ExternalLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <a href={href} className="link inline-block py-1" target="_blank" rel="noreferrer">
      {children} <span aria-hidden="true">↗</span>
      <span className="sr-only"> (abre em nova aba)</span>
    </a>
  );
}

export default function Home() {
  const [firstName, ...otherNames] = profile.shortName.split(" ");
  const featured = projects.filter((project) => project.featured);
  const others = projects.filter((project) => !project.featured);

  return (
    <>
      <Block id="abertura" under={underlayers.profile} demo>
        <h1 className="display-1 mt-[clamp(2rem,7vw,5rem)] mb-[clamp(1.5rem,5vw,3.5rem)]">
          {firstName} <em>{otherNames.join(" ")}</em>
        </h1>

        <div className="grid items-end gap-x-16 gap-y-8 lg:grid-cols-[1.1fr_1fr]">
          <div className="max-w-[34em] text-[clamp(1.125rem,1.6vw,1.375rem)] leading-[1.45]">
            <p className="font-medium">{profile.headline}.</p>
            <p className="mt-3">{profile.summary}</p>
          </div>

          <dl>
            <Fact term="Foco">{profile.focus}</Fact>
            <Fact term="Busco">{profile.goal}</Fact>
            <Fact term="Onde">
              {profile.location}
              {profile.remote && " · aberto a remoto"}
            </Fact>
            <Fact term="Links">
              <ExternalLink href={profile.links.github}>GitHub</ExternalLink>
              <span aria-hidden="true"> · </span>
              <ExternalLink href={profile.links.linkedin}>LinkedIn</ExternalLink>
              <span aria-hidden="true"> · </span>
              <ExternalLink href={resumeUrl}>Currículo (PDF)</ExternalLink>
            </Fact>
            <Fact term="API">
              <ApiStatus route={underlayers.profile.route} />
            </Fact>
          </dl>
        </div>
      </Block>

      <Block id="projetos" under={underlayers.projects} title="Projetos">
        <ol className={sectionGap}>
          {featured.map((project, index) => (
            <li key={project.slug} className={`${twoColumns} gap-y-4 border-t border-rule py-8`}>
              <div className="lg:col-start-2">
                <h3 className="display-3">{project.name}</h3>
                <p className="mt-4 max-w-[38em] text-[clamp(1.0625rem,1.4vw,1.25rem)] leading-normal">
                  {project.summary}
                </p>
                <p className="mt-5">
                  <Link
                    href={`/projetos/${project.slug}`}
                    className="link inline-block py-1 font-mono text-[0.8125rem]"
                    aria-label={`Ver projeto ${project.name}`}
                  >
                    Ver projeto <span aria-hidden="true">→</span>
                  </Link>
                </p>
              </div>

              <div className="grid content-start gap-3 lg:col-start-1 lg:row-start-1">
                <p
                  aria-hidden="true"
                  className="hidden font-display text-[clamp(2.5rem,5vw,4rem)] leading-none text-signal lg:block"
                >
                  {String(index + 1).padStart(2, "0")}
                </p>
                <p className="label">{projectStatusLabel[project.status]}</p>
                <p className="font-mono text-[0.8125rem] leading-[1.8] text-muted">
                  {project.technologies.join(" · ")}
                </p>
              </div>
            </li>
          ))}
        </ol>

        <h3 className="label mt-12 mb-2 text-muted">Outros projetos</h3>
        <ul>
          {others.map((project) => (
            <li key={project.slug} className={`${twoColumns} gap-y-1 border-t border-rule py-4`}>
              <p>
                <Link href={`/projetos/${project.slug}`} className="link inline-block py-1 font-medium">
                  {project.name}
                </Link>
              </p>
              <p className="text-muted lg:pt-1">{project.summary}</p>
            </li>
          ))}
        </ul>
      </Block>

      <Block id="stack" under={underlayers.technologies} title="Stack">
        <div className={`${sectionGap} grid gap-x-16 gap-y-10 lg:grid-cols-[1.6fr_1fr]`}>
          <div>
            <h3 className="label mb-2 text-muted">Comprovada em projeto</h3>
            <ul className="gap-x-12 [columns:2_14rem]">
              {provenTechnologies.map((technology) => (
                <li
                  key={technology.name}
                  className="flex break-inside-avoid items-baseline gap-2.5 border-t border-rule py-2"
                >
                  <span className="font-medium">{technology.name}</span>
                  <span aria-hidden="true" className="flex-1 -translate-y-1 border-b border-dotted border-muted" />
                  <span className="font-mono text-[0.8125rem] text-muted">
                    {technology.projects.length} {technology.projects.length === 1 ? "projeto" : "projetos"}
                  </span>
                </li>
              ))}
            </ul>
          </div>

          <div className="grid content-start gap-10">
            <div>
              <h3 className="label mb-2 text-muted">Estudando agora</h3>
              <ul>
                {studyingTechnologies.map((technology) => (
                  <li key={technology.name} className="border-t border-rule py-2">
                    {technology.name}
                  </li>
                ))}
              </ul>
            </div>

            <div>
              <h3 className="label mb-2 text-muted">Também já usei</h3>
              <p className="border-t border-rule py-2 text-muted">
                {otherTechnologies.map((technology) => technology.name).join(" · ")}
              </p>
            </div>
          </div>
        </div>
      </Block>

      <Block id="formacao" under={underlayers.education} title="Formação">
        <ul className={sectionGap}>
          {education.map((item) => (
            <li
              key={`${item.institution}-${item.title}`}
              className={`${twoColumns} gap-y-1 border-t border-rule py-4`}
            >
              <p className="label flex items-center gap-2.5 lg:pt-1">
                <span
                  aria-hidden="true"
                  className={`size-2 shrink-0 rounded-full border border-ink ${item.status === "Done" ? "bg-ink" : ""}`}
                />
                {educationStatusLabel[item.status]}
              </p>
              <div>
                <p className="font-medium">{item.title}</p>
                <p className="text-muted">
                  {item.institution}
                  {item.detail && ` — ${item.detail}`}
                </p>
              </div>
            </li>
          ))}
        </ul>
      </Block>

      <Block id="contato" under={underlayers.contact} title="Contato">
        <div className={`${sectionGap} grid gap-x-16 gap-y-10 lg:grid-cols-[1fr_1.2fr]`}>
          <div>
            <p className="display-3 max-w-[16em]">{profile.goal}.</p>
            <ul className="mt-8 flex flex-wrap gap-x-10 gap-y-2 text-[clamp(1.125rem,1.6vw,1.375rem)]">
              <li>
                <ExternalLink href={profile.links.linkedin}>LinkedIn</ExternalLink>
              </li>
              <li>
                <ExternalLink href={profile.links.github}>GitHub</ExternalLink>
              </li>
              <li>
                <ExternalLink href={resumeUrl}>Currículo (PDF)</ExternalLink>
              </li>
            </ul>
          </div>

          <ContactForm />
        </div>
      </Block>
    </>
  );
}
