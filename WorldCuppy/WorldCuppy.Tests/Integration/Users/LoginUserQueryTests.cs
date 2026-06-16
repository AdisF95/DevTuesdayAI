using Bogus;
using WorldCuppy.Features.Users;
using WorldCuppy.Infrastructure.Auth;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Users;

/// <summary>Integration tests for <see cref="LoginUserHandler" /> against a real PostgreSQL database.</summary>
public class LoginUserQueryTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    private RegisterUserHandler RegisterHandler() => new(db.CreateDbContext(), new PendingAuthStore());
    private LoginUserHandler LoginHandler() => new(db.CreateDbContext(), new PendingAuthStore());

    private static string SafeUsername(Faker faker)
    {
        var name = faker.Internet.UserName().Replace(".", "_").Replace("-", "_");
        return name[..Math.Min(10, name.Length)];
    }

    /// <summary>Seeds a user and returns the command used (so tests know the credentials).</summary>
    private async Task<RegisterUserCommand> SeedUserAsync()
    {
        var cmd = new RegisterUserCommand(
            Username: SafeUsername(_faker),
            Email: _faker.Internet.Email(),
            Password: _faker.Internet.Password(memorable: false, length: 12));

        await RegisterHandler().Handle(cmd, CancellationToken.None);
        return cmd;
    }

    [Fact]
    public async Task LoginUser_WhenCredentialsAreCorrectByUsername_ShouldReturnToken()
    {
        var seeded = await SeedUserAsync();

        var token = await LoginHandler().Handle(
            new LoginUserQuery(seeded.Username, seeded.Password),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, token);
    }

    [Fact]
    public async Task LoginUser_WhenCredentialsAreCorrectByEmail_ShouldReturnToken()
    {
        var seeded = await SeedUserAsync();

        var token = await LoginHandler().Handle(
            new LoginUserQuery(seeded.Email, seeded.Password),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, token);
    }

    [Fact]
    public async Task LoginUser_WhenPasswordIsWrong_ShouldThrowUnauthorized()
    {
        var seeded = await SeedUserAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => LoginHandler().Handle(
                new LoginUserQuery(seeded.Username, "wrongpassword"),
                CancellationToken.None));
    }

    [Fact]
    public async Task LoginUser_WhenUserDoesNotExist_ShouldThrowUnauthorized()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => LoginHandler().Handle(
                new LoginUserQuery("ghost_user_xyz", "anypassword"),
                CancellationToken.None));
    }
}
