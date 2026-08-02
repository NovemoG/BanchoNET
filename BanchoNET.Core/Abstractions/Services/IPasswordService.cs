namespace BanchoNET.Core.Abstractions.Services;

public interface IPasswordService
{
    /// <summary>
    /// Checks an md5 hashed password against a stored bcrypt hash. Successful pairs are
    /// cached, because stable clients re-send their credentials on every web request.
    /// </summary>
    bool Verify(
        string passwordMD5,
        string passwordHash
    );

    /// <summary>
    /// Creates a bcrypt hash for an md5 hashed password and primes the cache with it.
    /// </summary>
    string Hash(
        string passwordMD5
    );

    /// <summary>
    /// Drops a stored hash from the cache.
    /// </summary>
    void Invalidate(
        string passwordHash
    );
}