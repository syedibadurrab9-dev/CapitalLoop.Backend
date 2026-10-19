using Application.Portfolios.Commands.SubmitKycDocuments;
using Application.Validators;
using Domain.Exceptions;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.UnitTests;

public class SubmitKycDocumentsTests
{
    private readonly SubmitKycDocumentsCommandValidator _validator = new();

    [Fact]
    public void Validator_ValidSmartCnicAndAdultAge_ShouldNotHaveValidationError()
    {
        // Arrange
        var command = new SubmitKycDocumentsCommand(
            "investor@capitalloop.pk",
            "Smart_CNIC",
            "42101-1234567-1",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("4210112345671")] // Missing hyphens
    [InlineData("42101-123456-1")]  // 6 digits in middle instead of 7
    [InlineData("4210A-1234567-1")] // Alphanumeric
    [InlineData("")]                // Empty
    public void Validator_InvalidSmartCnicFormat_ShouldHaveValidationError(string cnic)
    {
        // Arrange
        var command = new SubmitKycDocumentsCommand(
            "investor@capitalloop.pk",
            "Smart_CNIC",
            cnic,
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-25))
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DocumentNumber);
    }

    [Fact]
    public void Validator_UnderageInvestor_ShouldHaveValidationError()
    {
        // Arrange (17 years old)
        var command = new SubmitKycDocumentsCommand(
            "minor@capitalloop.pk",
            "Smart_CNIC",
            "42101-1234567-1",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-17))
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("SECP and SBP regulatory policy: Investors must be at least 18 years old to open an investment profile.");
    }

    [Fact]
    public void Validator_ValidPassport_ShouldPassValidation()
    {
        // Arrange
        var command = new SubmitKycDocumentsCommand(
            "expats@capitalloop.pk",
            "Passport",
            "AB1234567",
            DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-30))
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UnderageInvestorException_ShouldInheritFromDomainException()
    {
        typeof(UnderageInvestorException).Should().BeAssignableTo<DomainException>();
    }
}
