# SecureBank System Workflow

This document outlines the operational flow of the SecureBank system, from user onboarding to complex atomic transfers.

## 1. User Onboarding Flow
The system follows a "Zero-Friction" onboarding process where a default account is provisioned immediately upon registration.

```mermaid
sequenceDiagram
    participant User
    participant API
    participant DB
    
    User->>API: POST /auth/register (Name, Email, Pwd)
    API->>API: Validate Input & Hash Password
    API->>DB: Begin Transaction
    DB->>DB: Create User Record
    DB->>DB: Provision Default Checking Account
    API->>DB: Commit Transaction
    API->>User: 201 Created + JWT
```

---

## 2. Core Ledger Mechanics
SecureBank uses a **pure ledger-based system**. Every change in balance is driven by a ledger entry (Debit or Credit).

### 2.1 Deposit Operation
```mermaid
graph TD
    A[Deposit Request] --> B{Validate Amount > 0}
    B -- No --> C[Error: Invalid Amount]
    B -- Yes --> D[Begin Transaction]
    D --> E[Create CREDIT Transaction Entry]
    E --> F[Update Account Balance]
    F --> G[Commit Transaction]
    G --> H[Success]
```

### 2.2 Withdrawal Operation
```mermaid
graph TD
    A[Withdrawal Request] --> B{Validate Amount > 0}
    B -- Yes --> C{Sufficient Funds?}
    C -- No --> D[Error: Insufficient Funds]
    C -- Yes --> E[Begin Transaction]
    E --> F[Create DEBIT Transaction Entry]
    F --> G[Update Account Balance]
    G --> H[Commit Transaction]
```

---

## 3. The Atomic Transfer Flow
Transfers are the most critical operations. They must be atomic (all-or-nothing) and concurrency-safe.

### Operational Sequence:
1.  **Authorization**: Verify the authenticated user owns the `fromAccountId`.
2.  **Discovery**: Find the `toAccount` using the unique `AccountNumber`.
3.  **Validation**: Check `amount > 0` and `fromAccount.Balance >= amount`.
4.  **Locking**: The database transaction ensures that the balance read for validation remains consistent during the update phase.
5.  **Execution** (Atomic Block):
    *   **Debit Sender**: Create a `Debit` transaction for Account A.
    *   **Credit Receiver**: Create a `Credit` transaction for Account B.
    *   **Link**: Both transactions share the same `ReferenceId`.
    *   **Snapshot**: Record `BalanceAfter` on both ledger entries.
6.  **Concurrency Resolution**: If another request modified the balance in the same millisecond, the `RowVersion` check will trigger a rollback, preventing a "Double Spend".

```mermaid
sequenceDiagram
    participant API
    participant DB
    
    API->>DB: Begin Transaction (Serializable/Snapshot)
    API->>DB: Get both Accounts (With Concurrency Tokens)
    API->>API: Validate Funds & Ownership
    
    rect rgb(200, 230, 255)
        Note right of API: Atomic Block
        API->>DB: Insert Ledger Entry (DEBIT from SENDER)
        API->>DB: Insert Ledger Entry (CREDIT to RECEIVER)
        API->>DB: Update Sender Balance (Checks RowVersion)
        API->>DB: Update Receiver Balance (Checks RowVersion)
    end
    
    API->>DB: Commit
    Note over API,DB: Success: Money moved across the ledger
```

---

## 4. Integrity Guardrails

| Mechanism | Purpose |
| :--- | :--- |
| **RowVersion** | Prevents race conditions where two simultaneous $100 transfers from a $100 balance could both succeed. |
| **Atomic Transactions** | Ensures that it is impossible for money to leave the sender without arriving at the receiver. |
| **Immutable Ledger** | Transactions are append-only. No one (not even an admin) can "edit" history; they must file a corrective transaction. |
| **ReferenceId** | Enables full auditability by linking the source and destination side of every movement. |
| **Balance Reconciliation** | The system allows an auditor to sum all ledger entries for an account to verify the current balance matches exactly. |
