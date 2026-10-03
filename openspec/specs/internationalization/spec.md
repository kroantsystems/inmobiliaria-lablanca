# internationalization Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Idiomas suportados
O site público e o painel SHALL suportar espanhol (`es`, padrão, foco Paraguai), português (`pt`), inglês (`en`) e guarani (`gn`), com todas as rotas prefixadas pelo idioma (`/es`, `/pt`, `/en`, `/gn`).

#### Scenario: Acesso à raiz
- **WHEN** um visitante acessa `/` sem preferência salva e com navegador em português
- **THEN** é redirecionado para `/pt`

#### Scenario: Navegador em idioma não suportado
- **WHEN** um visitante acessa `/` com navegador em francês
- **THEN** é redirecionado para `/es`

#### Scenario: Troca de idioma
- **WHEN** o visitante escolhe "Português" no seletor de idioma estando em um anúncio em espanhol
- **THEN** vai para a mesma página em português, usando o slug em português do anúncio

### Requirement: Textos de interface traduzidos
Todo texto fixo de interface SHALL vir de arquivos de mensagens por idioma (`messages/es.json`, `pt.json`, `en.json`, `gn.json`), sem texto fixo no código dos componentes.

#### Scenario: Chaves completas
- **WHEN** o teste de mensagens compara os arquivos de idioma
- **THEN** `pt`, `en` e `gn` possuem exatamente as mesmas chaves que `es`

#### Scenario: Guarani parcial
- **WHEN** um texto ainda não foi traduzido para guarani
- **THEN** o arquivo `gn.json` contém o texto em espanhol para essa chave, marcado na lista de pendências de tradução

### Requirement: Formatação regional
Datas, números e moedas SHALL ser formatados conforme o idioma (`es` → `es-PY`, `pt` → `pt-BR`, `en` → `en-US`, `gn` → `es-PY`).

#### Scenario: Preço em guaranis
- **WHEN** um preço em PYG é exibido em espanhol
- **THEN** usa o símbolo ₲, separador de milhar com ponto e nenhuma casa decimal

#### Scenario: Data em guarani
- **WHEN** uma data é exibida com idioma `gn`
- **THEN** é formatada com as regras de `es-PY`, sem erro no navegador

### Requirement: Idioma enviado à API
O cliente HTTP SHALL enviar o cabeçalho `Accept-Language` com o idioma atual em todas as chamadas à API.

#### Scenario: Erro de validação no idioma da tela
- **WHEN** o admin está em `/pt/admin` e envia um formulário inválido
- **THEN** as mensagens de erro retornadas pela API aparecem em português

### Requirement: Conteúdo de anúncio com fallback
Páginas de anúncio e zona SHALL exibir a tradução do idioma atual e, quando não existir, o conteúdo em espanhol, mantendo a interface no idioma atual.

#### Scenario: Anúncio só em espanhol
- **WHEN** um visitante abre em inglês um anúncio que só tem conteúdo em espanhol
- **THEN** a interface aparece em inglês e título e descrição aparecem em espanhol

