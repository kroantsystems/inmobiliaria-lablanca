## ADDED Requirements

### Requirement: Renderização no servidor e cache
Home, catálogo, páginas de anúncio, zonas e institucionais SHALL ser renderizadas no servidor com cache por tags e revalidação sob demanda, entregando todo o conteúdo principal no HTML inicial.

#### Scenario: Conteúdo visível sem JavaScript
- **WHEN** um robô busca a página de um anúncio sem executar JavaScript
- **THEN** o HTML contém título, preço, descrição, características e imagens do anúncio

#### Scenario: Anúncio atualizado
- **WHEN** o admin altera o preço de um anúncio publicado
- **THEN** a página do anúncio mostra o novo preço na próxima requisição após a revalidação

### Requirement: Metadados por página e idioma
Cada página pública SHALL gerar `title` (até 60 caracteres) e `meta description` (até 160 caracteres) únicos no idioma atual, URL canônica absoluta, Open Graph e Twitter Card. Anúncios SHALL usar título e descrição SEO quando preenchidos.

#### Scenario: Metadados do anúncio
- **WHEN** a página de um anúncio é renderizada
- **THEN** o `<head>` contém `title`, `description`, `link rel="canonical"`, `og:title`, `og:description`, `og:image`, `og:locale` e `twitter:card`

#### Scenario: Catálogo com filtros
- **WHEN** o catálogo é aberto com filtros e ordenação na URL
- **THEN** a canônica aponta para a versão com os filtros principais (operação, tipo, zona) sem parâmetros de ordenação e paginação

### Requirement: Alternativas de idioma (hreflang)
Cada página pública SHALL declarar `link rel="alternate" hreflang` para `es-PY`, `pt-BR`, `en`, `gn-PY` e `x-default` (apontando para `es`), usando os slugs traduzidos de cada idioma, e o `<html>` SHALL ter o atributo `lang` do idioma atual.

#### Scenario: Alternativas do anúncio
- **WHEN** a página em espanhol de um anúncio é renderizada
- **THEN** o `<head>` lista as quatro URLs de idioma com seus slugs e o `x-default`

### Requirement: URLs amigáveis por idioma
As rotas públicas SHALL usar caminhos traduzidos (ex.: `/es/propiedades`, `/pt/imoveis`, `/en/properties`, `/gn/propiedades`) e slugs legíveis sem acentos.

#### Scenario: Caminho traduzido
- **WHEN** o visitante troca de espanhol para português no catálogo
- **THEN** a URL muda de `/es/propiedades` para `/pt/imoveis`

### Requirement: Dados estruturados JSON-LD
O site SHALL publicar JSON-LD válido: `RealEstateAgent` com nome, `logo` (URL absoluta da logo oficial em PNG sem fundo), endereço, geo, telefone, horário, área atendida e redes (`sameAs`) em todas as páginas; `WebSite` com `SearchAction` na home; `RealEstateListing` com `Offer` (preço, moeda, disponibilidade), tipo de imóvel (`SingleFamilyResidence`, `Apartment`, `Place`), área, quartos, banheiros, endereço, geo e imagens nas páginas de anúncio; `BreadcrumbList` em páginas internas; `FAQPage` nas perguntas frequentes.

#### Scenario: JSON-LD do anúncio
- **WHEN** a página de um anúncio é renderizada
- **THEN** existe um `script type="application/ld+json"` com `RealEstateListing` contendo `offers.price`, `offers.priceCurrency` e `geo`

#### Scenario: Validação automatizada
- **WHEN** o teste de JSON-LD roda contra os geradores
- **THEN** todos os objetos têm `@context`, `@type` e os campos obrigatórios definidos

### Requirement: Sitemap e robots
O site SHALL gerar `sitemap.xml` dinâmico com todas as páginas públicas em todos os idiomas, com alternativas `hreflang` e data de última alteração dos anúncios, e `robots.txt` liberando o site, bloqueando `/*/admin` e `/api/`, e apontando para o sitemap.

#### Scenario: Anúncio novo no sitemap
- **WHEN** um anúncio é publicado
- **THEN** o sitemap passa a listar suas URLs nos quatro idiomas após a revalidação

#### Scenario: Painel fora dos buscadores
- **WHEN** o `robots.txt` é lido
- **THEN** ele contém `Disallow` para as rotas do painel e da API

### Requirement: Otimização para motores generativos (GEO)
O site SHALL facilitar a citação por assistentes de IA: `robots.txt` SHALL permitir os robôs de IA (GPTBot, OAI-SearchBot, ClaudeBot, PerplexityBot, Google-Extended); o site SHALL publicar `/llms.txt` com descrição da imobiliária, área de atuação, contatos e links para as principais páginas e anúncios publicados; páginas de anúncio e zona SHALL trazer um resumo factual no início (tipo, operação, preço, zona, área, quartos) e seções de perguntas e respostas em texto simples.

#### Scenario: llms.txt atualizado
- **WHEN** um anúncio é publicado
- **THEN** `/llms.txt` passa a listar o anúncio com título, preço e URL após a revalidação

#### Scenario: Resumo factual
- **WHEN** a página de um anúncio é renderizada
- **THEN** o primeiro parágrafo do conteúdo descreve tipo, operação, zona, preço, área e quartos em uma frase completa

### Requirement: Páginas por zona
O site SHALL gerar uma página por zona cadastrada e idioma (ex.: `/es/zonas/hernandarias`) com descrição da zona, anúncios publicados nela, mapa e perguntas frequentes, linkada a partir do rodapé e dos anúncios.

#### Scenario: Zona com anúncios
- **WHEN** a página da zona Paraná Country Club é aberta
- **THEN** ela lista os anúncios publicados da zona e contém JSON-LD de `BreadcrumbList`

### Requirement: Imagens Open Graph geradas
Cada anúncio SHALL ter imagem Open Graph de 1200×630 gerada pelo Next.js com foto de capa, título, preço e logo.

#### Scenario: Compartilhamento no WhatsApp
- **WHEN** o link de um anúncio é compartilhado
- **THEN** a prévia mostra a imagem gerada com título e preço

### Requirement: Desempenho (Core Web Vitals)
As páginas públicas SHALL atingir, em build de produção medido com Lighthouse em perfil móvel, LCP até 2,5 s, CLS até 0,1, pontuação de SEO de no mínimo 95 e de desempenho de no mínimo 90, usando imagens otimizadas (AVIF/WebP, tamanhos responsivos, prioridade para a imagem principal), fontes locais e carregamento tardio de mapa e componentes não essenciais.

#### Scenario: Auditoria da home
- **WHEN** o Lighthouse móvel roda na home do build de produção
- **THEN** SEO ≥ 95, desempenho ≥ 90 e CLS ≤ 0,1

#### Scenario: Imagens otimizadas
- **WHEN** a página de um anúncio é carregada
- **THEN** as imagens são servidas em AVIF ou WebP com `width`/`height` definidos e apenas a imagem principal é carregada com prioridade
