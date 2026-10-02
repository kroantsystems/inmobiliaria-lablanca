> Convenção TDD: em toda tarefa marcada **(TDD)**, escrever primeiro os testes dos cenários das specs citadas, vê-los falhar, implementar até passar e refatorar. Cada fase termina com `dotnet test`, `npm run lint`, `npm run build` (quando houver frontend) verdes e um commit.

## 1. Preparação do repositório

- [x] 1.1 Adicionar remoto `origin` apontando para `https://github.com/kroantsystems/inmobiliaria-lablanca` e fazer o commit inicial do scaffold existente (confirmar com o usuário antes do primeiro push)
- [x] 1.2 Arquivar `scaffold-initial-project` (`openspec archive scaffold-initial-project`) para criar `openspec/specs/backend-api-foundation` e `openspec/specs/frontend-app-shell`
- [x] 1.3 Preencher `context` em `openspec/config.yaml` com stack, idiomas, regras de licença de pacotes e convenção TDD

## 2. Reestruturação da solução .NET

- [x] 2.1 Criar `backend/Directory.Packages.props` (Central Package Management) e `Directory.Build.props` (nullable, warnings como erro, `LangVersion` 14), fixando MediatR 12.5.0, FluentAssertions 7.2.x, Moq 4.20.72, com auditoria do NuGet ativa
- [x] 2.2 Criar projetos `src/LaBlanca.Shared` (com `FrameworkReference` ASP.NET Core) e `src/LaBlanca.Migrations`, e adicioná-los à `LaBlanca.sln`
- [x] 2.3 Criar `tools/LaBlanca.Tools` (console) e adicioná-lo à solução
- [x] 2.4 Substituir `tests/LaBlanca.Tests` por `tests/LaBlanca.UnitTests` e `tests/LaBlanca.IntegrationTests` (xUnit, Moq, FluentAssertions 7.x, Testcontainers.PostgreSql, Respawn) e mover o teste de health para `IntegrationTests`
- [x] 2.5 Ajustar referências entre projetos conforme o design (Api → Application/Infrastructure/Migrations/Shared; Infrastructure → Application/Domain/Shared; Application → Domain/Shared; Migrations → Infrastructure)
- [x] 2.6 (TDD) Teste de arquitetura em `UnitTests` garantindo que `Domain` e `Shared` não referenciam outros projetos da solução
- [x] 2.7 Configurar `MigrationsAssembly("LaBlanca.Migrations")`, `HasDefaultSchema("lablanca")` e `MigrationsHistoryTable("__EFMigrationsHistory", "lablanca")` no `AppDbContext`, remover a migração `InitialCreate` da `Infrastructure` e atualizar comandos do README
- [x] 2.8 Alinhar a imagem do `docker-compose.yml` e do Testcontainers à versão major do PostgreSQL do Supabase (hoje 17), numa constante única nos testes
- [x] 2.9 Criar `IntegrationTests/Infrastructure/ApiFactory` (WebApplicationFactory + container PostgreSQL compartilhado + Respawn configurado para o schema `lablanca`) e fazer o teste de health passar contra Postgres real
- [x] 2.10 Commit da fase

## 3. Plataforma da API (cross-cutting)

- [x] 3.1 Migrar `Program.cs` de Minimal APIs para Controllers (`AddControllers`, `[ApiController]`), mantendo OpenAPI só em Development
- [x] 3.2 (TDD) Middleware global de exceções em `LaBlanca.Shared` com Problem Details (400 validação, 401, 403, 404, 409, 429, 500 sem detalhes fora de Development, `traceId`) — spec `api-platform`
- [x] 3.3 (TDD) `Messages.resx` (pt neutro), `Messages.es.resx`, `Messages.en.resx`, `Messages.gn.resx` (inicialmente com textos em espanhol); teste de paridade de chaves; `PredefinedCulturesOnly=false` e teste de `CultureInfo("gn")`
- [x] 3.4 (TDD) `RequestLocalization` com `pt` (padrão), `es`, `en`, `gn` lendo `Accept-Language`; teste de integração de mensagem de validação em espanhol e fallback para `fr`
- [x] 3.5 Configurar Serilog (console + arquivo rotativo, enrichers de `traceId`), `UseSerilogRequestLogging` sem logar conteúdo de Commands/Queries (só nome e duração, coberto por teste), de modo que senhas e tokens nunca chegam ao log
- [x] 3.6 (TDD) Marcadores `ICommand`/`IQuery`, `LoggingBehavior`, `ValidationBehavior` e `TransactionBehavior` (rollback em exceção, sem transação em Query)
- [x] 3.7 Registrar MediatR e FluentValidation (assembly scan); mapeamento manual com `ToDto()` por feature, cada um com teste de unidade (TDD) ao ser criado
- [x] 3.8 (TDD) Política de autorização padrão (`FallbackPolicy`), constantes de permissões e políticas por permissão; teste que enumera endpoints e falha se algum fora da lista pública não exigir autenticação
- [x] 3.9 (TDD) Rate limiting por IP (`login` 5/min, `public-forms` 10/h, `analytics` 120/min) com resposta 429 em Problem Details; `ForwardedHeaders` configurável
- [x] 3.10 (TDD) Health checks `/health`, `/health/live` e `/health/ready` (banco + escrita na pasta de armazenamento)
- [x] 3.11 CORS com `AllowCredentials` só para `Cors:AllowedOrigins`; (TDD) teste de origem permitida e negada
- [x] 3.12 (TDD) Validador de inicialização: em `Production` exige `SSL Mode` `Require`/`VerifyCA`/`VerifyFull`, chave JWT ≥ 32 bytes e segredo de revalidação; nunca aplica migrações na inicialização em `Production` (em `Development` aplicar automaticamente é opcional via configuração)
- [x] 3.13 Commit da fase

## 4. Domínio e persistência

- [x] 4.1 (TDD) Entidades base `Entity`/`TenantEntity`, `Tenant`, `User`, `RefreshToken` com regras de bloqueio (5 falhas → 15 min) e revogação
- [x] 4.2 (TDD) `Zone`/`ZoneTranslation`, `Property`/`PropertyTranslation` com regras de status, publicação (exige imagem pública), datas de venda/aluguel e destaque
- [x] 4.3 (TDD) Gerador de slug (sem acentos, minúsculo, sufixo numérico para repetição)
- [x] 4.4 (TDD) `MediaFile` (capa única por anúncio, ordem), `Lead` (status e origens), `Owner`, `Visit` (sobreposição de horário, status), `AnalyticsEvent`, `SiteSettings`
- [x] 4.5 Configurações EF (tipos `numeric(18,2)`, `timestamptz`, `jsonb` para características, enums como texto, índices do design) e `IAppDbContext` na Application
- [x] 4.6 (TDD) `ITenantContext` (claim `tenant_id` ou `Site:TenantId`), filtro global por tenant e interceptor que preenche/bloqueia `TenantId`; teste de integração de isolamento entre dois tenants
- [x] 4.7 (TDD) Teste de integração de segurança do Supabase: todas as tabelas do schema `lablanca` com Row Level Security habilitado e papel `anon` simulado (com `SELECT` concedido) sem acesso a nenhuma linha
- [x] 4.8 Gerar migração `InitialSchema` em `LaBlanca.Migrations` com `ENABLE ROW LEVEL SECURITY` em todas as tabelas, seed do tenant La Blanca, configurações padrão e zonas iniciais (Paraná Country Club, Centro CDE, Km 8 / Km 10, Área 1 / Área 4, Hernandarias); helper de migração que habilita RLS para usar nas próximas
- [x] 4.9 Commit da fase

## 5. Autenticação (spec `admin-auth`)

- [x] 5.1 (TDD) `IPasswordHasher` com BCrypt (work factor 12)
- [x] 5.2 (TDD) `ITokenService`: JWT HS256 de 15 min com `sub`, `jti`, `email`, `name`, `tenant_id`, `role`, `permission`; validação de tamanho mínimo da chave
- [x] 5.3 (TDD) Serviço de refresh token: geração opaca, hash SHA-256, rotação, detecção de reuso com revogação de todos os tokens do usuário
- [x] 5.4 (TDD) `IAccessTokenBlacklist` com `IMemoryCache` e checagem em `JwtBearerEvents.OnTokenValidated`
- [x] 5.5 (TDD) `LoginCommand` + validador + handler (mensagem genérica, usuário inativo, bloqueio, zera contador, `LastLoginAt`)
- [x] 5.6 (TDD) `AuthController`: `login`, `refresh`, `logout`, `change-password`, `me`, com cookie `lb_rt` (`HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`, 7 dias) e ausência de `/register`
- [x] 5.7 (TDD) `ChangePasswordCommand` (senha atual, política de 10+ caracteres com letras e números, diferente da atual, revoga refresh tokens, novo par)
- [x] 5.8 `BackgroundService` diário que remove refresh tokens expirados
- [x] 5.9 (TDD) `LaBlanca.Tools`: `create-admin` (senha sem eco, erro para e-mail repetido), `reset-password` e `hash-password`; documentar uso e SQL equivalente no README
- [x] 5.10 Teste de integração do fluxo completo: login → chamada protegida → refresh → reuso do token antigo → logout → token antigo recusado
- [x] 5.11 Commit da fase

## 6. Configurações e zonas (spec `site-settings`)

- [x] 6.1 (TDD) Queries/Commands de configurações (validação do WhatsApp internacional, cotações > 0, taxa do simulador, meta)
- [x] 6.2 (TDD) CRUD de zonas com traduções e bloqueio de exclusão em uso (409)
- [x] 6.3 (TDD) `AdminSettingsController`, `AdminZonesController` e `PublicSettingsController` (sem meta de vendas na resposta pública)
- [x] 6.4 Commit da fase

## 7. Anúncios (spec `property-listings`)

- [ ] 7.1 (TDD) `CreateProperty`/`UpdateProperty` com validadores (tradução `es` obrigatória, preço > 0, coordenadas, link de vídeo YouTube/Vimeo/MP4 HTTPS, zona existente)
- [ ] 7.2 (TDD) Commands de publicar, despublicar, destacar, mudar status e arquivar
- [ ] 7.3 (TDD) Query administrativa ampla (até 500 itens, todos os status) e detalhe para edição
- [ ] 7.4 (TDD) Busca pública com filtros, ordenação, paginação limitada a 24 e exclusão de rascunhos/arquivados
- [ ] 7.5 (TDD) Detalhe público por `{locale}/{slug}` com fallback para `es`, idioma efetivo e mapa de slugs por idioma; destaques com fallback para mais recentes
- [ ] 7.6 (TDD) `IRevalidationNotifier` com fila (`Channel`) e envio após commit para `POST {Site:RevalidateUrl}` com segredo; falha só gera log
- [ ] 7.7 `AdminPropertiesController` e `PublicPropertiesController` com permissões `properties.read`/`properties.write`
- [ ] 7.8 Commit da fase

## 8. Banco de arquivos (spec `media-library`)

- [ ] 8.1 (TDD) `FileValidationService` com extensões, `Content-Type`, magic bytes e limites configuráveis (imagens 50 MB, vídeos 50 MB, documentos 15 MB)
- [ ] 8.2 (TDD) `IFileStorage` + `LocalDiskFileStorage` (nome GUID, pasta fora do `wwwroot`, proteção contra caminho malicioso)
- [ ] 8.3 (TDD) Upload em stream com `[RequestSizeLimit]`/`[RequestFormLimits]` de 55 MB, vínculo opcional a anúncio, descrição e texto alternativo
- [ ] 8.4 (TDD) Listagem ampla, edição de vínculo/descrição, capa única, reordenação, exclusão (bloqueio 409 da última imagem pública de anúncio publicado)
- [ ] 8.5 (TDD) Rotas públicas e privadas: `/api/public/media/{id}` só para mídia pública de anúncio publicado (cache longo, `nosniff`); `/api/admin/files/{id}/content` com `attachment`
- [ ] 8.6 Teste de integração com upload real de imagem de ~50 MB e de arquivo disfarçado
- [ ] 8.7 Commit da fase

## 9. Leads, proprietários e agenda

- [ ] 9.1 (TDD) `SubmitPublicLead` (origens, consentimento, e-mail obrigatório só na newsletter, telefone ≥ 8 dígitos, honeypot, idioma, resposta 202 sem dados) com rate limit `public-forms`
- [ ] 9.2 (TDD) CRUD administrativo de leads, mudança de status, listagem ampla e conversão de `OwnerProposal` em proprietário — spec `leads`
- [ ] 9.3 (TDD) CRUD de proprietários com imóveis captados e bloqueio de exclusão com anúncio não arquivado — spec `property-owners`
- [ ] 9.4 (TDD) Visitas: criar/editar/cancelar, data futura, conflito de horário (409), atualização do status do lead, intervalo ≤ 62 dias, próximos 10, encerramento — spec `visit-scheduling`
- [ ] 9.5 Controllers `PublicLeadsController`, `AdminLeadsController`, `AdminOwnersController`, `AdminVisitsController` com permissões
- [ ] 9.6 Commit da fase

## 10. Analytics e dashboard (spec `site-analytics`)

- [ ] 10.1 (TDD) `RecordEvent` com tipos permitidos, filtro de robôs pelo user agent sem gravá-lo, sem IP; rate limit `analytics`
- [ ] 10.2 (TDD) Resumo do mês no fuso `America/Asuncion` com variação (nula quando o mês anterior é zero), incluindo vendas vs meta, aluguéis ativos e leads novos
- [ ] 10.3 (TDD) Tráfego diário (7 ou 30 dias, dias vazios com zero, 400 para outros valores) e top 5 anúncios dos últimos 30 dias
- [ ] 10.4 (TDD) Tarefa diária de retenção que apaga eventos com mais de 13 meses
- [ ] 10.5 `PublicAnalyticsController` e `AdminDashboardController` (`dashboard.read`)
- [ ] 10.6 Commit da fase

## 11. Fundação do frontend

- [ ] 11.1 Ler `node_modules/next/dist/docs` (upgrade para 16, `proxy`, metadata, `revalidateTag`, `next/image`) antes de codar; instalar next-intl compatível com Next 16, axios, @tanstack/react-query, react-hook-form, zod, @hookform/resolvers, lucide-react, date-fns, recharts, leaflet, react-leaflet, e Vitest + Testing Library + jsdom
- [ ] 11.2 Configurar Vitest (`npm run test`) e incluí-lo no checklist do README
- [ ] 11.3 Tema Tailwind 4 com tokens do protótipo e fontes Fredoka + Plus Jakarta Sans via `next/font`; remover Inter/Playfair e componentes placeholder do scaffold
- [ ] 11.4 next-intl: `src/i18n/routing.ts` (es padrão, pt, en, gn, `pathnames` traduzidos), `request.ts`, `navigation.ts`, `proxy.ts` com matcher que exclui `api`, `_next`, `revalidate`, arquivos de SEO e estáticos
- [ ] 11.5 (TDD) Teste de paridade de chaves entre `messages/es.json`, `pt.json`, `en.json`, `gn.json`; criar `gn-pending.md`
- [ ] 11.6 (TDD) Utilitários de formatação (mapa `gn→es-PY`, moedas USD/PYG/BRL, datas no fuso de Assunção)
- [ ] 11.7 `next.config.ts`: `rewrites` de `/api/:path*` para `API_INTERNAL_URL`, `images.remotePatterns` para a mídia pública, cabeçalhos de segurança; atualizar `.env.example` (`API_INTERNAL_URL`, `NEXT_PUBLIC_SITE_URL`, `REVALIDATE_SECRET`, `NEXT_PUBLIC_MAP_TILES_URL`)
- [ ] 11.8 Cliente de servidor (`lib/api/server.ts`) com `fetch`, tags de cache e `Accept-Language`
- [ ] 11.9 (TDD) Cliente Axios com interceptor de `Authorization` + `Accept-Language` e refresh single-flight em 401 com repetição das requisições pendentes
- [ ] 11.10 `AuthProvider` (token só em memória), hooks de sessão e `QueryClientProvider`
- [ ] 11.11 (TDD) `useDebounce` (300 ms) e `useClientTable` (busca, filtros e ordenação com `useMemo`)
- [ ] 11.12 (TDD) Validação de uploads no cliente com as mesmas regras da API
- [ ] 11.13 Route handler `POST /revalidate` com verificação de segredo e `revalidateTag(tag, { expire: 0 })`
- [ ] 11.14 Layouts `[locale]/(site)` e `[locale]/admin` com `lang` dinâmico — spec `frontend-app-shell`
- [ ] 11.15 Marca: copiar `logo-transparent*.{png,webp}` e `brand-mark.svg` de `openspec/changes/add-public-site-and-admin-panel/assets/` para `frontend/public/brand/`; usar `favicon.svg` como `src/app/icon.svg`; gerar com `sharp` (script em `frontend/scripts/`) `favicon.ico` (16/32/48), `apple-icon.png` 180 e ícones 192/512 + maskable com fundo `#005DAA`; criar `manifest.ts`; remover o `favicon.ico` do scaffold
- [ ] 11.16 Componentes `Logo` (variantes fundo claro/escuro, `next/image` com dimensões fixas e alt "Inmobiliaria La Blanca – Ciudad del Este") e `BrandMark` (SVG inline)
- [ ] 11.17 Commit da fase

## 12. Site público (spec `public-website`)

- [ ] 12.1 Barra superior (Login, Publicar meu imóvel, seletor de idioma que preserva página/slug, seletor de moeda lembrado no navegador) e cabeçalho com menu responsivo
- [ ] 12.2 Modal de login acessível (foco, `Esc`, trap de `Tab`), erro genérico, limpeza da senha e redirecionamento para `/{locale}/admin`
- [ ] 12.3 Contexto de moeda com conversão pelas cotações públicas e aviso de conversão aproximada com data
- [ ] 12.4 Home: hero com destaque, caixa de busca (abas Comprar/Alugar e filtros), grade de destaques, seção institucional azul, FAQ, rodapé com zonas e newsletter, estado sem anúncios
- [ ] 12.5 Catálogo `/{locale}/{imoveis}` renderizado no servidor com filtros na URL, ordenação, paginação por links e estado vazio
- [ ] 12.6 Página de detalhe: galeria com `next/image`, preço convertível, características, descrição, vídeo incorporado sob demanda, mapa, WhatsApp com mensagem do anúncio, formulário de visita, semelhantes e 404 com sugestões
- [ ] 12.7 Mapa com react-leaflet carregado sob demanda (só imóveis publicados, popup com foto/preço/link, tiles configuráveis)
- [ ] 12.8 (TDD) Simulador pela tabela Price (USD 100.000, 10 anos, 8% → ≈ USD 1.213) com "—" para valor inválido
- [ ] 12.9 Formulários públicos (contato VIP, visita, proposta de proprietário, newsletter) com zod, consentimento com link para privacidade, honeypot, confirmação e manutenção dos dados em erro
- [ ] 12.10 WhatsApp flutuante com número das configurações, mensagem por idioma, `rel="noopener"` e evento `WhatsAppClick`
- [ ] 12.11 Envio de eventos `PageView`/`PropertyView`/`ContactClick` com `sessionId` em `sessionStorage`
- [ ] 12.12 Páginas Sobre nós, Contato, Perguntas frequentes e Política de privacidade em todos os idiomas
- [ ] 12.13 Revisão responsiva (360 px a desktop) e de acessibilidade (contraste AA, alt, teclado)
- [ ] 12.14 Commit da fase

## 13. SEO e GEO (spec `seo-geo`)

- [ ] 13.1 Helpers de metadata (title ≤ 60, description ≤ 160, canônica absoluta, Open Graph, Twitter, `og:locale`) e `generateMetadata` em todas as páginas públicas
- [ ] 13.2 `alternates.languages` com `es-PY`, `pt-BR`, `en`, `gn-PY` e `x-default`, usando os slugs traduzidos de cada anúncio
- [ ] 13.3 (TDD) Geradores de JSON-LD (`RealEstateAgent`, `WebSite`+`SearchAction`, `RealEstateListing`+`Offer`, `BreadcrumbList`, `FAQPage`) e inclusão nas páginas
- [ ] 13.4 `sitemap.ts` dinâmico (páginas × idiomas, `alternates`, `lastModified`) e `robots.ts` (bloqueia `/*/admin` e `/api/`, libera robôs de busca e de IA, aponta o sitemap)
- [ ] 13.5 `llms.txt` gerado com resumo institucional, zonas, contatos e anúncios publicados, com tag de revalidação
- [ ] 13.6 Páginas de zona `/{locale}/zonas/{zona}` com descrição, anúncios, mapa, FAQ e breadcrumbs
- [ ] 13.7 Resumo factual no início das páginas de anúncio e zona
- [ ] 13.8 `opengraph-image.tsx` por anúncio (1200×630, capa, título, preço, logo)
- [ ] 13.9 Tags de cache por anúncio, listagem, zona, configurações e sitemap ligadas às notificações da API
- [ ] 13.10 Auditoria Lighthouse móvel no build de produção (home, catálogo, anúncio): SEO ≥ 95, desempenho ≥ 90, CLS ≤ 0,1, LCP ≤ 2,5 s; corrigir o que faltar
- [ ] 13.11 Validar JSON-LD de um anúncio no Rich Results Test e registrar o resultado no README
- [ ] 13.12 Commit da fase

## 14. Painel administrativo (spec `admin-panel`)

- [ ] 14.1 Layout do painel (sidebar escura com ícones abaixo de 1024 px, topo com título por seção, idioma e menu do usuário), `noindex` e guarda de sessão (refresh único, senão volta à home com modal de login)
- [ ] 14.2 Dashboard: cartões de KPIs com variação, gráfico Recharts 7/30 dias, ranking de anúncios e próximos compromissos
- [ ] 14.3 Componente de tabela reutilizável sobre `useClientTable` (ordenar por coluna, filtros, busca com debounce)
- [ ] 14.4 Anúncios & Mídia: lista, formulário com abas de idioma, campos do imóvel, vídeo, galeria (upload múltiplo com progresso, pré-visualização, arrastar para ordenar, capa, texto alternativo) e ações de publicar/despublicar/destacar/status/arquivar com link "Ver no site"
- [ ] 14.5 Teste manual de upload de imagem de ~50 MB pelo rewrite; se falhar, ativar envio direto para a origem da API (`NEXT_PUBLIC_API_UPLOAD_URL`)
- [ ] 14.6 Clientes / Leads: formulário, tabela, painel lateral de edição, mudança rápida de status, link de WhatsApp
- [ ] 14.7 Proprietários: formulário, tabela com imóveis captados e conversão de propostas
- [ ] 14.8 Agenda: formulário de visita, próximos compromissos e calendário mensal (navegação, dia atual, etiquetas por status, detalhe ao clicar, atualização sem recarregar)
- [ ] 14.9 Imagens e Documentos: upload com vínculo e descrição, grade com pré-visualização/ícone por tipo, etiqueta de vínculo, download e exclusão com confirmação
- [ ] 14.10 Configurações (dados institucionais, cotações, simulador, meta, zonas) e Minha conta (troca de senha com confirmação no cliente)
- [ ] 14.11 Tradução de todos os textos do painel nos quatro arquivos de mensagens
- [ ] 14.12 Commit da fase

## 15. Fechamento

- [ ] 15.1 Atualizar `README.md` (novos projetos, variáveis de ambiente, criação de admin, comandos de teste de unidade/integração/frontend, requisito de Docker para integração)
- [ ] 15.2 Seção de produção no README: Supabase (região, Data API desativada, pooler em modo sessão, `SSL Mode=VerifyFull` com certificado CA, `Maximum Pool Size`, geração e execução de `dotnet ef migrations bundle`, `create-admin`)
- [ ] 15.3 Rodar verificação completa: `dotnet build`, `dotnet test`, `npm run lint`, `npm run test`, `npm run build`
- [ ] 15.4 Passar pelo fluxo real no navegador: visitante busca, abre anúncio, envia visita; admin faz login pelo modal, vê o lead no painel, agenda a visita, publica anúncio novo e confere a página e o sitemap atualizados; conferir logo e favicon em tema claro e escuro
- [ ] 15.5 Registrar no `design.md` as respostas obtidas para as perguntas em aberto (vetor da marca, domínio, hospedagem, plano/região do Supabase, dados reais, tradutor de guarani)
- [ ] 15.6 Commit final e push para o GitHub (com confirmação do usuário)
