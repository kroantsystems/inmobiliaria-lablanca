## MODIFIED Requirements

### Requirement: Captação pública de contatos
A API SHALL expor `POST /api/public/leads` sem autenticação para os formulários do site, com origem `Contact` (contato), `VisitRequest` (pedido de visita a um anúncio) ou `OwnerProposal` (proprietário oferecendo imóvel), exigindo nome e telefone/WhatsApp, consentimento de contato e registrando o idioma do visitante. A inscrição em novidades SHALL usar a capacidade de visitantes inscritos, não leads.

#### Scenario: Pedido de visita válido
- **WHEN** um visitante envia nome, WhatsApp, consentimento e o id de um anúncio publicado com origem `VisitRequest`
- **THEN** a API retorna HTTP 202 e cria um lead com status `New` vinculado ao anúncio

#### Scenario: Newsletter sem e-mail
- **WHEN** a origem enviada é `Newsletter`, com ou sem e-mail
- **THEN** a API retorna HTTP 400 e nenhum lead é criado, porque a inscrição em novidades usa `/api/public/subscribers`

#### Scenario: Sem consentimento
- **WHEN** o formulário é enviado sem consentimento de contato
- **THEN** a API retorna HTTP 400

#### Scenario: Robô preenche campo armadilha
- **WHEN** o campo oculto anti-spam (honeypot) vem preenchido
- **THEN** a API retorna HTTP 202 sem gravar nenhum lead

#### Scenario: Telefone inválido
- **WHEN** o telefone tem menos de 8 dígitos
- **THEN** a API retorna HTTP 400 com mensagem no idioma do visitante

### Requirement: Gestão de clientes e leads no painel
O admin SHALL poder criar, editar, listar e excluir leads via `/api/admin/leads`, com nome, telefone, e-mail, documento opcional (CI, RUC, CPF ou passaporte, até 30 caracteres), interesse (comprar casa, alugar departamento, comprar terreno, outro), origem, status (`New`, `Contacted`, `VisitScheduled`, `Negotiating`, `Won`, `Lost`), anúncio de interesse opcional e observações.

#### Scenario: Cadastro manual
- **WHEN** o admin cadastra um lead pelo painel
- **THEN** o lead é criado com origem `Manual` e status `New`

#### Scenario: Documento do cliente
- **WHEN** o admin salva o cliente com documento `4.567.890`
- **THEN** o documento aparece no cadastro e na busca da lista de clientes

#### Scenario: Documento longo demais
- **WHEN** o documento tem mais de 30 caracteres
- **THEN** a API retorna HTTP 400

#### Scenario: Mudança de status
- **WHEN** o admin muda o status de um lead para `Negotiating`
- **THEN** o novo status aparece na lista com a data da última atualização

#### Scenario: Listagem ampla
- **WHEN** o painel pede leads com `pageSize=500`
- **THEN** a API retorna até 500 leads ordenados pelos mais recentes com o total disponível

### Requirement: Conversão de proposta em proprietário
O admin SHALL poder converter um lead de origem `OwnerProposal` em cadastro de proprietário, reaproveitando nome, telefone, e-mail e documento, e vinculando ao proprietário o anúncio de interesse do lead, se houver.

#### Scenario: Conversão
- **WHEN** o admin converte uma proposta de proprietário
- **THEN** um proprietário é criado com os dados do lead e o lead passa para `Won`

## ADDED Requirements

### Requirement: Inscritos antigos migrados
A migração SHALL transformar os leads de origem `Newsletter` existentes em visitantes inscritos (nome, telefone, e-mail, idioma, consentimento e data) e removê-los da lista de leads.

#### Scenario: Lead de newsletter existente
- **WHEN** a migração roda com um lead de origem `Newsletter`
- **THEN** existe um visitante inscrito com os mesmos dados e o lead não aparece mais em Clientes / Leads
