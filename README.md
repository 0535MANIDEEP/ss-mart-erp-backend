<div align="center">

# SS MART ERP Backend

### REST API Server for SS MART Retail ERP System

**A .NET 8 Web API with JWT authentication, Entity Framework Core, and PostgreSQL — powering the SS MART offline-first retail ERP.**

[![.NET](https://img.shields.io/badge/.NET-8-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-15-4169E1?logo=postgresql&logoColor=white)](https://postgresql.org)
[![License](https://img.shields.io/badge/License-MIT-blue)](LICENSE)

</div>

---

## Overview

This is the backend API server for the SS MART retail ERP system. It provides a RESTful API for the Flutter mobile/desktop client, handling authentication, data storage, business logic, and synchronization.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 |
| ORM | Entity Framework Core 8 |
| Database | PostgreSQL 15+ |
| Auth | JWT Bearer + Role-Based |
| API Docs | Swagger/OpenAPI |
| Validation | FluentValidation |

## Features

- **JWT Authentication** — token-based auth with role-based access control
- **24 API Controllers** — covering all retail operations
- **EF Core + PostgreSQL** — type-safe database access
- **Swagger UI** — interactive API documentation at `/swagger`
- **Global Authorization** — all endpoints require auth by default
- **CORS Support** — configured for Flutter client

## API Controllers

| Controller | Purpose |
|-----------|---------|
| AuthController | Login, register, token refresh |
| ProductsController | Product CRUD, search, categories |
| InventoryController | Stock management, low-stock alerts |
| BillsController | Invoice generation, GST calculations |
| CustomersController | Customer profiles, CRM |
| SuppliersController | Supplier management |
| PurchasesController | Purchase orders, receiving |
| EmployeesController | Staff management |
| ExpensesController | Expense tracking |
| AccountingController | Financial reports, profit/loss |
| LoyaltyController | Points earn/redeem |
| ReportsController | Sales reports, analytics |
| SettingsController | Store configuration |
| SyncController | Offline data synchronization |
| ... | And more |

## Getting Started

### Prerequisites

- .NET 8 SDK
- PostgreSQL 15+
- IDE (Visual Studio, Rider, or VS Code)

### Setup

```bash
# Clone the repository
git clone https://github.com/0535MANIDEEP/ss-mart-erp-backend.git
cd ss-mart-erp-backend/SS_MART_API

# Restore dependencies
dotnet restore

# Update connection string in appsettings.json
# "DefaultConnection": "Host=localhost;Database=ssmart;Username=postgres;Password=yourpassword"

# Run migrations
dotnet ef database update

# Start the server
dotnet run
```

API runs at `https://localhost:5001` with Swagger at `/swagger`.

### Environment Variables

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=ssmart;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "SecretKey": "your-secret-key-min-32-chars",
    "Issuer": "SS_MART_API",
    "Audience": "SS_MART_CLIENT",
    "ExpirationInMinutes": 60
  }
}
```

## Project Structure

```
SS_MART_API/
├── Controllers/          # 24 API controllers
├── Core/
│   ├── Application/      # Services, interfaces
│   ├── Domain/           # Entities, interfaces
│   └── Infrastructure/   # Data access, repositories
├── Program.cs            # Application entry point
├── appsettings.json      # Configuration
└── SS_MART_API.csproj    # Project file
```

## Related Repos

| Repo | Description |
|------|-------------|
| [ss-mart-erp](https://github.com/0535MANIDEEP/ss-mart-erp) | Architecture docs & system design |
| [ss-mart-erp-mobile](https://github.com/0535MANIDEEP/ss-mart-erp-mobile) | Flutter mobile/desktop client |

## Author

**Manideep Daram** — [GitHub](https://github.com/0535MANIDEEP) · [Email](mailto:darammanideep@gmail.com)
