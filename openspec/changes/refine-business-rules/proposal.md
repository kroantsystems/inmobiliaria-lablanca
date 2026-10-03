## Why

Com o site e o painel no ar localmente, o dono da imobiliária revisou o uso real e pediu uma rodada de ajustes de regra de negócio: o login por e-mail não combina com a operação, faltam informações que os clientes perguntam (capacidade, piscina, mobília, pets), alguns cadastros estão incompletos (documento do cliente, interesse e imóveis do proprietário), a agenda não cobre visitas de avaliação com proprietários, e o site precisa de mais controle pelo painel (perguntas frequentes, foto da sede, avaliações no Google, redes sociais) e de um canal para os inscritos em novidades.

## What Changes

**Acesso e configurações**
- **BREAKING**: login com **usuário e senha** em vez de e-mail. Usuários existentes recebem um nome de usuário na migração; a CLI passa a exigir `--username`. Os usuários do painel ficam visíveis (somente leitura) em Configurações.
- Novas moedas **EUR** e **ARS**: preço do anúncio, seletor de moeda do site, cotações nas configurações e simulador.
- Configurações ganham: cadastro de **perguntas frequentes** em todos os idiomas (como as zonas), **foto da sede** usada em "Tradição e transparência", **link de avaliação no Google** e coordenadas de referência por zona.

**Anúncios**
- **Destaque do mês exclusivo**: só um anúncio por vez, marcado à parte do "destaque" comum; ele aparece no topo da home, na grade de destaques e no catálogo. Hoje não existe essa opção: o topo da home mostra o destaque mais recente.
- Novos campos: capacidade máxima de pessoas, terreno exclusivo ou compartilhado, piscina, mobília (sem, semi, mobiliado), ar-condicionado e aceita pets com observação por idioma. Aparecem na página do anúncio e no JSON-LD.
- **Exclusão definitiva** do anúncio (além de arquivar), com confirmação.
- **Todos os anúncios publicados no mapa**: o mapa já mostra todos os publicados, mas só os que têm coordenadas. Anúncios sem coordenadas passam a usar o ponto de referência da zona (marcado como localização aproximada), e o formulário ganha um mapa para marcar o ponto com um clique.
- **Sede da imobiliária no mapa** com marcador próprio, em destaque.

**CRM e agenda**
- Clientes: campo **documento**.
- Proprietários: **interesse principal** (vender, alugar, vender ou alugar, outro) e **vínculo de anúncios** direto no cadastro do proprietário.
- Agenda: anúncio **opcional**, novo campo **proprietário**, tipo do compromisso (visita com cliente, avaliação com proprietário, outro) e local opcional; **excluir e editar** agendamentos; campos de data, hora e duração alinhados.
- Imagens e documentos: vínculo também com **cliente** ou **proprietário**; os documentos aparecem no cadastro da pessoa.

**Dashboard**
- Indicadores, tráfego e ranking **filtráveis por período** (presets e intervalo livre); padrão é o mês atual, comparando com o período anterior de mesma duração.

**Visitantes (novo módulo)**
- A inscrição em "Novidades exclusivas" do site deixa de virar lead e passa a criar um **visitante inscrito**, para depois entrar no grupo de WhatsApp de novidades. Novo módulo "Visitantes" no painel: lista, busca, marcação "adicionado ao grupo", link de WhatsApp, exportação CSV e exclusão. **BREAKING** para a API: `POST /api/public/leads` deixa de aceitar a origem `Newsletter`.

**Site público**
- Remover o botão "Atendimento VIP" do cabeçalho e o botão "Dúvidas pelo WhatsApp" do hero (o WhatsApp flutuante continua).
- Perguntas frequentes da home e da página de FAQ vêm do banco.
- Foto da sede em "Tradição e transparência" e na página Sobre nós.
- Botão "Avalie-nos no Google" e botões das redes sociais preenchidas (Facebook, Instagram, YouTube, TikTok).

## Capabilities

### New Capabilities
- `newsletter-subscribers`: inscrição pública em novidades, módulo "Visitantes" no painel, marcação de entrada no grupo de WhatsApp e exportação.
- `faq-management`: perguntas frequentes cadastradas pelo admin em todos os idiomas, com perguntas padrão já gravadas no banco e exibição no site.

### Modified Capabilities
- `admin-auth`: login por nome de usuário; `me` retorna o usuário.
- `site-settings`: moedas EUR/ARS, foto da sede, link de avaliação no Google, coordenadas das zonas e lista de usuários do painel.
- `property-listings`: novos campos, destaque do mês exclusivo, exclusão definitiva, moedas novas e localização aproximada pela zona.
- `property-owners`: interesse principal e vínculo de anúncios pelo cadastro do proprietário.
- `leads`: documento do cliente; newsletter sai dos leads.
- `visit-scheduling`: anúncio opcional, proprietário, tipo e local do compromisso, exclusão.
- `media-library`: vínculo com cliente ou proprietário e imagem da sede pública.
- `site-analytics`: indicadores, tráfego e ranking por período.
- `public-website`: remoção de botões, mapa com sede e anúncios sem coordenadas, FAQ do banco, foto da sede, avaliação no Google, redes sociais, moedas novas, newsletter para visitantes, novos dados do imóvel.
- `admin-panel`: módulo Visitantes, perguntas frequentes e foto da sede em Configurações, filtro de período no dashboard, novos campos e exclusão de anúncio, mapa para marcar o ponto, agenda revista, vínculos de arquivos.
- `seo-geo`: JSON-LD com capacidade, pets e comodidades; FAQ do banco no `FAQPage`.
- `internationalization`: formatação de EUR e ARS.

## Impact

- **Backend**: entidades `User` (username), `Property` (campos novos, destaque do mês), `PropertyTranslation` (observação de pets), `Lead` (documento), `Owner` (interesse), `Visit` (anúncio opcional, proprietário, tipo, local), `MediaFile` (cliente/proprietário), `Zone` (coordenadas), `SiteSettings` (EUR/ARS, foto, Google), novas `Subscriber` e `FaqItem`; enum `Currency`; migração com dados (usernames, inscritos vindos dos leads `Newsletter`, perguntas padrão, coordenadas das zonas); endpoints novos (`/api/admin/subscribers`, `/api/public/subscribers`, `/api/admin/faqs`, `/api/public/faqs`, `DELETE /api/admin/properties/{id}`, `DELETE /api/admin/visits/{id}`, destaque do mês, foto da sede) e parâmetros de período no dashboard; permissões novas no papel admin; CLI com `--username`.
- **Frontend**: modal de login, painel (Visitantes, Configurações, Dashboard, Anúncios, Agenda, Clientes, Proprietários, Arquivos), site (home, FAQ, Sobre nós, Contato, rodapé, mapa, página do anúncio), mensagens nos 4 idiomas, formatação de moedas.
- **Dependência**: aplicar depois de arquivar `add-public-site-and-admin-panel`, cujas especificações esta mudança altera.
- **Dados locais**: a migração preserva os dados de exemplo; o admin de teste passa a entrar com o usuário gerado (`admin`).
