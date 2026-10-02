# dev-environment Specification

## Purpose
TBD - created by archiving change scaffold-initial-project. Update Purpose after archive.
## Requirements
### Requirement: Banco de dados local via Docker
O projeto SHALL fornecer `docker-compose.yml` na raiz com um serviço PostgreSQL com volume persistente e credenciais de desenvolvimento.

#### Scenario: Banco sobe com um comando
- **WHEN** o desenvolvedor executa `docker compose up -d` na raiz
- **THEN** o PostgreSQL fica acessível em `localhost:5432` com o banco `lablanca`

#### Scenario: Dados persistem entre reinícios
- **WHEN** o container é parado e iniciado novamente
- **THEN** os dados gravados anteriormente continuam disponíveis

### Requirement: Configuração por ambiente sem segredos versionados
O projeto SHALL manter segredos fora do controle de versão, versionando apenas arquivos de exemplo (`.env.example`) e configurações de desenvolvimento sem credenciais reais.

#### Scenario: Arquivos sensíveis ignorados
- **WHEN** o desenvolvedor cria `.env`, `.env.local` ou `appsettings.*.local.json`
- **THEN** esses arquivos são ignorados pelo git

### Requirement: Documentação de execução
O projeto SHALL conter `README.md` na raiz descrevendo pré-requisitos, estrutura de pastas e passos para rodar banco, backend e frontend localmente.

#### Scenario: Novo desenvolvedor roda o projeto
- **WHEN** um desenvolvedor segue o README em uma máquina com os pré-requisitos instalados
- **THEN** consegue subir banco, API e frontend e ver a página inicial consumindo a API

### Requirement: Repositório git inicializado
O projeto SHALL ser um repositório git com `.gitignore` cobrindo Node.js, Next.js, .NET e arquivos de IDE, e `.editorconfig` com convenções de formatação.

#### Scenario: Artefatos de build não versionados
- **WHEN** o desenvolvedor executa os builds de frontend e backend
- **THEN** `git status` não lista `node_modules/`, `.next/`, `bin/` ou `obj/`

