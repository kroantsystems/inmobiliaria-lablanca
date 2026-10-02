# La Blanca Imóveis — Site

Monorepo do site da imobiliária La Blanca: frontend em Next.js e API em .NET 10 com PostgreSQL.

## Pré-requisitos

- [.NET SDK 10.0.202+](https://dotnet.microsoft.com/download) (fixado em `backend/global.json`)
- [Node.js 20+](https://nodejs.org/) e npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (para o PostgreSQL local)
- Ferramenta do EF Core: `dotnet tool install --global dotnet-ef`

## Estrutura

```
.
├── backend/                     # API .NET 10
│   ├── LaBlanca.sln
│   ├── Directory.Build.props        # Configurações comuns (net10.0, nullable, warnings como erro, auditoria NuGet)
│   ├── Directory.Packages.props     # Versões centralizadas dos pacotes
│   ├── src/
│   │   ├── LaBlanca.Api/            # Controllers, configuração, health check, OpenAPI
│   │   ├── LaBlanca.Application/    # Casos de uso (Commands/Queries)
│   │   ├── LaBlanca.Domain/         # Entidades e regras de negócio (sem dependências)
│   │   ├── LaBlanca.Infrastructure/ # EF Core, PostgreSQL, autenticação, arquivos
│   │   ├── LaBlanca.Migrations/     # Migrações do EF Core
│   │   └── LaBlanca.Shared/         # Erros (Problem Details) e recursos de idioma
│   ├── tools/LaBlanca.Tools/        # CLI de administração (criar admin, trocar senha)
│   └── tests/
│       ├── LaBlanca.UnitTests/        # Testes de unidade (sem Docker)
│       └── LaBlanca.IntegrationTests/ # Testes de integração (PostgreSQL via Testcontainers, exige Docker)
├── frontend/                    # Next.js (App Router, TypeScript, Tailwind)
├── openspec/                    # Especificações e mudanças (OpenSpec)
└── docker-compose.yml           # PostgreSQL local
```

## Rodando localmente

### 1. Banco de dados

```bash
docker compose up -d
```

Sobe o PostgreSQL em `localhost:5432` (banco `lablanca`, usuário/senha `postgres`/`postgres`, apenas para desenvolvimento).

### 2. API

```bash
cd backend
dotnet ef database update -p src/LaBlanca.Migrations -s src/LaBlanca.Api
dotnet run --project src/LaBlanca.Api --launch-profile http
```

- API: http://localhost:5080
- Health check: http://localhost:5080/health
- OpenAPI (só em Development): http://localhost:5080/openapi/v1.json

### 3. Frontend

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

Acesse http://localhost:3000. O rodapé da home mostra se a API está conectada.

## Administradores

Não existe cadastro de usuários pela API. O administrador é criado direto no banco pela ferramenta `LaBlanca.Tools` (a senha é pedida no terminal, sem eco; mínimo de 10 caracteres com letras e números):

```bash
cd backend
dotnet run --project tools/LaBlanca.Tools -- create-admin --tenant la-blanca --name "Seu Nome" --email admin@exemplo.com
```

Outros comandos: `reset-password --tenant la-blanca --email ...` (redefine a senha e encerra as sessões) e `hash-password`. Fora do ambiente local, informe a conexão com a variável `ConnectionStrings__Default` ou `--connection "<connection string>"`.

Se preferir inserir pelo SQL, gere o hash com `hash-password` e rode:

```sql
INSERT INTO lablanca."Users" ("Id", "TenantId", "Name", "Email", "PasswordHash", "Role", "IsActive", "FailedLoginCount", "CreatedAt")
VALUES (gen_random_uuid(), '6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71', 'Seu Nome', 'admin@exemplo.com', '<hash>', 'Admin', true, 0, now());
```

Depois de logado, o admin só pode trocar a própria senha em **Minha conta**.

## Comandos úteis

| Onde | Comando | O que faz |
|------|---------|-----------|
| `backend/` | `dotnet build` | Compila a solução |
| `backend/` | `dotnet test` | Roda todos os testes (integração exige Docker) |
| `backend/` | `dotnet test tests/LaBlanca.UnitTests` | Só testes de unidade |
| `backend/` | `dotnet ef migrations add <Nome> -p src/LaBlanca.Migrations -s src/LaBlanca.Api` | Cria uma migração |
| `frontend/` | `npm run lint` | ESLint |
| `frontend/` | `npm run build` | Build de produção |

## Configuração

- **API**: `ConnectionStrings:Default` e `Cors:AllowedOrigins` em `appsettings.Development.json`. Em produção, use variáveis de ambiente (`ConnectionStrings__Default`, `Cors__AllowedOrigins__0`).
- **Frontend**: `NEXT_PUBLIC_API_URL` em `.env.local`. Essa variável é embutida no build, então defina o valor certo antes de rodar `npm run build`.
- Arquivos `.env*` (exceto `.env.example`) e `appsettings.*.local.json` não são versionados.
