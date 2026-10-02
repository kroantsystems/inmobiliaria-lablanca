## Context

Projeto novo do site da imobiliária La Blanca. A pasta contém só a configuração do OpenSpec, sem git e sem código. A stack definida é Next.js no frontend e .NET 10 no backend. Esta mudança cria apenas a fundação. Funcionalidades de negócio (imóveis, busca, leads, painel administrativo) virão em mudanças próprias.

## Goals / Non-Goals

**Goals:**
- Estrutura de monorepo clara, com frontend e backend independentes.
- Rodar localmente com poucos comandos (Docker para o banco, `dotnet run` e `npm run dev`).
- Arquitetura do backend preparada para crescer sem acoplar domínio à infraestrutura.
- Base de testes e checagens (build, lint, testes) funcionando desde o início.

**Non-Goals:**
- Entidades de domínio (imóvel, corretor, lead) e endpoints de negócio.
- Autenticação e painel administrativo.
- Identidade visual final, SEO avançado, deploy e CI/CD.
- Containerizar frontend e API (apenas o banco roda em Docker por ora).

## Decisions

**Monorepo com `frontend/` e `backend/`.** Um repositório facilita mudanças que tocam as duas pontas (contrato da API e consumo). Alternativa: dois repositórios, que adiciona atrito sem ganho para um time pequeno.

**Next.js com App Router, TypeScript e Tailwind, criado via `create-next-app`.** App Router permite renderização no servidor, importante para SEO de páginas de imóveis. Tailwind acelera o layout. Alternativa: Pages Router, que é legado.

**.NET 10 com Minimal APIs e camadas Api / Application / Domain / Infrastructure.** Minimal APIs mantêm a API enxuta. A separação em camadas isola regras de negócio do EF Core. Alternativa: um único projeto, mais simples hoje mas difícil de organizar quando entrarem imóveis, filtros e integrações.

**PostgreSQL com EF Core (Npgsql).** Gratuito, robusto e bem suportado pelo EF Core, com bom suporte a busca textual e geolocalização (PostGIS) se precisarmos no futuro. Alternativa: SQL Server, que tem custo de licença/hospedagem maior.

**OpenAPI nativo do ASP.NET Core (`Microsoft.AspNetCore.OpenApi`).** É o padrão desde o .NET 9, sem dependência do Swashbuckle. Uma UI (ex.: Scalar) pode ser adicionada depois.

**Health checks nativos com checagem do DbContext.** Usa `AddHealthChecks().AddDbContextCheck<AppDbContext>()`, sem biblioteca extra.

**Testes com xUnit e `WebApplicationFactory`.** Teste de integração do `/health` valida a montagem da API. Para não depender do Docker nos testes iniciais, o teste substitui a checagem de banco ou usa banco em memória.

**Configuração.** Backend: `appsettings.Development.json` com connection string apontando para o Postgres do docker-compose (credenciais só de desenvolvimento). Frontend: `NEXT_PUBLIC_API_URL` em `.env.local`, com `.env.example` versionado.

## Risks / Trade-offs

- [.NET 10 exige SDK recente na máquina] → README indica a versão mínima e inclui `global.json` fixando o SDK.
- [Camadas podem ser excesso para o tamanho atual] → Camadas começam vazias e só ganham código quando houver necessidade real.
- [Credenciais de desenvolvimento versionadas no appsettings] → Usar apenas valores locais óbvios (`postgres`/`postgres`). Produção usará variáveis de ambiente ou secrets.
- [Versões de Next.js/Tailwind mudam rápido] → Fixar versões no `package.json` gerado e registrar no README.

## Migration Plan

Não se aplica: projeto novo, sem dados nem sistemas existentes.

## Open Questions

- Onde será feita a hospedagem (Vercel + Azure? VPS única?). Isso afeta a futura containerização e o CI/CD.
- Haverá painel administrativo para cadastrar imóveis ou integração com algum CRM imobiliário (ex.: Vista, Jetimob)?
