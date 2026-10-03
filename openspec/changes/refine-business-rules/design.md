## Context

O site e o painel da mudança `add-public-site-and-admin-panel` estão funcionando localmente com dados de exemplo. A revisão do dono da imobiliária trouxe ajustes de regra de negócio que tocam quase todas as áreas: autenticação, anúncios, CRM, agenda, arquivos, dashboard, configurações e site. Não há produção nem dados reais, então migrações podem transformar dados livremente, mas devem preservar os dados de exemplo (o admin de teste precisa continuar entrando).

Estado atual relevante:
- Login por e-mail (`Users.Email` único por tenant).
- `Currency` = USD, PYG, BRL; cotações `PygPerUsd` e `BrlPerUsd` nas configurações.
- `Property.IsFeatured` (vários); o topo da home usa o destaque mais recente. Não há destaque do mês exclusivo.
- O mapa público já lista todos os anúncios publicados, mas só os que têm coordenadas; o formulário não ajuda a obtê-las.
- `Visit.PropertyId` obrigatório; não há exclusão de visita.
- `MediaFile` só vincula a anúncio; a rota pública só serve mídia de anúncio publicado.
- Newsletter grava `Lead` com origem `Newsletter`.
- Perguntas frequentes fixas nos arquivos de mensagens.

Esta mudança depende de `add-public-site-and-admin-panel` estar arquivada, porque altera as especificações dela.

## Goals / Non-Goals

**Goals:**
- Implementar todos os itens pedidos com TDD no backend e testes de unidade no frontend onde houver lógica.
- Manter as garantias de segurança (autorização por padrão, documentos privados, mensagens de login genéricas).
- Migrações que convertem os dados existentes (usuários, inscritos, perguntas padrão, coordenadas das zonas).

**Non-Goals:**
- Automação do grupo de WhatsApp (API do WhatsApp Business): a entrada no grupo é manual, com link e CSV.
- Filtros novos no catálogo público (pets, mobília, capacidade).
- Lista de características traduzível por idioma (continua uma lista única).
- Gestão de usuários pelo painel (criar, editar, desativar): continua pela CLI; o painel só mostra.
- Integração com a API do Google (avaliações): apenas o link para avaliar.

## Decisions

### 1. Login por nome de usuário
- `Users.Username` normalizado (minúsculas, sem espaços nas pontas), 3 a 32 caracteres `[a-z0-9._-]`, índice único `(TenantId, Username)`. `Email` passa a ser opcional, com índice único filtrado (`WHERE "Email" IS NOT NULL`).
- O login procura pelo usuário normalizado; usuário inexistente continua passando por `SimulateVerify` para não revelar existência por tempo de resposta. A validação de entrada só exige os campos presentes (o formato só é validado na criação, para não dar pistas no login).
- O JWT ganha `preferred_username`; `email` só é emitido quando existir. `me` e a resposta de login retornam `username`.
- CLI: `create-admin --username --name [--email]`, `reset-password --username`, novo `rename-user --username --new-username` (revoga os refresh tokens).
- Migração: coluna anulável → preenchida por SQL com a parte do e-mail antes da `@` (`lower`, `regexp_replace` para tirar caracteres fora do formato, complemento `user` se ficar com menos de 3 caracteres, sufixo numérico por `row_number()` em repetições) → `NOT NULL` e índice único.
- Alternativa descartada: aceitar usuário **ou** e-mail no mesmo campo. O pedido é explícito (login por usuário, não e-mail), e um único identificador reduz superfície de enumeração.

### 2. Moedas EUR e ARS
- `Currency` ganha `EUR` e `ARS` (armazenado como texto). Configurações ganham `EurPerUsd` e `ArsPerUsd` (unidades por 1 USD, como as demais), com valores iniciais aproximados a revisar no painel.
- A expressão de preço em USD da busca e da ordenação passa a cobrir as cinco moedas.
- No frontend, `Rates` vira um mapa `moeda → unidades por USD`; `formatPrice` usa os prefixos `USD`, `₲`, `R$`, `€`, `AR$` (prefixo próprio evita confundir o peso com o dólar). O simulador usa o mesmo contexto de moeda.

### 3. Destaque do mês exclusivo
- Novo `Property.IsMonthlyHighlight`, separado de `IsFeatured`. Índice único filtrado `(TenantId) WHERE "IsMonthlyHighlight"` garante no máximo um por tenant mesmo com requisições simultâneas.
- `PUT /api/admin/properties/{id}/monthly-highlight {highlight}`: dentro da transação, primeiro limpa o atual com `ExecuteUpdate` e só depois marca o novo (dois comandos SQL em sequência, para o índice não acusar duplicidade no meio de um único `SaveChanges`). Marcar também liga `IsFeatured`.
- O domínio recusa marcar anúncio não publicado ou fora de `Available`/`Reserved`; `Unpublish`, `Archive` e a mudança para `Sold`/`Rented` limpam o destaque do mês.
- `featured` retorna `isMonthlyHighlight` e ordena o destaque do mês primeiro. A home usa esse item no topo e mantém a grade com todos os destaques; o catálogo já lista todos os publicados.
- Alternativa descartada: reaproveitar `IsFeatured` com limite de um. O dono quer os dois níveis (vários destaques na grade e um do mês no topo).

### 4. Novos dados do imóvel
- Colunas anuláveis em `Properties`: `MaxOccupants` (1 a 100), `LotType` (`Exclusive`/`Shared`), `HasPool`, `Furnishing` (`Unfurnished`/`SemiFurnished`/`Furnished`), `HasAirConditioning`, `PetsAllowed`. Anulável significa "não informado", e o site só mostra o que foi informado.
- `PropertyTranslations.PetNotes` (até 300 caracteres) por idioma, com o mesmo fallback para espanhol das demais traduções.
- Painel: seletores de três estados ("Não informado", "Sim", "Não"). Site: novos itens em "Dados do imóvel". JSON-LD: `occupancy` e `petsAllowed` só para tipos de acomodação (`SingleFamilyResidence`, `Apartment`); comodidades em `amenityFeature` para todos.
- Rótulos de terreno: "Terreno isolado" / "Terreno compartilhado" (pt), "Terreno independiente" / "Terreno compartido" (es), a confirmar com o dono (ver perguntas em aberto).

### 5. Exclusão definitiva de anúncio
- `DELETE /api/admin/properties/{id}` (`properties.write`). Na transação: limpa destaque, anula vínculos (`Leads.PropertyId`, `Visits.PropertyId`, `MediaFiles.PropertyId` dos documentos, `AnalyticsEvents.PropertyId`), remove imagens e vídeos da galeria e o anúncio com suas traduções.
- Os arquivos físicos da galeria são apagados **depois** do commit, em melhor esforço com log de falha; assim um erro de disco não deixa o banco apontando para arquivos inexistentes.
- As chaves estrangeiras desses vínculos passam a `ON DELETE SET NULL` como rede de segurança.
- Revalida anúncio, listagem, destaques, sitemap e `llms.txt`.

### 6. Mapa: localização aproximada e sede
- `Zones` ganha `Latitude`/`Longitude` opcionais, preenchidas na migração para as cinco zonas iniciais (pontos aproximados, ajustáveis no painel).
- A projeção pública de mapa e de detalhe usa as coordenadas do anúncio ou, na falta, as da zona, com `approximateLocation`. Anúncios sem nenhuma das duas ficam fora do mapa.
- O mapa do site ganha uma camada da sede (coordenadas das configurações): ícone próprio azul com o prédio da marca, `zIndexOffset` alto, popup com nome, endereço e link de rota. A página de contato deixa de usar o "cartão falso" de imóvel e passa a usar essa camada.
- Painel: componente `LocationPicker` (react-leaflet, carregado sob demanda) no formulário do anúncio e no da zona; clique ou arraste do marcador preenche latitude e longitude.

### 7. Dashboard por período
- `from`/`to` opcionais (datas locais de Assunção, inclusivas) em `summary`, `traffic` e `top-properties`, interpretados por um `DashboardPeriod` testável. Padrão: do dia 1 do mês até hoje, comparado com o mês anterior inteiro (comportamento atual). Período informado: comparado com o período imediatamente anterior de mesma duração. Máximo de 366 dias.
- Tráfego diário até 92 dias; acima disso, mensal. A meta de vendas é multiplicada pelo número de meses do período (mínimo 1). "Aluguéis ativos" é a fotografia no fim do período.
- Painel: seletor com presets e intervalo livre; período na URL (`?from=&to=`) para compartilhar e recarregar.

### 8. Arquivos ligados a pessoas
- `MediaFiles` ganha `LeadId` e `OwnerId` opcionais (`ON DELETE SET NULL`), além de `IsSiteAsset` para a foto da sede.
- A rota pública passa a servir: imagem/vídeo público de anúncio publicado **ou** a mídia definida como foto da sede. Arquivos ligados só a pessoa nunca são públicos.
- A lista do banco de arquivos traz nomes do cliente e do proprietário; os painéis laterais de cliente e proprietário filtram essa mesma lista no cliente (sem endpoint novo). A foto da sede não aparece no banco de arquivos.

### 9. Foto da sede, avaliação no Google e redes sociais
- `SiteSettings.HeadquartersImageId` (FK para `MediaFiles`, `SET NULL`). `PUT /api/admin/settings/headquarters-image` (multipart, valida como imagem com o `FileInspector`, limite de 15 MB) substitui a anterior e apaga o arquivo antigo depois do commit; `DELETE` remove. O envio vai direto para a origem da API, como os outros uploads.
- `SiteSettings.GoogleReviewUrl`, validado como HTTPS em domínios do Google (`g.page`, `google.com` e subdomínios, `goo.gl`, `maps.app.goo.gl`).
- Configurações públicas expõem a URL pública da foto, o link de avaliação e as redes.
- Ícones das redes em SVG inline (traçados do Simple Icons, licença CC0), porque o lucide não tem mais ícones de marca. Componentes `SocialLinks` e `GoogleReviewButton` no rodapé e na página de contato; o botão de avaliação também aparece na confirmação dos formulários de contato e de visita.

### 10. Visitantes inscritos (newsletter)
- Nova entidade `Subscriber`: nome, telefone, dígitos do telefone (índice único por tenant para evitar duplicatas), e-mail opcional, idioma, consentimento, data de entrada no grupo, observações.
- `POST /api/public/subscribers` (limite dos formulários públicos, honeypot) faz upsert pelo telefone. `/api/admin/subscribers` lista, atualiza (observações, entrada no grupo) e exclui; `/export` gera CSV UTF-8 com BOM e `;` (abre direto no Excel em português e espanhol).
- Permissões novas `subscribers.read` e `subscribers.write`, incluídas no papel admin.
- O telefone passa a ser o dado principal da inscrição (o canal é o WhatsApp); e-mail fica opcional. O texto do rodapé e da política de privacidade explicam que as novidades chegam por grupo de WhatsApp.
- Migração: leads `Newsletter` viram inscritos (deduplicados pelo telefone, mantendo o mais recente) e são removidos dos leads. A API pública de leads passa a recusar `Newsletter`; o valor continua no enum só para não quebrar dados antigos.
- Alternativa descartada: filtrar os leads por origem. Inscritos não são oportunidades de venda; misturá-los poluiria o funil de clientes e o indicador de leads novos.

### 11. Perguntas frequentes no banco
- `FaqItems` (ordem, ativa) e `FaqTranslations` (idioma, pergunta, resposta), no mesmo padrão das zonas. CRUD em `/api/admin/faqs` com reordenação; `GET /api/public/faqs?locale=` com fallback para espanhol e tag de cache `faqs`.
- As quatro perguntas padrão entram por `HasData` (ids fixos, como as zonas), com os textos atuais de es, pt e en; gn recebe o espanhol.
- O site lê do banco na home (primeiras quatro ativas) e na página de FAQ (todas), e o `FAQPage` do JSON-LD usa os mesmos dados. As perguntas saem dos arquivos de mensagens.

### 12. Proprietários e clientes
- `Owners.Interest` (`Sell`, `Rent`, `SellOrRent`, `Other`, padrão `Other`). O comando de salvar proprietário recebe `PropertyIds`: vincula os escolhidos (tirando de outro proprietário, se for o caso) e desvincula os que saíram, na mesma transação.
- `Leads.Document` (até 30 caracteres). A conversão de proposta em proprietário copia o documento e vincula o anúncio de interesse.

### 13. Agenda
- `Visits.PropertyId` anulável; novos `OwnerId` (`SET NULL`), `Kind` (`Showing`, `Evaluation`, `Other`; padrão `Showing`) e `Location` (até 200 caracteres).
- `ClientName` continua gravado como **nome de exibição** do compromisso: nome livre, do lead ou, sem eles, do proprietário. Assim a etiqueta continua legível se a pessoa for excluída.
- Regras: ao menos uma pessoa; `Showing` exige anúncio; conflito de horário só quando há anúncio; data no passado recusada na criação e quando a edição muda a data para o passado.
- `DELETE /api/admin/visits/{id}`.
- Painel: formulário com tipo, anúncio (opcional exceto para visita), cliente, proprietário e local; data, hora e duração numa linha com rótulos curtos e `items-end` (duração com sufixo "min" dentro do campo); detalhe com edição completa e exclusão.

### 14. Usuários visíveis em Configurações
- `GET /api/admin/users` (`settings.read`) com usuário, nome, e-mail, papel, situação e último acesso. O painel mostra um cartão "Acesso ao painel" somente leitura, com a dica de que usuários são gerenciados pela ferramenta de administração.

### 15. Site: remoção de botões
- Saem o botão "Atendimento VIP" do cabeçalho (desktop e menu móvel) e "Dúvidas pelo WhatsApp" do hero. O modal de contato continua disponível pelos outros chamados (estado vazio, Sobre nós, FAQ) e o WhatsApp flutuante permanece.

## Risks / Trade-offs

- [Admin esquece o novo usuário depois da migração] → o painel mostra o usuário em Configurações e Minha conta; o README documenta o `rename-user`; o admin de teste vira `admin`.
- [Corrida ao trocar o destaque do mês] → índice único filtrado no banco; a segunda requisição concorrente falha com 409 em vez de deixar dois destaques.
- [Exclusão definitiva apaga fotos sem volta] → confirmação explícita no painel, sugestão de arquivar, documentos preservados; o README recomenda backup do armazenamento.
- [Arquivo físico órfão se a exclusão do disco falhar] → exclusão após o commit com log; arquivos órfãos não são servidos (não há registro) e podem ser limpos depois.
- [Coordenadas das zonas imprecisas] → marcadas como aproximadas no site, fora do JSON-LD do anúncio e ajustáveis no painel.
- [Cotações de EUR/ARS desatualizadas, principalmente o peso argentino] → data da cotação visível no site e aviso de conversão aproximada; valores iniciais a revisar.
- [Upsert de inscrito pelo telefone sobrescreve nome] → comportamento esperado (a pessoa reenviou os dados); a data de consentimento é atualizada.
- [Escopo grande numa mudança só] → tarefas em fases com commit ao fim de cada uma, como na mudança anterior.

## Migration Plan

1. Arquivar `add-public-site-and-admin-panel` (as tarefas abertas dela dependem de produção).
2. Uma migração por fase de backend, todas com teste de integração sobre banco limpo e sobre banco com dados (usuários, leads `Newsletter`).
3. Ordem dos dados na migração de usuários e inscritos: adicionar colunas/tabelas → copiar/transformar por SQL → restrições (`NOT NULL`, índices únicos) → remover dados antigos.
4. Rollback: cada fase é um commit; `dotnet ef database update <migração anterior>` reverte o esquema. As migrações de dados têm `Down` que recria os leads `Newsletter` a partir dos inscritos e remove a coluna de usuário.
5. Depois de aplicar localmente, entrar com o usuário `admin` (senha inalterada).

## Open Questions

- **Terreno "isolado ou compartilhado"**: confirmar o significado (lote exclusivo do imóvel vs. lote dividido com outras unidades) e os rótulos em espanhol.
- **Capacidade de pessoas**: vale só para aluguel ou também para venda? A proposta mostra para ambos quando informada.
- **Link do Google**: obter no Perfil da Empresa no Google ("Pedir avaliações"); confirmar se a imobiliária já tem perfil.
- **Cotações iniciais** de EUR e ARS e a fonte que o dono usará para atualizá-las.
- **Coordenadas** reais da sede e dos centros das zonas.
- **Usuários iniciais**: quais nomes de usuário o dono quer para cada admin.
