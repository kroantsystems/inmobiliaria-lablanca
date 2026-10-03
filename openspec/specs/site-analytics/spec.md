# site-analytics Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Registro anônimo de eventos
A API SHALL expor `POST /api/public/analytics/events` aceitando eventos `PageView`, `PropertyView`, `WhatsAppClick` e `ContactClick`, com caminho, idioma, anúncio opcional, identificador de sessão aleatório gerado no navegador e domínio de origem (referrer). O sistema SHALL NOT gravar IP, user agent completo, cookies ou dados pessoais nos eventos.

#### Scenario: Visualização de anúncio
- **WHEN** um visitante abre a página de um anúncio publicado
- **THEN** o site envia um evento `PropertyView` com o id do anúncio e a API retorna HTTP 202

#### Scenario: Evento de robô
- **WHEN** a requisição vem com user agent de robô conhecido (Googlebot, bingbot, GPTBot, ClaudeBot, etc.)
- **THEN** a API retorna HTTP 202 sem gravar o evento

#### Scenario: Tipo de evento desconhecido
- **WHEN** o evento tem tipo fora da lista
- **THEN** a API retorna HTTP 400

#### Scenario: Nenhum dado pessoal gravado
- **WHEN** a tabela de eventos é consultada
- **THEN** não existem colunas de IP, user agent ou e-mail

### Requirement: Resumo do dashboard
A API SHALL expor `GET /api/admin/dashboard/summary` com os indicadores do mês corrente no fuso `America/Asuncion`, cada um com a variação percentual em relação ao mês anterior:
- Acessos ao site (sessões únicas com `PageView`).
- Cliques em anúncios (eventos `PropertyView`).
- Vendas concluídas (anúncios marcados `Sold` no mês) e a meta mensal configurada.
- Aluguéis ativos (anúncios em `Rented`).
- Leads novos no mês.

#### Scenario: Indicadores do mês
- **WHEN** o admin abre o dashboard
- **THEN** a API retorna os cinco indicadores com valor atual e variação

#### Scenario: Mês anterior sem dados
- **WHEN** o mês anterior tem valor zero
- **THEN** a variação é retornada como nula em vez de divisão por zero

### Requirement: Tráfego diário
A API SHALL expor `GET /api/admin/dashboard/traffic?days=7|30` com a contagem diária de acessos e de cliques em anúncios, incluindo dias sem eventos com valor zero.

#### Scenario: Últimos 7 dias
- **WHEN** o painel pede `days=7`
- **THEN** a API retorna exatamente 7 pontos, um por dia, em ordem cronológica

#### Scenario: Período não permitido
- **WHEN** o painel pede `days=90`
- **THEN** a API retorna HTTP 400

### Requirement: Anúncios mais clicados
A API SHALL expor `GET /api/admin/dashboard/top-properties` com os 5 anúncios com mais `PropertyView` nos últimos 30 dias.

#### Scenario: Ranking
- **WHEN** existem visualizações para 8 anúncios
- **THEN** a API retorna os 5 com mais visualizações, em ordem decrescente, com título e contagem

### Requirement: Retenção limitada de eventos
O sistema SHALL apagar eventos brutos com mais de 13 meses por meio de uma tarefa em segundo plano diária.

#### Scenario: Limpeza de eventos antigos
- **WHEN** a tarefa diária roda
- **THEN** eventos com mais de 13 meses são removidos e os demais permanecem

