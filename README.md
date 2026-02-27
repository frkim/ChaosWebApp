# ChaosWebApp

A Web Application and Web API built with **.NET 10** that simulates a product catalogue while allowing deliberate fault injection for **chaos engineering**, **resilience testing**, and **observability validation**.

## Features

### Product Catalogue
- 50+ realistic seed products
- Paginated, sortable, and filterable product table
- Product detail view with star ratings, stock indicators, and pricing
- On-demand random product generation (+5, +10, +100, +1 000)
- RESTful API documented with Swagger/OpenAPI

### Chaos Simulator
- **CPU stress** — configurable duration of intensive computation
- **Memory pressure** — configurable RAM allocation
- **High latency** — delay before request processing
- **Slow response** — delay after processing, before response
- **HTTP errors** — 404, 500, 503, 429 injection
- **Stack overflow** — simulated StackOverflowException
- **Random errors** — random HTTP status codes (400, 401, 403, …)
- Frequency control: percentage-based, every N requests, or every N seconds
- Target scope: Web App only, Web API only, or both

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- (Optional) [Docker](https://www.docker.com/) for containerised deployment
- (Optional) [Azure CLI](https://learn.microsoft.com/cli/azure/) for infrastructure provisioning

## Getting Started

### Run Locally

```bash
cd src/ChaosWebApp
dotnet run
```

The application starts on `http://localhost:5000` by default.

### Run with Docker

```bash
docker compose up --build
```

The application is available on `http://localhost:8080`.

### Build

```bash
dotnet build src/ChaosWebApp/ChaosWebApp.csproj -c Release
```

## Configuration

### Azure App Configuration (recommended for cloud)

Set the `AZURE_APPCONFIG_ENDPOINT` environment variable to point to your Azure App Configuration store. The app uses `DefaultAzureCredential` (managed identity, service principal, or local dev credentials).

```bash
export AZURE_APPCONFIG_ENDPOINT=https://<your-store>.azconfig.io
```

### Environment Variable Fallback

When Azure App Configuration is **not** configured, the app reads configuration from environment variables prefixed with `CHAOSAPP_`. Use double underscores (`__`) to represent section nesting.

```bash
# Example: override ApplicationInsights:ConnectionString
export CHAOSAPP_ApplicationInsights__ConnectionString="InstrumentationKey=..."
```

### Application Insights

Set the connection string via either:
- `APPLICATIONINSIGHTS_CONNECTION_STRING` environment variable, or
- `ApplicationInsights:ConnectionString` in `appsettings.json`

## API Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| `GET` | `/api/products` | Paginated product list (query: page, pageSize, sortBy, ascending, filter) |
| `GET` | `/api/products/{id}` | Single product by ID |
| `POST` | `/api/products/generate/{count}` | Generate random products (5, 10, 100, or 1 000) |
| `GET` | `/api/products/stats` | Catalogue statistics |
| `GET` | `/api/chaos` | Current chaos configuration |
| `PUT` | `/api/chaos` | Update chaos configuration |
| `GET` | `/api/chaos/active` | Active chaos types |
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness probe |
| `GET` | `/swagger` | Swagger UI |

## Infrastructure as Code

The `infra/` directory contains **Azure Bicep** templates to provision the full environment:

| Module | Resource |
|--------|----------|
| `logAnalytics.bicep` | Log Analytics workspace |
| `acr.bicep` | Azure Container Registry |
| `appInsights.bicep` | Application Insights |
| `appConfiguration.bicep` | Azure App Configuration |
| `containerApp.bicep` | Azure Container Apps (environment + app) |
| `loadTesting.bicep` | Azure Load Testing |

### Deploy

```bash
az group create -n rg-chaoswebapp -l eastus2
az deployment group create \
  -g rg-chaoswebapp \
  -f infra/main.bicep \
  -p appName=chaoswebapp \
     containerImage=<your-acr>.azurecr.io/chaoswebapp:latest
```

## Tech Stack

- **ASP.NET Core 10** — Razor Pages (UI) + Web API (Controllers)
- **Bootstrap 5** — responsive layout
- **Material Design** — Roboto typography, Material elevation, Google Material Symbols
- **Application Insights** — monitoring and telemetry
- **Azure App Configuration** — centralised configuration management
- **Azure Container Apps** — serverless container hosting
- **Azure Load Testing** — load testing as code
- **Docker** — containerisation
- **Bicep** — infrastructure as code

## Project Structure

```
ChaosWebApp/
├── infra/                     # Bicep IaC templates
│   ├── main.bicep
│   └── modules/
├── src/ChaosWebApp/
│   ├── Controllers/           # API controllers
│   ├── Middleware/             # Chaos injection middleware
│   ├── Models/                # Domain models
│   ├── Pages/                 # Razor Pages (UI)
│   ├── Services/              # Business logic
│   ├── wwwroot/               # Static assets
│   └── Program.cs             # Application entry point
├── Dockerfile
├── docker-compose.yml
└── README.md
```

## License

See [LICENSE](LICENSE) for details.
