## ADDED Requirements

### Requirement: Cadastro de perguntas frequentes
O admin SHALL poder criar, editar, reordenar, ativar ou desativar e excluir perguntas frequentes via `/api/admin/faqs`, cada uma com ordem e traduções de pergunta e resposta por idioma (`es`, `pt`, `en`, `gn`). A tradução em espanhol SHALL ser obrigatória; pergunta até 200 caracteres e resposta até 2.000.

#### Scenario: Nova pergunta
- **WHEN** o admin cria uma pergunta com textos em espanhol e português
- **THEN** a API retorna HTTP 201 e a pergunta aparece no site nesses idiomas após a revalidação

#### Scenario: Sem espanhol
- **WHEN** a pergunta não tem tradução em espanhol
- **THEN** a API retorna HTTP 400

#### Scenario: Pergunta desativada
- **WHEN** o admin desativa uma pergunta
- **THEN** ela continua no painel e deixa de aparecer no site

### Requirement: Perguntas padrão
A migração SHALL gravar no banco, para o tenant inicial, quatro perguntas ativas nesta ordem, com textos em espanhol, português e inglês (guarani igual ao espanhol até a tradução): "Como agendo uma visita?", "Como ofereço meu imóvel para venda ou aluguel?", "O simulador de parcelas é uma oferta de crédito?" e "Vocês atendem compradores do Brasil e de outros países?".

#### Scenario: Banco novo
- **WHEN** as migrações rodam em um banco vazio
- **THEN** existem exatamente essas quatro perguntas, ativas, nos quatro idiomas

### Requirement: Perguntas frequentes públicas
A API SHALL expor `GET /api/public/faqs?locale=` com as perguntas ativas em ordem, no idioma pedido ou, na falta dele, em espanhol, e SHALL notificar a revalidação do site (tag de perguntas frequentes) a cada alteração.

#### Scenario: Idioma sem tradução
- **WHEN** o site pede as perguntas em inglês e uma delas só tem espanhol
- **THEN** essa pergunta volta em espanhol e as demais em inglês

#### Scenario: Alteração reflete no site
- **WHEN** o admin edita uma resposta
- **THEN** a API chama a revalidação com a tag de perguntas frequentes
