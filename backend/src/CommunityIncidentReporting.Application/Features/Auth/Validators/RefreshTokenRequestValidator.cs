using CommunityIncidentReporting.Application.Features.Auth.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Auth.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
