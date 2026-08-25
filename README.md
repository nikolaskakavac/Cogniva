# Cogniva

Cogniva is an LLM-powered document intelligence system for uploading, processing, and querying personal PDF and DOCX documents. Its goal is to demonstrate a complete information system with document management, semantic retrieval, grounded AI answers, summaries, and source citations.

## UI language convention

All user-facing application text is written in natural Serbian Latin. Source code identifiers, API contracts, database names, and other technical elements remain in English. This convention applies to every current and future frontend feature, including validation, errors, statuses, notifications, empty states, and the AI workspace.

> **Development status:** Cogniva MVP is finalized through Phase 6. Authentication, document processing, AI summaries, semantic search, grounded RAG conversations, retry, history, citations, and a polished Serbian UI are implemented. Production deployment still requires additional hardening.

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

AI configuration is backend-only. Never expose an API key through a `VITE_` variable or commit it to the repository.

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
POST /api/documents/{id}/summarize
POST /api/conversations
GET /api/conversations
GET /api/conversations/{id}
POST /api/conversations/{id}/messages
POST /api/conversations/{id}/messages/{messageId}/retry
GET /api/dashboard
```

`GET /api/auth/me` requires a valid bearer token. Swagger exposes an Authorize action for testing protected endpoints.

The browser client stores the MVP access token in `sessionStorage`; it is removed on logout or a `401` response. A production deployment should prefer a short-lived access token delivered through a secure `HttpOnly`, `Secure`, `SameSite` cookie and should evaluate an appropriate refresh-token strategy.

## Database model

The initial `InitialCreate` migration creates users, documents, document chunks, conversations, conversation-document links, messages, and message sources. It also enables PostgreSQL's `vector` extension. `DocumentChunk.Embedding` intentionally uses unconstrained `vector` for now; its dimension will be fixed after the embedding provider is chosen.

## Current limitations

- Scanned PDFs without a text layer are not supported because OCR is outside the current scope
- RAG and summary responses are synchronous; streaming and background queues are not part of the MVP
- Follow-up history is limited and query rewriting is not implemented
- Relevance uses a configurable conservative cosine-distance threshold, not a universal confidence score
- Web search, tools, organizations, billing, and OCR are not implemented
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
AI__ChatModel=
AI__SummaryModel=

RAG__TopK=5
RAG__MaxCosineDistance=0.65
RAG__HistoryMessageLimit=8
RAG__MaxContextCharacters=12000
```

The pgvector column remains unconstrained so the embedding model can be changed through configuration during development. The application validates that every returned vector matches `AI__EmbeddingDimensions`. Existing chunks must be reprocessed when the model or dimension changes.

## Semantic search and RAG

RAG (retrieval-augmented generation) first retrieves relevant document excerpts and only then asks the chat model to answer from that context. Cogniva's flow is:

```text
User question
  → query embedding
  → PostgreSQL/pgvector cosine search
  → ownership + selected-document + Ready filters
  → TopK context with document/page metadata
  → OpenAI-compatible chat completion
  → saved user/assistant messages and MessageSource citations
```

Search runs inside PostgreSQL with pgvector cosine distance; embeddings are not loaded as a table into application memory. The default `TopK` is 5. Results above `RAG__MaxCosineDistance=0.65` are excluded. This threshold is intentionally conservative and should be evaluated for the chosen embedding model and document domain. If no chunk passes it, Cogniva answers that the information was not found and does not call the LLM.

Every query is restricted to the authenticated user's selected Ready documents. Conversation details, messages, and citations use the same ownership boundary. The LLM receives at most the configured number of recent messages (8 by default) and a bounded context. Document text is marked as untrusted data so instructions embedded inside a document cannot override the system prompt.

## Local AI with Ollama

Install Ollama, then pull an embedding model, a chat model, and optionally a smaller summary model manually. For the current local setup:

```bash
ollama pull embeddinggemma
ollama pull qwen3:4b-instruct
ollama pull qwen3:1.7b
```

`nomic-embed-text` is also supported, but changing from `embeddinggemma` to it requires reprocessing every existing document:

```bash
ollama pull nomic-embed-text
```

Example local configuration using .NET User Secrets:

```bash
dotnet user-secrets set "AI:Provider" "OpenAICompatible" --project backend/Cogniva.Api
dotnet user-secrets set "AI:BaseUrl" "http://localhost:11434/v1/" --project backend/Cogniva.Api
dotnet user-secrets set "AI:ApiKey" "" --project backend/Cogniva.Api
dotnet user-secrets set "AI:EmbeddingModel" "embeddinggemma" --project backend/Cogniva.Api
dotnet user-secrets set "AI:EmbeddingDimensions" "768" --project backend/Cogniva.Api
dotnet user-secrets set "AI:ChatModel" "qwen3:4b-instruct" --project backend/Cogniva.Api
dotnet user-secrets set "AI:SummaryModel" "qwen3:1.7b" --project backend/Cogniva.Api
```

Ollama does not require an API key. Cogniva omits the Authorization header when the key is empty; remote OpenAI-compatible providers can still use `AI:ApiKey`. Verify local models with `ollama list`, restart the backend after configuration changes, and reprocess documents whenever the embedding model or dimension changes. Do not mix vectors generated by different models.

`EmbeddingModel` creates vectors for documents and questions; changing it or its dimensions requires reprocessing existing documents. `ChatModel` generates RAG conversation answers. Optional `SummaryModel` generates AI summaries and can use a smaller, faster model; when it is empty, Cogniva falls back to `ChatModel`. Changing either text-generation model does not require document reprocessing because stored embeddings are unchanged. The official Ollama tag for the smaller Qwen3 variant is `qwen3:1.7b`; `qwen3:1.7b-instruct` is not used because that exact official tag is not available.

## AI summary flow

Summaries are available only for Ready documents. Cogniva loads ordered text chunks without embeddings and groups them into bounded blocks. Each block receives a partial Serbian summary; when multiple blocks exist, a final reduce request combines them without repetition. The result is stored in `Document.Summary` and can be regenerated.

```text
Ready chunks → bounded blocks → partial summaries → final summary → Document.Summary
```

Short documents up to `Summary__DirectSummaryMaxCharacters` use one direct LLM call. Longer documents use map-reduce batches controlled by `Summary__PartialBatchMaxCharacters`, `Summary__MaxPartialSummariesPerFinalPrompt`, `Summary__PartialMaxTokens`, and `Summary__FinalMaxTokens`. Document content is treated as untrusted data and prompts require grounded Serbian Latin output.

## Architecture overview

- Controllers expose authenticated HTTP contracts and delegate business logic.
- EF Core services enforce ownership for documents, conversations, messages, dashboard data, and citations.
- PostgreSQL with pgvector performs server-side cosine retrieval.
- Provider-independent embedding and LLM interfaces support OpenAI-compatible services and Ollama.
- React uses typed Axios clients; all user-facing text is Serbian Latin.
- Local uploads use generated server filenames while original names remain metadata.

## MVP demo scenario

1. Register and sign in.
2. Upload a text-based PDF or DOCX document.
3. Process it and wait for Ready status.
4. Generate an AI summary from document details.
5. Create a conversation with the Ready document.
6. Ask a relevant question and inspect its sources.
7. Ask an unrelated question and verify the no-context response.
8. If a local LLM call fails, retry the saved user message without duplicating it.
9. Reopen the conversation and verify persisted history and citations.

## Future improvements

- OCR for scanned documents
- background processing and durable queues
- teams and organizations
- billing and usage limits
- cloud object storage
- streamed AI responses
- richer roles and permissions
- production-grade refresh tokens, observability, rate limiting, and secret management
