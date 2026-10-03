## MODIFIED Requirements

### Requirement: Agendamento de visitas
O admin SHALL poder agendar, editar e cancelar compromissos via `/api/admin/visits`, informando o tipo (`Showing` visita de cliente a imóvel, `Evaluation` avaliação de imóvel com proprietário, `Other` outro), anúncio opcional, cliente (lead existente ou nome livre) opcional, proprietário opcional, local opcional (até 200 caracteres), data e hora, duração (padrão 60 minutos) e observações. Todo compromisso SHALL ter ao menos uma pessoa: lead, nome de cliente ou proprietário. Uma visita do tipo `Showing` SHALL exigir anúncio. Datas SHALL ser gravadas em UTC e exibidas no fuso `America/Asuncion`.

#### Scenario: Visita válida
- **WHEN** o admin agenda uma visita `Showing` futura para um anúncio existente e um cliente
- **THEN** a API retorna HTTP 201 com status `Scheduled`

#### Scenario: Avaliação sem anúncio
- **WHEN** o admin agenda uma `Evaluation` com um proprietário e o local "Área 1, casa 45", sem anúncio
- **THEN** a API retorna HTTP 201 e o compromisso aparece no calendário com o nome do proprietário

#### Scenario: Visita de cliente sem anúncio
- **WHEN** o tipo é `Showing` e nenhum anúncio é informado
- **THEN** a API retorna HTTP 400

#### Scenario: Compromisso sem pessoa
- **WHEN** não há lead, nome de cliente nem proprietário
- **THEN** a API retorna HTTP 400

#### Scenario: Visita vinculada a lead
- **WHEN** a visita é agendada para um lead existente
- **THEN** o status do lead passa para `VisitScheduled` se ainda estiver em `New` ou `Contacted`

#### Scenario: Data no passado
- **WHEN** a data e hora de um novo compromisso já passou, ou a edição move um compromisso para o passado
- **THEN** a API retorna HTTP 400

#### Scenario: Edição de compromisso passado sem mudar a data
- **WHEN** o admin edita as observações de um compromisso que já aconteceu, mantendo data e hora
- **THEN** a alteração é salva

#### Scenario: Conflito no mesmo imóvel
- **WHEN** já existe compromisso `Scheduled` para o mesmo anúncio em horário que se sobrepõe
- **THEN** a API retorna HTTP 409 informando o conflito

### Requirement: Calendário mensal
A API SHALL expor `GET /api/admin/visits?from=&to=` retornando os compromissos do intervalo, para o painel montar o calendário mensal, limitado a 62 dias por consulta, com tipo, anúncio, cliente, proprietário, local, hora e status.

#### Scenario: Visitas do mês
- **WHEN** o painel pede o intervalo de 1 a 31 de outubro
- **THEN** a API retorna todos os compromissos desse intervalo com tipo, anúncio, cliente, proprietário, local, hora e status

#### Scenario: Intervalo grande demais
- **WHEN** o intervalo pedido passa de 62 dias
- **THEN** a API retorna HTTP 400

## ADDED Requirements

### Requirement: Exclusão de compromisso
O admin SHALL poder excluir um compromisso via `DELETE /api/admin/visits/{id}`, removendo-o do calendário e dos próximos compromissos.

#### Scenario: Exclusão
- **WHEN** o admin exclui uma visita agendada
- **THEN** a API retorna HTTP 204 e a visita não aparece mais no calendário

#### Scenario: Compromisso inexistente
- **WHEN** o id não existe no tenant
- **THEN** a API retorna HTTP 404

### Requirement: Compromissos ligados a pessoas e anúncios excluídos
Excluir um anúncio, lead ou proprietário SHALL manter os compromissos ligados, apenas sem o vínculo correspondente, preservando o nome do cliente gravado no compromisso.

#### Scenario: Proprietário excluído
- **WHEN** o proprietário de uma avaliação agendada é excluído
- **THEN** o compromisso continua no calendário sem proprietário vinculado
