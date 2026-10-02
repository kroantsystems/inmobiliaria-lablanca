## ADDED Requirements

### Requirement: Cadastro de proprietários
O admin SHALL poder criar, editar, listar e excluir proprietários via `/api/admin/owners`, com nome, telefone/WhatsApp, e-mail opcional, documento opcional e observações.

#### Scenario: Cadastro válido
- **WHEN** o admin envia nome e telefone válidos
- **THEN** a API retorna HTTP 201 com o proprietário criado

#### Scenario: Nome ausente
- **WHEN** o nome está vazio
- **THEN** a API retorna HTTP 400

### Requirement: Imóveis captados por proprietário
A listagem de proprietários SHALL mostrar os anúncios vinculados a cada um, e o vínculo SHALL ser feito no cadastro do anúncio.

#### Scenario: Proprietário com imóveis
- **WHEN** dois anúncios apontam para o mesmo proprietário
- **THEN** a listagem mostra os dois títulos na coluna de imóveis captados

### Requirement: Exclusão protegida
O sistema SHALL impedir a exclusão de proprietário que ainda tenha anúncios não arquivados vinculados.

#### Scenario: Proprietário com anúncio ativo
- **WHEN** o admin tenta excluir um proprietário com anúncio `Available`
- **THEN** a API retorna HTTP 409

#### Scenario: Proprietário sem anúncios ativos
- **WHEN** o proprietário só tem anúncios arquivados ou nenhum
- **THEN** a exclusão é concluída e os anúncios arquivados ficam sem proprietário
