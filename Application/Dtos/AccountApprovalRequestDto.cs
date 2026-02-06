using Microsoft.AspNetCore.Http;

namespace Application.DTOs;

public record AccountApprovalRequestDto(
    int AccountId,
    IFormFile IdDocument); // Using IFormFile for simulated upload
