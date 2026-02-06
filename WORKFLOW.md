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
    API->>DB: Commit Transaction
    API->>User: 201 Created + JWT
```

---

### 1.5 Enhanced KYC Flow (Identity Verification)

To meet compliance standards, the system supports a "Pending" state for new accounts pending Admin Approval.

```mermaid
sequenceDiagram
    participant User
    participant API
    participant Admin
    
    User->>API: POST /Account
    API->>User: 201 Created (Default Account Status: Pending)
    User->>API: POST /accountapproval/request (Upload ID)
    API->>DB: Store Request & File Path
    
    Admin->>API: GET /accountapproval/admin/pending
    Admin->>API: POST /accountapproval/admin/decide (Approve)
    API->>DB: Update Account Status -> Active
    API->>User: Notify (Simulated Email)
```

---

## 2. Core Ledger Mechanics

SecureBank uses a **pure ledger-based system**. Every change in balance is driven by a ledger entry (Debit or Credit).

### 2.1 Deposit Operation (Idempotent)

```mermaid
graph TD
    A[Deposit Request] --> B{Idempotency Key Exists?}
    B -- Yes --> C[Return Saved Response]
    B -- No --> D{Validate Amount > 0}
    D -- No --> E[Error: Invalid Amount]
    D -- Yes --> F[Begin Transaction]
    F --> G[Create CREDIT Transaction Entry]
    G --> H[Update Account Balance]
    H --> I[Commit Transaction]
    I --> J[Save Idempotency Response]
    J --> K[Success]
```

### 2.2 Withdrawal Operation (Idempotent)

Similar to Deposit, with sufficiency checks + Idempotency Guard.

---

## 3. The Atomic Transfer Flow

Transfers are the most critical operations. They must be atomic (all-or-nothing) and concurrency-safe.

### Operational Sequence

1. **Idempotency Check**: Return cached result if key exists.
2. **Authorization**: Verify the authenticated user owns the account associated with `fromAccountNumber`.
3. **Discovery**: Find the `toAccount` using the unique `AccountNumber`.
4. **Validation**: Check `amount > 0` and `fromAccount.Balance >= amount`.
5. **Locking**: The database transaction ensures that the balance read for validation remains consistent during the update phase.
6. **Execution** (Atomic Block):
    * **Debit Sender**: Create a `Debit` transaction for Account A.
    * **Credit Receiver**: Create a `Credit` transaction for Account B.
    * **Link**: Both transactions share the same `ReferenceId`.
    * **Snapshot**: Record `BalanceAfter` on both ledger entries.
7. **Concurrency Resolution**: If another request modified the balance in the same millisecond, the `RowVersion` check will trigger a rollback, preventing a "Double Spend".

```mermaid
sequenceDiagram
    participant API
    participant DB
    
    API->>DB: Check Idempotency Key
    alt Key Exists
        DB-->>API: Return Cached Response
    else New Request
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
        
        API->>DB: Save Idempotency Record
        API->>DB: Commit
        Note over API,DB: Success: Money moved across the ledger
    end
```

---

## 4. Integrity Guardrails

| Mechanism | Purpose |
| :--- | :--- |
| **RowVersion** | Prevents race conditions where two simultaneous $100 transfers from a $100 balance could both succeed. |
| **Atomic Transactions** | Ensures that it is impossible for money to leave the sender without arriving at the receiver. |
| **Immutable Ledger** | Transactions are append-only. No one (not even an admin) can "edit" history; they must file a corrective transaction. |
| **ReferenceId** | Enables full auditability by linking the source and destination side of every movement. |
| **Idempotency Keys** | Prevents duplicate processing of the same request due to network retries. |

---

## 5. Background Workflows (Interest)

To simulate a real banking environment, a background job runs periodically.

* **Trigger**: `AccountInterestJob` (HostedService) runs every 2 minutes.
* **Action**:
    1. Scan all Active Savings Accounts.
    2. Calculate 0.01% Interest.
    3. Create `Credit` Transaction.
    4. Update Balance Atomically.
    5. Handle concurrency via Optimistic Locking (Skip on conflict).
