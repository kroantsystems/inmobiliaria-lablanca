## Why

A La Blanca (Ciudad del Este / Alto Paraná) precisa de um site público que gere contatos e seja bem encontrado em buscadores e assistentes de IA, e de um painel interno onde o dono publica anúncios, acompanha métricas, leads, proprietários, visitas e documentos. Hoje existe apenas o scaffold técnico; os protótipos (`proto 19.html`, `dashboard.html`, `calendario e banco de imagens.html`) já definem a identidade visual e os módulos, então é hora de construir o produto real em cima dessa base.

## What Changes

- Site público multilíngue (espanhol e português como foco, inglês e guarani) baseado no `proto 19.html`: home com destaque do mês, busca (comprar/alugar, tipo, zona, preço, quartos), catálogo, página de detalhe por imóvel, mapa com apenas os imóveis da La Blanca, seção institucional, simulador de parcelas, conversão de moeda (USD/PYG/BRL), formulários de contato/visita/proprietário/newsletter e botão de WhatsApp.
- Marca oficial: logo sem fundo (variantes para fundo claro e escuro) e ícone redesenhado em SVG, usado como favicon adaptável ao tema claro/escuro e como base dos ícones de aplicativo. Os arquivos estão em `assets/` desta mudança.
- Botão "Login" (antes "Acesso ADM") no topo abre um modal de login; só administradores acessam o sistema. Não há cadastro público de usuários: o admin é criado direto no banco por uma ferramenta de linha de comando e só pode trocar a própria senha depois de logado.
- Painel administrativo com o visual do `dashboard.html`: Dashboard de métricas, Anúncios & Mídia, Clientes/Leads, Proprietários, Agenda/Calendário de visitas e Banco de Imagens e Documentos (do segundo protótipo), além de Configurações e Minha Conta.
- Autenticação JWT com Access Token de 15 min (claims `TenantId`, `Role`, `Permissions`) e Refresh Token opaco de 7 dias em cookie HttpOnly + Secure, com rotação, renovação silenciosa via interceptor Axios, logout com revogação e blacklist em memória. Senhas com BCrypt. Todos os endpoints exigem autorização por padrão; só os endpoints públicos do site são liberados explicitamente.
- Backend em TDD com ASP.NET Core (.NET 10, C# 14), Controllers, MediatR (CQRS), FluentValidation no pipeline, AutoMapper, DTOs, EF Core 10 + PostgreSQL, Serilog, Problem Details (RFC 7807), localização por `.resx` via `Accept-Language`, rate limiting e health checks.
- Novos projetos `LaBlanca.Shared` (tratamento global de exceções e recursos de idioma) e `LaBlanca.Migrations` (histórico de migrações isolado). Testes separados em `LaBlanca.UnitTests` e `LaBlanca.IntegrationTests` (xUnit, Moq, FluentAssertions 7.x). **BREAKING** (interno): o projeto `LaBlanca.Tests` e a pasta de migrações da `Infrastructure` deixam de existir.
- Upload de arquivos com validação no cliente e no servidor: imagens (`.png`, `.jpg`, `.jpeg`, `.webp`) até 50 MB, vídeos e documentos (`.pdf`, `.doc`, `.docx`) com limites menores. Documentos são privados; só mídia publicada de anúncios é pública.
- Padrões de frontend no painel: listas carregadas em páginas grandes e filtradas/ordenadas no cliente com `useMemo`, busca com `useDebounce` de 300 ms.
- SEO e GEO: renderização no servidor, metadados por página e idioma, `hreflang`, URLs amigáveis por idioma, `sitemap.xml`, `robots.txt`, dados estruturados JSON-LD (imobiliária, anúncios, breadcrumbs, FAQ), imagens Open Graph geradas, páginas por zona, `llms.txt`, revalidação sob demanda ao publicar anúncios e foco em Core Web Vitals.
- Next.js usado como camada de servidor onde faz sentido: proxy same-origin para a API (cookie de refresh first-party), revalidação de cache, geração de sitemap/robots/llms.txt e imagens OG.

## Capabilities

### New Capabilities
- `admin-auth`: Login de administrador, emissão e rotação de tokens JWT/refresh, logout com revogação, troca de senha, bloqueio por tentativas e criação de admin via ferramenta de linha de comando.
- `api-platform`: Regras transversais da API: autorização obrigatória por padrão com permissões, isolamento por tenant, Problem Details, localização de mensagens, pipeline CQRS com validação e transação, logging estruturado e rate limiting.
- `property-listings`: Cadastro e gestão de anúncios de imóveis (traduções, status, destaque, publicação) e consulta pública com busca e filtros.
- `media-library`: Banco de imagens, vídeos e documentos com validação de tipo e tamanho, vínculo opcional a anúncio, separação entre arquivos públicos e privados.
- `leads`: Captação de contatos pelo site (contato, pedido de visita, proposta de proprietário, newsletter) e gestão de clientes/leads no painel.
- `property-owners`: Cadastro de proprietários e vínculo com os imóveis captados.
- `visit-scheduling`: Agenda de visitas com visão de calendário mensal e lista de próximos compromissos.
- `site-analytics`: Registro anônimo de eventos do site e métricas do dashboard (acessos, cliques, vendas, aluguéis, ranking de anúncios).
- `site-settings`: Configurações editáveis pelo admin (contatos, WhatsApp, cotações de moeda, taxa do simulador, metas, zonas).
- `internationalization`: Suporte a es, pt, en e gn no site, no painel e nas mensagens da API, com fallback de idioma.
- `public-website`: Páginas e componentes do site público baseados no protótipo, incluindo modal de login.
- `admin-panel`: Interface do painel administrativo baseada nos protótipos, com filtros no cliente, busca com debounce e validação de uploads.
- `seo-geo`: Otimização para buscadores e motores generativos (metadados, hreflang, sitemap, robots, JSON-LD, llms.txt, OG, performance).

### Modified Capabilities
- `backend-api-foundation`: Estrutura da solução passa a incluir `LaBlanca.Shared`, `LaBlanca.Migrations`, `LaBlanca.UnitTests` e `LaBlanca.IntegrationTests`; migrações saem da `Infrastructure`; tabelas vão para o schema `lablanca` com Row Level Security; produção no Supabase com SSL obrigatório e migrações por bundle; health check ganha endpoints de liveness e readiness; CORS passa a aceitar credenciais.
- `frontend-app-shell`: Layout deixa de ser fixo em `pt-BR` e passa a ser por idioma; acesso à API passa por proxy same-origin do Next.js em vez de chamar a API direto pelo navegador.

> Pré-requisito: arquivar `scaffold-initial-project` antes de arquivar esta mudança, para que `backend-api-foundation` e `frontend-app-shell` existam em `openspec/specs/`.

## Impact

- **Backend**: novos projetos `LaBlanca.Shared`, `LaBlanca.Migrations`, `LaBlanca.Tools`, `LaBlanca.UnitTests`, `LaBlanca.IntegrationTests`; remoção de `LaBlanca.Tests`; Controllers, entidades, migrações e serviços novos em todas as camadas.
- **Frontend**: reestruturação de rotas para `src/app/[locale]/...`, área `/[locale]/admin`, arquivos de mensagens por idioma, cliente Axios com interceptors, novo tema (cores e fontes do protótipo), testes com Vitest.
- **Dependências backend**: MediatR 12.x, AutoMapper 14.x, FluentValidation, BCrypt.Net-Next, Microsoft.AspNetCore.Authentication.JwtBearer, Serilog.AspNetCore, Moq, FluentAssertions 7.x, Testcontainers.PostgreSql. MediatR e AutoMapper ficam nas últimas versões de licença aberta, pelo mesmo motivo do FluentAssertions 7.x.
- **Dependências frontend**: next-intl, axios, @tanstack/react-query, react-hook-form, zod, react-leaflet/leaflet, recharts, lucide-react, date-fns, Vitest + Testing Library.
- **Infraestrutura**: PostgreSQL local em Docker no desenvolvimento e PostgreSQL do Supabase em produção (só banco: sem Supabase Auth nem cliente Supabase, Data API desativada); armazenamento de arquivos em disco local (abstraído para troca futura, por exemplo para o Supabase Storage); Docker necessário também para os testes de integração; novas variáveis de ambiente (segredo JWT, URL pública do site, URL interna da API, segredo de revalidação).
- **Segurança**: endpoints administrativos fechados por padrão, rate limiting em login e formulários públicos, documentos privados nunca expostos em URLs públicas.
