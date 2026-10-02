# backend-api-foundation Specification

## Purpose
TBD - created by archiving change scaffold-initial-project. Update Purpose after archive.
## Requirements
### Requirement: Solução .NET 10 em camadas
O sistema SHALL fornecer em `backend/` uma solução .NET 10 com os projetos `LaBlanca.Api`, `LaBlanca.Application`, `LaBlanca.Domain`, `LaBlanca.Infrastructure` e `LaBlanca.Tests`, respeitando a direção de dependências Api → Application/Infrastructure → Domain.

#### Scenario: Solução compila
- **WHEN** o desenvolvedor executa `dotnet build` em `backend/`
- **THEN** todos os projetos compilam sem erros

#### Scenario: Domain sem dependências externas
- **WHEN** as referências do projeto `LaBlanca.Domain` são inspecionadas
- **THEN** ele não referencia nenhum outro projeto da solução nem pacotes de infraestrutura

### Requirement: Health check
A API SHALL expor `GET /health` que informa o estado da aplicação e da conexão com o banco de dados.

#### Scenario: API e banco saudáveis
- **WHEN** a API está rodando e o PostgreSQL está acessível
- **THEN** `GET /health` retorna HTTP 200 com status `Healthy`

#### Scenario: Banco indisponível
- **WHEN** o PostgreSQL não está acessível
- **THEN** `GET /health` retorna HTTP 503 com status `Unhealthy`

### Requirement: Documentação OpenAPI em desenvolvimento
A API SHALL publicar o documento OpenAPI apenas no ambiente `Development`.

#### Scenario: Documento disponível em desenvolvimento
- **WHEN** a API roda com `ASPNETCORE_ENVIRONMENT=Development`
- **THEN** `GET /openapi/v1.json` retorna o documento OpenAPI

#### Scenario: Documento oculto em produção
- **WHEN** a API roda com `ASPNETCORE_ENVIRONMENT=Production`
- **THEN** `GET /openapi/v1.json` retorna HTTP 404

### Requirement: CORS para o frontend
A API SHALL aceitar requisições cross-origin apenas das origens listadas na configuração `Cors:AllowedOrigins`.

#### Scenario: Origem permitida
- **WHEN** uma requisição chega com `Origin: http://localhost:3000` e essa origem está configurada
- **THEN** a resposta inclui o cabeçalho `Access-Control-Allow-Origin: http://localhost:3000`

#### Scenario: Origem não permitida
- **WHEN** uma requisição chega de origem não configurada
- **THEN** a resposta não inclui cabeçalho `Access-Control-Allow-Origin`

### Requirement: Acesso a dados com EF Core e PostgreSQL
A camada `Infrastructure` SHALL configurar um `DbContext` do EF Core com provedor Npgsql, lendo a connection string `ConnectionStrings:Default`, e a solução SHALL conter uma migração inicial.

#### Scenario: Migração aplicada
- **WHEN** o desenvolvedor executa `dotnet ef database update` com o PostgreSQL local rodando
- **THEN** o banco é criado com a tabela de histórico de migrações

### Requirement: Testes automatizados de base
O projeto `LaBlanca.Tests` SHALL conter ao menos um teste de integração que sobe a API em memória e valida o endpoint de health.

#### Scenario: Testes executam
- **WHEN** o desenvolvedor executa `dotnet test` em `backend/`
- **THEN** todos os testes passam

