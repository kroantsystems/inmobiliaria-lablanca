## MODIFIED Requirements

### Requirement: Cadastro de anúncio
O admin SHALL poder criar, editar, arquivar e excluir anúncios via `/api/admin/properties` com: operação (venda ou aluguel), tipo (casa, departamento, terreno, comercial, outro), preço e moeda (USD, PYG, BRL, EUR ou ARS), zona, cidade, endereço, latitude e longitude, quartos, banheiros, área construída, área do terreno, vagas, capacidade máxima de pessoas, terreno exclusivo ou compartilhado, piscina, mobília (sem mobília, semimobiliado ou mobiliado), ar-condicionado instalado, aceita pets, lista de características, link de vídeo (YouTube, Vimeo ou MP4), proprietário opcional e traduções de título, descrição e observação sobre pets. Os campos de capacidade, terreno, piscina, mobília, ar-condicionado e pets SHALL ser opcionais, distinguindo "não informado" de "não".

#### Scenario: Criação válida
- **WHEN** o admin envia um anúncio com tradução em espanhol, preço maior que zero e zona existente
- **THEN** a API retorna HTTP 201 com o anúncio criado em status `Draft` e não publicado

#### Scenario: Quitinete para duas pessoas
- **WHEN** o admin salva um departamento com capacidade máxima 2, mobiliado e com ar-condicionado
- **THEN** o anúncio guarda esses dados e o site os mostra nos dados do imóvel

#### Scenario: Aceita pets com observação
- **WHEN** o admin marca "aceita pets" e escreve "Somente pequeno porte" na tradução em português
- **THEN** a página em português mostra que aceita pets com a observação, e as outras línguas mostram a observação do próprio idioma ou, sem ela, a do espanhol

#### Scenario: Capacidade inválida
- **WHEN** a capacidade máxima é zero, negativa ou maior que 100
- **THEN** a API retorna HTTP 400

#### Scenario: Preço em euro
- **WHEN** o admin cadastra um anúncio com preço 150.000 e moeda EUR
- **THEN** o anúncio é salvo em EUR e a busca por faixa de preço o compara convertido em dólares pelas cotações vigentes

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

### Requirement: Destaque do mês
O sistema SHALL distinguir dois tipos de destaque: "destaque" (vários anúncios, exibidos na grade de destaques da home) e "destaque do mês" (no máximo um anúncio por tenant, exibido no topo da home). Marcar um anúncio como destaque do mês SHALL desmarcar o anterior na mesma operação e SHALL marcá-lo também como destaque. Só anúncios publicados e disponíveis ou reservados SHALL poder ser destaque do mês; despublicar, arquivar, excluir, vender ou alugar o anúncio SHALL retirar o destaque do mês. O banco SHALL garantir a unicidade por tenant.

#### Scenario: Troca do destaque do mês
- **WHEN** o anúncio A é o destaque do mês e o admin marca o anúncio B
- **THEN** B passa a ser o destaque do mês, A deixa de ser, e ambos continuam na grade de destaques

#### Scenario: Destaque do mês em todos os lugares
- **WHEN** um anúncio é o destaque do mês
- **THEN** ele aparece no topo da home, na grade de destaques e no catálogo

#### Scenario: Anúncio não publicado
- **WHEN** o admin tenta marcar como destaque do mês um anúncio em rascunho ou não publicado
- **THEN** a API retorna HTTP 400

#### Scenario: Anúncio vendido
- **WHEN** o destaque do mês é marcado como `Sold`
- **THEN** ele deixa de ser destaque do mês e o topo da home passa a mostrar o destaque mais recente

#### Scenario: Destaque exibido
- **WHEN** existe ao menos um anúncio publicado marcado como destaque
- **THEN** `GET /api/public/properties/featured` retorna os destaques com o destaque do mês primeiro, seguido dos demais pelo mais recente, e indica qual é o destaque do mês

#### Scenario: Sem destaque
- **WHEN** nenhum anúncio está marcado como destaque
- **THEN** o endpoint retorna os anúncios publicados mais recentes

### Requirement: Detalhe público por slug
A API SHALL expor `GET /api/public/properties/{locale}/{slug}` retornando o anúncio publicado com traduções, galeria pública, vídeo, características, capacidade, terreno, piscina, mobília, ar-condicionado, pets com observação no idioma, coordenadas (ou as da zona, marcadas como aproximadas) e os slugs dos outros idiomas.

#### Scenario: Slug existente
- **WHEN** o site pede um slug publicado
- **THEN** a API retorna o anúncio e o mapa de slugs por idioma

#### Scenario: Slug de anúncio despublicado
- **WHEN** o slug pertence a um anúncio não publicado, arquivado ou excluído
- **THEN** a API retorna HTTP 404

### Requirement: Revalidação do site após alterações
Ao publicar, editar, despublicar, arquivar, excluir ou mudar o destaque de um anúncio, a API SHALL notificar o endpoint de revalidação do Next.js para atualizar as páginas afetadas.

#### Scenario: Anúncio publicado
- **WHEN** o admin publica um anúncio
- **THEN** a API chama a revalidação com as tags do anúncio, da listagem e do sitemap

#### Scenario: Anúncio excluído
- **WHEN** o admin exclui um anúncio publicado
- **THEN** a API chama a revalidação com as tags do anúncio, da listagem, dos destaques, do sitemap e do `llms.txt`

#### Scenario: Falha na revalidação
- **WHEN** o endpoint de revalidação não responde
- **THEN** a operação do admin é concluída mesmo assim e a falha é registrada no log

## ADDED Requirements

### Requirement: Exclusão definitiva de anúncio
O admin SHALL poder excluir definitivamente um anúncio via `DELETE /api/admin/properties/{id}`. A exclusão SHALL remover o anúncio, suas traduções e as imagens e vídeos da galeria (registro e arquivo armazenado); documentos vinculados SHALL ser mantidos no banco de arquivos sem vínculo de anúncio; leads e visitas que apontavam para o anúncio SHALL ser mantidos sem o vínculo.

#### Scenario: Exclusão de anúncio com galeria
- **WHEN** o admin exclui um anúncio com três fotos e um contrato em PDF vinculado
- **THEN** a API retorna HTTP 204, as fotos são apagadas do armazenamento e o PDF continua no banco de arquivos como "Sem vínculo"

#### Scenario: Visitas e leads preservados
- **WHEN** o anúncio excluído tinha um lead e uma visita vinculados
- **THEN** o lead e a visita continuam no painel, sem o anúncio

#### Scenario: Anúncio inexistente
- **WHEN** o id não existe no tenant
- **THEN** a API retorna HTTP 404

### Requirement: Localização aproximada pela zona
Anúncios publicados sem latitude e longitude SHALL usar as coordenadas de referência da zona nas respostas públicas de mapa e de detalhe, com um indicador de localização aproximada. Anúncios sem coordenadas cuja zona também não tem coordenadas SHALL ficar fora do mapa.

#### Scenario: Anúncio sem coordenadas
- **WHEN** um anúncio publicado da zona Hernandarias não tem coordenadas
- **THEN** `GET /api/public/properties/map` o retorna com as coordenadas da zona e `approximateLocation` verdadeiro

#### Scenario: Anúncio com coordenadas
- **WHEN** o anúncio tem coordenadas próprias
- **THEN** a resposta usa as coordenadas do anúncio e `approximateLocation` falso
