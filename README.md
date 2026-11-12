# Portfólio — Mateus Marins

Portfólio de desenvolvedor com foco em back-end. O site tem duas camadas: a página, e por
baixo de cada bloco o endpoint, a resposta real da API e o trecho de C# que a gerou.

Em construção. Hoje o repositório tem o conteúdo, a API e o site com a camada de baixo e
o formulário de contato; falta a publicação.

## Estrutura

```
content/   Conteúdo do portfólio em JSON (perfil, projetos, formação, tecnologias)
api/       API em ASP.NET Core (.NET 10) + testes de integração
web/       Site em Next.js (geração estática)
```

`content/` é a fonte de verdade. Na subida, a API aplica as migrations e sincroniza o
banco com esses arquivos. O site lê os mesmos arquivos no build, então continua no ar
mesmo com a API fora.

## API

| Método | Rota | Devolve |
|---|---|---|
| GET | `/api/profile` | Perfil |
| GET | `/api/projects` | Projetos, com tecnologias |
| GET | `/api/projects/{slug}` | Um projeto completo, ou 404 |
| GET | `/api/education` | Formação |
| GET | `/api/technologies` | Tecnologias e os projetos que usam cada uma |
| POST | `/api/contact` | Recebe uma mensagem do formulário de contato (202) |
| GET | `/health` | Estado da API e do banco |

Documentação navegável em `/docs`; documento OpenAPI em `/openapi/v1.json`.

Uma tecnologia aparece como "comprovada" (`proven`) quando pelo menos um projeto a usa.
Isso sai de uma consulta ao relacionamento N:N entre projetos e tecnologias, não de um
campo preenchido à mão.

### Contato

`POST /api/contact` valida a requisição (400 com um erro por campo), grava a mensagem no
banco e só depois tenta avisar por e-mail. Se o envio falhar, a mensagem continua gravada,
com `NotifiedAt` vazio. Há um limite de mensagens por IP a cada 10 minutos (429) e um
campo-isca contra robôs.

| Configuração | Variável de ambiente | Para quê |
|---|---|---|
| `Email:ApiKey` | `Email__ApiKey` | Chave da API do Resend. Sem ela, a mensagem fica só no banco. |
| `Email:To` | `Email__To` | Endereço que recebe os avisos. |
| `Email:From` | `Email__From` | Remetente. Opcional. |
| `Contact:MaxPerWindow` | `Contact__MaxPerWindow` | Mensagens por IP a cada 10 minutos. Padrão: 3. |

Atrás de um proxy, defina `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true` para o limite usar
o IP de quem visita, e não o do proxy.

### Stack

ASP.NET Core com controllers, Entity Framework Core (Fluent API e migrations), PostgreSQL,
xUnit com `WebApplicationFactory` e Testcontainers.

### Decisões

- **Um projeto só, organizado em pastas** (`Domain`, `Data`, `Contracts`, `Controllers`).
  Separar em vários projetos por camada faz sentido quando há regra de negócio para
  proteger; aqui são poucos endpoints de leitura, e a separação seria só cerimônia.
- **Controllers consultam o `DbContext` direto**, sem repositório nem service no meio.
  As consultas são projeções simples para DTOs; uma camada a mais só repassaria chamadas.
- **Entidades nunca saem na resposta.** Tudo que a API devolve é um DTO de `Contracts`.
- **Conteúdo validado antes de tocar o banco.** Arquivo ausente, JSON inválido, slug
  repetido ou tecnologia inexistente derrubam a subida com uma mensagem que diz onde está
  o erro.
- **Testes contra PostgreSQL de verdade**, em contêiner. Provider em memória não se
  comporta como o banco real (colunas `text[]`, índices únicos, transações).

## Como rodar

Requisitos: .NET 10 SDK e Docker.

```bash
docker compose up -d
dotnet run --project api/src/Portfolio.Api
```

A API sobe em `http://localhost:5036`. Abra `http://localhost:5036/docs`.

### Site

Requisito: Node.js 20 ou mais novo.

```bash
npm install --prefix web
npm run dev --prefix web
```

O site sobe em `http://localhost:3000` e não precisa da API para funcionar.

#### Camada de baixo

Cada bloco do site tem, por baixo, o endpoint que serve aquele dado. Com mouse, uma lente
segue o cursor e o clique fixa a camada; com toque ou teclado, o botão "Ver por baixo"
alterna a camada inteira.

- **Resposta:** o navegador chama a API de verdade e mostra status e tempo medidos ali.
  Enquanto a API não responde, aparece um snapshot montado no build a partir de
  `content/`, identificado como snapshot.
- **Código:** o trecho de C# é lido de `api/` no build (`web/lib/underlayer.ts`), pelo
  arquivo inteiro ou por `#region`. Nada é copiado à mão.

| Variável | Para quê |
|---|---|
| `NEXT_PUBLIC_API_URL` | Endereço da API. Em desenvolvimento o padrão é `http://localhost:5036`. |
| `NEXT_PUBLIC_REPO_URL` | Endereço do repositório no GitHub, para o link do arquivo de código. Opcional. |

Na API, a origem do site precisa estar em `Cors:Origins` (variável `Cors__Origins__0`).

### Testes

```bash
dotnet test api/Portfolio.slnx
```

Os testes sobem um PostgreSQL próprio em contêiner; não usam o banco do `docker compose`.
