# admin-auth Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Login de administrador
A API SHALL expor `POST /api/auth/login`, aceitando e-mail e senha, e SHALL autenticar apenas usuários ativos com papel de administrador, comparando a senha com o hash BCrypt armazenado.

#### Scenario: Credenciais válidas
- **WHEN** um admin ativo envia e-mail e senha corretos
- **THEN** a API retorna HTTP 200 com `accessToken`, `expiresIn` e dados básicos do usuário (id, nome, e-mail, papel)
- **AND** a resposta define o cookie de refresh token

#### Scenario: Credenciais inválidas
- **WHEN** o e-mail não existe ou a senha está errada
- **THEN** a API retorna HTTP 401 com Problem Details e mensagem genérica, sem indicar qual dos dois campos falhou

#### Scenario: Usuário inativo
- **WHEN** um usuário marcado como inativo envia credenciais corretas
- **THEN** a API retorna HTTP 401 com a mesma mensagem genérica

#### Scenario: Dados de login ausentes
- **WHEN** o corpo da requisição não tem e-mail válido ou senha
- **THEN** a API retorna HTTP 400 com os erros de validação por campo

### Requirement: Access token JWT de curta duração
O access token SHALL ser um JWT assinado, válido por 15 minutos, contendo as claims `sub`, `jti`, `email`, `name`, `tenant_id`, `role` e uma claim `permission` para cada permissão do usuário, e SHALL trafegar apenas no cabeçalho `Authorization: Bearer`.

#### Scenario: Token com claims esperadas
- **WHEN** um login é bem-sucedido
- **THEN** o access token decodificado contém `tenant_id`, `role` e as permissões do usuário
- **AND** sua expiração é 15 minutos após a emissão

#### Scenario: Token expirado
- **WHEN** um endpoint protegido recebe um access token expirado
- **THEN** a API retorna HTTP 401

#### Scenario: Token com assinatura inválida
- **WHEN** um endpoint protegido recebe um token alterado ou assinado com outra chave
- **THEN** a API retorna HTTP 401

### Requirement: Refresh token opaco em cookie seguro
O refresh token SHALL ser uma string aleatória opaca, válida por 7 dias, armazenada no banco apenas como hash e vinculada ao usuário, e SHALL trafegar exclusivamente em cookie `HttpOnly`, `Secure`, `SameSite=Strict`, restrito ao caminho `/api/auth`.

#### Scenario: Cookie emitido no login
- **WHEN** o login é bem-sucedido
- **THEN** a resposta contém `Set-Cookie` do refresh token com `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/api/auth` e expiração de 7 dias
- **AND** o corpo da resposta não contém o refresh token

#### Scenario: Token não fica legível no banco
- **WHEN** a tabela de refresh tokens é consultada
- **THEN** ela contém apenas o hash do token, nunca o valor original

### Requirement: Renovação com rotação de refresh token
A API SHALL expor `POST /api/auth/refresh`, que lê o refresh token do cookie, valida-o no banco, revoga-o e emite um novo par de access token e refresh token.

#### Scenario: Renovação bem-sucedida
- **WHEN** o cliente chama `/api/auth/refresh` com um refresh token válido e não revogado
- **THEN** a API retorna um novo access token e define um novo cookie de refresh token
- **AND** o refresh token anterior fica marcado como revogado e substituído

#### Scenario: Refresh token expirado ou inexistente
- **WHEN** o cookie está ausente, expirado ou não corresponde a nenhum token
- **THEN** a API retorna HTTP 401 e remove o cookie

#### Scenario: Reuso de refresh token já rotacionado
- **WHEN** o cliente apresenta um refresh token que já foi revogado por rotação
- **THEN** a API retorna HTTP 401
- **AND** revoga todos os refresh tokens ativos daquele usuário

### Requirement: Logout com invalidação imediata
A API SHALL expor `POST /api/auth/logout`, que remove o refresh token do banco, apaga o cookie e coloca o `jti` do access token atual numa blacklist em memória até o fim da sua validade.

#### Scenario: Logout invalida a sessão
- **WHEN** um admin autenticado chama `/api/auth/logout`
- **THEN** a API retorna HTTP 204, remove o refresh token do banco e expira o cookie

#### Scenario: Access token usado após logout
- **WHEN** o mesmo access token é usado em um endpoint protegido depois do logout, ainda dentro dos 15 minutos
- **THEN** a API retorna HTTP 401

### Requirement: Troca de senha pelo próprio admin
A API SHALL expor `POST /api/auth/change-password` para o usuário autenticado, exigindo a senha atual e uma nova senha com no mínimo 10 caracteres contendo letras e números, diferente da atual.

#### Scenario: Troca bem-sucedida
- **WHEN** o admin informa a senha atual correta e uma nova senha válida
- **THEN** a API grava o novo hash BCrypt, revoga todos os refresh tokens do usuário e retorna um novo par de tokens

#### Scenario: Senha atual incorreta
- **WHEN** a senha atual informada está errada
- **THEN** a API retorna HTTP 400 sem alterar a senha

#### Scenario: Nova senha fraca
- **WHEN** a nova senha não atende à política
- **THEN** a API retorna HTTP 400 com a mensagem de validação no idioma da requisição

### Requirement: Bloqueio por tentativas de login
O sistema SHALL bloquear a conta por 15 minutos após 5 tentativas de senha errada consecutivas e SHALL limitar o endpoint de login a 5 requisições por minuto por IP.

#### Scenario: Conta bloqueada
- **WHEN** um usuário erra a senha 5 vezes seguidas
- **THEN** as tentativas seguintes nos próximos 15 minutos retornam HTTP 401 mesmo com a senha correta

#### Scenario: Contador zera após sucesso
- **WHEN** o usuário acerta a senha antes de atingir o limite
- **THEN** o contador de falhas volta a zero

#### Scenario: Excesso de requisições
- **WHEN** um mesmo IP envia mais de 5 requisições de login em um minuto
- **THEN** a API retorna HTTP 429

### Requirement: Sem cadastro público de usuários
O sistema SHALL NOT expor endpoint de cadastro de usuários. Administradores SHALL ser criados por uma ferramenta de linha de comando (`LaBlanca.Tools`) que grava o usuário no banco com senha em hash BCrypt.

#### Scenario: Criação de admin via ferramenta
- **WHEN** o operador executa o comando de criação de admin informando tenant, nome, e-mail e senha
- **THEN** o usuário é gravado no banco com papel de administrador e senha em hash BCrypt

#### Scenario: E-mail já existente
- **WHEN** o comando recebe um e-mail já cadastrado no mesmo tenant
- **THEN** a ferramenta encerra com erro e não altera o usuário existente

#### Scenario: Nenhuma rota de registro
- **WHEN** alguém envia `POST /api/auth/register`
- **THEN** a API retorna HTTP 404

### Requirement: Dados do usuário logado
A API SHALL expor `GET /api/auth/me`, protegido, retornando id, nome, e-mail, papel e permissões do usuário do token.

#### Scenario: Consulta autenticada
- **WHEN** um admin autenticado chama `/api/auth/me`
- **THEN** a API retorna HTTP 200 com seus dados

#### Scenario: Consulta sem token
- **WHEN** a chamada é feita sem access token
- **THEN** a API retorna HTTP 401

