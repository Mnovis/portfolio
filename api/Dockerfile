# Imagem da API. O contexto do build é a raiz do repositório, porque a API leva content/ junto:
#   docker build -f api/Dockerfile -t portfolio-api .

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Só o .csproj primeiro: enquanto as dependências não mudam, o Docker reaproveita o restore.
COPY api/src/Portfolio.Api/Portfolio.Api.csproj api/src/Portfolio.Api/
RUN dotnet restore api/src/Portfolio.Api/Portfolio.Api.csproj

COPY content/ content/
COPY api/src/Portfolio.Api/ api/src/Portfolio.Api/
RUN dotnet publish api/src/Portfolio.Api/Portfolio.Api.csproj -c Release -o /app --no-restore

# A imagem final tem só o runtime e a aplicação publicada, sem SDK nem código-fonte.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Roda sem privilégios de root.
USER $APP_UID

# A hospedagem informa a porta na variável PORT; sem ela, 8080.
CMD ["sh", "-c", "ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080} exec dotnet Portfolio.Api.dll"]
