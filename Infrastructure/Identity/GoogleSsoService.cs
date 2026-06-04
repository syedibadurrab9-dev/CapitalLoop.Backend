using Application.Common.DTOs;
using Application.Common.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Identity;

public class GoogleSsoService : IIdentityService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleSsoService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public GoogleSsoService(
        IConfiguration configuration,
        ILogger<GoogleSsoService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    // 🚀 UPDATED: Signature now explicitly accepts an optional codeVerifier for PKCE compliance checks
    public async Task<AuthResultDto> ValidateGoogleTokenAndGenerateJwtAsync(
        string incomingPayload,
        string? codeVerifier,
        CancellationToken cancellationToken)
    {
        string idToken = incomingPayload;

        // DETECT: If the payload is an Auth Code instead of a structured JWT token string, perform back-channel swap
        if (!incomingPayload.Contains("."))
        {
            _logger.LogInformation("🔄 Raw Authorization Code detected. Exchanging token with Google API...");
            idToken = await ExchangeCodeForIdTokenAsync(incomingPayload, codeVerifier, cancellationToken);
        }

        try
        {
            var allowedAudiences = new List<string>();
            var audienceSection = _configuration.GetSection("Authentication:Google:ValidClientIds");

            foreach (var child in audienceSection.GetChildren())
            {
                if (!string.IsNullOrEmpty(child.Value))
                    allowedAudiences.Add(child.Value);
            }

            if (allowedAudiences.Count == 0)
            {
                allowedAudiences.Add("511627953374-9cv6hkgusktu30rhianqoduq1easgs9l.apps.googleusercontent.com"); // React Native
                allowedAudiences.Add("13131396392-40lscdfkt1896c3s52p7hbvs9gtg6vah.apps.googleusercontent.com");  // Angular Web
            }

            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = allowedAudiences
            };

            // Securely evaluate the signature against Google's public key arrays
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, validationSettings);

            string assignedTier =
                payload.Email.EndsWith("@capitalloop.com") ||
                payload.Email == "syedibad335@gmail.com"
                    ? "agent"
                    : "premium";

            var appJwt = GenerateApplicationJwt(payload.Name, payload.Email, assignedTier);

            return new AuthResultDto
            {
                AccessToken = appJwt,
                Name = payload.Name,
                Email = payload.Email,
                Tier = assignedTier,
                IsPremium = assignedTier != "free",
                Success = true
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogError("❌ Token validation failed: {Message}", ex.Message);
            throw new UnauthorizedAccessException("Invalid Google token context parameters.", ex);
        }
    }

    private async Task<string> ExchangeCodeForIdTokenAsync(string code, string? codeVerifier, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient();

        var secret = _configuration["Authentication:Google:WebClientSecret"]
                     ?? throw new InvalidOperationException("Missing Google WebClientSecret inside appsettings config.");

        var requestParameters = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", "http://localhost:4200/login" },
            { "client_id", "13131396392-40lscdfkt1896c3s52p7hbvs9gtg6vah.apps.googleusercontent.com" },
            { "client_secret", secret }
        };

        // 🚀 CRITICAL FIX: If Angular sent a PKCE verification key token, pass it down to finalize the handshake authority
        if (!string.IsNullOrWhiteSpace(codeVerifier))
        {
            requestParameters.Add("code_verifier", codeVerifier);
        }

        var response = await client.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(requestParameters),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("❌ Google OAuth Server Rejected Exchange Handshake: {Body}", error);
            throw new UnauthorizedAccessException("Google token exchange failed down the pipe branch channel.");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var token = JsonSerializer.Deserialize<GoogleTokenResponse>(json);

        if (token == null || string.IsNullOrEmpty(token.IdToken))
            throw new UnauthorizedAccessException("Invalid token block response state layout.");

        return token.IdToken;
    }

    private string GenerateApplicationJwt(string name, string email, string tier)
    {
        var handler = new JwtSecurityTokenHandler();

        var key = Encoding.UTF8.GetBytes(
            _configuration["Jwt:Secret"]
            ?? "SuperSecretLongBaseSecureKey6366F1!CapitalLoopMasterTokenValidationKey2026");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, email),
            new Claim("tier", tier)
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"] ?? "CapitalLoopAuthAuthority",
            Audience = _configuration["Jwt:Audience"] ?? "CapitalLoopMobileAppClients",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}

public class GoogleTokenResponse
{
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;
}