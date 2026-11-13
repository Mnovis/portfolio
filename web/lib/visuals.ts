// O que cada projeto mostra além do texto: uma tela do app, quando existe, e o modelo de
// dados. Tabelas, colunas e rotas daqui foram copiadas do código de cada projeto
// (migrations do EF Core, Script.sql ou schema.prisma), não inventadas para o desenho.
import type { StaticImageData } from "next/image";
import corteCerto from "@/public/projetos/corte-certo.png";

export type Column = { name: string; type: string; key?: "PK" | "FK" };
export type Table = { name: string; columns: Column[] };

export type DataModel = {
  /** De onde o modelo foi tirado. */
  source: string;
  tables: Table[];
  relations: string[];
};

export type Visual = {
  screenshot?: { src: StaticImageData; alt: string };
  model?: DataModel;
  /** Rotas da API, no formato "VERBO /caminho". */
  endpoints?: string[];
};

const pk = "PK" as const;
const fk = "FK" as const;

export const visuals: Record<string, Visual> = {
  portfolio: {
    model: {
      source: "Entity Framework Core · PostgreSQL",
      tables: [
        {
          name: "Projects",
          columns: [
            { name: "Id", type: "integer", key: pk },
            { name: "Slug", type: "varchar(80) único" },
            { name: "Name", type: "varchar(120)" },
            { name: "Highlights", type: "text[]" },
            { name: "Status", type: "varchar(20)" },
            { name: "Position", type: "integer" },
          ],
        },
        {
          name: "ProjectTechnology",
          columns: [
            { name: "ProjectsId", type: "integer", key: fk },
            { name: "TechnologiesId", type: "integer", key: fk },
          ],
        },
        {
          name: "Technologies",
          columns: [
            { name: "Id", type: "integer", key: pk },
            { name: "Name", type: "varchar(60) único" },
            { name: "Category", type: "varchar(20)" },
            { name: "Studying", type: "boolean" },
            { name: "Position", type: "integer" },
          ],
        },
        {
          name: "ContactMessages",
          columns: [
            { name: "Id", type: "integer", key: pk },
            { name: "Name", type: "varchar(100)" },
            { name: "Email", type: "varchar(254)" },
            { name: "Message", type: "varchar(4000)" },
            { name: "CreatedAt", type: "timestamptz" },
            { name: "NotifiedAt", type: "timestamptz nulo" },
          ],
        },
      ],
      relations: ["Projects N:N Technologies, pela tabela ProjectTechnology"],
    },
    endpoints: [
      "GET /api/profile",
      "GET /api/projects",
      "GET /api/projects/{slug}",
      "GET /api/education",
      "GET /api/technologies",
      "POST /api/contact",
    ],
  },

  "api-usuarios": {
    model: {
      source: "Entity Framework Core (Fluent API) · SQL Server",
      tables: [
        {
          name: "PERFIS",
          columns: [
            { name: "ID", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "nvarchar(25) único" },
          ],
        },
        {
          name: "USUARIOS",
          columns: [
            { name: "ID", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "nvarchar(150)" },
            { name: "EMAIL", type: "nvarchar(50) único" },
            { name: "SENHA", type: "nvarchar(100)" },
            { name: "DATACADASTRO", type: "datetime2" },
            { name: "PERFIL_ID", type: "uniqueidentifier", key: fk },
          ],
        },
      ],
      relations: ["PERFIS 1:N USUARIOS"],
    },
    endpoints: ["POST /api/v1/usuario/criar"],
  },

  clientes: {
    model: {
      source: "Dapper · SQL Server",
      tables: [
        {
          name: "CLIENTES",
          columns: [
            { name: "ID", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "varchar(150)" },
            { name: "EMAIL", type: "varchar(100)" },
            { name: "CPF", type: "varchar(14)" },
            { name: "DATACADASTRO", type: "datetime" },
          ],
        },
      ],
      relations: [],
    },
    endpoints: [
      "GET /api/v1/clientes",
      "POST /api/v1/clientes",
      "PUT /api/v1/clientes/{id}",
      "DELETE /api/v1/clientes/{id}",
    ],
  },

  taskflow: {
    model: {
      source: "Prisma · PostgreSQL",
      tables: [
        {
          name: "User",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "name", type: "String" },
            { name: "email", type: "String único" },
            { name: "passwordHash", type: "String" },
          ],
        },
        {
          name: "Board",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "title", type: "String" },
            { name: "ownerId", type: "String", key: fk },
          ],
        },
        {
          name: "BoardMember",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "role", type: "BoardRole" },
            { name: "boardId", type: "String", key: fk },
            { name: "userId", type: "String", key: fk },
          ],
        },
        {
          name: "Column",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "title", type: "String" },
            { name: "position", type: "Int" },
            { name: "boardId", type: "String", key: fk },
          ],
        },
        {
          name: "Card",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "title", type: "String" },
            { name: "priority", type: "Priority" },
            { name: "position", type: "Int" },
            { name: "columnId", type: "String", key: fk },
            { name: "assigneeId", type: "String nulo", key: fk },
          ],
        },
      ],
      relations: [
        "User 1:N Board (dono do quadro)",
        "Board N:N User, pela tabela BoardMember, com o papel de cada membro",
        "Board 1:N Column 1:N Card",
        "User 1:N Card (responsável pelo cartão)",
      ],
    },
  },

  "corte-certo": {
    screenshot: {
      src: corteCerto,
      alt: "Tela de agendamento do Corte Certo: escolha do serviço, do profissional, do dia e do horário.",
    },
    model: {
      source: "Prisma",
      tables: [
        {
          name: "User",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "email", type: "String único" },
            { name: "passwordHash", type: "String" },
            { name: "role", type: "Role" },
          ],
        },
        {
          name: "Service",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "name", type: "String" },
            { name: "durationMin", type: "Int" },
            { name: "priceCents", type: "Int" },
          ],
        },
        {
          name: "Professional",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "name", type: "String" },
            { name: "active", type: "Boolean" },
          ],
        },
        {
          name: "Appointment",
          columns: [
            { name: "id", type: "String (uuid)", key: pk },
            { name: "clientId", type: "String", key: fk },
            { name: "serviceId", type: "String", key: fk },
            { name: "professionalId", type: "String", key: fk },
            { name: "date", type: "Date" },
            { name: "startTime", type: "String (HH:mm)" },
            { name: "endTime", type: "String (HH:mm)" },
            { name: "status", type: "AppointmentStatus" },
          ],
        },
      ],
      relations: ["User 1:N Appointment (cliente)", "Service 1:N Appointment", "Professional 1:N Appointment"],
    },
  },

  "api-fornecedores": {
    model: {
      source: "Dapper · SQL Server",
      tables: [
        {
          name: "FORNECEDORES",
          columns: [
            { name: "ID", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "varchar(150)" },
            { name: "CNPJ", type: "char(14)" },
            { name: "EMAIL", type: "varchar(100)" },
            { name: "TELEFONE", type: "varchar(20)" },
            { name: "DATACADASTRO", type: "datetime" },
          ],
        },
      ],
      relations: [],
    },
    endpoints: [
      "GET /api/v1/fornecedores",
      "GET /api/v1/fornecedores/{id}",
      "POST /api/v1/fornecedores",
      "PUT /api/v1/fornecedores/{id}",
      "DELETE /api/v1/fornecedores/{id}",
    ],
  },

  "console-csharp": {
    model: {
      source: "Dapper · SQL Server (gestão escolar)",
      tables: [
        {
          name: "PROFESSORES",
          columns: [
            { name: "IDPROFESSOR", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "varchar(150)" },
            { name: "TELEFONE", type: "char(15)" },
            { name: "EMAIL", type: "varchar(150)" },
          ],
        },
        {
          name: "CURSOS",
          columns: [
            { name: "IDCURSO", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "varchar(150)" },
            { name: "CARGAHORARIA", type: "int" },
          ],
        },
        {
          name: "TURMAS",
          columns: [
            { name: "IDTURMA", type: "uniqueidentifier", key: pk },
            { name: "NOME", type: "varchar(150)" },
            { name: "DATAINICIO", type: "datetime" },
            { name: "HORARIO", type: "varchar(150)" },
            { name: "IDPROFESSOR", type: "uniqueidentifier", key: fk },
            { name: "IDCURSO", type: "uniqueidentifier", key: fk },
          ],
        },
      ],
      relations: ["PROFESSORES 1:N TURMAS", "CURSOS 1:N TURMAS"],
    },
  },

  "saas-financeiro": {
    model: {
      source: "Prisma (mais as tabelas de sessão do NextAuth)",
      tables: [
        {
          name: "User",
          columns: [
            { name: "id", type: "String (cuid)", key: pk },
            { name: "email", type: "String único" },
            { name: "password", type: "String nulo" },
          ],
        },
        {
          name: "Categoria",
          columns: [
            { name: "id", type: "String (cuid)", key: pk },
            { name: "nome", type: "String" },
            { name: "tipo", type: "TipoCategoria" },
            { name: "userId", type: "String", key: fk },
          ],
        },
        {
          name: "Receita",
          columns: [
            { name: "id", type: "String (cuid)", key: pk },
            { name: "descricao", type: "String" },
            { name: "valor", type: "Decimal(12,2)" },
            { name: "data", type: "DateTime" },
            { name: "userId", type: "String", key: fk },
            { name: "categoriaId", type: "String", key: fk },
          ],
        },
        {
          name: "Despesa",
          columns: [
            { name: "id", type: "String (cuid)", key: pk },
            { name: "descricao", type: "String" },
            { name: "valor", type: "Decimal(12,2)" },
            { name: "pago", type: "Boolean" },
            { name: "userId", type: "String", key: fk },
            { name: "categoriaId", type: "String", key: fk },
          ],
        },
      ],
      relations: [
        "User 1:N Categoria, Receita e Despesa: toda linha leva o userId, e é isso que isola os dados de cada usuário",
        "Categoria 1:N Receita e Despesa",
      ],
    },
  },
};
