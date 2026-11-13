import type { MetadataRoute } from "next";
import { projects, siteUrl } from "@/lib/content";

// A página inicial e uma página por projeto de content/projects.json.
export default function sitemap(): MetadataRoute.Sitemap {
  return [{ url: siteUrl }, ...projects.map((project) => ({ url: `${siteUrl}/projetos/${project.slug}` }))];
}
