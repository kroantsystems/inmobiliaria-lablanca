## MODIFIED Requirements

### Requirement: Login de administrador
A API SHALL expor `POST /api/auth/login`, aceitando nome de usuário e senha, e SHALL autenticar apenas usuários ativos com papel de administrador, comparando a senha com o hash BCrypt armazenado. O nome de usuário SHALL ser comparado sem diferenciar maiúsculas de minúsculas e sem espaços nas pontas. O e-mail SHALL NOT ser aceito como identificador de login.

#### Scenario: Credenciais válidas
- **WHEN** um admin ativo envia usuário e senha corretos
- **THEN** a API retorna HTTP 200 com `accessToken`, `expiresIn` e dados básicos do usuário (id, nome, usuário, e-mail opcional, papel)
- **AND** a resposta define o cookie de refresh token

#### Scenario: Usuário com letras maiúsculas
- **WHEN** o usuário cadastrado é `admin` e o login é enviado como ` Admin `
- **THEN** a autenticação é feita normalmente

#### Scenario: Login com e-mail
- **WHEN** alguém envia o e-mail do admin no campo de usuário
- **THEN** a API retorna HTTP 401 com a mensagem genérica

#### Scenario: Credenciais inválidas
- **WHEN** o usuário não existe ou a senha está errada
- **THEN** a API retorna HTTP 401 com Problem Details e mensagem genérica, sem indicar qual dos dois campos falhou

#### Scenario: Usuário inativo
- **WHEN** um usuário marcado como inativo envia credenciais corretas
- **THEN** a API retorna HTTP 401 com a mesma mensagem genérica

#### Scenario: Dados de login ausentes
- **WHEN** o corpo da requisição não tem usuário ou senha
- **THEN** a API retorna HTTP 400 com os erros de validação por campo

### Requirement: Sem cadastro público de usuários
O sistema SHALL NOT expor endpoint de cadastro de usuários. Administradores SHALL ser criados por uma ferramenta de linha de comando (`LaBlanca.Tools`) que grava o usuário no banco com nome de usuário, nome, e-mail opcional e senha em hash BCrypt. O nome de usuário SHALL ter de 3 a 32 caracteres entre letras minúsculas sem acento, números, ponto, hífen e sublinhado, e SHALL ser único por tenant.

#### Scenario: Criação de admin via ferramenta
- **WHEN** o operador executa o comando de criação de admin informando tenant, usuário, nome e senha
- **THEN** o usuário é gravado no banco com papel de administrador e senha em hash BCrypt

#### Scenario: Usuário já existente
- **WHEN** o comando recebe um nome de usuário já cadastrado no mesmo tenant
- **THEN** a ferramenta encerra com erro e não altera o usuário existente

#### Scenario: E-mail já existente
- **WHEN** o comando recebe um e-mail opcional já usado por outro usuário do mesmo tenant
- **THEN** a ferramenta encerra com erro e não altera o usuário existente

#### Scenario: Usuário fora do formato
- **WHEN** o nome de usuário tem espaço, acento ou menos de 3 caracteres
- **THEN** a ferramenta encerra com erro explicando o formato aceito

#### Scenario: Troca do nome de usuário
- **WHEN** o operador executa o comando de renomear informando o usuário atual e o novo
- **THEN** o usuário passa a entrar com o novo nome e as sessões abertas são encerradas

#### Scenario: Nenhuma rota de registro
- **WHEN** alguém envia `POST /api/auth/register`
- **THEN** a API retorna HTTP 404

### Requirement: Dados do usuário logado
A API SHALL expor `GET /api/auth/me`, protegido, retornando id, nome, nome de usuário, e-mail opcional, papel e permissões do usuário do token.

#### Scenario: Consulta autenticada
- **WHEN** um admin autenticado chama `/api/auth/me`
- **THEN** a API retorna HTTP 200 com seus dados, incluindo o nome de usuário

#### Scenario: Consulta sem token
- **WHEN** a chamada é feita sem access token
- **THEN** a API retorna HTTP 401

## ADDED Requirements

### Requirement: Nome de usuário para contas existentes
A migração SHALL atribuir um nome de usuário a cada conta existente a partir da parte do e-mail antes da `@`, normalizada para o formato aceito, acrescentando sufixo numérico em caso de repetição, e SHALL manter o e-mail como dado opcional da conta.

#### Scenario: Conta criada antes da mudança
- **WHEN** a migração roda com o usuário `admin@lablanca.test`
- **THEN** a conta passa a ter o nome de usuário `admin` e continua com a mesma senha

#### Scenario: Partes iguais de e-mail
- **WHEN** existem as contas `ana@a.com` e `ana@b.com` no mesmo tenant
- **THEN** elas recebem os usuários `ana` e `ana2`
