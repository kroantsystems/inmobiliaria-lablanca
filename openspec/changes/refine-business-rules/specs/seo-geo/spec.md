## MODIFIED Requirements

### Requirement: Dados estruturados JSON-LD
O site SHALL publicar JSON-LD válido: `RealEstateAgent` com nome, `logo` (URL absoluta da logo oficial em PNG sem fundo), imagem da sede quando configurada, endereço, geo, telefone, horário, área atendida e redes (`sameAs`) em todas as páginas; `WebSite` com `SearchAction` na home; `RealEstateListing` com `Offer` (preço, moeda, disponibilidade), tipo de imóvel (`SingleFamilyResidence`, `Apartment`, `Place`), área, quartos, banheiros, capacidade (`occupancy`), aceitação de pets (`petsAllowed`), comodidades (`amenityFeature` para piscina, ar-condicionado, mobília e terreno exclusivo ou compartilhado), endereço, geo e imagens nas páginas de anúncio; `BreadcrumbList` em páginas internas; `FAQPage` com as perguntas frequentes ativas do banco na página de perguntas frequentes. Dados não informados SHALL ficar fora do JSON-LD, e coordenadas aproximadas pela zona SHALL NOT ser publicadas em `geo` do anúncio.

#### Scenario: JSON-LD do anúncio
- **WHEN** a página de um anúncio é renderizada
- **THEN** existe um `script type="application/ld+json"` com `RealEstateListing` contendo `offers.price`, `offers.priceCurrency` e `geo`

#### Scenario: Pets e capacidade
- **WHEN** o anúncio aceita pets e tem capacidade para 2 pessoas
- **THEN** o JSON-LD contém `petsAllowed: true` e `occupancy` com `maxValue` 2

#### Scenario: Localização aproximada
- **WHEN** o anúncio não tem coordenadas próprias
- **THEN** o JSON-LD do anúncio não contém `geo`

#### Scenario: Validação automatizada
- **WHEN** o teste de JSON-LD roda contra os geradores
- **THEN** todos os objetos têm `@context`, `@type` e os campos obrigatórios definidos

### Requirement: Renderização no servidor e cache
Home, catálogo, páginas de anúncio, zonas e institucionais SHALL ser renderizadas no servidor com cache por tags e revalidação sob demanda, entregando todo o conteúdo principal no HTML inicial, incluindo as perguntas frequentes cadastradas no painel.

#### Scenario: Conteúdo visível sem JavaScript
- **WHEN** um robô busca a página de um anúncio sem executar JavaScript
- **THEN** o HTML contém título, preço, descrição, características e imagens do anúncio

#### Scenario: Anúncio atualizado
- **WHEN** o admin altera o preço de um anúncio publicado
- **THEN** a página do anúncio mostra o novo preço na próxima requisição após a revalidação

#### Scenario: Pergunta frequente atualizada
- **WHEN** o admin altera uma pergunta frequente
- **THEN** a home e a página de perguntas frequentes mostram o novo texto na próxima requisição após a revalidação
