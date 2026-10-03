## MODIFIED Requirements

### Requirement: Vínculo com anúncio e descrição
O admin SHALL poder vincular um arquivo a um anúncio, a um cliente (lead) e/ou a um proprietário, ou deixá-lo sem vínculo (documento geral), editar descrição e texto alternativo, e trocar os vínculos depois. Arquivos vinculados a um cliente ou proprietário SHALL aparecer no cadastro dessa pessoa no painel. Excluir o cliente, o proprietário ou o anúncio SHALL manter o arquivo, apenas sem aquele vínculo, exceto as imagens e vídeos da galeria de um anúncio excluído.

#### Scenario: Upload vinculado
- **WHEN** o admin envia uma foto escolhendo o anúncio "Residencia Aura Light"
- **THEN** o arquivo aparece na galeria desse anúncio no painel

#### Scenario: Documento de pessoa
- **WHEN** o admin envia a cópia do documento de identidade escolhendo o cliente "Rodrigo Silva"
- **THEN** o arquivo aparece no banco de arquivos com a etiqueta do cliente e no cadastro do cliente

#### Scenario: Contrato de proprietário e anúncio
- **WHEN** o admin envia um contrato vinculado ao proprietário "Hernán Gómez" e ao anúncio "Residencia Aura Light"
- **THEN** o arquivo aparece no cadastro do proprietário e na lista de arquivos do anúncio, e continua privado

#### Scenario: Pessoa excluída
- **WHEN** o cliente vinculado a um documento é excluído
- **THEN** o documento continua no banco de arquivos sem vínculo de cliente

#### Scenario: Documento sem vínculo
- **WHEN** o admin envia um `.docx` sem escolher anúncio nem pessoa
- **THEN** o arquivo fica no banco marcado como "Sem vínculo"

### Requirement: Arquivos públicos e privados
Documentos SHALL ser sempre privados. Arquivos vinculados apenas a cliente ou proprietário SHALL ser privados. Imagens e vídeos SHALL ser públicos somente quando marcados como públicos e vinculados a um anúncio publicado, ou quando forem a foto da sede definida nas configurações. Arquivos públicos SHALL ser servidos por `GET /api/public/media/{id}`; arquivos privados SHALL ser baixados apenas por `GET /api/admin/files/{id}/content` com autenticação.

#### Scenario: Foto de anúncio publicado
- **WHEN** um visitante acessa a URL pública de uma imagem pública de anúncio publicado
- **THEN** a API retorna a imagem com cache de longa duração e `X-Content-Type-Options: nosniff`

#### Scenario: Foto da sede
- **WHEN** um visitante acessa a URL pública da foto da sede
- **THEN** a API retorna a imagem, mesmo sem vínculo com anúncio

#### Scenario: Foto de documento pessoal
- **WHEN** alguém pede pela rota pública uma imagem vinculada só a um cliente
- **THEN** a API retorna HTTP 404

#### Scenario: Documento pela rota pública
- **WHEN** alguém pede um contrato `.pdf` pela rota `/api/public/media/{id}`
- **THEN** a API retorna HTTP 404

#### Scenario: Documento pela rota administrativa
- **WHEN** um admin autenticado baixa um documento
- **THEN** a API retorna o arquivo com `Content-Disposition: attachment` e o nome original

#### Scenario: Download privado sem token
- **WHEN** a rota administrativa de download é chamada sem token
- **THEN** a API retorna HTTP 401
