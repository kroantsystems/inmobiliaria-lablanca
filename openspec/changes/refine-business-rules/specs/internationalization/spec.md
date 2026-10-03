## MODIFIED Requirements

### Requirement: Formatação regional
Datas, números e moedas SHALL ser formatados conforme o idioma (`es` → `es-PY`, `pt` → `pt-BR`, `en` → `en-US`, `gn` → `es-PY`). Os preços SHALL usar o prefixo da moeda (`USD`, `₲`, `R$`, `€`, `AR$`) e nenhuma casa decimal.

#### Scenario: Preço em guaranis
- **WHEN** um preço em PYG é exibido em espanhol
- **THEN** usa o símbolo ₲, separador de milhar com ponto e nenhuma casa decimal

#### Scenario: Preço em euro em inglês
- **WHEN** um preço de 92.000 EUR é exibido em inglês
- **THEN** aparece como `€ 92,000`

#### Scenario: Preço em peso argentino
- **WHEN** um preço em ARS é exibido em português
- **THEN** aparece com o prefixo `AR$`, separador de milhar com ponto e sem casas decimais

#### Scenario: Data em guarani
- **WHEN** uma data é exibida com idioma `gn`
- **THEN** é formatada com as regras de `es-PY`, sem erro no navegador
