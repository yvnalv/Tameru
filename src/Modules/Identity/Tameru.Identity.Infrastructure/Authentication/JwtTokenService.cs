using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Tameru.Identity.Application.Abstractions;
using Tameru.Identity.Domain;
using Tameru.SharedKernel.Time;

namespace Tameru.Identity.Infrastructure.Authentication;

/// <summary>
/// Issues HMAC-SHA256 signed JWT access tokens and cryptographically-random opaque refresh tokens.
/// Refresh tokens are stored only as a SHA-256 hash (docs/SECURITY.md).
/// </summary>
internal sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IClock _clock;
    private readonly SigningCredentials _credentials;

    public JwtTokenService(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
        _credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    }

    public AccessToken CreateAccessToken(User user)
    {
        var expiresAt = _clock.UtcNow.AddMinutes(_options.AccessMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("name", user.DisplayName),
            new Claim("locale", user.Locale),
        };

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: _credentials);

        var jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessToken(jwt, expiresAt);
    }

    public RefreshTokenValue CreateRefreshToken()
    {
        var raw = Base64Url(RandomNumberGenerator.GetBytes(32));
        var hash = HashRefreshToken(raw);
        var expiresAt = _clock.UtcNow.AddDays(_options.RefreshDays);
        return new RefreshTokenValue(raw, hash, expiresAt);
    }

    public string HashRefreshToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
