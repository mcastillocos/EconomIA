using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;
using FluentAssertions;

namespace EconomIA.Domain.Tests.Entities;

public class UserTests
{
    [Fact]
    public void Create_ValidData_CreatesUser()
    {
        var user = User.Create("test@example.com", "Test User", "password123");

        user.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
        user.Role.Should().Be(Roles.Free);
        user.IsActive.Should().BeTrue();
        user.Id.Should().NotBeEmpty();
        user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_AdminRole_SetsRole()
    {
        var user = User.Create("admin@test.com", "Admin", "securePass1", Roles.Admin);
        user.Role.Should().Be(Roles.Admin);
    }

    [Fact]
    public void Create_InvalidEmail_Throws()
    {
        var act = () => User.Create("notanemail", "Name", "password123");
        act.Should().Throw<DomainException>().WithMessage("*Email*");
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        var act = () => User.Create("test@test.com", "", "password123");
        act.Should().Throw<DomainException>().WithMessage("*Nombre*");
    }

    [Fact]
    public void Create_ShortPassword_Throws()
    {
        var act = () => User.Create("test@test.com", "Name", "12345");
        act.Should().Throw<DomainException>().WithMessage("*6 caracteres*");
    }

    [Fact]
    public void Create_InvalidRole_Throws()
    {
        var act = () => User.Create("test@test.com", "Name", "password123", "superadmin");
        act.Should().Throw<DomainException>().WithMessage("*Rol inválido*");
    }

    [Fact]
    public void Create_NormalizesEmail()
    {
        var user = User.Create("  TEST@EXAMPLE.COM  ", "Name", "password123");
        user.Email.Should().Be("test@example.com");
    }

    [Fact]
    public void VerifyPassword_CorrectPassword_ReturnsTrue()
    {
        var user = User.Create("test@test.com", "Name", "mySecurePass");
        user.VerifyPassword("mySecurePass").Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WrongPassword_ReturnsFalse()
    {
        var user = User.Create("test@test.com", "Name", "mySecurePass");
        user.VerifyPassword("wrongPassword").Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_Valid_ChangesHash()
    {
        var user = User.Create("test@test.com", "Name", "oldPassword");
        var oldHash = user.PasswordHash;

        user.ChangePassword("newPassword123");

        user.PasswordHash.Should().NotBe(oldHash);
        user.VerifyPassword("newPassword123").Should().BeTrue();
        user.VerifyPassword("oldPassword").Should().BeFalse();
    }

    [Fact]
    public void ChangePassword_TooShort_Throws()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        var act = () => user.ChangePassword("abc");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RefreshToken_SetAndValidate()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        user.SetRefreshToken("my-refresh-token", TimeSpan.FromDays(7));

        user.IsRefreshTokenValid("my-refresh-token").Should().BeTrue();
        user.IsRefreshTokenValid("wrong-token").Should().BeFalse();
    }

    [Fact]
    public void RefreshToken_Revoke_InvalidatesToken()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        user.SetRefreshToken("token", TimeSpan.FromDays(7));
        user.RevokeRefreshToken();

        user.IsRefreshTokenValid("token").Should().BeFalse();
    }

    [Fact]
    public void ChangeRole_Valid_ChangesRole()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        user.ChangeRole(Roles.Premium);
        user.Role.Should().Be(Roles.Premium);
    }

    [Fact]
    public void ChangeRole_Invalid_Throws()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        var act = () => user.ChangeRole("god");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        user.Deactivate();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RecordLogin_SetsLastLoginAt()
    {
        var user = User.Create("test@test.com", "Name", "password123");
        user.RecordLogin();
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Theory]
    [InlineData(Roles.Free, true)]
    [InlineData(Roles.Premium, true)]
    [InlineData(Roles.Admin, true)]
    [InlineData("invalid", false)]
    [InlineData("", false)]
    public void Roles_IsValid_ReturnsExpected(string role, bool expected)
    {
        Roles.IsValid(role).Should().Be(expected);
    }
}
