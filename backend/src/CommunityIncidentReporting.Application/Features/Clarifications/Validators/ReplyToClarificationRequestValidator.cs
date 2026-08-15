using CommunityIncidentReporting.Application.Features.Clarifications.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Clarifications.Validators;

public class ReplyToClarificationRequestValidator : AbstractValidator<ReplyToClarificationRequest>
{
    public ReplyToClarificationRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.AttachmentId).NotEqual(Guid.Empty).When(x => x.AttachmentId is not null);
    }
}
