using FluentValidation;

namespace WorldCuppy.Features.Users;

/// <summary>Validates <see cref="RegisterUserCommand" /> inputs before the handler runs.</summary>
public class RegisterUserValidator : AbstractValidator<RegisterUserCommand>
{
    /// <summary>Configures validation rules for registration fields.</summary>
    public RegisterUserValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(50)
            .Matches("^[a-zA-Z0-9_]+$")
            .WithMessage("Username may only contain letters, numbers, and underscores.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(200);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters.");
    }
}
