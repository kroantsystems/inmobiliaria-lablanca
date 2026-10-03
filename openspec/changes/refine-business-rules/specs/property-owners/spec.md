## MODIFIED Requirements

### Requirement: Cadastro de proprietários
O admin SHALL poder criar, editar, listar e excluir proprietários via `/api/admin/owners`, com nome, telefone/WhatsApp, e-mail opcional, documento opcional, interesse principal (`Sell` vender, `Rent` alugar, `SellOrRent` vender ou alugar, `Other` outro), anúncios vinculados e observações.

#### Scenario: Cadastro válido
- **WHEN** o admin envia nome, telefone válido e interesse `Rent`
- **THEN** a API retorna HTTP 201 com o proprietário criado e o interesse informado

#### Scenario: Interesse padrão
- **WHEN** o proprietário é criado sem interesse
- **THEN** o interesse fica como `Other`

#### Scenario: Nome ausente
- **WHEN** o nome está vazio
- **THEN** a API retorna HTTP 400

### Requirement: Imóveis captados por proprietário
A listagem de proprietários SHALL mostrar os anúncios vinculados a cada um. O vínculo SHALL poder ser feito tanto no cadastro do anúncio quanto no cadastro do proprietário; cada anúncio SHALL ter no máximo um proprietário.

#### Scenario: Proprietário com imóveis
- **WHEN** dois anúncios apontam para o mesmo proprietário
- **THEN** a listagem mostra os dois títulos na coluna de imóveis captados

#### Scenario: Vínculo pelo cadastro do proprietário
- **WHEN** o admin salva o proprietário escolhendo os anúncios A e B
- **THEN** A e B passam a ter esse proprietário, e anúncios que estavam vinculados a ele e não foram escolhidos ficam sem proprietário

#### Scenario: Anúncio de outro proprietário
- **WHEN** o admin escolhe um anúncio que já pertence a outro proprietário
- **THEN** o anúncio passa para o novo proprietário e sai da lista do anterior

#### Scenario: Anúncio inexistente
- **WHEN** a lista de anúncios contém um id que não existe no tenant
- **THEN** a API retorna HTTP 400 e nenhum vínculo é alterado
