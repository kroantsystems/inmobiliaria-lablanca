## ADDED Requirements

### Requirement: Captação pública de contatos
A API SHALL expor `POST /api/public/leads` sem autenticação para os formulários do site, com origem `Contact` (contato VIP), `VisitRequest` (pedido de visita a um anúncio), `OwnerProposal` (proprietário oferecendo imóvel) ou `Newsletter`, exigindo nome e telefone/WhatsApp (e-mail obrigatório apenas na newsletter), consentimento de contato e registrando o idioma do visitante.

#### Scenario: Pedido de visita válido
- **WHEN** um visitante envia nome, WhatsApp, consentimento e o id de um anúncio publicado com origem `VisitRequest`
- **THEN** a API retorna HTTP 202 e cria um lead com status `New` vinculado ao anúncio

#### Scenario: Newsletter sem e-mail
- **WHEN** a origem é `Newsletter` e o e-mail está vazio
- **THEN** a API retorna HTTP 400

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
O admin SHALL poder criar, editar, listar e excluir leads via `/api/admin/leads`, com nome, telefone, e-mail, interesse (comprar casa, alugar departamento, comprar terreno, outro), origem, status (`New`, `Contacted`, `VisitScheduled`, `Negotiating`, `Won`, `Lost`), anúncio de interesse opcional e observações.

#### Scenario: Cadastro manual
- **WHEN** o admin cadastra um lead pelo painel
- **THEN** o lead é criado com origem `Manual` e status `New`

#### Scenario: Mudança de status
- **WHEN** o admin muda o status de um lead para `Negotiating`
- **THEN** o novo status aparece na lista com a data da última atualização

#### Scenario: Listagem ampla
- **WHEN** o painel pede leads com `pageSize=500`
- **THEN** a API retorna até 500 leads ordenados pelos mais recentes com o total disponível

### Requirement: Conversão de proposta em proprietário
O admin SHALL poder converter um lead de origem `OwnerProposal` em cadastro de proprietário, reaproveitando nome, telefone e e-mail.

#### Scenario: Conversão
- **WHEN** o admin converte uma proposta de proprietário
- **THEN** um proprietário é criado com os dados do lead e o lead passa para `Won`

### Requirement: Dados pessoais protegidos
Leads SHALL ser acessíveis apenas por usuários autenticados com permissão `leads.read`, e os endpoints públicos SHALL responder sem devolver dados de outros leads.

#### Scenario: Resposta pública mínima
- **WHEN** um visitante envia um formulário
- **THEN** a resposta não contém id nem dados de qualquer lead
