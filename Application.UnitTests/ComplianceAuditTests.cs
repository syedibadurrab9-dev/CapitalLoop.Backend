using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests;

public class ComplianceAuditTests
{
    [Fact]
    public void ComplianceAuditLog_Create_ShouldInitializeCorrectly()
    {
        // Arrange
        var targetEmail = "Investor@CapitalLoop.pk";
        var reviewerEmail = "Agent@CapitalLoop.pk";
        var action = "KYC_APPROVE";
        var comments = "Approved Smart CNIC 42101-1234567-1 after NADRA check.";

        // Act
        var audit = ComplianceAuditLog.Create(targetEmail, reviewerEmail, action, comments);

        // Assert
        audit.Id.Should().NotBeEmpty();
        audit.TargetEmail.Should().Be("investor@capitalloop.pk"); // Normalized lowercase
        audit.ReviewerAgentEmail.Should().Be("agent@capitalloop.pk"); // Normalized lowercase
        audit.ActionPerformed.Should().Be("KYC_APPROVE");
        audit.Comments.Should().Be(comments);
        audit.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(KycStatus.Approved)]
    [InlineData(KycStatus.Pending)]
    [InlineData(KycStatus.Unverified)]
    [InlineData(KycStatus.Rejected)]
    public void UserProfile_UpdateKycStatus_ShouldTransitionStatus(KycStatus status)
    {
        // Arrange
        var user = UserProfile.Register("Ali Khan", "ali@capitalloop.pk");

        // Act
        user.UpdateKycStatus(status);

        // Assert
        user.KycStatus.Should().Be(status);
    }

    [Fact]
    public void UserProfile_UpdateFilerStatus_ShouldUpdateIsFilerAndTimestamp()
    {
        // Arrange
        var user = UserProfile.Register("Usman Tariq", "usman@capitalloop.pk");
        user.IsFiler.Should().BeFalse();
        user.AtlVerifiedUtc.Should().BeNull();

        // Act
        user.UpdateFilerStatus(true);

        // Assert
        user.IsFiler.Should().BeTrue();
        user.AtlVerifiedUtc.Should().NotBeNull();
        user.AtlVerifiedUtc.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GoogleSsoCommand_ShouldPreserve_Token_CodeVerifier_And_RedirectUri()
    {
        // Arrange
        var token = "4/0AeanS0Y...";
        var verifier = "transient-pkce-verifier-12345";
        var redirectUri = "capitalloop://auth";

        // Act
        var command = new Application.Portfolios.Commands.GoogleSso.GoogleSsoCommand(token, verifier, redirectUri);

        // Assert
        command.Token.Should().Be(token);
        command.CodeVerifier.Should().Be(verifier);
        command.RedirectUri.Should().Be(redirectUri);
    }

    [Fact]
    public void AuthResultDto_ShouldSupport_KycStatus_And_Role_Mapping()
    {
        // Arrange & Act
        var dto = new Application.Common.DTOs.AuthResultDto
        {
            AccessToken = "jwt-test-token",
            Name = "Citizen Investor",
            Email = "investor@capitalloop.pk",
            Role = "Investor",
            Tier = "free",
            IsPremium = false,
            Success = true,
            KycStatus = "Pending"
        };

        // Assert
        dto.Role.Should().Be("Investor");
        dto.KycStatus.Should().Be("Pending");
        dto.IsPremium.Should().BeFalse();
        dto.Success.Should().BeTrue();
    }
}
