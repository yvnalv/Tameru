namespace Tameru.Identity.Application.Abstractions;

/// <summary>Hashes and verifies passwords. Implemented in Infrastructure (docs/SECURITY.md).</summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string passwordHash, string providedPassword);
}
