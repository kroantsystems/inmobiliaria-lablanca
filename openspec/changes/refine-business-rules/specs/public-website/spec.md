## MODIFIED Requirements

### Requirement: Barra superior
A barra superior SHALL conter o botão "Login", o botão "Publicar meu imóvel", o seletor de idioma e o seletor de moeda (USD, PYG, BRL, EUR, ARS). A escolha de moeda SHALL ser lembrada no navegador.

#### Scenario: Troca de moeda
- **WHEN** o visitante escolhe PYG
- **THEN** todos os preços da página são convertidos com a cotação configurada e exibidos com ₲
- **AND** um aviso indica que a conversão é aproximada e mostra a data da cotação

#### Scenario: Peso argentino
- **WHEN** o visitante escolhe ARS
- **THEN** os preços aparecem convertidos com o prefixo `AR$`

#### Scenario: Moeda lembrada
- **WHEN** o visitante volta ao site depois de escolher BRL
- **THEN** os preços aparecem em BRL

### Requirement: Modal de login
O botão "Login" SHALL abrir um modal pequeno com usuário e senha, sem sair da página. Em caso de sucesso SHALL redirecionar para `/{locale}/admin`; em caso de erro SHALL mostrar a mensagem retornada pela API no idioma atual.

#### Scenario: Login com sucesso
- **WHEN** o admin preenche usuário e senha válidos no modal
- **THEN** o modal fecha e o navegador abre o dashboard

#### Scenario: Credenciais erradas
- **WHEN** as credenciais são inválidas
- **THEN** o modal continua aberto com mensagem de erro genérica e a senha é limpa

#### Scenario: Acessibilidade do modal
- **WHEN** o modal abre
- **THEN** o foco vai para o campo de usuário, `Esc` fecha o modal e o foco não sai do modal com `Tab`

### Requirement: Home
A home SHALL conter: cabeçalho com logo e menu (Início, Imóveis, Localizações, Sobre nós, Simulador, Contato), sem botão de contato VIP; hero com título, subtítulo, botões para imóveis e simulador e cartão do destaque do mês; caixa de busca; grade de imóveis em destaque (incluindo o destaque do mês); mapa; seção institucional com números e a foto da sede; simulador; perguntas frequentes cadastradas no painel; e rodapé com zonas, navegação, redes sociais, avaliação no Google e newsletter.

#### Scenario: Conteúdo no HTML inicial
- **WHEN** a home é pedida sem JavaScript
- **THEN** o HTML retornado já contém títulos, destaque e cartões dos imóveis

#### Scenario: Botões removidos
- **WHEN** a home é exibida em qualquer tamanho de tela
- **THEN** não existem os botões "Atendimento VIP" no cabeçalho nem "Dúvidas pelo WhatsApp" no hero, e o WhatsApp flutuante continua visível

#### Scenario: Destaque do mês no topo
- **WHEN** existe um destaque do mês
- **THEN** o cartão do topo mostra esse anúncio; sem destaque do mês, mostra o destaque mais recente

#### Scenario: Foto da sede
- **WHEN** a foto da sede está configurada
- **THEN** a seção "Tradição e transparência" mostra essa foto; sem ela, mostra a foto de um anúncio ou o ícone da marca

#### Scenario: Sem destaque cadastrado
- **WHEN** não existe anúncio publicado
- **THEN** a home mostra uma mensagem de catálogo em atualização com botão de contato, sem quebrar o layout

### Requirement: Página de detalhe do imóvel
Cada anúncio publicado SHALL ter página própria `/{locale}/{imoveis}/{slug}` com galeria, preço convertível, dados do imóvel (quartos, banheiros, áreas, vagas, capacidade, terreno exclusivo ou compartilhado, piscina, mobília, ar-condicionado, pets com observação), características, descrição, vídeo, mapa da localização (avisando quando é aproximada), botão de WhatsApp com mensagem pré-preenchida com o título do anúncio, formulário de pedido de visita e imóveis semelhantes. Dados não informados SHALL ficar ocultos.

#### Scenario: Dados do imóvel
- **WHEN** o anúncio tem capacidade 2, mobiliado e aceita pets "somente pequeno porte"
- **THEN** a página mostra "2 pessoas", "Mobiliado" e "Aceita pets – somente pequeno porte" no idioma atual

#### Scenario: Dado não informado
- **WHEN** o anúncio não informa piscina
- **THEN** a página não mostra nada sobre piscina

#### Scenario: Pedido de visita pelo anúncio
- **WHEN** o visitante envia o formulário de visita na página do anúncio
- **THEN** é criado um lead `VisitRequest` vinculado ao anúncio e o visitante vê confirmação no idioma atual

#### Scenario: Anúncio indisponível
- **WHEN** o visitante acessa o slug de um anúncio vendido, despublicado ou excluído
- **THEN** a página retorna HTTP 404 com sugestões de imóveis semelhantes

### Requirement: Mapa só com imóveis La Blanca
O site SHALL exibir um mapa interativo com marcadores de todos os imóveis publicados (com coordenadas próprias ou aproximadas pela zona) e da sede da imobiliária, carregado sob demanda, com base cartográfica configurável. Cada marcador de imóvel SHALL levar à página do anúncio; o marcador da sede SHALL ter ícone e cor próprios, aparecer acima dos demais e mostrar nome, endereço e link de rota.

#### Scenario: Mapa carregado sob demanda
- **WHEN** a home é carregada
- **THEN** o código do mapa só é baixado quando a seção do mapa se aproxima da área visível

#### Scenario: Clique no marcador
- **WHEN** o visitante clica em um marcador de imóvel
- **THEN** aparece um resumo com foto, preço e link para o anúncio, e "localização aproximada" quando for o caso

#### Scenario: Sede no mapa
- **WHEN** as coordenadas da sede estão configuradas
- **THEN** o mapa da home e o da página de contato mostram o marcador da sede, em destaque

#### Scenario: Anúncio sem coordenadas
- **WHEN** um anúncio publicado não tem coordenadas mas sua zona tem
- **THEN** ele aparece no mapa na posição da zona

### Requirement: Formulários públicos
O site SHALL oferecer os formulários de contato, pedido de visita, proposta de proprietário ("Publicar meu imóvel") e inscrição em novidades, todos com validação no cliente, consentimento de contato, campo anti-spam oculto e mensagem de confirmação no idioma atual. A inscrição em novidades SHALL pedir nome e WhatsApp (e-mail opcional), avisar que as novidades chegam por WhatsApp e criar um visitante inscrito.

#### Scenario: Proposta de proprietário
- **WHEN** um proprietário envia nome e WhatsApp pelo modal "Publicar meu imóvel"
- **THEN** é criado um lead `OwnerProposal` e o modal mostra a confirmação

#### Scenario: Inscrição em novidades
- **WHEN** um visitante se inscreve no rodapé com nome e WhatsApp
- **THEN** ele aparece no módulo Visitantes do painel e vê a confirmação

#### Scenario: Erro de envio
- **WHEN** a API retorna erro ou limite de requisições
- **THEN** o formulário mostra a mensagem de erro e mantém os dados preenchidos

### Requirement: Páginas institucionais e legais
O site SHALL ter páginas Sobre nós, Contato, Perguntas frequentes e Política de privacidade em todos os idiomas. A página de perguntas frequentes SHALL listar as perguntas ativas cadastradas no painel; Sobre nós SHALL mostrar a foto da sede quando configurada; Contato SHALL mostrar as redes sociais preenchidas e o botão de avaliação no Google. A política de privacidade SHALL informar que as novidades são enviadas por grupo de WhatsApp.

#### Scenario: Política de privacidade acessível
- **WHEN** o visitante abre um formulário
- **THEN** o texto de consentimento tem link para a política de privacidade

#### Scenario: FAQ editada no painel
- **WHEN** o admin altera uma pergunta frequente
- **THEN** a página de perguntas frequentes mostra o novo texto após a revalidação

## ADDED Requirements

### Requirement: Avaliação no Google
Quando o link de avaliação estiver configurado, o site SHALL mostrar o botão "Avalie-nos no Google" no rodapé, na página de contato e na confirmação dos formulários de contato e de visita, abrindo o link em nova aba com `rel="noopener"`. Sem link configurado, o botão SHALL ficar oculto.

#### Scenario: Link configurado
- **WHEN** o admin configurou o link de avaliação
- **THEN** o rodapé mostra o botão com estrelas e o texto no idioma atual

#### Scenario: Link vazio
- **WHEN** o link não está configurado
- **THEN** nenhum botão de avaliação aparece

### Requirement: Botões das redes sociais
O site SHALL mostrar no rodapé e na página de contato um botão com o ícone de cada rede social preenchida nas configurações (Facebook, Instagram, YouTube, TikTok), com nome acessível, abrindo em nova aba com `rel="noopener"`. Redes não preenchidas SHALL ficar ocultas.

#### Scenario: Só Instagram e YouTube
- **WHEN** apenas Instagram e YouTube estão preenchidos
- **THEN** o rodapé mostra apenas esses dois ícones, cada um com o nome da rede para leitores de tela
