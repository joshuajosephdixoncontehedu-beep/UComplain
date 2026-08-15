using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileAuth.Validators;

public class RegisterReporterRequestValidator : AbstractValidator<RegisterReporterRequest>
{
    public RegisterReporterRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .Matches(@"^\+?[1-9]\d{6,14}$")
            .WithMessage("Phone number must be a valid number, e.g. +23276111999.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(200)
            .Matches("[A-Za-z]").WithMessage("Password must contain at least one letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match.");

        RuleFor(x => x.ConsentAccepted)
            .Equal(true)
            .WithMessage("You must accept the data consent terms to register.");
    }
}
