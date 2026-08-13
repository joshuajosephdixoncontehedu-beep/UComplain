using CommunityIncidentReporting.Application.Features.Reports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.Reports.Validators;

public class AssignReportRequestValidator : AbstractValidator<AssignReportRequest>
{
    public AssignReportRequestValidator()
    {
        RuleFor(x => x.AdminUserId).NotEmpty();
    }
}
