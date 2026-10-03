## Context

O scaffold (`scaffold-initial-project`) entregou um monorepo com `frontend/` (Next.js 16.3, React 19.2, Tailwind 4) e `backend/` (.NET 10 com Minimal APIs, camadas Api/Application/Domain/Infrastructure, EF Core + Npgsql, health check e um projeto `LaBlanca.Tests`). Não há regras de negócio, autenticação nem identidade visual. O repositório git não tem commits e ainda não aponta para `https://github.com/kroantsystems/inmobiliaria-lablanca`.

Os protótipos do Gemini definem o visual e os módulos:
- `proto 19.html`: site público em espanhol (Ciudad del Este), barra superior com idioma/moeda/"Acceso ADM"/"Publicar mi Inmueble", hero com destaque, busca, cartões, mapa, seção institucional azul, simulador, rodapé com newsletter, WhatsApp flutuante e modais.
- `dashboard.html`: painel com sidebar escura, KPIs, gráfico de barras, ranking, cadastros de anúncios, clientes e proprietários.
- `calendario e banco de imagens.html`: módulos Agenda (calendário mensal estilo Google Calendar) e Banco de Imagens e Documentos com vínculo a anúncios.

A marca oficial foi entregue depois dos protótipos: logo (bloco vermelho "INMOBILIARIA", bloco azul "La Blanca", "CIUDAD DEL ESTE") em foto de 720×421 com fundo cinza e linhas decorativas, e ícone (prédio branco com janelas e casa vermelha) de 50×58 px sobre fundo preto. Os originais e as versões tratadas estão em `openspec/changes/add-public-site-and-admin-panel/assets/`. O banco de produção será o PostgreSQL do Supabase.

Restrições do cliente: TDD no backend, JWT access + refresh com regras específicas, admin criado direto no banco, só admins acessam o sistema, quatro idiomas (foco espanhol do Paraguai e português), máximo de SEO e GEO, pacotes sem custo de licença (FluentAssertions 7.x). O desenvolvedor (kroantsystems) atende várias imobiliárias, o que justifica o `TenantId` pedido nos tokens.

## Goals / Non-Goals

**Goals:**
- Site público rápido, indexável e citável por IA, nos quatro idiomas, gerando leads.
- Painel administrativo completo para o dono da imobiliária, fiel aos protótipos.
- Backend seguro por padrão (nada aberto sem `[AllowAnonymous]` explícito) e coberto por testes escritos antes do código.
- Base multi-tenant pronta para reutilizar o sistema com outras imobiliárias sem reescrever o modelo.

**Non-Goals:**
- Tela de gestão de usuários, cadastro público ou recuperação de senha por e-mail (admin é criado pela ferramenta de linha de comando; reset é feito por ela).
- Envio de e-mail, SMS ou WhatsApp automático (notificação de novos leads fica para outra mudança).
- Cotação automática de moedas, tradução automática de anúncios, integração com portais/CRMs, cobrança de aluguéis.
- Deploy, CI/CD e containerização de API e frontend (dependem da decisão de hospedagem).
- Resolução de tenant por domínio no site público (um deploy do site atende um tenant configurado).
- Testes end-to-end em navegador (ficam para depois; nesta mudança há testes de unidade no frontend e auditoria Lighthouse manual).

## Decisions

### 1. Uma mudança, implementada em fases
Todo o produto fica em uma mudança porque os módulos compartilham modelo de dados, autenticação e layout. O `tasks.md` é ordenado em fases entregáveis (fundação → autenticação → domínio → APIs → frontend base → site → SEO/GEO → painel), e cada fase termina com build e testes verdes e um commit. Alternativa: várias mudanças menores; seria melhor para revisão, mas exigiria repetir contexto e coordenar dependências entre elas. Se preferir, a fase de painel pode ser separada sem alterar as specs.

### 2. Estrutura da solução .NET
```
backend/
  src/
    LaBlanca.Domain/          Entidades, enums, regras puras, exceções de domínio. Sem dependências.
    LaBlanca.Shared/          Middleware Problem Details, mapeamento exceção → status, Messages*.resx, constantes de cultura.
    LaBlanca.Application/     Features/<Modulo>/{Commands,Queries,Dtos,Validators,Mapping (ToDto manual)}, Behaviors, Abstractions (IAppDbContext, ITenantContext, IFileStorage, IClock, ITokenService, IPasswordHasher, IRevalidationNotifier).
    LaBlanca.Infrastructure/  AppDbContext + configurações EF, autenticação (JWT, BCrypt, refresh tokens, blacklist), armazenamento em disco, notificador de revalidação, tarefas em segundo plano.
    LaBlanca.Migrations/      Migrações do EF (MigrationsAssembly).
    LaBlanca.Api/             Controllers, Program, políticas de autorização, rate limiting, Serilog, health checks.
  tools/LaBlanca.Tools/       CLI: create-admin, reset-password.
  tests/
    LaBlanca.UnitTests/       Domain/, Application/ (handlers, validators, behaviors), Infrastructure/ (token, hash, validação de arquivo), Shared/ (resx).
    LaBlanca.IntegrationTests/ Api/ por módulo, com WebApplicationFactory + Testcontainers.
```
Dependências: Api → Application, Infrastructure, Migrations, Shared; Infrastructure → Application, Domain, Shared; Application → Domain, Shared; Migrations → Infrastructure. A migração `InitialCreate` existente é removida e recriada em `LaBlanca.Migrations` (não há banco em produção). `LaBlanca.Tests` é substituído pelos dois projetos de teste; o teste de health vai para `IntegrationTests`.

### 3. Controllers + MediatR + FluentValidation + mapeamento manual
O scaffold usava Minimal APIs; passamos a Controllers (`[ApiController]`) porque o cliente pediu `[Authorize]` nos endpoints e Controllers dão atributos por ação, filtros e agrupamento por módulo de forma direta. Controllers são finos: montam o Command/Query, chamam `ISender.Send` e devolvem o DTO.

Versões de pacote (licença):
- **MediatR 12.5.0** (última Apache 2.0; 13+ exige licença comercial).
- **FluentAssertions 7.2.x** (pedido do cliente; 8+ comercial).
- **Moq 4.20.72** (versão sem SponsorLink).
Versões ficam fixadas com `Directory.Packages.props` (Central Package Management) para que nenhum `dotnet add` suba a major por acidente.

**AutoMapper removido (decisão do cliente na implementação).** A última versão MIT (14.0.0) tem a vulnerabilidade GHSA-rvv3-g6hj-g44x (CVSS 7.5, DoS por recursão sem limite), corrigida só nas versões comerciais 15.1.1+/16.1.1+. O mapeamento passa a ser manual: métodos de extensão `ToDto()` por feature em `Application/Features/<Modulo>/Mapping` e projeções `Select(...)` nas Queries (o EF gera SQL só com as colunas usadas). Alternativas descartadas: suprimir o alerta, licença Community do AutoMapper 16 e Mapster.

### 4. Pipeline do MediatR
Ordem: `LoggingBehavior` → `ValidationBehavior` (executa todos os `IValidator<T>` e lança `ValidationException` com erros por campo) → `TransactionBehavior` (só para requests que implementam `ICommand`/`ICommand<T>`; abre transação, chama `SaveChangesAsync` e faz commit; rollback em exceção). Queries usam `AsNoTracking`. Marcadores `ICommand` e `IQuery` em `Application/Abstractions/Messaging`.

### 5. Autenticação e autorização
- **Hash de senha**: BCrypt.Net-Next com work factor 12, atrás de `IPasswordHasher` (permite simular nos testes).
- **Access token**: JWT HS256, chave de no mínimo 32 bytes em `Jwt:SigningKey` (variável de ambiente fora de Development), `Issuer` e `Audience` validados, `ClockSkew` de 30 s, 15 min de validade, claims `sub`, `jti`, `email`, `name`, `tenant_id`, `role` e `permission` (uma por permissão). Permissões são constantes em `Application/Authorization/Permissions.cs`; o papel `Admin` recebe todas.
- **Autorização**: `FallbackPolicy` exige usuário autenticado; uma política por permissão (`RequireClaim("permission", ...)`), aplicada com `[Authorize(Policy = Permissions.X)]` nos Controllers administrativos. Endpoints públicos ficam em Controllers `Public*` marcados `[AllowAnonymous]`. Um teste de integração percorre `EndpointDataSource` e falha se algum endpoint fora da lista pública não exigir autenticação.
- **Refresh token**: 64 bytes aleatórios (`RandomNumberGenerator`) em Base64Url; no banco só o SHA-256. Tabela `RefreshTokens` com `UserId`, `TokenHash`, `ExpiresAt`, `CreatedAt`, `RevokedAt`, `ReplacedByTokenHash`. Rotação marca o antigo como revogado e substituído; reuso de token revogado revoga todos os tokens ativos do usuário. Logout apaga o registro (como pedido). `BackgroundService` diário remove tokens expirados.
- **Cookie**: `lb_rt`, `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth`, 7 dias. Em desenvolvimento `localhost` é contexto seguro, então `Secure` funciona sem HTTPS no navegador.
- **Blacklist**: `IAccessTokenBlacklist` com `IMemoryCache`, chave `jti`, expiração absoluta igual ao `exp` do token; checada em `JwtBearerEvents.OnTokenValidated`. Troca de senha também revoga todos os refresh tokens e coloca o `jti` atual na blacklist.
- **Bloqueio e limite**: `FailedLoginCount` e `LockoutUntil` no usuário (5 falhas → 15 min); rate limiter nativo do ASP.NET Core com política `login` (5/min por IP), `public-forms` (10/h por IP) e `analytics` (120/min por IP). Atrás de proxy reverso, `ForwardedHeaders` é configurado com proxies conhecidos para o IP ser o do visitante.

### 6. Next.js como camada de servidor (BFF leve)
- **Mesma origem**: `next.config.ts` faz `rewrites` de `/api/:path*` para `API_INTERNAL_URL`. O cookie de refresh fica first-party com `SameSite=Strict`, sem depender de cookies de terceiros. Componentes de servidor chamam a API direto por `API_INTERNAL_URL` com `fetch` e tags de cache.
- **`proxy.ts`** (antigo `middleware`, renomeado no Next 16): usado só para detecção e redirecionamento de idioma do next-intl. O `matcher` exclui `api`, `_next`, `revalidate`, `llms.txt`, `sitemap.xml`, `robots.txt` e arquivos estáticos, porque o proxy bufferiza o corpo em até 10 MB (`proxyClientMaxBodySize`) e quebraria uploads de 50 MB.
- **Uploads**: passam pelo rewrite de `/api`. Se o teste de upload de 50 MB pelo rewrite falhar ou ficar lento, o painel envia uploads direto para a origem da API (`NEXT_PUBLIC_API_UPLOAD_URL`), o que funciona porque uploads usam `Authorization: Bearer` e não o cookie; o CORS já aceita a origem do site.
- **Revalidação**: route handler `POST /revalidate` com segredo em cabeçalho (`X-Revalidate-Secret`) chama `revalidateTag(tag, { expire: 0 })` para cada tag recebida (forma indicada pela documentação do Next 16 para chamadas vindas de fora de Server Actions). A API publica as tags por `IRevalidationNotifier`, que enfileira num `Channel` e envia depois do commit em um `BackgroundService` com nova tentativa simples; falha não desfaz a operação do admin.
- **Outros usos**: `sitemap.ts`, `robots.ts`, `llms.txt/route.ts` e `opengraph-image.tsx` por anúncio.

Alternativa considerada: chamar a API direto do navegador com CORS e `SameSite=None`. Descartada porque navegadores estão bloqueando cookies de terceiros e porque a API ficaria exposta em outro domínio para o login.

### 7. Multi-tenant
Toda entidade de negócio herda `TenantEntity` (`TenantId`). `AppDbContext` aplica filtro global `e.TenantId == _tenant.TenantId`. `ITenantContext` resolve o tenant pela claim `tenant_id` nas rotas autenticadas e por `Site:TenantId` (configuração, Guid fixo do seed) nas rotas públicas. Na gravação, um interceptor do `SaveChanges` preenche `TenantId` e bloqueia alteração de tenant. Índices únicos incluem `TenantId` (e-mail do usuário, slug, zona). O tenant La Blanca é criado na migração de dados inicial (seed).

### 8. Modelo de dados
| Entidade | Campos principais |
|---|---|
| `Tenant` | Id, Name, Slug |
| `User` | Id, TenantId, Name, Email, PasswordHash, Role, IsActive, FailedLoginCount, LockoutUntil, LastLoginAt |
| `RefreshToken` | Id, UserId, TokenHash, ExpiresAt, CreatedAt, RevokedAt, ReplacedByTokenHash |
| `Zone` + `ZoneTranslation` | Slug, City, SortOrder / Locale, Name, Description |
| `Property` | Code, Operation, Type, Status, IsPublished, IsFeatured, Price, Currency, ZoneId, City, Address, Latitude, Longitude, Bedrooms, Bathrooms, BuiltAreaM2, LotAreaM2, ParkingSpaces, Features (jsonb), VideoUrl, OwnerId, PublishedAt, SoldAt, RentedAt, CreatedAt, UpdatedAt |
| `PropertyTranslation` | PropertyId, Locale, Title, Slug, Description, SeoTitle, SeoDescription |
| `MediaFile` | PropertyId?, OriginalName, StorageKey, ContentType, Kind, SizeBytes, Description, AltText, IsPublic, IsCover, SortOrder, UploadedAt, UploadedBy |
| `Lead` | Name, Phone, Email, Interest, Source, Status, PropertyId?, Message, Locale, ConsentAt, CreatedAt, UpdatedAt |
| `Owner` | Name, Phone, Email, Document, Notes |
| `Visit` | PropertyId, LeadId?, ClientName, StartsAt (UTC), DurationMinutes, Notes, Status |
| `AnalyticsEvent` | Type, PropertyId?, Path, Locale, SessionId, ReferrerHost, OccurredAt |
| `SiteSettings` | TenantId (PK), contatos, WhatsApp, redes, PygPerUsd, BrlPerUsd, RatesUpdatedAt, SimulatorAnnualRate, MonthlySalesGoal, OfficeLat/Lng, OpeningHours |

Preços em `numeric(18,2)`; datas em `timestamptz` (UTC). Fuso de exibição `America/Asuncion` (o Paraguai adotou UTC−3 fixo em 2024; o servidor precisa de tzdata atualizado). Índices: `(TenantId, IsPublished, Status, Operation, Type, ZoneId, Price)` para a busca, `(TenantId, Locale, Slug)` único, `(TenantId, OccurredAt, Type)` em eventos.

### 9. Internacionalização
- **Frontend**: next-intl (versão compatível com Next 16 e `proxy.ts`), `localePrefix: "always"`, idiomas `es` (padrão), `pt`, `en`, `gn`, `pathnames` traduzidos (`/propiedades`, `/imoveis`, `/properties`; `gn` usa os caminhos em espanhol). Mensagens em `frontend/messages/*.json`; um teste Vitest garante chaves iguais. `gn.json` começa com textos em espanhol e uma lista `gn-pending.md` controla o que falta traduzir por falante nativo.
- **Formatação**: mapa `es→es-PY`, `pt→pt-BR`, `en→en-US`, `gn→es-PY`, porque o suporte a `gn` em `Intl` varia entre navegadores.
- **Backend**: `RequestLocalizationOptions` com culturas `pt` (padrão), `es`, `en`, `gn`. `Messages.resx` é o português neutro (como no exemplo do cliente). O .NET usa ICU e `gn` pode não existir como cultura predefinida, então o `LaBlanca.Api.csproj` define `PredefinedCulturesOnly=false`; um teste de unidade garante que `CultureInfo("gn")` funciona. `Messages.gn.resx` começa com os textos em espanhol (mesma estratégia do frontend), e um teste garante que todos os `.resx` têm as mesmas chaves. FluentValidation usa `WithMessage` com as chaves do `.resx`.

### 10. Arquivos
- `IFileStorage` com implementação `LocalDiskFileStorage` (`Storage:RootPath`, fora do `wwwroot`), chaves `yyyy/MM/<guid><ext>`. Trocar por S3/Azure Blob depois exige só nova implementação.
- `FileValidationService` (testado por unidade) confere extensão, `Content-Type` e magic bytes: PNG `89 50 4E 47`, JPEG `FF D8 FF`, WEBP `RIFF....WEBP`, PDF `%PDF`, DOC `D0 CF 11 E0`, DOCX `PK 03 04` contendo `word/`, MP4/MOV `ftyp` no byte 4, WEBM `1A 45 DF A3`. SVG e HTML não são aceitos (risco de XSS).
- Limites em `Uploads:*` no `appsettings`; endpoint de upload com `[RequestSizeLimit]` e `[RequestFormLimits]` de 55 MB; upload lido em stream, sem carregar o arquivo inteiro em memória.
- Público: `GET /api/public/media/{id}` com `Cache-Control: public, max-age=31536000, immutable` e `nosniff`. O site usa `next/image` com `remotePatterns` apontando para essa rota; o otimizador do Next gera AVIF/WebP redimensionados (sem custo de licença de biblioteca de imagem no backend).
- Privado: `GET /api/admin/files/{id}/content` com `Content-Disposition: attachment`.

### 11. Frontend
```
frontend/
  messages/{es,pt,en,gn}.json
  src/
    proxy.ts
    i18n/{routing,request,navigation}.ts
    app/
      [locale]/
        (site)/ layout, page (home), [catalog]/..., zonas/[zone], nosotros, contacto, preguntas-frecuentes, privacidad
        admin/ layout (guarda de sessão), page (dashboard), agenda, archivos, anuncios, clientes, propietarios, configuracion, cuenta
      revalidate/route.ts, llms.txt/route.ts, sitemap.ts, robots.ts
    components/{ui,site,admin}
    features/<modulo>/ (componentes, hooks e chamadas de API por módulo)
    hooks/useDebounce.ts, useClientTable.ts (filtro/ordenação com useMemo)
    lib/api/{http.ts (Axios + interceptors), server.ts (fetch de servidor)}, lib/auth/, lib/seo/ (metadata e JSON-LD), lib/format/, lib/uploads/validation.ts
```
- **Estado de servidor no painel**: TanStack Query (cache e invalidação após mutações). **Formulários**: react-hook-form + zod, espelhando as regras do FluentValidation. **Gráfico**: Recharts (carregado só no painel). **Mapa**: react-leaflet com OpenStreetMap por padrão e URL de tiles configurável, carregado com `dynamic(..., { ssr: false })` quando a seção entra na tela. **Ícones**: lucide-react (WhatsApp como SVG próprio). **Datas**: date-fns.
- **Tema**: tokens do protótipo no `@theme` do Tailwind 4 (`--color-lb-red`, `--color-lb-blue`, etc.), fontes Fredoka e Plus Jakarta Sans via `next/font/google` (servidas localmente no build). Inter e Playfair do scaffold saem.
- **Sessão**: `AuthProvider` guarda usuário e access token em memória; o interceptor de requisição adiciona `Authorization` e `Accept-Language`; o de resposta, em 401, usa uma única promessa de refresh compartilhada (single-flight) e repete as requisições pendentes.
- **Listas do painel**: `useClientTable` recebe itens, termo (já com debounce de 300 ms), filtros e ordenação e devolve o resultado com `useMemo`. Busca pública não usa esse padrão: filtros vão na URL e o servidor responde (SEO).

### 12. SEO e GEO
- `generateMetadata` em todas as páginas, com helpers em `lib/seo` para canônica, `alternates.languages` (hreflang com slugs traduzidos vindos da API), Open Graph e Twitter.
- Geradores de JSON-LD puros e testados (`RealEstateAgent`, `WebSite`+`SearchAction`, `RealEstateListing`+`Offer`, `BreadcrumbList`, `FAQPage`).
- `sitemap.ts` com todas as páginas × idiomas e `alternates`; `robots.ts` bloqueando `/*/admin` e `/api/`, liberando robôs de busca e de IA.
- `llms.txt` gerado com resumo institucional, zonas e anúncios publicados.
- Páginas de anúncio e zona começam com um parágrafo factual gerado a partir dos dados (útil para buscadores e para respostas de IA) e têm FAQ em texto.
- Cabeçalhos de segurança no `next.config.ts` (`Strict-Transport-Security`, `X-Content-Type-Options`, `Referrer-Policy`, `X-Frame-Options: DENY`, `Permissions-Policy`, CSP básica) sem prejudicar o SEO.

### 13. Analytics próprio
Eventos gravados em tabela própria em vez de Google Analytics: não exige banner de cookies, não envia dados a terceiros e alimenta o dashboard direto. O `sessionId` é um UUID em `sessionStorage` (sem cookie). Robôs são filtrados pelo user agent no servidor, e o user agent não é gravado. Agregações por SQL com índice em `(TenantId, OccurredAt, Type)`; se o volume crescer, entra uma tabela de totais diários. Integração futura com Google Search Console é independente disso.

### 14. Estratégia de testes (TDD)
- Ciclo vermelho → verde → refatorar por requisito: cada cenário das specs vira ao menos um teste antes do código.
- **Unidade**: entidades e regras de domínio, validadores, handlers (EF Core InMemory para fluxos simples e Moq para serviços externos como relógio, hash, armazenamento e revalidação; o que depende de SQL do PostgreSQL fica nos testes de integração), behaviors, `TokenService`, `PasswordHasher`, `FileValidationService`, geração de slug, cálculo de KPIs, chaves de `.resx`, mapeamentos `ToDto()`.
- **Integração**: `WebApplicationFactory` + Testcontainers PostgreSQL (um container por execução, banco limpo por classe de teste via Respawn), cobrindo autorização, Problem Details, localização, rate limit, upload real e fluxo completo de login/refresh/logout.
- **Frontend**: Vitest + Testing Library para `useDebounce`, `useClientTable`, validação de upload, interceptor de refresh (single-flight), formatação, simulador, geradores de JSON-LD e paridade de mensagens.

### 15. Ferramenta de administração
`LaBlanca.Tools` (console) reutiliza `Infrastructure`: `create-admin --tenant la-blanca --name ... --email ...` pede a senha sem eco no terminal; `reset-password --email ...` para recuperação. Também documentado o SQL equivalente para quem preferir inserir pelo banco, com o hash gerado por `hash-password`.

### 16. Banco de produção no Supabase (só PostgreSQL)
- **Uso**: apenas o PostgreSQL gerenciado. A autenticação continua própria (JWT + BCrypt + refresh rotation, como especificado); Supabase Auth foi descartado porque não atende às regras pedidas (claims `TenantId`/`Permissions`, cookie de refresh próprio, blacklist) e criaria dependência do fornecedor. O frontend não usa o cliente do Supabase.
- **Data API**: o Supabase publica automaticamente uma API REST (PostgREST) sobre os schemas expostos (por padrão `public`) usando a chave `anon`, que é pública. Defesa em camadas: (1) todas as tabelas e o `__EFMigrationsHistory` ficam no schema `lablanca` (`HasDefaultSchema` + `MigrationsHistoryTable(..., "lablanca")`), que não é exposto; (2) toda migração habilita `ROW LEVEL SECURITY` em cada tabela, sem políticas, e um teste de integração verifica isso no catálogo e simula o papel `anon`; (3) no painel do Supabase, desativar a Data API ou garantir que `lablanca` não esteja na lista de schemas expostos. A API conecta como dona das tabelas, então o RLS não a afeta.
- **Conexão**: connection string só por variável de ambiente (`ConnectionStrings__Default`). Para uma API de processo contínuo, usar o pooler Supavisor em **modo sessão** (aceita IPv4 e não tem as restrições do modo transação) ou a conexão direta, se a hospedagem tiver IPv6. SSL obrigatório: preferir `SSL Mode=VerifyFull` com o certificado CA do Supabase; no mínimo `Require`. Um validador de inicialização falha em `Production` sem SSL. `Maximum Pool Size` ajustado ao limite de conexões do plano.
- **Migrações**: nunca na inicialização em `Production`. O deploy gera `dotnet ef migrations bundle` e o executa com a conexão direta ou o pooler em modo sessão.
- **Versão**: `docker-compose.yml` e Testcontainers usam a mesma versão major do PostgreSQL do projeto Supabase (conferir no painel ao criar o projeto; hoje 17). Testes continuam com a imagem oficial `postgres`, já que não usamos recursos exclusivos do Supabase.
- **Região**: recomendada `South America (São Paulo)`, a mais próxima de Ciudad del Este.
- **Arquivos**: o banco no Supabase não muda a abstração `IFileStorage`. Se a hospedagem da API tiver disco efêmero, a próxima implementação natural é o Supabase Storage (compatível com S3) em bucket privado; isso fica fora desta mudança (ver perguntas em aberto).

### 17. Marca: logo e ícones
- **Logo**: a imagem recebida é uma foto da peça impressa. O fundo cinza e as linhas decorativas foram removidos por segmentação de cor. As cores foram normalizadas para os tokens do tema (vermelho `#E31B23`, azul `#005DAA` nos blocos e no texto), mantendo a suavização das letras. Na foto elas aparecem mais escuras (`#CA1F20` e `#01559D`) por causa da iluminação; o ícone digital (`#E20F16`) confirma o vermelho vivo. O resultado são duas variantes em PNG e WebP (610×348): `logo-transparent` (fundo claro) e `logo-transparent-white-text` (fundo escuro, "CIUDAD DEL ESTE" em branco). Uso via `next/image` com largura e altura fixas. O arquivo vetorial oficial, quando chegar, substitui essas versões sem mudar o código.
- **Ícone**: o original de 50×58 px é pequeno demais para 180/512 px e tinha o beiral do telhado cortado na borda. Foi redesenhado em SVG a partir do mapa de pixels (`brand-mark.svg`, viewBox 54×58, janelas e porta vazadas por máscara). `favicon.svg` é a mesma forma com o prédio azul `#005DAA` no tema claro e branco no tema escuro (`prefers-color-scheme`), para não sumir em abas claras sem precisar de fundo. ICO, apple-touch-icon e ícones 192/512 (opacos por exigência das plataformas) são gerados com `sharp` a partir do SVG, sobre fundo azul.
- **Cores do tema**: mantidos os tokens do protótipo (`#E31B23`, `#005DAA`), agora usados também na logo e no ícone, para que marca e interface tenham exatamente as mesmas cores.

### 18. Ajustes descobertos na implementação
- **Uploads direto para a API**: um arquivo de ~45 MB pelo rewrite `/api` do Next estourou o tempo limite do proxy (30 s) e chegou truncado; direto na API levou menos de 1 s. O painel envia para `NEXT_PUBLIC_API_UPLOAD_URL` com o token no cabeçalho; a API libera a origem do site no CORS e a CSP do site inclui essa origem em `connect-src`. As demais chamadas continuam pela mesma origem.
- **Imagens de rascunhos no painel**: a rota pública de mídia só serve anúncios publicados; o painel carrega as fotos de anúncios não publicados pela rota autenticada (`/api/admin/files/{id}/content`) como blob.
- **Peso das páginas públicas**: o formulário público usa `zod/mini` e `fetch` (sem Axios); o login, que usa a sessão com Axios, é carregado só quando o modal abre; o cliente recebe só os namespaces de mensagens usados no site; CSS embutido (`experimental.inlineCss`) e fontes variáveis, com a Fredoka sem pré-carregamento.
- **404 do anúncio**: o `not-found.tsx` do anúncio é componente de cliente. No servidor ele seria renderizado junto com a página estática (ISR) e ler o idioma exigiria `headers()`, o que derrubava a página com erro 500 em produção.

## Risks / Trade-offs

- [MediatR 12 não recebe mais correções da linha aberta] → versão fixada em `Directory.Packages.props`, uso restrito a `ISender`/`IPipelineBehavior`; auditoria do NuGet ativa e com aviso tratado como erro, de modo que uma vulnerabilidade nova quebra o build.
- [Blacklist em memória não é compartilhada entre instâncias e some ao reiniciar] → aceitável com uma instância; access token dura só 15 min. A interface permite trocar por `IDistributedCache` (Redis) se houver mais de uma instância.
- [Cultura `gn` pode não existir no ICU] → `PredefinedCulturesOnly=false` e teste de unidade; no frontend, formatação de `gn` usa `es-PY`.
- [Traduções em guarani de baixa qualidade prejudicam a marca e o SEO] → `gn` começa com espanhol e só recebe textos revisados por falante nativo; páginas `gn` sem tradução de conteúdo continuam canônicas a si mesmas, mas a lista de pendências fica no repositório.
- [Upload de 50 MB pelo rewrite do Next pode estourar tempo ou memória] → `proxy.ts` não intercepta `/api`; teste de upload grande na fase do painel; plano B com envio direto para a origem da API.
- [Fotos originais podem conter EXIF com localização] → o site exibe apenas versões otimizadas pelo `next/image` (que remove metadados); a rota pública original continua acessível para quem tiver a URL. Remover EXIF no upload fica como melhoria se o cliente pedir.
- [Tiles do OpenStreetMap têm política de uso justo] → URL de tiles configurável; para tráfego alto, trocar por provedor com chave.
- [Cotação manual pode ficar desatualizada] → data da cotação visível no site e no painel; cotação automática fica fora do escopo.
- [Testes de integração exigem Docker] → testes de unidade rodam sem Docker; README explica.
- [Escopo grande em uma única mudança] → fases com commit e testes verdes ao fim de cada uma.
- [Analytics próprio conta menos que ferramentas de mercado (bloqueadores, robôs não identificados)] → números servem para tendência no dashboard, não para auditoria.
- [Data API do Supabase expor tabelas pela chave pública `anon`] → schema `lablanca` não exposto, RLS em todas as tabelas com teste automático, Data API desativada no painel.
- [Limite de conexões e pausa do plano gratuito do Supabase] → pool de conexões limitado na connection string; para produção, usar plano pago (o gratuito pausa projetos inativos e tem backups limitados; conferir condições atuais).
- [Logo raster de 610 px fica levemente suave em "CIUDAD DEL ESTE" em telas de alta densidade] → usar no tamanho de exibição até ~300 px de largura; pedir o vetor oficial.
- [Ícone redesenhado diferir do original em detalhes] → comparação lado a lado aprovada nesta etapa; ajustes finos quando houver arquivo vetorial oficial.

## Migration Plan

Não há produção nem dados reais. Ordem de implementação:
1. Commitar o scaffold atual e configurar o remoto `origin` no GitHub.
2. Arquivar `scaffold-initial-project` para que `backend-api-foundation` e `frontend-app-shell` existam em `openspec/specs/`.
3. Reestruturar a solução, apagar a migração inicial do scaffold (`docker compose down -v` no banco local) e gerar a nova migração em `LaBlanca.Migrations`.
4. Seguir as fases do `tasks.md`, com commit ao fim de cada fase.

Rollback: cada fase é um commit; voltar uma fase é `git revert` do commit correspondente e `dotnet ef database update <migração anterior>`.

Primeiro deploy no Supabase (quando a hospedagem estiver definida):
1. Criar o projeto na região de São Paulo e conferir a versão major do PostgreSQL.
2. Desativar a Data API (ou confirmar que só `public`/`graphql_public` estão expostos).
3. Baixar o certificado CA e montar a connection string do pooler em modo sessão com `SSL Mode=VerifyFull`.
4. Executar o bundle de migrações e depois `LaBlanca.Tools create-admin`.
5. Configurar as variáveis de ambiente da API e do site e verificar `/health/ready`.

## Open Questions

- **Vetor da marca**: existe arquivo vetorial (SVG, AI, PDF ou EPS) da logo e do ícone? Ele substituiria as versões tratadas a partir da foto e do PNG de 50 px e confirmaria os códigos de cor oficiais.
- **Domínio** definitivo do site (necessário para canônicas, sitemap e Open Graph) e **hospedagem** da API e do site (afeta HTTPS, proxy reverso, IPv4/IPv6 para o Supabase e disco para arquivos).
- **Supabase**: plano (gratuito ou pago) e região. Usar também o Supabase Storage para os arquivos, caso a hospedagem não tenha disco persistente?
- **Dados reais**: telefone, WhatsApp, endereço do escritório, horário, redes sociais, números institucionais ("+12 anos", "USD 2,5B+") e taxa do simulador.
- **Tradutor de guarani**: quem revisa os textos?
- **Política de privacidade**: texto jurídico a ser fornecido ou validado pela imobiliária, considerando a legislação paraguaia de proteção de dados e a LGPD para visitantes brasileiros.
- **Quantos admins** iniciais e com quais e-mails.

### Situação em 2 de outubro de 2026
- Vetor da marca: **pendente**; seguem em uso as versões tratadas da foto e o ícone redesenhado.
- Domínio e hospedagem: **pendentes**; `NEXT_PUBLIC_SITE_URL` e a origem de upload precisam ser definidos antes do build de produção.
- Supabase: **decidido** como banco de produção (só PostgreSQL); plano **pendente**, região recomendada São Paulo. Supabase Storage segue fora do escopo.
- Dados reais: **pendentes**; o painel permite editá-los em Configurações. Os números institucionais continuam provisórios nos arquivos de mensagens.
- Tradutor de guarani: **pendente**; `frontend/gn-pending.md` lista as chaves ainda em espanhol.
- Política de privacidade: rascunho publicado nos quatro idiomas; **revisão jurídica pendente**. As respostas das perguntas frequentes também precisam de conferência da imobiliária.
- Admins iniciais: **pendente**; criados com `LaBlanca.Tools create-admin`.
