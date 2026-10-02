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
A aplicação SHALL renderizar um layout comum a todas as páginas contendo cabeçalho com o nome "La Blanca" e navegação, e rodapé com informações de contato placeholder.

#### Scenario: Página inicial exibe layout
- **WHEN** o usuário acessa `/`
- **THEN** a página exibe cabeçalho, conteúdo placeholder da home e rodapé
- **AND** o documento possui `lang="pt-BR"` e título com "La Blanca"

### Requirement: Configuração de acesso à API
A aplicação SHALL ler a URL base da API da variável de ambiente `NEXT_PUBLIC_API_URL` e expor um cliente HTTP único reutilizável pelas páginas.

#### Scenario: URL da API configurada
- **WHEN** `NEXT_PUBLIC_API_URL` está definida em `.env.local`
- **THEN** as requisições do cliente HTTP usam essa URL como base

#### Scenario: Exemplo de variáveis documentado
- **WHEN** o desenvolvedor clona o projeto
- **THEN** existe `frontend/.env.example` com `NEXT_PUBLIC_API_URL` preenchida para o ambiente local

