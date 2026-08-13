using CommunityIncidentReporting.Infrastructure.Security;
using FluentAssertions;

namespace CommunityIncidentReporting.Api.Tests.Security;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _sut = new();

    [Fact]
    public void Hash_ProducesAValueThatVerifySucceedsAgainst()
    {
        var hash = _sut.Hash("Correct-Horse-Battery-Staple-1");

        _sut.Verify("Correct-Horse-Battery-Staple-1", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_FailsForAWrongPassword()
    {
        var hash = _sut.Hash("Correct-Horse-Battery-Staple-1");

        _sut.Verify("some-other-password", hash).Should().BeFalse();
    }

    [Fact]
    public void Hash_NeverReturnsThePlainTextPassword()
    {
        const string password = "Correct-Horse-Battery-Staple-1";

        _sut.Hash(password).Should().NotBe(password);
    }

    [Fact]
    public void Hash_ProducesADifferentValueEachTime()
    {
        const string password = "Correct-Horse-Battery-Staple-1";

        _sut.Hash(password).Should().NotBe(_sut.Hash(password), "BCrypt salts each hash independently");
    }
}
