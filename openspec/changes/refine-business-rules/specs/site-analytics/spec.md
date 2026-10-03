## MODIFIED Requirements

### Requirement: Resumo do dashboard
A API SHALL expor `GET /api/admin/dashboard/summary?from=&to=` com os indicadores de um período de dias no fuso `America/Asuncion` (datas `aaaa-mm-dd`, inclusivas), cada um com a variação percentual em relação ao período anterior de mesma duração. Sem parâmetros, o período SHALL ser o mês atual até hoje, comparado com o mês anterior inteiro. O período SHALL ter no máximo 366 dias e `from` SHALL ser anterior ou igual a `to`.
- Acessos ao site (sessões únicas com `PageView`).
- Cliques em anúncios (eventos `PropertyView`).
- Vendas concluídas (anúncios marcados `Sold` no período) e a meta mensal configurada, proporcional ao número de meses do período.
- Aluguéis ativos (anúncios em `Rented` no fim do período).
- Leads novos no período.

#### Scenario: Indicadores do mês
- **WHEN** o admin abre o dashboard sem escolher período
- **THEN** a API retorna os cinco indicadores do mês atual com variação em relação ao mês anterior

#### Scenario: Período personalizado
- **WHEN** o painel pede `from=2026-09-01&to=2026-09-15`
- **THEN** os indicadores contam só esses 15 dias e a variação compara com 17 a 31 de agosto

#### Scenario: Mês anterior sem dados
- **WHEN** o período anterior (no padrão, o mês anterior) tem valor zero
- **THEN** a variação é retornada como nula em vez de divisão por zero

#### Scenario: Período inválido
- **WHEN** `from` é posterior a `to` ou o período passa de 366 dias
- **THEN** a API retorna HTTP 400

### Requirement: Tráfego diário
A API SHALL expor `GET /api/admin/dashboard/traffic?from=&to=` com a contagem de acessos e de cliques em anúncios no período (mesmas regras e padrão do resumo), um ponto por dia para períodos de até 92 dias e um ponto por mês para períodos maiores, incluindo dias ou meses sem eventos com valor zero.

#### Scenario: Últimos 7 dias
- **WHEN** o painel pede `from=2026-10-14&to=2026-10-20`
- **THEN** a API retorna exatamente 7 pontos, um por dia, em ordem cronológica

#### Scenario: Período não permitido
- **WHEN** o painel pede um período maior que 366 dias ou com `from` depois de `to`
- **THEN** a API retorna HTTP 400

#### Scenario: Mês atual
- **WHEN** o painel pede o tráfego sem parâmetros em 20 de outubro
- **THEN** a API retorna 20 pontos diários, de 1 a 20 de outubro, em ordem cronológica

#### Scenario: Ano inteiro
- **WHEN** o painel pede `from=2026-01-01&to=2026-12-31`
- **THEN** a API retorna 12 pontos mensais

### Requirement: Anúncios mais clicados
A API SHALL expor `GET /api/admin/dashboard/top-properties?from=&to=` com os 5 anúncios com mais `PropertyView` no período (mesmas regras e padrão do resumo).

#### Scenario: Ranking
- **WHEN** existem visualizações para 8 anúncios no período
- **THEN** a API retorna os 5 com mais visualizações, em ordem decrescente, com título e contagem
