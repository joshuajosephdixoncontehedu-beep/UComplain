using CommunityIncidentReporting.Application.Features.Notifications.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Notifications.Validators;

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Platform).IsInEnum();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(500);
    }
}
