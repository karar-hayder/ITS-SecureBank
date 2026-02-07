# Database Schema Documentation

This document describes the database schema for the ITS-Hackthon project, illustrating entity relationships and detailed table structures.

## Entity Relationship Diagram

```mermaid
erDiagram
    Users ||--o{ Accounts : owns
    Users ||--o{ RefreshTokens : has
    Users ||--o{ AccountApprovalRequests : requests
    
    Accounts ||--o{ Transactions : receives
    Accounts ||--o{ AccountApprovalRequests : associated_with
    
    Accounts ||--o{ TransferIntents : from
    Accounts ||--o{ TransferIntents : to

    Users {
        int Id PK
        string FullName
        string Email
        string PhoneNumber
        string PasswordHash
        int userRole
    }

    Accounts {
        int Id PK
        string AccountNumber
        int AccountType
        decimal Balance
        int UserId FK
        int Status
        int Level
    }

    Transactions {
        int Id PK
        int Type
        decimal Amount
        int AccountId FK
        decimal BalanceAfter
        int RelatedAccountId FK
        string ReferenceId
    }

    TransferIntents {
        string TransferIntentId PK
        int FromAccountId FK
        int ToAccountId FK
        decimal Amount
        int Status
    }

    AccountApprovalRequests {
        int Id PK
        int AccountId FK
        int UserId FK
        string IdDocumentUrl
        bool IsApproved
    }

    RefreshTokens {
        int Id PK
        string Refreshtoken
        datetime ExpiresAt
        bool IsRevoked
        int UserId FK
    }

    AuditLogs {
        int Id PK
        string EntityName
        int EntityId
        string Action
        string Changes
    }

    IdempotencyRecords {
        int Id PK
        string Key
        int UserId
        string Path
    }
```

---

## Tables

### Users
Stores user account information for authentication and profile management.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **FullName** | NVARCHAR(100) | NOT NULL | User's full name |
| **Email** | NVARCHAR(100) | NOT NULL | User's email address |
| **PhoneNumber** | NVARCHAR(20) | NOT NULL | User's phone number |
| **PasswordHash** | NVARCHAR(100) | NOT NULL | Hashed password |
| **userRole** | INT | NOT NULL | User role (See [Enums](#enums)) |
| **CreatedAt** | DATETIME2 | NOT NULL | Creation timestamp |
| **UpdatedAt** | DATETIME2 | NULL | Last update timestamp |
| **CreatedBy** | INT | NULL | ID of creator |
| **UpdatedBy** | INT | NULL | ID of last updater |
| **IsDeleted** | BIT | NOT NULL | Soft delete flag |
| **DeletedAt** | DATETIME2 | NULL | Soft delete timestamp |

---

### Accounts
Stores banking account information associated with users.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **AccountNumber** | NVARCHAR(34) | NOT NULL, UNIQUE | IBAN-style account number |
| **AccountType** | INT | NOT NULL | Type of account (See [Enums](#enums)) |
| **Balance** | DECIMAL(18,2) | NOT NULL, DEFAULT 0 | Current balance |
| **UserId** | INT | FK (Users.Id) | Owner of the account |
| **Status** | INT | NOT NULL, DEFAULT 0 | Account status (See [Enums](#enums)) |
| **Level** | INT | NOT NULL, DEFAULT 1 | Account level (See [Enums](#enums)) |
| **RowVersion** | VARBINARY(MAX) | Concurrency Token | Used for optimistic concurrency |
| **CreatedAt** | DATETIME2 | NOT NULL | Creation timestamp |
| **UpdatedAt** | DATETIME2 | NULL | Last update timestamp |
| **CreatedBy** | INT | NULL | ID of creator |
| **UpdatedBy** | INT | NULL | ID of last updater |
| **IsDeleted** | BIT | NOT NULL | Soft delete flag |
| **DeletedAt** | DATETIME2 | NULL | Soft delete timestamp |

---

### Transactions
Stores all financial movements (deposits, withdrawals, transfers).

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **Type** | INT | NOT NULL | Transaction type (See [Enums](#enums)) |
| **Amount** | DECIMAL(18,2) | NOT NULL | Movement amount |
| **AccountId** | INT | FK (Accounts.Id) | Main account for transaction |
| **BalanceAfter** | DECIMAL(18,2) | NOT NULL | Balance after transaction |
| **RelatedAccountId**| INT | FK (Accounts.Id), NULL | Source/Dest account for transfers |
| **ReferenceId** | NVARCHAR(50) | NOT NULL, INDEX | External/System reference |
| **ReferenceNumber** | NVARCHAR(50) | UNIQUE, INDEX | Unique reference for the transaction |
| **Description** | NVARCHAR(250) | NULL | Optional description |
| **CreatedAt** | DATETIME2 | NOT NULL | Creation timestamp |
| **UpdatedAt** | DATETIME2 | NULL | Last update timestamp |
| **CreatedBy** | INT | NULL | ID of creator |
| **UpdatedBy** | INT | NULL | ID of last updater |
| **IsDeleted** | BIT | NOT NULL | Soft delete flag |
| **DeletedAt** | DATETIME2 | NULL | Soft delete timestamp |

---

### TransferIntents
Tracks the progress of a multi-step transfer operation.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **TransferIntentId** | NVARCHAR(128) | PK | Primary Key (GUID string) |
| **FromAccountId** | INT | FK (Accounts.Id) | Source account |
| **ToAccountId** | INT | FK (Accounts.Id) | Destination account |
| **Amount** | DECIMAL(18,2) | NULL | Amount to transfer |
| **Status** | INT | NOT NULL | Current status (See [Enums](#enums)) |
| **CreatedAt** | DATETIME2 | NOT NULL | Creation timestamp |
| **CompletedAt** | DATETIME2 | NULL | Completion timestamp |
| **UpdatedAt** | DATETIME2 | NULL | Last update timestamp |
| **CreatedBy** | INT | NULL | ID of creator |
| **UpdatedBy** | INT | NULL | ID of last updater |
| **IsDeleted** | BIT | NOT NULL | Soft delete flag |
| **DeletedAt** | DATETIME2 | NULL | Soft delete timestamp |

---

### AccountApprovalRequests
Stores KYC/Account approval requests by users.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **AccountId** | INT | FK (Accounts.Id) | Account to approve |
| **UserId** | INT | FK (Users.Id) | User requesting |
| **IdDocumentUrl** | NVARCHAR(MAX)| NOT NULL | Link to ID document |
| **IsApproved** | BIT | NOT NULL | Approval status |
| **ProcessedAt** | DATETIME2 | NULL | Processing timestamp |
| **ProcessedByAdminId**| INT | NULL | Admin who processed request |
| **AdminRemarks** | NVARCHAR(MAX)| NULL | Admin notes |
| **CreatedAt** | DATETIME2 | NOT NULL | Creation timestamp |
| **UpdatedAt** | DATETIME2 | NULL | Last update timestamp |
| **CreatedBy** | INT | NULL | ID of creator |
| **UpdatedBy** | INT | NULL | ID of last updater |
| **IsDeleted** | BIT | NOT NULL | Soft delete flag |
| **DeletedAt** | DATETIME2 | NULL | Soft delete timestamp |

---

### AuditLogs
Stores change history for major entities.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **EntityName** | NVARCHAR(100) | NOT NULL, INDEX | Target entity table |
| **EntityId** | INT | NOT NULL, INDEX | Target entity record ID |
| **Action** | NVARCHAR(50) | NOT NULL | Action (Created, Updated, etc) |
| **ChangedBy** | INT | NULL | User who made the change |
| **ChangedAt** | DATETIME2 | NOT NULL | Change timestamp |
| **Changes** | TEXT | NULL | JSON of changes |

---

### RefreshTokens
Stores JWT refresh tokens for persistent sessions.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **Refreshtoken** | NVARCHAR(500) | NOT NULL | Token value |
| **ExpiresAt** | DATETIME2 | NOT NULL | Expiry timestamp |
| **IsRevoked** | BIT | NOT NULL | Revoke flag |
| **UserId** | INT | FK (Users.Id) | Associated user |

---

### IdempotencyRecords
Used to prevent duplicate processing of API requests.

| Column | Type | Constraints | Description |
| :--- | :--- | :--- | :--- |
| **Id** | INT | PK, AI | Primary Key |
| **Key** | NVARCHAR(100) | NOT NULL | Idempotency Key |
| **UserId** | INT | NOT NULL | User who sent the request |
| **Path** | NVARCHAR(MAX)| NOT NULL | API endpoint |
| **Method** | NVARCHAR(MAX)| NOT NULL | HTTP Method |
| **ResponseStatusCode**| INT | NOT NULL | Cached status code |
| **ResponseBody** | NVARCHAR(MAX)| NULL | Cached response body |

---

## Enums

### UserRole
- `Admin = 1`
- `User = 2`

### AccountType
- `Checking = 1`
- `Savings = 2`

### AccountStatus
- `Pending = 0`
- `Active = 1`
- `Inactive = 2`
- `Suspended = 3`
- `Closed = 4`

### AccountLevel
- `Level1 = 1`
- `Level2 = 2`
- `Level3 = 3`

### TransactionType
- `Debit = 1`
- `Credit = 2`
- `Transfer = 3`

### TransferIntentStatus
- `Pending = 0`
- `Completed = 1`
- `Cancelled = 2`
