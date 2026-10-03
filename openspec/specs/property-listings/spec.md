# property-listings Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Cadastro de anúncio
O admin SHALL poder criar, editar e arquivar anúncios via `/api/admin/properties` com: operação (venda ou aluguel), tipo (casa, departamento, terreno, comercial, outro), preço e moeda (USD, PYG ou BRL), zona, cidade, endereço, latitude e longitude, quartos, banheiros, área construída, área do terreno, vagas, lista de características, link de vídeo (YouTube, Vimeo ou MP4), proprietário opcional e traduções de título e descrição.

#### Scenario: Criação válida
- **WHEN** o admin envia um anúncio com tradução em espanhol, preço maior que zero e zona existente
- **THEN** a API retorna HTTP 201 com o anúncio criado em status `Draft` e não publicado

#### Scenario: Tradução em espanhol obrigatória
- **WHEN** o anúncio não tem título em espanhol
- **THEN** a API retorna HTTP 400 indicando o campo de título em espanhol

#### Scenario: Preço inválido
- **WHEN** o preço é zero ou negativo
- **THEN** a API retorna HTTP 400

#### Scenario: Coordenadas fora do intervalo
- **WHEN** a latitude não está entre -90 e 90 ou a longitude não está entre -180 e 180
- **THEN** a API retorna HTTP 400

#### Scenario: Link de vídeo não suportado
- **WHEN** o link de vídeo não é YouTube, Vimeo ou um arquivo `.mp4` com HTTPS
- **THEN** a API retorna HTTP 400

### Requirement: Traduções e slugs por idioma
Cada anúncio SHALL ter traduções por idioma (`es`, `pt`, `en`, `gn`) com título, descrição, título SEO e descrição SEO opcionais. O sistema SHALL gerar um slug único por idioma a partir do título, sem acentos e em minúsculas.

#### Scenario: Slug gerado
- **WHEN** um anúncio é salvo com título em espanhol "Residencia Aura Light – Área 1"
- **THEN** o slug em espanhol é `residencia-aura-light-area-1`

#### Scenario: Slug repetido
- **WHEN** outro anúncio do mesmo tenant gera o mesmo slug no mesmo idioma
- **THEN** o sistema acrescenta um sufixo numérico (`-2`, `-3`) para manter a unicidade

#### Scenario: Idioma sem tradução
- **WHEN** o site pede o anúncio em português e não há tradução em português
- **THEN** o conteúdo retornado usa a tradução em espanhol e indica o idioma efetivo

### Requirement: Ciclo de vida do anúncio
O anúncio SHALL ter status `Draft`, `Available`, `Reserved`, `Sold`, `Rented` ou `Archived` e um indicador separado de publicação. Marcar como `Sold` ou `Rented` SHALL registrar a data da conclusão.

#### Scenario: Publicação
- **WHEN** o admin publica um anúncio `Available` com ao menos uma imagem pública
- **THEN** o anúncio fica visível no site e recebe data de publicação

#### Scenario: Publicação sem imagem
- **WHEN** o admin tenta publicar um anúncio sem nenhuma imagem pública
- **THEN** a API retorna HTTP 400

#### Scenario: Venda concluída
- **WHEN** o admin muda o status para `Sold`
- **THEN** o sistema registra a data da venda e o anúncio deixa de aparecer na busca pública de imóveis disponíveis

#### Scenario: Anúncio arquivado
- **WHEN** o admin arquiva um anúncio
- **THEN** ele deixa de ser publicado e não aparece no site nem no sitemap

### Requirement: Destaque do mês
O admin SHALL poder marcar anúncios publicados como destaque, e o site SHALL exibir no topo da home o destaque mais recente.

#### Scenario: Destaque exibido
- **WHEN** existe ao menos um anúncio publicado marcado como destaque
- **THEN** `GET /api/public/properties/featured` retorna os destaques ordenados pelo mais recente

#### Scenario: Sem destaque
- **WHEN** nenhum anúncio está marcado como destaque
- **THEN** o endpoint retorna os anúncios publicados mais recentes

### Requirement: Busca pública
A API SHALL expor `GET /api/public/properties` retornando somente anúncios publicados e não arquivados, com filtros por operação, tipo, zona, faixa de preço, quartos mínimos e texto, ordenação (mais recentes, menor preço, maior preço) e paginação de até 24 itens.

#### Scenario: Filtro combinado
- **WHEN** o site pede operação `Sale`, tipo `House`, zona `parana-country-club` e quartos mínimos 3
- **THEN** a resposta contém apenas anúncios que atendem a todos os filtros

#### Scenario: Rascunho nunca aparece
- **WHEN** existe um anúncio em `Draft` que atende aos filtros
- **THEN** ele não aparece no resultado

#### Scenario: Página acima do limite
- **WHEN** o site pede `pageSize=100`
- **THEN** a API limita o resultado a 24 itens por página

### Requirement: Detalhe público por slug
A API SHALL expor `GET /api/public/properties/{locale}/{slug}` retornando o anúncio publicado com traduções, galeria pública, vídeo, características, coordenadas e os slugs dos outros idiomas.

#### Scenario: Slug existente
- **WHEN** o site pede um slug publicado
- **THEN** a API retorna o anúncio e o mapa de slugs por idioma

#### Scenario: Slug de anúncio despublicado
- **WHEN** o slug pertence a um anúncio não publicado ou arquivado
- **THEN** a API retorna HTTP 404

### Requirement: Listagem administrativa ampla
A API SHALL expor `GET /api/admin/properties` com páginas de até 500 itens contendo todos os status, para que o painel filtre e ordene no cliente.

#### Scenario: Lista completa para o painel
- **WHEN** o painel pede `pageSize=500`
- **THEN** a API retorna até 500 anúncios de todos os status com o total disponível

### Requirement: Revalidação do site após alterações
Ao publicar, editar, despublicar ou arquivar um anúncio, a API SHALL notificar o endpoint de revalidação do Next.js para atualizar as páginas afetadas.

#### Scenario: Anúncio publicado
- **WHEN** o admin publica um anúncio
- **THEN** a API chama a revalidação com as tags do anúncio, da listagem e do sitemap

#### Scenario: Falha na revalidação
- **WHEN** o endpoint de revalidação não responde
- **THEN** a operação do admin é concluída mesmo assim e a falha é registrada no log

