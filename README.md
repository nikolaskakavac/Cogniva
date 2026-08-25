# Cogniva

Cogniva je web aplikacija koja korisniku omogućava da otpremi PDF ili DOCX dokument, obradi njegov sadržaj i zatim koristi AI za postavljanje pitanja i generisanje sažetaka. Projekat je napravljen kao praktičan primer povezivanja klasičnog informacionog sistema sa velikim jezičkim modelima.

Korisnički interfejs je na srpskom jeziku, latinicom, dok su nazivi klasa, metoda, API endpointa i ostali tehnički delovi koda na engleskom.

## O projektu

Korisnik prvo pravi nalog i prijavljuje se. Nakon toga može da otpremi PDF ili DOCX dokument. Sistem iz dokumenta izdvaja tekst, deli ga na manje delove (chunkove) i za svaki deo generiše embedding, odnosno vektorsku reprezentaciju teksta. Embeddings se čuvaju u PostgreSQL bazi uz pgvector ekstenziju.

Kada korisnik započne razgovor, bira jedan ili više obrađenih dokumenata. Pitanje se takođe pretvara u embedding, a semantička pretraga pronalazi delove dokumenata koji su mu najsličniji. Ti delovi se prosleđuju jezičkom modelu, koji na osnovu njih sastavlja odgovor. Uz odgovor se prikazuju i korišćeni izvori, kao što su naziv dokumenta, stranica i relevantan deo teksta.

Za obrađene dokumente može se generisati i AI sažetak. Sažetak se čuva uz dokument i po potrebi može ponovo da se generiše.

## Korišćene tehnologije

Backend:

- ASP.NET Core i .NET 8
- Entity Framework Core
- PostgreSQL
- pgvector
- JWT autentifikacija

Frontend:

- React
- TypeScript
- Vite
- React Router
- Axios

AI:

- Ollama
- Qwen
- embeddinggemma
- RAG
- embeddings i semantička pretraga

Rad sa dokumentima:

- PdfPig za PDF dokumente
- Open XML SDK za DOCX dokumente

Ostalo:

- Docker i Docker Compose
- xUnit
- Swagger / OpenAPI

## Struktura projekta

```text
Cogniva/
├── backend/
│   ├── Cogniva.Api/
│   ├── Cogniva.Api.Tests/
│   └── Cogniva.sln
├── frontend/
├── docker-compose.yml
└── README.md
```

U folderu `backend/Cogniva.Api` nalazi se ASP.NET Core API, pristup bazi, modeli, servisi, migracije i povezivanje sa AI servisom. Backend testovi su izdvojeni u `backend/Cogniva.Api.Tests`.

Frontend se nalazi u folderu `frontend`. Napravljen je pomoću Reacta i TypeScripta, a komunikacija sa backendom ide preko Axios klijenta.

## Kako aplikacija radi

### Obrada dokumenta

```text
Otpremanje
→ ekstrakcija teksta
→ podela na chunkove
→ generisanje embeddinga
→ čuvanje u PostgreSQL/pgvector
```

PdfPig čita tekst iz PDF dokumenta po stranicama. Open XML SDK čita pasuse, naslove, liste i tabele iz DOCX dokumenta. Izdvojeni tekst se zatim normalizuje i deli na manje delove, kako bi kasnija pretraga mogla da pronađe samo relevantan sadržaj.

Za svaki chunk se generiše embedding. Vektori se čuvaju u pgvector koloni i koriste za poređenje značenja tekstova, a ne samo njihovih tačnih reči. Dokument kroz obradu prolazi kroz statuse `Uploaded`, `Processing`, `Ready` ili `Failed`, koji se u interfejsu prikazuju na srpskom.

### Postavljanje pitanja

```text
Pitanje korisnika
→ embedding pitanja
→ semantička pretraga
→ relevantni chunkovi
→ jezički model
→ odgovor sa izvorima
```

Ovaj pristup se obično naziva RAG (Retrieval-Augmented Generation). Jezički model ne dobija ceo dokument, već samo delove koje je semantička pretraga pronašla kao relevantne. Na taj način odgovor ostaje vezan za izabrane dokumente, a korisnik može da proveri izvore.

### AI sažetak

Kraći dokumenti, do konfigurisanog limita, sažimaju se jednim pozivom jezičkom modelu. Duži dokumenti se grupišu u veće celine, za svaku se pravi parcijalni sažetak, a zatim se parcijalni rezultati spajaju u konačan sažetak. Ovakav postupak smanjuje broj AI poziva u odnosu na sažimanje svakog chunka posebno.

## Pokretanje projekta

### Preduslovi

Potrebno je instalirati:

- .NET 8 SDK
- Node.js 20 ili noviji
- pnpm
- PostgreSQL ili Docker sa Docker Compose podrškom
- Ollama

### PostgreSQL i pgvector

Iz root foldera projekta pokrenuti:

```bash
docker compose up -d postgres
docker compose ps
```

PostgreSQL je dostupan na portu `5432`. Podrazumevana razvojna baza je `cogniva`, sa korisnikom `cogniva` i lozinkom `cogniva_dev`. Podaci se čuvaju u Docker volume-u i ostaju sačuvani nakon restarta kontejnera.

Kontejner se zaustavlja komandom:

```bash
docker compose down
```

Komanda `docker compose down -v` briše i lokalni volume sa podacima, pa je treba koristiti samo kada je to namerno.

### Backend

Pre prvog pokretanja potrebno je podesiti konekciju ka bazi i JWT secret:

```bash
dotnet user-secrets set "Database:ConnectionString" "Host=localhost;Port=5432;Database=cogniva;Username=cogniva;Password=cogniva_dev" --project backend/Cogniva.Api
dotnet user-secrets set "Jwt:Secret" "lokalni-razvojni-secret-koji-ima-najmanje-32-karaktera" --project backend/Cogniva.Api
```

Zatim se paketi vraćaju, migracije primenjuju i backend pokreće sledećim komandama:

```bash
dotnet restore backend/Cogniva.sln --configfile NuGet.Config
dotnet ef database update --project backend/Cogniva.Api --startup-project backend/Cogniva.Api
dotnet run --project backend/Cogniva.Api
```

Razvojni API je podrazumevano dostupan na `http://localhost:5080`.

- Health provera: `http://localhost:5080/api/health`
- Swagger: `http://localhost:5080/swagger`

Ako terminal prikaže drugi port, treba koristiti adresu iz terminala.

### Frontend

U novom terminalu pokrenuti:

```bash
cd frontend
pnpm install
pnpm dev
```

Frontend je podrazumevano dostupan na `http://localhost:5173`. Production build se proverava komandom:

```bash
pnpm build
```

## Ollama i AI modeli

Lokalna konfiguracija projekta koristi tri modela:

- `embeddinggemma` pravi vektorske reprezentacije dokumenata i pitanja
- `qwen3:4b-instruct` generiše odgovore u razgovorima
- `qwen3:1.7b` generiše sažetke dokumenata

Modeli se preuzimaju ručno:

```bash
ollama pull embeddinggemma
ollama pull qwen3:4b-instruct
ollama pull qwen3:1.7b
```

Za sažetke je izabran manji model zato što radi brže na računaru bez odgovarajuće GPU akceleracije. Ako poseban summary model nije podešen, aplikacija za sažetke koristi isti model kao i za razgovore.

## Konfiguracija

Fajl `.env.example` sadrži primere promenljivih koje su potrebne za lokalni rad. Docker Compose automatski čita vrednosti iz root `.env` fajla, ako postoji. Frontend koristi promenljivu:

```text
VITE_API_BASE_URL=http://localhost:5080
```

Backend konfiguraciju čita iz `appsettings.json`, `appsettings.Development.json`, environment promenljivih i .NET User Secrets. Osetljive vrednosti ne treba upisivati u source kod niti commitovati.

Primer lokalne Ollama konfiguracije:

```bash
dotnet user-secrets set "AI:Provider" "OpenAICompatible" --project backend/Cogniva.Api
dotnet user-secrets set "AI:BaseUrl" "http://localhost:11434/v1/" --project backend/Cogniva.Api
dotnet user-secrets set "AI:EmbeddingModel" "embeddinggemma" --project backend/Cogniva.Api
dotnet user-secrets set "AI:EmbeddingDimensions" "768" --project backend/Cogniva.Api
dotnet user-secrets set "AI:ChatModel" "qwen3:4b-instruct" --project backend/Cogniva.Api
dotnet user-secrets set "AI:SummaryModel" "qwen3:1.7b" --project backend/Cogniva.Api
```

Ollama ne zahteva API ključ. Ako se koristi udaljeni OpenAI-compatible servis, ključ se podešava kroz `AI:ApiKey`, bez izlaganja u frontend promenljivama.

Promena `ChatModel` ili `SummaryModel` ne zahteva ponovnu obradu dokumenata. Promena `EmbeddingModel` ili broja dimenzija zahteva ponovnu obradu, jer se menjaju vektorske reprezentacije sačuvanih chunkova.

Otpremani dokumenti se lokalno čuvaju u `backend/Cogniva.Api/Storage/uploads`. Podrazumevana maksimalna veličina fajla je 20 MB, a prihvataju se samo PDF i DOCX dokumenti.

## Glavne funkcionalnosti

- registracija i prijava korisnika
- JWT autentifikacija
- otpremanje PDF i DOCX dokumenata
- pregled i brisanje dokumenata
- ekstrakcija i obrada teksta
- podela teksta na chunkove i generisanje embeddinga
- semantička pretraga pomoću pgvector-a
- razgovori nad jednim ili više dokumenata
- čuvanje istorije razgovora
- prikaz izvora odgovora
- generisanje AI sažetka dokumenta
- ponovno generisanje neuspelog AI odgovora
- kontrolna tabla sa osnovnim pregledom

## API

Najvažniji endpointi su grupisani na sledeći način.

Auth:

```http
POST /api/auth/register
POST /api/auth/login
GET  /api/auth/me
```

Documents:

```http
GET    /api/documents
GET    /api/documents/{id}
POST   /api/documents/upload
POST   /api/documents/{id}/process
POST   /api/documents/{id}/summarize
DELETE /api/documents/{id}
```

Conversations:

```http
POST /api/conversations
GET  /api/conversations
GET  /api/conversations/{id}
POST /api/conversations/{id}/messages
POST /api/conversations/{id}/messages/{messageId}/retry
```

Dashboard:

```http
GET /api/dashboard
```

Kompletan pregled API-ja i testiranje endpointa dostupni su preko Swagger-a kada je backend pokrenut.

## Testovi

Backend ima xUnit testove u projektu `backend/Cogniva.Api.Tests`. Testovi okvirno pokrivaju:

- ekstrakciju teksta i chunking
- komunikaciju sa embedding i LLM servisima
- RAG tok i pripremu konteksta
- direktno i map-reduce generisanje sažetka
- retry neuspelog AI odgovora

Testovi se pokreću iz root foldera:

```bash
dotnet test backend/Cogniva.sln
```

Može se koristiti i opšta komanda `dotnet test` iz `backend` foldera.

## Ograničenja

Trenutna verzija projekta:

- nema OCR, pa skenirani PDF dokumenti bez tekstualnog sloja nisu podržani
- AI obradu izvršava sinhrono
- može sporije da radi sa lokalnim modelima bez GPU akceleracije
- otpremljene fajlove čuva lokalno
- nema korisničke organizacije i timove
- nema naplatu i korisničke pakete

## Moguća dalja unapređenja

- OCR za skenirane dokumente
- background obrada dokumenata i AI zahteva
- čuvanje dokumenata u cloud storage servisu
- streaming AI odgovora
- korisničke organizacije i timovi
- dodatne korisničke uloge i dozvole
- podrška za cloud LLM providere
- naplata i korisnički paketi
