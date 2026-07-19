using FluentAssertions;
using FluentValidation;
using Sardanapal.Identity.ViewModel.Extensions;
using Xunit;

namespace Sardanapal.Identity.ViewModel.Tests.Unit;

public class PasswordValidationTests
{
    private class Sample
    {
        public string Password { get; set; } = string.Empty;
    }

    private static InlineValidator<Sample> NewValidator()
    {
        var v = new InlineValidator<Sample>();
        v.RuleFor(x => x.Password).Password();
        return v;
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("Abcd1")]
    [InlineData("Password123")]
    [InlineData("aB3d")]
    public void Password_Valid_Accepted(string password)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Password = password });

        result.IsValid.Should().BeTrue($"'{password}' should be a valid basic password");
    }

    [Theory]
    [InlineData("1234")]
    [InlineData("99999")]
    public void Password_Rejects_Digits_Only(string password)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Password = password });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("a1")]
    [InlineData("Ab1")]
    public void Password_Rejects_Too_Short(string password)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Password = password });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("P@ssword1")]
    [InlineData("Abcd!")]
    [InlineData("abc#123")]
    public void Password_Rejects_Special_Chars(string password)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Password = password });

        result.IsValid.Should().BeFalse();
    }
}
