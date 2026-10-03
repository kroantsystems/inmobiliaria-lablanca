# frontend-app-shell Specification

## Purpose
TBD - created by archiving change scaffold-initial-project. Update Purpose after archive.
## Requirements
### Requirement: Aplicação Next.js inicializável
O sistema SHALL fornecer uma aplicação Next.js (App Router, TypeScript, Tailwind CSS) em `frontend/` que inicia em modo de desenvolvimento e compila para produção sem erros.

#### Scenario: Servidor de desenvolvimento inicia
- **WHEN** o desenvolvedor executa `npm run dev` em `frontend/`
- **THEN** a aplicação fica disponível em `http://localhost:3000`

#### Scenario: Build de produção
- **WHEN** o desenvolvedor executa `npm run build` em `frontend/`
- **THEN** o build termina com sucesso, sem erros de TypeScript ou ESLint

### Requirement: Layout base do site
A aplicação SHALL renderizar, para cada idioma em `src/app/[locale]/`, um layout público comum com barra superior, cabeçalho com a marca "La Blanca" e navegação, rodapé e botão flutuante de WhatsApp, e um layout separado para o painel em `/[locale]/admin`.

#### Scenario: Página inicial exibe layout
- **WHEN** o usuário acessa `/es`
- **THEN** a página exibe barra superior, cabeçalho, conteúdo da home e rodapé
- **AND** o documento possui `lang="es"` e título com "La Blanca"

#### Scenario: Layout do painel separado
- **WHEN** o admin acessa `/es/admin`
- **THEN** a página usa o layout do painel, sem cabeçalho, rodapé nem WhatsApp do site público

### Requirement: Configuração de acesso à API
O navegador SHALL chamar a API sempre pela mesma origem do site, em `/api/*`, que o Next.js repassa para a API .NET configurada em `API_INTERNAL_URL`. Componentes de servidor SHALL chamar a API diretamente por `API_INTERNAL_URL`. A aplicação SHALL expor um único cliente Axios para o navegador e um único cliente de servidor.

#### Scenario: URL da API configurada
- **WHEN** `API_INTERNAL_URL` está definida em `.env.local`
- **THEN** os componentes de servidor e o repasse de `/api/*` usam essa URL como base, e o navegador nunca recebe esse endereço

#### Scenario: Chamada pelo navegador
- **WHEN** o painel faz `GET /api/admin/leads`
- **THEN** a requisição sai para a origem do site e o Next.js a encaminha para `API_INTERNAL_URL`

#### Scenario: Cookie de refresh de primeira parte
- **WHEN** o login é feito pelo navegador
- **THEN** o cookie de refresh pertence ao domínio do site, não ao domínio da API

#### Scenario: Exemplo de variáveis documentado
- **WHEN** o desenvolvedor clona o projeto
- **THEN** existe `frontend/.env.example` com `API_INTERNAL_URL`, `NEXT_PUBLIC_SITE_URL` e `REVALIDATE_SECRET` preenchidas para o ambiente local

