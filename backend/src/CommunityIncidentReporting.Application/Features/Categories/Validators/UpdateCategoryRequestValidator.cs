using CommunityIncidentReporting.Application.Features.Categories.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Categories.Validators;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DefaultPriority).IsInEnum();
        RuleFor(x => x.SlaHours).GreaterThan(0).LessThanOrEqualTo(24 * 30);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
