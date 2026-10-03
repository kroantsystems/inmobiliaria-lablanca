## MODIFIED Requirements

### Requirement: Layout do painel
O painel em `/{locale}/admin` SHALL seguir o `dashboard.html`: barra lateral escura fixa com o ícone oficial sem fundo (`brand-mark.svg`, prédio branco e casa vermelha) ao lado de "LA BLANCA / ADMINISTRACIÓN" e menu (Dashboard, Agenda / Calendário, Imagens e Documentos, Anúncios & Mídia, Clientes / Leads, Proprietários, Visitantes, Configurações), topo com título da seção, saudação, seletor de idioma e menu do usuário (Minha conta, Sair). Em telas menores que 1024 px a barra lateral SHALL mostrar só ícones, mantendo o ícone da marca.

#### Scenario: Item ativo no menu
- **WHEN** o admin abre a seção Agenda
- **THEN** o item Agenda aparece destacado em azul e o título do topo muda para "Agenda & Calendário de Visitas" no idioma atual

#### Scenario: Tela de tablet
- **WHEN** o painel é aberto com 900 px de largura
- **THEN** a barra lateral mostra apenas o ícone da marca e os ícones do menu, e o conteúdo ocupa o restante da tela

### Requirement: Dashboard
A seção Dashboard SHALL mostrar um seletor de período (mês atual, mês anterior, últimos 7, 30 e 90 dias, ano atual e intervalo personalizado), com o mês atual como padrão e o período escolhido refletido na URL, e para esse período os cartões de indicadores (acessos, cliques em anúncios, vendas com meta, aluguéis ativos, leads novos) com variação em relação ao período anterior, o gráfico de barras de tráfego, o ranking dos anúncios mais clicados e os próximos compromissos da agenda.

#### Scenario: Padrão
- **WHEN** o admin abre o dashboard
- **THEN** os indicadores, o gráfico e o ranking mostram o mês atual

#### Scenario: Gráfico de 30 dias
- **WHEN** o admin escolhe "últimos 30 dias"
- **THEN** o gráfico mostra 30 barras com rótulos de data no idioma atual

#### Scenario: Troca de período
- **WHEN** o admin escolhe "últimos 90 dias"
- **THEN** cartões, gráfico e ranking são recarregados para esse período e o gráfico mostra uma barra por dia com rótulos de data no idioma atual

#### Scenario: Intervalo personalizado inválido
- **WHEN** a data inicial escolhida é posterior à final
- **THEN** o painel mostra o erro sem chamar a API

### Requirement: Anúncios & Mídia
A seção SHALL listar os anúncios e oferecer formulário de criação e edição com abas de idioma para título, descrição e observação sobre pets, campos do imóvel (incluindo capacidade, terreno exclusivo ou compartilhado, piscina, mobília, ar-condicionado e aceita pets, com opção "não informado"), mapa para marcar a localização com um clique (preenchendo latitude e longitude), link de vídeo, galeria com upload múltiplo, pré-visualização, ordenação por arrastar, escolha da capa e texto alternativo, e ações de publicar, despublicar, destacar, marcar como destaque do mês, mudar status, arquivar e excluir. A lista SHALL indicar qual anúncio é o destaque do mês.

#### Scenario: Publicar a partir do formulário
- **WHEN** o admin salva um anúncio com imagem e clica em "Publicar"
- **THEN** o anúncio aparece como publicado na lista e o link "Ver no site" abre a página pública

#### Scenario: Marcar no mapa
- **WHEN** o admin clica em um ponto do mapa do formulário
- **THEN** latitude e longitude são preenchidas com o ponto clicado e o marcador vai para lá

#### Scenario: Destaque do mês
- **WHEN** o admin marca um anúncio como destaque do mês
- **THEN** o painel avisa que o destaque do mês anterior será substituído e, após confirmar, a lista mostra o novo destaque do mês

#### Scenario: Excluir anúncio
- **WHEN** o admin clica em excluir
- **THEN** o painel pede confirmação explicando que fotos e vídeos serão apagados e que arquivar mantém o histórico, e só então exclui

### Requirement: Clientes / Leads e Proprietários
As seções SHALL ter formulário de cadastro e tabela com nome, contato, documento, interesse ou imóveis captados e status, com edição em painel lateral, mudança rápida de status, link de WhatsApp para o contato e conversão de proposta de proprietário. O formulário de proprietário SHALL ter interesse principal e seleção dos anúncios vinculados. O painel lateral de cliente e de proprietário SHALL listar os arquivos vinculados à pessoa, com download.

#### Scenario: Contato por WhatsApp
- **WHEN** o admin clica no ícone de WhatsApp de um lead
- **THEN** abre conversa com o número do lead em nova aba

#### Scenario: Vincular anúncios ao proprietário
- **WHEN** o admin escolhe dois anúncios no formulário do proprietário e salva
- **THEN** a tabela mostra os dois anúncios em imóveis captados

#### Scenario: Documentos da pessoa
- **WHEN** o admin abre o cadastro de um cliente com um documento vinculado
- **THEN** o painel lateral lista o arquivo com opção de download

### Requirement: Agenda / Calendário
A seção SHALL mostrar o formulário "Agendar compromisso" com tipo (visita com cliente, avaliação com proprietário, outro), anúncio (obrigatório só para visita com cliente), cliente, proprietário, local, data, hora e duração alinhados na mesma linha em telas largas (e empilhados em telas estreitas) e observações; a lista de próximos compromissos; e um calendário mensal estilo Google Calendar com navegação entre meses, destaque do dia atual, compromissos como etiquetas coloridas por status (com o anúncio ou, sem anúncio, o nome do proprietário ou cliente) e detalhe ao clicar, onde o admin SHALL poder alterar todos os campos, mudar o status ou excluir o compromisso.

#### Scenario: Navegar entre meses
- **WHEN** o admin clica na seta para o próximo mês
- **THEN** o calendário carrega os compromissos do novo mês e atualiza o título

#### Scenario: Visita criada aparece no calendário
- **WHEN** o admin agenda uma visita para o dia 20
- **THEN** a etiqueta aparece no dia 20 e na lista de próximos compromissos sem recarregar a página

#### Scenario: Avaliação com proprietário
- **WHEN** o admin escolhe "avaliação com proprietário", um proprietário e nenhum anúncio
- **THEN** o compromisso é salvo e a etiqueta mostra o nome do proprietário

#### Scenario: Campos alinhados
- **WHEN** o formulário é exibido com largura suficiente
- **THEN** data, hora e duração ficam na mesma linha, com rótulos e campos na mesma altura

#### Scenario: Excluir compromisso
- **WHEN** o admin exclui um compromisso pelo detalhe e confirma
- **THEN** ele some do calendário e dos próximos compromissos sem recarregar a página

### Requirement: Imagens e Documentos
A seção SHALL permitir enviar arquivos escolhendo o anúncio vinculado, o cliente e/ou o proprietário (ou "Sem vínculo") e a descrição, e SHALL mostrar os arquivos em grade com pré-visualização para imagens, ícone por tipo para documentos e vídeos, etiquetas dos vínculos, descrição, download e exclusão com confirmação, com filtro por tipo de vínculo (anúncio, cliente, proprietário, sem vínculo).

#### Scenario: Pré-visualização de documento
- **WHEN** o arquivo é um `.pdf`
- **THEN** a grade mostra o ícone de PDF, o nome e as etiquetas dos vínculos

#### Scenario: Filtro por pessoa
- **WHEN** o admin filtra por "Cliente"
- **THEN** só aparecem arquivos vinculados a algum cliente

### Requirement: Configurações
A seção Configurações SHALL permitir editar os dados institucionais, o link de avaliação no Google, as cotações de todas as moedas, a taxa do simulador, a meta mensal, a foto da sede (com pré-visualização, troca e remoção), as zonas (com coordenadas de referência marcadas no mapa) e as perguntas frequentes (lista ordenável com edição em painel lateral por idioma, como as zonas, e opção de ativar ou desativar), e SHALL mostrar os usuários do painel com o nome de usuário usado no login, somente para leitura.

#### Scenario: Salvar cotações
- **WHEN** o admin salva novas cotações
- **THEN** o painel mostra confirmação e a data da atualização

#### Scenario: Nova pergunta frequente
- **WHEN** o admin cria uma pergunta com textos em espanhol e português
- **THEN** ela aparece na lista de perguntas e no site após a revalidação

#### Scenario: Usuários do painel
- **WHEN** o admin abre Configurações
- **THEN** vê a lista de usuários com nome de usuário, nome e último acesso, sem opção de editar

## ADDED Requirements

### Requirement: Visitantes
A seção Visitantes SHALL listar os inscritos em novidades com nome, WhatsApp, e-mail, idioma, data de inscrição e situação no grupo, com busca com debounce, filtro (pendentes, no grupo, todos), ordenação por coluna no cliente, link de WhatsApp, marcação rápida "adicionado ao grupo", edição de observações, exclusão com confirmação e botão para exportar CSV.

#### Scenario: Marcar vários
- **WHEN** o admin marca três inscritos como adicionados ao grupo
- **THEN** eles saem do filtro "Pendentes" sem recarregar a página

#### Scenario: Exportar
- **WHEN** o admin clica em exportar com o filtro "Pendentes"
- **THEN** o navegador baixa o CSV só com os pendentes
