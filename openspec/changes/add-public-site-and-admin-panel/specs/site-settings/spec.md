## ADDED Requirements

### Requirement: Configurações da imobiliária
O admin SHALL poder consultar e alterar via `/api/admin/settings` os dados institucionais: nome comercial, telefone, número de WhatsApp em formato internacional, e-mail, endereço, coordenadas do escritório, horário de atendimento e links de redes sociais.

#### Scenario: Alteração do WhatsApp
- **WHEN** o admin salva um novo número de WhatsApp válido (ex.: `+595981123456`)
- **THEN** o botão flutuante e os links de WhatsApp do site passam a usar o novo número após a revalidação

#### Scenario: Número em formato inválido
- **WHEN** o número não está em formato internacional com `+` e apenas dígitos
- **THEN** a API retorna HTTP 400

### Requirement: Cotações e simulador
As configurações SHALL incluir a cotação de PYG e BRL por 1 USD, a data da última atualização das cotações, a taxa anual do simulador de financiamento e a meta mensal de vendas.

#### Scenario: Cotação atualizada
- **WHEN** o admin altera a cotação de PYG para 7.450
- **THEN** o site passa a converter preços usando 7.450 e mostra a data da atualização

#### Scenario: Cotação inválida
- **WHEN** a cotação é zero ou negativa
- **THEN** a API retorna HTTP 400

### Requirement: Zonas de atuação
O admin SHALL poder cadastrar zonas (ex.: Paraná Country Club, Centro CDE, Km 8 / Km 10, Área 1 / Área 4, Hernandarias) com nome, cidade, slug e descrição traduzível, usadas nos filtros de busca, nos anúncios e nas páginas de zona do site.

#### Scenario: Nova zona
- **WHEN** o admin cria a zona "Hernandarias" com descrição em espanhol
- **THEN** ela aparece nos filtros do site e ganha página própria

#### Scenario: Excluir zona em uso
- **WHEN** o admin tenta excluir uma zona usada por algum anúncio
- **THEN** a API retorna HTTP 409

### Requirement: Configurações públicas
A API SHALL expor `GET /api/public/settings` apenas com os campos necessários ao site (contatos, WhatsApp, redes sociais, cotações, taxa do simulador, zonas), sem metas comerciais.

#### Scenario: Meta não exposta
- **WHEN** um visitante consulta as configurações públicas
- **THEN** a resposta não contém a meta mensal de vendas
