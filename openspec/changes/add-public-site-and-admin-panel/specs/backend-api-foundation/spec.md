## MODIFIED Requirements

### Requirement: Solução .NET 10 em camadas
O sistema SHALL fornecer em `backend/` uma solução .NET 10 com os projetos `src/LaBlanca.Api`, `src/LaBlanca.Application`, `src/LaBlanca.Domain`, `src/LaBlanca.Infrastructure`, `src/LaBlanca.Shared`, `src/LaBlanca.Migrations`, `tools/LaBlanca.Tools`, `tests/LaBlanca.UnitTests` e `tests/LaBlanca.IntegrationTests`, respeitando a direção de dependências Api → Application/Infrastructure/Migrations → Domain, com `Shared` sem dependência de nenhum outro projeto da solução.

#### Scenario: Solução compila
- **WHEN** o desenvolvedor executa `dotnet build` em `backend/`
- **THEN** todos os projetos compilam sem erros

#### Scenario: Domain sem dependências externas
- **WHEN** as referências do projeto `LaBlanca.Domain` são inspecionadas
- **THEN** ele não referencia nenhum outro projeto da solução nem pacotes de infraestrutura

#### Scenario: Shared independente
- **WHEN** as referências do projeto `LaBlanca.Shared` são inspecionadas
- **THEN** ele não referencia nenhum outro projeto da solução

### Requirement: Health check
A API SHALL expor `GET /health` com o estado agregado da aplicação e do banco, `GET /health/live` que indica apenas que o processo responde, e `GET /health/ready` que verifica o banco de dados e a pasta de armazenamento de arquivos. Os três endpoints SHALL ser públicos.

#### Scenario: API e banco saudáveis
- **WHEN** a API está rodando e o PostgreSQL está acessível
- **THEN** `GET /health` retorna HTTP 200 com status `Healthy`

#### Scenario: Banco indisponível
- **WHEN** o PostgreSQL não está acessível
- **THEN** `GET /health` e `GET /health/ready` retornam HTTP 503 com status `Unhealthy`
- **AND** `GET /health/live` continua retornando HTTP 200

#### Scenario: Armazenamento sem permissão de escrita
- **WHEN** a pasta de armazenamento de arquivos não existe ou não aceita escrita
- **THEN** `GET /health/ready` retorna HTTP 503 indicando a checagem `storage`

### Requirement: CORS para o frontend
A API SHALL aceitar requisições cross-origin apenas das origens listadas na configuração `Cors:AllowedOrigins`, permitindo credenciais (cookies) somente para essas origens.

#### Scenario: Origem permitida
- **WHEN** uma requisição chega com `Origin: http://localhost:3000` e essa origem está configurada
- **THEN** a resposta inclui `Access-Control-Allow-Origin: http://localhost:3000` e `Access-Control-Allow-Credentials: true`

#### Scenario: Origem não permitida
- **WHEN** uma requisição chega de origem não configurada
- **THEN** a resposta não inclui cabeçalho `Access-Control-Allow-Origin`

### Requirement: Acesso a dados com EF Core e PostgreSQL
A camada `Infrastructure` SHALL configurar um `DbContext` do EF Core com provedor Npgsql, lendo a connection string `ConnectionStrings:Default`, com schema padrão `lablanca` para as tabelas e para o histórico de migrações, e as migrações SHALL ficar no projeto `LaBlanca.Migrations`, configurado como assembly de migrações do `DbContext`.

#### Scenario: Migração aplicada
- **WHEN** o desenvolvedor executa `dotnet ef database update -p src/LaBlanca.Migrations -s src/LaBlanca.Api` com o PostgreSQL local rodando
- **THEN** o banco recebe, no schema `lablanca`, todas as tabelas do modelo e a tabela de histórico de migrações
- **AND** nenhuma tabela da aplicação é criada no schema `public`

#### Scenario: Nova migração no projeto certo
- **WHEN** o desenvolvedor executa `dotnet ef migrations add <Nome> -p src/LaBlanca.Migrations -s src/LaBlanca.Api`
- **THEN** os arquivos da migração são criados em `LaBlanca.Migrations` e nenhum arquivo é criado em `LaBlanca.Infrastructure`

### Requirement: Testes automatizados de base
A solução SHALL separar testes em `LaBlanca.UnitTests` (domínio, handlers, validadores e serviços, com dependências simuladas via Moq) e `LaBlanca.IntegrationTests` (API em memória com `WebApplicationFactory` e PostgreSQL real via Testcontainers), ambos com xUnit e FluentAssertions 7.x, e todo comportamento novo SHALL ser escrito com o teste antes da implementação.

#### Scenario: Testes executam
- **WHEN** o desenvolvedor executa `dotnet test` em `backend/` com Docker disponível
- **THEN** todos os testes de unidade e de integração passam

#### Scenario: Apenas testes de unidade
- **WHEN** o desenvolvedor executa `dotnet test tests/LaBlanca.UnitTests`
- **THEN** os testes rodam sem precisar de Docker nem de banco

#### Scenario: Versão do FluentAssertions travada
- **WHEN** as referências de pacote dos projetos de teste são inspecionadas
- **THEN** `FluentAssertions` está fixado em versão 7.x

## ADDED Requirements

### Requirement: Banco de produção no Supabase
O banco de produção SHALL ser o PostgreSQL gerenciado do Supabase, usado apenas como banco de dados: a aplicação SHALL NOT usar Supabase Auth nem o cliente JavaScript do Supabase. Para que a Data API do Supabase (PostgREST com a chave pública `anon`) não exponha dados, as tabelas SHALL ficar no schema `lablanca`, fora dos schemas expostos, e todas as tabelas SHALL ter Row Level Security habilitado sem políticas para os papéis `anon` e `authenticated`.

#### Scenario: Toda tabela com RLS
- **WHEN** o teste de integração consulta o catálogo do PostgreSQL depois de aplicar as migrações
- **THEN** todas as tabelas do schema `lablanca` têm Row Level Security habilitado

#### Scenario: Papel anônimo não lê dados
- **WHEN** um papel equivalente ao `anon` do Supabase recebe permissão de leitura no schema `lablanca` e consulta a tabela de leads
- **THEN** a consulta não retorna nenhuma linha

#### Scenario: Nova tabela sem RLS
- **WHEN** uma migração futura cria tabela sem habilitar Row Level Security
- **THEN** o teste de integração de RLS falha

### Requirement: Conexão segura e migrações controladas em produção
Em ambiente `Production`, a API SHALL exigir connection string vinda de variável de ambiente com SSL obrigatório (`SSL Mode` igual a `Require`, `VerifyCA` ou `VerifyFull`) e SHALL NOT aplicar migrações automaticamente na inicialização. Migrações de produção SHALL ser aplicadas por um bundle do EF Core (`dotnet ef migrations bundle`) usando a conexão direta ou o pooler do Supabase em modo sessão.

#### Scenario: Conexão sem SSL em produção
- **WHEN** a API inicia em `Production` com connection string sem `SSL Mode` seguro
- **THEN** a inicialização falha com mensagem clara indicando a configuração de SSL

#### Scenario: Inicialização em produção
- **WHEN** a API inicia em `Production` com migrações pendentes
- **THEN** nenhuma migração é aplicada e o `GET /health/ready` continua refletindo o estado do banco

#### Scenario: Banco local continua funcionando
- **WHEN** a API roda em `Development` com o PostgreSQL do `docker-compose` sem SSL
- **THEN** a conexão é aceita normalmente
