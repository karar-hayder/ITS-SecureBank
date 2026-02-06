namespace Application.DTOs;

public record ApproveAccountDto(int RequestId, bool IsApproved, string? Remarks);
