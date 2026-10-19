using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace Application.UnitTests;

public class DomainEntityTests
{
    [Fact]
    public void AssetNode_CreateManual_ValidPsxEquity_ShouldSetProperties()
    {
        // Arrange & Act
        var node = AssetNode.CreateManual("investor@example.com", "Lucky Cement (LUCK)", AssetCategory.PSX_Equity, 150000m);

        // Assert
        node.Should().NotBeNull();
        node.Name.Should().Be("Lucky Cement (LUCK)");
        node.Category.Should().Be(AssetCategory.PSX_Equity);
        node.HoldingValuePKR.Should().Be(150000m);
        node.IsAutoSynced.Should().BeFalse();
        node.InstallmentsPaid.Should().BeNull();
    }

    [Fact]
    public void AssetNode_CreateManual_NegativeValuation_ShouldThrowInvalidAssetValueException()
    {
        // Act
        var act = () => AssetNode.CreateManual("investor@example.com", "Gold Tola", AssetCategory.Precious_Metal, -5000m);

        // Assert
        act.Should().Throw<InvalidAssetValueException>()
           .WithMessage("*positive numeric sums*");
    }

    [Fact]
    public void AssetNode_CreateManual_RealEstateInstallments_PaidExceedsTotal_ShouldThrow()
    {
        // Act
        var act = () => AssetNode.CreateManual(
            "investor@example.com",
            "Bahria Town Sector C Plot File",
            AssetCategory.Real_Estate,
            2500000m,
            paid: 15,
            total: 12
        );

        // Assert
        act.Should().Throw<InvalidInstallmentSchemaException>()
           .WithMessage("*outpace your master real estate file framework*");
    }

    [Fact]
    public void AssetNode_CreateManual_RealEstateInstallments_ValidScheme_ShouldTrackPaidAndTotal()
    {
        // Act
        var node = AssetNode.CreateManual(
            "investor@example.com",
            "DHA Phase 9 Prism File",
            AssetCategory.Real_Estate,
            4500000m,
            paid: 6,
            total: 12
        );

        // Assert
        node.InstallmentsPaid.Should().Be(6);
        node.TotalInstallments.Should().Be(12);
        node.HoldingValuePKR.Should().Be(4500000m);
    }

    [Fact]
    public void UserProfile_Register_ShouldDefaultToUnverifiedKyc()
    {
        // Act
        var profile = UserProfile.Register("Syed Investor", "syed@example.com");

        // Assert
        profile.Name.Should().Be("Syed Investor");
        profile.Email.Should().Be("syed@example.com");
        profile.IsPremiumUser.Should().BeFalse();
        profile.KycStatus.Should().Be(KycStatus.Unverified);
    }

    [Fact]
    public void KycDocument_RecordSubmission_ShouldStoreSmartCnicDetails()
    {
        // Act
        var kyc = KycDocument.RecordSubmission(
            "syed@example.com",
            "Smart_CNIC",
            "42101-1234567-1",
            "https://storage.capitalloop.pk/kyc/42101-1234567-1_front.jpg"
        );

        // Assert
        kyc.DocumentType.Should().Be("Smart_CNIC");
        kyc.DocumentNumber.Should().Be("42101-1234567-1");
        kyc.StorageUrlPath.Should().Contain("front.jpg");
    }

    [Fact]
    public void AssetNode_UpdateInstallments_ValidAdvancement_ShouldUpdatePaidAndTotal()
    {
        // Arrange
        var node = AssetNode.CreateManual(
            "investor@example.com",
            "DHA Phase 9 Prism File",
            AssetCategory.Real_Estate,
            2000000m,
            paid: 3,
            total: 12
        );

        // Act
        node.UpdateInstallments(4, 12, 2500000m);

        // Assert
        node.InstallmentsPaid.Should().Be(4);
        node.TotalInstallments.Should().Be(12);
        node.HoldingValuePKR.Should().Be(2500000m);
        node.LastUpdatedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AssetNode_UpdateInstallments_PaidExceedsTotal_ShouldThrow()
    {
        // Arrange
        var node = AssetNode.CreateManual(
            "investor@example.com",
            "Park View City Plot",
            AssetCategory.Real_Estate,
            1000000m,
            paid: 5,
            total: 10
        );

        // Act
        var act = () => node.UpdateInstallments(11, 10);

        // Assert
        act.Should().Throw<InvalidInstallmentSchemaException>()
           .WithMessage("*outpace your master real estate file framework*");
    }

    [Fact]
    public void AssetNode_UpdateInstallments_NonRealEstateAsset_ShouldThrow()
    {
        // Arrange
        var node = AssetNode.CreateManual("investor@example.com", "BTC", AssetCategory.Crypto, 500000m);

        // Act
        var act = () => node.UpdateInstallments(2, 5);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*exclusively to real estate*");
    }

    [Fact]
    public void DomainExceptions_ShouldInheritFromDomainExceptionBase()
    {
        typeof(InvalidAssetValueException).Should().BeAssignableTo<DomainException>();
        typeof(InvalidInstallmentSchemaException).Should().BeAssignableTo<DomainException>();
        typeof(AssetCapExceededException).Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void AdminOperator_Provision_ShouldInitializeCorrectly()
    {
        // Arrange & Act
        var op = AdminOperator.Provision(
            "Syed Ibad Ur Rab",
            "SyedIbad335@gmail.com",
            AdminRole.SuperAdmin,
            "secp-adv-8492",
            "system-genesis@capitalloop.pk"
        );

        // Assert
        op.Id.Should().NotBeEmpty();
        op.Name.Should().Be("Syed Ibad Ur Rab");
        op.Email.Should().Be("syedibad335@gmail.com");
        op.Role.Should().Be(AdminRole.SuperAdmin);
        op.LicenseNumber.Should().Be("SECP-ADV-8492");
        op.IsActive.Should().BeTrue();
        op.OnboardedBy.Should().Be("system-genesis@capitalloop.pk");
        op.OnboardedUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AdminOperator_DeactivateAndActivate_ShouldTransitionStatus()
    {
        // Arrange
        var op = AdminOperator.Provision("Test Officer", "officer@capitalloop.com", AdminRole.ComplianceOfficer, "SECP-ADV-1234", "admin@capitalloop.com");

        // Act & Assert
        op.Deactivate();
        op.IsActive.Should().BeFalse();

        op.Activate();
        op.IsActive.Should().BeTrue();
    }
}
