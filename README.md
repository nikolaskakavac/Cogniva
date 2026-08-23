# Cogniva

Cogniva is an LLM-powered document intelligence system for uploading, processing, and querying personal PDF and DOCX documents. Its goal is to demonstrate a complete information system with document management, semantic retrieval, grounded AI answers, summaries, and source citations.

> **Development status:** Cogniva is currently in Phase 1. This repository contains the project foundation only. Authentication, persistence models, document processing, embeddings, RAG, summaries, and chat will be implemented in later phases.

## Technology stack

- Backend: ASP.NET Core Web API, .NET 8
- Frontend: React, TypeScript, Vite, React Router, Axios
- Database: PostgreSQL 16 with pgvector
- Local infrastructure: Docker Compose
- API documentation: Swagger / OpenAPI

## Current structure

```text
Cogniva/
├── backend/
│   ├── Cogniva.sln
│   └── Cogniva.Api/
│       ├── Configuration/
│       ├── Controllers/
│       ├── Data/
│       ├── DTOs/
│       ├── ExternalServices/AI/
│       ├── Middleware/
│       ├── Migrations/
│       ├── Models/
│       ├── Services/Interfaces/
│       ├── Storage/uploads/
│       └── Validation/
├── frontend/
│   └── src/
│       ├── api/
│       ├── auth/
│       ├── components/
│       ├── hooks/
│       ├── pages/
│       ├── styles/
│       ├── types/
│       └── utils/
├── .env.example
└── docker-compose.yml
```

## Prerequisites

- .NET SDK 8
- Node.js 20 or newer and pnpm
- Docker Desktop or another Docker Engine with Compose v2

## Environment configuration

Copy `.env.example` to `.env` and adjust local values when needed. `.env` files are ignored by Git.

The backend reads hierarchical configuration from ASP.NET Core configuration providers. Environment variables override JSON settings, for example:

```text
Database__ConnectionString=Host=localhost;Port=5432;Database=cogniva;Username=cogniva;Password=cogniva_dev
```

The frontend API URL is configured through:

```text
VITE_API_BASE_URL=http://localhost:5080
```

AI-related variables shown in `.env.example` are reserved for a later phase and are not used yet.

## Start PostgreSQL and pgvector

From the repository root:

```bash
docker compose up -d postgres
docker compose ps
```

Stop the container with:

```bash
docker compose down
```

The named volume preserves database data between container restarts. Use `docker compose down -v` only when you intentionally want to delete local database data.

## Run the backend

```bash
dotnet restore backend/Cogniva.sln --configfile NuGet.Config
dotnet run --project backend/Cogniva.Api
```

The development API is available at `http://localhost:5080`. Useful endpoints:

- Health: `http://localhost:5080/api/health`
- Swagger: `http://localhost:5080/swagger`

The launch profile can select a different local port; the terminal output is the authoritative URL.

## Run the frontend

```bash
cd frontend
pnpm install
pnpm dev
```

Vite serves the application at `http://localhost:5173` by default.

Create a production build with:

```bash
pnpm build
```

## Phase 1 API

The only application endpoint currently implemented is:

```http
GET /api/health
```

It returns a small JSON response confirming that the API is running. No database connection is required by this endpoint in Phase 1.
