# StockSense — Migration Progress

## Project Overview

StockSense is a .NET Blazor web application for a motor parts/repair shop. It handles inventory management, POS, order slips, appointments, mechanic scheduling, pre-built packages, customer motor builds, and ML-based stock prediction.

The application uses a Blazor Server + Blazor WASM (hosted) architecture with ASP.NET Core API controllers serving the client.

---

## Previous Structure (Before Migration)

The original solution was a monolithic layout with two projects:

```
StockSense/
├── StockSense/               # Server — Blazor + API + Data + Services all in one
│   ├── Components/           # Blazor pages (Employee, Account)
│   ├── Controllers/          # API controllers
│   ├── Data/                 # ApplicationDbContext + ApplicationUser
│   ├── Migrations/           # EF Core migrations
│   ├── Services/             # Business logic mixed with infrastructure concerns
│   ├── Helper/
│   └── wwwroot/
└── StockSense.Client/        # Blazor WASM client
    ├── Layout/
    └── Pages/                # Admin, Employee, Customer pages

StockSense.shared/            # Shared models (flat, no separation of concerns)
```

**Problems with the old structure:**
- No separation between domain entities, business logic, and data access
- Controllers directly consumed `ApplicationDbContext` or thin services
- All models lived in `StockSense.shared/` as flat files with no layering
- Services mixed infrastructure concerns (email, EF queries) with business rules
- No interfaces — nothing was abstracted or testable in isolation

---

## Migration Steps Taken

### Step 1 — Restructured Solution into Four Distinct Projects

The monolithic `StockSense/` project was broken apart into separate layers following Clean Architecture:

| Project | Role |
|---|---|
| `StockSense.Domain` | Core entities — no dependencies |
| `StockSense.Application` | Business logic, interfaces, DTOs |
| `StockSense.Infrastructure` | EF Core, repositories, external services |
| `StockSense.Web` | Presentation host (renamed from `StockSense`) |
| `StockSense.Client` | Blazor WASM client — unchanged |

---

### Step 2 — Extracted Domain Entities into `StockSense.Domain`

All models from `StockSense.shared/` were moved into `StockSense.Domain/Entities/` as proper domain entities. Compound model files were split into individual entity files:

- `OrderSlipModels.cs` → `OrderSlip.cs` + `OrderSlipItem.cs`
- `Transactions.cs` → `Transaction.cs` + `TransactionItem.cs`

New entities added during this step:
- `Supplier.cs`
- `BuildRequest.cs`

`StockSense.shared/` was emptied and now only holds its `.csproj` as a stub.

---

### Step 3 — Created the Application Layer

`StockSense.Application/` was introduced to enforce business logic boundaries:

**Interfaces** (`Application/Interfaces/`) — enforces Dependency Inversion:
- `IProductRepository`, `IProductService`
- `IOrderSlipRepository`, `IOrderSlipService`
- `IAppointmentRepository`
- `IPreBuildRepository`, `IPreBuildService`
- `ITransactionRepository`, `ITransactionService`
- `IDocumentService`, `IEmailSender`, `IOrderEmailSender`

**Services** (`Application/Services/`) — concrete business logic, no EF or HTTP:
- `ProductService`, `AppointmentService`, `PreBuildService`, `TransactionService`

**DTOs** (`Application/DTOs/`) — consolidated and organized:
- `DTO.cs`, `ProductDto.cs`, `AppointmentDtos.cs`, `OrderSlipDto.cs`, `PreBuildDtos.cs`, `SupplierDto.cs`

**Mappings** (`Application/Mappings/`):
- `MappingExtensions.cs` — centralized mapping logic, replacing ad-hoc mapping in controllers

---

### Step 4 — Moved Infrastructure Concerns into `StockSense.Infrastructure`

All data access and external service code was relocated:

**Repositories** (`Infrastructure/Data/Repositories/`):
- `ProductRepository`, `AppointmentRepository`, `OrderSlipRepository`, `PreBuildRepository`, `TransactionRepository`

**Data** (`Infrastructure/Data/`):
- `ApplicationDbContext.cs`

**Migrations** (`Infrastructure/Migrations/`):
- All existing EF Core migrations moved here (unchanged)

**Services** (`Infrastructure/Services/`):
- `EmailSender.cs`, `OrderSlipService.cs` — moved from old `StockSense/Services/`
- `DocumentService.cs`, `OrderEmailSender.cs` — newly added

**ML Model** (`Infrastructure/StockSenseML/`):
- `ModelInput.cs`, `ModelOutput.cs` — moved from the old shared project root

---

### Step 5 — Cleaned Up `StockSense.Web`

The host project was renamed `StockSense.Web` and stripped of all non-presentation concerns:
- `Data/` folder removed (now lives in Infrastructure)
- `Services/` folder removed (split between Application and Infrastructure)
- Controllers retained, now delegating to Application layer services via injected interfaces
- `Helper/PhDateTimeConverter.cs` retained as a web-specific utility

---

### Step 6 — Added Agent Skill References

A `.agents/skills/` folder was added to guide AI-assisted development:
- `clean-architecture/` skill — includes reference docs on SOLID principles, dependency rule, component principles, boundaries, entities/use cases, and adapters/frameworks
- `solid-principles/` skill — dedicated SOLID reference, replacing an earlier `frontend-design` skill

---

## Current State

The solution is fully restructured into Clean Architecture layers. The `StockSense.Client` (Blazor WASM) is untouched and continues to consume the API controllers in `StockSense.Web`.

**Dependency flow:**
```
StockSense.Web → StockSense.Application → StockSense.Domain
StockSense.Infrastructure → StockSense.Application → StockSense.Domain
StockSense.Client → (API via HTTP)
```

---

## What's Next (Suggested)

- [ ] Register all repositories and services in `StockSense.Web/Program.cs` via DI
- [ ] Audit controllers to ensure none reference `ApplicationDbContext` directly
- [ ] Add a `ServiceRepository` to cover the `StoreService` entity
- [ ] Consider adding a `StockSensePredictionService` equivalent in Application or Infrastructure
- [ ] Add unit tests targeting Application layer services (now that they're properly abstracted)
