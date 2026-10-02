## ADDED Requirements

### Requirement: Agendamento de visitas
O admin SHALL poder agendar, editar e cancelar visitas via `/api/admin/visits`, informando anúncio, cliente (lead existente ou nome livre), data e hora, duração (padrão 60 minutos) e observações. Datas SHALL ser gravadas em UTC e exibidas no fuso `America/Asuncion`.

#### Scenario: Visita válida
- **WHEN** o admin agenda uma visita futura para um anúncio existente
- **THEN** a API retorna HTTP 201 com status `Scheduled`

#### Scenario: Visita vinculada a lead
- **WHEN** a visita é agendada para um lead existente
- **THEN** o status do lead passa para `VisitScheduled` se ainda estiver em `New` ou `Contacted`

#### Scenario: Data no passado
- **WHEN** a data e hora da nova visita já passou
- **THEN** a API retorna HTTP 400

#### Scenario: Conflito no mesmo imóvel
- **WHEN** já existe visita `Scheduled` para o mesmo anúncio em horário que se sobrepõe
- **THEN** a API retorna HTTP 409 informando o conflito

### Requirement: Calendário mensal
A API SHALL expor `GET /api/admin/visits?from=&to=` retornando as visitas do intervalo, para o painel montar o calendário mensal, limitado a 62 dias por consulta.

#### Scenario: Visitas do mês
- **WHEN** o painel pede o intervalo de 1 a 31 de outubro
- **THEN** a API retorna todas as visitas desse intervalo com anúncio, cliente, hora e status

#### Scenario: Intervalo grande demais
- **WHEN** o intervalo pedido passa de 62 dias
- **THEN** a API retorna HTTP 400

### Requirement: Próximos compromissos
A API SHALL expor os próximos compromissos (visitas `Scheduled` a partir de agora), ordenados por data, limitados a 10.

#### Scenario: Lista de próximos
- **WHEN** existem 15 visitas futuras agendadas
- **THEN** a API retorna as 10 mais próximas

### Requirement: Encerramento da visita
O admin SHALL poder marcar a visita como `Done`, `Cancelled` ou `NoShow`.

#### Scenario: Visita cancelada
- **WHEN** o admin cancela uma visita
- **THEN** ela sai dos próximos compromissos e aparece riscada no calendário
