using Bogus;
using WorldCuppy.Features.Users;
using WorldCuppy.Infrastructure.Auth;
using WorldCuppy.Tests.Integration.Infrastructure;

namespace WorldCuppy.Tests.Integration.Users;

/// <summary>Integration tests for <see cref="RegisterUserHandler" /> against a real PostgreSQL database.</summary>
public class RegisterUserCommandTests(PostgreSqlFixture db) : IClassFixture<PostgreSqlFixture>
{
    private readonly Faker _faker = new();

    private static string SafeUsername(Faker faker)
    {
        var name = faker.Internet.UserName().Replace(".", "_").Replace("-", "_");
        return name[..Math.Min(10, name.Length)];
    }

    /// <summary>Builds a valid register command with Bogus-generated data.</summary>
    private RegisterUserCommand ValidCommand() => new(
        Username: SafeUsername(_faker),
        Email: _faker.Internet.Email(),
        Password: _faker.Internet.Password(memorable: false, length: 12));

    private RegisterUserHandler CreateHandler() =>
        new(db.CreateDbContext(), new PendingAuthStore());

    [Fact]
    public async Task RegisterUser_WhenCommandIsValid_ShouldPersistUserAndReturnToken()
    {
        var cmd = ValidCommand();
        var handler = CreateHandler();

        var token = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, token);

        await using var ctx = db.CreateDbContext();
        var user = ctx.Users.SingleOrDefault(u => u.Username == cmd.Username);
        Assert.NotNull(user);
        Assert.Equal(cmd.Email, user.Email);
        // Password must be stored hashed — never in plain text.
        Assert.NotEqual(cmd.Password, user.PasswordHash);
    }

    [Fact]
    public async Task RegisterUser_WhenUsernameAlreadyTaken_ShouldThrow()
    {
        var cmd = ValidCommand();
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        // Second registration with same username but different email.
        var duplicate = cmd with { Email = _faker.Internet.Email() };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateHandler().Handle(duplicate, CancellationToken.None));
    }

    [Fact]
    public async Task RegisterUser_WhenEmailAlreadyRegistered_ShouldThrow()
    {
        var cmd = ValidCommand();
        var handler = CreateHandler();

        await handler.Handle(cmd, CancellationToken.None);

        // Second registration with different username but same email.
        var duplicate = cmd with { Username = SafeUsername(_faker) };
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateHandler().Handle(duplicate, CancellationToken.None));
    }
}
