using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Reports.Validators;

public class AddNoteRequestValidator : AbstractValidator<AddNoteRequest>
{
    public AddNoteRequestValidator()
    {
        RuleFor(x => x.Content).NotEmpty().MaximumLength(4000);
    }
}
