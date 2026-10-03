> Convenção TDD: em toda tarefa marcada **(TDD)**, escrever primeiro os testes dos cenários das specs citadas, vê-los falhar, implementar até passar e refatorar. Cada fase termina com `dotnet test`, `npm run lint`, `npm run test` e `npm run build` verdes e um commit. Textos novos entram em `messages/es.json`, `pt.json` e `en.json`, seguidos de `node scripts/sync-guarani.mjs`.

## 1. Preparação

- [x] 1.1 Arquivar `add-public-site-and-admin-panel` (`openspec archive`), confirmando com o usuário, para que as specs alteradas existam em `openspec/specs/`
- [x] 1.2 Conferir que `openspec validate refine-business-rules` continua válido depois do arquivamento

## 2. Login por usuário (spec `admin-auth`, `site-settings`)

- [ ] 2.1 (TDD) Domínio: `User.Username` normalizado (minúsculas, sem espaços nas pontas), formato 3–32 `[a-z0-9._-]`, `Email` opcional
- [ ] 2.2 (TDD) Migração: coluna anulável, preenchimento por SQL a partir do e-mail (normalização, complemento para menos de 3 caracteres, sufixo em repetições), `NOT NULL`, índice único `(TenantId, Username)` e índice único filtrado de e-mail; teste de integração com contas `ana@a.com` e `ana@b.com`
- [ ] 2.3 (TDD) Login por usuário (sem diferenciar maiúsculas, recusa e-mail, mensagem genérica, `SimulateVerify` para usuário inexistente), JWT com `preferred_username`, `me` e resposta de login com `username`
- [ ] 2.4 (TDD) CLI: `create-admin --username --name [--email]`, `reset-password --username`, `rename-user --username --new-username` (revoga refresh tokens); erros para usuário repetido e formato inválido
- [ ] 2.5 (TDD) `GET /api/admin/users` (`settings.read`) sem dados sensíveis
- [ ] 2.6 Frontend: modal de login com campo "Usuário" (`autocomplete="username"`, foco inicial), tipos `AuthUser.username`, Minha conta mostrando o usuário
- [ ] 2.7 README: criação de admin com `--username`, `rename-user` e o usuário `admin` do ambiente local
- [ ] 2.8 Commit da fase

## 3. Moedas EUR e ARS (specs `site-settings`, `internationalization`, `public-website`)

- [ ] 3.1 (TDD) `Currency` com `EUR` e `ARS`; `SiteSettings.EurPerUsd` e `ArsPerUsd` (> 0, valores iniciais na migração), data da cotação atualizada ao mudar qualquer cotação; configurações públicas com as cinco cotações
- [ ] 3.2 (TDD) Preço em USD da busca e da ordenação cobrindo as cinco moedas (teste com anúncio em EUR na faixa de preço)
- [ ] 3.3 (TDD) Frontend: `Rates` como mapa por moeda, `convertPrice` e `formatPrice` com `€` e `AR$` (testes em es, pt e en)
- [ ] 3.4 Seletor de moeda do site, simulador, formulário do anúncio e cotações em Configurações com EUR e ARS
- [ ] 3.5 Commit da fase

## 4. Dados novos do anúncio (spec `property-listings`, `public-website`, `seo-geo`)

- [ ] 4.1 (TDD) Domínio e validação: `MaxOccupants` (1–100), `LotType`, `HasPool`, `Furnishing`, `HasAirConditioning`, `PetsAllowed` anuláveis; `PetNotes` (até 300) na tradução com fallback para espanhol
- [ ] 4.2 Migração e DTOs admin/públicos (detalhe e cartão) com os novos campos
- [ ] 4.3 Painel: seletores de três estados ("Não informado", "Sim", "Não"), capacidade, terreno, mobília e observação de pets nas abas de idioma
- [ ] 4.4 Site: novos itens em "Dados do imóvel" com ícones, só quando informados, nos quatro idiomas
- [ ] 4.5 (TDD) JSON-LD: `occupancy` e `petsAllowed` para tipos de acomodação, `amenityFeature` para piscina, ar-condicionado, mobília e terreno; campos ausentes omitidos
- [ ] 4.6 Commit da fase

## 5. Destaque do mês e exclusão de anúncio (spec `property-listings`, `admin-panel`, `public-website`)

- [ ] 5.1 (TDD) `IsMonthlyHighlight` com regras de domínio (só publicado e disponível/reservado; limpo ao despublicar, arquivar, vender ou alugar) e índice único filtrado por tenant
- [ ] 5.2 (TDD) `PUT /api/admin/properties/{id}/monthly-highlight`: limpa o anterior com `ExecuteUpdate` antes de marcar, liga `IsFeatured`, revalida; teste de troca A→B e de recusa para rascunho
- [ ] 5.3 (TDD) `featured` com `isMonthlyHighlight` e destaque do mês primeiro
- [ ] 5.4 (TDD) `DELETE /api/admin/properties/{id}`: anula vínculos de leads, visitas, documentos e eventos; remove galeria e anúncio; apaga arquivos físicos após o commit; revalida anúncio, listagem, destaques, sitemap e `llms.txt`; FKs com `ON DELETE SET NULL`
- [ ] 5.5 Painel: ação "Destaque do mês" com aviso de substituição, indicador na lista, botão "Excluir" com confirmação que sugere arquivar
- [ ] 5.6 Site: topo da home usa o destaque do mês (fallback para o destaque mais recente)
- [ ] 5.7 Commit da fase

## 6. Mapa: zonas com coordenadas e sede (spec `site-settings`, `property-listings`, `public-website`, `admin-panel`)

- [ ] 6.1 (TDD) `Zone.Latitude/Longitude` com validação de intervalo; migração com coordenadas aproximadas das cinco zonas iniciais; DTOs admin e públicos
- [ ] 6.2 (TDD) Projeção pública de mapa e detalhe com coordenadas da zona e `approximateLocation` quando o anúncio não tem coordenadas; anúncios sem nenhuma coordenada fora do mapa
- [ ] 6.3 Componente `LocationPicker` (react-leaflet sob demanda, clique e arraste) no formulário do anúncio e no da zona
- [ ] 6.4 Mapa do site com camada da sede (ícone próprio, acima dos demais, popup com endereço e rota) na home e no contato; contato deixa de usar o cartão falso de imóvel; popup e página do anúncio avisam "localização aproximada"; JSON-LD do anúncio sem `geo` quando aproximado
- [ ] 6.5 Commit da fase

## 7. Clientes e proprietários (spec `leads`, `property-owners`, `admin-panel`)

- [ ] 7.1 (TDD) `Lead.Document` (até 30) no cadastro, edição e busca
- [ ] 7.2 (TDD) `Owner.Interest` (padrão `Other`) e `PropertyIds` no salvar: vincula, move de outro proprietário, desvincula os retirados, recusa id inexistente sem alterar nada
- [ ] 7.3 (TDD) Conversão de proposta copia documento e vincula o anúncio de interesse
- [ ] 7.4 Painel: documento em Clientes (formulário, tabela, busca); interesse e seleção múltipla de anúncios em Proprietários
- [ ] 7.5 Commit da fase

## 8. Agenda (spec `visit-scheduling`, `admin-panel`)

- [ ] 8.1 (TDD) `Visit.PropertyId` anulável, `OwnerId`, `Kind`, `Location`; `ClientName` como nome de exibição (livre, do lead ou do proprietário); regras: ao menos uma pessoa, `Showing` exige anúncio, conflito só com anúncio, passado recusado na criação e ao mover a data
- [ ] 8.2 (TDD) `DELETE /api/admin/visits/{id}`; exclusão de anúncio, lead ou proprietário mantém o compromisso sem o vínculo
- [ ] 8.3 DTOs de calendário e próximos compromissos com tipo, proprietário e local
- [ ] 8.4 Painel: formulário com tipo, anúncio opcional, cliente, proprietário e local; data, hora e duração alinhadas numa linha (empilhadas no celular); etiqueta com anúncio ou nome da pessoa; detalhe com edição completa e exclusão com confirmação
- [ ] 8.5 Commit da fase

## 9. Arquivos ligados a pessoas e foto da sede (spec `media-library`, `site-settings`, `admin-panel`)

- [ ] 9.1 (TDD) `MediaFile.LeadId`, `OwnerId` e `IsSiteAsset`; upload e edição com os vínculos; lista com nomes do cliente e do proprietário; foto da sede fora do banco de arquivos
- [ ] 9.2 (TDD) Rota pública serve a foto da sede e recusa arquivos ligados só a pessoas
- [ ] 9.3 (TDD) `PUT`/`DELETE /api/admin/settings/headquarters-image` (só imagem, até 15 MB, troca apaga a anterior após o commit) e URL pública nas configurações públicas
- [ ] 9.4 Painel: vínculos de cliente e proprietário no envio e na edição de arquivos, filtro por tipo de vínculo, arquivos no painel lateral de cliente e de proprietário, foto da sede em Configurações com pré-visualização, troca e remoção
- [ ] 9.5 Site: foto da sede em "Tradição e transparência" e em Sobre nós (fallback atual); imagem da sede no JSON-LD `RealEstateAgent`
- [ ] 9.6 Commit da fase

## 10. Configurações: Google, redes e usuários (spec `site-settings`, `public-website`, `admin-panel`)

- [ ] 10.1 (TDD) `GoogleReviewUrl` com validação de HTTPS e domínios do Google; exposto nas configurações públicas
- [ ] 10.2 Painel: campo do link de avaliação e cartão "Acesso ao painel" com os usuários (somente leitura)
- [ ] 10.3 Site: `SocialLinks` (ícones SVG de Facebook, Instagram, YouTube e TikTok, só os preenchidos, nome acessível) e `GoogleReviewButton` no rodapé e no contato; botão de avaliação na confirmação dos formulários de contato e de visita
- [ ] 10.4 Site: remover "Atendimento VIP" do cabeçalho (desktop e menu móvel) e "Dúvidas pelo WhatsApp" do hero
- [ ] 10.5 Commit da fase

## 11. Perguntas frequentes (spec `faq-management`, `public-website`, `seo-geo`, `admin-panel`)

- [ ] 11.1 (TDD) `FaqItem` e `FaqTranslation`, CRUD em `/api/admin/faqs` com reordenação e ativação, espanhol obrigatório, limites de tamanho, revalidação com a tag `faqs`
- [ ] 11.2 (TDD) Perguntas padrão por `HasData` (4 perguntas em es, pt, en; gn = es) e teste de banco novo
- [ ] 11.3 (TDD) `GET /api/public/faqs?locale=` com fallback para espanhol
- [ ] 11.4 Painel: lista ordenável em Configurações com painel lateral por idioma e ativar/desativar
- [ ] 11.5 Site: home (quatro primeiras ativas), página de FAQ e `FAQPage` do JSON-LD lendo da API com a tag `faqs`; remover as perguntas dos arquivos de mensagens
- [ ] 11.6 Commit da fase

## 12. Visitantes inscritos (spec `newsletter-subscribers`, `leads`, `public-website`, `admin-panel`)

- [ ] 12.1 (TDD) Entidade `Subscriber` com dígitos do telefone únicos por tenant; `POST /api/public/subscribers` com upsert, consentimento, honeypot e limite de requisições
- [ ] 12.2 (TDD) `/api/admin/subscribers` (listar, atualizar observações e entrada no grupo, excluir) e `/export` em CSV UTF-8 com BOM e `;`, com filtro de pendentes; permissões `subscribers.read` e `subscribers.write` no papel admin
- [ ] 12.3 (TDD) API pública de leads recusa `Newsletter`; migração transfere leads `Newsletter` para inscritos (deduplicados) e os remove dos leads
- [ ] 12.4 Painel: seção Visitantes no menu (busca com debounce, filtro pendentes/no grupo/todos, ordenação, WhatsApp, marcação rápida, observações, exclusão, exportar CSV); origem `Newsletter` fora dos filtros de leads
- [ ] 12.5 Site: formulário "Novidades exclusivas" com nome, WhatsApp e e-mail opcional, texto explicando o grupo de WhatsApp, envio para `/api/public/subscribers`; política de privacidade atualizada nos quatro idiomas
- [ ] 12.6 Commit da fase

## 13. Dashboard por período (spec `site-analytics`, `admin-panel`)

- [ ] 13.1 (TDD) `DashboardPeriod` (padrão do mês até hoje vs. mês anterior; período informado vs. período anterior de mesma duração; máximo 366 dias; `from` ≤ `to`)
- [ ] 13.2 (TDD) `summary`, `traffic` (diário até 92 dias, mensal acima) e `top-properties` com `from`/`to`; meta proporcional aos meses; aluguéis ativos no fim do período
- [ ] 13.3 Painel: seletor de período (mês atual, mês anterior, 7, 30 e 90 dias, ano, personalizado) refletido na URL, validação do intervalo no cliente, gráfico com rótulos por dia ou mês
- [ ] 13.4 Commit da fase

## 14. Fechamento

- [ ] 14.1 Traduções de todos os textos novos em es, pt e en, `sync-guarani.mjs` e `gn-pending.md` atualizado
- [ ] 14.2 Atualizar README (usuário no login, CLI, moedas, visitantes e exportação, perguntas frequentes, foto da sede, link do Google)
- [ ] 14.3 Verificação completa: `dotnet build`, `dotnet test`, `npm run lint`, `npm run test`, `npm run build`
- [ ] 14.4 Fluxo no navegador: login com usuário; anúncio com os campos novos, destaque do mês e exclusão; anúncio sem coordenadas no mapa; sede no mapa; avaliação com proprietário na agenda e exclusão; documento de cliente; inscrição na newsletter aparecendo em Visitantes e exportação; FAQ editada refletindo no site; foto da sede; botões de redes e Google; dashboard com período
- [ ] 14.5 Registrar no `design.md` as respostas das perguntas em aberto
- [ ] 14.6 Commit final (push só com confirmação do usuário)
