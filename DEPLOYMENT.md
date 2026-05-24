# Deployment

Deze fase zet alleen de staging/testomgeving klaar voor de bestaande MVP:

- `Booking.Api`
- `Booking.BlazorApp`
- SQL Server
- Docker Compose
- later een Nginx reverse proxy

Er worden geen nieuwe applicatiefeatures toegevoegd.

## Bestanden

- `Dockerfile.Api`: build en runtime image voor `src/Booking.Api`.
- `Dockerfile.BlazorApp`: build en runtime image voor `src/Booking.BlazorApp`.
- `docker-compose.yml`: services voor API, Blazor en SQL Server.
- `.env.example`: voorbeeldconfiguratie zonder echte secrets.
- `.dockerignore`: voorkomt dat lokale build-output en secrets in Docker build context komen.

## Lokale Docker Compose Test

Maak eerst een lokale `.env`:

```bash
cp .env.example .env
```

PowerShell:

```powershell
Copy-Item .env.example .env
```

Pas minimaal `SA_PASSWORD` aan. SQL Server vereist een sterk wachtwoord.

Start alles:

```bash
docker compose up --build
```

Open daarna:

- Blazor app: `http://localhost:5001`
- API: `http://localhost:5000`
- Swagger in Development: `http://localhost:5000/swagger`

Bekijk logs:

```bash
docker compose logs
docker compose logs booking-api
docker compose logs booking-blazor
docker compose logs booking-sql
```

Stop containers:

```bash
docker compose down
```

Stop containers en verwijder ook de database volume alleen als testdata weg mag:

```bash
docker compose down -v
```

## Environment Variables

De compose setup leest waarden uit `.env`.

Verplichte waarden:

```bash
SA_PASSWORD=ChangeThis_StrongPassword_123!
DB_NAME=BookingDb
API_URL=http://booking-api:8080
ASPNETCORE_ENVIRONMENT=Staging
```

Optionele lokale hostpoorten:

```bash
SQL_PORT=1433
API_PORT=5000
BLAZOR_PORT=5001
```

Gebruik op een VPS echte secrets in `.env`. Commit `.env` niet.

## Database Migrations

De database wordt niet automatisch gemigreerd bij container-start. Voer migraties bewust uit, zodat staging voorspelbaar blijft.

Start eerst alleen SQL Server:

```bash
docker compose up -d booking-sql
```

Wacht tot SQL gezond is:

```bash
docker compose ps
docker compose logs booking-sql
```

Voer daarna de EF Core migraties uit vanaf de host met de .NET SDK:

```bash
export ConnectionStrings__BookingDatabase="Server=localhost,${SQL_PORT:-1433};Database=${DB_NAME};User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;Encrypt=False;"
dotnet ef database update --project src/Booking.Infrastructure/Booking.Infrastructure.csproj --startup-project src/Booking.Api/Booking.Api.csproj
```

PowerShell:

```powershell
$sqlPort = if ([string]::IsNullOrWhiteSpace($env:SQL_PORT)) { "1433" } else { $env:SQL_PORT }
$env:ConnectionStrings__BookingDatabase = "Server=localhost,$sqlPort;Database=$env:DB_NAME;User Id=sa;Password=$env:SA_PASSWORD;TrustServerCertificate=True;Encrypt=False;"
dotnet ef database update --project src/Booking.Infrastructure/Booking.Infrastructure.csproj --startup-project src/Booking.Api/Booking.Api.csproj
```

Als de host geen .NET SDK heeft, gebruik tijdelijk een SDK-container op hetzelfde Docker-netwerk:

```bash
docker run --rm \
  --network booking_default \
  -v "$PWD:/work" \
  -w /work \
  -e ConnectionStrings__BookingDatabase="Server=booking-sql,1433;Database=${DB_NAME};User Id=sa;Password=${SA_PASSWORD};TrustServerCertificate=True;Encrypt=False;" \
  mcr.microsoft.com/dotnet/sdk:9.0 \
  sh -c "dotnet tool install --global dotnet-ef --version 9.0.16 && export PATH=\"\$PATH:/root/.dotnet/tools\" && dotnet ef database update --project src/Booking.Infrastructure/Booking.Infrastructure.csproj --startup-project src/Booking.Api/Booking.Api.csproj"
```

Start daarna de volledige stack:

```bash
docker compose up -d --build
```

## VPS Deploy Stappen

1. Maak een map op de VPS, bijvoorbeeld `/opt/booking`.
2. Plaats de repository of releasebestanden in die map.
3. Maak `.env` op basis van `.env.example`.
4. Zet een sterk `SA_PASSWORD`.
5. Zet `ASPNETCORE_ENVIRONMENT=Staging`.
6. Laat `API_URL=http://booking-api:8080` staan voor container-intern verkeer.
7. Start SQL:
   ```bash
   docker compose up -d booking-sql
   ```
8. Voer database migrations uit.
9. Start de API en Blazor app:
   ```bash
   docker compose up -d --build booking-api booking-blazor
   ```
10. Controleer de containers:
    ```bash
    docker compose ps
    docker compose logs --tail=100
    ```
11. Doorloop `MANUAL_TEST_PLAN.md` tegen de staging URL.

## Containers Herstarten

Herstart alles:

```bash
docker compose restart
```

Herstart alleen de API:

```bash
docker compose restart booking-api
```

Herstart alleen Blazor:

```bash
docker compose restart booking-blazor
```

## Nieuwe Versie Deployen

1. Haal de nieuwe code op of kopieer de nieuwe releasebestanden.
2. Build images opnieuw:
   ```bash
   docker compose build
   ```
3. Start SQL als die nog niet draait:
   ```bash
   docker compose up -d booking-sql
   ```
4. Voer migrations uit als er nieuwe migrations zijn.
5. Start de app-containers:
   ```bash
   docker compose up -d booking-api booking-blazor
   ```
6. Controleer logs en voer de belangrijkste checks uit `MANUAL_TEST_PLAN.md` uit.

## Rollback Basis

Voor een eenvoudige staging rollback:

1. Ga terug naar de vorige git tag, commit of releasemap.
2. Build de vorige images opnieuw:
   ```bash
   docker compose build booking-api booking-blazor
   ```
3. Start de vorige versie:
   ```bash
   docker compose up -d booking-api booking-blazor
   ```
4. Controleer logs:
   ```bash
   docker compose logs --tail=100 booking-api booking-blazor
   ```

Let op: database migrations worden niet automatisch teruggedraaid. Maak voor staging-deploys eerst een database backup of snapshot als er schemawijzigingen zijn.

## Later: Nginx Reverse Proxy

Voor fase 8 blijft Nginx buiten scope. De compose setup houdt hier wel rekening mee:

- API container luistert intern op `booking-api:8080`.
- Blazor container luistert intern op `booking-blazor:8080`.
- Hostpoorten zijn nu alleen voor staging/test.

Een latere Nginx-configuratie kan verkeer doorsturen naar deze interne services.
