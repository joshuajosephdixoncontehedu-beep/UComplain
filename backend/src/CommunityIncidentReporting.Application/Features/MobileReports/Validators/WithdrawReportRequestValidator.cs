using CommunityIncidentReporting.Application.Features.MobileReports.Dtos;
using FluentValidation;

namespace CommunityIncidentReporting.Application.Features.MobileReports.Validators;

public class WithdrawReportRequestValidator : AbstractValidator<WithdrawReportRequest>
{
    public WithdrawReportRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}
