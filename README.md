# Inmobiliaria La Blanca — Site e painel

Site público multilíngue (espanhol, português, inglês e guarani) e painel administrativo da Inmobiliaria La Blanca (Ciudad del Este, Paraguai).

- **Frontend**: Next.js 16 (App Router, React 19, Tailwind 4, next-intl), Vitest.
- **API**: ASP.NET Core / .NET 10, EF Core 10 + PostgreSQL, MediatR, FluentValidation, JWT + refresh token rotativo, xUnit.
- **Banco de produção**: PostgreSQL do Supabase (só o banco; autenticação e arquivos ficam na API).

## Pré-requisitos

- [.NET SDK 10.0.202+](https://dotnet.microsoft.com/download) (fixado em `backend/global.json`)
- [Node.js 20+](https://nodejs.org/) e npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (PostgreSQL local e testes de integração)
- Ferramenta do EF Core: `dotnet tool install --global dotnet-ef`

## Estrutura

```
.
├── backend/                         # API .NET 10
│   ├── src/
│   │   ├── LaBlanca.Api/            # Controllers, autenticação, CORS, rate limit, health check, OpenAPI
│   │   ├── LaBlanca.Application/    # Casos de uso (MediatR), validação, DTOs e mapeamento manual
│   │   ├── LaBlanca.Domain/         # Entidades e regras de negócio (sem dependências)
│   │   ├── LaBlanca.Infrastructure/ # EF Core, PostgreSQL, JWT, BCrypt, arquivos, revalidação do site
│   │   ├── LaBlanca.Migrations/     # Migrações do EF Core
│   │   └── LaBlanca.Shared/         # Problem Details (RFC 7807) e mensagens .resx por idioma
│   ├── tools/LaBlanca.Tools/        # CLI de administração (criar admin, redefinir senha)
│   └── tests/
│       ├── LaBlanca.UnitTests/        # Testes de unidade (sem Docker)
│       └── LaBlanca.IntegrationTests/ # Testes de integração (PostgreSQL via Testcontainers, exige Docker)
├── frontend/                        # Next.js
│   ├── messages/                    # Textos por idioma (es, pt, en, gn)
│   ├── scripts/                     # Ícones (sharp) e sincronização do guarani
│   └── src/
│       ├── app/[locale]/(site)/     # Site público
│       ├── app/[locale]/admin/      # Painel administrativo
│       ├── app/{sitemap,robots}.ts, llms.txt/, og/   # SEO/GEO e imagens Open Graph
│       └── components/, lib/, hooks/, i18n/
├── openspec/                        # Especificações e mudanças (OpenSpec)
└── docker-compose.yml               # PostgreSQL local
```

## Rodando localmente

### 1. Banco de dados

```bash
docker compose up -d
```

PostgreSQL em `localhost:5432` (banco `lablanca`, usuário e senha `postgres`, só para desenvolvimento).

### 2. API

```bash
cd backend
dotnet run --project src/LaBlanca.Api --launch-profile http
```

Em Development as migrações rodam na inicialização. Para rodar à parte: `dotnet ef database update -p src/LaBlanca.Migrations -s src/LaBlanca.Api`.

- API: http://localhost:5080
- Health check: `/health` (geral), `/health/live` e `/health/ready` (banco e armazenamento)
- OpenAPI (só em Development): http://localhost:5080/openapi/v1.json

### 3. Site

```bash
cd frontend
cp .env.example .env.local
npm install
npm run dev
```

Acesse http://localhost:3000 (redireciona para `/es`). O painel fica em `/es/admin`; o botão **Ingresar / Login** da barra superior abre o modal de acesso.

### 4. Primeiro administrador

Não existe cadastro pela API nem pelo site. Crie o admin com a CLI (a senha é pedida no terminal, sem eco; mínimo de 10 caracteres com letras e números):

```bash
cd backend
dotnet run --project tools/LaBlanca.Tools -- create-admin --tenant la-blanca --name "Seu Nome" --email admin@exemplo.com
```

Outros comandos: `reset-password --tenant la-blanca --email ...` (redefine a senha e encerra as sessões) e `hash-password`. Fora do ambiente local, informe a conexão com `ConnectionStrings__Default` ou `--connection "<connection string>"`. Para senha via pipe, use `--password-stdin`.

Equivalente em SQL (gere o hash com `hash-password`):

```sql
INSERT INTO lablanca."Users" ("Id", "TenantId", "Name", "Email", "PasswordHash", "Role", "IsActive", "FailedLoginCount", "CreatedAt")
VALUES (gen_random_uuid(), '6f1c2a4e-8b3d-4f5a-9c7e-2d1b0a9e8f71', 'Seu Nome', 'admin@exemplo.com', '<hash>', 'Admin', true, 0, now());
```

Depois de logado, o admin só troca a própria senha em **Minha conta**.

## Variáveis de ambiente

### Site (`frontend/.env.local`)

| Variável | Uso |
|---|---|
| `API_INTERNAL_URL` | URL da API vista pelo servidor do Next (componentes de servidor e rewrite de `/api/*`) |
| `NEXT_PUBLIC_SITE_URL` | URL pública do site, sem barra final (canônicas, hreflang, sitemap, Open Graph). Embutida no build |
| `REVALIDATE_SECRET` | Segredo compartilhado com a API (`Site:RevalidateSecret`) para o endpoint `POST /revalidate` |
| `NEXT_PUBLIC_API_UPLOAD_URL` | Origem pública da API para uploads (até 50 MB). Ver "Uploads grandes" |
| `NEXT_PUBLIC_MAP_TILES_URL`, `NEXT_PUBLIC_MAP_ATTRIBUTION` | Base cartográfica do mapa (padrão OpenStreetMap) |

O navegador fala com a API pela mesma origem do site (`/api/*`, rewrite do Next), então o cookie de refresh é de primeira parte.

### API (`appsettings.json` + variáveis de ambiente em produção)

| Chave | Uso |
|---|---|
| `ConnectionStrings__Default` | Conexão com o PostgreSQL |
| `Jwt__SigningKey` | Chave HMAC do access token (mínimo 32 bytes, aleatória) |
| `Cors__AllowedOrigins__0` | Origem do site (necessária para os uploads diretos) |
| `Site__RevalidateUrl`, `Site__RevalidateSecret` | Endpoint `/revalidate` do site e segredo, para atualizar páginas e sitemap após cada alteração |
| `Storage__RootPath` | Pasta dos arquivos enviados (disco persistente) |

Arquivos `.env*` (exceto `.env.example`) e `appsettings.*.local.json` não são versionados.

## Testes e verificação

| Onde | Comando | O que faz |
|---|---|---|
| `backend/` | `dotnet build` | Compila (avisos e vulnerabilidades do NuGet quebram o build) |
| `backend/` | `dotnet test tests/LaBlanca.UnitTests` | Testes de unidade |
| `backend/` | `dotnet test tests/LaBlanca.IntegrationTests` | Testes de integração (exige Docker; sobe PostgreSQL 17 via Testcontainers) |
| `frontend/` | `npm run test` | Testes do frontend (Vitest) |
| `frontend/` | `npm run lint` | ESLint |
| `frontend/` | `npm run build` | Build de produção |
| `frontend/` | `node scripts/sync-guarani.mjs` | Copia chaves novas do espanhol para `gn.json` e atualiza `gn-pending.md` |

Pare a API antes de `dotnet test` no Windows: o processo em execução trava as DLLs da compilação.

## Uploads grandes

Imagens e vídeos de até 50 MB (documentos até 15 MB) são validados no navegador e de novo na API (extensão, tamanho e conteúdo real do arquivo). No teste da tarefa 14.5, um arquivo de ~45 MB enviado pelo rewrite `/api` do Next estourou o tempo limite do proxy (30 s) e chegou truncado; o mesmo envio direto para a API levou menos de 1 s. Por isso o painel envia arquivos para `NEXT_PUBLIC_API_UPLOAD_URL` (origem pública da API), com o token no cabeçalho `Authorization`. Exigências:

- a API precisa liberar a origem do site em `Cors:AllowedOrigins`;
- a CSP do site libera essa origem em `connect-src` automaticamente no build;
- o proxy reverso da API deve aceitar corpos de ~55 MB.

## SEO e GEO

- Páginas renderizadas no servidor com cache por tags; a API chama `POST /revalidate` após cada alteração (anúncios, zonas, configurações), e o site atualiza página, sitemap e `llms.txt`.
- Rotas traduzidas (`/es/propiedades`, `/pt/imoveis`, `/en/properties`), `hreflang` com os slugs de cada idioma, canônicas absolutas, Open Graph e imagem 1200×630 gerada por anúncio (`/og/property/{locale}/{slug}`).
- JSON-LD: `RealEstateAgent` (todas as páginas), `WebSite` com busca (home), `RealEstateListing` + `Offer` (anúncios), `BreadcrumbList` e `FAQPage`.
- `robots.txt` libera buscadores e robôs de IA (GPTBot, ClaudeBot, PerplexityBot, Google-Extended…) e bloqueia `/*/admin` e `/api/` (exceto as fotos públicas). `llms.txt` resume a imobiliária, zonas, contatos e anúncios publicados.

### Auditoria Lighthouse (mobile, build de produção local, 2 de outubro de 2026)

| Página | Desempenho | Acessibilidade | Boas práticas | SEO | CLS | LCP (simulado) |
|---|---|---|---|---|---|---|
| Home | 87 | 100 | 100 | 100 | 0 | 3,9 s |
| Catálogo | 91 | 100 | 100 | 100 | 0 | 3,5 s |
| Anúncio | 90 | 100 | 100 | 100 | 0 | 3,6 s |

SEO, acessibilidade, boas práticas e CLS atingem as metas. **O LCP simulado ainda está acima da meta de 2,5 s** (medido no navegador local, sem limitação, é 0,4 s): a simulação de rede 4G lenta soma o JavaScript do React/Next e as fontes. Próximos passos se a medição em produção (com CDN e Brotli) confirmar o problema: trocar o Zod do formulário público por validação manual (~30 KB) e reduzir o uso da fonte Fredoka.

**Rich Results Test**: ainda não validado, porque exige uma URL pública. Depois do deploy, testar um anúncio em https://search.google.com/test/rich-results e registrar o resultado aqui.

## Produção com Supabase

O Supabase é usado só como PostgreSQL gerenciado. A autenticação é da própria API e o frontend não usa o cliente do Supabase.

1. **Projeto**: criar na região **South America (São Paulo)**, a mais próxima de Ciudad del Este, e conferir a versão major do PostgreSQL (o projeto e os testes usam 17).
2. **Data API desativada**: em *Project Settings › Data API*, desativar a API REST ou garantir que o schema `lablanca` não esteja exposto. Todas as tabelas ficam em `lablanca` e com RLS ativado sem políticas (verificado por teste de integração), mas a Data API não deve publicá-las.
3. **Conexão**: usar o pooler Supavisor em **modo sessão** (aceita IPv4) ou a conexão direta, se a hospedagem tiver IPv6. Não usar o modo transação. Copiar host e usuário em *Connect › Session pooler*, baixar o certificado CA em *Database Settings › SSL* e montar no formato do Npgsql (exemplo):

   ```
   Host=<host-do-pooler>.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.<ref>;Password=<senha>;SSL Mode=VerifyFull;Root Certificate=/caminho/do/certificado.crt;Maximum Pool Size=15
   ```

   Ajuste `Maximum Pool Size` ao limite de conexões do plano. Em `Production` a API recusa iniciar sem SSL.
4. **Migrações**: nunca na inicialização em produção (`Database:MigrateOnStartup` é recusado em `Production`). Gere e rode o bundle:

   ```bash
   cd backend
   dotnet ef migrations bundle -p src/LaBlanca.Migrations -s src/LaBlanca.Api -o efbundle --self-contained
   ./efbundle --connection "<connection string>"
   ```

5. **Admin**: `dotnet run --project tools/LaBlanca.Tools -- create-admin --tenant la-blanca --name "..." --email ... --connection "<connection string>"`.
6. **Variáveis**: configurar as da API e do site (tabelas acima) e conferir `GET /health/ready`.
7. **Arquivos**: a API grava em disco (`Storage:RootPath`). Se a hospedagem tiver disco efêmero, é preciso um volume persistente ou implementar o armazenamento no Supabase Storage (fora do escopo atual).

## Pendências que dependem da imobiliária

- Dados reais: telefone, WhatsApp, endereço, horário, redes sociais, cotações e taxa do simulador (editáveis em **Configurações** no painel).
- Números institucionais da home e da página Sobre nós (hoje "+12 anos", "+500 famílias", "99%"), em `frontend/messages/*.json` (`home.stat*`).
- Revisão jurídica da política de privacidade e conferência das respostas das perguntas frequentes.
- Tradução para guarani: lista em `frontend/gn-pending.md` (hoje o guarani mostra o texto em espanhol).
- Arquivo vetorial da logo, domínio definitivo, hospedagem e plano do Supabase.
