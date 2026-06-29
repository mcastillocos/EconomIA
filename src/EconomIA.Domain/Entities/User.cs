using System.Security.Cryptography;
using System.Text;

namespace EconomIA.Domain.Entities;

/// <summary>
/// Usuario de la plataforma. Gestión de credenciales y rol.
/// </summary>
public class User : Entity<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string PasswordSalt { get; private set; } = string.Empty;
    public string Role { get; private set; } = Roles.Free; // free | premium | admin
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }

    private User() { }

    public static User Create(string email, string displayName, string password, string role = Roles.Free)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new Exceptions.DomainException("Email inválido.");
        if (string.IsNullOrWhiteSpace(displayName))
            throw new Exceptions.DomainException("Nombre de usuario requerido.");
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            throw new Exceptions.DomainException("La contraseña debe tener al menos 6 caracteres.");
        if (!Roles.IsValid(role))
            throw new Exceptions.DomainException($"Rol inválido: {role}");

        var salt = GenerateSalt();
        var hash = HashPassword(password, salt);

        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.ToLowerInvariant().Trim(),
            DisplayName = displayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public bool VerifyPassword(string password)
    {
        var hash = HashPassword(password, PasswordSalt);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(hash),
            Encoding.UTF8.GetBytes(PasswordHash));
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void SetRefreshToken(string token, TimeSpan expiration)
    {
        RefreshToken = token;
        RefreshTokenExpiresAt = DateTime.UtcNow.Add(expiration);
    }

    public void RevokeRefreshToken()
    {
        RefreshToken = null;
        RefreshTokenExpiresAt = null;
    }

    public bool IsRefreshTokenValid(string token)
    {
        return RefreshToken == token && RefreshTokenExpiresAt > DateTime.UtcNow;
    }

    public void ChangeRole(string newRole)
    {
        if (!Roles.IsValid(newRole))
            throw new Exceptions.DomainException($"Rol inválido: {newRole}");
        Role = newRole;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public void ChangePassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            throw new Exceptions.DomainException("La contraseña debe tener al menos 6 caracteres.");

        PasswordSalt = GenerateSalt();
        PasswordHash = HashPassword(newPassword, PasswordSalt);
    }

    private static string GenerateSalt()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashPassword(string password, string salt)
    {
        using var hmac = new HMACSHA512(Convert.FromBase64String(salt));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
}

public static class Roles
{
    public const string Free = "free";
    public const string Premium = "premium";
    public const string Admin = "admin";

    public static bool IsValid(string role) => role is Free or Premium or Admin;
}
