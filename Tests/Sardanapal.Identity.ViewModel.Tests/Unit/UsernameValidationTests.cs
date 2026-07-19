using FluentAssertions;
using FluentValidation;
using Sardanapal.Identity.ViewModel.Extensions;
using Xunit;

namespace Sardanapal.Identity.ViewModel.Tests.Unit;

public class UsernameValidationTests
{
    private class Sample
    {
        public string Username { get; set; } = string.Empty;
    }

    private static InlineValidator<Sample> NewValidator()
    {
        var v = new InlineValidator<Sample>();
        v.RuleFor(x => x.Username).Username();
        return v;
    }

    [Theory]
    [InlineData("abcd")]
    [InlineData("user.name")]
    [InlineData("user_name")]
    [InlineData("user123")]
    [InlineData("User.Name_99")]
    [InlineData("1234")]
    public void Username_Valid_Inputs_Accepted(string username)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Username = username });

        result.IsValid.Should().BeTrue($"'{username}' should be a valid username");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("abc")]
    public void Username_Rejects_Too_Short(string username)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Username = username });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Username_Rejects_Too_Long()
    {
        string username = new string('a', 21);
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Username = username });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData(".starts-with-dot")]
    [InlineData("_starts-with-underscore")]
    [InlineData("ends-with-dot.")]
    [InlineData("ends-with-underscore_")]
    public void Username_Rejects_Leading_Or_Trailing_Underscore_Or_Dot(string username)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Username = username });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("double..dot")]
    [InlineData("double__underscore")]
    [InlineData("mixed_.dot")]
    public void Username_Rejects_Consecutive_Underscore_Or_Dot(string username)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Username = username });

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("user@name")]
    [InlineData("user-name")]
    [InlineData("user space")]
    [InlineData("user#")]
    public void Username_Rejects_Special_Characters(string username)
    {
        var validator = NewValidator();

        var result = validator.Validate(new Sample { Username = username });

        result.IsValid.Should().BeFalse();
    }
}
