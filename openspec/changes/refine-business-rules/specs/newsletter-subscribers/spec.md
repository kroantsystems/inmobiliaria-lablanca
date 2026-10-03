## ADDED Requirements

### Requirement: Inscrição pública em novidades
A API SHALL expor `POST /api/public/subscribers` sem autenticação para o formulário "Novidades exclusivas" do site, exigindo nome, telefone/WhatsApp com ao menos 8 dígitos e consentimento, com e-mail opcional, gravando o idioma do visitante e a data do consentimento. O endpoint SHALL usar o limite de requisições dos formulários públicos e o campo anti-spam oculto.

#### Scenario: Inscrição válida
- **WHEN** um visitante envia nome, WhatsApp e consentimento
- **THEN** a API retorna HTTP 202 e cria um visitante inscrito ainda não adicionado ao grupo

#### Scenario: Inscrição repetida
- **WHEN** o mesmo telefone (comparando só os dígitos) se inscreve de novo
- **THEN** a API retorna HTTP 202, atualiza nome, e-mail, idioma e data do consentimento do inscrito existente e não cria duplicata

#### Scenario: Sem consentimento
- **WHEN** a inscrição chega sem consentimento
- **THEN** a API retorna HTTP 400

#### Scenario: Robô preenche campo armadilha
- **WHEN** o campo oculto anti-spam vem preenchido
- **THEN** a API retorna HTTP 202 sem gravar nada

### Requirement: Módulo Visitantes no painel
O admin SHALL poder listar (até 500 por página), editar observações, marcar ou desmarcar "adicionado ao grupo de WhatsApp" (gravando a data) e excluir visitantes inscritos via `/api/admin/subscribers`, com permissões próprias de leitura e escrita incluídas no papel de administrador.

#### Scenario: Marcar como adicionado ao grupo
- **WHEN** o admin marca um inscrito como adicionado ao grupo
- **THEN** o inscrito passa a ter a data de entrada no grupo e sai do filtro "Pendentes"

#### Scenario: Excluir inscrito
- **WHEN** o admin exclui um inscrito a pedido da pessoa
- **THEN** a API retorna HTTP 204 e o inscrito não aparece mais

#### Scenario: Acesso sem permissão
- **WHEN** um usuário sem a permissão de leitura de inscritos chama a listagem
- **THEN** a API retorna HTTP 403

### Requirement: Exportação para o grupo de WhatsApp
A API SHALL expor `GET /api/admin/subscribers/export` retornando um CSV em UTF-8 (com BOM, separador `;`) com nome, telefone, e-mail, idioma, data de inscrição e se já está no grupo, com filtro opcional de pendentes.

#### Scenario: Exportar pendentes
- **WHEN** o admin exporta com o filtro de pendentes
- **THEN** o arquivo contém só os inscritos ainda não adicionados ao grupo

#### Scenario: Acentos no Excel
- **WHEN** o CSV é aberto no Excel
- **THEN** nomes com acento aparecem corretamente
