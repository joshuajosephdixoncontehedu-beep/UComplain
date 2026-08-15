using CommunityIncidentReporting.Application.Common.Exceptions;
using CommunityIncidentReporting.Domain.Enums;
using CommunityIncidentReporting.Infrastructure.Persistence;
using CommunityIncidentReporting.Infrastructure.Security;
using CommunityIncidentReporting.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CommunityIncidentReporting.Api.Tests.Services;

public class EmailOtpServiceTests
{
    private static readonly OtpOptions TestOptions = new()
    {
        HashKey = "unit-test-otp-hash-key",
        ExpiryMinutes = 10,
        MaxAttempts = 3,
        ResendCooldownSeconds = 60
    };

    private static AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static EmailOtpService CreateSut(AppDbContext db, OtpOptions? options = null) =>
        new(db, Options.Create(options ?? TestOptions));

    [Fact]
    public async Task IssueThenVerify_WithTheCorrectCode_Succeeds()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);

        var code = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, code, consume: true, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Verify_WithTheWrongCode_ThrowsInvalidOtpAndIncrementsAttemptCount()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);
        await sut.IssueAsync("reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, "000000", consume: true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOtpException>();
        var stored = await db.EmailOtpVerifications.SingleAsync();
        stored.AttemptCount.Should().Be(1);
        stored.IsUsed.Should().BeFalse();
    }

    [Fact]
    public async Task Verify_AfterMaxAttempts_ThrowsInvalidOtpEvenWithTheCorrectCode()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db, new OtpOptions
        {
            HashKey = TestOptions.HashKey, ExpiryMinutes = TestOptions.ExpiryMinutes,
            MaxAttempts = 2, ResendCooldownSeconds = TestOptions.ResendCooldownSeconds
        });
        var code = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);

        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<InvalidOtpException>(() => sut.VerifyAsync(
                "reporter@example.com", EmailOtpPurpose.SignUpVerification, "000000", consume: true, CancellationToken.None));
        }

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, code, consume: true, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOtpException>("the code is no longer eligible once MaxAttempts is reached");
    }

    [Fact]
    public async Task Verify_WithAnExpiredCode_ThrowsInvalidOtp()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db, new OtpOptions
        {
            HashKey = TestOptions.HashKey, ExpiryMinutes = -1,
            MaxAttempts = TestOptions.MaxAttempts, ResendCooldownSeconds = TestOptions.ResendCooldownSeconds
        });
        var code = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, code, consume: true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOtpException>();
    }

    [Fact]
    public async Task Verify_WithConsumeTrue_CannotBeUsedASecondTime()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);
        var code = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);
        await sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, code, consume: true, CancellationToken.None);

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, code, consume: true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOtpException>("a used code must never verify again");
    }

    [Fact]
    public async Task Verify_WithConsumeFalse_CanBeVerifiedAgainAfterwards()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);
        var code = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.PasswordReset, null, null, null, CancellationToken.None);

        await sut.VerifyAsync("reporter@example.com", EmailOtpPurpose.PasswordReset, code, consume: false, CancellationToken.None);
        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.PasswordReset, code, consume: true, CancellationToken.None);

        await act.Should().NotThrowAsync("verify-only calls (the password-reset 'verify code' step) must not consume the code");
    }

    [Fact]
    public async Task Issue_ForTheSameEmailAndPurpose_InvalidatesThePreviousCode()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);
        var firstCode = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);
        await sut.IssueAsync("reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, firstCode, consume: true, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOtpException>("issuing a new code must invalidate the previous one");
    }

    [Fact]
    public async Task Issue_ForADifferentPurpose_DoesNotInvalidateTheOtherPurposesCode()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);
        var signUpCode = await sut.IssueAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);
        await sut.IssueAsync("reporter@example.com", EmailOtpPurpose.PasswordReset, null, null, null, CancellationToken.None);

        var act = () => sut.VerifyAsync(
            "reporter@example.com", EmailOtpPurpose.SignUpVerification, signUpCode, consume: true, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task CanIssueAsync_ImmediatelyAfterIssuing_IsFalseUntilTheCooldownElapses()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db, new OtpOptions
        {
            HashKey = TestOptions.HashKey, ExpiryMinutes = TestOptions.ExpiryMinutes,
            MaxAttempts = TestOptions.MaxAttempts, ResendCooldownSeconds = 3600
        });
        await sut.IssueAsync("reporter@example.com", EmailOtpPurpose.SignUpVerification, null, null, null, CancellationToken.None);

        var canIssue = await sut.CanIssueAsync("reporter@example.com", EmailOtpPurpose.SignUpVerification, CancellationToken.None);

        canIssue.Should().BeFalse();
    }

    [Fact]
    public async Task CanIssueAsync_ForAnEmailWithNoPriorOtp_IsTrue()
    {
        await using var db = CreateContext();
        var sut = CreateSut(db);

        var canIssue = await sut.CanIssueAsync("nobody-yet@example.com", EmailOtpPurpose.SignUpVerification, CancellationToken.None);

        canIssue.Should().BeTrue();
    }
}
