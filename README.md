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
| POST | `/api/admin/login` | Token JWT do painel (401 com e-mail ou senha errados) |
| GET | `/api/admin/overview` | Contagens de mensagens e de conteúdo. Exige token |
| GET | `/api/admin/messages` | Mensagens do formulário, mais recentes primeiro. Exige token |
| PATCH | `/api/admin/messages/{id}` | Marca a mensagem como lida ou não lida (204). Exige token |
| DELETE | `/api/admin/messages/{id}` | Exclui a mensagem (204). Exige token |
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

### Painel

A página `/admin` do site mostra as mensagens do formulário e uma visão geral com
contagens. `POST /api/admin/login` confere e-mail e senha e devolve um JWT válido por
2 horas; as outras rotas `api/admin/*` só respondem com esse token no cabeçalho
`Authorization: Bearer ...` (401 sem ele). Há um limite de tentativas de login por IP a
cada 10 minutos (429).

| Configuração | Variável de ambiente | Para quê |
|---|---|---|
| `Admin:Email` | `Admin__Email` | E-mail do administrador. |
| `Admin:Password` | `Admin__Password` | Senha do administrador. |
| `Admin:JwtKey` | `Admin__JwtKey` | Segredo que assina os tokens. No mínimo 32 caracteres. |
| `Admin:MaxLoginsPerWindow` | `Admin__MaxLoginsPerWindow` | Tentativas de login por IP a cada 10 minutos. Padrão: 5. |

Sem as três primeiras, o login responde 503 e o resto da API funciona normalmente. Os
valores nunca ficam no repositório: em desenvolvimento, defina as variáveis no terminal
antes de `dotnet run`.

### Stack

ASP.NET Core com controllers, autenticação JWT, Entity Framework Core (Fluent API e migrations), PostgreSQL,
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
- **Um administrador, com e-mail e senha na configuração.** Tabela de usuários com hash
  de senha só faz sentido a partir do segundo usuário. A comparação é em tempo constante
  e a resposta é a mesma para e-mail errado e para senha errada.
- **Token do painel no `sessionStorage`, não em cookie.** Site e API ficam em domínios
  diferentes, e o navegador bloqueia cookie de terceiros. Em troca, o token vale pouco
  tempo e some ao fechar a aba.
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

A cada push, o GitHub Actions (`.github/workflows/ci.yml`) roda os testes da API e o lint
e o build do site.

## Publicação

Três serviços, todos em plano gratuito: banco no Neon, API no Render, site na Vercel.

1. **Neon** — crie um projeto PostgreSQL e copie a connection string no formato .NET:
   `Host=...;Database=...;Username=...;Password=...;SSL Mode=Require`.
2. **Render** — *New > Blueprint*, escolha este repositório. O `render.yaml` descreve o
   serviço (imagem Docker de `api/Dockerfile`); o painel pede os valores:
   - `ConnectionStrings__Default`: a connection string do Neon.
   - `Cors__Origins__0`: o endereço do site na Vercel, sem barra no final.
   - `Email__ApiKey` e `Email__To`: chave do Resend e o e-mail que recebe os avisos.
   - `Admin__Email`, `Admin__Password` e `Admin__JwtKey`: login do painel e o segredo que
     assina os tokens (texto aleatório de 32 caracteres ou mais).
3. **Vercel** — importe o repositório com *Root Directory* `web`, mantendo ligada a opção
   de incluir arquivos de fora desse diretório (o build lê `content/` e `api/`). Variáveis:
   - `NEXT_PUBLIC_API_URL`: o endereço da API no Render, sem barra no final.
   - `NEXT_PUBLIC_REPO_URL`: o endereço deste repositório.

No plano gratuito do Render a API dorme depois de 15 minutos sem tráfego e leva cerca de
um minuto para acordar. O site continua no ar nesse intervalo: mostra o snapshot do build
e troca pela resposta ao vivo quando a API responde.

Para testar a imagem da API localmente:

```bash
docker build -f api/Dockerfile -t portfolio-api .
```

Os testes sobem um PostgreSQL próprio em contêiner; não usam o banco do `docker compose`.
