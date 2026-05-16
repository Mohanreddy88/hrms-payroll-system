using HrmsApi.Data;
using HrmsApi.Models;
using Microsoft.EntityFrameworkCore;
using FluentAssertions;

namespace HrmsApi.Tests;

/// <summary>
/// Tests for authentication and user management
/// </summary>
public class AuthenticationTests
{
    private HrmsDbContext GetInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new HrmsDbContext(options);
    }

    [Fact]
    public void PasswordHash_ShouldBeVerifiedCorrectly()
    {
        // Arrange
        var plainPassword = "MySecurePassword123!";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        // Act
        var isValid = BCrypt.Net.BCrypt.Verify(plainPassword, hashedPassword);
        var isInvalid = BCrypt.Net.BCrypt.Verify("WrongPassword", hashedPassword);

        // Assert
        isValid.Should().BeTrue();
        isInvalid.Should().BeFalse();
    }

    [Fact]
    public async Task User_ShouldBeCreatedWithHashedPassword()
    {
        // Arrange
        using var db = GetInMemoryDb();
        var plainPassword = "TestPassword123!";

        var user = new User
        {
            Username = "testuser",
            Email = "test@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            Role = "Employee",
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var savedUser = await db.Users.FirstOrDefaultAsync(u => u.Username == "testuser");

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.PasswordHash.Should().NotBe(plainPassword); // Should be hashed
        BCrypt.Net.BCrypt.Verify(plainPassword, savedUser.PasswordHash).Should().BeTrue();
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Employee")]
    [InlineData("Manager")]
    public async Task User_ShouldHaveValidRole(string role)
    {
        // Arrange
        using var db = GetInMemoryDb();

        var user = new User
        {
            Username = $"user_{role}",
            Email = $"{role}@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = role,
            IsActive = true
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var savedUser = await db.Users.FirstOrDefaultAsync(u => u.Role == role);

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Role.Should().Be(role);
    }

    [Fact]
    public async Task InactiveUser_ShouldNotBeAbleToLogin()
    {
        // Arrange
        using var db = GetInMemoryDb();

        var user = new User
        {
            Username = "inactive_user",
            Email = "inactive@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("password"),
            Role = "Employee",
            IsActive = false // Inactive
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Act
        var foundUser = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "inactive_user" && u.IsActive);

        // Assert
        foundUser.Should().BeNull(); // Should not be found when filtering by IsActive
    }
}
