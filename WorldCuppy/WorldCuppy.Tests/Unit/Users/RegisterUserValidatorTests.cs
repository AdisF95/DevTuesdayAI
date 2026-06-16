using Bogus;
using FluentValidation.TestHelper;
using WorldCuppy.Features.Users;

namespace WorldCuppy.Tests.Unit.Users;

/// <summary>Unit tests for <see cref="RegisterUserValidator" />.</summary>
public class RegisterUserValidatorTests
{
    private readonly RegisterUserValidator _validator = new();
    private readonly Faker _faker = new();

    private static string SafeUsername(Faker faker)
    {
        var name = faker.Internet.UserName().Replace(".", "_").Replace("-", "_");
        return name[..Math.Min(50, name.Length)];
    }

    /// <summary>A valid command built with Bogus — all rule variations start from this baseline.</summary>
    private RegisterUserCommand ValidCommand() => new(
        Username: SafeUsername(_faker),
        Email: _faker.Internet.Email(),
        Password: _faker.Internet.Password(memorable: false, length: 12));

    [Fact]
    public void RegisterUserValidator_WhenCommandIsValid_ShouldPassAllRules()
    {
        var cmd = new RegisterUserCommand("TestUser123", _faker.Internet.Email(), "SecurePass1!");
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RegisterUserValidator_WhenUsernameIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { Username = string.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void RegisterUserValidator_WhenUsernameTooShort_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { Username = "ab" };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void RegisterUserValidator_WhenUsernameTooLong_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { Username = new string('a', 51) };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("user name")]
    [InlineData("user@name")]
    [InlineData("user-name")]
    [InlineData("user.name")]
    public void RegisterUserValidator_WhenUsernameHasInvalidChars_ShouldFailValidation(string username)
    {
        var cmd = ValidCommand() with { Username = username };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Username);
    }

    [Theory]
    [InlineData("validuser")]
    [InlineData("Valid_User_123")]
    [InlineData("abc")]
    public void RegisterUserValidator_WhenUsernameIsValid_ShouldPassValidation(string username)
    {
        var cmd = ValidCommand() with { Username = username };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Username);
    }

    [Fact]
    public void RegisterUserValidator_WhenEmailIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { Email = string.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("missing@")]
    [InlineData("@nodomain.com")]
    public void RegisterUserValidator_WhenEmailIsInvalid_ShouldFailValidation(string email)
    {
        var cmd = ValidCommand() with { Email = email };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void RegisterUserValidator_WhenPasswordIsEmpty_ShouldFailValidation()
    {
        var cmd = ValidCommand() with { Password = string.Empty };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("1234567")]
    public void RegisterUserValidator_WhenPasswordTooShort_ShouldFailValidation(string password)
    {
        var cmd = ValidCommand() with { Password = password };
        var result = _validator.TestValidate(cmd);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void RegisterUserValidator_WhenPasswordIsEightChars_ShouldPassValidation()
    {
        var cmd = ValidCommand() with { Password = "12345678" };
        var result = _validator.TestValidate(cmd);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }
}
