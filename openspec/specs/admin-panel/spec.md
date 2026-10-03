# admin-panel Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Layout do painel
O painel em `/{locale}/admin` SHALL seguir o `dashboard.html`: barra lateral escura fixa com o ícone oficial sem fundo (`brand-mark.svg`, prédio branco e casa vermelha) ao lado de "LA BLANCA / ADMINISTRACIÓN" e menu (Dashboard, Agenda / Calendário, Imagens e Documentos, Anúncios & Mídia, Clientes / Leads, Proprietários, Configurações), topo com título da seção, saudação, seletor de idioma e menu do usuário (Minha conta, Sair). Em telas menores que 1024 px a barra lateral SHALL mostrar só ícones, mantendo o ícone da marca.

#### Scenario: Item ativo no menu
- **WHEN** o admin abre a seção Agenda
- **THEN** o item Agenda aparece destacado em azul e o título do topo muda para "Agenda & Calendário de Visitas" no idioma atual

#### Scenario: Tela de tablet
- **WHEN** o painel é aberto com 900 px de largura
- **THEN** a barra lateral mostra apenas o ícone da marca e os ícones do menu, e o conteúdo ocupa o restante da tela

### Requirement: Proteção das rotas do painel
As páginas do painel SHALL exigir sessão válida. Ao abrir o painel sem access token em memória, o cliente SHALL tentar renovar a sessão uma vez; se falhar, SHALL voltar para a home do idioma atual com o modal de login aberto. Páginas do painel SHALL ter `noindex`.

#### Scenario: Recarregar a página do painel
- **WHEN** o admin logado recarrega `/es/admin/anuncios`
- **THEN** a sessão é renovada pelo cookie e a página continua aberta

#### Scenario: Sem sessão
- **WHEN** um visitante sem cookie de refresh acessa `/es/admin`
- **THEN** é levado para `/es` com o modal de login aberto e nenhum dado do painel é exibido

### Requirement: Renovação silenciosa de sessão
O cliente Axios SHALL guardar o access token apenas em memória, anexá-lo em `Authorization: Bearer` e, ao receber HTTP 401, chamar `POST /api/auth/refresh` uma única vez para todas as requisições simultâneas, repetindo-as com o novo token. Se a renovação falhar, SHALL encerrar a sessão local e ir para o login.

#### Scenario: Token expirado durante o uso
- **WHEN** três requisições recebem 401 ao mesmo tempo por token expirado
- **THEN** apenas uma chamada de refresh é feita e as três requisições são repetidas com sucesso

#### Scenario: Refresh também falha
- **WHEN** a chamada de refresh retorna 401
- **THEN** o painel limpa o estado do usuário e mostra o modal de login

#### Scenario: Token fora do armazenamento do navegador
- **WHEN** `localStorage` e `sessionStorage` são inspecionados após o login
- **THEN** nenhum token está presente

### Requirement: Dashboard
A seção Dashboard SHALL mostrar os cartões de indicadores (acessos, cliques em anúncios, vendas com meta, aluguéis ativos, leads novos) com variação, o gráfico de barras de tráfego com alternância entre 7 e 30 dias, o ranking dos anúncios mais clicados e os próximos compromissos da agenda.

#### Scenario: Gráfico de 30 dias
- **WHEN** o admin alterna o gráfico para 30 dias
- **THEN** o gráfico mostra 30 barras com rótulos de data no idioma atual

### Requirement: Listas com filtro e ordenação no cliente
As listas de anúncios, leads, proprietários e arquivos SHALL carregar páginas amplas (até 500 itens) e aplicar busca textual, filtros e ordenação por coluna no cliente com `useMemo`, sem nova requisição a cada mudança de filtro.

#### Scenario: Ordenar por coluna
- **WHEN** o admin clica no cabeçalho "Preço" da lista de anúncios
- **THEN** a lista é reordenada sem nova requisição à API, e um segundo clique inverte a ordem

#### Scenario: Filtro por status
- **WHEN** o admin filtra leads pelo status `Negotiating`
- **THEN** só os leads desse status aparecem, sem nova requisição

### Requirement: Busca rápida com debounce
As caixas de busca rápida das listas SHALL usar um hook `useDebounce` que só atualiza o termo de filtragem 300 ms após o fim da digitação.

#### Scenario: Digitação contínua
- **WHEN** o admin digita "aura" rapidamente
- **THEN** a lista é filtrada uma única vez, 300 ms depois da última tecla

### Requirement: Anúncios & Mídia
A seção SHALL listar os anúncios e oferecer formulário de criação e edição com abas de idioma para título e descrição, campos do imóvel, link de vídeo, galeria com upload múltiplo, pré-visualização, ordenação por arrastar, escolha da capa e texto alternativo, e ações de publicar, despublicar, destacar, mudar status e arquivar.

#### Scenario: Publicar a partir do formulário
- **WHEN** o admin salva um anúncio com imagem e clica em "Publicar"
- **THEN** o anúncio aparece como publicado na lista e o link "Ver no site" abre a página pública

### Requirement: Validação de uploads no cliente
Antes de enviar, o painel SHALL validar extensão e tamanho de cada arquivo com as mesmas regras da API (imagens até 50 MB; vídeos até 50 MB; documentos até 15 MB) e SHALL mostrar o progresso do envio de cada arquivo.

#### Scenario: Arquivo grande demais
- **WHEN** o admin seleciona uma imagem de 60 MB
- **THEN** o painel mostra o erro para esse arquivo e não envia nada para a API

#### Scenario: Tipo não permitido
- **WHEN** o admin seleciona um `.zip`
- **THEN** o painel recusa o arquivo informando os tipos aceitos

### Requirement: Clientes / Leads e Proprietários
As seções SHALL ter formulário de cadastro e tabela com nome, contato, interesse ou imóveis captados e status, com edição em painel lateral, mudança rápida de status, link de WhatsApp para o contato e conversão de proposta de proprietário.

#### Scenario: Contato por WhatsApp
- **WHEN** o admin clica no ícone de WhatsApp de um lead
- **THEN** abre conversa com o número do lead em nova aba

### Requirement: Agenda / Calendário
A seção SHALL mostrar o formulário "Agendar visita", a lista de próximos compromissos e um calendário mensal estilo Google Calendar com navegação entre meses, destaque do dia atual, visitas como etiquetas coloridas por status e detalhe da visita ao clicar.

#### Scenario: Navegar entre meses
- **WHEN** o admin clica na seta para o próximo mês
- **THEN** o calendário carrega as visitas do novo mês e atualiza o título

#### Scenario: Visita criada aparece no calendário
- **WHEN** o admin agenda uma visita para o dia 20
- **THEN** a etiqueta aparece no dia 20 e na lista de próximos compromissos sem recarregar a página

### Requirement: Imagens e Documentos
A seção SHALL permitir enviar arquivos escolhendo o anúncio vinculado (ou "Sem vínculo") e a descrição, e SHALL mostrar os arquivos em grade com pré-visualização para imagens, ícone por tipo para documentos e vídeos, etiqueta do vínculo, descrição, download e exclusão com confirmação.

#### Scenario: Pré-visualização de documento
- **WHEN** o arquivo é um `.pdf`
- **THEN** a grade mostra o ícone de PDF, o nome e a etiqueta do vínculo

### Requirement: Minha conta
A seção Minha conta SHALL permitir trocar a senha informando a senha atual, a nova senha e a confirmação, com indicação das regras da senha.

#### Scenario: Confirmação diferente
- **WHEN** a nova senha e a confirmação não coincidem
- **THEN** o formulário mostra o erro sem chamar a API

### Requirement: Configurações
A seção Configurações SHALL permitir editar os dados institucionais, cotações, taxa do simulador, meta mensal e zonas.

#### Scenario: Salvar cotações
- **WHEN** o admin salva novas cotações
- **THEN** o painel mostra confirmação e a data da atualização

