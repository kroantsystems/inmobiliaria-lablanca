## ADDED Requirements

### Requirement: Identidade visual do protótipo
O site SHALL seguir a identidade do `proto 19.html`: vermelho `#E31B23`, azul `#005DAA` e azul escuro `#003B6F`, fundo `#F8FAFC`, tipografias Fredoka (títulos) e Plus Jakarta Sans (texto) servidas pelo próprio site, cantos arredondados e cartões com sombra suave.

#### Scenario: Fontes sem dependência externa
- **WHEN** a home é carregada
- **THEN** nenhuma requisição é feita a `fonts.googleapis.com` ou a CDNs de ícones

### Requirement: Logo oficial sem fundo
O site e o painel SHALL usar a logo oficial da imobiliária (bloco vermelho "INMOBILIARIA", bloco azul "La Blanca" e texto "CIUDAD DEL ESTE") sem fundo, a partir de `assets/logo-transparent.*` (fundos claros) e `assets/logo-transparent-white-text.*` (fundos escuros, com "CIUDAD DEL ESTE" em branco). A logo SHALL aparecer no cabeçalho, no rodapé, no modal de login, nas imagens Open Graph e no JSON-LD, sempre com texto alternativo "Inmobiliaria La Blanca – Ciudad del Este" e largura e altura declaradas.

#### Scenario: Logo sobre fundo escuro
- **WHEN** o cabeçalho é exibido sobre o hero escuro
- **THEN** a logo usada é a variante com "CIUDAD DEL ESTE" em branco e todo o texto fica legível

#### Scenario: Sem fundo nem linhas decorativas
- **WHEN** a logo é exibida sobre qualquer cor de fundo
- **THEN** não aparece retângulo cinza nem as linhas decorativas da foto original ao redor dela

#### Scenario: Logo sem deslocar o layout
- **WHEN** a página carrega
- **THEN** a logo ocupa o espaço reservado desde o primeiro render, sem contribuir para o CLS

### Requirement: Favicon e ícones de aplicativo
O site SHALL publicar o ícone oficial (prédio com janelas e casa vermelha) redesenhado em SVG: `favicon.svg` sem fundo, com o prédio azul `#005DAA` em abas de tema claro e branco em abas de tema escuro, e a casa sempre vermelha. A partir dele SHALL gerar `favicon.ico` (16, 32 e 48 px), `apple-touch-icon` de 180 px e ícones de 192 e 512 px (incluindo versão maskable) com fundo azul `#005DAA`, porque esses formatos exigem ícone opaco, além de `manifest.webmanifest` com nome, cor de tema e ícones.

#### Scenario: Aba de tema claro
- **WHEN** o site é aberto em um navegador com tema claro
- **THEN** o favicon mostra o prédio azul e a casa vermelha, visíveis sobre a aba clara

#### Scenario: Aba de tema escuro
- **WHEN** o site é aberto em um navegador com tema escuro
- **THEN** o favicon mostra o prédio branco e a casa vermelha

#### Scenario: Atalho na tela inicial do celular
- **WHEN** o visitante adiciona o site à tela inicial no iOS ou Android
- **THEN** o ícone exibido é o de fundo azul com o prédio branco e a casa vermelha, sem bordas cortadas

### Requirement: Barra superior
A barra superior SHALL conter o botão "Login", o botão "Publicar meu imóvel", o seletor de idioma e o seletor de moeda (USD, PYG, BRL). A escolha de moeda SHALL ser lembrada no navegador.

#### Scenario: Troca de moeda
- **WHEN** o visitante escolhe PYG
- **THEN** todos os preços da página são convertidos com a cotação configurada e exibidos com ₲
- **AND** um aviso indica que a conversão é aproximada e mostra a data da cotação

#### Scenario: Moeda lembrada
- **WHEN** o visitante volta ao site depois de escolher BRL
- **THEN** os preços aparecem em BRL

### Requirement: Modal de login
O botão "Login" SHALL abrir um modal pequeno com e-mail e senha, sem sair da página. Em caso de sucesso SHALL redirecionar para `/{locale}/admin`; em caso de erro SHALL mostrar a mensagem retornada pela API no idioma atual.

#### Scenario: Login com sucesso
- **WHEN** o admin preenche credenciais válidas no modal
- **THEN** o modal fecha e o navegador abre o dashboard

#### Scenario: Credenciais erradas
- **WHEN** as credenciais são inválidas
- **THEN** o modal continua aberto com mensagem de erro genérica e a senha é limpa

#### Scenario: Acessibilidade do modal
- **WHEN** o modal abre
- **THEN** o foco vai para o campo de e-mail, `Esc` fecha o modal e o foco não sai do modal com `Tab`

### Requirement: Home
A home SHALL conter: cabeçalho com logo e menu (Início, Imóveis, Localizações, Sobre nós, Simulador) e botão de contato VIP; hero com título, subtítulo, botões para imóveis, simulador e WhatsApp e cartão do destaque do mês; caixa de busca; grade de imóveis em destaque; mapa; seção institucional com números; simulador; perguntas frequentes; e rodapé com zonas, navegação e newsletter.

#### Scenario: Conteúdo no HTML inicial
- **WHEN** a home é pedida sem JavaScript
- **THEN** o HTML retornado já contém títulos, destaque e cartões dos imóveis

#### Scenario: Sem destaque cadastrado
- **WHEN** não existe anúncio publicado
- **THEN** a home mostra uma mensagem de catálogo em atualização com botão de contato, sem quebrar o layout

### Requirement: Busca e catálogo
A busca SHALL ter abas Comprar e Alugar e filtros de tipo, zona, faixa de preço e quartos, e SHALL levar para a página de catálogo `/{locale}/{imoveis}` com os filtros na URL, renderizada no servidor, com ordenação e paginação por links.

#### Scenario: Filtros na URL
- **WHEN** o visitante busca casas para comprar em Hernandarias
- **THEN** a URL contém os filtros escolhidos e pode ser compartilhada mostrando o mesmo resultado

#### Scenario: Nenhum resultado
- **WHEN** a busca não encontra imóveis
- **THEN** a página mostra sugestão de limpar filtros e botão de contato pelo WhatsApp

### Requirement: Página de detalhe do imóvel
Cada anúncio publicado SHALL ter página própria `/{locale}/{imoveis}/{slug}` com galeria, preço convertível, características, descrição, vídeo, mapa da localização, botão de WhatsApp com mensagem pré-preenchida com o título do anúncio, formulário de pedido de visita e imóveis semelhantes.

#### Scenario: Pedido de visita pelo anúncio
- **WHEN** o visitante envia o formulário de visita na página do anúncio
- **THEN** é criado um lead `VisitRequest` vinculado ao anúncio e o visitante vê confirmação no idioma atual

#### Scenario: Anúncio indisponível
- **WHEN** o visitante acessa o slug de um anúncio vendido ou despublicado
- **THEN** a página retorna HTTP 404 com sugestões de imóveis semelhantes

### Requirement: Mapa só com imóveis La Blanca
O site SHALL exibir um mapa interativo com marcadores apenas dos imóveis publicados, carregado sob demanda, com base cartográfica configurável e com cada marcador levando à página do anúncio.

#### Scenario: Mapa carregado sob demanda
- **WHEN** a home é carregada
- **THEN** o código do mapa só é baixado quando a seção do mapa se aproxima da área visível

#### Scenario: Clique no marcador
- **WHEN** o visitante clica em um marcador
- **THEN** aparece um resumo com foto, preço e link para o anúncio

### Requirement: Simulador de parcelas
O simulador SHALL calcular a parcela mensal pela tabela Price usando a taxa anual configurada, com valor do imóvel e prazo de 5, 10, 15 ou 20 anos, exibindo o resultado na moeda escolhida e um aviso de que é uma estimativa.

#### Scenario: Cálculo
- **WHEN** o valor é USD 100.000, prazo 10 anos e taxa 8% ao ano
- **THEN** a parcela exibida é USD 1.213 aproximadamente

#### Scenario: Valor inválido
- **WHEN** o valor está vazio ou é zero
- **THEN** o resultado mostra "—" em vez de número

### Requirement: Formulários públicos
O site SHALL oferecer os formulários de contato VIP, pedido de visita, proposta de proprietário ("Publicar meu imóvel") e newsletter, todos com validação no cliente, consentimento de contato, campo anti-spam oculto e mensagem de confirmação no idioma atual.

#### Scenario: Proposta de proprietário
- **WHEN** um proprietário envia nome e WhatsApp pelo modal "Publicar meu imóvel"
- **THEN** é criado um lead `OwnerProposal` e o modal mostra a confirmação

#### Scenario: Erro de envio
- **WHEN** a API retorna erro ou limite de requisições
- **THEN** o formulário mostra a mensagem de erro e mantém os dados preenchidos

### Requirement: WhatsApp flutuante
O site SHALL exibir um botão flutuante de WhatsApp em todas as páginas públicas, usando o número das configurações e mensagem inicial no idioma atual, e SHALL registrar o evento `WhatsAppClick`.

#### Scenario: Clique no WhatsApp
- **WHEN** o visitante clica no botão flutuante
- **THEN** abre `https://wa.me/<numero>` em nova aba com `rel="noopener"` e o evento é registrado

### Requirement: Páginas institucionais e legais
O site SHALL ter páginas Sobre nós, Contato, Perguntas frequentes e Política de privacidade em todos os idiomas.

#### Scenario: Política de privacidade acessível
- **WHEN** o visitante abre um formulário
- **THEN** o texto de consentimento tem link para a política de privacidade

### Requirement: Layout responsivo e acessível
Todas as páginas públicas SHALL funcionar de 360 px de largura até desktop sem rolagem horizontal, com menu móvel, contraste AA, textos alternativos em imagens e navegação por teclado.

#### Scenario: Celular
- **WHEN** a home é aberta com 375 px de largura
- **THEN** busca, cartões, simulador e rodapé ficam em uma coluna sem rolagem horizontal
