## MODIFIED Requirements

### Requirement: Configurações da imobiliária
O admin SHALL poder consultar e alterar via `/api/admin/settings` os dados institucionais: nome comercial, telefone, número de WhatsApp em formato internacional, e-mail, endereço, coordenadas do escritório (sede), horário de atendimento, links de redes sociais (Facebook, Instagram, TikTok, YouTube) e link para avaliação no Google.

#### Scenario: Alteração do WhatsApp
- **WHEN** o admin salva um novo número de WhatsApp válido (ex.: `+595981123456`)
- **THEN** o botão flutuante e os links de WhatsApp do site passam a usar o novo número após a revalidação

#### Scenario: Número em formato inválido
- **WHEN** o número não está em formato internacional com `+` e apenas dígitos
- **THEN** a API retorna HTTP 400

#### Scenario: Link de avaliação do Google
- **WHEN** o admin salva um link HTTPS de um domínio do Google (ex.: `https://g.page/r/abc/review`)
- **THEN** o site passa a mostrar o botão de avaliação usando esse link

#### Scenario: Link de avaliação fora do Google
- **WHEN** o link informado não é HTTPS ou não pertence a um domínio do Google (`g.page`, `google.com`, `goo.gl`, `maps.app.goo.gl`)
- **THEN** a API retorna HTTP 400

### Requirement: Cotações e simulador
As configurações SHALL incluir a cotação de PYG, BRL, EUR e ARS por 1 USD, a data da última atualização das cotações, a taxa anual do simulador de financiamento e a meta mensal de vendas. Alterar qualquer cotação SHALL atualizar a data da atualização.

#### Scenario: Cotação atualizada
- **WHEN** o admin altera a cotação de PYG para 7.450
- **THEN** o site passa a converter preços usando 7.450 e mostra a data da atualização

#### Scenario: Cotação do euro
- **WHEN** o admin informa 0,92 euro por dólar
- **THEN** um imóvel de USD 100.000 aparece como € 92.000 para quem escolhe euro no site

#### Scenario: Cotação inválida
- **WHEN** alguma cotação é zero ou negativa
- **THEN** a API retorna HTTP 400

### Requirement: Zonas de atuação
O admin SHALL poder cadastrar zonas (ex.: Paraná Country Club, Centro CDE, Km 8 / Km 10, Área 1 / Área 4, Hernandarias) com nome, cidade, slug, descrição traduzível e coordenadas de referência opcionais, usadas nos filtros de busca, nos anúncios, nas páginas de zona do site e como posição aproximada no mapa de anúncios sem coordenadas.

#### Scenario: Nova zona
- **WHEN** o admin cria a zona "Hernandarias" com descrição em espanhol
- **THEN** ela aparece nos filtros do site e ganha página própria

#### Scenario: Excluir zona em uso
- **WHEN** o admin tenta excluir uma zona usada por algum anúncio
- **THEN** a API retorna HTTP 409

#### Scenario: Zonas iniciais com coordenadas
- **WHEN** a migração roda
- **THEN** as cinco zonas iniciais recebem coordenadas de referência no centro de cada área

### Requirement: Configurações públicas
A API SHALL expor `GET /api/public/settings` apenas com os campos necessários ao site (contatos, WhatsApp, coordenadas da sede, redes sociais, link de avaliação no Google, URL da foto da sede, cotações de todas as moedas, taxa do simulador, zonas com coordenadas), sem metas comerciais nem dados de usuários.

#### Scenario: Meta não exposta
- **WHEN** um visitante consulta as configurações públicas
- **THEN** a resposta não contém a meta mensal de vendas nem nomes de usuário do painel

## ADDED Requirements

### Requirement: Foto da sede
O admin SHALL poder enviar uma imagem da sede da imobiliária (PNG, JPG, JPEG ou WEBP, até 15 MB) com texto alternativo, trocá-la ou removê-la. A imagem SHALL ser pública e usada pelo site na seção "Tradição e transparência" e na página Sobre nós.

#### Scenario: Envio da foto
- **WHEN** o admin envia `sede.jpg` nas configurações
- **THEN** as configurações públicas passam a ter a URL pública da imagem e o site mostra a foto após a revalidação

#### Scenario: Troca da foto
- **WHEN** o admin envia outra imagem
- **THEN** a anterior é apagada do armazenamento e a nova passa a ser usada

#### Scenario: Arquivo que não é imagem
- **WHEN** o admin envia um `.pdf` como foto da sede
- **THEN** a API retorna HTTP 400

### Requirement: Usuários do painel visíveis
A API SHALL expor `GET /api/admin/users` (permissão de leitura de configurações) com nome de usuário, nome, e-mail opcional, papel, situação (ativo ou inativo) e último acesso dos usuários do tenant, sem hash de senha nem dados de sessão.

#### Scenario: Lista de usuários
- **WHEN** o admin abre Configurações
- **THEN** a API retorna os usuários do painel com o nome de usuário usado no login

#### Scenario: Dados sensíveis
- **WHEN** a lista de usuários é consultada
- **THEN** a resposta não contém hash de senha, contador de falhas nem tokens
