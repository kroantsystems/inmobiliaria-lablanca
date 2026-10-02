## Why

O site da imobiliária La Blanca ainda não tem código: a pasta do projeto contém apenas a configuração do OpenSpec. Antes de construir funcionalidades (catálogo de imóveis, busca, contato), precisamos de uma base de projeto padronizada com frontend em Next.js e backend em .NET 10, que rode localmente com um comando e sirva de alicerce para as próximas mudanças.

## What Changes

- Criar estrutura de monorepo com `frontend/` (Next.js) e `backend/` (.NET 10) na raiz do projeto.
- Criar app Next.js (App Router, TypeScript, Tailwind CSS, ESLint) com layout base (cabeçalho, rodapé, página inicial placeholder) e cliente HTTP configurado para a API.
- Criar solução .NET 10 com Web API em camadas (`Api`, `Application`, `Domain`, `Infrastructure`) e projeto de testes.
- Configurar Entity Framework Core com PostgreSQL e uma migração inicial vazia.
- Expor endpoint de health check (`GET /health`) e documentação OpenAPI em desenvolvimento.
- Configurar CORS para permitir o frontend local consumir a API.
- Adicionar `docker-compose.yml` com PostgreSQL para desenvolvimento local.
- Adicionar `.gitignore`, `.editorconfig`, `README.md` com instruções de execução e inicializar repositório git.

## Capabilities

### New Capabilities
- `frontend-app-shell`: Aplicação Next.js com layout base, página inicial e configuração de acesso à API.
- `backend-api-foundation`: Web API .NET 10 em camadas com health check, OpenAPI, CORS e acesso a banco via EF Core.
- `dev-environment`: Ambiente de desenvolvimento local (docker-compose com PostgreSQL, variáveis de ambiente, scripts e documentação de execução).

### Modified Capabilities
<!-- Nenhuma: não há specs existentes. -->

## Impact

- Código: novas pastas `frontend/`, `backend/` e arquivos de raiz (`docker-compose.yml`, `README.md`, `.gitignore`, `.editorconfig`).
- Dependências: Node.js 20+ (Next.js, React, Tailwind), .NET 10 SDK (ASP.NET Core, EF Core, Npgsql, xUnit), Docker.
- Sistemas: banco PostgreSQL local via Docker.
- Nenhuma API ou sistema existente afetado (projeto novo).
