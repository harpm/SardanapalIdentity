using FluentAssertions;
using FluentValidation;
using Sardanapal.Identity.ViewModel.Extensions;
using Xunit;

namespace Sardanapal.Identity.ViewModel.Tests.Unit;

public class PhoneNumberValidationTests
{
    private class SampleLong
    {
        public long PhoneNumber { get; set; }
    }

    private class SampleString
    {
        public string PhoneNumber { get; set; } = string.Empty;
    }

    private static InlineValidator<SampleLong> NewLongValidator()
    {
        var v = new InlineValidator<SampleLong>();
        v.RuleFor(x => x.PhoneNumber).PhoneNumber();
        return v;
    }

    private static InlineValidator<SampleString> NewStringValidator()
    {
        var v = new InlineValidator<SampleString>();
        v.RuleFor(x => x.PhoneNumber).PhoneNumber();
        return v;
    }

    [Theory]
    [InlineData(9000000001L)]
    [InlineData(9123456789L)]
    [InlineData(9876543210L)]
    public void PhoneNumber_Long_Must_Be_Greater_Than_9000000000(long phone)
    {
        var validator = NewLongValidator();

        var result = validator.Validate(new SampleLong { PhoneNumber = phone });

        result.IsValid.Should().BeTrue($"{phone} should be valid");
    }

    [Theory]
    [InlineData(9000000000L)]
    [InlineData(1L)]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void PhoneNumber_Long_Rejects_At_Or_Below_9000000000(long phone)
    {
        var validator = NewLongValidator();

        var result = validator.Validate(new SampleLong { PhoneNumber = phone });

        result.IsValid.Should().BeFalse($"{phone} should be rejected");
    }

    [Theory]
    [InlineData("09123456789")]
    [InlineData("9123456789")]
    [InlineData("+989123456789")]
    [InlineData("00989123456789")]
    [InlineData("989123456789")]
    public void PhoneNumber_String_Accepts_Local_And_Intl_Formats(string phone)
    {
        var validator = NewStringValidator();

        var result = validator.Validate(new SampleString { PhoneNumber = phone });

        result.IsValid.Should().BeTrue($"'{phone}' should be valid");
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abc1234567")]
    [InlineData("08123456789")]
    [InlineData("+9812345678")]
    [InlineData("")]
    public void PhoneNumber_String_Rejects_Invalid_Formats(string phone)
    {
        var validator = NewStringValidator();

        var result = validator.Validate(new SampleString { PhoneNumber = phone });

        result.IsValid.Should().BeFalse($"'{phone}' should be rejected");
    }
}
