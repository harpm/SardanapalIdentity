using System.Reflection;
using FluentAssertions;
using Sardanapal.Identity.Localization;
using Xunit;

namespace Sardanapal.Identity.Localization.Tests.Unit;

public class IdentityMessagesSmokeTests
{
    private static readonly string[] ReferencedKeys =
    {
        nameof(Identity_Messages.AccountLockedOut),
        nameof(Identity_Messages.AlreadyVerified),
        nameof(Identity_Messages.DifferentPassword),
        nameof(Identity_Messages.DuplicateEmail),
        nameof(Identity_Messages.DuplicatePhoneNumber),
        nameof(Identity_Messages.DuplicateUsername),
        nameof(Identity_Messages.EmailNotFound),
        nameof(Identity_Messages.FailedGeneratingToken),
        nameof(Identity_Messages.InvalidEmailOrNumber),
        nameof(Identity_Messages.InvalidOtpCode),
        nameof(Identity_Messages.OtpCooldown),
        nameof(Identity_Messages.OtpCodeExpired),
        nameof(Identity_Messages.UserNotFound),
        nameof(Identity_Messages.WrongPassword)
    };

    [Fact]
    public void Localization_All_Referenced_Message_Keys_Exist_In_Designer()
    {
        foreach (string key in ReferencedKeys)
        {
            PropertyInfo? prop = typeof(Identity_Messages).GetProperty(key, BindingFlags.Public | BindingFlags.Static);
            prop.Should().NotBeNull($"Identity_Messages.{key} must exist in the designer");

            string? value = (string?)prop!.GetValue(null);
            value.Should().NotBeNullOrWhiteSpace($"Identity_Messages.{key} must resolve to a non-empty resource string");
        }
    }

    [Fact]
    public void Identity_Messages_Designer_Class_Is_Public()
    {
        Type type = typeof(Identity_Messages);

        type.IsPublic.Should().BeTrue("Identity_Messages must be public so consumers can reference localized strings");
    }

    [Fact]
    public void Every_Designer_Property_Resolves_To_NonEmpty_Value()
    {
        PropertyInfo[] props = typeof(Identity_Messages)
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(p => p.PropertyType == typeof(string) && p.GetMethod != null)
            .ToArray();

        props.Should().NotBeEmpty();

        foreach (PropertyInfo prop in props)
        {
            string? value = (string?)prop.GetValue(null);
            value.Should().NotBeNullOrWhiteSpace($"designer property {prop.Name} resolved to an empty resource value");
        }
    }
}
