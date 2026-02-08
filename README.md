# 🏦 SECUREBANK — Bulletproof Core Banking API

**SECUREBANK** is a high-integrity, ledger-based core banking API designed for a 2-day hackathon. The system guarantees that money is never lost, duplicated, or partially applied—even under server crashes, malicious input, and extreme concurrency.

---

## 🚀 Key Features

- **Pure Ledger Architecture**: Every balance change is backed by immutable `Debit` or `Credit` entries.
- **Atomic Transfers**: All-or-nothing transfers ensuring sender debit and receiver credit are perfectly synchronized.
- **Idempotency Guards**: Prevents duplicate processing via both header-based (`Idempotency-Key`) and intent-based workflows.
- **Zero-Trust Security**: Robust authentication using JWT and Refresh Tokens, with role-based access control (Admin/User).
- **Concurrency Safety**: Optimistic locking (`RowVersion`) prevents race conditions and double-spending.
- **Automated Interest**: Background jobs (Hangfire) to apply periodic interest to savings accounts.
- **Full Auditability**: Transparent transaction history allowing for full balance reconstruction.

---

## 🏗️ Technical Architecture

The project follows **Clean Architecture** principles to ensure separation of concerns and maintainability:

- **API**: Entry point, middleware (Rate Limiting, Exception Handling), and controllers.
- **Application**: Core business logic, DTOs, interfaces, and service implementations.
- **Domain**: Pure entities, enums, and business invariants.
- **Infrastructure**: Persistence (EF Core), background jobs (Hangfire), and external services (CSV, Identity).

### Technology Stack

- **Framework**: .NET 8.0
- **Database**: PostgreSQL (via EF Core)
- **Background Jobs**: Hangfire
- **Authentication**: JWT Bearer + Identity
- **Documentation**: Swagger/OpenAPI

---

## 🛠️ Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/)

### Setup

1. **Clone the repository**:

   ```bash
   git clone https://github.com/karar-hayder/ITS-SecureBank.git
   cd ITS-SecureBank
   ```

2. **Configure Database**:
   Update the `DefaultConnection` in `API/appsettings.Development.json`:

   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=bank_db;Username=postgres;Password=your_password"
   }
   ```

3. **Run Migrations**:

   ```bash
   dotnet ef database update --project Infrastructure --startup-project API
   ```

4. **Run the Application**:

   ```bash
   dotnet run --project API
   ```

   The API will be available at `http://localhost:5202` (or as configured). Swagger UI can be accessed at `/swagger`.

---

## 📜 Documentation

For deeper architectural details, refer to:

- 📑 [Workflow Documentation](WORKFLOW.md): Operational flows and sequence diagrams.
- 🗺️ [Request Flow](REQUEST_FLOW.md): Detailed pipeline and layer interaction.
- 🗄️ [Database Schema](SCHEMA.md): Entity Relationships and table definitions.

---

## 👥 Core Team

This project, This Backend was developed for the hackathon by:

- **Karar Haider** - [@karar-hayder](https://github.com/karar-hayder)
- **Ali Mohammed** - [@NOT-Ali0](https://github.com/NOT-Ali0)

---

## 🛡️ Core Banking Invariants

1. **Money Conservation**: Transfers have a net-zero effect.
2. **Atomicity**: Operations are all-or-nothing.
3. **Consistency**: Balances never go negative.
4. **Isolation**: Double spending is impossible.
5. **Durability**: Committed transactions survive any crash.

---
*Built for the ITS (Iraq Tech School) SecureBank Hackathon — Focused on Correctness, Resilience, and Clarity.*
