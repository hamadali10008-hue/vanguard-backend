# Vanguard SaaS Platform - Backend API

Welcome to the backend engine of the Vanguard SaaS Platform. This system is built using **.NET 10** and adheres strictly to the principles of **Clean Architecture** (DDD - Domain-Driven Design) to ensure decoupling, testability, and high performance.

---

## Clean Architecture Overview

The solution is divided into four distinct layers, separating core business logic from external frameworks:

* **`Saas.Domain`**: Contains core enterprise logic, entities, value objects, and domain exceptions. It has zero external dependencies.
* **`Saas.Appplication`**: Handles application use cases, DTOs, mapping, interfaces, and CQRS/mediator patterns.
* **`Saas.Infrastructure`**: Implements data persistence (Entity Framework Core), external API integrations, caching, and identity management.
* **`Saas.Api`**: The presentation layer. Exposes RESTful endpoints, handles authentication/authorization middleware, and manages OpenAPI documentation.

---

## Getting Started

### Prerequisites
* [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) (SDK `10.0.300` or higher)
* Visual Studio 2022 (v17.13+) with the .NET 10 workload enabled.
* A running instance of your configured database.

### Initial Installation & Setup

1. **Clone the repository:**
   ```bash
   git clone [https://github.com/hamadali10008-hue/vanguard-backend.git](https://github.com/hamadali10008-hue/vanguard-backend.git)
   cd vanguard-backend
