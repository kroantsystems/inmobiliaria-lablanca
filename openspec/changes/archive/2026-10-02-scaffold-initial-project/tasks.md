## 1. Repositório e raiz

- [x] 1.1 Inicializar repositório git na raiz do projeto
- [x] 1.2 Criar `.gitignore` cobrindo Node.js, Next.js, .NET, IDEs e arquivos `.env*`/`appsettings.*.local.json` (exceto `.env.example`)
- [x] 1.3 Criar `.editorconfig` com convenções para C#, TypeScript, JSON e Markdown

## 2. Ambiente local

- [x] 2.1 Criar `docker-compose.yml` com serviço PostgreSQL (banco `lablanca`, porta 5432, volume nomeado)
- [x] 2.2 Validar que `docker compose up -d` sobe o banco e ele aceita conexões

## 3. Backend .NET 10

- [x] 3.1 Criar `backend/global.json` fixando o SDK .NET 10 e a solução `LaBlanca.sln`
- [x] 3.2 Criar projetos `LaBlanca.Domain`, `LaBlanca.Application`, `LaBlanca.Infrastructure` (classlib) e `LaBlanca.Api` (web, Minimal APIs), adicioná-los à solução com as referências corretas
- [x] 3.3 Adicionar EF Core + Npgsql em `Infrastructure`, criar `AppDbContext` e método de extensão para registrar a infraestrutura via `ConnectionStrings:Default`
- [x] 3.4 Configurar `appsettings.json` e `appsettings.Development.json` (connection string local, `Cors:AllowedOrigins` com `http://localhost:3000`)
- [x] 3.5 Configurar em `Program.cs`: OpenAPI apenas em Development, CORS por configuração e health checks com checagem do `AppDbContext` em `/health`
- [x] 3.6 Criar a migração inicial (`InitialCreate`) e validar `dotnet ef database update` contra o banco local
- [x] 3.7 Criar projeto `LaBlanca.Tests` (xUnit) com teste de integração do `/health` usando `WebApplicationFactory`
- [x] 3.8 Validar `dotnet build` e `dotnet test` sem erros

## 4. Frontend Next.js

- [x] 4.1 Criar app em `frontend/` com `create-next-app` (App Router, TypeScript, Tailwind, ESLint, pasta `src/`)
- [x] 4.2 Configurar `layout.tsx` com `lang="pt-BR"`, metadata com título "La Blanca", componentes `Header` e `Footer`
- [x] 4.3 Criar página inicial placeholder da imobiliária
- [x] 4.4 Criar cliente HTTP em `src/lib/api.ts` usando `NEXT_PUBLIC_API_URL` e `frontend/.env.example`
- [x] 4.5 Exibir na home o status retornado por `/health` da API (indicador simples de conexão)
- [x] 4.6 Validar `npm run lint` e `npm run build` sem erros

## 5. Documentação e verificação final

- [x] 5.1 Escrever `README.md` com pré-requisitos, estrutura de pastas e passos para rodar banco, API e frontend
- [x] 5.2 Seguir o README do zero e confirmar que a home exibe o status da API
- [x] 5.3 Confirmar com `git status` que `node_modules/`, `.next/`, `bin/` e `obj/` não aparecem
