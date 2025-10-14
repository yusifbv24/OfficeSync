using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using IdentityService.Application.Interfaces;
using IdentityService.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;

namespace IdentityService.Infrastructure.Services;

/// <summary>
/// Implements JWT token generation and validation.
/// JWT (JSON Web Token) is a standard for securely transmitting information as a JSON object.
/// The token is digitally signed, so recipients can verify it hasn't been tampered with.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _accessTokenExpirationMinutes;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;

        // Load JWT settings from configuration
        _secretKey = _configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured");

        _issuer = _configuration["Jwt:Issuer"] ?? "IdentityService";
        _audience = _configuration["Jwt:Audience"] ?? "ChatApplication";
        _accessTokenExpirationMinutes = int.Parse(_configuration["Jwt:AccessTokenExpirationMinutes"] ?? "60");

        // Validate that the secret key is strong enough
        if (_secretKey.Length < 32)
            throw new InvalidOperationException("JWT SecretKey must be at least 32 characters long");
    }

    /// <summary>
    /// Generate a JWT access token for a user.
    /// The token contains claims (user information) that can be read by anyone,
    /// but only our service can create or modify tokens (because we have the secret key).
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        // Claims are pieces of information about the user stored in the token
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
            new(JwtRegisteredClaimNames.Iat, new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Issued at (Unix time)
        };

        // Create the signing key from our secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Build the token with all its components
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpirationMinutes),
            signingCredentials: credentials
        );

        // Serialize the token to a string that can be sent to clients
        return new JwtSecurityTokenHandler().WriteToken(token);
    }




    /// <summary>
    /// Generate a refresh token.
    /// Unlike JWTs, refresh tokens are completely random and stored in the database.
    /// This gives us more control - we can revoke them individually.
    /// </summary>
    public string GenerateRefreshToken()
    {
        // Generate 64 random bytes
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        // Convert to Base64 string for easy transmission
        return Convert.ToBase64String(randomNumber);
    }




    /// <summary>
    /// Validate a JWT token and extract the user ID.
    /// Returns null if the token is invalid, expired, or tampered with.
    /// </summary>
    public Guid? ValidateAccessToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            // Define what makes a token valid
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _issuer,
                ValidateAudience = true,
                ValidAudience = _audience,
                ValidateLifetime = true, // Check if token is expired
                ClockSkew = TimeSpan.Zero // No grace period for expiration
            };

            // Validate and parse the token
            var principal = tokenHandler.ValidateToken(token, validationParameters, out var securityToken);

            // Verify the token uses the correct algorithm (prevent algorithm substitution attacks)
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            // Extract and return the user ID from the token claims
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return null;
            }

            return userId;
        }
        catch
        {
            // If anything goes wrong during validation, the token is invalid
            return null;
        }
    }
}