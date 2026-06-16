namespace WorldCuppy.Domain;

/// <summary>A registered player who submits predictions and accumulates points.</summary>
public class User
{
    /// <summary>Unique identifier for this user.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Unique display name chosen at registration.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Unique email address used for login.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>PBKDF2-hashed password (salt:hash, base-64 encoded).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>UTC timestamp of account creation.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;
}
