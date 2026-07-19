using FluentAssertions;
using FluentValidation;
using Sardanapal.Identity.ViewModel.Extensions;
using Xunit;

namespace Sardanapal.Identity.ViewModel.Tests.Unit;

public class PasswordPlusCapitalLetterValidationTests
{
    private class Sample
    {
        public string Password { get; set; } = string.Empty;
    }

    private static InlineValidator<Sample> NewValidator()
    {
        var v = new InlineValidator<Sample>();
        v.RuleFor(x => x.Password).PasswordPlusCapitalLetter();
        return v;
    }

    [Theory]
    [InlineData("Abcdef12")]
    [InlineData("Password1")]
    [InlineData("aA1bcdef")]
    public void PasswordPlusCapitalLetter_Requires_Lower_Upper_Digit_And_Min_8(string password)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Password = password });

        result.IsValid.Should().BeTrue($"'{password}' satisfies all classes and length");
    }

    [Theory]
    [InlineData("ABCDEFG1", "missing lowercase")]
    [InlineData("abcdefg1", "missing uppercase")]
    [InlineData("Abcdefgh", "missing digit")]
    [InlineData("Ab1", "too short")]
    [InlineData("ABCDEFGH", "missing lowercase and digit")]
    [InlineData("abcdefgH", "missing digit")]
    public void PasswordPlusCapitalLetter_Rejects_Missing_Each_Class(string password, string reason)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Password = password });

        result.IsValid.Should().BeFalse($"{reason}: '{password}'");
    }
}
