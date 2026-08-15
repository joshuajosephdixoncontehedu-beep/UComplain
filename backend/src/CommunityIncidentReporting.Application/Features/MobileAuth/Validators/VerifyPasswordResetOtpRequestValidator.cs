using CommunityIncidentReporting.Application.Features.MobileAuth.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileAuth.Validators;

public class VerifyPasswordResetOtpRequestValidator : AbstractValidator<VerifyPasswordResetOtpRequest>
{
    public VerifyPasswordResetOtpRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(x => x.OtpCode).NotEmpty().Matches(@"^\d{6}$").WithMessage("Code must be 6 digits.");
    }
}
