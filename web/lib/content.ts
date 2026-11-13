// Conteúdo do site. A fonte é a pasta content/ da raiz do repositório: os mesmos
// arquivos que a API valida e carrega no banco.
import educationJson from "../../content/education.json";
import profileJson from "../../content/profile.json";
import projectsJson from "../../content/projects.json";
import technologiesJson from "../../content/technologies.json";

export type Status = "Done" | "InProgress";

export type Project = {
  slug: string;
  name: string;
  summary: string;
  context: string | null;
  highlights: string[];
  status: Status;
  featured: boolean;
  repoUrl: string | null;
  liveUrl: string | null;
  technologies: string[];
};

export type EducationItem = {
  institution: string;
  title: string;
  detail: string | null;
  status: Status;
};

export type Technology = {
  name: string;
  category: string;
  studying: boolean;
  /** Projetos que usam a tecnologia. */
  projects: Project[];
};

export const profile = profileJson;
export const projects = projectsJson as Project[];
export const education = educationJson as EducationItem[];

// Endereço público do site: o mesmo link que aparece no projeto "portfolio".
// Trocou de domínio? Basta mudar em content/projects.json.
export const siteUrl = projects.find((project) => project.slug === "portfolio")?.liveUrl ?? "http://localhost:3000";

const technologies: Technology[] = technologiesJson.map((technology) => ({
  ...technology,
  projects: projects.filter((project) => project.technologies.includes(technology.name)),
}));

// Mesma regra da API: comprovada = usada em pelo menos um projeto.
// A ordenação é estável, então empates mantêm a ordem de technologies.json.
export const provenTechnologies = technologies
  .filter((technology) => technology.projects.length > 0)
  .sort((a, b) => b.projects.length - a.projects.length);

export const studyingTechnologies = technologies.filter((technology) => technology.studying);

export const otherTechnologies = technologies.filter(
  (technology) => technology.projects.length === 0 && !technology.studying,
);

export const projectStatusLabel: Record<Status, string> = {
  Done: "Concluído",
  InProgress: "Em desenvolvimento",
};

export const educationStatusLabel: Record<Status, string> = {
  Done: "Concluído",
  InProgress: "Em andamento",
};
