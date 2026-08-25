# Cogniva

Cogniva is an LLM-powered document intelligence system for uploading, processing, and querying personal PDF and DOCX documents. Its goal is to demonstrate a complete information system with document management, semantic retrieval, grounded AI answers, summaries, and source citations.

## UI language convention

All user-facing application text is written in natural Serbian Latin. Source code identifiers, API contracts, database names, and other technical elements remain in English. This convention applies to every current and future frontend feature, including validation, errors, statuses, notifications, empty states, and the AI workspace.

> **Development status:** Cogniva is currently in Phase 4. Authentication, secure document management, PDF/DOCX extraction, chunking, and embeddings are implemented. RAG answers, summaries, and chat will be implemented in later phases.

## Technology stack

- Backend: ASP.NET Core Web API, .NET 8, EF Core 8, JWT Bearer authentication
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

Configure the JWT signing secret through an environment variable or .NET user secrets. Never commit a production secret:

```bash
dotnet user-secrets set "Jwt:Secret" "replace-with-a-local-secret-at-least-32-characters-long" --project backend/Cogniva.Api
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
dotnet ef database update --project backend/Cogniva.Api --startup-project backend/Cogniva.Api
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

## Authentication API

The current API endpoints are:

```http
GET /api/health
POST /api/auth/register
POST /api/auth/login
GET /api/auth/me
GET /api/documents
GET /api/documents/{id}
POST /api/documents/upload
DELETE /api/documents/{id}
POST /api/documents/{id}/process
```

`GET /api/auth/me` requires a valid bearer token. Swagger exposes an Authorize action for testing protected endpoints.

The browser client stores the MVP access token in `sessionStorage`; it is removed on logout or a `401` response. A production deployment should prefer a short-lived access token delivered through a secure `HttpOnly`, `Secure`, `SameSite` cookie and should evaluate an appropriate refresh-token strategy.

## Database model

The initial `InitialCreate` migration creates users, documents, document chunks, conversations, conversation-document links, messages, and message sources. It also enables PostgreSQL's `vector` extension. `DocumentChunk.Embedding` intentionally uses unconstrained `vector` for now; its dimension will be fixed after the embedding provider is chosen.

## Current limitations

- Scanned PDFs without a text layer are not supported because OCR is outside the current scope
- Embeddings are stored, but semantic search is not exposed yet
- No LLM, RAG, summaries, or chat workflow
- No refresh tokens

## Local document storage

Uploaded documents are stored under `backend/Cogniva.Api/Storage/uploads` by default. The physical filename is a generated GUID with the validated `.pdf` or `.docx` extension; the original filename is metadata only and is never used as a server-side path. Configure storage through:

```text
FileStorage__UploadPath=Storage/uploads
FileStorage__MaxFileSizeMb=20
```

Only PDF and DOCX uploads are accepted. Every list, details, and delete operation is filtered by the authenticated user's server-side identity.

## Document processing flow

```text
Uploaded → Processing → Ready
                    ↘ Failed
```

Processing reads PDF files page by page with PdfPig and preserves page numbers. DOCX paragraphs, headings, lists, and table rows are read in document order with Open XML; DOCX page numbers remain `null` because pagination is not part of the document structure.

Normalized text is split with a paragraph-aware strategy. Long paragraphs are split at sentence boundaries and only then by words. The default target is approximately 750 tokens with a 120-token overlap and an 80-token minimum. Token counts use a documented approximation of four characters per token. When a chunk spans multiple PDF pages, its `PageNumber` represents the first page contributing content to that chunk.

After extraction and chunking, content is sent in one batch to an OpenAI-compatible `/embeddings` endpoint. New chunks replace old chunks only after extraction and embedding generation succeed, and the database replacement is transactional.

Configure processing with:

```text
Chunking__TargetTokens=750
Chunking__OverlapTokens=120
Chunking__MinimumTokens=80

AI__Provider=OpenAICompatible
AI__BaseUrl=https://api.openai.com/v1/
AI__ApiKey=
AI__EmbeddingModel=text-embedding-3-small
AI__EmbeddingDimensions=1536
```

The pgvector column remains unconstrained so the embedding model can be changed through configuration during development. The application validates that every returned vector matches `AI__EmbeddingDimensions`. Existing chunks must be reprocessed when the model or dimension changes.
