using Domain.Enums;

namespace Application.DTOs;

public record AccountApprovalRequestResponseDto(
    int Id,
    int AccountId,
    string AccountNumber,
    AccountType AccountType,
    int UserId,
    string UserName,
    string UserEmail,
    string IdDocumentUrl,
    DateTime CreatedAt, // Assuming BaseEntity has CreatedAt
    bool IsApproved,
    DateTime? ProcessedAt
);
