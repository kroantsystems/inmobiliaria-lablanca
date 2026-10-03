# api-platform Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Autorização obrigatória por padrão
A API SHALL aplicar uma política de autorização padrão que exige usuário autenticado em todos os endpoints. Endpoints públicos SHALL ser liberados explicitamente com `[AllowAnonymous]` e ficar agrupados sob `/api/public/*`, `/api/auth/login`, `/api/auth/refresh` e `/health*`.

#### Scenario: Endpoint administrativo sem token
- **WHEN** qualquer rota `/api/admin/*` é chamada sem access token
- **THEN** a API retorna HTTP 401

#### Scenario: Endpoint público sem token
- **WHEN** uma rota `/api/public/*` é chamada sem access token
- **THEN** a API processa a requisição normalmente

#### Scenario: Teste de cobertura de autorização
- **WHEN** a suíte de integração enumera todos os endpoints registrados
- **THEN** todo endpoint fora da lista pública exige autenticação

### Requirement: Autorização por permissão
Cada endpoint administrativo SHALL declarar a permissão exigida (por exemplo `properties.write`, `leads.read`, `files.write`, `dashboard.read`, `settings.write`) via `[Authorize(Policy = ...)]`, e a política SHALL validar a claim `permission` do token.

#### Scenario: Token sem a permissão exigida
- **WHEN** um usuário autenticado sem a permissão `properties.write` tenta criar um anúncio
- **THEN** a API retorna HTTP 403

#### Scenario: Token com a permissão exigida
- **WHEN** um admin com `properties.write` cria um anúncio válido
- **THEN** a API retorna HTTP 201

### Requirement: Isolamento por tenant
Toda entidade de negócio SHALL ter `TenantId`, e o acesso a dados SHALL filtrar automaticamente pelo tenant do token nas rotas administrativas e pelo tenant configurado do site nas rotas públicas.

#### Scenario: Dados de outro tenant invisíveis
- **WHEN** um admin do tenant A consulta um anúncio pelo id de um registro do tenant B
- **THEN** a API retorna HTTP 404

#### Scenario: Gravação recebe tenant do token
- **WHEN** um admin cria um registro
- **THEN** o registro é gravado com o `TenantId` da claim `tenant_id`, ignorando qualquer valor enviado no corpo

### Requirement: Erros no padrão Problem Details
A API SHALL tratar exceções em um middleware global do projeto `LaBlanca.Shared` e responder sempre no formato RFC 7807 (`application/problem+json`), sem expor stack trace fora do ambiente `Development`.

#### Scenario: Erro de validação
- **WHEN** um comando falha na validação
- **THEN** a API retorna HTTP 400 com `type`, `title`, `status`, `traceId` e `errors` agrupados por campo

#### Scenario: Recurso não encontrado
- **WHEN** um handler lança a exceção de "não encontrado"
- **THEN** a API retorna HTTP 404 em Problem Details

#### Scenario: Erro inesperado em produção
- **WHEN** ocorre uma exceção não tratada com `ASPNETCORE_ENVIRONMENT=Production`
- **THEN** a API retorna HTTP 500 com mensagem genérica e `traceId`, sem detalhes internos
- **AND** a exceção é registrada no log com o mesmo `traceId`

### Requirement: Mensagens localizadas por Accept-Language
A API SHALL resolver o idioma pelo cabeçalho `Accept-Language` entre `pt`, `es`, `en` e `gn`, e SHALL traduzir mensagens de erro e validação usando os recursos `.resx` do `LaBlanca.Shared` (`Messages.resx` como português neutro, `Messages.es.resx`, `Messages.en.resx`, `Messages.gn.resx`).

#### Scenario: Requisição em espanhol
- **WHEN** uma requisição com `Accept-Language: es-PY` falha na validação
- **THEN** as mensagens de erro vêm em espanhol

#### Scenario: Idioma não suportado
- **WHEN** a requisição envia `Accept-Language: fr`
- **THEN** as mensagens vêm no idioma padrão da API (português)

#### Scenario: Mensagem ainda sem tradução em guarani
- **WHEN** uma requisição com `Accept-Language: gn` recebe uma mensagem que ainda não foi traduzida para guarani
- **THEN** a API retorna o texto em espanhol

#### Scenario: Recursos completos em todos os idiomas
- **WHEN** o teste unitário de recursos compara as chaves dos arquivos `.resx`
- **THEN** todos os idiomas possuem exatamente as mesmas chaves que `Messages.resx`

### Requirement: Pipeline CQRS com validação e transação
Toda operação de negócio SHALL ser um Command (mutação) ou Query (leitura sem efeitos colaterais) do MediatR. O pipeline SHALL executar, nesta ordem, log, validação FluentValidation e, apenas para Commands, transação de banco.

#### Scenario: Command inválido não chega ao handler
- **WHEN** um Command falha na validação
- **THEN** o handler não é executado e nada é gravado

#### Scenario: Falha no meio de um Command
- **WHEN** um Command lança exceção depois de alterar dados
- **THEN** a transação é desfeita e nenhuma alteração persiste

#### Scenario: Query não abre transação
- **WHEN** uma Query é executada
- **THEN** nenhuma transação explícita é aberta

### Requirement: Log estruturado
A API SHALL registrar logs estruturados com Serilog, incluindo requisições HTTP (método, rota, status, duração), erros com `traceId` e Commands executados, e SHALL NOT registrar senhas, tokens ou cookies.

#### Scenario: Requisição registrada
- **WHEN** uma requisição termina
- **THEN** um log com método, rota, status e duração é gravado

#### Scenario: Dados sensíveis omitidos
- **WHEN** o Command de login ou troca de senha é registrado no log
- **THEN** os campos de senha não aparecem no log

### Requirement: Limite de requisições em endpoints públicos
A API SHALL aplicar rate limiting por IP: login com 5 requisições por minuto, formulários públicos com 10 requisições por hora e eventos de analytics com 120 requisições por minuto.

#### Scenario: Formulário público acima do limite
- **WHEN** um IP envia o 11º formulário em menos de uma hora
- **THEN** a API retorna HTTP 429 em Problem Details

### Requirement: Contratos via DTOs
Os Controllers SHALL receber e devolver apenas DTOs, nunca entidades de domínio. O mapeamento entre entidades e DTOs SHALL ser feito por código explícito (métodos de extensão `ToDto()` e projeções `Select` do EF Core), sem biblioteca de mapeamento por reflexão, e coberto por testes de unidade.

#### Scenario: Mapeamento completo
- **WHEN** o teste de unidade mapeia uma entidade preenchida para o DTO correspondente
- **THEN** todos os campos do DTO recebem o valor esperado da entidade

#### Scenario: Campo sensível fora do DTO
- **WHEN** um usuário é mapeado para o DTO de resposta
- **THEN** o DTO não contém hash de senha, contador de falhas nem dados de bloqueio

