# media-library Specification

## Purpose
TBD - created by archiving change add-public-site-and-admin-panel. Update Purpose after archive.
## Requirements
### Requirement: Upload com validação de tipo e tamanho
A API SHALL aceitar uploads em `POST /api/admin/files` (multipart) apenas dos tipos e limites abaixo, configuráveis em `appsettings`:
- Imagens `.png`, `.jpg`, `.jpeg`, `.webp`: até 50 MB.
- Vídeos `.mp4`, `.webm`, `.mov`: até 50 MB.
- Documentos `.pdf`, `.doc`, `.docx`: até 15 MB.

A validação SHALL conferir extensão, `Content-Type` e assinatura binária (magic bytes) do conteúdo.

#### Scenario: Imagem válida
- **WHEN** o admin envia um `.jpg` de 8 MB com conteúdo JPEG real
- **THEN** a API retorna HTTP 201 com id, nome original, tipo, tamanho e categoria `Image`

#### Scenario: Imagem acima do limite
- **WHEN** o admin envia um `.png` de 51 MB
- **THEN** a API retorna HTTP 400 informando o limite de 50 MB

#### Scenario: Documento acima do limite
- **WHEN** o admin envia um `.pdf` de 20 MB
- **THEN** a API retorna HTTP 400 informando o limite de 15 MB

#### Scenario: Extensão não permitida
- **WHEN** o admin envia um `.exe`, `.svg`, `.html` ou `.zip`
- **THEN** a API retorna HTTP 400

#### Scenario: Conteúdo disfarçado
- **WHEN** o admin envia um arquivo com extensão `.jpg` cujo conteúdo não é JPEG
- **THEN** a API retorna HTTP 400

### Requirement: Armazenamento seguro de arquivos
O sistema SHALL gravar cada arquivo com nome gerado (GUID) fora da pasta pública da aplicação, por meio de uma abstração de armazenamento, e SHALL guardar no banco nome original, tipo, tamanho, categoria, descrição, vínculo opcional a anúncio, visibilidade, ordem, capa, texto alternativo, data e autor do upload.

#### Scenario: Nome original com caminho malicioso
- **WHEN** o arquivo enviado se chama `../../appsettings.json.jpg`
- **THEN** o arquivo é gravado com nome GUID dentro da pasta de armazenamento e o nome original é guardado apenas como metadado

### Requirement: Vínculo com anúncio e descrição
O admin SHALL poder vincular um arquivo a um anúncio ou deixá-lo sem vínculo (documento geral), editar descrição e texto alternativo, e trocar o vínculo depois.

#### Scenario: Upload vinculado
- **WHEN** o admin envia uma foto escolhendo o anúncio "Residencia Aura Light"
- **THEN** o arquivo aparece na galeria desse anúncio no painel

#### Scenario: Documento sem vínculo
- **WHEN** o admin envia um `.docx` sem escolher anúncio
- **THEN** o arquivo fica no banco marcado como "Sem vínculo"

### Requirement: Arquivos públicos e privados
Documentos SHALL ser sempre privados. Imagens e vídeos SHALL ser públicos somente quando marcados como públicos e vinculados a um anúncio publicado. Arquivos públicos SHALL ser servidos por `GET /api/public/media/{id}`; arquivos privados SHALL ser baixados apenas por `GET /api/admin/files/{id}/content` com autenticação.

#### Scenario: Foto de anúncio publicado
- **WHEN** um visitante acessa a URL pública de uma imagem pública de anúncio publicado
- **THEN** a API retorna a imagem com cache de longa duração e `X-Content-Type-Options: nosniff`

#### Scenario: Documento pela rota pública
- **WHEN** alguém pede um contrato `.pdf` pela rota `/api/public/media/{id}`
- **THEN** a API retorna HTTP 404

#### Scenario: Documento pela rota administrativa
- **WHEN** um admin autenticado baixa um documento
- **THEN** a API retorna o arquivo com `Content-Disposition: attachment` e o nome original

#### Scenario: Download privado sem token
- **WHEN** a rota administrativa de download é chamada sem token
- **THEN** a API retorna HTTP 401

### Requirement: Galeria do anúncio
O admin SHALL poder definir a ordem das imagens e a imagem de capa de um anúncio. Cada anúncio SHALL ter no máximo uma capa.

#### Scenario: Troca de capa
- **WHEN** o admin marca outra imagem como capa
- **THEN** a capa anterior é desmarcada e a nova aparece primeiro no site

#### Scenario: Reordenação
- **WHEN** o admin envia a nova ordem das imagens
- **THEN** a galeria pública passa a seguir essa ordem

### Requirement: Listagem e exclusão no banco de arquivos
A API SHALL listar os arquivos do tenant em páginas de até 500 itens com metadados e vínculo, e SHALL permitir excluir um arquivo, apagando registro e conteúdo armazenado.

#### Scenario: Exclusão de arquivo
- **WHEN** o admin exclui um documento
- **THEN** o registro e o arquivo físico são removidos

#### Scenario: Última imagem de anúncio publicado
- **WHEN** o admin tenta excluir a única imagem pública de um anúncio publicado
- **THEN** a API retorna HTTP 409 explicando que o anúncio precisa de ao menos uma imagem

