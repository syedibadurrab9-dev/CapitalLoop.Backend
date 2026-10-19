using System;

namespace Application.Common.DTOs;

public class AuthResultDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Tier { get; set; } = string.Empty;
    public bool IsPremium { get; set; }
    public bool Success { get; set; }
    public string? Role { get; set; }
    public string? LicenseNumber { get; set; }
    public string? KycStatus { get; set; }
}