using CommunityIncidentReporting.Application.Features.Categories.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Categories.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DefaultPriority).IsInEnum();
        RuleFor(x => x.SlaHours).GreaterThan(0).LessThanOrEqualTo(24 * 30);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);

        RuleFor(x => x.Slug)
            .Matches("^[a-z0-9]+(-[a-z0-9]+)*$").WithMessage("Slug must be lowercase, alphanumeric, hyphen-separated.")
            .MaximumLength(80)
            .When(x => x.Slug is not null);
        RuleFor(x => x.IconKey).MaximumLength(80).When(x => x.IconKey is not null);
        RuleFor(x => x.ColourToken).MaximumLength(40).When(x => x.ColourToken is not null);
    }
}
