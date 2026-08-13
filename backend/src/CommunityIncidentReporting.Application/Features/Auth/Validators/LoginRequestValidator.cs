using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(200);
    }
}
